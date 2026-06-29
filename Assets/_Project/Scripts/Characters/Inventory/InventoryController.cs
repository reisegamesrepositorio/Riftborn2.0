using System;
using Riftborn.Items;
using UnityEngine;

namespace Riftborn.Characters.Inventory
{
    /// <summary>
    /// Mantido como camada de compatibilidade para sistemas
    /// que já trabalham com slots e quantidades.
    ///
    /// O conteúdo real do slot agora é uma ItemInstance.
    /// </summary>
    [Serializable]
    public sealed class ItemStack
    {
        [SerializeField]
        private ItemInstance instance;

        public ItemStack(ItemInstance instance)
        {
            this.instance = instance;
        }

        public ItemStack(
            ItemData item,
            int quantity)
        {
            instance =
                item != null
                    ? new ItemInstance(
                        item,
                        quantity)
                    : null;
        }

        public ItemInstance Instance =>
            instance;

        public ItemData Item =>
            instance?.Item;

        public ItemRarityData Rarity =>
            instance?.Rarity;

        public int Quantity =>
            instance?.Quantity ?? 0;

        public bool IsValid =>
            instance != null &&
            instance.IsValid;

        public void SetQuantity(int quantity)
        {
            instance?.SetQuantity(quantity);
        }

        public int AddQuantity(int quantity)
        {
            return instance != null
                ? instance.AddQuantity(quantity)
                : 0;
        }

        public int RemoveQuantity(int quantity)
        {
            return instance != null
                ? instance.RemoveQuantity(quantity)
                : 0;
        }
    }

    public sealed class InventoryController : MonoBehaviour
    {
        [Header("Inventory")]
        [SerializeField, Min(1)]
        private int slotCount = 30;

        [SerializeField]
        private ItemStack[] slots;

        public event Action InventoryChanged;

        public event Action<int, ItemInstance>
            SlotChanged;

        public int SlotCount =>
            slots?.Length ?? slotCount;

        public int OccupiedSlotCount
        {
            get
            {
                EnsureSlots();

                int occupiedCount = 0;

                for (int index = 0;
                     index < slots.Length;
                     index++)
                {
                    if (IsOccupied(index))
                    {
                        occupiedCount++;
                    }
                }

                return occupiedCount;
            }
        }

        public int EmptySlotCount =>
            SlotCount - OccupiedSlotCount;

        private void Awake()
        {
            EnsureSlots();
        }

        private void OnValidate()
        {
            slotCount =
                Mathf.Max(
                    1,
                    slotCount);

            ResizeSlots(slotCount);
        }

        public ItemStack GetSlot(int index)
        {
            EnsureSlots();

            return IsValidSlot(index)
                ? slots[index]
                : null;
        }

        public ItemInstance GetItemInstance(
            int index)
        {
            return GetSlot(index)?.Instance;
        }

        public bool IsOccupied(int index)
        {
            if (!IsValidSlot(index))
            {
                return false;
            }

            ItemStack slot =
                slots[index];

            return slot != null &&
                   slot.IsValid;
        }

        /// <summary>
        /// Adiciona uma instância já gerada ao inventário.
        ///
        /// Equipamentos preservam exatamente a mesma instância,
        /// incluindo ID, raridade e afixos.
        /// </summary>
        public bool Add(
            ItemInstance itemInstance)
        {
            EnsureSlots();

            if (itemInstance == null ||
                !itemInstance.IsValid)
            {
                return false;
            }

            if (!CanAdd(itemInstance))
            {
                return false;
            }

            if (!itemInstance.IsStackable ||
                itemInstance.AffixCount > 0)
            {
                int emptySlot =
                    FindFirstEmptySlot();

                if (emptySlot < 0)
                {
                    return false;
                }

                slots[emptySlot] =
                    new ItemStack(itemInstance);

                NotifySlotChanged(emptySlot);

                return true;
            }

            int remainingQuantity =
                itemInstance.Quantity;

            bool mergedIntoExistingStack =
                false;

            for (int index = 0;
                 index < slots.Length &&
                 remainingQuantity > 0;
                 index++)
            {
                ItemInstance existingInstance =
                    slots[index]?.Instance;

                if (existingInstance == null ||
                    !existingInstance.CanStackWith(
                        itemInstance))
                {
                    continue;
                }

                int amountAdded =
                    existingInstance.AddQuantity(
                        remainingQuantity);

                if (amountAdded <= 0)
                {
                    continue;
                }

                remainingQuantity -=
                    amountAdded;

                mergedIntoExistingStack = true;

                NotifySlotChanged(
                    index,
                    invokeInventoryChanged: false);
            }

            if (remainingQuantity > 0)
            {
                int emptySlot =
                    FindFirstEmptySlot();

                if (emptySlot < 0)
                {
                    return false;
                }

                ItemInstance instanceToStore;

                if (!mergedIntoExistingStack &&
                    remainingQuantity ==
                    itemInstance.Quantity)
                {
                    instanceToStore =
                        itemInstance;
                }
                else
                {
                    instanceToStore =
                        new ItemInstance(
                            itemInstance.Item,
                            remainingQuantity,
                            itemInstance.Rarity);
                }

                slots[emptySlot] =
                    new ItemStack(
                        instanceToStore);

                NotifySlotChanged(
                    emptySlot,
                    invokeInventoryChanged: false);
            }

            InventoryChanged?.Invoke();

            return true;
        }

        /// <summary>
        /// Compatibilidade para sistemas antigos que adicionam
        /// diretamente ItemData e quantidade.
        ///
        /// Novos drops devem preferir Add(ItemInstance).
        /// </summary>
        public bool Add(
            ItemData item,
            int quantity)
        {
            EnsureSlots();

            if (item == null ||
                quantity <= 0)
            {
                return false;
            }

            if (!CanAdd(
                    item,
                    quantity,
                    rarity: null))
            {
                return false;
            }

            int remainingQuantity =
                quantity;

            if (item.IsStackable)
            {
                for (int index = 0;
                     index < slots.Length &&
                     remainingQuantity > 0;
                     index++)
                {
                    ItemInstance existingInstance =
                        slots[index]?.Instance;

                    if (!CanStackWithDefinition(
                            existingInstance,
                            item,
                            rarity: null))
                    {
                        continue;
                    }

                    int amountAdded =
                        existingInstance.AddQuantity(
                            remainingQuantity);

                    if (amountAdded <= 0)
                    {
                        continue;
                    }

                    remainingQuantity -=
                        amountAdded;

                    NotifySlotChanged(
                        index,
                        invokeInventoryChanged: false);
                }
            }

            while (remainingQuantity > 0)
            {
                int emptySlot =
                    FindFirstEmptySlot();

                if (emptySlot < 0)
                {
                    return false;
                }

                int stackQuantity =
                    Mathf.Min(
                        remainingQuantity,
                        item.MaxStack);

                ItemInstance newInstance =
                    new ItemInstance(
                        item,
                        stackQuantity);

                slots[emptySlot] =
                    new ItemStack(
                        newInstance);

                remainingQuantity -=
                    stackQuantity;

                NotifySlotChanged(
                    emptySlot,
                    invokeInventoryChanged: false);
            }

            InventoryChanged?.Invoke();

            return true;
        }

        public bool CanAdd(
            ItemInstance itemInstance)
        {
            EnsureSlots();

            if (itemInstance == null ||
                !itemInstance.IsValid)
            {
                return false;
            }

            if (!itemInstance.IsStackable ||
                itemInstance.AffixCount > 0)
            {
                return FindFirstEmptySlot() >= 0;
            }

            int availableCapacity = 0;

            for (int index = 0;
                 index < slots.Length;
                 index++)
            {
                ItemInstance existingInstance =
                    slots[index]?.Instance;

                if (existingInstance == null)
                {
                    availableCapacity +=
                        itemInstance.MaximumStack;

                    continue;
                }

                if (!existingInstance.CanStackWith(
                        itemInstance))
                {
                    continue;
                }

                availableCapacity +=
                    existingInstance.AvailableStackSpace;
            }

            return availableCapacity >=
                   itemInstance.Quantity;
        }

        public bool CanAdd(
            ItemData item,
            int quantity,
            ItemRarityData rarity = null)
        {
            EnsureSlots();

            if (item == null ||
                quantity <= 0)
            {
                return false;
            }

            int availableCapacity = 0;

            for (int index = 0;
                 index < slots.Length;
                 index++)
            {
                ItemInstance existingInstance =
                    slots[index]?.Instance;

                if (existingInstance == null)
                {
                    availableCapacity +=
                        item.MaxStack;

                    continue;
                }

                if (!CanStackWithDefinition(
                        existingInstance,
                        item,
                        rarity))
                {
                    continue;
                }

                availableCapacity +=
                    existingInstance.AvailableStackSpace;
            }

            return availableCapacity >= quantity;
        }

        public bool RemoveAt(
            int index,
            int quantity)
        {
            EnsureSlots();

            if (!IsValidSlot(index) ||
                quantity <= 0)
            {
                return false;
            }

            ItemInstance instance =
                slots[index]?.Instance;

            if (instance == null ||
                !instance.IsValid)
            {
                return false;
            }

            instance.RemoveQuantity(
                quantity);

            if (instance.IsEmpty)
            {
                slots[index] = null;
            }

            NotifySlotChanged(index);

            return true;
        }

        public ItemInstance TakeAt(int index)
        {
            EnsureSlots();

            if (!IsOccupied(index))
            {
                return null;
            }

            ItemInstance instance =
                slots[index].Instance;

            slots[index] = null;

            NotifySlotChanged(index);

            return instance;
        }

        public bool RemoveInstance(
            string instanceId)
        {
            int slotIndex =
                FindSlotByInstanceId(
                    instanceId);

            if (slotIndex < 0)
            {
                return false;
            }

            slots[slotIndex] = null;

            NotifySlotChanged(slotIndex);

            return true;
        }

        public bool Move(
            int from,
            int to)
        {
            EnsureSlots();

            if (!IsValidSlot(from) ||
                !IsValidSlot(to) ||
                from == to)
            {
                return false;
            }

            (slots[from], slots[to]) =
                (slots[to], slots[from]);

            NotifySlotChanged(
                from,
                invokeInventoryChanged: false);

            NotifySlotChanged(
                to,
                invokeInventoryChanged: false);

            InventoryChanged?.Invoke();

            return true;
        }

        public int FindSlotByInstanceId(
            string instanceId)
        {
            EnsureSlots();

            if (string.IsNullOrWhiteSpace(
                    instanceId))
            {
                return -1;
            }

            for (int index = 0;
                 index < slots.Length;
                 index++)
            {
                ItemInstance instance =
                    slots[index]?.Instance;

                if (instance == null)
                {
                    continue;
                }

                if (string.Equals(
                        instance.InstanceId,
                        instanceId,
                        StringComparison.Ordinal))
                {
                    return index;
                }
            }

            return -1;
        }

        public bool ContainsInstance(
            string instanceId)
        {
            return FindSlotByInstanceId(
                       instanceId) >= 0;
        }

        public void Clear()
        {
            EnsureSlots();

            for (int index = 0;
                 index < slots.Length;
                 index++)
            {
                slots[index] = null;

                SlotChanged?.Invoke(
                    index,
                    null);
            }

            InventoryChanged?.Invoke();
        }

        private void EnsureSlots()
        {
            slotCount =
                Mathf.Max(
                    1,
                    slotCount);

            ResizeSlots(slotCount);
        }

        private void ResizeSlots(
            int requestedSlotCount)
        {
            int safeSlotCount =
                Mathf.Max(
                    1,
                    requestedSlotCount);

            if (slots != null &&
                slots.Length == safeSlotCount)
            {
                return;
            }

            ItemStack[] resizedSlots =
                new ItemStack[safeSlotCount];

            if (slots != null)
            {
                int amountToCopy =
                    Mathf.Min(
                        slots.Length,
                        resizedSlots.Length);

                Array.Copy(
                    slots,
                    resizedSlots,
                    amountToCopy);
            }

            slots =
                resizedSlots;

            slotCount =
                safeSlotCount;
        }

        private int FindFirstEmptySlot()
        {
            for (int index = 0;
                 index < slots.Length;
                 index++)
            {
                if (!IsOccupied(index))
                {
                    return index;
                }
            }

            return -1;
        }

        private bool IsValidSlot(int index)
        {
            return slots != null &&
                   index >= 0 &&
                   index < slots.Length;
        }

        private static bool CanStackWithDefinition(
            ItemInstance existingInstance,
            ItemData item,
            ItemRarityData rarity)
        {
            if (existingInstance == null ||
                !existingInstance.IsValid ||
                !existingInstance.IsStackable)
            {
                return false;
            }

            if (existingInstance.Item != item ||
                existingInstance.Rarity != rarity)
            {
                return false;
            }

            if (existingInstance.AffixCount > 0)
            {
                return false;
            }

            return existingInstance.AvailableStackSpace > 0;
        }

        private void NotifySlotChanged(
            int index,
            bool invokeInventoryChanged = true)
        {
            SlotChanged?.Invoke(
                index,
                GetItemInstance(index));

            if (invokeInventoryChanged)
            {
                InventoryChanged?.Invoke();
            }
        }
    }
}