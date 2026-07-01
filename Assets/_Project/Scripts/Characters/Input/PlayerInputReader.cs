using System;
using Riftborn.Characters.Controllers;
using Riftborn.Characters.Core;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace Riftborn.Characters.Input
{
    [Serializable]
    public sealed class PlayerInputReader
    {
        [Header("Input Action")]
        [SerializeField]
        private InputActionAsset inputActions;

        [SerializeField]
        private string actionMapName = "Player";

        [SerializeField]
        private string selectTargetActionName =
            "SelectTarget";

        [Header("Target Selection")]
        [SerializeField]
        private Camera selectionCamera;

        [SerializeField]
        private LayerMask selectableLayers = ~0;

        [SerializeField, Min(1f)]
        private float selectionDistance = 500f;

        [Tooltip(
            "Quando desmarcado, clicar no chão, no próprio player " +
            "ou em um objeto inválido mantém o alvo atual.")]
        [SerializeField]
        private bool clearTargetWhenClickingEmptySpace;

        [Header("References")]
        [NonSerialized]
        private PlayerController playerController;

        [NonSerialized]
        private CharacterContext selfContext;

        [NonSerialized]
        private Transform selfTransform;

        [Header("Debug")]
        [SerializeField]
        private bool showDebugLogs = true;

        private InputAction selectTargetAction;
        private bool ownsSelectTargetAction;

        public void Initialize(PlayerController controller, CharacterContext self)
        {
            playerController = controller;
            selfContext = self;
            selfTransform = self != null ? self.transform : null;
            CacheReferences();
            ResolveAction();
            selectTargetAction?.Enable();
        }

        public void Disable()
        {
            selectTargetAction?.Disable();
        }

        public void Dispose()
        {
            if (ownsSelectTargetAction)
            {
                selectTargetAction?.Dispose();
            }
        }

        public void Validate()
        {
            selectionDistance =
                Mathf.Max(
                    1f,
                    selectionDistance);
        }

        public void Tick()
        {
            ReadTargetSelection();
        }

        private void ReadTargetSelection()
        {
            bool selectionPressed =
                selectTargetAction != null &&
                selectTargetAction.WasPressedThisFrame();

            if (Mouse.current != null &&
                Mouse.current.leftButton.wasPressedThisFrame)
            {
                selectionPressed = true;
            }

            if (!selectionPressed)
            {
                return;
            }

            if (EventSystem.current != null &&
                EventSystem.current.IsPointerOverGameObject())
            {
                return;
            }

            CacheReferences();

            Camera cameraToUse =
                selectionCamera != null
                    ? selectionCamera
                    : Camera.main;

            if (cameraToUse == null ||
                Mouse.current == null ||
                playerController == null)
            {
                Debug.LogWarning(
                    "[TARGETING INPUT] Não foi possível processar " +
                    "a seleção. Verifique a câmera e o " +
                    "PlayerController.", playerController);

                return;
            }

            Vector2 mousePosition =
                Mouse.current.position.ReadValue();

            Ray ray =
                cameraToUse.ScreenPointToRay(
                    mousePosition);

            RaycastHit[] hits =
                Physics.RaycastAll(
                    ray,
                    selectionDistance,
                    selectableLayers,
                    QueryTriggerInteraction.Collide);

            Array.Sort(
                hits,
                CompareHitsByDistance);

            for (int index = 0;
                 index < hits.Length;
                 index++)
            {
                RaycastHit hit =
                    hits[index];

                if (hit.collider == null)
                {
                    continue;
                }

                CharacterContext clickedCharacter =
                    hit.collider.GetComponentInParent<
                        CharacterContext>();

                if (clickedCharacter == null ||
                    IsSelf(clickedCharacter) ||
                    !playerController.IsValidTarget(
                        clickedCharacter))
                {
                    continue;
                }

                bool targetSelected =
                    playerController.TrySelectTarget(
                        clickedCharacter);

                bool isAlreadySelected =
                    ReferenceEquals(
                        playerController.CurrentTarget,
                        clickedCharacter);

                if (!targetSelected &&
                    !isAlreadySelected)
                {
                    continue;
                }

                if (showDebugLogs)
                {
                    Debug.Log(
                        $"[TARGETING INPUT] Alvo selecionado: " +
                        $"{clickedCharacter.name}",
                        clickedCharacter);
                }

                return;
            }

            HandleClickWithoutValidTarget(
                hits);
        }

        private void HandleClickWithoutValidTarget(
            RaycastHit[] hits)
        {
            if (clearTargetWhenClickingEmptySpace)
            {
                playerController?.ClearTarget();

                if (showDebugLogs)
                {
                    Debug.Log(
                        "[TARGETING INPUT] Nenhum alvo válido " +
                        "encontrado. Seleção removida.", playerController);
                }

                return;
            }

            if (!showDebugLogs)
            {
                return;
            }

            string hitName =
                GetFirstHitName(
                    hits);

            CharacterContext currentTarget =
                playerController?.CurrentTarget;

            Debug.Log(
                $"[TARGETING INPUT] Nenhum novo alvo válido " +
                $"encontrado{hitName}. Alvo atual mantido: " +
                $"{(currentTarget != null ? currentTarget.name : "Nenhum")}.", playerController);
        }

        private bool IsSelf(
            CharacterContext candidate)
        {
            if (candidate == null)
            {
                return false;
            }

            if (selfContext != null &&
                ReferenceEquals(
                    candidate,
                    selfContext))
            {
                return true;
            }

            return candidate.transform == selfTransform ||
                   candidate.transform.IsChildOf(selfTransform) ||
                   selfTransform != null && selfTransform.IsChildOf(candidate.transform);
        }

        private static int CompareHitsByDistance(
            RaycastHit first,
            RaycastHit second)
        {
            return first.distance.CompareTo(
                second.distance);
        }

        private static string GetFirstHitName(
            RaycastHit[] hits)
        {
            if (hits == null ||
                hits.Length == 0 ||
                hits[0].collider == null)
            {
                return string.Empty;
            }

            return
                $" ao clicar em '{hits[0].collider.name}'";
        }

        private void ResolveAction()
        {
            if (selectTargetAction != null)
            {
                return;
            }

            InputActionMap actionMap =
                inputActions != null
                    ? inputActions.FindActionMap(
                        actionMapName,
                        throwIfNotFound: false)
                    : null;

            selectTargetAction =
                actionMap?.FindAction(
                    selectTargetActionName,
                    throwIfNotFound: false);

            if (selectTargetAction != null)
            {
                ownsSelectTargetAction = false;
                return;
            }

            selectTargetAction =
                CreateFallbackSelectTargetAction();

            ownsSelectTargetAction = true;
        }

        private void CacheReferences()
        {
            if (selectionCamera == null)
            {
                selectionCamera =
                    Camera.main;
            }
        }

        private static InputAction
            CreateFallbackSelectTargetAction()
        {
            InputAction action =
                new InputAction(
                    name: "SelectTarget",
                    type: InputActionType.Button);

            action.AddBinding(
                "<Mouse>/leftButton");

            return action;
        }
    }
}
