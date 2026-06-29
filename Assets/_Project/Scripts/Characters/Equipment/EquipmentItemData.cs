using System;
using System.Collections.Generic;
using Riftborn.Characters.Defense;
using Riftborn.Characters.Stats;
using Riftborn.Items;
using UnityEngine;

namespace Riftborn.Characters.Equipment
{
    public enum EquipmentSlot
    {
        Head = 0,
        Chest = 1,
        Legs = 2,
        Weapon = 3,
        OffHand = 4,
        Accessory = 5
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

        public CharacterStat Stat =>
            stat;

        public float FlatValue =>
            flatValue;

        public float AdditivePercent =>
            additivePercent;

        public float MultiplicativePercent =>
            multiplicativePercent;
    }

    [Serializable]
    public sealed class DefenseModifierDefinition
    {
        [SerializeField]
        private DefenseType defenseType;

        [SerializeField]
        private float flatValue;

        [SerializeField]
        private float additivePercent;

        [SerializeField]
        private float multiplicativePercent;

        public DefenseType DefenseType =>
            defenseType;

        public float FlatValue =>
            flatValue;

        public float AdditivePercent =>
            additivePercent;

        public float MultiplicativePercent =>
            multiplicativePercent;
    }

    [CreateAssetMenu(
        fileName = "NewEquipmentItem",
        menuName = "Riftborn/Items/Equipment Item")]
    public class EquipmentItemData : ItemData
    {
        [Header("Equipment")]
        [SerializeField]
        private EquipmentSlot slot;

        [Header("Base Stat Modifiers")]
        [SerializeField]
        private List<StatModifierDefinition> statModifiers =
            new();

        [Header("Base Defense Modifiers")]
        [SerializeField]
        private List<DefenseModifierDefinition> defenseModifiers =
            new();

        public EquipmentSlot Slot =>
            slot;

        public IReadOnlyList<StatModifierDefinition>
            StatModifiers =>
                statModifiers;

        public IReadOnlyList<DefenseModifierDefinition>
            DefenseModifiers =>
                defenseModifiers;
    }
}