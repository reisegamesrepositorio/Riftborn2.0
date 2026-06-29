using System.Text;
using UnityEngine;

namespace Riftborn.Items
{
    public sealed class ItemGenerationTester :
        MonoBehaviour
    {
        [SerializeField]
        private ItemGenerationProfile profile;

        [ContextMenu("Generate Test Item")]
        public void GenerateTestItem()
        {
            if (!ItemGenerator.TryGenerate(
                    profile,
                    out ItemInstance itemInstance))
            {
                Debug.LogError(
                    "[ITEM GENERATOR] Falha ao gerar item.",
                    this);

                return;
            }

            StringBuilder result =
                new StringBuilder();

            result.AppendLine(
                "[ITEM GENERATOR] Item gerado");

            result.AppendLine(
                $"Nome: {itemInstance.DisplayName}");

            result.AppendLine(
                $"Instance ID: {itemInstance.InstanceId}");

            result.AppendLine(
                $"Raridade: " +
                $"{itemInstance.Rarity.DisplayName}");

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

            Debug.Log(
                result.ToString(),
                this);
        }
    }
}