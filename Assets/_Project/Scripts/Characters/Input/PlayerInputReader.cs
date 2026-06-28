using Riftborn.Characters.Movement;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Riftborn.Characters.Input
{
    public sealed class PlayerInputReader : MonoBehaviour
    {
        [SerializeField] private InputActionAsset inputActions;
        [SerializeField] private string actionMapName = "Player";
        [SerializeField] private string moveActionName = "Move";
        [SerializeField] private MovementController movementController;

        private InputAction moveAction;
        private bool ownsMoveAction;

        private void Awake()
        {
            movementController ??= GetComponent<MovementController>();
            ResolveActions();
        }

        private void OnEnable()
        {
            ResolveActions();
            moveAction?.Enable();
        }

        private void OnDisable()
        {
            moveAction?.Disable();
            movementController?.SetMoveInput(Vector2.zero);
        }

        private void OnDestroy()
        {
            if (ownsMoveAction)
            {
                moveAction?.Dispose();
            }
        }

        private void Update()
        {
            Vector2 input = moveAction != null ? moveAction.ReadValue<Vector2>() : Vector2.zero;
            movementController?.SetMoveInput(input);
        }

        private void ResolveActions()
        {
            if (moveAction != null)
            {
                return;
            }

            if (inputActions != null)
            {
                InputActionMap actionMap = inputActions.FindActionMap(actionMapName, false);
                moveAction = actionMap?.FindAction(moveActionName, false);
            }

            if (moveAction == null)
            {
                moveAction = CreateFallbackMoveAction();
                ownsMoveAction = true;
            }
        }

        private static InputAction CreateFallbackMoveAction()
        {
            var action = new InputAction("Move", InputActionType.Value, expectedControlType: "Vector2");
            action.AddCompositeBinding("2DVector")
                .With("Up", "<Keyboard>/w")
                .With("Up", "<Keyboard>/upArrow")
                .With("Down", "<Keyboard>/s")
                .With("Down", "<Keyboard>/downArrow")
                .With("Left", "<Keyboard>/a")
                .With("Left", "<Keyboard>/leftArrow")
                .With("Right", "<Keyboard>/d")
                .With("Right", "<Keyboard>/rightArrow");
            action.AddBinding("<Gamepad>/leftStick");
            return action;
        }
    }
}
