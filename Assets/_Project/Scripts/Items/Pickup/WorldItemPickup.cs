using Riftborn.Characters.Input;
using Riftborn.Characters.Inventory;
using UnityEngine;

namespace Riftborn.Items
{
    [RequireComponent(typeof(Collider))]
    public sealed class WorldItemPickup : MonoBehaviour
    {
        [Header("Runtime Item")]
        [SerializeField]
        private ItemInstance itemInstance;

        [Header("Pickup")]
        [Tooltip(
            "Quando marcado, somente personagens com " +
            "PlayerInputReader ativo podem coletar.")]
        [SerializeField]
        private bool requireActivePlayerInput = true;

        [SerializeField]
        private bool destroyAfterPickup = true;

        [Header("Debug")]
        [SerializeField]
        private bool showDebugLogs = true;

        private bool pickupProcessed;

        public ItemInstance ItemInstance =>
            itemInstance;

        public bool HasItem =>
            itemInstance != null &&
            itemInstance.IsValid;

        private void Awake()
        {
            EnsureTriggerCollider();
        }

        private void Reset()
        {
            EnsureTriggerCollider();
        }

        public void Initialize(
            ItemInstance runtimeItemInstance)
        {
            itemInstance =
                runtimeItemInstance;

            pickupProcessed = false;

            if (!HasItem)
            {
                Debug.LogError(
                    "[WORLD ITEM] O pickup foi inicializado " +
                    "sem uma ItemInstance válida.",
                    this);

                return;
            }

            gameObject.name =
                $"WorldItem_{itemInstance.DisplayName}";

            if (showDebugLogs)
            {
                Debug.Log(
                    $"[WORLD ITEM] Item criado no mundo: " +
                    $"{itemInstance.DisplayName} | " +
                    $"ID: {itemInstance.InstanceId}",
                    this);
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            if (pickupProcessed ||
                !HasItem ||
                other == null)
            {
                return;
            }

            TryCollect(other);
        }

        private void TryCollect(Collider collectorCollider)
        {
            InventoryController inventory =
                collectorCollider
                    .GetComponentInParent<InventoryController>();

            if (inventory == null)
            {
                return;
            }

            if (requireActivePlayerInput)
            {
                PlayerInputReader playerInput =
                    collectorCollider
                        .GetComponentInParent<PlayerInputReader>();

                if (playerInput == null ||
                    !playerInput.enabled)
                {
                    return;
                }
            }

            bool added =
                inventory.Add(itemInstance);

            if (!added)
            {
                if (showDebugLogs)
                {
                    Debug.LogWarning(
                        $"[WORLD ITEM] O inventário não possui " +
                        $"espaço para '{itemInstance.DisplayName}'.",
                        inventory);
                }

                return;
            }

            pickupProcessed = true;

            if (showDebugLogs)
            {
                int inventorySlot =
                    inventory.FindSlotByInstanceId(
                        itemInstance.InstanceId);

                Debug.Log(
                    $"[WORLD ITEM] Coletado: " +
                    $"{itemInstance.DisplayName} | " +
                    $"Slot: {inventorySlot} | " +
                    $"ID: {itemInstance.InstanceId}",
                    inventory);
            }

            if (destroyAfterPickup)
            {
                Destroy(gameObject);
            }
            else
            {
                gameObject.SetActive(false);
            }
        }

        private void EnsureTriggerCollider()
        {
            Collider pickupCollider =
                GetComponent<Collider>();

            if (pickupCollider != null)
            {
                pickupCollider.isTrigger = true;
            }
        }
    }
}