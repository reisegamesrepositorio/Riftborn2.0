#if UNITY_EDITOR

using System.Collections.Generic;
using System.Linq;
using Riftborn.Items;
using UnityEditor;
using UnityEngine;

namespace Riftborn.EditorTools.Items
{
    [CustomEditor(typeof(ItemAffixData))]
    public sealed class ItemAffixDataEditor :
        UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            EditorGUILayout.Space(12f);

            ItemAffixData affix =
                target as ItemAffixData;

            if (affix == null)
            {
                return;
            }

            if (affix.ProgressionProfile == null)
            {
                EditorGUILayout.HelpBox(
                    "Atribua um Tier Progression Profile " +
                    "para gerar automaticamente os tiers.",
                    MessageType.Info);
            }

            using (
                new EditorGUI.DisabledScope(
                    affix.ProgressionProfile == null))
            {
                if (GUILayout.Button(
                        "Generate T1-T10 From Progression",
                        GUILayout.Height(32f)))
                {
                    Undo.RecordObject(
                        affix,
                        "Generate Affix Tiers");

                    bool generated =
                        affix.GenerateTiersFromProgression();

                    if (generated)
                    {
                        EditorUtility.SetDirty(
                            affix);

                        AssetDatabase.SaveAssets();

                        Debug.Log(
                            $"[AFFIX TIERS] Tiers regenerados " +
                            $"para '{affix.name}'.",
                            affix);
                    }
                    else
                    {
                        Debug.LogWarning(
                            $"[AFFIX TIERS] Não foi possível " +
                            $"regenerar os tiers de " +
                            $"'{affix.name}'.",
                            affix);
                    }
                }
            }
        }
    }

    [CustomEditor(
        typeof(ItemAffixTierProgressionData))]
    public sealed class
        ItemAffixTierProgressionDataEditor :
            UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            EditorGUILayout.Space(12f);

            ItemAffixTierProgressionData profile =
                target as
                    ItemAffixTierProgressionData;

            if (profile == null)
            {
                return;
            }

            if (GUILayout.Button(
                    "Regenerate All Affixes Using This Profile",
                    GUILayout.Height(32f)))
            {
                int regenerated =
                    ItemAffixTierEditorUtility
                        .RegenerateAffixesUsingProfile(
                            profile);

                Debug.Log(
                    $"[AFFIX TIERS] {regenerated} afixo(s) " +
                    $"regenerado(s) usando o perfil " +
                    $"'{profile.name}'.",
                    profile);
            }
        }
    }

    public static class ItemAffixTierEditorUtility
    {
        [MenuItem(
            "Tools/Riftborn/Items/" +
            "Regenerate Selected Affix Tiers")]
        private static void
            RegenerateSelectedAffixTiers()
        {
            ItemAffixData[] selectedAffixes =
                Selection.objects
                    .OfType<ItemAffixData>()
                    .ToArray();

            if (selectedAffixes.Length == 0)
            {
                Debug.LogWarning(
                    "[AFFIX TIERS] Nenhum ItemAffixData " +
                    "foi selecionado.");

                return;
            }

            Undo.RecordObjects(
                selectedAffixes,
                "Regenerate Selected Affix Tiers");

            int regenerated = 0;

            for (int index = 0;
                 index < selectedAffixes.Length;
                 index++)
            {
                ItemAffixData affix =
                    selectedAffixes[index];

                if (affix == null ||
                    !affix.GenerateTiersFromProgression())
                {
                    continue;
                }

                EditorUtility.SetDirty(
                    affix);

                regenerated++;
            }

            AssetDatabase.SaveAssets();

            Debug.Log(
                $"[AFFIX TIERS] {regenerated} de " +
                $"{selectedAffixes.Length} afixo(s) " +
                "selecionado(s) foram regenerados.");
        }

        [MenuItem(
            "Tools/Riftborn/Items/" +
            "Regenerate All Affix Tiers")]
        private static void
            RegenerateAllAffixTiers()
        {
            ItemAffixData[] allAffixes =
                FindAllAffixes();

            if (allAffixes.Length == 0)
            {
                Debug.LogWarning(
                    "[AFFIX TIERS] Nenhum ItemAffixData " +
                    "foi encontrado no projeto.");

                return;
            }

            Undo.RecordObjects(
                allAffixes,
                "Regenerate All Affix Tiers");

            int regenerated = 0;

            for (int index = 0;
                 index < allAffixes.Length;
                 index++)
            {
                ItemAffixData affix =
                    allAffixes[index];

                if (affix == null ||
                    !affix.GenerateTiersFromProgression())
                {
                    continue;
                }

                EditorUtility.SetDirty(
                    affix);

                regenerated++;
            }

            AssetDatabase.SaveAssets();

            Debug.Log(
                $"[AFFIX TIERS] {regenerated} de " +
                $"{allAffixes.Length} afixo(s) foram " +
                "regenerados.");
        }

        public static int RegenerateAffixesUsingProfile(
            ItemAffixTierProgressionData profile)
        {
            if (profile == null)
            {
                return 0;
            }

            ItemAffixData[] matchingAffixes =
                FindAllAffixes()
                    .Where(
                        affix =>
                            affix != null &&
                            affix.ProgressionProfile ==
                            profile)
                    .ToArray();

            if (matchingAffixes.Length == 0)
            {
                return 0;
            }

            Undo.RecordObjects(
                matchingAffixes,
                "Regenerate Affixes From Profile");

            int regenerated = 0;

            for (int index = 0;
                 index < matchingAffixes.Length;
                 index++)
            {
                ItemAffixData affix =
                    matchingAffixes[index];

                if (!affix.GenerateTiersFromProgression())
                {
                    continue;
                }

                EditorUtility.SetDirty(
                    affix);

                regenerated++;
            }

            AssetDatabase.SaveAssets();

            return regenerated;
        }

        private static ItemAffixData[] FindAllAffixes()
        {
            string[] guids =
                AssetDatabase.FindAssets(
                    "t:ItemAffixData");

            List<ItemAffixData> affixes =
                new(guids.Length);

            for (int index = 0;
                 index < guids.Length;
                 index++)
            {
                string path =
                    AssetDatabase.GUIDToAssetPath(
                        guids[index]);

                ItemAffixData affix =
                    AssetDatabase.LoadAssetAtPath<
                        ItemAffixData>(
                        path);

                if (affix != null)
                {
                    affixes.Add(
                        affix);
                }
            }

            return affixes.ToArray();
        }
    }
}

#endif