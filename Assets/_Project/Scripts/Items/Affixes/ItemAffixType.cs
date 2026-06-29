namespace Riftborn.Items
{
    public enum ItemAffixType
    {
        Prefix = 0,
        Suffix = 1
    }

    public enum ItemAffixEffectType
    {
        CharacterStat = 0,
        PhysicalDefense = 1,
        MagicalDefense = 2,
        BasicAttackDamage = 3,
        AttackSpeed = 4,
        CriticalChance = 5,
        CriticalMultiplier = 6,
        MaximumHealth = 7,
        MaximumResource = 8,
        ResourceRegeneration = 9,
        AbilityDamage = 10,
        MovementSpeed = 11,
        CooldownReduction = 12,
        ResourceCostReduction = 13,

        Custom = 100
    }

    public enum ItemAffixValueMode
    {
        Flat = 0,
        AdditivePercent = 1,
        MultiplicativePercent = 2
    }
}