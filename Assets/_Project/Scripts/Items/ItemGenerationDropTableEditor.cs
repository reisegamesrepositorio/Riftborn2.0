#if UNITY_EDITOR

using Riftborn.Items;
using UnityEditor;
using UnityEngine;

namespace Riftborn.EditorTools.Items
{
    [CustomEditor(typeof(ItemGenerationDropTable))]
    public sealed class ItemGenerationDropTableEditor :
        UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            EditorGUILayout.Space(12f);

            ItemGenerationDropTable table =
                target as ItemGenerationDropTable;

            if (table == null)
            {
                return;
            }

            ItemGenerationDropTableValidationResult validation =
                table.ValidateTable();

            EditorGUILayout.LabelField(
                "Loot Table Validation",
                EditorStyles.boldLabel);

            EditorGUILayout.LabelField(
                "Total Entries",
                validation.TotalEntries.ToString());

            EditorGUILayout.LabelField(
                "Valid Entries",
                validation.ValidProfiles.Count.ToString());

            EditorGUILayout.LabelField(
                "Total Valid Weight",
                validation.TotalValidWeight.ToString("0.###"));

            for (int index = 0;
                 index < validation.Warnings.Count;
                 index++)
            {
                EditorGUILayout.HelpBox(
                    validation.Warnings[index],
                    MessageType.Warning);
            }

            EditorGUILayout.Space(8f);

            if (GUILayout.Button(
                    "Validate Loot Table",
                    GUILayout.Height(30f)))
            {
                Debug.Log(
                    validation.BuildDiagnosticText(table),
                    table);
            }
        }
    }
}

#endif
