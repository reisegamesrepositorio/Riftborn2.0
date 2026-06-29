using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using Riftborn.Characters.Equipment;
using Riftborn.Items;
using UnityEditor;
using UnityEngine;

namespace Riftborn.Tests
{
    public sealed class ItemGenerationDropTableTests
    {
        private const string DropTablePath =
            "Assets/_Project/Data/Items/LootTables/EquipmentDropTable_AllTest.asset";

        private static readonly string[] ProfilePaths =
        {
            "Assets/_Project/Data/Items/GenerationProfiles/GenerationProfile_Sword.asset",
            "Assets/_Project/Data/Items/GenerationProfiles/GenerationProfile_TwoHandSword.asset",
            "Assets/_Project/Data/Items/GenerationProfiles/GenerationProfile_Dagger.asset",
            "Assets/_Project/Data/Items/GenerationProfiles/GenerationProfile_Staff.asset",
            "Assets/_Project/Data/Items/GenerationProfiles/GenerationProfile_Wand.asset",
            "Assets/_Project/Data/Items/GenerationProfiles/GenerationProfile_MagicBook.asset",
            "Assets/_Project/Data/Items/GenerationProfiles/GenerationProfile_Helmet.asset",
            "Assets/_Project/Data/Items/GenerationProfiles/GenerationProfile_ChestArmor.asset",
            "Assets/_Project/Data/Items/GenerationProfiles/GenerationProfile_Legs.asset",
            "Assets/_Project/Data/Items/GenerationProfiles/GenerationProfile_Shield.asset",
            "Assets/_Project/Data/Items/GenerationProfiles/GenerationProfile_Accessory.asset"
        };

        [Test]
        public void ValidDropTableSelectsProfile()
        {
            ItemGenerationDropTable table =
                LoadDropTable();

            Assert.IsTrue(
                table.TrySelectProfile(
                    out ItemGenerationProfile selectedProfile));

            Assert.NotNull(selectedProfile);
        }

        [Test]
        public void InvalidEntriesAreIgnored()
        {
            ItemGenerationProfile validProfile =
                LoadProfile(ProfilePaths[0]);

            ItemGenerationDropTable table =
                CreateTable(
                    CreateEntry(null, 1f, true),
                    CreateEntry(validProfile, 0f, true),
                    CreateEntry(validProfile, -1f, true),
                    CreateEntry(validProfile, 1f, false),
                    CreateEntry(validProfile, 1f, true));

            ItemGenerationDropTableValidationResult validation =
                table.ValidateTable();

            Assert.AreEqual(5, validation.TotalEntries);
            Assert.AreEqual(1, validation.ValidProfiles.Count);
            Assert.AreEqual(1f, validation.TotalValidWeight, 0.001f);
            Assert.IsTrue(table.TrySelectProfile(out ItemGenerationProfile selectedProfile));
            Assert.AreSame(validProfile, selectedProfile);
        }

        [Test]
        public void DuplicateEntriesWarnAndDoNotIncreaseChance()
        {
            ItemGenerationProfile validProfile =
                LoadProfile(ProfilePaths[0]);

            ItemGenerationDropTable table =
                CreateTable(
                    CreateEntry(validProfile, 1f, true),
                    CreateEntry(validProfile, 1f, true));

            ItemGenerationDropTableValidationResult validation =
                table.ValidateTable();

            Assert.AreEqual(1, validation.ValidProfiles.Count);
            Assert.AreEqual(1f, validation.TotalValidWeight, 0.001f);
            Assert.IsTrue(validation.HasWarnings);
        }

        [Test]
        public void TableWithoutValidEntriesFailsSafely()
        {
            ItemGenerationDropTable table =
                CreateTable(
                    CreateEntry(null, 1f, true),
                    CreateEntry(LoadProfile(ProfilePaths[0]), 0f, true));

            Assert.IsFalse(
                table.TrySelectProfile(
                    out ItemGenerationProfile selectedProfile));

            Assert.IsNull(selectedProfile);
        }

        [Test]
        public void AllGeneralDropTableProfilesGenerateValidItems()
        {
            ItemGenerationDropTable table =
                LoadDropTable();

            ItemGenerationDropTableValidationResult tableValidation =
                table.ValidateTable();

            Assert.AreEqual(ProfilePaths.Length, tableValidation.ValidProfiles.Count);
            Assert.AreEqual(ProfilePaths.Length, tableValidation.TotalValidWeight, 0.001f);

            for (int index = 0;
                 index < ProfilePaths.Length;
                 index++)
            {
                ItemGenerationProfile profile =
                    LoadProfile(ProfilePaths[index]);

                AssertGeneratedItemFromProfile(profile);
            }
        }

        [Test]
        public void SelectedProfileGeneratesMatchingItem()
        {
            ItemGenerationProfile profile =
                LoadProfile(ProfilePaths[3]);

            ItemGenerationDropTable table =
                CreateTable(
                    CreateEntry(profile, 1f, true));

            Assert.IsTrue(table.TrySelectProfile(out ItemGenerationProfile selectedProfile));
            Assert.AreSame(profile, selectedProfile);
            Assert.IsTrue(ItemGenerator.TryGenerate(selectedProfile, out ItemInstance itemInstance));
            Assert.AreSame(profile.Item, itemInstance.Item);
        }

        [Test]
        public void DirectGenerationProfileFlowStillWorks()
        {
            ItemGenerationProfile profile =
                LoadProfile(ProfilePaths[0]);

            AssertGeneratedItemFromProfile(profile);
        }

        [Test]
        public void DiagnosticSelectsGeneralTableAtMostTenTimes()
        {
            ItemGenerationDropTable table =
                LoadDropTable();

            for (int index = 0;
                 index < 10;
                 index++)
            {
                Assert.IsTrue(table.TrySelectProfile(out ItemGenerationProfile selectedProfile));
                Assert.IsTrue(ItemGenerator.TryGenerate(selectedProfile, out ItemInstance itemInstance));

                Debug.Log(
                    $"[LOOT TABLE DIAGNOSTIC] #{index + 1}: " +
                    $"Profile={selectedProfile.name} | " +
                    $"Item={itemInstance.DisplayName} | " +
                    $"Rarity={itemInstance.Rarity?.DisplayName ?? "NULL"}");
            }
        }

        private static void AssertGeneratedItemFromProfile(
            ItemGenerationProfile profile)
        {
            Assert.NotNull(profile);
            Assert.NotNull(profile.Item);
            Assert.IsTrue(ItemGenerator.TryGenerate(profile, out ItemInstance itemInstance), profile.name);
            Assert.NotNull(itemInstance, profile.name);
            Assert.IsTrue(itemInstance.IsValid, profile.name);
            Assert.AreSame(profile.Item, itemInstance.Item, profile.name);

            EquipmentItemData profileEquipmentItem =
                profile.Item as EquipmentItemData;

            EquipmentItemData equipmentItem =
                itemInstance.Item as EquipmentItemData;

            Assert.NotNull(profileEquipmentItem, profile.name);
            Assert.NotNull(equipmentItem, profile.name);
            Assert.AreEqual(profileEquipmentItem.Slot, equipmentItem.Slot, profile.name);
            AssertHasNoDuplicateAffixes(itemInstance.Prefixes);
            AssertHasNoDuplicateAffixes(itemInstance.Suffixes);
        }

        private static ItemGenerationDropTable LoadDropTable()
        {
            ItemGenerationDropTable table =
                AssetDatabase.LoadAssetAtPath<ItemGenerationDropTable>(DropTablePath);

            Assert.NotNull(table, DropTablePath);
            return table;
        }

        private static ItemGenerationProfile LoadProfile(
            string path)
        {
            ItemGenerationProfile profile =
                AssetDatabase.LoadAssetAtPath<ItemGenerationProfile>(path);

            Assert.NotNull(profile, path);
            return profile;
        }

        private static ItemGenerationDropTable CreateTable(
            params WeightedItemGenerationProfileEntry[] entries)
        {
            ItemGenerationDropTable table =
                ScriptableObject.CreateInstance<ItemGenerationDropTable>();

            SetPrivate(
                table,
                "entries",
                new List<WeightedItemGenerationProfileEntry>(entries));

            return table;
        }

        private static WeightedItemGenerationProfileEntry CreateEntry(
            ItemGenerationProfile profile,
            float weight,
            bool enabled)
        {
            WeightedItemGenerationProfileEntry entry =
                new WeightedItemGenerationProfileEntry();

            SetPrivate(entry, "profile", profile);
            SetPrivate(entry, "weight", weight);
            SetPrivate(entry, "enabled", enabled);

            return entry;
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
            object value)
        {
            FieldInfo field =
                target.GetType().GetField(
                    fieldName,
                    BindingFlags.Instance |
                    BindingFlags.NonPublic);

            Assert.NotNull(
                field,
                $"Field '{fieldName}' was not found in {target.GetType().Name}.");

            field.SetValue(target, value);
        }
    }
}
