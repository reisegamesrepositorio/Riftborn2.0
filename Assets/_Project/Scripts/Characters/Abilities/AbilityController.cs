using System;
using System.Collections.Generic;
using Riftborn.Characters.ActionStates;
using Riftborn.Characters.Core;
using Riftborn.Characters.Resources;
using UnityEngine;

namespace Riftborn.Characters.Abilities
{
    public sealed class AbilityController : MonoBehaviour
    {
        [Header("Abilities")]
        [SerializeField]
        private AbilityBase[] equippedAbilities =
            new AbilityBase[8];

        [Header("References")]
        [SerializeField]
        private ActionStateController actionState;

        private readonly Dictionary<AbilityBase, float>
            cooldownEnds = new();

        private CharacterContext context;

        public event Action<int, AbilityBase> AbilityUsed;

        public int SlotCount =>
            equippedAbilities?.Length ?? 0;

        private void Awake()
        {
            CacheReferences();
        }

        public bool TryUse(
            int slot,
            CharacterContext target)
        {
            CacheReferences();

            if (!TryGetAbility(
                    slot,
                    out AbilityBase ability))
            {
                return false;
            }

            if (!CanUse(
                    slot,
                    target))
            {
                return false;
            }

            ResourceController resources =
                context?.Resources;

            float resourceCost =
                Mathf.Max(
                    0f,
                    ability.ResourceCost);

            bool resourceConsumed = false;

            if (resourceCost > 0f)
            {
                if (resources == null)
                {
                    return false;
                }

                if (!resources.Consume(resourceCost))
                {
                    return false;
                }

                resourceConsumed = true;
            }

            bool executionSucceeded;

            try
            {
                executionSucceeded =
                    ability.Execute(
                        context,
                        target);
            }
            catch (Exception exception)
            {
                if (resourceConsumed)
                {
                    resources.Restore(resourceCost);
                }

                Debug.LogException(
                    exception,
                    this);

                return false;
            }

            /*
             * A habilidade passou pelas validações, mas ainda
             * assim falhou durante a execução.
             *
             * Nesse caso, o recurso reservado é devolvido
             * e nenhum cooldown é iniciado.
             */
            if (!executionSucceeded)
            {
                if (resourceConsumed)
                {
                    resources.Restore(resourceCost);
                }

                return false;
            }

            StartCooldown(ability);

            AbilityUsed?.Invoke(
                slot,
                ability);

            context?.Events?.
                RaiseAbilityUsed(ability);

            return true;
        }

        public bool CanUse(
            int slot,
            CharacterContext target = null)
        {
            CacheReferences();

            if (context == null)
            {
                return false;
            }

            if (context.Health != null &&
                context.Health.IsDead)
            {
                return false;
            }

            if (actionState != null &&
                !actionState.CanCast)
            {
                return false;
            }

            if (!TryGetAbility(
                    slot,
                    out AbilityBase ability))
            {
                return false;
            }

            if (IsOnCooldown(ability))
            {
                return false;
            }

            float resourceCost =
                Mathf.Max(
                    0f,
                    ability.ResourceCost);

            if (resourceCost > 0f)
            {
                ResourceController resources =
                    context.Resources;

                if (resources == null ||
                    !resources.CanConsume(resourceCost))
                {
                    return false;
                }
            }

            return ability.CanExecute(
                context,
                target);
        }

        public AbilityBase GetAbility(int slot)
        {
            return IsValidSlot(slot)
                ? equippedAbilities[slot]
                : null;
        }

        public bool SetAbility(
            int slot,
            AbilityBase ability)
        {
            if (!IsValidSlot(slot))
            {
                return false;
            }

            if (ReferenceEquals(
                    equippedAbilities[slot],
                    ability))
            {
                return false;
            }

            equippedAbilities[slot] = ability;

            return true;
        }

        public bool ClearAbility(int slot)
        {
            return SetAbility(
                slot,
                null);
        }

        public bool IsOnCooldown(int slot)
        {
            return TryGetAbility(
                       slot,
                       out AbilityBase ability) &&
                   IsOnCooldown(ability);
        }

        public float GetRemainingCooldown(int slot)
        {
            if (!TryGetAbility(
                    slot,
                    out AbilityBase ability))
            {
                return 0f;
            }

            return GetRemainingCooldown(ability);
        }

        private bool IsOnCooldown(
            AbilityBase ability)
        {
            if (ability == null)
            {
                return false;
            }

            if (!cooldownEnds.TryGetValue(
                    ability,
                    out float cooldownEnd))
            {
                return false;
            }

            if (Time.time >= cooldownEnd)
            {
                cooldownEnds.Remove(ability);
                return false;
            }

            return true;
        }

        private float GetRemainingCooldown(
            AbilityBase ability)
        {
            if (ability == null)
            {
                return 0f;
            }

            if (!cooldownEnds.TryGetValue(
                    ability,
                    out float cooldownEnd))
            {
                return 0f;
            }

            float remainingCooldown =
                Mathf.Max(
                    0f,
                    cooldownEnd - Time.time);

            if (remainingCooldown <= 0f)
            {
                cooldownEnds.Remove(ability);
            }

            return remainingCooldown;
        }

        private void StartCooldown(
            AbilityBase ability)
        {
            if (ability == null)
            {
                return;
            }

            float cooldownDuration =
                Mathf.Max(
                    0f,
                    ability.Cooldown);

            if (cooldownDuration <= 0f)
            {
                cooldownEnds.Remove(ability);
                return;
            }

            cooldownEnds[ability] =
                Time.time + cooldownDuration;
        }

        private bool TryGetAbility(
            int slot,
            out AbilityBase ability)
        {
            ability = null;

            if (!IsValidSlot(slot))
            {
                return false;
            }

            ability =
                equippedAbilities[slot];

            return ability != null;
        }

        private bool IsValidSlot(int slot)
        {
            return equippedAbilities != null &&
                   slot >= 0 &&
                   slot < equippedAbilities.Length;
        }

        private void CacheReferences()
        {
            context ??=
                GetComponent<CharacterContext>();

            actionState ??=
                GetComponent<ActionStateController>();
        }
    }
}