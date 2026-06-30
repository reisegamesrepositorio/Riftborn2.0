#if UNITY_EDITOR
using Riftborn.Characters.Controllers;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace Riftborn.Editor.Bootstrap
{
    public static class MovementPrototypeConfigurator
    {
        private const string InputActionsPath = "Assets/_Project/Settings/Input/RiftbornInputActions.inputactions";
        private const string PlayerPrefabPath = "Assets/_Project/Prefabs/Characters/PlayerPrototype.prefab";
        private const string DevelopmentScenePath = "Assets/_Project/Scenes/Development/DevelopmentTestScene.unity";
        private const string GroundMaterialPath = "Assets/_Project/Art/Materials/Ground/MAT_Ground.mat";

        [MenuItem("Riftborn/Configure Prototype Movement")]
        public static void Configure()
        {
            InputActionAsset inputActions = AssetDatabase.LoadAssetAtPath<InputActionAsset>(InputActionsPath);
            ConfigurePlayerPrefab(inputActions);
            ConfigureDevelopmentScene(inputActions);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        private static void ConfigurePlayerPrefab(InputActionAsset inputActions)
        {
            GameObject prefabRoot = PrefabUtility.LoadPrefabContents(PlayerPrefabPath);
            ConfigurePlayer(prefabRoot, inputActions, null);
            PrefabUtility.SaveAsPrefabAsset(prefabRoot, PlayerPrefabPath);
            PrefabUtility.UnloadPrefabContents(prefabRoot);
        }

        private static void ConfigureDevelopmentScene(InputActionAsset inputActions)
        {
            Scene scene = EditorSceneManager.OpenScene(DevelopmentScenePath, OpenSceneMode.Single);
            Camera camera = Camera.main ?? Object.FindAnyObjectByType<Camera>();
            GameObject player = GameObject.Find("PlayerPrototype");
            if (player != null)
            {
                ConfigurePlayer(player, inputActions, camera != null ? camera.transform : null);
            }

            EnsureGround();
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }

        private static void ConfigurePlayer(GameObject player, InputActionAsset inputActions, Transform cameraTransform)
        {
            RemoveCollider<CapsuleCollider>(player);

            CharacterController characterController = player.GetComponent<CharacterController>();
            if (characterController == null)
            {
                characterController = player.AddComponent<CharacterController>();
            }

            characterController.height = 2f;
            characterController.radius = 0.45f;
            characterController.center = new Vector3(0f, 1f, 0f);
            characterController.skinWidth = 0.08f;
            characterController.minMoveDistance = 0f;

            PlayerController controller = player.GetComponent<PlayerController>();
            if (controller == null)
            {
                controller = player.AddComponent<PlayerController>();
            }

            EditorUtility.SetDirty(player);
        }

        private static void EnsureGround()
        {
            GameObject ground = GameObject.Find("Prototype Ground");
            if (ground == null)
            {
                ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
                ground.name = "Prototype Ground";
            }

            ground.transform.position = Vector3.zero;
            ground.transform.rotation = Quaternion.identity;
            ground.transform.localScale = new Vector3(2.5f, 1f, 2.5f);
            if (ground.GetComponent<Collider>() == null)
            {
                ground.AddComponent<MeshCollider>();
            }

            Material material = AssetDatabase.LoadAssetAtPath<Material>(GroundMaterialPath);
            if (material != null && ground.TryGetComponent(out Renderer renderer))
            {
                renderer.sharedMaterial = material;
            }
        }

        private static void RemoveCollider<T>(GameObject target) where T : Collider
        {
            T collider = target.GetComponent<T>();
            if (collider != null)
            {
                Object.DestroyImmediate(collider, true);
            }
        }
    }
}
#endif
