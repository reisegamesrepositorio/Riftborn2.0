using System;
using Riftborn.Items;
using UnityEngine;
namespace Riftborn.Characters.Inventory
{
    [Serializable]
    public sealed class ItemStack { public ItemStack(ItemData item, int quantity) { Item = item; Quantity = quantity; } public ItemData Item { get; private set; } public int Quantity { get; private set; } public void SetQuantity(int quantity) { Quantity = quantity; } }
    public sealed class InventoryController : MonoBehaviour
    {
        [SerializeField] private int slotCount = 30;
        private ItemStack[] slots;
        public event Action InventoryChanged;
        public int SlotCount => slotCount;
        private void Awake() { slots = new ItemStack[slotCount]; }
        public ItemStack GetSlot(int index) => IsValidSlot(index) ? slots[index] : null;
        public bool Add(ItemData item, int quantity) { if (item == null || quantity <= 0) return false; for (int i = 0; i < slots.Length; i++) if (slots[i] == null) { slots[i] = new ItemStack(item, quantity); InventoryChanged?.Invoke(); return true; } return false; }
        public bool RemoveAt(int index, int quantity) { if (!IsValidSlot(index) || slots[index] == null || quantity <= 0) return false; int newQuantity = slots[index].Quantity - quantity; if (newQuantity <= 0) slots[index] = null; else slots[index].SetQuantity(newQuantity); InventoryChanged?.Invoke(); return true; }
        public bool Move(int from, int to) { if (!IsValidSlot(from) || !IsValidSlot(to) || from == to) return false; (slots[from], slots[to]) = (slots[to], slots[from]); InventoryChanged?.Invoke(); return true; }
        private bool IsValidSlot(int index) => index >= 0 && index < slots.Length;
    }
}
