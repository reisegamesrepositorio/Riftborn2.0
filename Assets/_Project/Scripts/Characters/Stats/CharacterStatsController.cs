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

    public sealed class CharacterStatsController : MonoBehaviour
    {
        [Header("Base Attributes")]
        [SerializeField] private float baseSTR = 10f;
        [SerializeField] private float baseDEX = 10f;
        [SerializeField] private float baseWIS = 10f;
        [SerializeField] private float baseISP = 10f;
        [SerializeField] private float baseFORT = 10f;

        private readonly Dictionary<CharacterStat, List<StatModifier>>
            modifiersByStat = new();

        private readonly Dictionary<string, StatModifier>
            modifiersById = new(StringComparer.Ordinal);

        private readonly Dictionary<CharacterStat, float>
            cachedFinalValues = new();

        private bool initialized;

        public event Action<StatChangedEventArgs> StatChanged;

        private void Awake()
        {
            EnsureInitialized();
        }

        public float GetBaseValue(CharacterStat stat)
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

        public float GetFinalValue(CharacterStat stat)
        {
            EnsureInitialized();
            return cachedFinalValues[stat];
        }

        public void SetBaseValue(
            CharacterStat stat,
            float value)
        {
            EnsureInitialized();

            switch (stat)
            {
                case CharacterStat.STR:
                    baseSTR = value;
                    break;

                case CharacterStat.DEX:
                    baseDEX = value;
                    break;

                case CharacterStat.WIS:
                    baseWIS = value;
                    break;

                case CharacterStat.ISP:
                    baseISP = value;
                    break;

                case CharacterStat.FORT:
                    baseFORT = value;
                    break;

                default:
                    throw new ArgumentOutOfRangeException(
                        nameof(stat),
                        stat,
                        "Unsupported character stat.");
            }

            Recalculate(stat);
        }

        public bool AddModifier(StatModifier modifier)
        {
            if (modifier == null)
            {
                return false;
            }

            EnsureInitialized();

            if (modifiersById.ContainsKey(modifier.Id))
            {
                Debug.LogWarning(
                    $"A stat modifier with ID '{modifier.Id}' " +
                    "is already active.",
                    this);

                return false;
            }

            modifiersByStat[modifier.Stat].Add(modifier);
            modifiersById.Add(modifier.Id, modifier);

            Recalculate(modifier.Stat);

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
                    out StatModifier modifier))
            {
                return false;
            }

            modifiersById.Remove(modifierId);

            bool removed =
                modifiersByStat[modifier.Stat].Remove(modifier);

            if (removed)
            {
                Recalculate(modifier.Stat);
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
            HashSet<CharacterStat> changedStats = new();

            foreach (
                KeyValuePair<CharacterStat, List<StatModifier>> pair
                in modifiersByStat)
            {
                List<StatModifier> statModifiers = pair.Value;

                for (int index = statModifiers.Count - 1;
                     index >= 0;
                     index--)
                {
                    StatModifier modifier = statModifiers[index];

                    if (!Equals(modifier.Source, source))
                    {
                        continue;
                    }

                    statModifiers.RemoveAt(index);
                    modifiersById.Remove(modifier.Id);

                    totalRemoved++;
                    changedStats.Add(pair.Key);
                }
            }

            foreach (CharacterStat stat in changedStats)
            {
                Recalculate(stat);
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
                CharacterStat stat
                in Enum.GetValues(typeof(CharacterStat)))
            {
                modifiersByStat[stat] =
                    new List<StatModifier>();

                cachedFinalValues[stat] =
                    GetBaseValue(stat);
            }

            initialized = true;
        }

        private void Recalculate(CharacterStat stat)
        {
            EnsureInitialized();

            float oldValue = cachedFinalValues[stat];
            float newValue = CalculateFinalValue(stat);

            cachedFinalValues[stat] = newValue;

            if (Mathf.Approximately(oldValue, newValue))
            {
                return;
            }

            StatChanged?.Invoke(
                new StatChangedEventArgs(
                    stat,
                    oldValue,
                    newValue));
        }

        private float CalculateFinalValue(CharacterStat stat)
        {
            float flatTotal = 0f;
            float additivePercentTotal = 0f;
            float multiplicativeFactor = 1f;

            List<StatModifier> statModifiers =
                modifiersByStat[stat];

            foreach (StatModifier modifier in statModifiers)
            {
                flatTotal += modifier.FlatValue;

                additivePercentTotal +=
                    modifier.AdditivePercent;

                multiplicativeFactor *=
                    1f + modifier.MultiplicativePercent;
            }

            float finalValue = GetBaseValue(stat);

            finalValue += flatTotal;

            finalValue *=
                1f + additivePercentTotal;

            finalValue *=
                multiplicativeFactor;

            return finalValue;
        }
    }
}