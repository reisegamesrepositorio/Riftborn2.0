using System;

namespace Riftborn.Damage
{
    public enum DamageType
    {
        Physical = 0,
        Magical = 1,
        True = 2
    }

    public enum DamageOrigin
    {
        Unknown = 0,
        BasicAttack = 1,
        Ability = 2,
        Item = 3,
        Rune = 4,
        StatusEffect = 5
    }

    [Flags]
    public enum DamageTag
    {
        None = 0,
        BasicAttack = 1 << 0,
        PhysicalAbility = 1 << 1,
        MagicalAbility = 1 << 2,
        DamageOverTime = 1 << 3,
        Area = 1 << 4,
        SingleTarget = 1 << 5,
        Critical = 1 << 6,
        StatusEffect = 1 << 7
    }
}