using Riftborn.Characters.Controllers;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Riftborn.Characters.Input
{
    public enum PlayerInputMode
    {
        Wasd = 0,
        ClickMove = 1
    }

    [DisallowMultipleComponent]
    [RequireComponent(typeof(PlayerController))]
    public sealed class PlayerInputModeController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField]
        private PlayerController playerController;

        [SerializeField]
        private WasdPlayerInput wasdInput;

        [SerializeField]
        private ClickMovePlayerInput clickMoveInput;

        [Header("Mode")]
        [SerializeField]
        private PlayerInputMode startingMode =
            PlayerInputMode.Wasd;

        [SerializeField]
        private Key switchModeKey = Key.I;

        [Header("Runtime")]
        [SerializeField]
        private PlayerInputMode currentMode;

        public PlayerInputMode CurrentMode =>
            currentMode;

        private void Awake()
        {
            ResolveReferences();
            DisableBothInputs();
        }

        private void Start()
        {
            SetMode(
                startingMode);
        }

        private void Update()
        {
            Keyboard keyboard =
                Keyboard.current;

            if (keyboard == null)
            {
                return;
            }

            if (keyboard[switchModeKey]
                .wasPressedThisFrame)
            {
                ToggleMode();
            }
        }

        public void ToggleMode()
        {
            PlayerInputMode nextMode =
                currentMode == PlayerInputMode.Wasd
                    ? PlayerInputMode.ClickMove
                    : PlayerInputMode.Wasd;

            SetMode(
                nextMode);
        }

        public void SetMode(
            PlayerInputMode newMode)
        {
            ResolveReferences();

            if (wasdInput == null ||
                clickMoveInput == null ||
                playerController == null)
            {
                Debug.LogWarning(
                    "[INPUT] PlayerController, WasdPlayerInput " +
                    "ou ClickMovePlayerInput não foi encontrado.",
                    this);

                return;
            }

            DisableBothInputs();

            currentMode =
                newMode;

            switch (currentMode)
            {
                case PlayerInputMode.Wasd:
                    playerController.ActivateWasdMode();
                    wasdInput.enabled = true;
                    break;

                case PlayerInputMode.ClickMove:
                    playerController.ActivateClickMoveMode();
                    clickMoveInput.enabled = true;
                    break;

                default:
                    Debug.LogWarning(
                        $"[INPUT] Modo de input desconhecido: " +
                        $"{currentMode}.",
                        this);
                    break;
            }
        }

        [ContextMenu("Set Mode: WASD")]
        private void SetWasdMode()
        {
            SetMode(
                PlayerInputMode.Wasd);
        }

        [ContextMenu("Set Mode: Click Move")]
        private void SetClickMoveMode()
        {
            SetMode(
                PlayerInputMode.ClickMove);
        }

        private void DisableBothInputs()
        {
            playerController?.StopMovement();

            if (wasdInput != null)
            {
                wasdInput.enabled = false;
            }

            if (clickMoveInput != null)
            {
                clickMoveInput.enabled = false;
            }
        }

        private void ResolveReferences()
        {
            playerController ??=
                GetComponent<PlayerController>();

            wasdInput ??=
                GetComponent<WasdPlayerInput>();

            clickMoveInput ??=
                GetComponent<ClickMovePlayerInput>();
        }
    }
}
