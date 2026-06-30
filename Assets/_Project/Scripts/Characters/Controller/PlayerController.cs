using System;
using Riftborn.Characters.Abilities;
using Riftborn.Characters.Combat;
using Riftborn.Characters.Core;
using Riftborn.Characters.Health;
using Riftborn.Characters.Movement;
using Riftborn.Characters.Targeting;
using Riftborn.Damage;
using UnityEngine;

namespace Riftborn.Characters.Controllers
{
    public enum PlayerLifeState
    {
        Alive = 0,
        Dead = 1
    }

    [DisallowMultipleComponent]
    [RequireComponent(typeof(CharacterContext))]
    public sealed class PlayerController : MonoBehaviour
    {
        [Header("Character")]
        [SerializeField]
        private CharacterContext context;

        [Header("Specialized Systems")]
        [SerializeField]
        private HealthController health;

        [SerializeField]
        private MovementController movement;

        [SerializeField]
        private TargetingController targeting;

        [SerializeField]
        private CombatController combat;

        [SerializeField]
        private AbilityController abilities;

        [SerializeField]
        private PlayerAutoAttackController autoAttack;

        [Header("Runtime")]
        [SerializeField]
        private PlayerLifeState lifeState =
            PlayerLifeState.Alive;

        [SerializeField]
        private bool showDebugLogs = true;

        private bool eventsSubscribed;
        private CharacterContext subscribedTarget;

        public event Action<
            PlayerLifeState,
            PlayerLifeState> LifeStateChanged;

        public event Action<
            CharacterContext,
            CharacterContext> TargetChanged;

        public event Action<HealthChangedEventArgs>
            HealthChanged;

        public event Action<DamageResult>
            DamageTaken;

        public event Action<bool>
            BasicAttackProcessed;

        public event Action<int, bool>
            AbilityProcessed;

        public event Action Died;

        public event Action Revived;

        public PlayerLifeState LifeState =>
            lifeState;

        public bool IsAlive =>
            lifeState == PlayerLifeState.Alive &&
            health != null &&
            !health.IsDead;

        public CharacterContext CurrentTarget =>
            targeting != null
                ? targeting.CurrentTarget
                : null;

        public bool HasTarget =>
            targeting != null &&
            targeting.HasTarget;

        public bool AutoAttackEnabled =>
            autoAttack != null &&
            autoAttack.AutoAttackEnabled;

        private void Awake()
        {
            CacheReferences();
            SynchronizeLifeState();
        }

        private void OnEnable()
        {
            CacheReferences();
            SubscribeToEvents();
            SynchronizeLifeState();
        }

        private void OnDisable()
        {
            UnsubscribeFromEvents();
            StopMovement();
            autoAttack?.ResetRuntimeState();
        }

        private void Update()
        {
            TickAutoAttack();
        }

        public bool ActivateWasdMode()
        {
            CacheReferences();

            if (movement == null)
            {
                LogMissingSystem(
                    nameof(MovementController));

                return false;
            }

            movement.ActivateWasdMode();
            return true;
        }

        public bool ActivateClickMoveMode()
        {
            CacheReferences();

            if (movement == null)
            {
                LogMissingSystem(
                    nameof(MovementController));

                return false;
            }

            movement.ActivateClickToMoveMode();
            return true;
        }

        public bool SetMoveInput(Vector2 input)
        {
            CacheReferences();

            if (movement == null)
            {
                LogMissingSystem(
                    nameof(MovementController));

                return false;
            }

            if (!IsAlive)
            {
                movement.SetMoveInput(
                    Vector2.zero);

                return false;
            }

            movement.SetMoveInput(
                input);

            return true;
        }

        public bool SetClickDestination(
            Vector3 worldPosition)
        {
            CacheReferences();

            if (!IsAlive ||
                movement == null)
            {
                return false;
            }

            return movement.SetClickDestination(
                worldPosition);
        }

        public void CancelClickDestination()
        {
            CacheReferences();
            movement?.CancelClickDestination();
        }

        public void StopMovement()
        {
            CacheReferences();

            movement?.SetMoveInput(
                Vector2.zero);

            movement?.CancelClickDestination();
        }

        public bool IsValidTarget(
            CharacterContext candidate)
        {
            CacheReferences();

            return IsAlive &&
                   targeting != null &&
                   targeting.IsValidTarget(
                       candidate);
        }

        public bool TrySelectTarget(
            CharacterContext newTarget)
        {
            CacheReferences();

            if (!IsAlive ||
                targeting == null)
            {
                return false;
            }

            return targeting.SetTarget(
                newTarget);
        }

        public void ClearTarget()
        {
            CacheReferences();
            targeting?.ClearTarget();
        }

        public bool RequestBasicAttack()
        {
            CacheReferences();

            bool succeeded =
                TryExecuteBasicAttack(
                    CurrentTarget);

            BasicAttackProcessed?.Invoke(
                succeeded);

            return succeeded;
        }

        public bool RequestAbility(int slot)
        {
            CacheReferences();

            bool succeeded =
                IsAlive &&
                abilities != null &&
                abilities.enabled &&
                abilities.TryUse(
                    slot,
                    CurrentTarget);

            AbilityProcessed?.Invoke(
                slot,
                succeeded);

            return succeeded;
        }

        public void SetAutoAttackEnabled(
            bool enabled)
        {
            CacheReferences();
            autoAttack?.SetAutoAttackEnabled(
                enabled);
        }

        public void StopAutoAttack(
            bool clearSelection)
        {
            CacheReferences();

            autoAttack?.ResetRuntimeState();

            if (clearSelection)
            {
                targeting?.ClearTarget();
            }
        }

        private void TickAutoAttack()
        {
            if (!IsAlive ||
                autoAttack == null)
            {
                autoAttack?.ResetRuntimeState();
                return;
            }

            CharacterContext selectedTarget =
                CurrentTarget;

            bool targetIsValid =
                targeting != null &&
                targeting.IsValidTarget(
                    selectedTarget);

            bool combatAvailable =
                combat != null &&
                combat.enabled;

            bool targetInRange =
                targetIsValid &&
                combatAvailable &&
                combat.IsTargetInRange(
                    selectedTarget);

            float distance =
                selectedTarget != null
                    ? Vector3.Distance(
                        transform.position,
                        selectedTarget.transform.position)
                    : 0f;

            float attackRange =
                combat != null
                    ? combat.AttackRange
                    : 0f;

            AutoAttackDecision decision =
                autoAttack.Evaluate(
                    selectedTarget,
                    targetIsValid,
                    combatAvailable,
                    targetInRange,
                    distance,
                    attackRange);

            switch (decision)
            {
                case AutoAttackDecision.InvalidTarget:
                    if (autoAttack.ClearInvalidTarget)
                    {
                        targeting?.ClearTarget();
                    }
                    break;

                case AutoAttackDecision.ReadyToAttack:
                    bool succeeded =
                        TryExecuteBasicAttack(
                            selectedTarget);

                    autoAttack.ReportAttackAttempt(
                        succeeded);
                    break;
            }
        }

        private bool TryExecuteBasicAttack(
            CharacterContext target)
        {
            if (!IsAlive ||
                combat == null ||
                !combat.enabled)
            {
                return false;
            }

            return combat.TryBasicAttack(
                target);
        }

        private void SubscribeToEvents()
        {
            if (eventsSubscribed)
            {
                return;
            }

            if (health != null)
            {
                health.HealthChanged +=
                    HandleHealthChanged;

                health.DamageTaken +=
                    HandleDamageTaken;

                health.Died +=
                    HandleDied;

                health.Revived +=
                    HandleRevived;
            }

            if (targeting != null)
            {
                targeting.TargetChanged +=
                    HandleTargetChanged;

                SubscribeToSelectedTarget(
                    targeting.CurrentTarget);
            }

            if (autoAttack != null)
            {
                autoAttack.StopRequested +=
                    HandleAutoAttackStopRequested;
            }

            eventsSubscribed = true;
        }

        private void UnsubscribeFromEvents()
        {
            if (!eventsSubscribed)
            {
                return;
            }

            if (health != null)
            {
                health.HealthChanged -=
                    HandleHealthChanged;

                health.DamageTaken -=
                    HandleDamageTaken;

                health.Died -=
                    HandleDied;

                health.Revived -=
                    HandleRevived;
            }

            if (targeting != null)
            {
                targeting.TargetChanged -=
                    HandleTargetChanged;
            }

            UnsubscribeFromSelectedTarget();

            if (autoAttack != null)
            {
                autoAttack.StopRequested -=
                    HandleAutoAttackStopRequested;
            }

            eventsSubscribed = false;
        }

        private void HandleHealthChanged(
            HealthChangedEventArgs eventArgs)
        {
            HealthChanged?.Invoke(
                eventArgs);
        }

        private void HandleDamageTaken(
            DamageResult result)
        {
            DamageTaken?.Invoke(
                result);
        }

        private void HandleDied()
        {
            StopMovement();
            autoAttack?.ResetRuntimeState();
            targeting?.ClearTarget();

            SetLifeState(
                PlayerLifeState.Dead);

            Died?.Invoke();

            if (showDebugLogs)
            {
                Debug.Log(
                    $"[PLAYER CONTROLLER] {name} morreu.",
                    this);
            }
        }

        private void HandleRevived()
        {
            StopMovement();
            autoAttack?.ResetRuntimeState();

            SetLifeState(
                PlayerLifeState.Alive);

            Revived?.Invoke();

            if (showDebugLogs)
            {
                Debug.Log(
                    $"[PLAYER CONTROLLER] {name} renasceu.",
                    this);
            }
        }

        private void HandleTargetChanged(
            CharacterContext previousTarget,
            CharacterContext newTarget)
        {
            UnsubscribeFromSelectedTarget();
            SubscribeToSelectedTarget(
                newTarget);

            autoAttack?.NotifyTargetChanged(
                newTarget);

            TargetChanged?.Invoke(
                previousTarget,
                newTarget);
        }

        private void SubscribeToSelectedTarget(
            CharacterContext selectedTarget)
        {
            if (selectedTarget == null ||
                selectedTarget.Health == null)
            {
                subscribedTarget = null;
                return;
            }

            subscribedTarget =
                selectedTarget;

            subscribedTarget.Health.Died +=
                HandleSelectedTargetDied;
        }

        private void UnsubscribeFromSelectedTarget()
        {
            if (subscribedTarget == null ||
                subscribedTarget.Health == null)
            {
                subscribedTarget = null;
                return;
            }

            subscribedTarget.Health.Died -=
                HandleSelectedTargetDied;

            subscribedTarget = null;
        }

        private void HandleSelectedTargetDied()
        {
            if (showDebugLogs &&
                subscribedTarget != null)
            {
                Debug.Log(
                    $"[PLAYER CONTROLLER] O alvo " +
                    $"{subscribedTarget.name} morreu. " +
                    "Solicitando remoção da seleção.",
                    this);
            }

            targeting?.ClearTarget();
        }

        private void HandleAutoAttackStopRequested(
            bool clearSelection)
        {
            if (clearSelection)
            {
                targeting?.ClearTarget();
            }
        }

        private void SynchronizeLifeState()
        {
            PlayerLifeState correctState =
                health != null &&
                health.IsDead
                    ? PlayerLifeState.Dead
                    : PlayerLifeState.Alive;

            SetLifeState(
                correctState);
        }

        private void SetLifeState(
            PlayerLifeState newState)
        {
            if (lifeState == newState)
            {
                return;
            }

            PlayerLifeState previousState =
                lifeState;

            lifeState =
                newState;

            LifeStateChanged?.Invoke(
                previousState,
                newState);
        }

        private void CacheReferences()
        {
            context ??=
                GetComponent<CharacterContext>();

            health ??=
                context?.Health;

            health ??=
                GetComponent<HealthController>();

            movement ??=
                context?.Movement;

            movement ??=
                GetComponent<MovementController>();

            targeting ??=
                context?.Targeting;

            targeting ??=
                GetComponent<TargetingController>();

            combat ??=
                context?.Combat;

            combat ??=
                GetComponent<CombatController>();

            abilities ??=
                context?.Abilities;

            abilities ??=
                GetComponent<AbilityController>();

            autoAttack ??=
                GetComponent<PlayerAutoAttackController>();
        }

        private void LogMissingSystem(
            string systemName)
        {
            if (!showDebugLogs)
            {
                return;
            }

            Debug.LogWarning(
                $"[PLAYER CONTROLLER] {name} não encontrou " +
                $"{systemName}.",
                this);
        }
    }
}
