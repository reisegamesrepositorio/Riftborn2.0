using Riftborn.Items;
using UnityEngine;

namespace Riftborn.Characters.Equipment
{
    [CreateAssetMenu(
        fileName = "NewWeaponItem",
        menuName = "Riftborn/Items/Weapon Item")]
    public sealed class WeaponItemData : EquipmentItemData
    {
        [Header("Weapon Damage")]
        [SerializeField, Min(0f)]
        private float baseMinimumDamage = 5f;

        [SerializeField, Min(0f)]
        private float baseMaximumDamage = 10f;

        public float BaseMinimumDamage =>
            Mathf.Min(
                baseMinimumDamage,
                baseMaximumDamage);

        public float BaseMaximumDamage =>
            Mathf.Max(
                baseMinimumDamage,
                baseMaximumDamage);

        public Vector2 GetFinalDamageRange(
            ItemRarityData rarity)
        {
            float rarityMultiplier =
                rarity != null
                    ? rarity.BaseValueMultiplier
                    : 1f;

            rarityMultiplier =
                Mathf.Max(
                    0.01f,
                    rarityMultiplier);

            float finalMinimumDamage =
                BaseMinimumDamage *
                rarityMultiplier;

            float finalMaximumDamage =
                BaseMaximumDamage *
                rarityMultiplier;

            return new Vector2(
                finalMinimumDamage,
                finalMaximumDamage);
        }

        protected override void OnValidate()
        {
            base.OnValidate();

            baseMinimumDamage =
                Mathf.Max(
                    0f,
                    baseMinimumDamage);

            baseMaximumDamage =
                Mathf.Max(
                    baseMinimumDamage,
                    baseMaximumDamage);
        }
    }
}