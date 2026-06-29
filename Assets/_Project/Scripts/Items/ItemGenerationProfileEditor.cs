#if UNITY_EDITOR

using System.Collections.Generic;
using System.Text;
using Riftborn.Items;
using UnityEditor;
using UnityEngine;

namespace Riftborn.EditorTools.Items
{
    [CustomEditor(typeof(ItemGenerationProfile))]
    public sealed class ItemGenerationProfileEditor :
        UnityEditor.Editor
    {
        private const int DiagnosticGenerationCount = 100;

        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            EditorGUILayout.Space(12f);

            ItemGenerationProfile profile =
                target as ItemGenerationProfile;

            if (profile == null)
            {
                return;
            }

            ItemGenerationValidationResult validationResult =
                ItemGenerationProfileValidator.Validate(profile);

            DrawValidationSummary(validationResult);

            EditorGUILayout.Space(8f);

            if (GUILayout.Button(
                    "Validate Generation Profile",
                    GUILayout.Height(30f)))
            {
                Debug.Log(
                    validationResult.BuildDiagnosticText(profile),
                    profile);
            }

            if (GUILayout.Button(
                    "Generate 100 Diagnostic Items",
                    GUILayout.Height(30f)))
            {
                RunDiagnosticGeneration(profile);
            }
        }

        private static void DrawValidationSummary(
            ItemGenerationValidationResult validationResult)
        {
            EditorGUILayout.LabelField(
                "Generation Validation",
                EditorStyles.boldLabel);

            EditorGUILayout.LabelField(
                "Valid Prefixes",
                validationResult.ValidPrefixes.Count.ToString());

            EditorGUILayout.LabelField(
                "Valid Suffixes",
                validationResult.ValidSuffixes.Count.ToString());

            for (int index = 0;
                 index < validationResult.Warnings.Count;
                 index++)
            {
                EditorGUILayout.HelpBox(
                    validationResult.Warnings[index],
                    MessageType.Warning);
            }

            for (int index = 0;
                 index < validationResult.DiscardedEntries.Count;
                 index++)
            {
                ItemAffixPoolDiscard discard =
                    validationResult.DiscardedEntries[index];

                EditorGUILayout.HelpBox(
                    $"Discarded {discard.PoolType}[{discard.Index}] " +
                    $"{discard.AffixName}: {discard.Reason}",
                    MessageType.Warning);
            }
        }

        private static void RunDiagnosticGeneration(
            ItemGenerationProfile profile)
        {
            int generatedCount = 0;
            int duplicateCount = 0;
            StringBuilder result =
                new StringBuilder();

            result.AppendLine(
                $"[ITEM GENERATOR] Editor diagnostic for '{profile.name}'");

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
                        $"#{index + 1}: generation failed.");

                    result.AppendLine(
                        validationResult.BuildDiagnosticText(profile));

                    continue;
                }

                generatedCount++;

                bool hasDuplicate =
                    HasDuplicateAffixes(itemInstance);

                if (hasDuplicate)
                {
                    duplicateCount++;
                }

                AppendGeneratedItem(
                    result,
                    index + 1,
                    itemInstance,
                    hasDuplicate);
            }

            ItemGenerationValidationResult finalValidation =
                ItemGenerationProfileValidator.Validate(profile);

            result.AppendLine(
                finalValidation.BuildDiagnosticText(profile));

            result.AppendLine(
                $"Generated: {generatedCount}/" +
                $"{DiagnosticGenerationCount}");

            result.AppendLine(
                $"Items with duplicate affixes: {duplicateCount}");

            if (duplicateCount > 0)
            {
                Debug.LogError(
                    result.ToString(),
                    profile);
                return;
            }

            Debug.Log(
                result.ToString(),
                profile);
        }

        private static void AppendGeneratedItem(
            StringBuilder result,
            int itemIndex,
            ItemInstance itemInstance,
            bool hasDuplicate)
        {
            result.AppendLine(
                $"#{itemIndex}: {itemInstance.DisplayName} | " +
                $"{itemInstance.Rarity?.DisplayName ?? "No rarity"} | " +
                $"Duplicate: {(hasDuplicate ? "YES" : "no")}");

            AppendAffixRolls(
                result,
                "Prefixes",
                itemInstance.Prefixes);

            AppendAffixRolls(
                result,
                "Suffixes",
                itemInstance.Suffixes);
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
                    $"Value {roll.RolledValue:0.##}");
            }
        }

        private static bool HasDuplicateAffixes(
            ItemInstance itemInstance)
        {
            HashSet<ItemAffixData> usedPrefixes =
                new HashSet<ItemAffixData>();

            for (int index = 0;
                 index < itemInstance.Prefixes.Count;
                 index++)
            {
                ItemAffixData affix =
                    itemInstance.Prefixes[index]?.Affix;

                if (affix != null &&
                    !usedPrefixes.Add(affix))
                {
                    return true;
                }
            }

            HashSet<ItemAffixData> usedSuffixes =
                new HashSet<ItemAffixData>();

            for (int index = 0;
                 index < itemInstance.Suffixes.Count;
                 index++)
            {
                ItemAffixData affix =
                    itemInstance.Suffixes[index]?.Affix;

                if (affix != null &&
                    !usedSuffixes.Add(affix))
                {
                    return true;
                }
            }

            return false;
        }
    }
}

#endif
