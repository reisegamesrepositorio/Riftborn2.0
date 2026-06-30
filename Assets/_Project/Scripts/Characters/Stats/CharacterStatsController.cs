using System;
using System.Collections.Generic;
using UnityEngine;

namespace Riftborn.Characters.Stats
{
    public sealed class StatChangedEventArgs : EventArgs
    {
        public StatChangedEventArgs(
            CharacterStat stat,
            float oldValue,
            float newValue)
        {
            Stat = stat;
            OldValue = oldValue;
            NewValue = newValue;
        }

        public CharacterStat Stat { get; }

        public float OldValue { get; }

        public float NewValue { get; }
    }

    [Serializable]
    public sealed class CharacterStatsController
    {
        [Header("Base Attributes")]
        [SerializeField]
        private float baseSTR = 10f;

        [SerializeField]
        private float baseDEX = 10f;

        [SerializeField]
        private float baseWIS = 10f;

        [SerializeField]
        private float baseISP = 10f;

        [SerializeField]
        private float baseFORT = 10f;

        private Dictionary<
            CharacterStat,
            List<StatModifier>>
            modifiersByStat = new();

        private Dictionary<string, StatModifier>
            modifiersById =
                new(StringComparer.Ordinal);

        private Dictionary<CharacterStat, float>
            cachedFinalValues = new();

        [NonSerialized]
        private UnityEngine.Object owner;

        private bool initialized;

        public event Action<StatChangedEventArgs>
            StatChanged;

        public void Initialize(
            UnityEngine.Object ownerContext)
        {
            owner = ownerContext;
            EnsureInitialized();
        }

        public void Validate()
        {
            baseSTR = Sanitize(baseSTR);
            baseDEX = Sanitize(baseDEX);
            baseWIS = Sanitize(baseWIS);
            baseISP = Sanitize(baseISP);
            baseFORT = Sanitize(baseFORT);
        }

        public float GetBaseValue(
            CharacterStat stat)
        {
            return stat switch
            {
                CharacterStat.STR => baseSTR,
                CharacterStat.DEX => baseDEX,
                CharacterStat.WIS => baseWIS,
                CharacterStat.ISP => baseISP,
                CharacterStat.FORT => baseFORT,

                _ => throw new ArgumentOutOfRangeException(
                    nameof(stat),
                    stat,
                    "Unsupported character stat.")
            };
        }

        public float GetFinalValue(
            CharacterStat stat)
        {
            EnsureInitialized();

            return cachedFinalValues[stat];
        }

        public void SetBaseValue(
            CharacterStat stat,
            float value)
        {
            EnsureInitialized();

            float safeValue =
                Sanitize(value);

            switch (stat)
            {
                case CharacterStat.STR:
                    baseSTR = safeValue;
                    break;

                case CharacterStat.DEX:
                    baseDEX = safeValue;
                    break;

                case CharacterStat.WIS:
                    baseWIS = safeValue;
                    break;

                case CharacterStat.ISP:
                    baseISP = safeValue;
                    break;

                case CharacterStat.FORT:
                    baseFORT = safeValue;
                    break;

                default:
                    throw new ArgumentOutOfRangeException(
                        nameof(stat),
                        stat,
                        "Unsupported character stat.");
            }

            Recalculate(stat);
        }

        public float AddBaseValue(
            CharacterStat stat,
            float amount)
        {
            if (float.IsNaN(amount) ||
                float.IsInfinity(amount))
            {
                throw new ArgumentOutOfRangeException(
                    nameof(amount),
                    amount,
                    "The base stat change must be finite.");
            }

            float newBaseValue =
                GetBaseValue(stat) +
                amount;

            SetBaseValue(
                stat,
                newBaseValue);

            return newBaseValue;
        }

        public bool AddModifier(
            StatModifier modifier)
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
                    $"A stat modifier with ID '{modifier.Id}' " +
                    "is already active.",
                    owner);

                return false;
            }

            modifiersByStat[
                    modifier.Stat]
                .Add(modifier);

            modifiersById.Add(
                modifier.Id,
                modifier);

            Recalculate(
                modifier.Stat);

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
                    out StatModifier modifier))
            {
                return false;
            }

            modifiersById.Remove(
                modifierId);

            bool removed =
                modifiersByStat[
                        modifier.Stat]
                    .Remove(modifier);

            if (removed)
            {
                Recalculate(
                    modifier.Stat);
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

            HashSet<CharacterStat> changedStats =
                new();

            foreach (
                KeyValuePair<
                    CharacterStat,
                    List<StatModifier>> pair
                in modifiersByStat)
            {
                List<StatModifier> statModifiers =
                    pair.Value;

                for (int index =
                         statModifiers.Count - 1;
                     index >= 0;
                     index--)
                {
                    StatModifier modifier =
                        statModifiers[index];

                    if (!Equals(
                            modifier.Source,
                            source))
                    {
                        continue;
                    }

                    statModifiers.RemoveAt(index);
                    modifiersById.Remove(modifier.Id);
                    totalRemoved++;
                    changedStats.Add(pair.Key);
                }
            }

            foreach (
                CharacterStat stat
                in changedStats)
            {
                Recalculate(stat);
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

            modifiersByStat ??=
                new Dictionary<CharacterStat, List<StatModifier>>();

            modifiersById ??=
                new Dictionary<string, StatModifier>(
                    StringComparer.Ordinal);

            cachedFinalValues ??=
                new Dictionary<CharacterStat, float>();

            modifiersByStat.Clear();
            modifiersById.Clear();
            cachedFinalValues.Clear();

            foreach (
                CharacterStat stat
                in Enum.GetValues(
                    typeof(CharacterStat)))
            {
                modifiersByStat[stat] =
                    new List<StatModifier>();

                cachedFinalValues[stat] =
                    GetBaseValue(stat);
            }

            initialized = true;
        }

        private void Recalculate(
            CharacterStat stat)
        {
            EnsureInitialized();

            float oldValue =
                cachedFinalValues[stat];

            float newValue =
                CalculateFinalValue(stat);

            cachedFinalValues[stat] =
                newValue;

            if (Mathf.Approximately(
                    oldValue,
                    newValue))
            {
                return;
            }

            StatChanged?.Invoke(
                new StatChangedEventArgs(
                    stat,
                    oldValue,
                    newValue));
        }

        private float CalculateFinalValue(
            CharacterStat stat)
        {
            float flatTotal = 0f;
            float additivePercentTotal = 0f;
            float multiplicativeFactor = 1f;

            List<StatModifier> statModifiers =
                modifiersByStat[stat];

            foreach (
                StatModifier modifier
                in statModifiers)
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
                GetBaseValue(stat);

            finalValue +=
                flatTotal;

            finalValue *=
                1f +
                additivePercentTotal;

            finalValue *=
                multiplicativeFactor;

            return finalValue;
        }

        private static float Sanitize(
            float value)
        {
            return float.IsNaN(value) ||
                   float.IsInfinity(value)
                ? 0f
                : value;
        }
    }
}
