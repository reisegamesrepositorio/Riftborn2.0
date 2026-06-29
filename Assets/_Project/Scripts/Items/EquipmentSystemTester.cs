using System.Collections.Generic;
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
        private static readonly CharacterStat[]
            ObservedStats =
            {
                CharacterStat.STR,
                CharacterStat.DEX,
                CharacterStat.WIS,
                CharacterStat.ISP,
                CharacterStat.FORT
            };

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

        [Header("Defense To Observe")]
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

            Dictionary<CharacterStat, float>
                statsBefore =
                    CaptureCurrentStats();

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

            Dictionary<CharacterStat, float>
                statsAfter =
                    CaptureCurrentStats();

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

            result.AppendLine();

            AppendStatComparison(
                result,
                statsBefore,
                statsAfter);

            result.AppendLine(
                $"{observedDefense}: " +
                $"{defenseBefore:0.##} → " +
                $"{defenseAfter:0.##}");

            result.AppendLine();

            AppendAffixes(
                result,
                takenInstance);

            result.AppendLine();

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

            Dictionary<CharacterStat, float>
                statsBefore =
                    CaptureCurrentStats();

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

            Dictionary<CharacterStat, float>
                statsAfter =
                    CaptureCurrentStats();

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
                "Retornou ao inventário: SIM");

            result.AppendLine(
                $"Slot do inventário: {inventorySlot}");

            result.AppendLine();

            AppendStatComparison(
                result,
                statsBefore,
                statsAfter);

            result.AppendLine(
                $"{observedDefense}: " +
                $"{defenseBefore:0.##} → " +
                $"{defenseAfter:0.##}");

            result.AppendLine();

            AppendAffixes(
                result,
                equippedInstance);

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

            result.AppendLine();

            AppendCurrentStats(
                result);

            result.AppendLine(
                $"{observedDefense}: " +
                $"{defense.GetFinalValue(observedDefense):0.##}");

            if (equippedInstance != null)
            {
                result.AppendLine();

                AppendAffixes(
                    result,
                    equippedInstance);
            }

            result.AppendLine();

            result.AppendLine(
                $"Inventário: " +
                $"{inventory.OccupiedSlotCount}/" +
                $"{inventory.SlotCount}");

            Debug.Log(
                result.ToString(),
                this);
        }

        private Dictionary<CharacterStat, float>
            CaptureCurrentStats()
        {
            Dictionary<CharacterStat, float> values =
                new Dictionary<CharacterStat, float>(
                    ObservedStats.Length);

            for (int index = 0;
                 index < ObservedStats.Length;
                 index++)
            {
                CharacterStat stat =
                    ObservedStats[index];

                values[stat] =
                    stats.GetFinalValue(
                        stat);
            }

            return values;
        }

        private static void AppendStatComparison(
            StringBuilder result,
            IReadOnlyDictionary<CharacterStat, float> before,
            IReadOnlyDictionary<CharacterStat, float> after)
        {
            result.AppendLine(
                "Atributos:");

            for (int index = 0;
                 index < ObservedStats.Length;
                 index++)
            {
                CharacterStat stat =
                    ObservedStats[index];

                float beforeValue =
                    before.TryGetValue(
                        stat,
                        out float storedBefore)
                            ? storedBefore
                            : 0f;

                float afterValue =
                    after.TryGetValue(
                        stat,
                        out float storedAfter)
                            ? storedAfter
                            : 0f;

                float difference =
                    afterValue -
                    beforeValue;

                string differenceText =
                    Mathf.Approximately(
                        difference,
                        0f)
                            ? string.Empty
                            : $" ({difference:+0.##;-0.##})";

                result.AppendLine(
                    $"{stat}: " +
                    $"{beforeValue:0.##} → " +
                    $"{afterValue:0.##}" +
                    differenceText);
            }
        }

        private void AppendCurrentStats(
            StringBuilder result)
        {
            result.AppendLine(
                "Atributos atuais:");

            for (int index = 0;
                 index < ObservedStats.Length;
                 index++)
            {
                CharacterStat stat =
                    ObservedStats[index];

                result.AppendLine(
                    $"{stat}: " +
                    $"{stats.GetFinalValue(stat):0.##}");
            }
        }

        private static void AppendAffixes(
            StringBuilder result,
            ItemInstance itemInstance)
        {
            result.AppendLine(
                "Afixos sorteados:");

            if (itemInstance == null)
            {
                result.AppendLine(
                    "Nenhum item.");

                return;
            }

            bool hasAnyAffix = false;

            if (itemInstance.Prefixes != null)
            {
                for (int index = 0;
                     index < itemInstance.Prefixes.Count;
                     index++)
                {
                    ItemAffixRoll roll =
                        itemInstance.Prefixes[index];

                    AppendAffixRoll(
                        result,
                        roll,
                        "Prefixo",
                        index + 1);

                    hasAnyAffix = true;
                }
            }

            if (itemInstance.Suffixes != null)
            {
                for (int index = 0;
                     index < itemInstance.Suffixes.Count;
                     index++)
                {
                    ItemAffixRoll roll =
                        itemInstance.Suffixes[index];

                    AppendAffixRoll(
                        result,
                        roll,
                        "Sufixo",
                        index + 1);

                    hasAnyAffix = true;
                }
            }

            if (!hasAnyAffix)
            {
                result.AppendLine(
                    "Nenhum.");
            }
        }

        private static void AppendAffixRoll(
            StringBuilder result,
            ItemAffixRoll roll,
            string category,
            int position)
        {
            if (roll == null ||
                roll.Affix == null)
            {
                result.AppendLine(
                    $"{category} {position}: inválido");

                return;
            }

            ItemAffixData affix =
                roll.Affix;

            string affectedValue =
                affix.EffectType ==
                ItemAffixEffectType.CharacterStat
                    ? affix.CharacterStat.ToString()
                    : affix.EffectType.ToString();

            string rolledValue =
                FormatAffixValue(
                    roll.RolledValue,
                    affix.ValueMode);

            result.AppendLine(
                $"{category} {position}: " +
                $"{affix.DisplayName}");

            result.AppendLine(
                $"  Efeito: {affectedValue}");

            result.AppendLine(
                $"  Tier: T{roll.Tier}");

            result.AppendLine(
                $"  Valor: {rolledValue}");
        }

        private static string FormatAffixValue(
            float value,
            ItemAffixValueMode valueMode)
        {
            switch (valueMode)
            {
                case ItemAffixValueMode.AdditivePercent:
                case ItemAffixValueMode.MultiplicativePercent:
                    return
                        $"{value * 100f:+0.##;-0.##;0}%";

                case ItemAffixValueMode.Flat:
                default:
                    return
                        $"{value:+0.##;-0.##;0}";
            }
        }

        private bool ResolveReferences()
        {
            if (inventory == null)
            {
                inventory =
                    FindAnyObjectByType<
                        InventoryController>();
            }

            if (equipment == null)
            {
                equipment =
                    FindAnyObjectByType<
                        EquipmentController>();
            }

            if (stats == null &&
                equipment != null)
            {
                stats =
                    equipment.GetComponent<
                        CharacterStatsController>();
            }

            if (defense == null &&
                equipment != null)
            {
                defense =
                    equipment.GetComponent<
                        DefenseController>();
            }

            return inventory != null &&
                   equipment != null &&
                   stats != null &&
                   defense != null;
        }
    }
}