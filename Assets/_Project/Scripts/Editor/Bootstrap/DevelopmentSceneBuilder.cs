#if UNITY_EDITOR
using Riftborn.Characters.Abilities;
using Riftborn.Characters.ActionStates;
using Riftborn.Characters.Animation;
using Riftborn.Characters.Combat;
using Riftborn.Characters.Core;
using Riftborn.Characters.Equipment;
using Riftborn.Characters.Health;
using Riftborn.Characters.Inventory;
using Riftborn.Characters.Movement;
using Riftborn.Characters.Resources;
using Riftborn.Characters.Runes;
using Riftborn.Characters.Stats;
using Riftborn.Characters.StatusEffects;
using Riftborn.Characters.Targeting;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
namespace Riftborn.Editor.Bootstrap
{
    public static class DevelopmentSceneBuilder
    {
        [MenuItem("Riftborn/Build Development Test Scene")]
        public static void Build()
        {
            const string scenePath = "Assets/_Project/Scenes/Development/DevelopmentTestScene.unity";
            const string playerPrefabPath = "Assets/_Project/Prefabs/Characters/PlayerPrototype.prefab";
            const string enemyPrefabPath = "Assets/_Project/Prefabs/Enemies/EnemyDummy.prefab";
            GameObject player = CreateCharacter("PlayerPrototype", new Vector3(-1.5f, 0f, 0f), Color.cyan);
            GameObject enemy = CreateCharacter("EnemyDummy", new Vector3(1.5f, 0f, 0f), Color.red);
            PrefabUtility.SaveAsPrefabAsset(player, playerPrefabPath); PrefabUtility.SaveAsPrefabAsset(enemy, enemyPrefabPath);
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(playerPrefabPath), new Vector3(-1.5f, 0f, 0f), Quaternion.identity).name = "PlayerPrototype";
            Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(enemyPrefabPath), new Vector3(1.5f, 0f, 0f), Quaternion.identity).name = "EnemyDummy";
            var lightObject = new GameObject("Directional Light"); var light = lightObject.AddComponent<Light>(); light.type = LightType.Directional; light.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
            var cameraObject = new GameObject("Iso Camera"); var camera = cameraObject.AddComponent<Camera>(); camera.transform.position = new Vector3(0f, 7f, -7f); camera.transform.rotation = Quaternion.Euler(45f, 0f, 0f); camera.orthographic = true; camera.orthographicSize = 5f; camera.tag = "MainCamera";
            EditorSceneManager.SaveScene(scene, scenePath); Object.DestroyImmediate(player); Object.DestroyImmediate(enemy); AssetDatabase.SaveAssets(); AssetDatabase.Refresh();
        }
        private static GameObject CreateCharacter(string name, Vector3 position, Color color)
        {
            GameObject character = GameObject.CreatePrimitive(PrimitiveType.Capsule); character.name = name; character.transform.position = position; character.GetComponent<Renderer>().sharedMaterial = CreateMaterial($"{name}Material", color);
            character.AddComponent<CharacterStatsController>(); character.AddComponent<HealthController>(); character.AddComponent<ResourceController>(); character.AddComponent<ActionStateController>(); character.AddComponent<MovementController>(); character.AddComponent<TargetingController>(); character.AddComponent<CombatController>(); character.AddComponent<AbilityController>(); character.AddComponent<StatusEffectController>(); character.AddComponent<InventoryController>(); character.AddComponent<EquipmentController>(); character.AddComponent<RuneController>(); character.AddComponent<CharacterAnimationController>(); character.AddComponent<CharacterEvents>(); character.AddComponent<CharacterContext>(); return character;
        }
        private static Material CreateMaterial(string name, Color color)
        {
            string path = $"Assets/_Project/Art/Materials/{name}.mat"; Material material = AssetDatabase.LoadAssetAtPath<Material>(path); if (material != null) return material;
            material = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard")); material.color = color; AssetDatabase.CreateAsset(material, path); return material;
        }
    }
}
#endif
