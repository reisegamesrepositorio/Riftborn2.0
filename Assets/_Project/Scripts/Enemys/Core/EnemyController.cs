using System;
using Riftborn.Characters.ActionStates;
using Riftborn.Characters.Combat;
using Riftborn.Characters.Core;
using Riftborn.Characters.Health;
using Riftborn.Characters.Progression;
using Riftborn.Characters.StatusEffects;
using Riftborn.Characters.Visual;
using Riftborn.Damage;
using Riftborn.Enemies.AI;
using Riftborn.Enemies.Movement;
using Riftborn.Enemies.Respawn;
using Riftborn.Items;
using UnityEngine;

namespace Riftborn.Enemies.Core
{
    public enum EnemyLifeState
    {
        Alive = 0,
        Dead = 1,
        Respawning = 2
    }

    [DisallowMultipleComponent]
    [RequireComponent(typeof(CharacterContext))]
    [RequireComponent(typeof(HealthController))]
    [RequireComponent(typeof(EnemyAIController))]
    [RequireComponent(typeof(EnemyMovementController))]
    [RequireComponent(typeof(CombatController))]
    [RequireComponent(typeof(EnemyRespawnController))]
    public sealed class EnemyController : MonoBehaviour
    {
        [Header("Core")]
        [SerializeField]
        private CharacterContext context;

        [SerializeField]
        private HealthController health;

        [SerializeField]
        private ActionStateController actionState;

        [SerializeField]
        private StatusEffectController statusEffects;

        [Header("Enemy Systems")]
        [SerializeField]
        private EnemyAIController ai;

        [SerializeField]
        private EnemyMovementController movement;

        [SerializeField]
        private CombatController combat;

        [SerializeField]
        private EnemyRespawnController respawn;

        [Header("Feedback and Rewards")]
        [SerializeField]
        private CharacterHitFeedback hitFeedback;

        [SerializeField]
        private CharacterDeathFeedback deathFeedback;

        [SerializeField]
        private LootDropController lootDrop;

        [SerializeField]
        private ExperienceReward experienceReward;

        [Header("Runtime Debug")]
        [SerializeField]
        private EnemyLifeState lifeState =
            EnemyLifeState.Alive;

        [SerializeField]
        private bool showDebugLogs = true;

        private bool healthEventsSubscribed;
        private bool respawnEventSubscribed;
        private bool initialized;

        public event Action<DamageResult>
            DamageReceived;

        public event Action
            Died;

        public event Action
            Revived;

        public EnemyLifeState LifeState =>
            lifeState;

        public EnemyAIState AIState =>
            ai != null
                ? ai.CurrentState
                : EnemyAIState.Dead;

        public CharacterContext Context =>
            context;

        public HealthController Health =>
            health;

        public CharacterContext CurrentTarget =>
            ai != null
                ? ai.CurrentTarget
                : null;

        private void Awake()
        {
            CacheReferences();
            InitializeSystems();
        }

        private void OnEnable()
        {
            CacheReferences();
            SubscribeToEvents();
        }

        private void Start()
        {
            CacheReferences();
            InitializeSystems();

            if (health != null &&
                health.IsDead)
            {
                ai?.NotifyDeath();
                deathFeedback?.PlayDeath();

                SetLifeState(
                    EnemyLifeState.Dead);

                RequestRespawn();
            }
        }

        private void OnDisable()
        {
            UnsubscribeFromEvents();
        }

        private void Reset()
        {
            CacheReferences();
        }

        private void Update()
        {
            if (!initialized)
            {
                InitializeSystems();
            }

            float deltaTime =
                Time.deltaTime;

            if (lifeState !=
                    EnemyLifeState.Alive ||
                health == null ||
                health.IsDead)
            {
                movement?.Stop(
                    deltaTime);

                return;
            }

            CharacterContext target =
                ai?.CurrentTarget;

            bool targetInAttackRange =
                target != null &&
                combat != null &&
                combat.IsTargetInRange(
                    target);

            ai?.Tick(
                targetInAttackRange,
                combat != null
                    ? combat.AttackRange
                    : 0f);

            ExecuteCurrentAIState(
                deltaTime);
        }

        public bool SetTarget(
            CharacterContext target)
        {
            if (lifeState !=
                EnemyLifeState.Alive ||
                ai == null)
            {
                return false;
            }

            return ai.SetTarget(
                target);
        }

        public void ForceReturnToSpawn()
        {
            if (lifeState !=
                EnemyLifeState.Alive)
            {
                return;
            }

            ai?.BeginReturn();
        }

        public bool RequestRespawn()
        {
            if (lifeState ==
                    EnemyLifeState.Alive ||
                respawn == null)
            {
                return false;
            }

            bool started =
                respawn.BeginRespawn();

            if (started)
            {
                SetLifeState(
                    EnemyLifeState.Respawning);
            }

            return started;
        }

        private void ExecuteCurrentAIState(
            float deltaTime)
        {
            if (ai == null)
            {
                movement?.Stop(
                    deltaTime);

                return;
            }

            switch (ai.CurrentState)
            {
                case EnemyAIState.Idle:
                    movement?.Stop(
                        deltaTime);
                    break;

                case EnemyAIState.Chase:
                    ExecuteChase(
                        deltaTime);
                    break;

                case EnemyAIState.Attack:
                    ExecuteAttack(
                        deltaTime);
                    break;

                case EnemyAIState.Return:
                    movement?.MoveTo(
                        ai.SpawnPosition,
                        deltaTime);
                    break;

                case EnemyAIState.Dead:
                    movement?.Stop(
                        deltaTime);
                    break;
            }
        }

        private void ExecuteChase(
            float deltaTime)
        {
            CharacterContext target =
                ai.CurrentTarget;

            if (target == null)
            {
                movement?.Stop(
                    deltaTime);

                return;
            }

            movement?.MoveTo(
                target.transform.position,
                deltaTime);
        }

        private void ExecuteAttack(
            float deltaTime)
        {
            CharacterContext target =
                ai.CurrentTarget;

            if (target == null)
            {
                movement?.Stop(
                    deltaTime);

                return;
            }

            movement?.Stop(
                deltaTime);

            Vector3 targetDirection =
                target.transform.position -
                transform.position;

            movement?.FaceDirection(
                targetDirection,
                deltaTime);

            combat?.TryBasicAttack(
                target);
        }

        private void HandleDamageTaken(
            DamageResult damageResult)
        {
            if (lifeState !=
                EnemyLifeState.Alive)
            {
                return;
            }

            ai?.NotifyDamageTaken(
                damageResult);

            hitFeedback?.PlayHit(
                damageResult);

            DamageReceived?.Invoke(
                damageResult);
        }

        private void HandleDied()
        {
            if (lifeState !=
                EnemyLifeState.Alive)
            {
                return;
            }

            SetLifeState(
                EnemyLifeState.Dead);

            ai?.NotifyDeath();
            movement?.Stop(0f);
            hitFeedback?.ResetFeedback();
            deathFeedback?.PlayDeath();

            lootDrop?.DropLoot();

            CharacterContext killer =
                health?
                    .LastDamageApplication?
                    .DamageResult?
                    .Request?
                    .Source;

            experienceReward?.GrantExperienceTo(
                killer);

            Died?.Invoke();

            if (showDebugLogs)
            {
                Debug.Log(
                    $"[ENEMY CONTROLLER] {name}: morte coordenada. " +
                    "Feedback, loot, XP e respawn foram encaminhados.",
                    this);
            }

            RequestRespawn();
        }

        private void HandleRespawnReady(
            EnemyRespawnResult result)
        {
            if (lifeState !=
                EnemyLifeState.Respawning)
            {
                return;
            }

            if (respawn != null &&
                respawn.ClearStatusEffectsOnRespawn)
            {
                statusEffects?.ClearAllEffects();
            }

            if (respawn != null &&
                respawn.ClearActionBlocksOnRespawn)
            {
                actionState?.ClearAllBlocks();
            }

            deathFeedback?.RestoreAfterRevive();
            hitFeedback?.ResetFeedback();

            movement?.Teleport(
                result.Position);

            transform.rotation =
                result.Rotation;

            ai?.SetSpawnPosition(
                result.Position);

            if (health != null &&
                health.IsDead)
            {
                float restoredHealth =
                    Mathf.Max(
                        1f,
                        health.MaxHealth *
                        result.HealthPercent);

                health.Revive(
                    restoredHealth);
            }
            else
            {
                CompleteRevive();
            }
        }

        private void HandleRevived()
        {
            CompleteRevive();
        }

        private void CompleteRevive()
        {
            if (health == null ||
                health.IsDead)
            {
                return;
            }

            deathFeedback?.RestoreAfterRevive();
            hitFeedback?.ResetFeedback();
            lootDrop?.ResetLootState();
            experienceReward?.ResetRewardState();

            ai?.ResetAfterRespawn();

            SetLifeState(
                EnemyLifeState.Alive);

            Revived?.Invoke();

            if (showDebugLogs)
            {
                Debug.Log(
                    $"[ENEMY CONTROLLER] {name}: revive concluído | " +
                    $"Posição: {transform.position} | " +
                    $"Vida: {health.CurrentHealth:0.##}/" +
                    $"{health.MaxHealth:0.##} | " +
                    $"AI: {ai?.CurrentState}.",
                    this);
            }
        }

        private void InitializeSystems()
        {
            if (initialized)
            {
                return;
            }

            if (context == null ||
                health == null ||
                ai == null ||
                movement == null ||
                combat == null ||
                respawn == null)
            {
                ValidateReferences();
                return;
            }

            ai.SetSpawnPosition(
                transform.position);

            if (health.IsDead)
            {
                lifeState =
                    EnemyLifeState.Dead;

                ai.NotifyDeath();
            }
            else
            {
                lifeState =
                    EnemyLifeState.Alive;

                ai.ResetAfterRespawn();
            }

            initialized =
                true;
        }

        private void CacheReferences()
        {
            context ??=
                GetComponent<CharacterContext>();

            health ??=
                GetComponent<HealthController>();

            actionState ??=
                GetComponent<ActionStateController>();

            statusEffects ??=
                GetComponent<StatusEffectController>();

            ai ??=
                GetComponent<EnemyAIController>();

            movement ??=
                GetComponent<EnemyMovementController>();

            combat ??=
                GetComponent<CombatController>();

            respawn ??=
                GetComponent<EnemyRespawnController>();

            hitFeedback ??=
                GetComponent<CharacterHitFeedback>();

            deathFeedback ??=
                GetComponent<CharacterDeathFeedback>();

            lootDrop ??=
                GetComponent<LootDropController>();

            experienceReward ??=
                GetComponent<ExperienceReward>();
        }

        private void SubscribeToEvents()
        {
            if (!healthEventsSubscribed &&
                health != null)
            {
                health.DamageTaken +=
                    HandleDamageTaken;

                health.Died +=
                    HandleDied;

                health.Revived +=
                    HandleRevived;

                healthEventsSubscribed =
                    true;
            }

            if (!respawnEventSubscribed &&
                respawn != null)
            {
                respawn.RespawnReady +=
                    HandleRespawnReady;

                respawnEventSubscribed =
                    true;
            }
        }

        private void UnsubscribeFromEvents()
        {
            if (healthEventsSubscribed &&
                health != null)
            {
                health.DamageTaken -=
                    HandleDamageTaken;

                health.Died -=
                    HandleDied;

                health.Revived -=
                    HandleRevived;

                healthEventsSubscribed =
                    false;
            }

            if (respawnEventSubscribed &&
                respawn != null)
            {
                respawn.RespawnReady -=
                    HandleRespawnReady;

                respawnEventSubscribed =
                    false;
            }
        }

        private void SetLifeState(
            EnemyLifeState newState)
        {
            if (lifeState == newState)
            {
                return;
            }

            EnemyLifeState previousState =
                lifeState;

            lifeState =
                newState;

            if (showDebugLogs)
            {
                Debug.Log(
                    $"[ENEMY CONTROLLER] {name}: " +
                    $"{previousState} → {newState}",
                    this);
            }
        }

        private void ValidateReferences()
        {
            if (context == null)
            {
                Debug.LogError(
                    $"{nameof(EnemyController)} requires a " +
                    $"{nameof(CharacterContext)}.",
                    this);
            }

            if (health == null)
            {
                Debug.LogError(
                    $"{nameof(EnemyController)} requires a " +
                    $"{nameof(HealthController)}.",
                    this);
            }

            if (ai == null)
            {
                Debug.LogError(
                    $"{nameof(EnemyController)} requires an " +
                    $"{nameof(EnemyAIController)}.",
                    this);
            }

            if (movement == null)
            {
                Debug.LogError(
                    $"{nameof(EnemyController)} requires an " +
                    $"{nameof(EnemyMovementController)}.",
                    this);
            }

            if (combat == null)
            {
                Debug.LogError(
                    $"{nameof(EnemyController)} requires a " +
                    $"{nameof(CombatController)}.",
                    this);
            }

            if (respawn == null)
            {
                Debug.LogError(
                    $"{nameof(EnemyController)} requires an " +
                    $"{nameof(EnemyRespawnController)}.",
                    this);
            }
        }
    }
}
