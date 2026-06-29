using Riftborn.Characters.Core;
using Riftborn.Characters.Stats;
using Riftborn.Damage;
using UnityEngine;

namespace Riftborn.Characters.Abilities
{
    [CreateAssetMenu(
        fileName = "TargetedDamageAbility",
        menuName = "Riftborn/Abilities/Targeted Damage Ability")]
    public sealed class TargetedDamageAbility : AbilityBase
    {
        [Header("Targeting")]
        [SerializeField, Min(0f)]
        private float castRange = 8f;

        [Header("Damage")]
        [SerializeField, Min(0f)]
        private float baseDamage = 20f;

        [SerializeField]
        private DamageType damageType =
            DamageType.Magical;

        [Header("Attribute Scaling")]
        [SerializeField]
        private bool usesAttributeScaling = true;

        [SerializeField]
        private CharacterStat scalingAttribute =
            CharacterStat.WIS;

        [SerializeField, Min(0f)]
        private float damagePerAttributePoint = 1f;

        [Header("Critical")]
        [SerializeField]
        private bool canCrit = true;

        [SerializeField, Range(0f, 1f)]
        private float criticalChance = 0.05f;

        [SerializeField, Min(1f)]
        private float criticalMultiplier = 1.5f;

        [Header("Penetration")]
        [SerializeField, Range(0f, 1f)]
        private float percentPenetration;

        [SerializeField, Min(0f)]
        private float flatPenetration;

        public float CastRange =>
            castRange;

        public float BaseDamage =>
            baseDamage;

        public DamageType DamageType =>
            damageType;

        public override bool CanExecute(
            CharacterContext caster,
            CharacterContext target)
        {
            if (!base.CanExecute(caster, target))
            {
                return false;
            }

            if (target == null)
            {
                return false;
            }

            if (ReferenceEquals(caster, target))
            {
                return false;
            }

            if (target.Health == null ||
                target.Health.IsDead)
            {
                return false;
            }

            float distance =
                Vector3.Distance(
                    caster.transform.position,
                    target.transform.position);

            return distance <= castRange;
        }

        public override bool Execute(
            CharacterContext caster,
            CharacterContext target)
        {
            /*
             * Execute também valida para permanecer seguro
             * caso seja chamado diretamente por outro sistema.
             */
            if (!CanExecute(caster, target))
            {
                return false;
            }

            float calculatedDamage =
                CalculateDamage(caster);

            if (calculatedDamage <= 0f)
            {
                return false;
            }

            DamageRequest request =
                new DamageRequest
                {
                    Source = caster,
                    Target = target,

                    BaseValue =
                        calculatedDamage,

                    Type =
                        damageType,

                    Tags =
                        BuildDamageTags(),

                    Scaling = 1f,

                    CanCrit =
                        canCrit,

                    CriticalChance =
                        canCrit
                            ? criticalChance
                            : 0f,

                    CriticalMultiplier =
                        criticalMultiplier,

                    PercentPenetration =
                        percentPenetration,

                    FlatPenetration =
                        flatPenetration,

                    Origin =
                        DamageOrigin.Ability,

                    OriginObject =
                        this
                };

            DamageResult damageResult =
                DamageCalculator.Calculate(request);

            DamageApplicationResult applicationResult =
                target.Health.ApplyDamage(damageResult);

            if (applicationResult == null)
            {
                return false;
            }

            /*
             * Mesmo quando o Shield absorve todo o dano,
             * o ataque foi executado e processado.
             */
            caster.Events?.
                RaiseDamageDealt(damageResult);

            /*
             * O alvo recebe esse evento somente quando
             * houve perda real de HP.
             */
            if (applicationResult.DamagedHealth)
            {
                target.Events?.
                    RaiseDamageTaken(damageResult);
            }

            if (damageResult.WasCritical)
            {
                caster.Events?.
                    RaiseCriticalHit(damageResult);
            }

            return true;
        }

        private float CalculateDamage(
            CharacterContext caster)
        {
            float calculatedDamage =
                baseDamage;

            if (!usesAttributeScaling ||
                caster?.Stats == null)
            {
                return Mathf.Max(
                    0f,
                    calculatedDamage);
            }

            float finalAttribute =
                caster.Stats.GetFinalValue(
                    scalingAttribute);

            calculatedDamage +=
                finalAttribute *
                damagePerAttributePoint;

            return Mathf.Max(
                0f,
                calculatedDamage);
        }

        private DamageTag BuildDamageTags()
        {
            DamageTag tags =
                DamageTag.SingleTarget;

            switch (damageType)
            {
                case DamageType.Physical:
                    tags |=
                        DamageTag.PhysicalAbility;
                    break;

                case DamageType.Magical:
                    tags |=
                        DamageTag.MagicalAbility;
                    break;
            }

            return tags;
        }

        protected override void OnValidate()
        {
            base.OnValidate();

            castRange =
                Mathf.Max(0f, castRange);

            baseDamage =
                Mathf.Max(0f, baseDamage);

            damagePerAttributePoint =
                Mathf.Max(
                    0f,
                    damagePerAttributePoint);

            criticalChance =
                Mathf.Clamp01(
                    criticalChance);

            criticalMultiplier =
                Mathf.Max(
                    1f,
                    criticalMultiplier);

            percentPenetration =
                Mathf.Clamp01(
                    percentPenetration);

            flatPenetration =
                Mathf.Max(
                    0f,
                    flatPenetration);
        }
    }
}