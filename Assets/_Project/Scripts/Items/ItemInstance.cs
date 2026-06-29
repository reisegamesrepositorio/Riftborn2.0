using System;
using System.Collections.Generic;
using UnityEngine;

namespace Riftborn.Items
{
    [Serializable]
    public sealed class ItemInstance
    {
        [SerializeField]
        private string instanceId;

        [SerializeField]
        private ItemData item;

        [SerializeField]
        private ItemRarityData rarity;

        [SerializeField, Min(0)]
        private int quantity = 1;

        [SerializeField]
        private List<ItemAffixRoll> prefixes =
            new();

        [SerializeField]
        private List<ItemAffixRoll> suffixes =
            new();

        public ItemInstance(
            ItemData item,
            int quantity = 1,
            ItemRarityData rarity = null)
        {
            instanceId =
                Guid.NewGuid().ToString("N");

            this.item =
                item;

            this.rarity =
                rarity;

            prefixes =
                new List<ItemAffixRoll>();

            suffixes =
                new List<ItemAffixRoll>();

            SetQuantity(quantity);
        }

        public ItemInstance(
            string instanceId,
            ItemData item,
            int quantity,
            ItemRarityData rarity = null)
        {
            this.instanceId =
                string.IsNullOrWhiteSpace(instanceId)
                    ? Guid.NewGuid().ToString("N")
                    : instanceId;

            this.item =
                item;

            this.rarity =
                rarity;

            prefixes =
                new List<ItemAffixRoll>();

            suffixes =
                new List<ItemAffixRoll>();

            SetQuantity(quantity);
        }

        public string InstanceId =>
            instanceId;

        public ItemData Item =>
            item;

        public ItemRarityData Rarity =>
            rarity;

        public string DisplayName =>
            item != null
                ? item.DisplayName
                : string.Empty;

        public int Quantity =>
            quantity;

        public int MaximumStack =>
            item != null
                ? item.MaxStack
                : 0;

        public IReadOnlyList<ItemAffixRoll> Prefixes =>
            prefixes;

        public IReadOnlyList<ItemAffixRoll> Suffixes =>
            suffixes;

        public int PrefixCount =>
            prefixes?.Count ?? 0;

        public int SuffixCount =>
            suffixes?.Count ?? 0;

        public int AffixCount =>
            PrefixCount +
            SuffixCount;

        public int MaximumPrefixCount =>
            rarity != null
                ? rarity.MaxPrefixes
                : 0;

        public int MaximumSuffixCount =>
            rarity != null
                ? rarity.MaxSuffixes
                : 0;

        public bool IsValid =>
            item != null &&
            quantity > 0;

        public bool IsEmpty =>
            quantity <= 0;

        public bool IsStackable =>
            item != null &&
            item.IsStackable;

        public int AvailableStackSpace =>
            Mathf.Max(
                0,
                MaximumStack - quantity);

        public void SetRarity(
            ItemRarityData newRarity)
        {
            rarity =
                newRarity;

            TrimAffixesToRarityCapacity();
        }

        public void SetQuantity(
            int newQuantity)
        {
            if (item == null)
            {
                quantity = 0;
                return;
            }

            quantity =
                Mathf.Clamp(
                    newQuantity,
                    0,
                    item.MaxStack);
        }

        public int AddQuantity(
            int requestedQuantity)
        {
            if (item == null ||
                requestedQuantity <= 0)
            {
                return 0;
            }

            int amountAdded =
                Mathf.Min(
                    requestedQuantity,
                    AvailableStackSpace);

            quantity +=
                amountAdded;

            return amountAdded;
        }

        public int RemoveQuantity(
            int requestedQuantity)
        {
            if (requestedQuantity <= 0 ||
                quantity <= 0)
            {
                return 0;
            }

            int amountRemoved =
                Mathf.Min(
                    requestedQuantity,
                    quantity);

            quantity -=
                amountRemoved;

            return amountRemoved;
        }

        public bool TryAddAffix(
            ItemAffixRoll roll)
        {
            if (roll == null ||
                !roll.IsValid)
            {
                return false;
            }

            if (ContainsAffix(roll.Affix))
            {
                return false;
            }

            switch (roll.AffixType)
            {
                case ItemAffixType.Prefix:
                    if (PrefixCount >=
                        MaximumPrefixCount)
                    {
                        return false;
                    }

                    prefixes.Add(roll);
                    return true;

                case ItemAffixType.Suffix:
                    if (SuffixCount >=
                        MaximumSuffixCount)
                    {
                        return false;
                    }

                    suffixes.Add(roll);
                    return true;

                default:
                    return false;
            }
        }

        public bool RemoveAffix(
            ItemAffixRoll roll)
        {
            if (roll == null)
            {
                return false;
            }

            return prefixes.Remove(roll) ||
                   suffixes.Remove(roll);
        }

        public bool ContainsAffix(
            ItemAffixData affix)
        {
            if (affix == null)
            {
                return false;
            }

            for (int index = 0;
                 index < PrefixCount;
                 index++)
            {
                if (prefixes[index]?.Affix == affix)
                {
                    return true;
                }
            }

            for (int index = 0;
                 index < SuffixCount;
                 index++)
            {
                if (suffixes[index]?.Affix == affix)
                {
                    return true;
                }
            }

            return false;
        }

        public bool CanStackWith(
            ItemInstance other)
        {
            if (other == null ||
                !IsStackable ||
                !other.IsStackable)
            {
                return false;
            }

            if (item != other.item)
            {
                return false;
            }

            if (rarity != other.rarity)
            {
                return false;
            }

            if (AffixCount > 0 ||
                other.AffixCount > 0)
            {
                return false;
            }

            return quantity < MaximumStack;
        }

        private void TrimAffixesToRarityCapacity()
        {
            prefixes ??=
                new List<ItemAffixRoll>();

            suffixes ??=
                new List<ItemAffixRoll>();

            while (prefixes.Count >
                   MaximumPrefixCount)
            {
                prefixes.RemoveAt(
                    prefixes.Count - 1);
            }

            while (suffixes.Count >
                   MaximumSuffixCount)
            {
                suffixes.RemoveAt(
                    suffixes.Count - 1);
            }
        }
    }
}