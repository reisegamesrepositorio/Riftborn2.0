using System.Text;
using Riftborn.Characters.Defense;
using Riftborn.Characters.Equipment;
using Riftborn.Characters.Inventory;
using Riftborn.Characters.Stats;
using UnityEngine;

namespace Riftborn.Items
{
    public sealed class EquipmentSystemTester : MonoBehaviour
    {
        [Header("Generation")]
        [SerializeField]
        private ItemGenerationProfile profile;

        [Header("Character References")]
        [SerializeField]
        private InventoryController inventory;

        [SerializeField]
        private EquipmentController equipment;

        [SerializeField]
        private CharacterStatsController stats;

        [SerializeField]
        private DefenseController defense;

        [Header("Values To Observe")]
        [SerializeField]
        private CharacterStat observedStat =
            CharacterStat.STR;

        [SerializeField]
        private DefenseType observedDefense =
            DefenseType.Physical;

        [Header("Unequip")]
        [SerializeField]
        private EquipmentSlot slotToUnequip =
            EquipmentSlot.Weapon;

        private void Awake()
        {
            ResolveReferences();
        }

        [ContextMenu("Generate Add And Equip")]
        public void GenerateAddAndEquip()
        {
            if (!ResolveReferences())
            {
                Debug.LogError(
                    "[EQUIPMENT TEST] Referências do personagem " +
                    "não foram encontradas.",
                    this);

                return;
            }

            if (profile == null)
            {
                Debug.LogError(
                    "[EQUIPMENT TEST] Nenhum perfil de geração " +
                    "foi configurado.",
                    this);

                return;
            }

            if (!ItemGenerator.TryGenerate(
                    profile,
                    out ItemInstance generatedInstance))
            {
                Debug.LogError(
                    "[EQUIPMENT TEST] Falha ao gerar o item.",
                    this);

                return;
            }

            EquipmentItemData equipmentData =
                generatedInstance.Item
                    as EquipmentItemData;

            if (equipmentData == null)
            {
                Debug.LogError(
                    $"[EQUIPMENT TEST] O item " +
                    $"'{generatedInstance.DisplayName}' foi criado " +
                    "com ItemData comum. O perfil precisa utilizar " +
                    "um EquipmentItemData.",
                    this);

                return;
            }

            slotToUnequip =
                equipmentData.Slot;

            float statBefore =
                stats.GetFinalValue(
                    observedStat);

            float defenseBefore =
                defense.GetFinalValue(
                    observedDefense);

            bool added =
                inventory.Add(
                    generatedInstance);

            if (!added)
            {
                Debug.LogError(
                    "[EQUIPMENT TEST] O item foi gerado, mas " +
                    "não entrou no inventário.",
                    this);

                return;
            }

            int inventorySlot =
                inventory.FindSlotByInstanceId(
                    generatedInstance.InstanceId);

            if (inventorySlot < 0)
            {
                Debug.LogError(
                    "[EQUIPMENT TEST] O item entrou no inventário, " +
                    "mas seu slot não foi encontrado.",
                    this);

                return;
            }

            ItemInstance storedInstance =
                inventory.GetItemInstance(
                    inventorySlot);

            bool sameStoredInstance =
                ReferenceEquals(
                    generatedInstance,
                    storedInstance);

            ItemInstance takenInstance =
                inventory.TakeAt(
                    inventorySlot);

            if (takenInstance == null)
            {
                Debug.LogError(
                    "[EQUIPMENT TEST] Não foi possível retirar o " +
                    "item do inventário.",
                    this);

                return;
            }

            bool sameTakenInstance =
                ReferenceEquals(
                    generatedInstance,
                    takenInstance);

            bool equippedSuccessfully =
                equipment.Equip(
                    takenInstance);

            if (!equippedSuccessfully)
            {
                inventory.Add(
                    takenInstance);

                Debug.LogError(
                    "[EQUIPMENT TEST] O item não pôde ser equipado " +
                    "e foi devolvido ao inventário.",
                    this);

                return;
            }

            float statAfter =
                stats.GetFinalValue(
                    observedStat);

            float defenseAfter =
                defense.GetFinalValue(
                    observedDefense);

            StringBuilder result =
                new StringBuilder();

            result.AppendLine(
                "[EQUIPMENT TEST] Item equipado");

            result.AppendLine(
                $"Nome: {takenInstance.DisplayName}");

            result.AppendLine(
                $"Instance ID: {takenInstance.InstanceId}");

            result.AppendLine(
                $"Raridade: " +
                $"{takenInstance.Rarity?.DisplayName ?? "Sem raridade"}");

            result.AppendLine(
                $"Slot de equipamento: {equipmentData.Slot}");

            result.AppendLine(
                $"Mesma instância no inventário: " +
                $"{(sameStoredInstance ? "SIM" : "NÃO")}");

            result.AppendLine(
                $"Mesma instância retirada: " +
                $"{(sameTakenInstance ? "SIM" : "NÃO")}");

            result.AppendLine(
                $"{observedStat}: " +
                $"{statBefore:0.##} → {statAfter:0.##}");

            result.AppendLine(
                $"{observedDefense}: " +
                $"{defenseBefore:0.##} → {defenseAfter:0.##}");

            result.AppendLine(
                $"Prefixos aplicáveis: " +
                $"{takenInstance.PrefixCount}");

            result.AppendLine(
                $"Sufixos aplicáveis: " +
                $"{takenInstance.SuffixCount}");

            result.AppendLine(
                $"Slots ocupados no inventário: " +
                $"{inventory.OccupiedSlotCount}/" +
                $"{inventory.SlotCount}");

            Debug.Log(
                result.ToString(),
                this);
        }

        [ContextMenu("Unequip And Return To Inventory")]
        public void UnequipAndReturnToInventory()
        {
            if (!ResolveReferences())
            {
                Debug.LogError(
                    "[EQUIPMENT TEST] Referências do personagem " +
                    "não foram encontradas.",
                    this);

                return;
            }

            ItemInstance equippedInstance =
                equipment.GetEquippedInstance(
                    slotToUnequip);

            if (equippedInstance == null)
            {
                Debug.LogWarning(
                    $"[EQUIPMENT TEST] O slot " +
                    $"'{slotToUnequip}' está vazio.",
                    this);

                return;
            }

            if (!inventory.CanAdd(
                    equippedInstance))
            {
                Debug.LogError(
                    "[EQUIPMENT TEST] O inventário não possui " +
                    "espaço para receber o item desequipado.",
                    this);

                return;
            }

            float statBefore =
                stats.GetFinalValue(
                    observedStat);

            float defenseBefore =
                defense.GetFinalValue(
                    observedDefense);

            bool unequipped =
                equipment.Unequip(
                    slotToUnequip);

            if (!unequipped)
            {
                Debug.LogError(
                    "[EQUIPMENT TEST] Não foi possível desequipar " +
                    "o item.",
                    this);

                return;
            }

            bool returnedToInventory =
                inventory.Add(
                    equippedInstance);

            if (!returnedToInventory)
            {
                Debug.LogError(
                    "[EQUIPMENT TEST] O item foi desequipado, " +
                    "mas não retornou ao inventário.",
                    this);

                return;
            }

            float statAfter =
                stats.GetFinalValue(
                    observedStat);

            float defenseAfter =
                defense.GetFinalValue(
                    observedDefense);

            int inventorySlot =
                inventory.FindSlotByInstanceId(
                    equippedInstance.InstanceId);

            StringBuilder result =
                new StringBuilder();

            result.AppendLine(
                "[EQUIPMENT TEST] Item desequipado");

            result.AppendLine(
                $"Nome: {equippedInstance.DisplayName}");

            result.AppendLine(
                $"Instance ID: {equippedInstance.InstanceId}");

            result.AppendLine(
                $"Retornou ao inventário: SIM");

            result.AppendLine(
                $"Slot do inventário: {inventorySlot}");

            result.AppendLine(
                $"{observedStat}: " +
                $"{statBefore:0.##} → {statAfter:0.##}");

            result.AppendLine(
                $"{observedDefense}: " +
                $"{defenseBefore:0.##} → {defenseAfter:0.##}");

            Debug.Log(
                result.ToString(),
                this);
        }

        [ContextMenu("Show Equipment State")]
        public void ShowEquipmentState()
        {
            if (!ResolveReferences())
            {
                Debug.LogError(
                    "[EQUIPMENT TEST] Referências do personagem " +
                    "não foram encontradas.",
                    this);

                return;
            }

            ItemInstance equippedInstance =
                equipment.GetEquippedInstance(
                    slotToUnequip);

            StringBuilder result =
                new StringBuilder();

            result.AppendLine(
                "[EQUIPMENT TEST] Estado atual");

            result.AppendLine(
                $"Slot observado: {slotToUnequip}");

            result.AppendLine(
                $"Equipado: " +
                $"{equippedInstance?.DisplayName ?? "Nada"}");

            result.AppendLine(
                $"{observedStat}: " +
                $"{stats.GetFinalValue(observedStat):0.##}");

            result.AppendLine(
                $"{observedDefense}: " +
                $"{defense.GetFinalValue(observedDefense):0.##}");

            result.AppendLine(
                $"Inventário: " +
                $"{inventory.OccupiedSlotCount}/" +
                $"{inventory.SlotCount}");

            Debug.Log(
                result.ToString(),
                this);
        }

        private bool ResolveReferences()
        {
            if (inventory == null)
            {
                inventory =
                    FindAnyObjectByType<InventoryController>();
            }

            if (equipment == null)
            {
                equipment =
                    FindAnyObjectByType<EquipmentController>();
            }

            if (stats == null &&
                equipment != null)
            {
                stats =
                    equipment.GetComponent<CharacterStatsController>();
            }

            if (defense == null &&
                equipment != null)
            {
                defense =
                    equipment.GetComponent<DefenseController>();
            }

            return inventory != null &&
                   equipment != null &&
                   stats != null &&
                   defense != null;
        }
    }
}