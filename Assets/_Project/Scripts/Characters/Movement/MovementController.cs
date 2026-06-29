using System.Collections.Generic;
using Riftborn.Characters.ActionStates;
using UnityEngine;

namespace Riftborn.Characters.Movement
{
    public sealed class MovementController : MonoBehaviour
    {
        [Header("Movement")]
        [SerializeField]
        private MovementMode movementMode = MovementMode.WASD;

        [SerializeField, Min(0f)]
        private float moveSpeed = 5f;

        [SerializeField, Min(0f)]
        private float rotationSpeed = 14f;

        [Header("Gravity")]
        [SerializeField]
        private float gravity = -25f;

        [SerializeField]
        private float groundedStickForce = -2f;

        [Header("References")]
        [SerializeField]
        private ActionStateController actionState;

        [SerializeField]
        private CharacterController characterController;

        [SerializeField]
        private Transform cameraTransform;

        private readonly Dictionary<object, float>
            slowsBySource = new();

        private Vector2 moveInput;
        private float verticalVelocity;
        private float strongestSlow;

        public MovementMode MovementMode =>
            movementMode;

        public float MoveSpeed =>
            moveSpeed;

        public float CurrentMoveSpeed =>
            GetCurrentMoveSpeed();

        public float StrongestSlow =>
            strongestSlow;

        public bool CanMove =>
            actionState == null ||
            actionState.CanMove;

        public Vector2 MoveInput =>
            moveInput;

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
            moveInput =
                Vector2.ClampMagnitude(input, 1f);
        }

        public void Move(
            Vector3 direction,
            float deltaTime)
        {
            CacheReferences();

            MoveWorldDirection(
                direction,
                deltaTime);
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

            Vector3 direction =
                CanMove
                    ? GetCameraRelativeDirection(moveInput)
                    : Vector3.zero;

            MoveWorldDirection(
                direction,
                deltaTime);
        }

        public Vector3 GetCameraRelativeDirection(
            Vector2 input)
        {
            Vector2 clampedInput =
                Vector2.ClampMagnitude(input, 1f);

            if (clampedInput.sqrMagnitude <= 0.0001f)
            {
                return Vector3.zero;
            }

            Transform reference =
                cameraTransform != null
                    ? cameraTransform
                    : Camera.main != null
                        ? Camera.main.transform
                        : null;

            Vector3 forward =
                reference != null
                    ? reference.forward
                    : Vector3.forward;

            Vector3 right =
                reference != null
                    ? reference.right
                    : Vector3.right;

            forward.y = 0f;
            right.y = 0f;

            forward =
                forward.sqrMagnitude > 0.0001f
                    ? forward.normalized
                    : Vector3.forward;

            right =
                right.sqrMagnitude > 0.0001f
                    ? right.normalized
                    : Vector3.right;

            Vector3 direction =
                forward * clampedInput.y +
                right * clampedInput.x;

            return direction.sqrMagnitude > 1f
                ? direction.normalized
                : direction;
        }

        public bool AddOrUpdateSlow(
            object source,
            float slowPercent)
        {
            if (source == null)
            {
                return false;
            }

            float safeSlow =
                Mathf.Clamp(
                    slowPercent,
                    0f,
                    0.95f);

            if (safeSlow <= 0f)
            {
                return RemoveSlow(source);
            }

            if (slowsBySource.TryGetValue(
                    source,
                    out float existingSlow) &&
                Mathf.Approximately(
                    existingSlow,
                    safeSlow))
            {
                return false;
            }

            slowsBySource[source] = safeSlow;

            RecalculateStrongestSlow();

            return true;
        }

        public bool RemoveSlow(object source)
        {
            if (source == null)
            {
                return false;
            }

            if (!slowsBySource.Remove(source))
            {
                return false;
            }

            RecalculateStrongestSlow();

            return true;
        }

        public bool HasSlowFromSource(object source)
        {
            return source != null &&
                   slowsBySource.ContainsKey(source);
        }

        public float GetSlowFromSource(object source)
        {
            if (source == null)
            {
                return 0f;
            }

            return slowsBySource.TryGetValue(
                source,
                out float slowPercent)
                    ? slowPercent
                    : 0f;
        }

        public void ClearAllSlows()
        {
            if (slowsBySource.Count == 0)
            {
                return;
            }

            slowsBySource.Clear();
            strongestSlow = 0f;
        }

        private void MoveWorldDirection(
            Vector3 direction,
            float deltaTime)
        {
            direction.y = 0f;

            if (direction.sqrMagnitude > 1f)
            {
                direction.Normalize();
            }

            float currentSpeed =
                GetCurrentMoveSpeed();

            Vector3 horizontalMotion =
                CanMove
                    ? direction * currentSpeed
                    : Vector3.zero;

            Vector3 verticalMotion =
                GetGravityMotion(deltaTime);

            Vector3 motion =
                (horizontalMotion + verticalMotion) *
                deltaTime;

            if (characterController != null &&
                characterController.enabled)
            {
                characterController.Move(motion);
            }
            else
            {
                transform.position += motion;
            }

            if (CanMove &&
                direction.sqrMagnitude > 0.0001f)
            {
                RotateTowards(
                    direction,
                    deltaTime);
            }
        }

        private void ApplyGravityOnly(float deltaTime)
        {
            MoveWorldDirection(
                Vector3.zero,
                deltaTime);
        }

        private Vector3 GetGravityMotion(
            float deltaTime)
        {
            if (characterController != null &&
                characterController.isGrounded &&
                verticalVelocity < 0f)
            {
                verticalVelocity =
                    groundedStickForce;
            }
            else
            {
                verticalVelocity +=
                    gravity * deltaTime;
            }

            return Vector3.up * verticalVelocity;
        }

        private float GetCurrentMoveSpeed()
        {
            float currentSpeed =
                moveSpeed * (1f - strongestSlow);

            return Mathf.Max(
                0f,
                currentSpeed);
        }

        private void RecalculateStrongestSlow()
        {
            float newStrongestSlow = 0f;

            foreach (float slowPercent in slowsBySource.Values)
            {
                if (slowPercent > newStrongestSlow)
                {
                    newStrongestSlow =
                        slowPercent;
                }
            }

            strongestSlow =
                Mathf.Clamp(
                    newStrongestSlow,
                    0f,
                    0.95f);
        }

        private void RotateTowards(
            Vector3 direction,
            float deltaTime)
        {
            Quaternion targetRotation =
                Quaternion.LookRotation(
                    direction,
                    Vector3.up);

            transform.rotation =
                Quaternion.Slerp(
                    transform.rotation,
                    targetRotation,
                    Mathf.Clamp01(
                        rotationSpeed * deltaTime));
        }

        private void CacheReferences()
        {
            actionState ??=
                GetComponent<ActionStateController>();

            characterController ??=
                GetComponent<CharacterController>();

            if (cameraTransform == null &&
                Camera.main != null)
            {
                cameraTransform =
                    Camera.main.transform;
            }
        }
    }
}