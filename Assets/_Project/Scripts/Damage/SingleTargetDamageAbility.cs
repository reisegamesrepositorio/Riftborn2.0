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

        [Header("Projectile")]
        [SerializeField]
        private SingleTargetDamageProjectile projectilePrefab;

        [Tooltip(
            "Altura em que o projétil nasce em relação ao personagem.")]
        [SerializeField, Min(0f)]
        private float projectileSpawnHeight = 0.7f;

        [Tooltip(
            "Distância em que o projétil nasce na direção do alvo.")]
        [SerializeField, Min(0f)]
        private float projectileForwardOffset = 0.5f;

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
            if (!base.CanExecute(
                    caster,
                    target))
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

            if (!target.gameObject.activeInHierarchy)
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

            if (projectilePrefab == null)
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
            if (!CanExecute(
                    caster,
                    target))
            {
                if (projectilePrefab == null)
                {
                    Debug.LogError(
                        $"[ABILITY] A habilidade '{AbilityId}' " +
                        "não possui Projectile Prefab configurado.",
                        caster);
                }

                return false;
            }

            float rawAbilityDamage =
                CalculateRawAbilityDamage();

            float finalAbilityDamage =
                caster.Abilities != null
                    ? caster.Abilities.ApplyDamageModifiers(
                        rawAbilityDamage)
                    : rawAbilityDamage;

            DamageRequest damageRequest =
                CreateDamageRequest(
                    caster,
                    target,
                    finalAbilityDamage);

            Vector3 directionToTarget =
                target.transform.position -
                caster.transform.position;

            directionToTarget.y = 0f;

            if (directionToTarget.sqrMagnitude <=
                Mathf.Epsilon)
            {
                directionToTarget =
                    caster.transform.forward;
            }

            directionToTarget.Normalize();

            Vector3 spawnPosition =
                caster.transform.position +
                Vector3.up *
                projectileSpawnHeight +
                directionToTarget *
                projectileForwardOffset;

            Quaternion spawnRotation =
                Quaternion.LookRotation(
                    directionToTarget,
                    Vector3.up);

            SingleTargetDamageProjectile projectile =
                Instantiate(
                    projectilePrefab,
                    spawnPosition,
                    spawnRotation);

            if (projectile == null)
            {
                Debug.LogError(
                    $"[ABILITY] Não foi possível criar o projétil " +
                    $"da habilidade '{AbilityId}'.",
                    caster);

                return false;
            }

            projectile.Initialize(
                damageRequest,
                target);

            caster.AnimationController?.
                PlayAbility();

            Debug.Log(
                $"[ABILITY] {AbilityId} lançado contra " +
                $"{target.name} | " +
                $"Dano original: " +
                $"{rawAbilityDamage:0.##} | " +
                $"Dano com modificadores: " +
                $"{finalAbilityDamage:0.##}.",
                caster);

            return true;
        }

        private float CalculateRawAbilityDamage()
        {
            float safeBaseDamage =
                Mathf.Max(
                    0f,
                    baseDamage);

            float safeScaling =
                Mathf.Max(
                    0f,
                    scaling);

            return safeBaseDamage *
                   safeScaling;
        }

        private DamageRequest CreateDamageRequest(
            CharacterContext caster,
            CharacterContext target,
            float finalAbilityDamage)
        {
            return new DamageRequest
            {
                Source = caster,
                Target = target,

                /*
                 * O scaling já foi calculado antes e o bônus
                 * de Ability Damage já foi aplicado.
                 *
                 * O DamageCalculator recebe o valor final
                 * pré-crítico e pré-mitigação.
                 */
                BaseValue =
                    Mathf.Max(
                        0f,
                        finalAbilityDamage),

                Scaling = 1f,

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

                Origin =
                    DamageOrigin.Ability,

                OriginObject = this
            };
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
                Mathf.Max(
                    0f,
                    baseDamage);

            scaling =
                Mathf.Max(
                    0f,
                    scaling);

            range =
                Mathf.Max(
                    0f,
                    range);

            projectileSpawnHeight =
                Mathf.Max(
                    0f,
                    projectileSpawnHeight);

            projectileForwardOffset =
                Mathf.Max(
                    0f,
                    projectileForwardOffset);

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