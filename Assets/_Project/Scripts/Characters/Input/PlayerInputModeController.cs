using System;
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
    [Serializable]
    public sealed class PlayerInputModeController
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

        public void Initialize(PlayerController controller, WasdPlayerInput wasdModule, ClickMovePlayerInput clickMoveModule)
        {
            playerController = controller;
            wasdInput = wasdModule;
            clickMoveInput = clickMoveModule;
            DisableBothInputs();
            SetMode(
                startingMode);
        }

        public void Tick()
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
                    "ou ClickMovePlayerInput não foi encontrado.", playerController);

                return;
            }

            DisableBothInputs();

            currentMode =
                newMode;

            switch (currentMode)
            {
                case PlayerInputMode.Wasd:
                    playerController.ActivateWasdMode();
                    wasdInput.Enable();
                    break;

                case PlayerInputMode.ClickMove:
                    playerController.ActivateClickMoveMode();
                    clickMoveInput.Enable();
                    break;

                default:
                    Debug.LogWarning(
                        $"[INPUT] Modo de input desconhecido: " +
                        $"{currentMode}.", playerController);
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
                wasdInput.Disable();
            }

            if (clickMoveInput != null)
            {
                clickMoveInput.Disable();
            }
        }

        private void ResolveReferences()
        {
        }
    }
}
