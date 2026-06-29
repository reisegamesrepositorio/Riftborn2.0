using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using Riftborn.Characters.Equipment;
using Riftborn.Items;
using UnityEditor;
using UnityEngine;

namespace Riftborn.Tests
{
    public sealed class ItemGenerationProfileValidationTests
    {
        private const int GeneratedItemsPerProfile = 3;

        private sealed class ProfileExpectation
        {
            public ProfileExpectation(
                string path,
                EquipmentSlot slot,
                string expectedItemName,
                params ItemAffixEffectType[] suffixEffects)
            {
                Path = path;
                Slot = slot;
                ExpectedItemName = expectedItemName;
                SuffixEffects = new HashSet<ItemAffixEffectType>(suffixEffects);
            }

            public string Path { get; }
            public EquipmentSlot Slot { get; }
            public string ExpectedItemName { get; }
            public HashSet<ItemAffixEffectType> SuffixEffects { get; }
        }

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

            Assert.AreEqual(1, result.ValidPrefixes.Count);
            Assert.AreEqual(1, result.ValidSuffixes.Count);
            Assert.AreEqual(8, result.DiscardedEntries.Count);
        }

        [Test]
        public void InMemoryWeaponProfileGeneratesWithoutDuplicateAffixes()
        {
            ItemGenerationProfile profile =
                CreateProfile(
                    maxPrefixes: 3,
                    maxSuffixes: 3,
                    prefixPool: new List<ItemAffixData>
                    {
                        CreateAffix("Prefix_STR", ItemAffixType.Prefix, ItemAffixEffectType.CharacterStat),
                        CreateAffix("Prefix_DEX", ItemAffixType.Prefix, ItemAffixEffectType.CharacterStat),
                        CreateAffix("Prefix_WIS", ItemAffixType.Prefix, ItemAffixEffectType.CharacterStat),
                        CreateAffix("Prefix_ISP", ItemAffixType.Prefix, ItemAffixEffectType.CharacterStat),
                        CreateAffix("Prefix_FORT", ItemAffixType.Prefix, ItemAffixEffectType.CharacterStat)
                    },
                    suffixPool: new List<ItemAffixData>
                    {
                        CreateAffix("Suffix_BasicAttackDamage", ItemAffixType.Suffix, ItemAffixEffectType.BasicAttackDamage),
                        CreateAffix("Suffix_AbilityDamage", ItemAffixType.Suffix, ItemAffixEffectType.AbilityDamage),
                        CreateAffix("Suffix_AttackSpeed", ItemAffixType.Suffix, ItemAffixEffectType.AttackSpeed),
                        CreateAffix("Suffix_CriticalChance", ItemAffixType.Suffix, ItemAffixEffectType.CriticalChance),
                        CreateAffix("Suffix_CooldownReduction", ItemAffixType.Suffix, ItemAffixEffectType.CooldownReduction),
                        CreateAffix("Suffix_ResourceCostReduction", ItemAffixType.Suffix, ItemAffixEffectType.ResourceCostReduction)
                    },
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

            Assert.AreEqual(5, validation.ValidPrefixes.Count);
            Assert.AreEqual(6, validation.ValidSuffixes.Count);
            Assert.AreEqual(0, validation.DiscardedEntries.Count);

            for (int index = 0;
                 index < GeneratedItemsPerProfile;
                 index++)
            {
                AssertGeneratedItem(
                    profile,
                    EquipmentSlot.Weapon,
                    "Generated Test Sword",
                    validation.ValidPrefixes.Count,
                    validation.ValidSuffixes.Count,
                    new HashSet<ItemAffixEffectType>
                    {
                        ItemAffixEffectType.BasicAttackDamage,
                        ItemAffixEffectType.AbilityDamage,
                        ItemAffixEffectType.AttackSpeed,
                        ItemAffixEffectType.CriticalChance,
                        ItemAffixEffectType.CooldownReduction,
                        ItemAffixEffectType.ResourceCostReduction
                    },
                    index + 1);
            }
        }

        [Test]
        public void ConfiguredEquipmentProfilesGenerateOnlyAllowedAffixes()
        {
            ProfileExpectation[] expectations =
            {
                new ProfileExpectation(
                    "Assets/_Project/Data/Items/GenerationProfiles/GenerationProfile_Sword.asset",
                    EquipmentSlot.Weapon,
                    "Sword",
                    ItemAffixEffectType.BasicAttackDamage,
                    ItemAffixEffectType.AbilityDamage,
                    ItemAffixEffectType.AttackSpeed,
                    ItemAffixEffectType.CriticalChance,
                    ItemAffixEffectType.CooldownReduction,
                    ItemAffixEffectType.ResourceCostReduction),
                new ProfileExpectation(
                    "Assets/_Project/Data/Items/GenerationProfiles/GenerationProfile_TwoHandSword.asset",
                    EquipmentSlot.Weapon,
                    "Two-Handed Sword Test",
                    ItemAffixEffectType.BasicAttackDamage,
                    ItemAffixEffectType.AbilityDamage,
                    ItemAffixEffectType.AttackSpeed,
                    ItemAffixEffectType.CriticalChance,
                    ItemAffixEffectType.CooldownReduction,
                    ItemAffixEffectType.ResourceCostReduction),
                new ProfileExpectation(
                    "Assets/_Project/Data/Items/GenerationProfiles/GenerationProfile_Dagger.asset",
                    EquipmentSlot.Weapon,
                    "Dagger Test",
                    ItemAffixEffectType.BasicAttackDamage,
                    ItemAffixEffectType.AbilityDamage,
                    ItemAffixEffectType.AttackSpeed,
                    ItemAffixEffectType.CriticalChance,
                    ItemAffixEffectType.CooldownReduction,
                    ItemAffixEffectType.ResourceCostReduction),
                new ProfileExpectation(
                    "Assets/_Project/Data/Items/GenerationProfiles/GenerationProfile_Staff.asset",
                    EquipmentSlot.Weapon,
                    "Staff Test",
                    ItemAffixEffectType.BasicAttackDamage,
                    ItemAffixEffectType.AbilityDamage,
                    ItemAffixEffectType.AttackSpeed,
                    ItemAffixEffectType.CriticalChance,
                    ItemAffixEffectType.CooldownReduction,
                    ItemAffixEffectType.ResourceCostReduction),
                new ProfileExpectation(
                    "Assets/_Project/Data/Items/GenerationProfiles/GenerationProfile_Wand.asset",
                    EquipmentSlot.Weapon,
                    "Wand Test",
                    ItemAffixEffectType.BasicAttackDamage,
                    ItemAffixEffectType.AbilityDamage,
                    ItemAffixEffectType.AttackSpeed,
                    ItemAffixEffectType.CriticalChance,
                    ItemAffixEffectType.CooldownReduction,
                    ItemAffixEffectType.ResourceCostReduction),
                new ProfileExpectation(
                    "Assets/_Project/Data/Items/GenerationProfiles/GenerationProfile_MagicBook.asset",
                    EquipmentSlot.Weapon,
                    "Magic Book Test",
                    ItemAffixEffectType.BasicAttackDamage,
                    ItemAffixEffectType.AbilityDamage,
                    ItemAffixEffectType.AttackSpeed,
                    ItemAffixEffectType.CriticalChance,
                    ItemAffixEffectType.CooldownReduction,
                    ItemAffixEffectType.ResourceCostReduction),
                new ProfileExpectation(
                    "Assets/_Project/Data/Items/GenerationProfiles/GenerationProfile_Helmet.asset",
                    EquipmentSlot.Head,
                    "Helmet Test",
                    ItemAffixEffectType.MaximumHealth,
                    ItemAffixEffectType.MaximumResource,
                    ItemAffixEffectType.PhysicalDefense,
                    ItemAffixEffectType.MagicalDefense,
                    ItemAffixEffectType.ResourceRegeneration,
                    ItemAffixEffectType.CooldownReduction),
                new ProfileExpectation(
                    "Assets/_Project/Data/Items/GenerationProfiles/GenerationProfile_ChestArmor.asset",
                    EquipmentSlot.Chest,
                    "Chest Armor Test",
                    ItemAffixEffectType.MaximumHealth,
                    ItemAffixEffectType.MaximumResource,
                    ItemAffixEffectType.PhysicalDefense,
                    ItemAffixEffectType.MagicalDefense,
                    ItemAffixEffectType.ResourceRegeneration),
                new ProfileExpectation(
                    "Assets/_Project/Data/Items/GenerationProfiles/GenerationProfile_Legs.asset",
                    EquipmentSlot.Legs,
                    "Legs Test",
                    ItemAffixEffectType.MaximumHealth,
                    ItemAffixEffectType.MaximumResource,
                    ItemAffixEffectType.PhysicalDefense,
                    ItemAffixEffectType.MagicalDefense,
                    ItemAffixEffectType.ResourceRegeneration),
                new ProfileExpectation(
                    "Assets/_Project/Data/Items/GenerationProfiles/GenerationProfile_Shield.asset",
                    EquipmentSlot.OffHand,
                    "Shield Test",
                    ItemAffixEffectType.MaximumHealth,
                    ItemAffixEffectType.MaximumResource,
                    ItemAffixEffectType.PhysicalDefense,
                    ItemAffixEffectType.MagicalDefense,
                    ItemAffixEffectType.ResourceRegeneration,
                    ItemAffixEffectType.AbilityDamage),
                new ProfileExpectation(
                    "Assets/_Project/Data/Items/GenerationProfiles/GenerationProfile_Accessory.asset",
                    EquipmentSlot.Accessory,
                    "Accessory Test",
                    ItemAffixEffectType.CriticalChance,
                    ItemAffixEffectType.BasicAttackDamage,
                    ItemAffixEffectType.AbilityDamage,
                    ItemAffixEffectType.MaximumHealth,
                    ItemAffixEffectType.MaximumResource,
                    ItemAffixEffectType.ResourceRegeneration,
                    ItemAffixEffectType.CooldownReduction,
                    ItemAffixEffectType.ResourceCostReduction)
            };

            for (int profileIndex = 0;
                 profileIndex < expectations.Length;
                 profileIndex++)
            {
                ProfileExpectation expectation =
                    expectations[profileIndex];

                ItemGenerationProfile profile =
                    AssetDatabase.LoadAssetAtPath<ItemGenerationProfile>(
                        expectation.Path);

                Assert.NotNull(
                    profile,
                    $"Profile not found: {expectation.Path}");

                ItemGenerationValidationResult validation =
                    ItemGenerationProfileValidator.Validate(profile);

                Assert.AreEqual(5, validation.ValidPrefixes.Count, profile.name);
                Assert.AreEqual(expectation.SuffixEffects.Count, validation.ValidSuffixes.Count, profile.name);
                Assert.AreEqual(0, validation.DiscardedEntries.Count, profile.name);

                for (int itemIndex = 0;
                     itemIndex < GeneratedItemsPerProfile;
                     itemIndex++)
                {
                    AssertGeneratedItem(
                        profile,
                        expectation.Slot,
                        expectation.ExpectedItemName,
                        validation.ValidPrefixes.Count,
                        validation.ValidSuffixes.Count,
                        expectation.SuffixEffects,
                        itemIndex + 1);
                }
            }
        }

        private static void AssertGeneratedItem(
            ItemGenerationProfile profile,
            EquipmentSlot expectedSlot,
            string expectedItemName,
            int validPrefixCount,
            int validSuffixCount,
            HashSet<ItemAffixEffectType> allowedSuffixEffects,
            int itemIndex)
        {
            bool generated =
                ItemGenerator.TryGenerate(
                    profile,
                    out ItemInstance itemInstance,
                    out ItemGenerationValidationResult generationValidation);

            Assert.IsTrue(generated, profile.name);
            Assert.NotNull(itemInstance, profile.name);
            Assert.AreEqual(0, generationValidation.DiscardedEntries.Count, profile.name);

            EquipmentItemData equipmentItem =
                itemInstance.Item as EquipmentItemData;

            Assert.NotNull(equipmentItem, profile.name);
            Assert.AreEqual(expectedSlot, equipmentItem.Slot, profile.name);
            Assert.AreEqual(expectedItemName, itemInstance.DisplayName, profile.name);
            Assert.LessOrEqual(itemInstance.PrefixCount, itemInstance.MaximumPrefixCount, profile.name);
            Assert.LessOrEqual(itemInstance.SuffixCount, itemInstance.MaximumSuffixCount, profile.name);
            Assert.LessOrEqual(itemInstance.PrefixCount, validPrefixCount, profile.name);
            Assert.LessOrEqual(itemInstance.SuffixCount, validSuffixCount, profile.name);

            AssertHasNoDuplicateAffixes(itemInstance.Prefixes);
            AssertHasNoDuplicateAffixes(itemInstance.Suffixes);

            for (int index = 0;
                 index < itemInstance.Prefixes.Count;
                 index++)
            {
                Assert.AreEqual(
                    ItemAffixEffectType.CharacterStat,
                    itemInstance.Prefixes[index].Affix.EffectType,
                    profile.name);
            }

            for (int index = 0;
                 index < itemInstance.Suffixes.Count;
                 index++)
            {
                ItemAffixEffectType effectType =
                    itemInstance.Suffixes[index].Affix.EffectType;

                Assert.IsTrue(
                    allowedSuffixEffects.Contains(effectType),
                    $"{profile.name} generated disallowed suffix {effectType}.");
            }

            Debug.Log(
                BuildDiagnosticLine(
                    profile,
                    itemInstance,
                    itemIndex));
        }

        private static string BuildDiagnosticLine(
            ItemGenerationProfile profile,
            ItemInstance itemInstance,
            int itemIndex)
        {
            System.Text.StringBuilder builder =
                new System.Text.StringBuilder();

            builder.AppendLine(
                $"[ITEM PROFILE DIAGNOSTIC] {profile.name} #{itemIndex}");

            builder.AppendLine(
                $"Item: {itemInstance.DisplayName}");

            builder.AppendLine(
                $"Rarity: {itemInstance.Rarity?.DisplayName ?? "NULL"}");

            AppendRolls(builder, "Prefixes", itemInstance.Prefixes);
            AppendRolls(builder, "Suffixes", itemInstance.Suffixes);

            return builder.ToString();
        }

        private static void AppendRolls(
            System.Text.StringBuilder builder,
            string label,
            IReadOnlyList<ItemAffixRoll> rolls)
        {
            builder.AppendLine($"{label}: {rolls.Count}");

            for (int index = 0;
                 index < rolls.Count;
                 index++)
            {
                ItemAffixRoll roll = rolls[index];

                builder.AppendLine(
                    $"- {roll.DisplayName} | {roll.Affix.EffectType} | " +
                    $"Tier {roll.Tier} | Value {roll.RolledValue:0.##}");
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

            weapon.name = "Generated Test Sword";

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

            rarity.name = "Generated Test Rarity";

            SetPrivate(rarity, "displayName", "Generated Test Rarity");
            SetPrivate(rarity, "maxPrefixes", maxPrefixes);
            SetPrivate(rarity, "maxSuffixes", maxSuffixes);

            WeightedItemRarity weightedRarity =
                new WeightedItemRarity();

            SetPrivate(weightedRarity, "rarity", rarity);
            SetPrivate(weightedRarity, "weight", 1f);

            ItemGenerationProfile profile =
                ScriptableObject.CreateInstance<ItemGenerationProfile>();

            profile.name = "Generated Test Profile";

            SetPrivate(profile, "item", weapon);
            SetPrivate(
                profile,
                "rarities",
                new List<WeightedItemRarity> { weightedRarity });
            SetPrivate(profile, "prefixPool", prefixPool);
            SetPrivate(profile, "suffixPool", suffixPool);
            SetPrivate(profile, "allowedPrefixEffects", allowedPrefixEffects);
            SetPrivate(profile, "allowedSuffixEffects", allowedSuffixEffects);
            SetPrivate(profile, "fillAllAffixSlots", true);

            return profile;
        }

        private static ItemAffixData CreateAffix(
            string name,
            ItemAffixType affixType,
            ItemAffixEffectType effectType)
        {
            ItemAffixData affix =
                ScriptableObject.CreateInstance<ItemAffixData>();

            affix.name = name;

            SetPrivate(affix, "displayName", name);
            SetPrivate(affix, "affixType", affixType);
            SetPrivate(affix, "effectType", effectType);
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

            field.SetValue(target, value);
        }
    }
}
