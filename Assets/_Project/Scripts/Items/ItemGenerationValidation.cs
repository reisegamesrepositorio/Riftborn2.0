using System.Collections.Generic;
using System.Text;

namespace Riftborn.Items
{
    public sealed class ItemAffixPoolDiscard
    {
        public ItemAffixPoolDiscard(
            ItemAffixType poolType,
            int index,
            ItemAffixData affix,
            string reason)
        {
            PoolType = poolType;
            Index = index;
            Affix = affix;
            Reason = reason;
        }

        public ItemAffixType PoolType { get; }
        public int Index { get; }
        public ItemAffixData Affix { get; }
        public string Reason { get; }

        public string AffixName =>
            Affix != null
                ? Affix.name
                : "NULL";
    }

    public sealed class ItemGenerationValidationResult
    {
        private readonly List<ItemAffixData> validPrefixes =
            new();

        private readonly List<ItemAffixData> validSuffixes =
            new();

        private readonly List<ItemAffixPoolDiscard> discardedEntries =
            new();

        private readonly List<string> warnings =
            new();

        public IReadOnlyList<ItemAffixData> ValidPrefixes =>
            validPrefixes;

        public IReadOnlyList<ItemAffixData> ValidSuffixes =>
            validSuffixes;

        public IReadOnlyList<ItemAffixPoolDiscard> DiscardedEntries =>
            discardedEntries;

        public IReadOnlyList<string> Warnings =>
            warnings;

        public bool HasWarnings =>
            warnings.Count > 0 ||
            discardedEntries.Count > 0;

        internal void AddValidAffix(
            ItemAffixType type,
            ItemAffixData affix)
        {
            if (type == ItemAffixType.Prefix)
            {
                validPrefixes.Add(affix);
                return;
            }

            validSuffixes.Add(affix);
        }

        internal void AddDiscard(
            ItemAffixType poolType,
            int index,
            ItemAffixData affix,
            string reason)
        {
            discardedEntries.Add(
                new ItemAffixPoolDiscard(
                    poolType,
                    index,
                    affix,
                    reason));
        }

        internal void AddWarning(
            string warning)
        {
            if (!string.IsNullOrWhiteSpace(warning))
            {
                warnings.Add(warning);
            }
        }

        public string BuildDiagnosticText(
            ItemGenerationProfile profile)
        {
            StringBuilder builder =
                new StringBuilder();

            builder.AppendLine(
                $"[ITEM PROFILE VALIDATION] {profile?.name ?? "NULL"}");

            builder.AppendLine(
                $"Item: {profile?.Item?.DisplayName ?? "NULL"}");

            builder.AppendLine(
                $"Prefixos validos: {ValidPrefixes.Count}");

            builder.AppendLine(
                $"Sufixos validos: {ValidSuffixes.Count}");

            for (int index = 0;
                 index < warnings.Count;
                 index++)
            {
                builder.AppendLine(
                    $"WARNING: {warnings[index]}");
            }

            for (int index = 0;
                 index < discardedEntries.Count;
                 index++)
            {
                ItemAffixPoolDiscard discard =
                    discardedEntries[index];

                builder.AppendLine(
                    $"DESCARTADO {discard.PoolType}[{discard.Index}] " +
                    $"{discard.AffixName}: {discard.Reason}");
            }

            return builder.ToString();
        }
    }

    public static class ItemGenerationProfileValidator
    {
        public static ItemGenerationValidationResult Validate(
            ItemGenerationProfile profile,
            bool includeRarityCapacityWarnings = true)
        {
            ItemGenerationValidationResult result =
                new ItemGenerationValidationResult();

            if (profile == null)
            {
                result.AddWarning(
                    "Perfil de geracao nulo.");

                return result;
            }

            if (profile.Item == null)
            {
                result.AddWarning(
                    "Perfil sem Item configurado.");
            }

            CollectValidAffixes(
                profile,
                profile.PrefixPool,
                ItemAffixType.Prefix,
                result);

            CollectValidAffixes(
                profile,
                profile.SuffixPool,
                ItemAffixType.Suffix,
                result);

            if (includeRarityCapacityWarnings)
            {
                AddRarityCapacityWarnings(
                    profile,
                    result);
            }

            return result;
        }

        private static void CollectValidAffixes(
            ItemGenerationProfile profile,
            IReadOnlyList<ItemAffixData> pool,
            ItemAffixType poolType,
            ItemGenerationValidationResult result)
        {
            if (pool == null)
            {
                return;
            }

            HashSet<ItemAffixData> usedAffixes =
                new HashSet<ItemAffixData>();

            for (int index = 0;
                 index < pool.Count;
                 index++)
            {
                ItemAffixData affix =
                    pool[index];

                if (affix == null)
                {
                    result.AddDiscard(
                        poolType,
                        index,
                        null,
                        "Referencia nula.");

                    continue;
                }

                if (!usedAffixes.Add(affix))
                {
                    result.AddDiscard(
                        poolType,
                        index,
                        affix,
                        "Afixo duplicado no mesmo pool.");

                    continue;
                }

                if (!profile.IsAffixCompatible(
                        affix,
                        poolType,
                        out string reason))
                {
                    result.AddDiscard(
                        poolType,
                        index,
                        affix,
                        reason);

                    continue;
                }

                result.AddValidAffix(
                    poolType,
                    affix);
            }
        }

        private static void AddRarityCapacityWarnings(
            ItemGenerationProfile profile,
            ItemGenerationValidationResult result)
        {
            IReadOnlyList<WeightedItemRarity> rarities =
                profile.Rarities;

            if (rarities == null)
            {
                return;
            }

            HashSet<ItemRarityData> checkedRarities =
                new HashSet<ItemRarityData>();

            for (int index = 0;
                 index < rarities.Count;
                 index++)
            {
                WeightedItemRarity entry =
                    rarities[index];

                if (entry == null ||
                    entry.Rarity == null ||
                    entry.Weight <= 0f ||
                    !checkedRarities.Add(entry.Rarity))
                {
                    continue;
                }

                ItemRarityData rarity =
                    entry.Rarity;

                if (result.ValidPrefixes.Count <
                    rarity.MaxPrefixes)
                {
                    result.AddWarning(
                        $"Raridade '{rarity.DisplayName}' pode pedir " +
                        $"{rarity.MaxPrefixes} prefixos, mas o perfil " +
                        $"possui apenas {result.ValidPrefixes.Count} prefixos validos.");
                }

                if (result.ValidSuffixes.Count <
                    rarity.MaxSuffixes)
                {
                    result.AddWarning(
                        $"Raridade '{rarity.DisplayName}' pode pedir " +
                        $"{rarity.MaxSuffixes} sufixos, mas o perfil " +
                        $"possui apenas {result.ValidSuffixes.Count} sufixos validos.");
                }
            }
        }
    }
}