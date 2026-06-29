using System;
using System.Collections.Generic;
using Riftborn.Characters.Stats;
using Riftborn.Items;
using UnityEngine;

namespace Riftborn.Characters.Equipment
{
    public enum EquipmentSlot
    {
        Head,
        Chest,
        Legs,
        Weapon,
        OffHand,
        Accessory
    }

    [Serializable]
    public sealed class StatModifierDefinition
    {
        [SerializeField]
        private CharacterStat stat;

        [SerializeField]
        private float flatValue;

        [SerializeField]
        private float additivePercent;

        [SerializeField]
        private float multiplicativePercent;

        public CharacterStat Stat => stat;

        public float FlatValue => flatValue;

        public float AdditivePercent =>
            additivePercent;

        public float MultiplicativePercent =>
            multiplicativePercent;
    }

    public sealed class EquipmentItemData : ItemData
    {
        [SerializeField]
        private EquipmentSlot slot;

        [SerializeField]
        private List<StatModifierDefinition> statModifiers = new();

        public EquipmentSlot Slot => slot;

        public IReadOnlyList<StatModifierDefinition> StatModifiers =>
            statModifiers;
    }

    public sealed class EquipmentController : MonoBehaviour
    {
        [SerializeField]
        private CharacterStatsController stats;

        private readonly Dictionary<EquipmentSlot, EquipmentItemData>
            equipped = new();

        public event Action<EquipmentSlot, EquipmentItemData>
            EquipmentChanged;

        private void Awake()
        {
            stats ??=
                GetComponent<CharacterStatsController>();

            if (stats == null)
            {
                Debug.LogError(
                    $"{nameof(EquipmentController)} requires a " +
                    $"{nameof(CharacterStatsController)}.",
                    this);
            }
        }

        public bool Equip(EquipmentItemData item)
        {
            if (item == null || stats == null)
            {
                return false;
            }

            // Remove primeiro o equipamento que já ocupa o slot.
            Unequip(item.Slot);

            equipped[item.Slot] = item;

            for (int index = 0;
                 index < item.StatModifiers.Count;
                 index++)
            {
                StatModifierDefinition definition =
                    item.StatModifiers[index];

                string modifierId =
                    $"equipment:{item.Slot}:{index}:{definition.Stat}";

                StatModifier modifier = new StatModifier(
                    id: modifierId,
                    source: item,
                    stat: definition.Stat,
                    flatValue: definition.FlatValue,
                    additivePercent:
                        definition.AdditivePercent,
                    multiplicativePercent:
                        definition.MultiplicativePercent);

                bool added = stats.AddModifier(modifier);

                if (added)
                {
                    continue;
                }

                // Impede que o item fique parcialmente aplicado.
                stats.RemoveModifiersFromSource(item);
                equipped.Remove(item.Slot);

                Debug.LogError(
                    $"Failed to apply modifier '{modifierId}' " +
                    $"from equipment '{item.name}'.",
                    this);

                return false;
            }

            EquipmentChanged?.Invoke(
                item.Slot,
                item);

            return true;
        }

        public bool Unequip(EquipmentSlot slot)
        {
            if (!equipped.TryGetValue(
                    slot,
                    out EquipmentItemData item))
            {
                return false;
            }

            if (stats != null)
            {
                stats.RemoveModifiersFromSource(item);
            }

            equipped.Remove(slot);

            EquipmentChanged?.Invoke(
                slot,
                null);

            return true;
        }

        public EquipmentItemData GetEquippedItem(
            EquipmentSlot slot)
        {
            equipped.TryGetValue(
                slot,
                out EquipmentItemData item);

            return item;
        }

        public bool IsSlotOccupied(EquipmentSlot slot)
        {
            return equipped.ContainsKey(slot);
        }
    }
}