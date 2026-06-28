using System;
namespace Riftborn.Characters.StatusEffects
{
    [Flags]
    public enum StatusEffectTag { None = 0, Buff = 1 << 0, Debuff = 1 << 1, CrowdControl = 1 << 2, HardControl = 1 << 3, DamageOverTime = 1 << 4, HealOverTime = 1 << 5, Bleed = 1 << 6, Burn = 1 << 7, Poison = 1 << 8, Stun = 1 << 9, Sleep = 1 << 10, Root = 1 << 11, Silence = 1 << 12, Slow = 1 << 13, Shield = 1 << 14, Physical = 1 << 15, Magical = 1 << 16 }
}
