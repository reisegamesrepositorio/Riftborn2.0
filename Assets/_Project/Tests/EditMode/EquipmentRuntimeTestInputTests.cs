#if false
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using Riftborn.Characters.Abilities;
using Riftborn.Characters.Combat;
using Riftborn.Characters.Defense;
using Riftborn.Characters.Equipment;
using Riftborn.Characters.Health;
using Riftborn.Characters.Inventory;
using Riftborn.Characters.Movement;
using Riftborn.Characters.Resources;
using Riftborn.Characters.Stats;
using Riftborn.Items;
using UnityEngine;

namespace Riftborn.Tests
{
    public sealed class EquipmentRuntimeTestInputTests
    {
        private sealed class TestRig
        {
            public GameObject GameObject;
            public InventoryController Inventory;
            public EquipmentController Equipment;
            public CharacterStatsController Stats;
            public EquipmentRuntimeTestInput Input;
        }

        [Test]
        public void EquipsExistingInventoryInstanceAndPreservesId()
        {
            TestRig rig = CreateRig();
            ItemInstance item = CreateEquipmentInstance("Sword", EquipmentSlot.Weapon, 7f);
            string instanceId = item.InstanceId;

            Assert.IsTrue(rig.Inventory.Add(item));
            int inventorySlot = rig.Inventory.FindSlotByInstanceId(instanceId);

            Assert.IsTrue(rig.Input.TryEquipMostRecentInventoryEquipment());

            Assert.AreEqual(-1, rig.Inventory.FindSlotByInstanceId(instanceId));
            Assert.AreSame(item, rig.Equipment.GetEquippedInstance(EquipmentSlot.Weapon));
            Assert.AreEqual(instanceId, rig.Equipment.GetEquippedInstance(EquipmentSlot.Weapon).InstanceId);
            Assert.IsFalse(rig.Inventory.IsOccupied(inventorySlot));
            Object.DestroyImmediate(rig.GameObject);
        }

        [Test]
        public void EquipmentModifiersDoNotAccumulateAfterRepeatedEquipUnequip()
        {
            TestRig rig = CreateRig();
            ItemInstance item = CreateEquipmentInstance("Sword", EquipmentSlot.Weapon, 5f);
            float baseStr = rig.Stats.GetFinalValue(CharacterStat.STR);

            Assert.IsTrue(rig.Inventory.Add(item));
            Assert.IsTrue(rig.Input.TryEquipMostRecentInventoryEquipment());
            Assert.AreEqual(baseStr + 5f, rig.Stats.GetFinalValue(CharacterStat.STR), 0.001f);
            Assert.IsTrue(rig.Input.TryUnequipLastTestEquipment());
            Assert.AreEqual(baseStr, rig.Stats.GetFinalValue(CharacterStat.STR), 0.001f);
            Assert.IsTrue(rig.Input.TryEquipMostRecentInventoryEquipment());
            Assert.AreEqual(baseStr + 5f, rig.Stats.GetFinalValue(CharacterStat.STR), 0.001f);
            Assert.IsTrue(rig.Input.TryUnequipLastTestEquipment());
            Assert.AreEqual(baseStr, rig.Stats.GetFinalValue(CharacterStat.STR), 0.001f);

            Object.DestroyImmediate(rig.GameObject);
        }

        [Test]
        public void ReplacesSameSlotWithoutLosingPreviousInstance()
        {
            TestRig rig = CreateRig();
            ItemInstance first = CreateEquipmentInstance("Sword A", EquipmentSlot.Weapon, 3f);
            ItemInstance second = CreateEquipmentInstance("Sword B", EquipmentSlot.Weapon, 8f);

            Assert.IsTrue(rig.Inventory.Add(first));
            Assert.IsTrue(rig.Input.TryEquipMostRecentInventoryEquipment());
            Assert.IsTrue(rig.Inventory.Add(second));

            Assert.IsTrue(rig.Input.TryEquipMostRecentInventoryEquipment());

            Assert.AreSame(second, rig.Equipment.GetEquippedInstance(EquipmentSlot.Weapon));
            Assert.GreaterOrEqual(rig.Inventory.FindSlotByInstanceId(first.InstanceId), 0);
            Assert.AreEqual(-1, rig.Inventory.FindSlotByInstanceId(second.InstanceId));
            Assert.AreEqual(18f, rig.Stats.GetFinalValue(CharacterStat.STR), 0.001f);

            Object.DestroyImmediate(rig.GameObject);
        }

        [Test]
        public void UnequipsAndReturnsSameInstanceToInventory()
        {
            TestRig rig = CreateRig();
            ItemInstance item = CreateEquipmentInstance("Helmet", EquipmentSlot.Head, 4f);
            string instanceId = item.InstanceId;
            ItemRarityData rarity = item.Rarity;
            int tier = item.Prefixes[0].Tier;
            float value = item.Prefixes[0].RolledValue;

            Assert.IsTrue(rig.Inventory.Add(item));
            Assert.IsTrue(rig.Input.TryEquipMostRecentInventoryEquipment());
            Assert.IsTrue(rig.Input.TryUnequipLastTestEquipment());

            int slot = rig.Inventory.FindSlotByInstanceId(instanceId);
            ItemInstance returned = rig.Inventory.GetItemInstance(slot);

            Assert.AreSame(item, returned);
            Assert.AreEqual(instanceId, returned.InstanceId);
            Assert.AreSame(rarity, returned.Rarity);
            Assert.AreEqual(tier, returned.Prefixes[0].Tier);
            Assert.AreEqual(value, returned.Prefixes[0].RolledValue, 0.001f);
            Assert.IsNull(rig.Equipment.GetEquippedInstance(EquipmentSlot.Head));

            Object.DestroyImmediate(rig.GameObject);
        }

        [Test]
        public void UnequipFailsSafelyWhenInventoryIsFull()
        {
            TestRig rig = CreateRig(slotCount: 1);
            ItemInstance item = CreateEquipmentInstance("Sword", EquipmentSlot.Weapon, 5f);
            ItemInstance filler = CreateConsumableInstance("Filler");

            Assert.IsTrue(rig.Inventory.Add(item));
            Assert.IsTrue(rig.Input.TryEquipMostRecentInventoryEquipment());
            Assert.IsTrue(rig.Inventory.Add(filler));

            Assert.IsFalse(rig.Input.TryUnequipLastTestEquipment());
            Assert.AreSame(item, rig.Equipment.GetEquippedInstance(EquipmentSlot.Weapon));
            Assert.GreaterOrEqual(rig.Inventory.FindSlotByInstanceId(filler.InstanceId), 0);

            Object.DestroyImmediate(rig.GameObject);
        }

        [Test]
        public void IgnoresNonEquippableItems()
        {
            TestRig rig = CreateRig();
            ItemInstance consumable = CreateConsumableInstance("Potion");

            Assert.IsTrue(rig.Inventory.Add(consumable));

            Assert.IsFalse(rig.Input.TryEquipMostRecentInventoryEquipment());
            Assert.GreaterOrEqual(rig.Inventory.FindSlotByInstanceId(consumable.InstanceId), 0);

            Object.DestroyImmediate(rig.GameObject);
        }

        [Test]
        public void DoesNothingWhenNoValidItemExists()
        {
            TestRig rig = CreateRig();

            Assert.IsFalse(rig.Input.TryEquipMostRecentInventoryEquipment());
            Assert.IsFalse(rig.Input.TryUnequipLastTestEquipment());

            Object.DestroyImmediate(rig.GameObject);
        }

        [Test]
        public void MostRecentEquipmentIsSelectedBeforeOlderEquipment()
        {
            TestRig rig = CreateRig();
            ItemInstance older = CreateEquipmentInstance("Older", EquipmentSlot.Head, 2f);
            ItemInstance newer = CreateEquipmentInstance("Newer", EquipmentSlot.Weapon, 6f);

            Assert.IsTrue(rig.Inventory.Add(older));
            Assert.IsTrue(rig.Inventory.Add(newer));
            Assert.IsTrue(rig.Input.TryEquipMostRecentInventoryEquipment());

            Assert.AreSame(newer, rig.Equipment.GetEquippedInstance(EquipmentSlot.Weapon));
            Assert.GreaterOrEqual(rig.Inventory.FindSlotByInstanceId(older.InstanceId), 0);

            Object.DestroyImmediate(rig.GameObject);
        }

        private static TestRig CreateRig(int slotCount = 30)
        {
            GameObject go = new GameObject("equipment-runtime-test");
            CharacterStatsController stats = go.AddComponent<CharacterStatsController>();
            go.AddComponent<DefenseController>();
            go.AddComponent<CombatController>();
            go.AddComponent<HealthController>();
            go.AddComponent<ResourceController>();
            go.AddComponent<AbilityController>();
            go.AddComponent<MovementController>();
            InventoryController inventory = go.AddComponent<InventoryController>();
            ConfigureInventorySlots(inventory, slotCount);
            EquipmentController equipment = go.AddComponent<EquipmentController>();
            EquipmentRuntimeTestInput input = go.AddComponent<EquipmentRuntimeTestInput>();

            return new TestRig
            {
                GameObject = go,
                Inventory = inventory,
                Equipment = equipment,
                Stats = stats,
                Input = input
            };
        }

        private static ItemInstance CreateEquipmentInstance(
            string name,
            EquipmentSlot slot,
            float statValue)
        {
            EquipmentItemData item = ScriptableObject.CreateInstance<EquipmentItemData>();
            item.name = name;
            SetPrivate(item, "displayName", name, typeof(ItemData));
            SetPrivate(item, "itemType", ItemType.Equipment, typeof(ItemData));
            SetPrivate(item, "slot", slot, typeof(EquipmentItemData));

            ItemRarityData rarity = ScriptableObject.CreateInstance<ItemRarityData>();
            rarity.name = name + " Rarity";
            SetPrivate(rarity, "displayName", name + " Rarity");
            SetPrivate(rarity, "maxPrefixes", 1);
            SetPrivate(rarity, "maxSuffixes", 0);

            ItemAffixData affix = ScriptableObject.CreateInstance<ItemAffixData>();
            affix.name = name + " STR";
            SetPrivate(affix, "affixId", name + "_STR");
            SetPrivate(affix, "displayName", name + " STR");
            SetPrivate(affix, "affixType", ItemAffixType.Prefix);
            SetPrivate(affix, "effectType", ItemAffixEffectType.CharacterStat);
            SetPrivate(affix, "valueMode", ItemAffixValueMode.Flat);
            SetPrivate(affix, "characterStat", CharacterStat.STR);

            ItemInstance instance = new ItemInstance(item, 1, rarity);
            Assert.IsTrue(instance.TryAddAffix(new ItemAffixRoll(affix, 3, statValue)));
            return instance;
        }

        private static ItemInstance CreateConsumableInstance(string name)
        {
            ItemData item = ScriptableObject.CreateInstance<ItemData>();
            item.name = name;
            SetPrivate(item, "displayName", name, typeof(ItemData));
            SetPrivate(item, "itemType", ItemType.Consumable, typeof(ItemData));
            return new ItemInstance(item, 1, null);
        }

        private static void ConfigureInventorySlots(
            InventoryController inventory,
            int slotCount)
        {
            SetPrivate(inventory, "slotCount", slotCount);
            SetPrivate(inventory, "slots", new ItemStack[slotCount]);
        }

        private static void SetPrivate(
            object target,
            string fieldName,
            object value,
            System.Type declaringType = null)
        {
            System.Type type = declaringType ?? target.GetType();
            FieldInfo field = type.GetField(
                fieldName,
                BindingFlags.Instance | BindingFlags.NonPublic);

            Assert.NotNull(field, $"Field '{fieldName}' was not found in {type.Name}.");
            field.SetValue(target, value);
        }
    }
}

#endif
