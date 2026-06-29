using Riftborn.Characters.Core;
using Riftborn.Damage;
using UnityEngine;

namespace Riftborn.Characters.Abilities
{
    [CreateAssetMenu(
        fileName = "NewSingleTargetDamageAbility",
        menuName = "Riftborn/Abilities/Single Target Damage")]
    public sealed class SingleTargetDamageAbility : AbilityBase
    {
        [Header("Damage")]
        [SerializeField, Min(0f)]
        private float baseDamage = 25f;

        [SerializeField, Min(0f)]
        private float scaling = 1f;

        [SerializeField]
        private DamageType damageType =
            DamageType.Magical;

        [Header("Targeting")]
        [SerializeField, Min(0f)]
        private float range = 8f;

        [Header("Critical")]
        [SerializeField]
        private bool canCrit;

        [SerializeField, Range(0f, 1f)]
        private float criticalChance;

        [SerializeField, Min(1f)]
        private float criticalMultiplier = 1.5f;

        [Header("Penetration")]
        [SerializeField, Range(0f, 1f)]
        private float percentPenetration;

        [SerializeField, Min(0f)]
        private float flatPenetration;

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

            if (target == caster)
            {
                return false;
            }

            if (target.Health == null)
            {
                return false;
            }

            if (target.Health.IsDead)
            {
                return false;
            }

            float distance =
                Vector3.Distance(
                    caster.transform.position,
                    target.transform.position);

            return distance <= range;
        }

        public override bool Execute(
            CharacterContext caster,
            CharacterContext target)
        {
            if (!CanExecute(caster, target))
            {
                return false;
            }

            DamageRequest request =
                new DamageRequest
                {
                    Source = caster,
                    Target = target,

                    BaseValue = baseDamage,
                    Scaling = scaling,

                    Type = damageType,
                    Tags = BuildDamageTags(),

                    CanCrit = canCrit,
                    CriticalChance = criticalChance,
                    CriticalMultiplier =
                        criticalMultiplier,

                    PercentPenetration =
                        percentPenetration,

                    FlatPenetration =
                        flatPenetration,

                    Origin = DamageOrigin.Ability,
                    OriginObject = this
                };

            DamageResult result =
                DamageCalculator.Calculate(request);

            var application =
                target.Health.ApplyDamage(result);

            if (application == null)
            {
                return false;
            }

            Debug.Log(
                $"[ABILITY] {AbilityId} atingiu " +
                $"{target.name} por " +
                $"{result.FinalAmount:0.##} de dano. " +
                $"Vida restante: " +
                $"{target.Health.CurrentHealth:0.##}/" +
                $"{target.Health.MaxHealth:0.##}",
                target);

            return true;
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

            baseDamage =
                Mathf.Max(0f, baseDamage);

            scaling =
                Mathf.Max(0f, scaling);

            range =
                Mathf.Max(0f, range);

            criticalChance =
                Mathf.Clamp01(criticalChance);

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