using UnityEngine;

namespace Riftborn.Items
{
    [CreateAssetMenu(
        fileName = "NewItemRarity",
        menuName = "Riftborn/Items/Item Rarity")]
    public sealed class ItemRarityData : ScriptableObject
    {
        [Header("Identity")]
        [SerializeField]
        private string rarityId;

        [SerializeField]
        private string displayName;

        [Tooltip(
            "Usado para ordenar raridades. " +
            "Quanto maior, mais elevada é a raridade.")]
        [SerializeField]
        private int sortOrder;

        [Header("Affix Capacity")]
        [Tooltip(
            "Quantidade máxima de prefixos que esta " +
            "raridade permite.")]
        [SerializeField, Min(0)]
        private int maxPrefixes;

        [Tooltip(
            "Quantidade máxima de sufixos que esta " +
            "raridade permite.")]
        [SerializeField, Min(0)]
        private int maxSuffixes;

        [Header("Equipment Scaling")]
        [Tooltip(
            "Multiplicador provisório aplicado ao valor-base " +
            "do equipamento. Será balanceado posteriormente.")]
        [SerializeField, Min(0.01f)]
        private float baseValueMultiplier = 1f;

        public string RarityId =>
            rarityId;

        public string DisplayName =>
            string.IsNullOrWhiteSpace(displayName)
                ? name
                : displayName;

        public int SortOrder =>
            sortOrder;

        public int MaxPrefixes =>
            Mathf.Max(
                0,
                maxPrefixes);

        public int MaxSuffixes =>
            Mathf.Max(
                0,
                maxSuffixes);

        public int MaximumAffixCount =>
            MaxPrefixes +
            MaxSuffixes;

        public float BaseValueMultiplier =>
            Mathf.Max(
                0.01f,
                baseValueMultiplier);

        private void OnValidate()
        {
            maxPrefixes =
                Mathf.Max(
                    0,
                    maxPrefixes);

            maxSuffixes =
                Mathf.Max(
                    0,
                    maxSuffixes);

            baseValueMultiplier =
                Mathf.Max(
                    0.01f,
                    baseValueMultiplier);
        }
    }
}