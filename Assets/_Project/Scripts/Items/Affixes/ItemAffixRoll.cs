using System;
using UnityEngine;

namespace Riftborn.Items
{
    [Serializable]
    public sealed class ItemAffixRoll
    {
        [SerializeField]
        private ItemAffixData affix;

        [SerializeField, Range(1, 10)]
        private int tier;

        [SerializeField]
        private float rolledValue;

        public ItemAffixRoll(
            ItemAffixData affix,
            int tier,
            float rolledValue)
        {
            this.affix =
                affix;

            this.tier =
                Mathf.Clamp(
                    tier,
                    1,
                    10);

            this.rolledValue =
                rolledValue;
        }

        public ItemAffixData Affix =>
            affix;

        public int Tier =>
            Mathf.Clamp(
                tier,
                1,
                10);

        public float RolledValue =>
            rolledValue;

        public ItemAffixType AffixType =>
            affix != null
                ? affix.AffixType
                : ItemAffixType.Prefix;

        public bool IsValid =>
            affix != null &&
            tier >= 1 &&
            tier <= 10;

        public string DisplayName =>
            affix != null
                ? affix.DisplayName
                : string.Empty;

        public static bool TryCreate(
            ItemAffixData affix,
            int tier,
            out ItemAffixRoll roll)
        {
            roll = null;

            if (affix == null)
            {
                return false;
            }

            if (!affix.TryRollValue(
                    tier,
                    out float rolledValue))
            {
                return false;
            }

            roll =
                new ItemAffixRoll(
                    affix,
                    tier,
                    rolledValue);

            return true;
        }
    }
}