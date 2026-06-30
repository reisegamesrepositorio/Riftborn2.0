using System.Collections;
using Riftborn.Characters.Health;
using Riftborn.Characters.Input;
using Riftborn.Characters.Movement;
using Riftborn.Characters.StatusEffects;
using Riftborn.Characters.Targeting;
using UnityEngine;

namespace Riftborn.Characters.Respawn
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(HealthController))]
    public sealed class PlayerDeathController : MonoBehaviour
    {
        [Header("Respawn")]
        [SerializeField]
        private Transform respawnPoint;

        [SerializeField, Min(0f)]
        private float respawnDelay = 3f;

        [SerializeField, Range(0.01f, 1f)]
        private float respawnHealthPercent = 1f;

        [SerializeField]
        private bool clearStatusEffectsOnDeath = true;

        [Header("Main References")]
        [SerializeField]
        private HealthController health;

        [SerializeField]
        private MovementController movement;

        [SerializeField]
        private TargetingController targeting;

        [SerializeField]
        private StatusEffectController statusEffects;

        [Header("Input References")]
        [SerializeField]
        private PlayerInputModeController inputModeController;

        [SerializeField]
        private WasdPlayerInput wasdInput;

        [SerializeField]
        private ClickMovePlayerInput clickMoveInput;

        [Tooltip(
            "Coloque aqui qualquer outro script de input " +
            "que precise ser desativado enquanto o player estiver morto. " +
            "Exemplo: PlayerInputReader.")]
        [SerializeField]
        private MonoBehaviour[] additionalInputBehaviours;

        [Header("Runtime Debug")]
        [SerializeField]
        private bool isRespawning;

        [SerializeField]
        private bool showDebugLogs = true;

        private Vector3 initialRespawnPosition;
        private Quaternion initialRespawnRotation;

        private PlayerInputMode previousInputMode;

        private bool inputModeControllerWasEnabled;
        private bool wasdInputWasEnabled;
        private bool clickMoveInputWasEnabled;

        private bool[] additionalInputWasEnabled;

        private Coroutine respawnCoroutine;
        private bool subscribedToHealth;

        public bool IsRespawning =>
            isRespawning;

        private void Awake()
        {
            CacheReferences();

            initialRespawnPosition =
                transform.position;

            initialRespawnRotation =
                transform.rotation;
        }

        private void OnEnable()
        {
            CacheReferences();
            SubscribeToHealth();
        }

        private void Start()
        {
            if (health != null &&
                health.IsDead)
            {
                HandleDied();
            }
        }

        private void OnDisable()
        {
            UnsubscribeFromHealth();
        }

        private void OnValidate()
        {
            respawnDelay =
                Mathf.Max(
                    0f,
                    respawnDelay);

            respawnHealthPercent =
                Mathf.Clamp(
                    respawnHealthPercent,
                    0.01f,
                    1f);
        }

        private void HandleDied()
        {
            if (isRespawning)
            {
                return;
            }

            isRespawning = true;

            CaptureInputState();
            DisablePlayerControl();

            targeting?.ClearTarget();

            if (clearStatusEffectsOnDeath)
            {
                statusEffects?.ClearAllEffects();
            }

            if (showDebugLogs)
            {
                Debug.Log(
                    $"[PLAYER DEATH] " +
                    $"{name} morreu. " +
                    $"Respawn em {respawnDelay:0.##} segundos.",
                    this);
            }

            respawnCoroutine =
                StartCoroutine(
                    RespawnRoutine());
        }

        private IEnumerator RespawnRoutine()
        {
            if (respawnDelay > 0f)
            {
                yield return new WaitForSeconds(
                    respawnDelay);
            }

            Vector3 destination =
                respawnPoint != null
                    ? respawnPoint.position
                    : initialRespawnPosition;

            Quaternion destinationRotation =
                respawnPoint != null
                    ? respawnPoint.rotation
                    : initialRespawnRotation;

            movement?.SetMoveInput(
                Vector2.zero);

            movement?.CancelClickDestination();

            if (movement != null)
            {
                movement.Teleport(
                    destination);
            }
            else
            {
                transform.position =
                    destination;
            }

            transform.rotation =
                destinationRotation;

            if (health != null &&
                health.IsDead)
            {
                float restoredHealth =
                    Mathf.Max(
                        1f,
                        health.MaxHealth *
                        respawnHealthPercent);

                health.Revive(
                    restoredHealth);
            }

            RestorePlayerControl();

            isRespawning = false;
            respawnCoroutine = null;

            if (showDebugLogs)
            {
                Debug.Log(
                    $"[PLAYER RESPAWN] " +
                    $"{name} renasceu em " +
                    $"{destination} com " +
                    $"{health?.CurrentHealth:0.##}/" +
                    $"{health?.MaxHealth:0.##} de vida.",
                    this);
            }
        }

        private void CaptureInputState()
        {
            inputModeControllerWasEnabled =
                inputModeController != null &&
                inputModeController.enabled;

            wasdInputWasEnabled =
                wasdInput != null &&
                wasdInput.enabled;

            clickMoveInputWasEnabled =
                clickMoveInput != null &&
                clickMoveInput.enabled;

            if (inputModeController != null)
            {
                previousInputMode =
                    inputModeController.CurrentMode;
            }
            else
            {
                previousInputMode =
                    clickMoveInputWasEnabled
                        ? PlayerInputMode.ClickMove
                        : PlayerInputMode.Wasd;
            }

            int additionalCount =
                additionalInputBehaviours?.Length ?? 0;

            additionalInputWasEnabled =
                new bool[additionalCount];

            for (int index = 0;
                 index < additionalCount;
                 index++)
            {
                MonoBehaviour behaviour =
                    additionalInputBehaviours[index];

                additionalInputWasEnabled[index] =
                    behaviour != null &&
                    behaviour.enabled;
            }
        }

        private void DisablePlayerControl()
        {
            movement?.SetMoveInput(
                Vector2.zero);

            movement?.CancelClickDestination();

            if (inputModeController != null)
            {
                inputModeController.enabled =
                    false;
            }

            if (wasdInput != null)
            {
                wasdInput.enabled =
                    false;
            }

            if (clickMoveInput != null)
            {
                clickMoveInput.enabled =
                    false;
            }

            int additionalCount =
                additionalInputBehaviours?.Length ?? 0;

            for (int index = 0;
                 index < additionalCount;
                 index++)
            {
                MonoBehaviour behaviour =
                    additionalInputBehaviours[index];

                if (behaviour == null ||
                    IsMainInputBehaviour(
                        behaviour))
                {
                    continue;
                }

                behaviour.enabled =
                    false;
            }
        }

        private void RestorePlayerControl()
        {
            movement?.SetMoveInput(
                Vector2.zero);

            movement?.CancelClickDestination();

            /*
             * Caso o PlayerInputModeController estivesse ativo,
             * ele volta a controlar qual dos dois modos será ligado.
             */
            if (inputModeController != null &&
                inputModeControllerWasEnabled)
            {
                inputModeController.enabled =
                    true;

                inputModeController.SetMode(
                    previousInputMode);
            }
            else
            {
                if (inputModeController != null)
                {
                    inputModeController.enabled =
                        false;
                }

                if (wasdInput != null)
                {
                    wasdInput.enabled =
                        wasdInputWasEnabled;
                }

                if (clickMoveInput != null)
                {
                    clickMoveInput.enabled =
                        clickMoveInputWasEnabled;
                }
            }

            int additionalCount =
                additionalInputBehaviours?.Length ?? 0;

            for (int index = 0;
                 index < additionalCount;
                 index++)
            {
                MonoBehaviour behaviour =
                    additionalInputBehaviours[index];

                if (behaviour == null ||
                    IsMainInputBehaviour(
                        behaviour))
                {
                    continue;
                }

                bool shouldEnable =
                    additionalInputWasEnabled != null &&
                    index <
                    additionalInputWasEnabled.Length &&
                    additionalInputWasEnabled[index];

                behaviour.enabled =
                    shouldEnable;
            }
        }

        private bool IsMainInputBehaviour(
            MonoBehaviour behaviour)
        {
            return
                ReferenceEquals(
                    behaviour,
                    inputModeController) ||
                ReferenceEquals(
                    behaviour,
                    wasdInput) ||
                ReferenceEquals(
                    behaviour,
                    clickMoveInput) ||
                ReferenceEquals(
                    behaviour,
                    this);
        }

        public void SetRespawnPoint(
            Transform newRespawnPoint)
        {
            respawnPoint =
                newRespawnPoint;

            if (showDebugLogs)
            {
                Debug.Log(
                    newRespawnPoint != null
                        ? $"[RESPAWN POINT] Novo ponto: " +
                          $"{newRespawnPoint.name}."
                        : "[RESPAWN POINT] Ponto removido. " +
                          "A posição inicial será utilizada.",
                    this);
            }
        }

        private void CacheReferences()
        {
            health ??=
                GetComponent<HealthController>();

            movement ??=
                GetComponent<MovementController>();

            targeting ??=
                GetComponent<TargetingController>();

            statusEffects ??=
                GetComponent<StatusEffectController>();

            inputModeController ??=
                GetComponent<PlayerInputModeController>();

            wasdInput ??=
                GetComponent<WasdPlayerInput>();

            clickMoveInput ??=
                GetComponent<ClickMovePlayerInput>();

            if (health == null)
            {
                Debug.LogError(
                    $"{nameof(PlayerDeathController)} " +
                    $"requires a " +
                    $"{nameof(HealthController)}.",
                    this);
            }
        }

        private void SubscribeToHealth()
        {
            if (subscribedToHealth ||
                health == null)
            {
                return;
            }

            health.Died +=
                HandleDied;

            subscribedToHealth =
                true;
        }

        private void UnsubscribeFromHealth()
        {
            if (!subscribedToHealth ||
                health == null)
            {
                return;
            }

            health.Died -=
                HandleDied;

            subscribedToHealth =
                false;
        }
    }
}