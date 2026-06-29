using Riftborn.Characters.Core;
using Riftborn.Damage;
using UnityEngine;

namespace Riftborn.Characters.Abilities
{
    public sealed class SingleTargetDamageProjectile :
        MonoBehaviour
    {
        [Header("Movement")]
        [SerializeField, Min(0.1f)]
        private float movementSpeed = 8f;

        [Tooltip(
            "Distância do centro do alvo em que o projétil " +
            "considera que acertou.")]
        [SerializeField, Min(0.01f)]
        private float hitDistance = 0.45f;

        [SerializeField, Min(0.1f)]
        private float maximumLifetime = 5f;

        [SerializeField]
        private bool followMovingTarget = true;

        [SerializeField]
        private bool rotateTowardMovement = true;

        [Header("Target Position")]
        [Tooltip(
            "Altura do ponto que o projétil persegue no alvo.")]
        [SerializeField]
        private Vector3 targetOffset =
            new Vector3(0f, 0.5f, 0f);

        [Header("Impact VFX")]
        [SerializeField]
        private GameObject impactVfxPrefab;

        [SerializeField, Min(0f)]
        private float impactVfxLifetime = 1.5f;

        [Tooltip(
            "Afasta o VFX do centro do alvo na direção " +
            "de onde o projétil chegou.")]
        [SerializeField, Min(0f)]
        private float impactSurfaceOffset = 0.65f;

        [Tooltip(
            "Ajuste adicional aplicado depois do afastamento " +
            "da superfície.")]
        [SerializeField]
        private Vector3 impactVfxOffset =
            new Vector3(0f, 0.1f, 0f);

        [SerializeField]
        private bool useProjectileRotationForImpact;

        private DamageRequest damageRequest;
        private CharacterContext target;

        private Vector3 lockedTargetPosition;
        private float elapsedLifetime;
        private bool initialized;
        private bool impactProcessed;

        public void Initialize(
            DamageRequest request,
            CharacterContext targetCharacter)
        {
            damageRequest =
                request;

            target =
                targetCharacter;

            if (target != null)
            {
                lockedTargetPosition =
                    target.transform.position +
                    targetOffset;
            }

            elapsedLifetime = 0f;
            initialized = true;
            impactProcessed = false;
        }

        private void Update()
        {
            if (!initialized ||
                impactProcessed)
            {
                return;
            }

            elapsedLifetime +=
                Time.deltaTime;

            if (elapsedLifetime >= maximumLifetime)
            {
                Destroy(gameObject);
                return;
            }

            if (!IsTargetStillValid())
            {
                Destroy(gameObject);
                return;
            }

            Vector3 destination =
                GetTargetPosition();

            MoveToward(destination);

            float remainingDistance =
                Vector3.Distance(
                    transform.position,
                    destination);

            if (remainingDistance <= hitDistance)
            {
                ProcessImpact(destination);
            }
        }

        private bool IsTargetStillValid()
        {
            if (target == null)
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

            return true;
        }

        private Vector3 GetTargetPosition()
        {
            if (followMovingTarget &&
                target != null)
            {
                return target.transform.position +
                       targetOffset;
            }

            return lockedTargetPosition;
        }

        private void MoveToward(
            Vector3 destination)
        {
            Vector3 movementDirection =
                destination -
                transform.position;

            if (movementDirection.sqrMagnitude >
                Mathf.Epsilon &&
                rotateTowardMovement)
            {
                transform.rotation =
                    Quaternion.LookRotation(
                        movementDirection.normalized,
                        Vector3.up);
            }

            transform.position =
                Vector3.MoveTowards(
                    transform.position,
                    destination,
                    movementSpeed *
                    Time.deltaTime);
        }

        private void ProcessImpact(
            Vector3 targetPosition)
        {
            if (impactProcessed)
            {
                return;
            }

            impactProcessed = true;

            if (!IsTargetStillValid() ||
                damageRequest == null)
            {
                Destroy(gameObject);
                return;
            }

            Vector3 impactDirection =
                targetPosition -
                transform.position;

            if (impactDirection.sqrMagnitude <=
                Mathf.Epsilon)
            {
                impactDirection =
                    transform.forward;
            }
            else
            {
                impactDirection.Normalize();
            }

            /*
             * O ponto começa no centro do alvo e volta na
             * direção de onde a Fireball chegou.
             *
             * Isso coloca o VFX na superfície frontal,
             * em vez de dentro do personagem.
             */
            Vector3 impactPosition =
                targetPosition -
                impactDirection *
                impactSurfaceOffset +
                impactVfxOffset;

            Quaternion impactRotation =
                useProjectileRotationForImpact
                    ? Quaternion.LookRotation(
                        impactDirection,
                        Vector3.up)
                    : Quaternion.identity;

            DamageResult result =
                DamageCalculator.Calculate(
                    damageRequest);

            DamageApplicationResult application =
                target.Health.ApplyDamage(result);

            if (application != null)
            {
                SpawnImpactVfx(
                    impactPosition,
                    impactRotation);

                string abilityName =
                    damageRequest.OriginObject
                        is AbilityBase ability
                            ? ability.AbilityId
                            : "projectile";

                Debug.Log(
                    $"[PROJECTILE] {abilityName} atingiu " +
                    $"{target.name} por " +
                    $"{application.HealthDamage:0.##} de dano. " +
                    $"Vida restante: " +
                    $"{target.Health.CurrentHealth:0.##}/" +
                    $"{target.Health.MaxHealth:0.##}",
                    target);
            }

            Destroy(gameObject);
        }

        private void SpawnImpactVfx(
            Vector3 position,
            Quaternion rotation)
        {
            if (impactVfxPrefab == null)
            {
                return;
            }

            GameObject impactInstance =
                Instantiate(
                    impactVfxPrefab,
                    position,
                    rotation);

            if (impactInstance == null)
            {
                return;
            }

            if (impactVfxLifetime > 0f)
            {
                Destroy(
                    impactInstance,
                    impactVfxLifetime);
            }
        }

        private void OnValidate()
        {
            movementSpeed =
                Mathf.Max(
                    0.1f,
                    movementSpeed);

            hitDistance =
                Mathf.Max(
                    0.01f,
                    hitDistance);

            maximumLifetime =
                Mathf.Max(
                    0.1f,
                    maximumLifetime);

            impactVfxLifetime =
                Mathf.Max(
                    0f,
                    impactVfxLifetime);

            impactSurfaceOffset =
                Mathf.Max(
                    0f,
                    impactSurfaceOffset);
        }
    }
}