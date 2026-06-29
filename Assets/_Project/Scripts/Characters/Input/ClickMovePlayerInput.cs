using Riftborn.Characters.Abilities;
using Riftborn.Characters.Combat;
using Riftborn.Characters.Movement;
using Riftborn.Characters.Targeting;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace Riftborn.Characters.Input
{
    public sealed class ClickMovePlayerInput : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private MovementController movement;
        [SerializeField] private AbilityController abilities;
        [SerializeField] private CombatController combat;
        [SerializeField] private TargetingController targeting;
        [SerializeField] private WasdPlayerInput wasdInput;
        [SerializeField] private Camera worldCamera;

        [Header("Ground Raycast")]
        [SerializeField] private LayerMask groundMask = ~0;
        [SerializeField, Min(0.1f)] private float maximumRaycastDistance = 500f;

        [Header("Mode")]
        [SerializeField] private Key switchModeKey = Key.I;

        [Header("Click-Move Ability Mapping")]
        [SerializeField] private Key ability1Key = Key.Q;
        [SerializeField] private Key ability2Key = Key.W;
        [SerializeField] private Key ability3Key = Key.E;
        [SerializeField] private Key ability4Key = Key.R;
        [SerializeField] private Key ability5Key = Key.F;
        [SerializeField] private Key basicAttackKey = Key.A;

        private void Awake()
        {
            ResolveReferences();
        }

        private void OnEnable()
        {
            ResolveReferences();

            if (wasdInput != null && wasdInput.enabled)
            {
                wasdInput.enabled = false;
            }

            movement?.ActivateClickToMoveMode();
        }

        private void OnDisable()
        {
            movement?.CancelClickDestination();
        }

        private void Update()
        {
            Keyboard keyboard = Keyboard.current;

            if (keyboard != null && WasPressed(keyboard, switchModeKey))
            {
                SwitchToWasd();
                return;
            }

            HandleClickDestination();

            if (keyboard == null)
            {
                return;
            }

            TryUseAbility(keyboard, ability1Key, 0);
            TryUseAbility(keyboard, ability2Key, 1);
            TryUseAbility(keyboard, ability3Key, 2);
            TryUseAbility(keyboard, ability4Key, 3);
            TryUseAbility(keyboard, ability5Key, 4);

            if (WasPressed(keyboard, basicAttackKey))
            {
                combat?.TryBasicAttack(targeting?.CurrentTarget);
            }
        }

        private void HandleClickDestination()
        {
            Mouse mouse = Mouse.current;

            if (mouse == null || !mouse.rightButton.wasPressedThisFrame)
            {
                return;
            }

            if (EventSystem.current != null &&
                EventSystem.current.IsPointerOverGameObject())
            {
                return;
            }

            Camera camera = worldCamera != null
                ? worldCamera
                : Camera.main;

            if (camera == null)
            {
                Debug.LogWarning(
                    "[INPUT] Nenhuma câmera foi encontrada para o click-to-move.",
                    this);

                return;
            }

            Ray ray = camera.ScreenPointToRay(mouse.position.ReadValue());

            if (!Physics.Raycast(
                    ray,
                    out RaycastHit hit,
                    maximumRaycastDistance,
                    groundMask,
                    QueryTriggerInteraction.Ignore))
            {
                return;
            }

            movement?.SetClickDestination(hit.point);
        }

        private void SwitchToWasd()
        {
            if (wasdInput == null)
            {
                Debug.LogWarning(
                    "[INPUT] WasdPlayerInput não foi encontrado.",
                    this);

                return;
            }

            movement?.CancelClickDestination();
            wasdInput.enabled = true;
            enabled = false;
        }

        private void TryUseAbility(Keyboard keyboard, Key key, int slot)
        {
            if (!WasPressed(keyboard, key))
            {
                return;
            }

            abilities?.TryUse(slot, targeting?.CurrentTarget);
        }

        private static bool WasPressed(Keyboard keyboard, Key key)
        {
            return keyboard[key].wasPressedThisFrame;
        }

        private void ResolveReferences()
        {
            movement ??= GetComponent<MovementController>();
            abilities ??= GetComponent<AbilityController>();
            combat ??= GetComponent<CombatController>();
            targeting ??= GetComponent<TargetingController>();
            wasdInput ??= GetComponent<WasdPlayerInput>();

            if (worldCamera == null && Camera.main != null)
            {
                worldCamera = Camera.main;
            }
        }

        private void OnValidate()
        {
            maximumRaycastDistance = Mathf.Max(0.1f, maximumRaycastDistance);
        }
    }
}
