using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using Riftborn.Characters.Equipment;
using Riftborn.Items;
using UnityEngine;

namespace Riftborn.Tests
{
    public sealed class ItemGenerationProfileValidationTests
    {
        [Test]
        public void ValidatorFiltersInvalidAffixPoolEntries()
        {
            ItemGenerationProfile profile =
                CreateProfile(
                    maxPrefixes: 1,
                    maxSuffixes: 1,
                    prefixPool: new List<ItemAffixData>(),
                    suffixPool: new List<ItemAffixData>(),
                    allowedPrefixEffects: new List<ItemAffixEffectType>
                    {
                        ItemAffixEffectType.CharacterStat
                    },
                    allowedSuffixEffects: new List<ItemAffixEffectType>
                    {
                        ItemAffixEffectType.BasicAttackDamage
                    });

            ItemAffixData validPrefix =
                CreateAffix(
                    "Prefix_STR",
                    ItemAffixType.Prefix,
                    ItemAffixEffectType.CharacterStat);

            ItemAffixData suffixInPrefixPool =
                CreateAffix(
                    "Suffix_BasicAttackDamage",
                    ItemAffixType.Suffix,
                    ItemAffixEffectType.BasicAttackDamage);

            ItemAffixData incompatiblePrefix =
                CreateAffix(
                    "Prefix_MovementSpeed",
                    ItemAffixType.Prefix,
                    ItemAffixEffectType.MovementSpeed);

            ItemAffixData validSuffix =
                CreateAffix(
                    "Suffix_BasicAttackDamage_Valid",
                    ItemAffixType.Suffix,
                    ItemAffixEffectType.BasicAttackDamage);

            ItemAffixData prefixInSuffixPool =
                CreateAffix(
                    "Prefix_DEX",
                    ItemAffixType.Prefix,
                    ItemAffixEffectType.CharacterStat);

            ItemAffixData incompatibleSuffix =
                CreateAffix(
                    "Suffix_MovementSpeed",
                    ItemAffixType.Suffix,
                    ItemAffixEffectType.MovementSpeed);

            SetPrivate(
                profile,
                "prefixPool",
                new List<ItemAffixData>
                {
                    validPrefix,
                    null,
                    validPrefix,
                    suffixInPrefixPool,
                    incompatiblePrefix
                });

            SetPrivate(
                profile,
                "suffixPool",
                new List<ItemAffixData>
                {
                    validSuffix,
                    null,
                    validSuffix,
                    prefixInSuffixPool,
                    incompatibleSuffix
                });

            ItemGenerationValidationResult result =
                ItemGenerationProfileValidator.Validate(profile);

            Assert.AreEqual(
                1,
                result.ValidPrefixes.Count);

            Assert.AreEqual(
                1,
                result.ValidSuffixes.Count);

            Assert.AreEqual(
                8,
                result.DiscardedEntries.Count);
        }

        [Test]
        public void GeneratorCreatesOneHundredItemsWithoutDuplicateAffixes()
        {
            List<ItemAffixData> prefixes =
                new List<ItemAffixData>
                {
                    CreateAffix("Prefix_STR", ItemAffixType.Prefix, ItemAffixEffectType.CharacterStat),
                    CreateAffix("Prefix_DEX", ItemAffixType.Prefix, ItemAffixEffectType.CharacterStat),
                    CreateAffix("Prefix_WIS", ItemAffixType.Prefix, ItemAffixEffectType.CharacterStat),
                    CreateAffix("Prefix_ISP", ItemAffixType.Prefix, ItemAffixEffectType.CharacterStat),
                    CreateAffix("Prefix_FORT", ItemAffixType.Prefix, ItemAffixEffectType.CharacterStat)
                };

            List<ItemAffixData> suffixes =
                new List<ItemAffixData>
                {
                    CreateAffix("Suffix_BasicAttackDamage", ItemAffixType.Suffix, ItemAffixEffectType.BasicAttackDamage),
                    CreateAffix("Suffix_AbilityDamage", ItemAffixType.Suffix, ItemAffixEffectType.AbilityDamage),
                    CreateAffix("Suffix_AttackSpeed", ItemAffixType.Suffix, ItemAffixEffectType.AttackSpeed),
                    CreateAffix("Suffix_CriticalChance", ItemAffixType.Suffix, ItemAffixEffectType.CriticalChance),
                    CreateAffix("Suffix_CooldownReduction", ItemAffixType.Suffix, ItemAffixEffectType.CooldownReduction),
                    CreateAffix("Suffix_ResourceCostReduction", ItemAffixType.Suffix, ItemAffixEffectType.ResourceCostReduction)
                };

            ItemGenerationProfile profile =
                CreateProfile(
                    maxPrefixes: 3,
                    maxSuffixes: 3,
                    prefixPool: prefixes,
                    suffixPool: suffixes,
                    allowedPrefixEffects: new List<ItemAffixEffectType>
                    {
                        ItemAffixEffectType.CharacterStat
                    },
                    allowedSuffixEffects: new List<ItemAffixEffectType>
                    {
                        ItemAffixEffectType.BasicAttackDamage,
                        ItemAffixEffectType.AbilityDamage,
                        ItemAffixEffectType.AttackSpeed,
                        ItemAffixEffectType.CriticalChance,
                        ItemAffixEffectType.CooldownReduction,
                        ItemAffixEffectType.ResourceCostReduction
                    });

            ItemGenerationValidationResult validation =
                ItemGenerationProfileValidator.Validate(profile);

            Assert.AreEqual(
                5,
                validation.ValidPrefixes.Count);

            Assert.AreEqual(
                6,
                validation.ValidSuffixes.Count);

            Assert.AreEqual(
                0,
                validation.DiscardedEntries.Count);

            for (int index = 0;
                 index < 100;
                 index++)
            {
                bool generated =
                    ItemGenerator.TryGenerate(
                        profile,
                        out ItemInstance itemInstance,
                        out ItemGenerationValidationResult generationValidation);

                Assert.IsTrue(generated);
                Assert.NotNull(itemInstance);
                Assert.AreEqual(0, generationValidation.DiscardedEntries.Count);
                Assert.AreEqual(3, itemInstance.PrefixCount);
                Assert.AreEqual(3, itemInstance.SuffixCount);
                AssertHasNoDuplicateAffixes(itemInstance.Prefixes);
                AssertHasNoDuplicateAffixes(itemInstance.Suffixes);
            }
        }

        private static ItemGenerationProfile CreateProfile(
            int maxPrefixes,
            int maxSuffixes,
            List<ItemAffixData> prefixPool,
            List<ItemAffixData> suffixPool,
            List<ItemAffixEffectType> allowedPrefixEffects,
            List<ItemAffixEffectType> allowedSuffixEffects)
        {
            WeaponItemData weapon =
                ScriptableObject.CreateInstance<WeaponItemData>();

            weapon.name =
                "Generated Test Sword";

            SetPrivate(
                weapon,
                "displayName",
                "Generated Test Sword",
                typeof(ItemData));

            SetPrivate(
                weapon,
                "slot",
                EquipmentSlot.Weapon,
                typeof(EquipmentItemData));

            ItemRarityData rarity =
                ScriptableObject.CreateInstance<ItemRarityData>();

            rarity.name =
                "Generated Test Rarity";

            SetPrivate(
                rarity,
                "displayName",
                "Generated Test Rarity");

            SetPrivate(
                rarity,
                "maxPrefixes",
                maxPrefixes);

            SetPrivate(
                rarity,
                "maxSuffixes",
                maxSuffixes);

            WeightedItemRarity weightedRarity =
                new WeightedItemRarity();

            SetPrivate(
                weightedRarity,
                "rarity",
                rarity);

            SetPrivate(
                weightedRarity,
                "weight",
                1f);

            ItemGenerationProfile profile =
                ScriptableObject.CreateInstance<ItemGenerationProfile>();

            profile.name =
                "Generated Test Profile";

            SetPrivate(
                profile,
                "item",
                weapon);

            SetPrivate(
                profile,
                "rarities",
                new List<WeightedItemRarity>
                {
                    weightedRarity
                });

            SetPrivate(
                profile,
                "prefixPool",
                prefixPool);

            SetPrivate(
                profile,
                "suffixPool",
                suffixPool);

            SetPrivate(
                profile,
                "allowedPrefixEffects",
                allowedPrefixEffects);

            SetPrivate(
                profile,
                "allowedSuffixEffects",
                allowedSuffixEffects);

            SetPrivate(
                profile,
                "fillAllAffixSlots",
                true);

            return profile;
        }

        private static ItemAffixData CreateAffix(
            string name,
            ItemAffixType affixType,
            ItemAffixEffectType effectType)
        {
            ItemAffixData affix =
                ScriptableObject.CreateInstance<ItemAffixData>();

            affix.name =
                name;

            SetPrivate(
                affix,
                "displayName",
                name);

            SetPrivate(
                affix,
                "affixType",
                affixType);

            SetPrivate(
                affix,
                "effectType",
                effectType);

            SetPrivate(
                affix,
                "tiers",
                new List<ItemAffixTierDefinition>
                {
                    new ItemAffixTierDefinition(1, 10f, 10f, 1f),
                    new ItemAffixTierDefinition(2, 5f, 5f, 1f)
                });

            return affix;
        }

        private static void AssertHasNoDuplicateAffixes(
            IReadOnlyList<ItemAffixRoll> rolls)
        {
            HashSet<ItemAffixData> usedAffixes =
                new HashSet<ItemAffixData>();

            for (int index = 0;
                 index < rolls.Count;
                 index++)
            {
                Assert.IsTrue(
                    usedAffixes.Add(rolls[index].Affix),
                    $"Duplicate affix '{rolls[index].Affix.name}' found.");
            }
        }

        private static void SetPrivate(
            object target,
            string fieldName,
            object value,
            System.Type declaringType = null)
        {
            System.Type type =
                declaringType ?? target.GetType();

            FieldInfo field =
                type.GetField(
                    fieldName,
                    BindingFlags.Instance |
                    BindingFlags.NonPublic);

            Assert.NotNull(
                field,
                $"Field '{fieldName}' was not found in {type.Name}.");

            field.SetValue(
                target,
                value);
        }
    }
}
