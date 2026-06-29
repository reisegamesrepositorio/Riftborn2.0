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
            return TryGenerate(
                profile,
                out itemInstance,
                out _,
                false);
        }

        public static bool TryGenerate(
            ItemGenerationProfile profile,
            out ItemInstance itemInstance,
            out ItemGenerationValidationResult validationResult,
            bool logValidationWarnings = false)
        {
            itemInstance = null;
            validationResult =
                ItemGenerationProfileValidator.Validate(profile);

            if (profile == null ||
                profile.Item == null)
            {
                LogValidationIfNeeded(
                    profile,
                    validationResult,
                    logValidationWarnings);

                return false;
            }

            LogValidationIfNeeded(
                profile,
                validationResult,
                logValidationWarnings);

            if (!TryRollRarity(
                    profile.Rarities,
                    out ItemRarityData rarity))
            {
                Debug.LogWarning(
                    $"O perfil '{profile.name}' nao possui uma " +
                    "raridade valida para sortear.",
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
                profile,
                validationResult);

            return itemInstance.IsValid;
        }

        private static void LogValidationIfNeeded(
            ItemGenerationProfile profile,
            ItemGenerationValidationResult validationResult,
            bool logValidationWarnings)
        {
            if (!logValidationWarnings ||
                validationResult == null ||
                !validationResult.HasWarnings)
            {
                return;
            }

            Debug.LogWarning(
                validationResult.BuildDiagnosticText(profile),
                profile);
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
            ItemGenerationProfile profile,
            ItemGenerationValidationResult validationResult)
        {
            if (itemInstance == null ||
                itemInstance.Rarity == null ||
                validationResult == null)
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
                validationResult.ValidPrefixes,
                prefixCount);

            FillAffixType(
                itemInstance,
                validationResult.ValidSuffixes,
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
            IReadOnlyList<ItemAffixData> validatedPool,
            int requestedCount)
        {
            if (itemInstance == null ||
                validatedPool == null ||
                requestedCount <= 0)
            {
                return;
            }

            List<ItemAffixData> availableAffixes =
                new(validatedPool.Count);

            for (int index = 0;
                 index < validatedPool.Count;
                 index++)
            {
                ItemAffixData affix =
                    validatedPool[index];

                if (affix == null ||
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
