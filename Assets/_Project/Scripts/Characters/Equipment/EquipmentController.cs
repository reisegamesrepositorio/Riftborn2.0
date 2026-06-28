using System;
using System.Collections.Generic;
using Riftborn.Characters.Stats;
using Riftborn.Items;
using UnityEngine;
namespace Riftborn.Characters.Equipment
{
    public enum EquipmentSlot { Head, Chest, Legs, Weapon, OffHand, Accessory }
    [Serializable]
    public sealed class StatModifierDefinition { public CharacterStat stat; public float flatValue; public float additivePercent; public float multiplicativePercent; public int priority; }
    public sealed class EquipmentItemData : ItemData
    {
        [SerializeField] private EquipmentSlot slot;
        [SerializeField] private List<StatModifierDefinition> statModifiers = new();
        public EquipmentSlot Slot => slot;
        public IReadOnlyList<StatModifierDefinition> StatModifiers => statModifiers;
    }
    public sealed class EquipmentController : MonoBehaviour
    {
        [SerializeField] private CharacterStatsController stats;
        private readonly Dictionary<EquipmentSlot, EquipmentItemData> equipped = new();
        public event Action<EquipmentSlot, EquipmentItemData> EquipmentChanged;
        private void Awake() { stats ??= GetComponent<CharacterStatsController>(); }
        public bool Equip(EquipmentItemData item)
        {
            if (item == null || stats == null) return false;
            Unequip(item.Slot); equipped[item.Slot] = item;
            foreach (var definition in item.StatModifiers) stats.AddModifier(new StatModifier($"equipment:{item.name}:{definition.stat}", item, definition.stat, definition.flatValue, definition.additivePercent, definition.multiplicativePercent, definition.priority));
            EquipmentChanged?.Invoke(item.Slot, item); return true;
        }
        public bool Unequip(EquipmentSlot slot)
        {
            if (!equipped.TryGetValue(slot, out EquipmentItemData item)) return false;
            stats?.RemoveModifiersFromSource(item); equipped.Remove(slot); EquipmentChanged?.Invoke(slot, null); return true;
        }
    }
}
