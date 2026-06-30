using System;
using System.Collections.Generic;
using Riftborn.Characters.Stats;
using Riftborn.Characters.StatusEffects;
using Riftborn.Damage;
using UnityEngine;

namespace Riftborn.Characters.Health
{
    public sealed class HealthChangedEventArgs : EventArgs
    {
        public HealthChangedEventArgs(
            float oldCurrent,
            float current,
            float oldMax,
            float max)
        {
            OldCurrent = oldCurrent;
            Current = current;
            OldMax = oldMax;
            Max = max;
        }

        public float OldCurrent { get; }

        public float Current { get; }

        public float OldMax { get; }

        public float Max { get; }

        public float Delta =>
            Current - OldCurrent;

        public float MaxDelta =>
            Max - OldMax;
    }

    [Serializable]
    public sealed class HealthController
    {
        [Header("Health Calculation")]
        [SerializeField, Min(1f)]
        private float baseMaxHealth = 100f;

        [SerializeField, Min(0f)]
        private float healthPerFort = 5f;

        [Header("Initial State")]
        [SerializeField, Min(0f)]
        private float currentHealth = 100f;

        [SerializeField]
        private bool startFullHealth = true;

        private List<HealthModifier>
            modifiers = new();

        private Dictionary<string, HealthModifier>
            modifiersById =
                new(StringComparer.Ordinal);

        [NonSerialized]
        private CharacterStatsController stats;

        [NonSerialized]
        private StatusEffectController statusEffects;

        [NonSerialized]
        private UnityEngine.Object owner;

        private float maxHealth;
        private bool initialized;
        private bool enabled;

        public event Action<HealthChangedEventArgs>
            HealthChanged;

        public event Action<DamageApplicationResult>
            DamageApplied;

        public event Action<DamageResult>
            DamageTaken;

        public event Action<float>
            Healed;

        public event Action Died;

        public event Action Revived;

        public float CurrentHealth =>
            currentHealth;

        public float MaxHealth =>
            maxHealth;

        public float BaseMaxHealth =>
            baseMaxHealth;

        public float HealthPerFort =>
            healthPerFort;

        public float HealthPercentage =>
            maxHealth > 0f
                ? currentHealth / maxHealth
                : 0f;

        public bool IsDead { get; private set; }

        public DamageApplicationResult
            LastDamageApplication { get; private set; }

        public void Initialize(
            UnityEngine.Object ownerContext,
            CharacterStatsController statsController,
            StatusEffectController statusEffectController)
        {
            owner = ownerContext;
            stats = statsController;
            statusEffects = statusEffectController;

            EnsureCollections();

            if (!initialized)
            {
                Validate();
                InitializeHealth();
                initialized = true;
            }
        }

        public void SetStatusEffects(
            StatusEffectController controller)
        {
            statusEffects = controller;
        }

        public void Enable()
        {
            if (enabled)
            {
                return;
            }

            if (!initialized)
            {
                Initialize(
                    owner,
                    stats,
                    statusEffects);
            }

            if (stats != null)
            {
                stats.StatChanged +=
                    HandleStatChanged;
            }

            enabled = true;
        }

        public void Disable()
        {
            if (!enabled)
            {
                return;
            }

            if (stats != null)
            {
                stats.StatChanged -=
                    HandleStatChanged;
            }

            enabled = false;
        }

        public void Validate()
        {
            baseMaxHealth =
                Mathf.Max(
                    1f,
                    baseMaxHealth);

            healthPerFort =
                Mathf.Max(
                    0f,
                    healthPerFort);

            currentHealth =
                Mathf.Max(
                    0f,
                    currentHealth);
        }

        public DamageApplicationResult ApplyDamage(
            DamageResult result)
        {
            if (result == null ||
                IsDead)
            {
                return null;
            }

            float incomingDamage =
                Mathf.Max(
                    0f,
                    result.FinalAmount);

            if (incomingDamage <= 0f)
            {
                return null;
            }

            float damageAfterShields =
                statusEffects != null
                    ? statusEffects.ResolveDamageAbsorption(
                        result,
                        incomingDamage)
                    : incomingDamage;

            damageAfterShields =
                Mathf.Clamp(
                    damageAfterShields,
                    0f,
                    incomingDamage);

            float absorbedDamage =
                incomingDamage -
                damageAfterShields;

            float oldHealth =
                currentHealth;

            if (damageAfterShields > 0f)
            {
                SetHealthState(
                    currentHealth -
                    damageAfterShields,
                    maxHealth);
            }

            float healthDamage =
                oldHealth -
                currentHealth;

            LastDamageApplication =
                new DamageApplicationResult(
                    damageResult: result,
                    incomingDamage: incomingDamage,
                    absorbedDamage: absorbedDamage,
                    damageAfterShields: damageAfterShields,
                    healthDamage: healthDamage,
                    oldHealth: oldHealth,
                    newHealth: currentHealth);

            DamageApplied?.Invoke(
                LastDamageApplication);

            if (healthDamage <= 0f)
            {
                return LastDamageApplication;
            }

            DamageTaken?.Invoke(result);

            if (currentHealth <= 0f &&
                !IsDead)
            {
                IsDead = true;
                Died?.Invoke();
            }

            return LastDamageApplication;
        }

        public void Heal(
            float amount)
        {
            if (IsDead)
            {
                return;
            }

            float requestedHealing =
                Mathf.Max(
                    0f,
                    amount);

            if (requestedHealing <= 0f)
            {
                return;
            }

            float oldHealth =
                currentHealth;

            SetHealthState(
                currentHealth +
                requestedHealing,
                maxHealth);

            float effectiveHealing =
                currentHealth -
                oldHealth;

            if (effectiveHealing <= 0f)
            {
                return;
            }

            Healed?.Invoke(
                effectiveHealing);
        }

        public void Revive(
            float healthAmount)
        {
            if (!IsDead)
            {
                return;
            }

            float revivedHealth =
                Mathf.Clamp(
                    healthAmount,
                    1f,
                    maxHealth);

            IsDead = false;

            SetHealthState(
                revivedHealth,
                maxHealth);

            Revived?.Invoke();
        }

        public void SetBaseMaxHealth(
            float value,
            bool fillToMax = false)
        {
            baseMaxHealth =
                Mathf.Max(
                    1f,
                    value);

            RecalculateMaxHealth(
                fillToMax);
        }

        public void SetHealthPerFort(
            float value,
            bool fillToMax = false)
        {
            healthPerFort =
                Mathf.Max(
                    0f,
                    value);

            RecalculateMaxHealth(
                fillToMax);
        }

        public void SetMaxHealth(
            float value,
            bool fillToMax = false)
        {
            SetBaseMaxHealth(
                value,
                fillToMax);
        }

        public bool AddModifier(
            HealthModifier modifier)
        {
            EnsureCollections();
            if (modifier == null)
            {
                return false;
            }

            if (modifiersById.ContainsKey(
                    modifier.Id))
            {
                Debug.LogWarning(
                    $"[HEALTH] Já existe um modificador " +
                    $"com o ID '{modifier.Id}'.",
                    owner);

                return false;
            }

            modifiers.Add(modifier);

            modifiersById.Add(
                modifier.Id,
                modifier);

            RecalculateMaxHealth(
                fillToMax: false);

            return true;
        }

        public bool RemoveModifier(
            string modifierId)
        {
            EnsureCollections();
            if (string.IsNullOrWhiteSpace(
                    modifierId))
            {
                return false;
            }

            if (!modifiersById.TryGetValue(
                    modifierId,
                    out HealthModifier modifier))
            {
                return false;
            }

            modifiersById.Remove(
                modifierId);

            bool removed =
                modifiers.Remove(
                    modifier);

            if (removed)
            {
                RecalculateMaxHealth(
                    fillToMax: false);
            }

            return removed;
        }

        public int RemoveModifiersFromSource(
            object source)
        {
            EnsureCollections();
            if (source == null)
            {
                return 0;
            }

            int totalRemoved = 0;

            for (int index =
                     modifiers.Count - 1;
                 index >= 0;
                 index--)
            {
                HealthModifier modifier =
                    modifiers[index];

                if (!Equals(
                        modifier.Source,
                        source))
                {
                    continue;
                }

                modifiers.RemoveAt(index);
                modifiersById.Remove(modifier.Id);
                totalRemoved++;
            }

            if (totalRemoved > 0)
            {
                RecalculateMaxHealth(
                    fillToMax: false);
            }

            return totalRemoved;
        }

        public bool HasModifier(
            string modifierId)
        {
            EnsureCollections();
            if (string.IsNullOrWhiteSpace(
                    modifierId))
            {
                return false;
            }

            return modifiersById.ContainsKey(
                modifierId);
        }

        public void LogCurrentHealthValues()
        {
            float finalFort =
                stats != null
                    ? stats.GetFinalValue(
                        CharacterStat.FORT)
                    : 0f;

            GetModifierTotals(
                out float flatValue,
                out float additivePercent,
                out float multiplicativeFactor);

            Debug.Log(
                $"[HEALTH VALUES] " +
                $"Vida: {CurrentHealth:0.##}/" +
                $"{MaxHealth:0.##} | " +
                $"Base: {baseMaxHealth:0.##} | " +
                $"FORT: {finalFort:0.##} | " +
                $"Vida por FORT: {healthPerFort:0.##} | " +
                $"Bônus fixo: {flatValue:0.##} | " +
                $"Bônus aditivo: " +
                $"{additivePercent * 100f:0.##}% | " +
                $"Multiplicador: " +
                $"{multiplicativeFactor:0.##}x",
                owner);
        }

        private void EnsureCollections()
        {
            modifiers ??=
                new List<HealthModifier>();

            modifiersById ??=
                new Dictionary<string, HealthModifier>(
                    StringComparer.Ordinal);
        }

        private void InitializeHealth()
        {
            maxHealth =
                CalculateMaxHealth();

            currentHealth =
                startFullHealth
                    ? maxHealth
                    : Mathf.Clamp(
                        currentHealth,
                        0f,
                        maxHealth);

            IsDead =
                currentHealth <= 0f;
        }

        private float CalculateMaxHealth()
        {
            float finalFort =
                stats != null
                    ? stats.GetFinalValue(
                        CharacterStat.FORT)
                    : 0f;

            float rawMaxHealth =
                baseMaxHealth +
                finalFort *
                healthPerFort;

            GetModifierTotals(
                out float flatValue,
                out float additivePercent,
                out float multiplicativeFactor);

            float finalMaxHealth =
                rawMaxHealth +
                flatValue;

            finalMaxHealth *=
                1f +
                additivePercent;

            finalMaxHealth *=
                multiplicativeFactor;

            return Mathf.Max(
                1f,
                finalMaxHealth);
        }

        private void GetModifierTotals(
            out float flatValue,
            out float additivePercent,
            out float multiplicativeFactor)
        {
            EnsureCollections();

            flatValue = 0f;
            additivePercent = 0f;
            multiplicativeFactor = 1f;

            for (int index = 0;
                 index < modifiers.Count;
                 index++)
            {
                HealthModifier modifier =
                    modifiers[index];

                flatValue +=
                    modifier.FlatValue;

                additivePercent +=
                    modifier.AdditivePercent;

                multiplicativeFactor *=
                    1f +
                    modifier.MultiplicativePercent;
            }
        }

        private void HandleStatChanged(
            StatChangedEventArgs eventArgs)
        {
            if (eventArgs.Stat !=
                CharacterStat.FORT)
            {
                return;
            }

            RecalculateMaxHealth(
                fillToMax: false);
        }

        private void RecalculateMaxHealth(
            bool fillToMax)
        {
            float oldMaxHealth =
                maxHealth;

            float oldCurrentHealth =
                currentHealth;

            float newMaxHealth =
                CalculateMaxHealth();

            float newCurrentHealth;

            if (fillToMax)
            {
                newCurrentHealth =
                    newMaxHealth;
            }
            else
            {
                float missingHealth =
                    Mathf.Max(
                        0f,
                        oldMaxHealth -
                        oldCurrentHealth);

                newCurrentHealth =
                    newMaxHealth -
                    missingHealth;
            }

            SetHealthState(
                newCurrentHealth,
                newMaxHealth);

            if (currentHealth <= 0f &&
                !IsDead)
            {
                IsDead = true;
                Died?.Invoke();
            }
        }

        private void SetHealthState(
            float newCurrentHealth,
            float newMaxHealth)
        {
            float oldCurrentHealth =
                currentHealth;

            float oldMaxHealth =
                maxHealth;

            maxHealth =
                Mathf.Max(
                    1f,
                    newMaxHealth);

            currentHealth =
                Mathf.Clamp(
                    newCurrentHealth,
                    0f,
                    maxHealth);

            bool currentChanged =
                !Mathf.Approximately(
                    oldCurrentHealth,
                    currentHealth);

            bool maxChanged =
                !Mathf.Approximately(
                    oldMaxHealth,
                    maxHealth);

            if (!currentChanged &&
                !maxChanged)
            {
                return;
            }

            HealthChanged?.Invoke(
                new HealthChangedEventArgs(
                    oldCurrentHealth,
                    currentHealth,
                    oldMaxHealth,
                    maxHealth));
        }
    }
}
