using System;
using System.Collections.Generic;
using System.Reflection;
using Riftborn.Characters.Controllers;
using Riftborn.Enemies.Core;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Riftborn.EditorTools.Migration
{
    public static class SingleComponentMigrationTool
    {
        private const string PlayerPrefabPath = "Assets/_Project/Prefabs/Characters/PlayerPrototype.prefab";
        private const string EnemyPrefabPath = "Assets/_Project/Prefabs/Enemies/EnemyDummy.prefab";
        private const string DevelopmentScenePath = "Assets/_Project/Scenes/Development/DevelopmentTestScene.unity";

        private static readonly HashSet<Type> AllowedRootScripts = new()
        {
            typeof(PlayerController),
            typeof(EnemyController)
        };

        [MenuItem("Tools/Riftborn/Migration/Convert Player and Enemies to Single Component")]
        public static void ConvertPlayerAndEnemiesToSingleComponent()
        {
            MigrationReport report = new();

            MigratePrefab(PlayerPrefabPath, typeof(PlayerController), report);
            MigratePrefab(EnemyPrefabPath, typeof(EnemyController), report);
            MigrateDevelopmentScene(report);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log(report.BuildSummary());
        }

        private static void MigratePrefab(string prefabPath, Type requiredControllerType, MigrationReport report)
        {
            GameObject root = PrefabUtility.LoadPrefabContents(prefabPath);
            if (root == null)
            {
                report.Errors.Add("Prefab not found: " + prefabPath);
                return;
            }

            try
            {
                Component controller = EnsureController(root, requiredControllerType, report, prefabPath);
                RebuildInlineModules(controller, report, prefabPath);
                RemoveLegacyRootScripts(root, report, prefabPath);
                RemoveMissingScripts(root, report, prefabPath);
                PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
                report.AssetsMigrated.Add(prefabPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        private static void MigrateDevelopmentScene(MigrationReport report)
        {
            Scene scene = EditorSceneManager.OpenScene(DevelopmentScenePath, OpenSceneMode.Single);

            MigrateSceneObject("PlayerPrototype", typeof(PlayerController), report);
            MigrateSceneObject("EnemyDummy", typeof(EnemyController), report);

            if (scene.IsValid())
            {
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
                report.AssetsMigrated.Add(DevelopmentScenePath);
            }
        }

        private static void MigrateSceneObject(string objectName, Type requiredControllerType, MigrationReport report)
        {
            GameObject root = GameObject.Find(objectName);
            if (root == null)
            {
                report.Warnings.Add(objectName + " was not found in DevelopmentTestScene.");
                return;
            }

            Component controller = EnsureController(root, requiredControllerType, report, DevelopmentScenePath);
            RebuildInlineModules(controller, report, DevelopmentScenePath);
            RemoveLegacyRootScripts(root, report, DevelopmentScenePath);
            RemoveMissingScripts(root, report, DevelopmentScenePath);
        }

        private static Component EnsureController(GameObject root, Type requiredControllerType, MigrationReport report, string assetPath)
        {
            Component controller = root.GetComponent(requiredControllerType);
            if (controller != null)
            {
                return controller;
            }

            controller = root.AddComponent(requiredControllerType);
            report.ComponentsAdded.Add(assetPath + ": " + requiredControllerType.Name);
            return controller;
        }

        private static void RebuildInlineModules(Component controller, MigrationReport report, string assetPath)
        {
            if (controller == null)
            {
                return;
            }

            FieldInfo[] fields = controller.GetType().GetFields(
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

            for (int index = 0; index < fields.Length; index++)
            {
                FieldInfo field = fields[index];
                Type fieldType = field.FieldType;

                if (fieldType.IsAbstract ||
                    typeof(UnityEngine.Object).IsAssignableFrom(fieldType) ||
                    !fieldType.IsSerializable)
                {
                    continue;
                }

                try
                {
                    field.SetValue(controller, Activator.CreateInstance(fieldType));
                    report.Warnings.Add(assetPath + ": rebuilt inline module " + field.Name + " (" + fieldType.Name + ") with default values.");
                }
                catch (Exception exception)
                {
                    report.Errors.Add(assetPath + ": failed to rebuild " + field.Name + ": " + exception.Message);
                }
            }

            EditorUtility.SetDirty(controller);
        }

        private static void RemoveLegacyRootScripts(GameObject root, MigrationReport report, string assetPath)
        {
            MonoBehaviour[] behaviours = root.GetComponents<MonoBehaviour>();
            for (int index = behaviours.Length - 1; index >= 0; index--)
            {
                MonoBehaviour behaviour = behaviours[index];
                if (behaviour == null)
                {
                    continue;
                }

                Type behaviourType = behaviour.GetType();
                if (AllowedRootScripts.Contains(behaviourType))
                {
                    continue;
                }

                report.ComponentsRemoved.Add(assetPath + ": " + behaviourType.Name);
                UnityEngine.Object.DestroyImmediate(behaviour, true);
            }
        }

        private static void RemoveMissingScripts(GameObject root, MigrationReport report, string assetPath)
        {
            int removed = GameObjectUtility.RemoveMonoBehavioursWithMissingScript(root);
            if (removed > 0)
            {
                report.ComponentsRemoved.Add(assetPath + ": " + removed + " missing script(s)");
            }
        }

        private sealed class MigrationReport
        {
            public readonly List<string> AssetsMigrated = new();
            public readonly List<string> ComponentsAdded = new();
            public readonly List<string> ComponentsRemoved = new();
            public readonly List<string> Warnings = new();
            public readonly List<string> Errors = new();

            public string BuildSummary()
            {
                return "[RIFTBORN MIGRATION] Single component migration finished.\n" +
                       BuildSection("Assets migrated", AssetsMigrated) +
                       BuildSection("Components added", ComponentsAdded) +
                       BuildSection("Components removed", ComponentsRemoved) +
                       BuildSection("Warnings", Warnings) +
                       BuildSection("Errors", Errors);
            }

            private static string BuildSection(string title, List<string> values)
            {
                if (values.Count == 0)
                {
                    return title + ": none\n";
                }

                return title + ":\n- " + string.Join("\n- ", values) + "\n";
            }
        }
    }
}