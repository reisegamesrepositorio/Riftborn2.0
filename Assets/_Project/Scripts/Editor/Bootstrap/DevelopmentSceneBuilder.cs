#if UNITY_EDITOR
using Riftborn.Characters.Abilities;
using Riftborn.Characters.Animation;
using Riftborn.Characters.Combat;
using Riftborn.Characters.Controllers;
using Riftborn.Characters.Equipment;
using Riftborn.Characters.Inventory;
using Riftborn.Characters.Movement;
using Riftborn.Characters.Resources;
using Riftborn.Characters.Runes;
using Riftborn.Characters.StatusEffects;
using Riftborn.Characters.Targeting;
using Riftborn.Enemies.Core;
using Riftborn.Enemies.AI;
using Riftborn.Enemies.Movement;
using Riftborn.Enemies.Respawn;
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
            const string scenePath =
                "Assets/_Project/Scenes/Development/DevelopmentTestScene.unity";

            const string playerPrefabPath =
                "Assets/_Project/Prefabs/Characters/PlayerPrototype.prefab";

            const string enemyPrefabPath =
                "Assets/_Project/Prefabs/Enemies/EnemyDummy.prefab";

            GameObject player =
                CreatePlayer(
                    "PlayerPrototype",
                    new Vector3(-1.5f, 0f, 0f),
                    Color.cyan);

            GameObject enemy =
                CreateEnemy(
                    "EnemyDummy",
                    new Vector3(1.5f, 0f, 0f),
                    Color.red);

            PrefabUtility.SaveAsPrefabAsset(
                player,
                playerPrefabPath);

            PrefabUtility.SaveAsPrefabAsset(
                enemy,
                enemyPrefabPath);

            Scene scene =
                EditorSceneManager.NewScene(
                    NewSceneSetup.EmptyScene,
                    NewSceneMode.Single);

            Object.Instantiate(
                    AssetDatabase.LoadAssetAtPath<GameObject>(
                        playerPrefabPath),
                    new Vector3(-1.5f, 0f, 0f),
                    Quaternion.identity)
                .name = "PlayerPrototype";

            Object.Instantiate(
                    AssetDatabase.LoadAssetAtPath<GameObject>(
                        enemyPrefabPath),
                    new Vector3(1.5f, 0f, 0f),
                    Quaternion.identity)
                .name = "EnemyDummy";

            GameObject lightObject =
                new GameObject(
                    "Directional Light");

            Light light =
                lightObject.AddComponent<Light>();

            light.type =
                LightType.Directional;

            light.transform.rotation =
                Quaternion.Euler(
                    50f,
                    -30f,
                    0f);

            GameObject cameraObject =
                new GameObject(
                    "Iso Camera");

            Camera camera =
                cameraObject.AddComponent<Camera>();

            camera.transform.position =
                new Vector3(
                    0f,
                    7f,
                    -7f);

            camera.transform.rotation =
                Quaternion.Euler(
                    45f,
                    0f,
                    0f);

            camera.orthographic = true;
            camera.orthographicSize = 5f;
            camera.tag = "MainCamera";

            EditorSceneManager.SaveScene(
                scene,
                scenePath);

            Object.DestroyImmediate(player);
            Object.DestroyImmediate(enemy);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        private static GameObject CreatePlayer(
            string name,
            Vector3 position,
            Color color)
        {
            GameObject character =
                CreateVisualCharacter(
                    name,
                    position,
                    color);

            EnsureCharacterController(character);

            character.AddComponent<PlayerController>();
            character.AddComponent<CharacterAnimationController>();

            return character;
        }

        private static GameObject CreateEnemy(
            string name,
            Vector3 position,
            Color color)
        {
            GameObject character =
                CreateVisualCharacter(
                    name,
                    position,
                    color);

            EnsureCharacterController(character);

            character.AddComponent<EnemyController>();

            return character;
        }

        private static GameObject CreateVisualCharacter(
            string name,
            Vector3 position,
            Color color)
        {
            GameObject character =
                GameObject.CreatePrimitive(
                    PrimitiveType.Capsule);

            character.name = name;
            character.transform.position = position;

            character.GetComponent<Renderer>()
                .sharedMaterial =
                    CreateMaterial(
                        $"{name}Material",
                        color);

            return character;
        }

        private static void EnsureCharacterController(
            GameObject character)
        {
            CapsuleCollider capsule =
                character.GetComponent<CapsuleCollider>();

            if (capsule != null)
            {
                Object.DestroyImmediate(capsule);
            }

            character.AddComponent<CharacterController>();
        }

        private static Material CreateMaterial(
            string name,
            Color color)
        {
            string path =
                $"Assets/_Project/Art/Materials/{name}.mat";

            Material material =
                AssetDatabase.LoadAssetAtPath<Material>(
                    path);

            if (material != null)
            {
                return material;
            }

            material =
                new Material(
                    Shader.Find(
                        "Universal Render Pipeline/Lit") ??
                    Shader.Find("Standard"));

            material.color = color;

            AssetDatabase.CreateAsset(
                material,
                path);

            return material;
        }
    }
}
#endif
