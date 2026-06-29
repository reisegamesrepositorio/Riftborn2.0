using Riftborn.Characters.Abilities;
using Riftborn.Characters.Combat;
using Riftborn.Characters.Movement;
using Riftborn.Characters.Targeting;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Riftborn.Characters.Input
{
    public sealed class WasdPlayerInput : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private MovementController movement;
        [SerializeField] private AbilityController abilities;
        [SerializeField] private CombatController combat;
        [SerializeField] private TargetingController targeting;
        [SerializeField] private ClickMovePlayerInput clickMoveInput;

        [Header("Mode")]
        [SerializeField] private Key switchModeKey = Key.I;

        [Header("WASD Ability Mapping")]
        [SerializeField] private Key ability1Key = Key.Q;
        [SerializeField] private Key ability2Key = Key.E;
        [SerializeField] private Key ability3Key = Key.R;
        [SerializeField] private Key ability4Key = Key.F;
        [SerializeField] private Key ability5Key = Key.T;
        [SerializeField] private Key basicAttackKey = Key.Space;

        private void Awake()
        {
            ResolveReferences();
        }

        private void OnEnable()
        {
            ResolveReferences();

            if (clickMoveInput != null && clickMoveInput.enabled)
            {
                clickMoveInput.enabled = false;
            }

            movement?.ActivateWasdMode();
        }

        private void OnDisable()
        {
            movement?.SetMoveInput(Vector2.zero);
        }

        private void Update()
        {
            Keyboard keyboard = Keyboard.current;

            if (keyboard == null)
            {
                movement?.SetMoveInput(Vector2.zero);
                return;
            }

            if (WasPressed(keyboard, switchModeKey))
            {
                SwitchToClickMove();
                return;
            }

            movement?.SetMoveInput(ReadWasdInput(keyboard));

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

        private void SwitchToClickMove()
        {
            if (clickMoveInput == null)
            {
                Debug.LogWarning(
                    "[INPUT] ClickMovePlayerInput não foi encontrado.",
                    this);

                return;
            }

            movement?.SetMoveInput(Vector2.zero);
            clickMoveInput.enabled = true;
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

        private static Vector2 ReadWasdInput(Keyboard keyboard)
        {
            float horizontal = 0f;
            float vertical = 0f;

            if (keyboard.aKey.isPressed)
            {
                horizontal -= 1f;
            }

            if (keyboard.dKey.isPressed)
            {
                horizontal += 1f;
            }

            if (keyboard.sKey.isPressed)
            {
                vertical -= 1f;
            }

            if (keyboard.wKey.isPressed)
            {
                vertical += 1f;
            }

            return Vector2.ClampMagnitude(
                new Vector2(horizontal, vertical),
                1f);
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
            clickMoveInput ??= GetComponent<ClickMovePlayerInput>();
        }
    }
}
