using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
namespace Riftborn.Characters.Stats
{
    public sealed class StatChangedEventArgs : EventArgs
    {
        public StatChangedEventArgs(CharacterStat stat, float oldValue, float newValue) { Stat = stat; OldValue = oldValue; NewValue = newValue; }
        public CharacterStat Stat { get; }
        public float OldValue { get; }
        public float NewValue { get; }
    }
    public sealed class CharacterStatsController : MonoBehaviour
    {
        [SerializeField] private float baseSTR = 10f, baseDEX = 10f, baseWIS = 10f, baseISP = 10f, baseFORT = 10f;
        private readonly Dictionary<CharacterStat, List<StatModifier>> modifiers = new();
        private readonly Dictionary<CharacterStat, float> cachedFinalValues = new();
        private readonly HashSet<CharacterStat> dirtyStats = new();
        public event Action<StatChangedEventArgs> StatChanged;
        private void Awake() { InitializeAll(); }
        public float GetBaseValue(CharacterStat stat) => stat switch { CharacterStat.STR => baseSTR, CharacterStat.DEX => baseDEX, CharacterStat.WIS => baseWIS, CharacterStat.ISP => baseISP, CharacterStat.FORT => baseFORT, _ => 0f };
        public void SetBaseValue(CharacterStat stat, float value) { Initialize(stat); switch (stat) { case CharacterStat.STR: baseSTR = value; break; case CharacterStat.DEX: baseDEX = value; break; case CharacterStat.WIS: baseWIS = value; break; case CharacterStat.ISP: baseISP = value; break; case CharacterStat.FORT: baseFORT = value; break; } MarkDirty(stat); }
        public float GetFinalValue(CharacterStat stat)
        {
            Initialize(stat);
            if (!dirtyStats.Contains(stat)) return cachedFinalValues[stat];
            float old = cachedFinalValues[stat];
            float value = GetBaseValue(stat);
            var ordered = modifiers[stat].OrderBy(m => m.Priority).ToList();
            value += ordered.Sum(m => m.FlatValue);
            value *= 1f + ordered.Sum(m => m.AdditivePercent);
            foreach (var modifier in ordered) if (!Mathf.Approximately(modifier.MultiplicativePercent, 0f)) value *= 1f + modifier.MultiplicativePercent;
            cachedFinalValues[stat] = value; dirtyStats.Remove(stat);
            if (!Mathf.Approximately(old, value)) StatChanged?.Invoke(new StatChangedEventArgs(stat, old, value));
            return value;
        }
        public bool AddModifier(StatModifier modifier)
        {
            if (modifier == null) return false;
            Initialize(modifier.Stat);
            if (modifiers[modifier.Stat].Any(existing => existing.Id == modifier.Id)) return false;
            modifiers[modifier.Stat].Add(modifier); MarkDirty(modifier.Stat); return true;
        }
        public bool RemoveModifier(string modifierId)
        {
            foreach (var pair in modifiers) if (pair.Value.RemoveAll(m => m.Id == modifierId) > 0) { MarkDirty(pair.Key); return true; }
            return false;
        }
        public int RemoveModifiersFromSource(object source)
        {
            int total = 0;
            foreach (var pair in modifiers) { int removed = pair.Value.RemoveAll(m => Equals(m.Source, source)); if (removed > 0) { total += removed; MarkDirty(pair.Key); } }
            return total;
        }
        private void InitializeAll() { foreach (CharacterStat stat in Enum.GetValues(typeof(CharacterStat))) Initialize(stat); }
        private void Initialize(CharacterStat stat) { if (!modifiers.ContainsKey(stat)) modifiers[stat] = new List<StatModifier>(); if (!cachedFinalValues.ContainsKey(stat)) cachedFinalValues[stat] = GetBaseValue(stat); }
        private void MarkDirty(CharacterStat stat) { Initialize(stat); dirtyStats.Add(stat); }
    }
}
