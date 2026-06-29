using System;

namespace Riftborn.Characters.Defense
{
    public sealed class DefenseModifier
    {
        public DefenseModifier(
            string id,
            object source,
            DefenseType defenseType,
            float flatValue = 0f,
            float additivePercent = 0f,
            float multiplicativePercent = 0f)
        {
            Id = string.IsNullOrWhiteSpace(id)
                ? Guid.NewGuid().ToString("N")
                : id;

            Source = source;
            DefenseType = defenseType;
            FlatValue = flatValue;
            AdditivePercent = additivePercent;
            MultiplicativePercent = multiplicativePercent;
        }

        public string Id { get; }

        public object Source { get; }

        public DefenseType DefenseType { get; }

        public float FlatValue { get; }

        public float AdditivePercent { get; }

        public float MultiplicativePercent { get; }
    }
}