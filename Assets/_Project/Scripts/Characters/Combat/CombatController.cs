using System;
using Riftborn.Characters.ActionStates;
using Riftborn.Characters.Core;
using Riftborn.Characters.Stats;
using Riftborn.Characters.Targeting;
using Riftborn.Damage;
using UnityEngine;

namespace Riftborn.Characters.Combat
{
    public sealed class CombatController : MonoBehaviour
    {
        [Header("Basic Attack Damage")]
        [SerializeField, Min(0f)]
        private float basicAttackBaseDamage = 10f;

        [SerializeField, Min(0f)]
        private float damagePerSTR = 1f;

        [Header("Attack Speed")]
        [SerializeField, Min(0.05f)]
        private float baseAttackInterval = 1.5f;

        [SerializeField, Min(0f)]
        private float attackSpeedPerDEX = 0.02f;

        [Header("Range")]
        [SerializeField, Min(0f)]
        private float attackRange = 2f;

        [Header("Critical")]
        [SerializeField, Range(0f, 1f)]
        private float criticalChance = 0.05f;

        [SerializeField, Min(1f)]
        private float criticalMultiplier = 1.5f;

        [Header("References")]
        [SerializeField]
        private ActionStateController actionState;

        [SerializeField]
        private TargetingController targeting;

        [SerializeField]
        private CharacterStatsController stats;

        private CharacterContext context;
        private float nextAttackTime;

        public event Action BasicAttackStarted;
        public event Action<DamageResult> BasicAttackHit;

        public float BasicAttackDamage =>
            CalculateBasicAttackDamage();

        public float CurrentAttackInterval =>
            CalculateAttackInterval();

        public float AttackRange =>
            attackRange;

        public float RemainingAttackCooldown =>
            Mathf.Max(
                0f,
                nextAttackTime - Time.time);

        private void Awake()
        {
            CacheReferences();
        }

        public bool TryBasicAttack(
            CharacterContext target = null)
        {
            CacheReferences();

            target ??=
                targeting?.CurrentTarget;

            if (!CanAttack(target))
            {
                return false;
            }

            float currentAttackInterval =
                CalculateAttackInterval();

            nextAttackTime =
                Time.time + currentAttackInterval;

            BasicAttackStarted?.Invoke();

            context?.Events?.
                RaiseBasicAttackStarted();

            DamageRequest request =
                CreateBasicAttackRequest(target);

            DamageResult damageResult =
                DamageCalculator.Calculate(request);

            DamageApplicationResult applicationResult =
                target.Health.ApplyDamage(damageResult);

            BasicAttackHit?.Invoke(damageResult);

            context?.Events?.
                RaiseBasicAttackHit(damageResult);

            /*
             * Dano absorvido por Shield ainda conta como dano
             * causado pelo atacante.
             */
            if (applicationResult != null)
            {
                context?.Events?.
                    RaiseDamageDealt(damageResult);
            }

            /*
             * O alvo somente recebe o evento DamageTaken quando
             * houve perda real de HP.
             */
            if (applicationResult != null &&
                applicationResult.DamagedHealth)
            {
                target.Events?.
                    RaiseDamageTaken(damageResult);
            }

            if (damageResult.WasCritical)
            {
                context?.Events?.
                    RaiseCriticalHit(damageResult);
            }

            return true;
        }

        public bool CanAttack(
            CharacterContext target)
        {
            CacheReferences();

            if (context == null ||
                context.Health == null ||
                context.Health.IsDead)
            {
                return false;
            }

            if (actionState != null &&
                !actionState.CanAttack)
            {
                return false;
            }

            if (Time.time < nextAttackTime)
            {
                return false;
            }

            if (target == null ||
                ReferenceEquals(target, context))
            {
                return false;
            }

            if (target.Health == null ||
                target.Health.IsDead)
            {
                return false;
            }

            if (targeting != null &&
                !targeting.IsValidTarget(target))
            {
                return false;
            }

            return IsTargetInRange(target);
        }

        public bool IsTargetInRange(
            CharacterContext target)
        {
            if (target == null)
            {
                return false;
            }

            float distance =
                Vector3.Distance(
                    transform.position,
                    target.transform.position);

            return distance <= attackRange;
        }

        private DamageRequest CreateBasicAttackRequest(
            CharacterContext target)
        {
            return new DamageRequest
            {
                Source = context,
                Target = target,

                BaseValue =
                    CalculateBasicAttackDamage(),

                Type =
                    DamageType.Physical,

                Tags =
                    DamageTag.BasicAttack |
                    DamageTag.SingleTarget,

                Scaling = 1f,

                CanCrit = true,

                CriticalChance =
                    criticalChance,

                CriticalMultiplier =
                    criticalMultiplier,

                Origin =
                    DamageOrigin.BasicAttack,

                OriginObject = this
            };
        }

        private float CalculateBasicAttackDamage()
        {
            float finalSTR =
                stats != null
                    ? stats.GetFinalValue(
                        CharacterStat.STR)
                    : 0f;

            float calculatedDamage =
                basicAttackBaseDamage +
                finalSTR * damagePerSTR;

            return Mathf.Max(
                0f,
                calculatedDamage);
        }

        private float CalculateAttackInterval()
        {
            float finalDEX =
                stats != null
                    ? stats.GetFinalValue(
                        CharacterStat.DEX)
                    : 0f;

            float attackSpeedMultiplier =
                1f +
                Mathf.Max(
                    0f,
                    finalDEX) *
                attackSpeedPerDEX;

            attackSpeedMultiplier =
                Mathf.Max(
                    0.1f,
                    attackSpeedMultiplier);

            return baseAttackInterval /
                   attackSpeedMultiplier;
        }

        private void CacheReferences()
        {
            context ??=
                GetComponent<CharacterContext>();

            actionState ??=
                GetComponent<ActionStateController>();

            targeting ??=
                GetComponent<TargetingController>();

            stats ??=
                GetComponent<CharacterStatsController>();
        }
    }
}