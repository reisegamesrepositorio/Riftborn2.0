using Riftborn.Characters.Abilities;
using Riftborn.Characters.Combat;
using Riftborn.Characters.Core;
using Riftborn.Characters.Movement;
using Riftborn.Characters.Targeting;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace Riftborn.Characters.Input
{
    public sealed class PlayerInputReader : MonoBehaviour
    {
        [Header("Input Actions")]
        [SerializeField]
        private InputActionAsset inputActions;

        [SerializeField]
        private string actionMapName = "Player";

        [SerializeField]
        private string moveActionName = "Move";

        [SerializeField]
        private string selectTargetActionName = "SelectTarget";

        [SerializeField]
        private string basicAttackActionName = "BasicAttack";

        [SerializeField]
        private string ability1ActionName = "Ability1";

        [Header("Target Selection")]
        [SerializeField]
        private Camera selectionCamera;

        [SerializeField]
        private LayerMask selectableLayers = ~0;

        [SerializeField, Min(1f)]
        private float selectionDistance = 500f;

        [Header("References")]
        [SerializeField]
        private MovementController movementController;

        [SerializeField]
        private CombatController combatController;

        [SerializeField]
        private AbilityController abilityController;

        [SerializeField]
        private TargetingController targetingController;

        private InputAction moveAction;
        private InputAction selectTargetAction;
        private InputAction basicAttackAction;
        private InputAction ability1Action;

        private bool ownsMoveAction;
        private bool ownsSelectTargetAction;
        private bool ownsBasicAttackAction;
        private bool ownsAbility1Action;

        private void Awake()
        {
            CacheReferences();
            ResolveActions();
        }

        private void OnEnable()
        {
            CacheReferences();
            ResolveActions();

            moveAction?.Enable();
            selectTargetAction?.Enable();
            basicAttackAction?.Enable();
            ability1Action?.Enable();
        }

        private void OnDisable()
        {
            moveAction?.Disable();
            selectTargetAction?.Disable();
            basicAttackAction?.Disable();
            ability1Action?.Disable();

            movementController?.SetMoveInput(
                Vector2.zero);
        }

        private void OnDestroy()
        {
            if (ownsMoveAction)
            {
                moveAction?.Dispose();
            }

            if (ownsSelectTargetAction)
            {
                selectTargetAction?.Dispose();
            }

            if (ownsBasicAttackAction)
            {
                basicAttackAction?.Dispose();
            }

            if (ownsAbility1Action)
            {
                ability1Action?.Dispose();
            }
        }

        private void Update()
        {
            ReadMovement();

            /*
             * A seleção acontece antes dos ataques e habilidades,
             * garantindo que ambos utilizem o alvo mais recente.
             */
            ReadTargetSelection();
            ReadBasicAttack();
            ReadAbilities();
        }

        private void ReadMovement()
        {
            Vector2 input =
                moveAction != null
                    ? moveAction.ReadValue<Vector2>()
                    : Vector2.zero;

            movementController?.SetMoveInput(input);
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

            Camera cameraToUse =
                selectionCamera != null
                    ? selectionCamera
                    : Camera.main;

            if (cameraToUse == null ||
                Mouse.current == null ||
                targetingController == null)
            {
                Debug.LogWarning(
                    "[TARGETING] Não foi possível processar o clique. " +
                    "Verifique Camera e TargetingController.",
                    this);

                return;
            }

            Vector2 mousePosition =
                Mouse.current.position.ReadValue();

            Ray ray =
                cameraToUse.ScreenPointToRay(
                    mousePosition);

            bool hitSomething =
                Physics.Raycast(
                    ray,
                    out RaycastHit hit,
                    selectionDistance,
                    selectableLayers,
                    QueryTriggerInteraction.Ignore);

            if (!hitSomething)
            {
                targetingController.ClearTarget();

                Debug.Log(
                    "[TARGETING] Alvo removido.",
                    this);

                return;
            }

            CharacterContext clickedCharacter =
                hit.collider
                    .GetComponentInParent<CharacterContext>();

            if (clickedCharacter == null)
            {
                targetingController.ClearTarget();

                Debug.Log(
                    $"[TARGETING] Clique em '{hit.collider.name}', " +
                    "mas o objeto não possui CharacterContext.",
                    hit.collider);

                return;
            }

            bool targetSelected =
                targetingController.SetTarget(
                    clickedCharacter);

            if (targetSelected)
            {
                Debug.Log(
                    $"[TARGETING] Alvo selecionado: " +
                    $"{clickedCharacter.name}",
                    clickedCharacter);

                return;
            }

            if (!targetingController.IsValidTarget(
                    clickedCharacter))
            {
                targetingController.ClearTarget();

                Debug.Log(
                    $"[TARGETING] '{clickedCharacter.name}' " +
                    "não é um alvo válido.",
                    clickedCharacter);
            }
        }

        private void ReadBasicAttack()
        {
            bool attackPressed =
                basicAttackAction != null &&
                basicAttackAction.WasPressedThisFrame();

            /*
             * Fallback provisório:
             * F executa o ataque básico mesmo que a action
             * BasicAttack ainda não exista no InputActionAsset.
             */
            if (Keyboard.current != null &&
                Keyboard.current.fKey.wasPressedThisFrame)
            {
                attackPressed = true;
            }

            if (!attackPressed)
            {
                return;
            }

            CharacterContext target =
                targetingController?.CurrentTarget;

            Debug.Log(
                $"[BASIC ATTACK TEST] F pressionado. " +
                $"CombatController: " +
                $"{(combatController != null ? "OK" : "NULL")} | " +
                $"Target: " +
                $"{(target != null ? target.name : "NULL")}",
                this);

            bool succeeded =
                combatController != null &&
                combatController.TryBasicAttack(target);

            Debug.Log(
                $"[BASIC ATTACK TEST] Resultado: " +
                $"{(succeeded ? "SUCESSO" : "FALHOU")}",
                this);
        }

        private void ReadAbilities()
        {
            bool abilityPressed =
                ability1Action != null &&
                ability1Action.WasPressedThisFrame();

            if (Keyboard.current != null &&
                Keyboard.current.qKey.wasPressedThisFrame)
            {
                abilityPressed = true;
            }

            if (!abilityPressed)
            {
                return;
            }

            CharacterContext target =
                targetingController?.CurrentTarget;

            Debug.Log(
                $"[ABILITY TEST] Q pressionado. " +
                $"AbilityController: " +
                $"{(abilityController != null ? "OK" : "NULL")} | " +
                $"Target: " +
                $"{(target != null ? target.name : "NULL")}",
                this);

            bool succeeded =
                abilityController != null &&
                abilityController.TryUse(
                    slot: 0,
                    target: target);

            Debug.Log(
                $"[ABILITY TEST] Resultado: " +
                $"{(succeeded ? "SUCESSO" : "FALHOU")}",
                this);
        }

        private void ResolveActions()
        {
            InputActionMap actionMap = null;

            if (inputActions != null)
            {
                actionMap =
                    inputActions.FindActionMap(
                        actionMapName,
                        throwIfNotFound: false);
            }

            if (moveAction == null)
            {
                moveAction =
                    actionMap?.FindAction(
                        moveActionName,
                        throwIfNotFound: false);

                if (moveAction == null)
                {
                    moveAction =
                        CreateFallbackMoveAction();

                    ownsMoveAction = true;
                }
            }

            if (selectTargetAction == null)
            {
                selectTargetAction =
                    actionMap?.FindAction(
                        selectTargetActionName,
                        throwIfNotFound: false);

                if (selectTargetAction == null)
                {
                    selectTargetAction =
                        CreateFallbackSelectTargetAction();

                    ownsSelectTargetAction = true;
                }
            }

            if (basicAttackAction == null)
            {
                basicAttackAction =
                    actionMap?.FindAction(
                        basicAttackActionName,
                        throwIfNotFound: false);

                if (basicAttackAction == null)
                {
                    basicAttackAction =
                        CreateFallbackBasicAttackAction();

                    ownsBasicAttackAction = true;
                }
            }

            if (ability1Action == null)
            {
                ability1Action =
                    actionMap?.FindAction(
                        ability1ActionName,
                        throwIfNotFound: false);

                if (ability1Action == null)
                {
                    ability1Action =
                        CreateFallbackAbility1Action();

                    ownsAbility1Action = true;
                }
            }
        }

        private void CacheReferences()
        {
            movementController ??=
                GetComponent<MovementController>();

            combatController ??=
                GetComponent<CombatController>();

            abilityController ??=
                GetComponent<AbilityController>();

            targetingController ??=
                GetComponent<TargetingController>();

            if (selectionCamera == null)
            {
                selectionCamera =
                    Camera.main;
            }
        }

        private static InputAction CreateFallbackMoveAction()
        {
            InputAction action =
                new InputAction(
                    name: "Move",
                    type: InputActionType.Value,
                    expectedControlType: "Vector2");

            action.AddCompositeBinding("2DVector")
                .With("Up", "<Keyboard>/w")
                .With("Up", "<Keyboard>/upArrow")
                .With("Down", "<Keyboard>/s")
                .With("Down", "<Keyboard>/downArrow")
                .With("Left", "<Keyboard>/a")
                .With("Left", "<Keyboard>/leftArrow")
                .With("Right", "<Keyboard>/d")
                .With("Right", "<Keyboard>/rightArrow");

            action.AddBinding(
                "<Gamepad>/leftStick");

            return action;
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

        private static InputAction
            CreateFallbackBasicAttackAction()
        {
            InputAction action =
                new InputAction(
                    name: "BasicAttack",
                    type: InputActionType.Button);

            action.AddBinding(
                "<Keyboard>/f");

            return action;
        }

        private static InputAction
            CreateFallbackAbility1Action()
        {
            InputAction action =
                new InputAction(
                    name: "Ability1",
                    type: InputActionType.Button);

            action.AddBinding(
                "<Keyboard>/q");

            action.AddBinding(
                "<Gamepad>/buttonWest");

            return action;
        }
    }
}