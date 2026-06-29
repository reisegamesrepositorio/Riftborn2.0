using System;
using System.Collections.Generic;
using Riftborn.Characters.Equipment;
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

        [Header("Affix Restrictions")]
        [Tooltip(
            "Efeitos de prefixo permitidos para o item deste perfil. " +
            "Lista vazia mantem compatibilidade e permite qualquer prefixo valido.")]
        [SerializeField]
        private List<ItemAffixEffectType> allowedPrefixEffects =
            new();

        [Tooltip(
            "Efeitos de sufixo permitidos para o item deste perfil. " +
            "Lista vazia mantem compatibilidade e permite qualquer sufixo valido.")]
        [SerializeField]
        private List<ItemAffixEffectType> allowedSuffixEffects =
            new();

        [Header("Affix Generation")]
        [Tooltip(
            "Quando marcado, todos os espacos permitidos " +
            "pela raridade serao preenchidos.")]
        [SerializeField]
        private bool fillAllAffixSlots = true;

        [Tooltip(
            "Chance individual de preencher cada espaco. " +
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

        public IReadOnlyList<ItemAffixEffectType> AllowedPrefixEffects =>
            allowedPrefixEffects;

        public IReadOnlyList<ItemAffixEffectType> AllowedSuffixEffects =>
            allowedSuffixEffects;

        public bool HasPrefixRestrictions =>
            allowedPrefixEffects != null &&
            allowedPrefixEffects.Count > 0;

        public bool HasSuffixRestrictions =>
            allowedSuffixEffects != null &&
            allowedSuffixEffects.Count > 0;

        public bool FillAllAffixSlots =>
            fillAllAffixSlots;

        public float AffixSlotFillChance =>
            Mathf.Clamp01(
                affixSlotFillChance);

        public bool IsAffixCompatible(
            ItemAffixData affix,
            ItemAffixType expectedPoolType,
            out string reason)
        {
            reason = string.Empty;

            if (affix == null)
            {
                reason = "Referencia nula.";
                return false;
            }

            if (affix.AffixType != expectedPoolType)
            {
                reason =
                    $"Afixo e {affix.AffixType}, mas esta no pool de {expectedPoolType}.";

                return false;
            }

            IReadOnlyList<ItemAffixEffectType> allowedEffects =
                expectedPoolType == ItemAffixType.Prefix
                    ? allowedPrefixEffects
                    : allowedSuffixEffects;

            if (allowedEffects != null &&
                allowedEffects.Count > 0 &&
                !ContainsEffect(allowedEffects, affix.EffectType))
            {
                reason =
                    $"Efeito {affix.EffectType} nao e permitido para {GetItemCompatibilityLabel()}.";

                return false;
            }

            return true;
        }

        public string GetItemCompatibilityLabel()
        {
            if (item is EquipmentItemData equipmentItem)
            {
                return
                    $"{equipmentItem.Slot} '{equipmentItem.DisplayName}'";
            }

            return item != null
                ? item.DisplayName
                : "item nao configurado";
        }

        private static bool ContainsEffect(
            IReadOnlyList<ItemAffixEffectType> effects,
            ItemAffixEffectType effect)
        {
            for (int index = 0;
                 index < effects.Count;
                 index++)
            {
                if (effects[index] == effect)
                {
                    return true;
                }
            }

            return false;
        }

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

            allowedPrefixEffects ??=
                new List<ItemAffixEffectType>();

            allowedSuffixEffects ??=
                new List<ItemAffixEffectType>();

            affixSlotFillChance =
                Mathf.Clamp01(
                    affixSlotFillChance);
        }
    }
}