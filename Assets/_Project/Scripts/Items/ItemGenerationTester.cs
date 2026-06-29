using System.Collections.Generic;
using System.Text;
using Riftborn.Characters.Inventory;
using UnityEngine;

namespace Riftborn.Items
{
    public sealed class ItemGenerationTester : MonoBehaviour
    {
        private const int DiagnosticGenerationCount = 100;

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
                    out ItemInstance itemInstance,
                    out ItemGenerationValidationResult validationResult))
            {
                return;
            }

            StringBuilder log =
                new StringBuilder();

            log.Append(
                BuildItemDescription(
                    itemInstance,
                    "[ITEM GENERATOR] Item gerado"));

            AppendValidationSummary(
                log,
                validationResult);

            Debug.Log(
                log.ToString(),
                this);
        }

        [ContextMenu("Generate 100 Diagnostic Items")]
        public void GenerateDiagnosticItems()
        {
            if (profile == null)
            {
                Debug.LogError(
                    "[ITEM GENERATOR] Nenhum perfil de geracao " +
                    "foi configurado.",
                    this);

                return;
            }

            int generatedCount = 0;
            int duplicateCount = 0;
            StringBuilder result =
                new StringBuilder();

            result.AppendLine(
                "[ITEM GENERATOR] Diagnostico de 100 itens");

            for (int index = 0;
                 index < DiagnosticGenerationCount;
                 index++)
            {
                if (!ItemGenerator.TryGenerate(
                        profile,
                        out ItemInstance itemInstance,
                        out ItemGenerationValidationResult validationResult,
                        index == 0))
                {
                    result.AppendLine(
                        $"#{index + 1}: falha ao gerar item.");

                    AppendValidationSummary(
                        result,
                        validationResult);

                    continue;
                }

                generatedCount++;

                bool hasDuplicate =
                    HasDuplicateAffixes(itemInstance);

                if (hasDuplicate)
                {
                    duplicateCount++;
                    Debug.LogError(
                        BuildItemDescription(
                            itemInstance,
                            "[ITEM GENERATOR] Afixo duplicado detectado"),
                        this);
                }

                result.Append(
                    BuildItemDescription(
                        itemInstance,
                        $"[ITEM GENERATOR] Item #{index + 1}"));

                result.AppendLine(
                    hasDuplicate
                        ? "Duplicidade: SIM"
                        : "Duplicidade: nao");
            }

            ItemGenerationValidationResult finalValidation =
                ItemGenerationProfileValidator.Validate(profile);

            AppendValidationSummary(
                result,
                finalValidation);

            result.AppendLine(
                $"Gerados com sucesso: {generatedCount}/" +
                $"{DiagnosticGenerationCount}");

            result.AppendLine(
                $"Itens com afixo duplicado: {duplicateCount}");

            if (duplicateCount > 0)
            {
                Debug.LogError(
                    result.ToString(),
                    this);
                return;
            }

            Debug.Log(
                result.ToString(),
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
                    out ItemInstance itemInstance,
                    out _))
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
                    "mas nao pode ser adicionado ao inventario.",
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
                "Instance ID gerado: " +
                $"{originalInstanceId}");

            result.AppendLine(
                "Instance ID armazenado: " +
                $"{storedInstance?.InstanceId ?? "NULL"}");

            result.AppendLine(
                "Mesma instancia: " +
                $"{(sameInstance ? "SIM" : "NAO")}");

            result.AppendLine(
                "Raridade: " +
                $"{itemInstance.Rarity?.DisplayName ?? "Sem raridade"}");

            result.AppendLine(
                $"Quantidade: {itemInstance.Quantity}");

            result.AppendLine(
                "Prefixos preservados: " +
                $"{storedInstance?.PrefixCount ?? 0}");

            result.AppendLine(
                "Sufixos preservados: " +
                $"{storedInstance?.SuffixCount ?? 0}");

            result.AppendLine(
                "Slots ocupados: " +
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
                "[INVENTORY] Conteudo atual");

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
                    "Inventario vazio.");
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
                "[INVENTORY] Inventario limpo.",
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
            out ItemInstance itemInstance,
            out ItemGenerationValidationResult validationResult)
        {
            itemInstance = null;
            validationResult = null;

            if (profile == null)
            {
                Debug.LogError(
                    "[ITEM GENERATOR] Nenhum perfil de geracao " +
                    "foi configurado.",
                    this);

                return false;
            }

            if (!ItemGenerator.TryGenerate(
                    profile,
                    out itemInstance,
                    out validationResult,
                    true))
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
                $"Item gerado: {itemInstance.DisplayName}");

            result.AppendLine(
                $"Instance ID: {itemInstance.InstanceId}");

            result.AppendLine(
                "Raridade: " +
                $"{itemInstance.Rarity?.DisplayName ?? "Sem raridade"}");

            result.AppendLine(
                $"Quantidade: {itemInstance.Quantity}");

            AppendAffixRolls(
                result,
                "Prefixos",
                itemInstance.Prefixes);

            AppendAffixRolls(
                result,
                "Sufixos",
                itemInstance.Suffixes);

            return result.ToString();
        }

        private static void AppendAffixRolls(
            StringBuilder result,
            string label,
            IReadOnlyList<ItemAffixRoll> rolls)
        {
            result.AppendLine(
                $"{label}: {rolls.Count}");

            for (int index = 0;
                 index < rolls.Count;
                 index++)
            {
                ItemAffixRoll roll =
                    rolls[index];

                result.AppendLine(
                    $"- {roll.DisplayName} | " +
                    $"{roll.Affix.EffectType} | " +
                    $"Tier {roll.Tier} | " +
                    $"Valor {roll.RolledValue:0.##}");
            }
        }

        private static void AppendValidationSummary(
            StringBuilder result,
            ItemGenerationValidationResult validationResult)
        {
            if (validationResult == null)
            {
                return;
            }

            result.AppendLine(
                "[ITEM GENERATOR] Validacao do perfil");

            result.AppendLine(
                $"Prefixos validos: {validationResult.ValidPrefixes.Count}");

            result.AppendLine(
                $"Sufixos validos: {validationResult.ValidSuffixes.Count}");

            for (int index = 0;
                 index < validationResult.Warnings.Count;
                 index++)
            {
                result.AppendLine(
                    $"WARNING: {validationResult.Warnings[index]}");
            }

            for (int index = 0;
                 index < validationResult.DiscardedEntries.Count;
                 index++)
            {
                ItemAffixPoolDiscard discard =
                    validationResult.DiscardedEntries[index];

                result.AppendLine(
                    $"DESCARTADO {discard.PoolType}[{discard.Index}] " +
                    $"{discard.AffixName}: {discard.Reason}");
            }
        }

        private static bool HasDuplicateAffixes(
            ItemInstance itemInstance)
        {
            HashSet<ItemAffixData> usedAffixes =
                new HashSet<ItemAffixData>();

            for (int index = 0;
                 index < itemInstance.Prefixes.Count;
                 index++)
            {
                ItemAffixData affix =
                    itemInstance.Prefixes[index]?.Affix;

                if (affix != null &&
                    !usedAffixes.Add(affix))
                {
                    return true;
                }
            }

            usedAffixes.Clear();

            for (int index = 0;
                 index < itemInstance.Suffixes.Count;
                 index++)
            {
                ItemAffixData affix =
                    itemInstance.Suffixes[index]?.Affix;

                if (affix != null &&
                    !usedAffixes.Add(affix))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
