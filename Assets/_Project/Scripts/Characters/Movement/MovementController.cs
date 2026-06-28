using Riftborn.Characters.ActionStates;
using Riftborn.Characters.Stats;
using UnityEngine;

namespace Riftborn.Characters.Movement
{
    public sealed class MovementController : MonoBehaviour
    {
        [SerializeField] private MovementMode movementMode = MovementMode.WASD;
        [SerializeField] private float moveSpeed = 5f;
        [SerializeField] private float dexSpeedBonus = 0.02f;
        [SerializeField] private float rotationSpeed = 14f;
        [SerializeField] private float gravity = -25f;
        [SerializeField] private float groundedStickForce = -2f;
        [SerializeField] private ActionStateController actionState;
        [SerializeField] private CharacterStatsController stats;
        [SerializeField] private CharacterController characterController;
        [SerializeField] private Transform cameraTransform;

        private Vector2 moveInput;
        private float verticalVelocity;

        public MovementMode MovementMode => movementMode;
        public float MoveSpeed => moveSpeed;
        public bool CanMove => actionState == null || actionState.CanMove;
        public Vector2 MoveInput => moveInput;

        private void Awake()
        {
            CacheReferences();
        }

        private void Update()
        {
            TickMovement(Time.deltaTime);
        }

        public void SetMoveInput(Vector2 input)
        {
            moveInput = Vector2.ClampMagnitude(input, 1f);
        }

        public void Move(Vector3 direction, float deltaTime)
        {
            CacheReferences();
            MoveWorldDirection(direction, deltaTime);
        }

        public void Teleport(Vector3 position)
        {
            if (characterController != null)
            {
                characterController.enabled = false;
                transform.position = position;
                characterController.enabled = true;
                verticalVelocity = 0f;
                return;
            }

            transform.position = position;
            verticalVelocity = 0f;
        }

        public void TickMovement(float deltaTime)
        {
            CacheReferences();

            if (movementMode != MovementMode.WASD)
            {
                ApplyGravityOnly(deltaTime);
                return;
            }

            Vector3 direction = CanMove ? GetCameraRelativeDirection(moveInput) : Vector3.zero;
            MoveWorldDirection(direction, deltaTime);
        }

        public Vector3 GetCameraRelativeDirection(Vector2 input)
        {
            Vector2 clampedInput = Vector2.ClampMagnitude(input, 1f);
            if (clampedInput.sqrMagnitude <= 0.0001f)
            {
                return Vector3.zero;
            }

            Transform reference = cameraTransform != null ? cameraTransform : Camera.main != null ? Camera.main.transform : null;
            Vector3 forward = reference != null ? reference.forward : Vector3.forward;
            Vector3 right = reference != null ? reference.right : Vector3.right;

            forward.y = 0f;
            right.y = 0f;
            forward = forward.sqrMagnitude > 0.0001f ? forward.normalized : Vector3.forward;
            right = right.sqrMagnitude > 0.0001f ? right.normalized : Vector3.right;

            Vector3 direction = forward * clampedInput.y + right * clampedInput.x;
            return direction.sqrMagnitude > 1f ? direction.normalized : direction;
        }

        private void MoveWorldDirection(Vector3 direction, float deltaTime)
        {
            direction.y = 0f;
            if (direction.sqrMagnitude > 1f)
            {
                direction.Normalize();
            }

            float speed = GetCurrentMoveSpeed();
            Vector3 horizontalMotion = CanMove ? direction * speed : Vector3.zero;
            Vector3 verticalMotion = GetGravityMotion(deltaTime);
            Vector3 motion = (horizontalMotion + verticalMotion) * deltaTime;

            if (characterController != null && characterController.enabled)
            {
                characterController.Move(motion);
            }
            else
            {
                transform.position += motion;
            }

            if (CanMove && direction.sqrMagnitude > 0.0001f)
            {
                RotateTowards(direction, deltaTime);
            }
        }

        private void ApplyGravityOnly(float deltaTime)
        {
            MoveWorldDirection(Vector3.zero, deltaTime);
        }

        private Vector3 GetGravityMotion(float deltaTime)
        {
            if (characterController != null && characterController.isGrounded && verticalVelocity < 0f)
            {
                verticalVelocity = groundedStickForce;
            }
            else
            {
                verticalVelocity += gravity * deltaTime;
            }

            return Vector3.up * verticalVelocity;
        }

        private float GetCurrentMoveSpeed()
        {
            float speed = moveSpeed;
            if (stats != null)
            {
                speed += Mathf.Max(0f, stats.GetFinalValue(CharacterStat.DEX) * dexSpeedBonus);
            }

            return Mathf.Max(0f, speed);
        }

        private void RotateTowards(Vector3 direction, float deltaTime)
        {
            Quaternion targetRotation = Quaternion.LookRotation(direction, Vector3.up);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Mathf.Clamp01(rotationSpeed * deltaTime));
        }

        private void CacheReferences()
        {
            actionState ??= GetComponent<ActionStateController>();
            stats ??= GetComponent<CharacterStatsController>();
            characterController ??= GetComponent<CharacterController>();
            if (cameraTransform == null && Camera.main != null)
            {
                cameraTransform = Camera.main.transform;
            }
        }
    }
}
