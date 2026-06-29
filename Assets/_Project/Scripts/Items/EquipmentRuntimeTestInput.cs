using System.Collections.Generic;
using Riftborn.Characters.Equipment;
using Riftborn.Characters.Inventory;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Riftborn.Items
{
    public sealed class EquipmentRuntimeTestInput : MonoBehaviour
    {
        [Header("References")]
        [SerializeField]
        private InventoryController inventory;

        [SerializeField]
        private EquipmentController equipment;

        [Header("Debug")]
        [SerializeField]
        private bool showDebugLogs = true;

        private readonly List<string> inventoryHistory = new();

        private EquipmentSlot lastEquippedSlot;
        private bool hasLastEquippedSlot;

        private void Awake()
        {
            ResolveReferences();
            SeedInventoryHistory();
        }

        private void OnEnable()
        {
            ResolveReferences();

            if (inventory != null)
            {
                inventory.SlotChanged +=
                    HandleInventorySlotChanged;
            }
        }

        private void OnDisable()
        {
            if (inventory != null)
            {
                inventory.SlotChanged -=
                    HandleInventorySlotChanged;
            }
        }

        private void Update()
        {
            Keyboard keyboard =
                Keyboard.current;

            if (keyboard == null)
            {
                return;
            }

            if (keyboard.vKey.wasPressedThisFrame)
            {
                TryEquipMostRecentInventoryEquipment();
            }

            if (keyboard.bKey.wasPressedThisFrame)
            {
                TryUnequipLastTestEquipment();
            }
        }

        public bool TryEquipMostRecentInventoryEquipment()
        {
            if (!ResolveReferences())
            {
                LogWarning(
                    "[Equipment Test] Missing InventoryController or EquipmentController.");

                return false;
            }

            if (!TryFindMostRecentEquippableInventoryItem(
                    out int inventorySlot,
                    out ItemInstance itemInstance,
                    out EquipmentItemData equipmentData))
            {
                LogWarning(
                    "[Equipment Test] No valid equippable item found in inventory.");

                return false;
            }

            EquipmentSlot equipmentSlot =
                equipmentData.Slot;

            ItemInstance previousInstance =
                equipment.GetEquippedInstance(
                    equipmentSlot);

            ItemInstance takenInstance =
                inventory.TakeAt(
                    inventorySlot);

            if (!ReferenceEquals(takenInstance, itemInstance))
            {
                if (takenInstance != null)
                {
                    inventory.Add(takenInstance);
                }

                LogWarning(
                    "[Equipment Test] Inventory item changed before equip could complete.");

                return false;
            }

            bool equipped =
                equipment.Equip(
                    takenInstance);

            if (!equipped)
            {
                inventory.Add(takenInstance);

                LogWarning(
                    $"[Equipment Test] Could not equip {takenInstance.DisplayName}; returned to inventory.");

                return false;
            }

            if (previousInstance != null &&
                !ReferenceEquals(previousInstance, takenInstance))
            {
                if (!inventory.Add(previousInstance))
                {
                    RollbackFailedSwap(
                        equipmentSlot,
                        takenInstance,
                        previousInstance);

                    LogWarning(
                        "[Equipment Test] Swap failed because previous item could not return to inventory.");

                    return false;
                }
            }

            lastEquippedSlot =
                equipmentSlot;

            hasLastEquippedSlot =
                true;

            if (showDebugLogs)
            {
                int previousReturnedSlot =
                    previousInstance != null
                        ? inventory.FindSlotByInstanceId(previousInstance.InstanceId)
                        : -1;

                Debug.Log(
                    $"[Equipment Test] Equipped: {takenInstance.DisplayName} | " +
                    $"Instance: {takenInstance.InstanceId} | " +
                    $"Inventory: {inventorySlot} -> Equipment: {equipmentSlot}" +
                    (previousReturnedSlot >= 0
                        ? $" | Previous returned to Inventory: {previousReturnedSlot}"
                        : string.Empty),
                    this);
            }

            return true;
        }

        public bool TryUnequipLastTestEquipment()
        {
            if (!ResolveReferences())
            {
                LogWarning(
                    "[Equipment Test] Missing InventoryController or EquipmentController.");

                return false;
            }

            if (!hasLastEquippedSlot)
            {
                LogWarning(
                    "[Equipment Test] No item was equipped by V yet.");

                return false;
            }

            ItemInstance equippedInstance =
                equipment.GetEquippedInstance(
                    lastEquippedSlot);

            if (equippedInstance == null)
            {
                LogWarning(
                    $"[Equipment Test] Slot {lastEquippedSlot} is already empty.");

                return false;
            }

            if (!inventory.CanAdd(equippedInstance))
            {
                LogWarning(
                    $"[Equipment Test] Inventory is full; cannot unequip {equippedInstance.DisplayName}.");

                return false;
            }

            bool unequipped =
                equipment.Unequip(
                    lastEquippedSlot);

            if (!unequipped)
            {
                LogWarning(
                    $"[Equipment Test] Could not unequip slot {lastEquippedSlot}.");

                return false;
            }

            if (!inventory.Add(equippedInstance))
            {
                equipment.Equip(equippedInstance);

                LogWarning(
                    $"[Equipment Test] Failed to return {equippedInstance.DisplayName}; equipment was restored.");

                return false;
            }

            int inventorySlot =
                inventory.FindSlotByInstanceId(
                    equippedInstance.InstanceId);

            if (showDebugLogs)
            {
                Debug.Log(
                    $"[Equipment Test] Unequipped: {equippedInstance.DisplayName} | " +
                    $"Instance: {equippedInstance.InstanceId} | " +
                    $"Equipment: {lastEquippedSlot} -> Inventory: {inventorySlot}",
                    this);
            }

            return true;
        }

        private void RollbackFailedSwap(
            EquipmentSlot equipmentSlot,
            ItemInstance newInstance,
            ItemInstance previousInstance)
        {
            equipment.Unequip(equipmentSlot);
            equipment.Equip(previousInstance);
            inventory.Add(newInstance);
        }

        private bool TryFindMostRecentEquippableInventoryItem(
            out int inventorySlot,
            out ItemInstance itemInstance,
            out EquipmentItemData equipmentData)
        {
            inventorySlot = -1;
            itemInstance = null;
            equipmentData = null;

            for (int index = inventoryHistory.Count - 1;
                 index >= 0;
                 index--)
            {
                string instanceId =
                    inventoryHistory[index];

                int candidateSlot =
                    inventory.FindSlotByInstanceId(
                        instanceId);

                if (candidateSlot < 0)
                {
                    inventoryHistory.RemoveAt(index);
                    continue;
                }

                ItemInstance candidate =
                    inventory.GetItemInstance(
                        candidateSlot);

                if (candidate == null ||
                    !candidate.IsValid)
                {
                    inventoryHistory.RemoveAt(index);
                    continue;
                }

                EquipmentItemData candidateEquipment =
                    candidate.Item as EquipmentItemData;

                if (candidateEquipment == null)
                {
                    continue;
                }

                if (ReferenceEquals(
                        equipment.GetEquippedInstance(candidateEquipment.Slot),
                        candidate))
                {
                    continue;
                }

                inventorySlot = candidateSlot;
                itemInstance = candidate;
                equipmentData = candidateEquipment;
                return true;
            }

            return false;
        }

        private void HandleInventorySlotChanged(
            int slotIndex,
            ItemInstance itemInstance)
        {
            if (itemInstance == null ||
                !itemInstance.IsValid ||
                string.IsNullOrWhiteSpace(itemInstance.InstanceId))
            {
                return;
            }

            RememberInventoryInstance(
                itemInstance.InstanceId);
        }

        private void SeedInventoryHistory()
        {
            if (inventory == null)
            {
                return;
            }

            for (int index = 0;
                 index < inventory.SlotCount;
                 index++)
            {
                ItemInstance itemInstance =
                    inventory.GetItemInstance(index);

                if (itemInstance == null ||
                    !itemInstance.IsValid)
                {
                    continue;
                }

                RememberInventoryInstance(
                    itemInstance.InstanceId);
            }
        }

        private void RememberInventoryInstance(
            string instanceId)
        {
            inventoryHistory.Remove(instanceId);
            inventoryHistory.Add(instanceId);
        }

        private bool ResolveReferences()
        {
            inventory ??=
                GetComponent<InventoryController>();

            equipment ??=
                GetComponent<EquipmentController>();

            return inventory != null &&
                   equipment != null;
        }

        private void LogWarning(
            string message)
        {
            if (showDebugLogs)
            {
                Debug.LogWarning(
                    message,
                    this);
            }
        }
    }
}
