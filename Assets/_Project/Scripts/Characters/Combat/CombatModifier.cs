using System;

namespace Riftborn.Characters.Combat
{
    public enum CombatModifierType
    {
        BasicAttackDamage = 0,
        AttackSpeed = 1,
        CriticalChance = 2,
        CriticalMultiplier = 3
    }

    public sealed class CombatModifier
    {
        public CombatModifier(
            string id,
            object source,
            CombatModifierType modifierType,
            float flatValue = 0f,
            float additivePercent = 0f,
            float multiplicativePercent = 0f)
        {
            Id = string.IsNullOrWhiteSpace(id)
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

        public CombatModifierType ModifierType { get; }

        public float FlatValue { get; }

        public float AdditivePercent { get; }

        public float MultiplicativePercent { get; }
    }
}