using System;
using System.Collections.Generic;
using Riftborn.Characters.Core;
using Riftborn.Characters.Health;
using Riftborn.Damage;
using UnityEngine;

namespace Riftborn.Characters.StatusEffects
{
    public sealed class StatusEffectController : MonoBehaviour
    {
        private readonly List<StatusEffectBase>
            activeEffects = new();

        private readonly List<StatusEffectBase>
            updateBuffer = new();

        private readonly List<StatusEffectBase>
            damageAbsorberBuffer = new();

        private CharacterContext context;
        private HealthController health;
        private bool subscribedToHealth;

        public event Action<StatusEffectBase> StatusApplied;
        public event Action<StatusEffectBase> StatusRemoved;
        public event Action<StatusEffectBase> StatusReapplied;

        public IReadOnlyList<StatusEffectBase> ActiveEffects =>
            activeEffects;

        public CharacterContext Context
        {
            get
            {
                EnsureReferences();
                return context;
            }
        }

        private void Awake()
        {
            EnsureReferences();
        }

        private void OnEnable()
        {
            EnsureReferences();
            SubscribeToHealth();
        }

        private void OnDisable()
        {
            UnsubscribeFromHealth();
        }

        private void Update()
        {
            UpdateEffects(Time.deltaTime);
        }

        public bool Apply(StatusEffectBase effect)
        {
            EnsureReferences();

            if (effect == null)
            {
                return false;
            }

            if (context == null)
            {
                Debug.LogError(
                    $"{nameof(StatusEffectController)} requires a " +
                    $"{nameof(CharacterContext)}.",
                    this);

                return false;
            }

            if (!ReferenceEquals(effect.Target, context))
            {
                Debug.LogWarning(
                    $"Status effect '{effect.GetType().Name}' was created " +
                    $"for another target and cannot be applied to '{name}'.",
                    this);

                return false;
            }

            if (IsImmuneTo(effect.Tags))
            {
                return false;
            }

            StatusEffectBase existingEffect =
                FindCompatibleEffect(effect);

            if (existingEffect != null)
            {
                existingEffect.OnReapply(
                    this,
                    effect);

                StatusReapplied?.Invoke(existingEffect);

                return true;
            }

            activeEffects.Add(effect);

            effect.OnApply(this);

            StatusApplied?.Invoke(effect);

            context.Events?.RaiseStatusApplied(effect);

            return true;
        }

        public bool Remove(StatusEffectBase effect)
        {
            if (effect == null)
            {
                return false;
            }

            if (!activeEffects.Remove(effect))
            {
                return false;
            }

            effect.OnRemove(this);

            StatusRemoved?.Invoke(effect);

            context?.Events?.RaiseStatusRemoved(effect);

            return true;
        }

        public int Cleanse(StatusEffectTag tags)
        {
            if (tags == StatusEffectTag.None)
            {
                return 0;
            }

            int removedCount = 0;

            for (int index = activeEffects.Count - 1;
                 index >= 0;
                 index--)
            {
                StatusEffectBase effect =
                    activeEffects[index];

                if ((effect.Tags & tags) == 0)
                {
                    continue;
                }

                if (Remove(effect))
                {
                    removedCount++;
                }
            }

            return removedCount;
        }

        public int ClearAllEffects()
        {
            int removedCount = 0;

            while (activeEffects.Count > 0)
            {
                StatusEffectBase effect =
                    activeEffects[activeEffects.Count - 1];

                if (effect == null)
                {
                    activeEffects.RemoveAt(
                        activeEffects.Count - 1);

                    continue;
                }

                if (Remove(effect))
                {
                    removedCount++;
                }
            }

            updateBuffer.Clear();
            damageAbsorberBuffer.Clear();

            return removedCount;
        }

        public bool Has(StatusEffectTag tags)
        {
            if (tags == StatusEffectTag.None)
            {
                return false;
            }

            for (int index = 0;
                 index < activeEffects.Count;
                 index++)
            {
                StatusEffectBase effect =
                    activeEffects[index];

                if (effect != null &&
                    (effect.Tags & tags) != 0)
                {
                    return true;
                }
            }

            return false;
        }

        public int Count(StatusEffectTag tags)
        {
            if (tags == StatusEffectTag.None)
            {
                return 0;
            }

            int count = 0;

            for (int index = 0;
                 index < activeEffects.Count;
                 index++)
            {
                StatusEffectBase effect =
                    activeEffects[index];

                if (effect != null &&
                    (effect.Tags & tags) != 0)
                {
                    count++;
                }
            }

            return count;
        }

        public void UpdateEffects(float deltaTime)
        {
            float safeDeltaTime =
                Mathf.Max(0f, deltaTime);

            for (int index = activeEffects.Count - 1;
                 index >= 0;
                 index--)
            {
                if (activeEffects[index] == null)
                {
                    activeEffects.RemoveAt(index);
                }
            }

            updateBuffer.Clear();

            for (int index = 0;
                 index < activeEffects.Count;
                 index++)
            {
                updateBuffer.Add(activeEffects[index]);
            }

            for (int index = 0;
                 index < updateBuffer.Count;
                 index++)
            {
                StatusEffectBase effect =
                    updateBuffer[index];

                if (!activeEffects.Contains(effect))
                {
                    continue;
                }

                effect.Tick(
                    this,
                    safeDeltaTime);

                if (effect.IsExpired &&
                    activeEffects.Contains(effect))
                {
                    Remove(effect);
                }
            }

            updateBuffer.Clear();
        }

        /// <summary>
        /// Processa escudos e outros absorvedores na ordem
        /// em que foram aplicados.
        /// </summary>
        public float ResolveDamageAbsorption(
            DamageResult result,
            float incomingDamage)
        {
            float remainingDamage =
                Mathf.Max(0f, incomingDamage);

            if (remainingDamage <= 0f)
            {
                return 0f;
            }

            damageAbsorberBuffer.Clear();

            for (int index = 0;
                 index < activeEffects.Count;
                 index++)
            {
                StatusEffectBase effect =
                    activeEffects[index];

                if (effect is IDamageAbsorber)
                {
                    damageAbsorberBuffer.Add(effect);
                }
            }

            for (int index = 0;
                 index < damageAbsorberBuffer.Count;
                 index++)
            {
                if (remainingDamage <= 0f)
                {
                    break;
                }

                StatusEffectBase effect =
                    damageAbsorberBuffer[index];

                if (!activeEffects.Contains(effect))
                {
                    continue;
                }

                if (effect.IsExpired)
                {
                    Remove(effect);
                    continue;
                }

                IDamageAbsorber absorber =
                    (IDamageAbsorber)effect;

                remainingDamage =
                    Mathf.Max(
                        0f,
                        absorber.AbsorbDamage(
                            result,
                            remainingDamage));

                if (effect.IsExpired &&
                    activeEffects.Contains(effect))
                {
                    Remove(effect);
                }
            }

            damageAbsorberBuffer.Clear();

            return remainingDamage;
        }

        public DamageResult ApplyDamage(
            DamageRequest request)
        {
            if (request == null)
            {
                return DamageCalculator.Calculate(null);
            }

            DamageResult result =
                DamageCalculator.Calculate(request);

            DamageApplicationResult applicationResult =
                request.Target?.Health?.ApplyDamage(result);

            /*
             * Dano absorvido por Shield ainda conta como dano
             * processado pelo atacante.
             */
            if (applicationResult != null)
            {
                request.Source?.Events?.
                    RaiseDamageDealt(result);
            }

            /*
             * O alvo só recebe o evento de dano quando houve
             * perda real de vida depois dos escudos.
             *
             * Portanto, um dano totalmente absorvido não
             * remove SleepEffect.
             */
            if (applicationResult != null &&
                applicationResult.DamagedHealth)
            {
                request.Target?.Events?.
                    RaiseDamageTaken(result);
            }

            /*
             * Um acerto continua sendo crítico mesmo quando
             * o dano é absorvido por um escudo.
             */
            if (applicationResult != null &&
                result.WasCritical)
            {
                request.Source?.Events?.
                    RaiseCriticalHit(result);
            }

            return result;
        }

        private StatusEffectBase FindCompatibleEffect(
            StatusEffectBase incoming)
        {
            for (int index = 0;
                 index < activeEffects.Count;
                 index++)
            {
                StatusEffectBase activeEffect =
                    activeEffects[index];

                if (activeEffect != null &&
                    activeEffect.CanStackWith(incoming))
                {
                    return activeEffect;
                }
            }

            return null;
        }

        private bool IsImmuneTo(StatusEffectTag tags)
        {
            return false;
        }

        private void EnsureReferences()
        {
            context ??=
                GetComponent<CharacterContext>();

            health ??=
                GetComponent<HealthController>();
        }

        private void SubscribeToHealth()
        {
            if (subscribedToHealth || health == null)
            {
                return;
            }

            health.DamageTaken += HandleDamageTaken;
            subscribedToHealth = true;
        }

        private void UnsubscribeFromHealth()
        {
            if (!subscribedToHealth || health == null)
            {
                return;
            }

            health.DamageTaken -= HandleDamageTaken;
            subscribedToHealth = false;
        }

        private void HandleDamageTaken(DamageResult result)
        {
            for (int index = activeEffects.Count - 1;
                 index >= 0;
                 index--)
            {
                StatusEffectBase effect =
                    activeEffects[index];

                if (effect is not IRemoveOnDamage removable)
                {
                    continue;
                }

                if (removable.ShouldRemoveOnDamage(result))
                {
                    Remove(effect);
                }
            }
        }
    }
}