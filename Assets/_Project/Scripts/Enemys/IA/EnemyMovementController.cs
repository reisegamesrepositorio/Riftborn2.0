using Riftborn.Characters.ActionStates;
using UnityEngine;

namespace Riftborn.Enemies.Movement
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(CharacterController))]
    public sealed class EnemyMovementController : MonoBehaviour
    {
        [Header("Movement")]
        [SerializeField, Min(0f)]
        private float moveSpeed = 3f;

        [SerializeField, Min(0f)]
        private float rotationSpeed = 720f;

        [Header("Gravity")]
        [SerializeField]
        private float gravity = -25f;

        [SerializeField]
        private float groundedStickForce = -2f;

        [Header("References")]
        [SerializeField]
        private CharacterController characterController;

        [SerializeField]
        private ActionStateController actionState;

        private float verticalVelocity;

        public float MoveSpeed =>
            moveSpeed;

        public bool CanMove =>
            actionState == null ||
            actionState.CanMove;

        private void Awake()
        {
            CacheReferences();
        }

        private void Reset()
        {
            CacheReferences();
        }

        private void OnValidate()
        {
            moveSpeed =
                Mathf.Max(
                    0f,
                    moveSpeed);

            rotationSpeed =
                Mathf.Max(
                    0f,
                    rotationSpeed);

            gravity =
                Mathf.Min(
                    0f,
                    gravity);

            groundedStickForce =
                Mathf.Min(
                    0f,
                    groundedStickForce);
        }

        public void MoveTo(
            Vector3 destination,
            float deltaTime)
        {
            CacheReferences();

            if (!CanMove)
            {
                Stop(deltaTime);
                return;
            }

            Vector3 direction =
                destination -
                transform.position;

            direction.y = 0f;

            if (direction.sqrMagnitude <= 0.0001f)
            {
                Stop(deltaTime);
                return;
            }

            direction.Normalize();

            FaceDirection(
                direction,
                deltaTime);

            Vector3 horizontalVelocity =
                direction *
                moveSpeed;

            ApplyMovement(
                horizontalVelocity,
                deltaTime);
        }

        public void Stop(float deltaTime)
        {
            CacheReferences();

            ApplyMovement(
                Vector3.zero,
                deltaTime);
        }

        public void FaceDirection(
            Vector3 direction,
            float deltaTime)
        {
            if (!CanMove)
            {
                return;
            }

            direction.y = 0f;

            if (direction.sqrMagnitude <= 0.0001f)
            {
                return;
            }

            Quaternion targetRotation =
                Quaternion.LookRotation(
                    direction.normalized,
                    Vector3.up);

            transform.rotation =
                Quaternion.RotateTowards(
                    transform.rotation,
                    targetRotation,
                    rotationSpeed *
                    deltaTime);
        }

        public void Teleport(Vector3 position)
        {
            CacheReferences();

            if (characterController == null)
            {
                transform.position =
                    position;

                verticalVelocity = 0f;
                return;
            }

            bool wasEnabled =
                characterController.enabled;

            characterController.enabled =
                false;

            transform.position =
                position;

            characterController.enabled =
                wasEnabled;

            verticalVelocity = 0f;
        }

        private void ApplyMovement(
            Vector3 horizontalVelocity,
            float deltaTime)
        {
            if (characterController == null ||
                !characterController.enabled)
            {
                return;
            }

            float safeDeltaTime =
                Mathf.Max(
                    0f,
                    deltaTime);

            if (characterController.isGrounded &&
                verticalVelocity < 0f)
            {
                verticalVelocity =
                    groundedStickForce;
            }
            else
            {
                verticalVelocity +=
                    gravity *
                    safeDeltaTime;
            }

            Vector3 velocity =
                horizontalVelocity;

            velocity.y =
                verticalVelocity;

            CollisionFlags collisionFlags =
                characterController.Move(
                    velocity *
                    safeDeltaTime);

            if ((collisionFlags &
                 CollisionFlags.Below) != 0 &&
                verticalVelocity < 0f)
            {
                verticalVelocity =
                    groundedStickForce;
            }
        }

        private void CacheReferences()
        {
            characterController ??=
                GetComponent<CharacterController>();

            actionState ??=
                GetComponent<ActionStateController>();
        }
    }
}