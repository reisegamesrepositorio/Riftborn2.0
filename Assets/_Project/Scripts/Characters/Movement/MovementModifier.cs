using System;

namespace Riftborn.Characters.Movement
{
    public sealed class MovementModifier
    {
        public MovementModifier(
            string id,
            object source,
            float flatValue = 0f,
            float additivePercent = 0f,
            float multiplicativePercent = 0f)
        {
            Id =
                string.IsNullOrWhiteSpace(id)
                    ? Guid.NewGuid().ToString("N")
                    : id;

            Source = source;
            FlatValue = flatValue;
            AdditivePercent = additivePercent;
            MultiplicativePercent = multiplicativePercent;
        }

        public string Id { get; }

        public object Source { get; }

        public float FlatValue { get; }

        public float AdditivePercent { get; }

        public float MultiplicativePercent { get; }
    }
}