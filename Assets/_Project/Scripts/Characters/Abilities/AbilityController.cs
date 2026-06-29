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

        private readonly Dictionary<
            AbilityModifierType,
            List<AbilityModifier>>
            modifiersByType = new();

        private readonly Dictionary<string, AbilityModifier>
            modifiersById =
                new(StringComparer.Ordinal);

        private CharacterContext context;
        private bool modifiersInitialized;

        public event Action<int, AbilityBase> AbilityUsed;

        public event Action AbilityValuesChanged;

        public int SlotCount =>
            equippedAbilities?.Length ?? 0;

        private void Awake()
        {
            CacheReferences();
            EnsureModifiersInitialized();
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
                    resources.Restore(
                        resourceCost);
                }

                Debug.LogException(
                    exception,
                    this);

                return false;
            }

            if (!executionSucceeded)
            {
                if (resourceConsumed)
                {
                    resources.Restore(
                        resourceCost);
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
                    !resources.CanConsume(
                        resourceCost))
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

            equippedAbilities[slot] =
                ability;

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

            return GetRemainingCooldown(
                ability);
        }

        public float ApplyDamageModifiers(
            float baseDamage)
        {
            float safeBaseDamage =
                Mathf.Max(
                    0f,
                    baseDamage);

            float finalDamage =
                ApplyModifiers(
                    AbilityModifierType.AbilityDamage,
                    safeBaseDamage);

            return Mathf.Max(
                0f,
                finalDamage);
        }

        public bool AddModifier(
            AbilityModifier modifier)
        {
            if (modifier == null)
            {
                return false;
            }

            EnsureModifiersInitialized();

            if (modifiersById.ContainsKey(
                    modifier.Id))
            {
                Debug.LogWarning(
                    $"[ABILITY] Já existe um modificador " +
                    $"com o ID '{modifier.Id}'.",
                    this);

                return false;
            }

            modifiersByType[
                    modifier.ModifierType]
                .Add(modifier);

            modifiersById.Add(
                modifier.Id,
                modifier);

            AbilityValuesChanged?.Invoke();

            return true;
        }

        public bool RemoveModifier(
            string modifierId)
        {
            if (string.IsNullOrWhiteSpace(
                    modifierId))
            {
                return false;
            }

            EnsureModifiersInitialized();

            if (!modifiersById.TryGetValue(
                    modifierId,
                    out AbilityModifier modifier))
            {
                return false;
            }

            modifiersById.Remove(
                modifierId);

            bool removed =
                modifiersByType[
                        modifier.ModifierType]
                    .Remove(modifier);

            if (removed)
            {
                AbilityValuesChanged?.Invoke();
            }

            return removed;
        }

        public int RemoveModifiersFromSource(
            object source)
        {
            if (source == null)
            {
                return 0;
            }

            EnsureModifiersInitialized();

            int totalRemoved = 0;

            foreach (
                KeyValuePair<
                    AbilityModifierType,
                    List<AbilityModifier>> pair
                in modifiersByType)
            {
                List<AbilityModifier> modifiers =
                    pair.Value;

                for (int index =
                         modifiers.Count - 1;
                     index >= 0;
                     index--)
                {
                    AbilityModifier modifier =
                        modifiers[index];

                    if (!Equals(
                            modifier.Source,
                            source))
                    {
                        continue;
                    }

                    modifiers.RemoveAt(index);

                    modifiersById.Remove(
                        modifier.Id);

                    totalRemoved++;
                }
            }

            if (totalRemoved > 0)
            {
                AbilityValuesChanged?.Invoke();
            }

            return totalRemoved;
        }

        public bool HasModifier(
            string modifierId)
        {
            if (string.IsNullOrWhiteSpace(
                    modifierId))
            {
                return false;
            }

            EnsureModifiersInitialized();

            return modifiersById.ContainsKey(
                modifierId);
        }

        [ContextMenu("Log Current Ability Modifiers")]
        public void LogCurrentAbilityModifiers()
        {
            GetModifierTotals(
                AbilityModifierType.AbilityDamage,
                out float flatValue,
                out float additivePercent,
                out float multiplicativeFactor);

            Debug.Log(
                $"[ABILITY VALUES] " +
                $"Dano fixo: {flatValue:0.##} | " +
                $"Dano aditivo: " +
                $"{additivePercent * 100f:0.##}% | " +
                $"Multiplicador: " +
                $"{multiplicativeFactor:0.##}x | " +
                $"Exemplo sobre 25: " +
                $"{ApplyDamageModifiers(25f):0.##}",
                this);
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
                cooldownEnds.Remove(
                    ability);

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
                    cooldownEnd -
                    Time.time);

            if (remainingCooldown <= 0f)
            {
                cooldownEnds.Remove(
                    ability);
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
                cooldownEnds.Remove(
                    ability);

                return;
            }

            cooldownEnds[ability] =
                Time.time +
                cooldownDuration;
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
                   slot <
                   equippedAbilities.Length;
        }

        private float ApplyModifiers(
            AbilityModifierType modifierType,
            float baseValue)
        {
            GetModifierTotals(
                modifierType,
                out float flatValue,
                out float additivePercent,
                out float multiplicativeFactor);

            float finalValue =
                baseValue +
                flatValue;

            finalValue *=
                1f +
                additivePercent;

            finalValue *=
                multiplicativeFactor;

            return finalValue;
        }

        private void GetModifierTotals(
            AbilityModifierType modifierType,
            out float flatValue,
            out float additivePercent,
            out float multiplicativeFactor)
        {
            EnsureModifiersInitialized();

            flatValue = 0f;
            additivePercent = 0f;
            multiplicativeFactor = 1f;

            List<AbilityModifier> modifiers =
                modifiersByType[
                    modifierType];

            for (int index = 0;
                 index < modifiers.Count;
                 index++)
            {
                AbilityModifier modifier =
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

        private void EnsureModifiersInitialized()
        {
            if (modifiersInitialized)
            {
                return;
            }

            foreach (
                AbilityModifierType modifierType
                in Enum.GetValues(
                    typeof(AbilityModifierType)))
            {
                modifiersByType[modifierType] =
                    new List<AbilityModifier>();
            }

            modifiersInitialized = true;
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