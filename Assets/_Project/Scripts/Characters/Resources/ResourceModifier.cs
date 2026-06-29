using System;

namespace Riftborn.Characters.Resources
{
    public enum ResourceModifierType
    {
        MaximumResource = 0,
        Regeneration = 1
    }

    public sealed class ResourceModifier
    {
        public ResourceModifier(
            string id,
            object source,
            ResourceModifierType modifierType,
            float flatValue = 0f,
            float additivePercent = 0f,
            float multiplicativePercent = 0f)
        {
            Id =
                string.IsNullOrWhiteSpace(id)
                    ? Guid.NewGuid().ToString("N")
                    : id;

            Source = source;
            ModifierType = modifierType;
            FlatValue = flatValue;
            AdditivePercent = additivePercent;
            MultiplicativePercent = multiplicativePercent;
        }

        public string Id { get; }

        public object Source { get; }

        public ResourceModifierType ModifierType { get; }

        public float FlatValue { get; }

        public float AdditivePercent { get; }

        public float MultiplicativePercent { get; }
    }
}