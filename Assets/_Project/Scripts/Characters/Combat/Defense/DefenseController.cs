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

        public float Delta =>
            NewValue - OldValue;
    }

    [Serializable]
    public sealed class DefenseController
    {
        [Header("Base Defenses")]
        [SerializeField, Min(0f)]
        private float basePhysicalDefense;

        [SerializeField, Min(0f)]
        private float baseMagicalDefense;

        private Dictionary<
            DefenseType,
            List<DefenseModifier>>
            modifiersByType = new();

        private Dictionary<string, DefenseModifier>
            modifiersById =
                new(StringComparer.Ordinal);

        private Dictionary<DefenseType, float>
            cachedFinalValues = new();

        [NonSerialized]
        private UnityEngine.Object owner;

        private bool initialized;

        public event Action<DefenseChangedEventArgs>
            DefenseChanged;

        public void Initialize(
            UnityEngine.Object ownerContext)
        {
            owner = ownerContext;
            EnsureInitialized();
        }

        public void Validate()
        {
            basePhysicalDefense =
                Mathf.Max(
                    0f,
                    basePhysicalDefense);

            baseMagicalDefense =
                Mathf.Max(
                    0f,
                    baseMagicalDefense);
        }

        public float GetBaseValue(
            DefenseType defenseType)
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

        public float GetFinalValue(
            DefenseType defenseType)
        {
            EnsureInitialized();

            return cachedFinalValues[
                defenseType];
        }

        public void SetBaseValue(
            DefenseType defenseType,
            float value)
        {
            EnsureInitialized();

            float safeValue =
                Mathf.Max(
                    0f,
                    value);

            switch (defenseType)
            {
                case DefenseType.Physical:
                    basePhysicalDefense =
                        safeValue;
                    break;

                case DefenseType.Magical:
                    baseMagicalDefense =
                        safeValue;
                    break;

                default:
                    throw new ArgumentOutOfRangeException(
                        nameof(defenseType),
                        defenseType,
                        "Unsupported defense type.");
            }

            Recalculate(defenseType);
        }

        public bool AddModifier(
            DefenseModifier modifier)
        {
            if (modifier == null)
            {
                return false;
            }

            EnsureInitialized();

            if (modifiersById.ContainsKey(
                    modifier.Id))
            {
                Debug.LogWarning(
                    $"A defense modifier with ID " +
                    $"'{modifier.Id}' is already active.",
                    owner);

                return false;
            }

            modifiersByType[
                    modifier.DefenseType]
                .Add(modifier);

            modifiersById.Add(
                modifier.Id,
                modifier);

            Recalculate(
                modifier.DefenseType);

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

            EnsureInitialized();

            if (!modifiersById.TryGetValue(
                    modifierId,
                    out DefenseModifier modifier))
            {
                return false;
            }

            modifiersById.Remove(
                modifierId);

            bool removed =
                modifiersByType[
                        modifier.DefenseType]
                    .Remove(modifier);

            if (removed)
            {
                Recalculate(
                    modifier.DefenseType);
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

            EnsureInitialized();

            int totalRemoved = 0;

            HashSet<DefenseType> changedTypes =
                new();

            foreach (
                KeyValuePair<
                    DefenseType,
                    List<DefenseModifier>> pair
                in modifiersByType)
            {
                List<DefenseModifier> modifiers =
                    pair.Value;

                for (int index =
                         modifiers.Count - 1;
                     index >= 0;
                     index--)
                {
                    DefenseModifier modifier =
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
                    changedTypes.Add(pair.Key);
                }
            }

            foreach (
                DefenseType defenseType
                in changedTypes)
            {
                Recalculate(defenseType);
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

            EnsureInitialized();

            return modifiersById.ContainsKey(
                modifierId);
        }

        private void EnsureInitialized()
        {
            if (initialized)
            {
                return;
            }

            Validate();

            modifiersByType ??=
                new Dictionary<DefenseType, List<DefenseModifier>>();

            modifiersById ??=
                new Dictionary<string, DefenseModifier>(
                    StringComparer.Ordinal);

            cachedFinalValues ??=
                new Dictionary<DefenseType, float>();

            modifiersByType.Clear();
            modifiersById.Clear();
            cachedFinalValues.Clear();

            foreach (
                DefenseType defenseType
                in Enum.GetValues(
                    typeof(DefenseType)))
            {
                modifiersByType[defenseType] =
                    new List<DefenseModifier>();

                cachedFinalValues[defenseType] =
                    GetBaseValue(defenseType);
            }

            initialized = true;
        }

        private void Recalculate(
            DefenseType defenseType)
        {
            EnsureInitialized();

            float oldValue =
                cachedFinalValues[
                    defenseType];

            float newValue =
                CalculateFinalValue(
                    defenseType);

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
                modifiersByType[
                    defenseType];

            foreach (
                DefenseModifier modifier
                in modifiers)
            {
                flatTotal +=
                    modifier.FlatValue;

                additivePercentTotal +=
                    modifier.AdditivePercent;

                multiplicativeFactor *=
                    1f +
                    modifier.MultiplicativePercent;
            }

            float finalValue =
                GetBaseValue(
                    defenseType);

            finalValue +=
                flatTotal;

            finalValue *=
                1f +
                additivePercentTotal;

            finalValue *=
                multiplicativeFactor;

            return finalValue;
        }
    }
}
