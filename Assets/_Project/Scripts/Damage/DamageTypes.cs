using System;
namespace Riftborn.Damage
{
    public enum DamageType { Physical, Magical, True }
    public enum DamageOrigin { Unknown, BasicAttack, Ability, Item, Rune, StatusEffect }
    [Flags]
    public enum DamageTag { None = 0, BasicAttack = 1 << 0, PhysicalAbility = 1 << 1, MagicalAbility = 1 << 2, DamageOverTime = 1 << 3, Area = 1 << 4, SingleTarget = 1 << 5, Critical = 1 << 6, StatusEffect = 1 << 7 }
}
