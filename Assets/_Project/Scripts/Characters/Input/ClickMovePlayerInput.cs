using Riftborn.Characters.Controllers;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace Riftborn.Characters.Input
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(PlayerController))]
    public sealed class ClickMovePlayerInput : MonoBehaviour
    {
        [Header("References")]
        [SerializeField]
        private PlayerController playerController;

        [SerializeField]
        private Camera worldCamera;

        [Header("Ground Raycast")]
        [SerializeField]
        private LayerMask groundMask = ~0;

        [SerializeField, Min(0.1f)]
        private float maximumRaycastDistance = 500f;

        [Header("Click-Move Ability Mapping")]
        [SerializeField]
        private Key ability1Key = Key.Q;

        [SerializeField]
        private Key ability2Key = Key.W;

        [SerializeField]
        private Key ability3Key = Key.E;

        [SerializeField]
        private Key ability4Key = Key.R;

        [SerializeField]
        private Key ability5Key = Key.F;

        [SerializeField]
        private Key basicAttackKey = Key.A;

        private void Awake()
        {
            ResolveReferences();
        }

        private void OnEnable()
        {
            ResolveReferences();
            playerController?.ActivateClickMoveMode();
        }

        private void OnDisable()
        {
            playerController?.CancelClickDestination();
        }

        private void Update()
        {
            HandleClickDestination();

            Keyboard keyboard =
                Keyboard.current;

            if (keyboard == null)
            {
                return;
            }

            TryUseAbility(
                keyboard,
                ability1Key,
                0);

            TryUseAbility(
                keyboard,
                ability2Key,
                1);

            TryUseAbility(
                keyboard,
                ability3Key,
                2);

            TryUseAbility(
                keyboard,
                ability4Key,
                3);

            TryUseAbility(
                keyboard,
                ability5Key,
                4);

            if (WasPressed(
                    keyboard,
                    basicAttackKey))
            {
                playerController?.RequestBasicAttack();
            }
        }

        private void HandleClickDestination()
        {
            Mouse mouse =
                Mouse.current;

            if (mouse == null ||
                !mouse.rightButton.wasPressedThisFrame)
            {
                return;
            }

            if (EventSystem.current != null &&
                EventSystem.current.IsPointerOverGameObject())
            {
                return;
            }

            Camera cameraToUse =
                worldCamera != null
                    ? worldCamera
                    : Camera.main;

            if (cameraToUse == null)
            {
                Debug.LogWarning(
                    "[INPUT] Nenhuma câmera foi encontrada " +
                    "para o click-to-move.",
                    this);

                return;
            }

            Ray ray =
                cameraToUse.ScreenPointToRay(
                    mouse.position.ReadValue());

            if (!Physics.Raycast(
                    ray,
                    out RaycastHit hit,
                    maximumRaycastDistance,
                    groundMask,
                    QueryTriggerInteraction.Ignore))
            {
                return;
            }

            playerController?.SetClickDestination(
                hit.point);
        }

        private void TryUseAbility(
            Keyboard keyboard,
            Key key,
            int slot)
        {
            if (!WasPressed(
                    keyboard,
                    key))
            {
                return;
            }

            playerController?.RequestAbility(
                slot);
        }

        private static bool WasPressed(
            Keyboard keyboard,
            Key key)
        {
            return keyboard[key]
                .wasPressedThisFrame;
        }

        private void ResolveReferences()
        {
            playerController ??=
                GetComponent<PlayerController>();

            if (worldCamera == null &&
                Camera.main != null)
            {
                worldCamera =
                    Camera.main;
            }
        }

        private void OnValidate()
        {
            maximumRaycastDistance =
                Mathf.Max(
                    0.1f,
                    maximumRaycastDistance);
        }
    }
}
