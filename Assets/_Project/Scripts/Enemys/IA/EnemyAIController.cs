using System;
using Riftborn.Characters.Combat;
using Riftborn.Characters.Core;
using Riftborn.Damage;
using UnityEngine;

namespace Riftborn.Enemies.AI
{
    public enum EnemyAIState
    {
        Idle = 0,
        Chase = 1,
        Attack = 2,
        Return = 3,
        Dead = 4
    }
    [Serializable]
    public sealed class EnemyAIController
    {
        [Header("Target")]
        [NonSerialized]
        private CharacterContext target;

        [SerializeField]
        private bool findPlayerByTag = true;

        [SerializeField]
        private string playerTag = "Player";

        [Header("Detection")]
        [SerializeField, Min(0f)]
        private float detectionRange = 8f;

        [SerializeField, Min(0f)]
        private float leashRange = 14f;

        [Header("Attack")]
        [SerializeField, Min(0f)]
        private float resumeChaseBuffer = 0.25f;

        [Header("Return")]
        [SerializeField, Min(0.01f)]
        private float returnStoppingDistance = 0.2f;

        [Header("References")]
        [NonSerialized]
        private CharacterContext context;

        [NonSerialized]
        private CombatController combatForGizmos;

        [NonSerialized]
        private Transform ownerTransform;

        [Header("Runtime Debug")]
        [SerializeField]
        private EnemyAIState currentState = EnemyAIState.Idle;

        [SerializeField]
        private bool showDebugLogs = true;

        private Vector3 spawnPosition;
        private bool warnedAboutMissingPlayerTag;

        public EnemyAIState CurrentState =>
            currentState;

        public CharacterContext CurrentTarget =>
            target;

        public Vector3 SpawnPosition =>
            spawnPosition;

        public float DetectionRange =>
            detectionRange;

        public float LeashRange =>
            leashRange;

        public void Initialize(CharacterContext owner, CombatController combat)
        {
            context = owner;
            ownerTransform = owner != null ? owner.transform : null;
            combatForGizmos = combat ?? combatForGizmos;
            if (ownerTransform != null)
            {
                spawnPosition =
                    ownerTransform.position;
            }
        }

        public void Validate()
        {
            detectionRange =
                Mathf.Max(
                    0f,
                    detectionRange);

            leashRange =
                Mathf.Max(
                    detectionRange,
                    leashRange);

            resumeChaseBuffer =
                Mathf.Max(
                    0f,
                    resumeChaseBuffer);

            returnStoppingDistance =
                Mathf.Max(
                    0.01f,
                    returnStoppingDistance);
        }

        /// <summary>
        /// Atualiza somente a decisão da IA. Este componente não move,
        /// não ataca e não controla vida ou respawn.
        /// </summary>
        public void Tick(
            bool targetIsInsideAttackRange,
            float attackRange)
        {
            if (currentState ==
                EnemyAIState.Dead)
            {
                return;
            }

            if (context == null ||
                ownerTransform == null)
            {
                Debug.LogError(
                    "[ENEMY AI] Tick chamado antes de Initialize. " +
                    "EnemyController deve inicializar a AI antes do Update.",
                    context);

                return;
            }

            switch (currentState)
            {
                case EnemyAIState.Idle:
                    TickIdle();
                    break;

                case EnemyAIState.Chase:
                    TickChase(
                        targetIsInsideAttackRange);
                    break;

                case EnemyAIState.Attack:
                    TickAttack(
                        attackRange);
                    break;

                case EnemyAIState.Return:
                    TickReturn();
                    break;
            }
        }

        public void NotifyDamageTaken(
            DamageResult damageResult)
        {
            if (currentState ==
                EnemyAIState.Dead)
            {
                return;
            }

            CharacterContext attacker =
                damageResult?
                    .Request?
                    .Source;

            if (!IsValidTarget(attacker))
            {
                return;
            }

            target = attacker;

            ChangeState(
                EnemyAIState.Chase);
        }

        public void NotifyDeath()
        {
            target = null;

            ForceState(
                EnemyAIState.Dead);
        }

        public void ResetAfterRespawn()
        {
            target = null;

            ForceState(
                EnemyAIState.Idle);

            if (showDebugLogs)
            {
                Debug.Log(
                    $"[ENEMY AI] {context?.name} foi reiniciado " +
                    "e está pronto para receber ordens do EnemyController.", context);
            }
        }

        public bool SetTarget(
            CharacterContext newTarget)
        {
            if (!IsValidTarget(newTarget))
            {
                return false;
            }

            target = newTarget;

            if (currentState !=
                EnemyAIState.Dead)
            {
                ChangeState(
                    EnemyAIState.Chase);
            }

            return true;
        }

        public void ClearTarget()
        {
            target = null;
        }

        public void BeginReturn()
        {
            if (currentState ==
                EnemyAIState.Dead)
            {
                return;
            }

            target = null;

            ChangeState(
                EnemyAIState.Return);
        }

        public void SetSpawnPosition(
            Vector3 newSpawnPosition)
        {
            spawnPosition =
                newSpawnPosition;
        }

        private void TickIdle()
        {
            if (!IsValidTarget(target))
            {
                TryFindPlayer();
            }

            if (!IsValidTarget(target))
            {
                return;
            }

            float distanceToTarget =
                HorizontalDistance(
                    ownerTransform.position,
                    target.transform.position);

            if (distanceToTarget <=
                detectionRange)
            {
                ChangeState(
                    EnemyAIState.Chase);
            }
        }

        private void TickChase(
            bool targetIsInsideAttackRange)
        {
            if (!IsValidTarget(target))
            {
                BeginReturn();
                return;
            }

            if (HasExceededLeash())
            {
                BeginReturn();
                return;
            }

            if (targetIsInsideAttackRange)
            {
                ChangeState(
                    EnemyAIState.Attack);
            }
        }

        private void TickAttack(
            float attackRange)
        {
            if (!IsValidTarget(target))
            {
                BeginReturn();
                return;
            }

            if (HasExceededLeash())
            {
                BeginReturn();
                return;
            }

            float distanceToTarget =
                HorizontalDistance(
                    ownerTransform.position,
                    target.transform.position);

            float maximumAttackDistance =
                Mathf.Max(
                    0f,
                    attackRange) +
                resumeChaseBuffer;

            if (distanceToTarget >
                maximumAttackDistance)
            {
                ChangeState(
                    EnemyAIState.Chase);
            }
        }

        private void TickReturn()
        {
            float distanceToSpawn =
                HorizontalDistance(
                    ownerTransform.position,
                    spawnPosition);

            if (distanceToSpawn <=
                returnStoppingDistance)
            {
                target = null;

                ChangeState(
                    EnemyAIState.Idle);
            }
        }

        private bool HasExceededLeash()
        {
            float distanceFromSpawn =
                HorizontalDistance(
                    ownerTransform.position,
                    spawnPosition);

            return distanceFromSpawn >
                   leashRange;
        }

        private bool TryFindPlayer()
        {
            if (IsValidTarget(target))
            {
                return true;
            }

            target = null;

            if (!findPlayerByTag ||
                string.IsNullOrWhiteSpace(
                    playerTag))
            {
                return false;
            }

            GameObject playerObject;

            try
            {
                playerObject =
                    GameObject.FindGameObjectWithTag(
                        playerTag);
            }
            catch (UnityException)
            {
                if (!warnedAboutMissingPlayerTag)
                {
                    Debug.LogError(
                        $"[ENEMY AI] A tag " +
                        $"'{playerTag}' não existe.", context);

                    warnedAboutMissingPlayerTag =
                        true;
                }

                return false;
            }

            if (playerObject == null)
            {
                return false;
            }

            target =
                playerObject.GetComponent<
                    CharacterContext>();

            target ??=
                playerObject.GetComponentInParent<
                    CharacterContext>();

            target ??=
                playerObject.GetComponentInChildren<
                    CharacterContext>();

            return IsValidTarget(target);
        }

        private bool IsValidTarget(
            CharacterContext candidate)
        {
            if (candidate == null ||
                ReferenceEquals(
                    candidate,
                    context))
            {
                return false;
            }

            return candidate.Health != null &&
                   !candidate.Health.IsDead;
        }

        private void ChangeState(
            EnemyAIState newState)
        {
            if (currentState == newState)
            {
                return;
            }

            EnemyAIState previousState =
                currentState;

            currentState =
                newState;

            LogStateChange(
                previousState,
                newState);
        }

        private void ForceState(
            EnemyAIState newState)
        {
            EnemyAIState previousState =
                currentState;

            currentState =
                newState;

            if (previousState != newState)
            {
                LogStateChange(
                    previousState,
                    newState);
            }
        }

        private void LogStateChange(
            EnemyAIState previousState,
            EnemyAIState newState)
        {
            if (!showDebugLogs)
            {
                return;
            }

            Debug.Log(
                $"[ENEMY AI] {context?.name}: " +
                $"{previousState} → {newState}", context);
        }

        private void CacheReferences()
        {
        }

        private static float HorizontalDistance(
            Vector3 first,
            Vector3 second)
        {
            first.y = 0f;
            second.y = 0f;

            return Vector3.Distance(
                first,
                second);
        }

    }
}
