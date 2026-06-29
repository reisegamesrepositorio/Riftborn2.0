using System;
using System.Collections.Generic;
using UnityEngine;

namespace Riftborn.Items
{
    [Serializable]
    public sealed class WeightedItemRarity
    {
        [SerializeField]
        private ItemRarityData rarity;

        [SerializeField, Min(0f)]
        private float weight = 1f;

        public ItemRarityData Rarity =>
            rarity;

        public float Weight =>
            Mathf.Max(
                0f,
                weight);
    }

    [CreateAssetMenu(
        fileName = "NewItemGenerationProfile",
        menuName = "Riftborn/Items/Item Generation Profile")]
    public sealed class ItemGenerationProfile :
        ScriptableObject
    {
        [Header("Item")]
        [SerializeField]
        private ItemData item;

        [Header("Quantity")]
        [SerializeField, Min(1)]
        private int minimumQuantity = 1;

        [SerializeField, Min(1)]
        private int maximumQuantity = 1;

        [Header("Rarity Pool")]
        [SerializeField]
        private List<WeightedItemRarity> rarities =
            new();

        [Header("Affix Pools")]
        [SerializeField]
        private List<ItemAffixData> prefixPool =
            new();

        [SerializeField]
        private List<ItemAffixData> suffixPool =
            new();

        [Header("Affix Generation")]
        [Tooltip(
            "Quando marcado, todos os espaços permitidos " +
            "pela raridade serão preenchidos.")]
        [SerializeField]
        private bool fillAllAffixSlots = true;

        [Tooltip(
            "Chance individual de preencher cada espaço. " +
            "Usado somente quando Fill All Affix Slots " +
            "estiver desmarcado.")]
        [SerializeField, Range(0f, 1f)]
        private float affixSlotFillChance = 0.75f;

        public ItemData Item =>
            item;

        public int MinimumQuantity =>
            Mathf.Max(
                1,
                minimumQuantity);

        public int MaximumQuantity =>
            Mathf.Max(
                MinimumQuantity,
                maximumQuantity);

        public IReadOnlyList<WeightedItemRarity> Rarities =>
            rarities;

        public IReadOnlyList<ItemAffixData> PrefixPool =>
            prefixPool;

        public IReadOnlyList<ItemAffixData> SuffixPool =>
            suffixPool;

        public bool FillAllAffixSlots =>
            fillAllAffixSlots;

        public float AffixSlotFillChance =>
            Mathf.Clamp01(
                affixSlotFillChance);

        private void OnValidate()
        {
            minimumQuantity =
                Mathf.Max(
                    1,
                    minimumQuantity);

            maximumQuantity =
                Mathf.Max(
                    minimumQuantity,
                    maximumQuantity);

            rarities ??=
                new List<WeightedItemRarity>();

            prefixPool ??=
                new List<ItemAffixData>();

            suffixPool ??=
                new List<ItemAffixData>();

            affixSlotFillChance =
                Mathf.Clamp01(
                    affixSlotFillChance);
        }
    }
}