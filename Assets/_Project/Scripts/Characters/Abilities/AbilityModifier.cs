using System;

namespace Riftborn.Characters.Abilities
{
    public enum AbilityModifierType
    {
        AbilityDamage = 0,
        CooldownReduction = 1,
        ResourceCostReduction = 2
    }

    public sealed class AbilityModifier
    {
        public AbilityModifier(
            string id,
            object source,
            AbilityModifierType modifierType,
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

        public AbilityModifierType ModifierType { get; }

        public float FlatValue { get; }

        public float AdditivePercent { get; }

        public float MultiplicativePercent { get; }
    }
}