using System;
namespace Riftborn.Characters.Stats
{
    [Serializable]
    public sealed class StatModifier
    {
        public StatModifier(string id, object source, CharacterStat stat, float flatValue = 0f, float additivePercent = 0f, float multiplicativePercent = 0f, int priority = 0)
        {
            Id = string.IsNullOrWhiteSpace(id) ? Guid.NewGuid().ToString("N") : id;
            Source = source; Stat = stat; FlatValue = flatValue; AdditivePercent = additivePercent; MultiplicativePercent = multiplicativePercent; Priority = priority;
        }
        public string Id { get; }
        public object Source { get; }
        public CharacterStat Stat { get; }
        public float FlatValue { get; }
        public float AdditivePercent { get; }
        public float MultiplicativePercent { get; }
        public int Priority { get; }
    }
}
