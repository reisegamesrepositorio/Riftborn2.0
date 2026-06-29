using System.Collections.Generic;
using UnityEngine;

namespace Riftborn.Items
{
    public static class ItemGenerator
    {
        public static bool TryGenerate(
            ItemGenerationProfile profile,
            out ItemInstance itemInstance)
        {
            itemInstance = null;

            if (profile == null ||
                profile.Item == null)
            {
                return false;
            }

            if (!TryRollRarity(
                    profile.Rarities,
                    out ItemRarityData rarity))
            {
                Debug.LogWarning(
                    $"O perfil '{profile.name}' não possui uma " +
                    "raridade válida para sortear.",
                    profile);

                return false;
            }

            int quantity =
                Random.Range(
                    profile.MinimumQuantity,
                    profile.MaximumQuantity + 1);

            itemInstance =
                new ItemInstance(
                    profile.Item,
                    quantity,
                    rarity);

            GenerateAffixes(
                itemInstance,
                profile);

            return itemInstance.IsValid;
        }

        private static bool TryRollRarity(
            IReadOnlyList<WeightedItemRarity> rarities,
            out ItemRarityData selectedRarity)
        {
            selectedRarity = null;

            if (rarities == null ||
                rarities.Count == 0)
            {
                return false;
            }

            float totalWeight = 0f;

            for (int index = 0;
                 index < rarities.Count;
                 index++)
            {
                WeightedItemRarity entry =
                    rarities[index];

                if (entry == null ||
                    entry.Rarity == null)
                {
                    continue;
                }

                totalWeight +=
                    entry.Weight;
            }

            if (totalWeight <= 0f)
            {
                return false;
            }

            float roll =
                Random.value *
                totalWeight;

            float accumulatedWeight = 0f;

            for (int index = 0;
                 index < rarities.Count;
                 index++)
            {
                WeightedItemRarity entry =
                    rarities[index];

                if (entry == null ||
                    entry.Rarity == null ||
                    entry.Weight <= 0f)
                {
                    continue;
                }

                accumulatedWeight +=
                    entry.Weight;

                if (roll > accumulatedWeight)
                {
                    continue;
                }

                selectedRarity =
                    entry.Rarity;

                return true;
            }

            return false;
        }

        private static void GenerateAffixes(
            ItemInstance itemInstance,
            ItemGenerationProfile profile)
        {
            if (itemInstance == null ||
                itemInstance.Rarity == null)
            {
                return;
            }

            int prefixCount =
                DetermineAffixCount(
                    itemInstance.MaximumPrefixCount,
                    profile);

            int suffixCount =
                DetermineAffixCount(
                    itemInstance.MaximumSuffixCount,
                    profile);

            FillAffixType(
                itemInstance,
                profile.PrefixPool,
                ItemAffixType.Prefix,
                prefixCount);

            FillAffixType(
                itemInstance,
                profile.SuffixPool,
                ItemAffixType.Suffix,
                suffixCount);
        }

        private static int DetermineAffixCount(
            int maximumSlots,
            ItemGenerationProfile profile)
        {
            if (maximumSlots <= 0)
            {
                return 0;
            }

            if (profile.FillAllAffixSlots)
            {
                return maximumSlots;
            }

            int count = 0;

            for (int slotIndex = 0;
                 slotIndex < maximumSlots;
                 slotIndex++)
            {
                if (Random.value <=
                    profile.AffixSlotFillChance)
                {
                    count++;
                }
            }

            return count;
        }

        private static void FillAffixType(
            ItemInstance itemInstance,
            IReadOnlyList<ItemAffixData> sourcePool,
            ItemAffixType requiredType,
            int requestedCount)
        {
            if (itemInstance == null ||
                sourcePool == null ||
                requestedCount <= 0)
            {
                return;
            }

            List<ItemAffixData> availableAffixes =
                new();

            for (int index = 0;
                 index < sourcePool.Count;
                 index++)
            {
                ItemAffixData affix =
                    sourcePool[index];

                if (affix == null ||
                    affix.AffixType != requiredType ||
                    itemInstance.ContainsAffix(affix))
                {
                    continue;
                }

                availableAffixes.Add(affix);
            }

            int amountToGenerate =
                Mathf.Min(
                    requestedCount,
                    availableAffixes.Count);

            for (int index = 0;
                 index < amountToGenerate;
                 index++)
            {
                int selectedIndex =
                    Random.Range(
                        0,
                        availableAffixes.Count);

                ItemAffixData selectedAffix =
                    availableAffixes[selectedIndex];

                availableAffixes.RemoveAt(
                    selectedIndex);

                if (!selectedAffix.TryCreateRandomRoll(
                        out ItemAffixRoll roll))
                {
                    continue;
                }

                itemInstance.TryAddAffix(roll);
            }
        }
    }
}