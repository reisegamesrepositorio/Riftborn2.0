using System.Text;
using Riftborn.Characters.Inventory;
using UnityEngine;

namespace Riftborn.Items
{
    public sealed class ItemGenerationTester : MonoBehaviour
    {
        [Header("Generation")]
        [SerializeField]
        private ItemGenerationProfile profile;

        [Header("Inventory")]
        [SerializeField]
        private InventoryController inventory;

        private void Awake()
        {
            ResolveInventory();
        }

        [ContextMenu("Generate Test Item")]
        public void GenerateTestItem()
        {
            if (!TryGenerateItem(
                    out ItemInstance itemInstance))
            {
                return;
            }

            Debug.Log(
                BuildItemDescription(
                    itemInstance,
                    "[ITEM GENERATOR] Item gerado"),
                this);
        }

        [ContextMenu("Generate And Add To Inventory")]
        public void GenerateAndAddToInventory()
        {
            if (!ResolveInventory())
            {
                Debug.LogError(
                    "[ITEM INVENTORY TEST] Nenhum " +
                    "InventoryController foi encontrado.",
                    this);

                return;
            }

            if (!TryGenerateItem(
                    out ItemInstance itemInstance))
            {
                return;
            }

            string originalInstanceId =
                itemInstance.InstanceId;

            bool added =
                inventory.Add(itemInstance);

            if (!added)
            {
                Debug.LogError(
                    "[ITEM INVENTORY TEST] O item foi gerado, " +
                    "mas não pôde ser adicionado ao inventário.",
                    this);

                return;
            }

            int slotIndex =
                inventory.FindSlotByInstanceId(
                    originalInstanceId);

            ItemInstance storedInstance =
                slotIndex >= 0
                    ? inventory.GetItemInstance(slotIndex)
                    : null;

            bool sameInstance =
                ReferenceEquals(
                    itemInstance,
                    storedInstance);

            StringBuilder result =
                new StringBuilder();

            result.AppendLine(
                "[ITEM INVENTORY TEST] Item adicionado");

            result.AppendLine(
                $"Slot: {slotIndex}");

            result.AppendLine(
                $"Nome: {itemInstance.DisplayName}");

            result.AppendLine(
                $"Instance ID gerado: " +
                $"{originalInstanceId}");

            result.AppendLine(
                $"Instance ID armazenado: " +
                $"{storedInstance?.InstanceId ?? "NULL"}");

            result.AppendLine(
                $"Mesma instância: " +
                $"{(sameInstance ? "SIM" : "NÃO")}");

            result.AppendLine(
                $"Raridade: " +
                $"{itemInstance.Rarity?.DisplayName ?? "Sem raridade"}");

            result.AppendLine(
                $"Quantidade: {itemInstance.Quantity}");

            result.AppendLine(
                $"Prefixos preservados: " +
                $"{storedInstance?.PrefixCount ?? 0}");

            result.AppendLine(
                $"Sufixos preservados: " +
                $"{storedInstance?.SuffixCount ?? 0}");

            result.AppendLine(
                $"Slots ocupados: " +
                $"{inventory.OccupiedSlotCount}/" +
                $"{inventory.SlotCount}");

            Debug.Log(
                result.ToString(),
                this);
        }

        [ContextMenu("Show Inventory Contents")]
        public void ShowInventoryContents()
        {
            if (!ResolveInventory())
            {
                Debug.LogError(
                    "[INVENTORY] Nenhum InventoryController " +
                    "foi encontrado.",
                    this);

                return;
            }

            StringBuilder result =
                new StringBuilder();

            result.AppendLine(
                "[INVENTORY] Conteúdo atual");

            bool foundAnyItem = false;

            for (int index = 0;
                 index < inventory.SlotCount;
                 index++)
            {
                ItemInstance instance =
                    inventory.GetItemInstance(index);

                if (instance == null ||
                    !instance.IsValid)
                {
                    continue;
                }

                foundAnyItem = true;

                result.AppendLine(
                    $"Slot {index}: " +
                    $"{instance.DisplayName} | " +
                    $"Qtd. {instance.Quantity} | " +
                    $"{instance.Rarity?.DisplayName ?? "Sem raridade"} | " +
                    $"ID {instance.InstanceId}");
            }

            if (!foundAnyItem)
            {
                result.AppendLine(
                    "Inventário vazio.");
            }

            Debug.Log(
                result.ToString(),
                this);
        }

        [ContextMenu("Clear Inventory")]
        public void ClearInventory()
        {
            if (!ResolveInventory())
            {
                Debug.LogError(
                    "[INVENTORY] Nenhum InventoryController " +
                    "foi encontrado.",
                    this);

                return;
            }

            inventory.Clear();

            Debug.Log(
                "[INVENTORY] Inventário limpo.",
                this);
        }

        private bool ResolveInventory()
        {
            if (inventory != null)
            {
                return true;
            }

            inventory =
                FindAnyObjectByType<InventoryController>();

            return inventory != null;
        }

        private bool TryGenerateItem(
            out ItemInstance itemInstance)
        {
            itemInstance = null;

            if (profile == null)
            {
                Debug.LogError(
                    "[ITEM GENERATOR] Nenhum perfil de geração " +
                    "foi configurado.",
                    this);

                return false;
            }

            if (!ItemGenerator.TryGenerate(
                    profile,
                    out itemInstance))
            {
                Debug.LogError(
                    "[ITEM GENERATOR] Falha ao gerar item.",
                    this);

                return false;
            }

            return true;
        }

        private static string BuildItemDescription(
            ItemInstance itemInstance,
            string header)
        {
            StringBuilder result =
                new StringBuilder();

            result.AppendLine(header);

            result.AppendLine(
                $"Nome: {itemInstance.DisplayName}");

            result.AppendLine(
                $"Instance ID: {itemInstance.InstanceId}");

            result.AppendLine(
                $"Raridade: " +
                $"{itemInstance.Rarity?.DisplayName ?? "Sem raridade"}");

            result.AppendLine(
                $"Quantidade: {itemInstance.Quantity}");

            result.AppendLine(
                $"Prefixos: {itemInstance.PrefixCount}");

            for (int index = 0;
                 index < itemInstance.Prefixes.Count;
                 index++)
            {
                ItemAffixRoll roll =
                    itemInstance.Prefixes[index];

                result.AppendLine(
                    $"- {roll.DisplayName}: " +
                    $"{roll.RolledValue:0.##} " +
                    $"(T{roll.Tier})");
            }

            result.AppendLine(
                $"Sufixos: {itemInstance.SuffixCount}");

            for (int index = 0;
                 index < itemInstance.Suffixes.Count;
                 index++)
            {
                ItemAffixRoll roll =
                    itemInstance.Suffixes[index];

                result.AppendLine(
                    $"- {roll.DisplayName}: " +
                    $"{roll.RolledValue:0.##} " +
                    $"(T{roll.Tier})");
            }

            return result.ToString();
        }
    }
}