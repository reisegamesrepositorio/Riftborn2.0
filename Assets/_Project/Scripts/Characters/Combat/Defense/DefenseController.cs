using System;
using System.Collections.Generic;
using UnityEngine;

namespace Riftborn.Characters.Defense
{
    public sealed class DefenseChangedEventArgs : EventArgs
    {
        public DefenseChangedEventArgs(
            DefenseType defenseType,
            float oldValue,
            float newValue)
        {
            DefenseType = defenseType;
            OldValue = oldValue;
            NewValue = newValue;
        }

        public DefenseType DefenseType { get; }

        public float OldValue { get; }

        public float NewValue { get; }

        public float Delta => NewValue - OldValue;
    }

    public sealed class DefenseController : MonoBehaviour
    {
        [Header("Base Defenses")]
        [SerializeField, Min(0f)]
        private float basePhysicalDefense;

        [SerializeField, Min(0f)]
        private float baseMagicalDefense;

        private readonly Dictionary<DefenseType, List<DefenseModifier>>
            modifiersByType = new();

        private readonly Dictionary<string, DefenseModifier>
            modifiersById = new(StringComparer.Ordinal);

        private readonly Dictionary<DefenseType, float>
            cachedFinalValues = new();

        private bool initialized;

        public event Action<DefenseChangedEventArgs> DefenseChanged;

        private void Awake()
        {
            EnsureInitialized();
        }

        private void OnValidate()
        {
            basePhysicalDefense =
                Mathf.Max(0f, basePhysicalDefense);

            baseMagicalDefense =
                Mathf.Max(0f, baseMagicalDefense);
        }

        public float GetBaseValue(DefenseType defenseType)
        {
            return defenseType switch
            {
                DefenseType.Physical =>
                    basePhysicalDefense,

                DefenseType.Magical =>
                    baseMagicalDefense,

                _ => throw new ArgumentOutOfRangeException(
                    nameof(defenseType),
                    defenseType,
                    "Unsupported defense type.")
            };
        }

        public float GetFinalValue(DefenseType defenseType)
        {
            EnsureInitialized();

            return cachedFinalValues[defenseType];
        }

        public void SetBaseValue(
            DefenseType defenseType,
            float value)
        {
            EnsureInitialized();

            float safeValue = Mathf.Max(0f, value);

            switch (defenseType)
            {
                case DefenseType.Physical:
                    basePhysicalDefense = safeValue;
                    break;

                case DefenseType.Magical:
                    baseMagicalDefense = safeValue;
                    break;

                default:
                    throw new ArgumentOutOfRangeException(
                        nameof(defenseType),
                        defenseType,
                        "Unsupported defense type.");
            }

            Recalculate(defenseType);
        }

        public bool AddModifier(DefenseModifier modifier)
        {
            if (modifier == null)
            {
                return false;
            }

            EnsureInitialized();

            if (modifiersById.ContainsKey(modifier.Id))
            {
                Debug.LogWarning(
                    $"A defense modifier with ID " +
                    $"'{modifier.Id}' is already active.",
                    this);

                return false;
            }

            modifiersByType[modifier.DefenseType]
                .Add(modifier);

            modifiersById.Add(
                modifier.Id,
                modifier);

            Recalculate(modifier.DefenseType);

            return true;
        }

        public bool RemoveModifier(string modifierId)
        {
            if (string.IsNullOrWhiteSpace(modifierId))
            {
                return false;
            }

            EnsureInitialized();

            if (!modifiersById.TryGetValue(
                    modifierId,
                    out DefenseModifier modifier))
            {
                return false;
            }

            modifiersById.Remove(modifierId);

            bool removed =
                modifiersByType[modifier.DefenseType]
                    .Remove(modifier);

            if (removed)
            {
                Recalculate(modifier.DefenseType);
            }

            return removed;
        }

        public int RemoveModifiersFromSource(object source)
        {
            if (source == null)
            {
                return 0;
            }

            EnsureInitialized();

            int totalRemoved = 0;

            HashSet<DefenseType> changedTypes = new();

            foreach (
                KeyValuePair<DefenseType, List<DefenseModifier>> pair
                in modifiersByType)
            {
                List<DefenseModifier> modifiers = pair.Value;

                for (int index = modifiers.Count - 1;
                     index >= 0;
                     index--)
                {
                    DefenseModifier modifier =
                        modifiers[index];

                    if (!Equals(modifier.Source, source))
                    {
                        continue;
                    }

                    modifiers.RemoveAt(index);
                    modifiersById.Remove(modifier.Id);

                    totalRemoved++;
                    changedTypes.Add(pair.Key);
                }
            }

            foreach (DefenseType defenseType in changedTypes)
            {
                Recalculate(defenseType);
            }

            return totalRemoved;
        }

        public bool HasModifier(string modifierId)
        {
            if (string.IsNullOrWhiteSpace(modifierId))
            {
                return false;
            }

            EnsureInitialized();

            return modifiersById.ContainsKey(modifierId);
        }

        private void EnsureInitialized()
        {
            if (initialized)
            {
                return;
            }

            foreach (
                DefenseType defenseType
                in Enum.GetValues(typeof(DefenseType)))
            {
                modifiersByType[defenseType] =
                    new List<DefenseModifier>();

                cachedFinalValues[defenseType] =
                    GetBaseValue(defenseType);
            }

            initialized = true;
        }

        private void Recalculate(DefenseType defenseType)
        {
            EnsureInitialized();

            float oldValue =
                cachedFinalValues[defenseType];

            float newValue =
                CalculateFinalValue(defenseType);

            cachedFinalValues[defenseType] =
                newValue;

            if (Mathf.Approximately(
                    oldValue,
                    newValue))
            {
                return;
            }

            DefenseChanged?.Invoke(
                new DefenseChangedEventArgs(
                    defenseType,
                    oldValue,
                    newValue));
        }

        private float CalculateFinalValue(
            DefenseType defenseType)
        {
            float flatTotal = 0f;
            float additivePercentTotal = 0f;
            float multiplicativeFactor = 1f;

            List<DefenseModifier> modifiers =
                modifiersByType[defenseType];

            foreach (DefenseModifier modifier in modifiers)
            {
                flatTotal += modifier.FlatValue;

                additivePercentTotal +=
                    modifier.AdditivePercent;

                multiplicativeFactor *=
                    1f + modifier.MultiplicativePercent;
            }

            float finalValue =
                GetBaseValue(defenseType);

            finalValue += flatTotal;

            finalValue *=
                1f + additivePercentTotal;

            finalValue *= multiplicativeFactor;

            // Permitimos defesa negativa por causa de debuffs.
            return finalValue;
        }
    }
}