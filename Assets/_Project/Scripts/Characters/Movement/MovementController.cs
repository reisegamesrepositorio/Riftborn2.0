using System;
using System.Collections.Generic;
using Riftborn.Characters.ActionStates;
using UnityEngine;

namespace Riftborn.Characters.Movement
{
    public sealed class MovementController : MonoBehaviour
    {
        [Header("Movement")]
        [SerializeField] private MovementMode movementMode = MovementMode.WASD;
        [SerializeField, Min(0f)] private float moveSpeed = 5f;
        [SerializeField, Min(0f)] private float rotationSpeed = 14f;

        [Header("Click To Move")]
        [SerializeField, Min(0f)] private float clickStoppingDistance = 0.15f;

        [Header("Gravity")]
        [SerializeField] private float gravity = -25f;
        [SerializeField] private float groundedStickForce = -2f;

        [Header("References")]
        [SerializeField] private ActionStateController actionState;
        [SerializeField] private CharacterController characterController;
        [SerializeField] private Transform cameraTransform;

        private readonly Dictionary<object, float> slowsBySource = new();
        private readonly List<MovementModifier> movementModifiers = new();
        private readonly Dictionary<string, MovementModifier> modifiersById =
            new(StringComparer.Ordinal);

        private Vector2 moveInput;
        private Vector3 clickDestination;
        private float verticalVelocity;
        private float strongestSlow;
        private bool clickToMoveActive;
        private bool hasClickDestination;

        public event Action MovementValuesChanged;

        public MovementMode MovementMode => movementMode;
        public bool IsClickToMoveActive => clickToMoveActive;
        public bool HasClickDestination => hasClickDestination;
        public Vector3 ClickDestination => clickDestination;
        public float MoveSpeed => moveSpeed;
        public float BaseMoveSpeed => moveSpeed;
        public float ModifiedMoveSpeed => GetModifiedMoveSpeed();
        public float CurrentMoveSpeed => GetCurrentMoveSpeed();
        public float StrongestSlow => strongestSlow;
        public bool CanMove => actionState == null || actionState.CanMove;
        public Vector2 MoveInput => moveInput;

        private void Awake()
        {
            CacheReferences();

            clickToMoveActive =
                !string.Equals(
                    movementMode.ToString(),
                    nameof(MovementMode.WASD),
                    StringComparison.OrdinalIgnoreCase);
        }

        private void Update()
        {
            TickMovement(Time.deltaTime);
        }

        private void OnValidate()
        {
            moveSpeed = Mathf.Max(0f, moveSpeed);
            rotationSpeed = Mathf.Max(0f, rotationSpeed);
            clickStoppingDistance = Mathf.Max(0f, clickStoppingDistance);
        }

        public void ActivateWasdMode()
        {
            clickToMoveActive = false;
            CancelClickDestination();
            movementMode = MovementMode.WASD;
        }

        public void ActivateClickToMoveMode()
        {
            clickToMoveActive = true;
            SetMoveInput(Vector2.zero);
            CancelClickDestination();
            TrySetMovementModeByName("ClickToMove", "ClickMove");
        }

        public void SetMoveInput(Vector2 input)
        {
            moveInput = Vector2.ClampMagnitude(input, 1f);
        }

        public bool SetClickDestination(Vector3 worldPosition)
        {
            if (float.IsNaN(worldPosition.x) ||
                float.IsNaN(worldPosition.y) ||
                float.IsNaN(worldPosition.z) ||
                float.IsInfinity(worldPosition.x) ||
                float.IsInfinity(worldPosition.y) ||
                float.IsInfinity(worldPosition.z))
            {
                return false;
            }

            clickDestination = worldPosition;
            hasClickDestination = true;
            return true;
        }

        public void CancelClickDestination()
        {
            hasClickDestination = false;
        }

        public void Move(Vector3 direction, float deltaTime)
        {
            CacheReferences();
            MoveWorldDirection(direction, deltaTime);
        }

        public void Teleport(Vector3 position)
        {
            CancelClickDestination();

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

            Vector3 direction = Vector3.zero;

            if (CanMove)
            {
                direction = clickToMoveActive
                    ? GetClickToMoveDirection(deltaTime)
                    : GetCameraRelativeDirection(moveInput);
            }

            MoveWorldDirection(direction, deltaTime);
        }

        public Vector3 GetCameraRelativeDirection(Vector2 input)
        {
            Vector2 clampedInput = Vector2.ClampMagnitude(input, 1f);

            if (clampedInput.sqrMagnitude <= 0.0001f)
            {
                return Vector3.zero;
            }

            Transform reference = cameraTransform != null
                ? cameraTransform
                : Camera.main != null
                    ? Camera.main.transform
                    : null;

            Vector3 forward = reference != null
                ? reference.forward
                : Vector3.forward;

            Vector3 right = reference != null
                ? reference.right
                : Vector3.right;

            forward.y = 0f;
            right.y = 0f;

            forward = forward.sqrMagnitude > 0.0001f
                ? forward.normalized
                : Vector3.forward;

            right = right.sqrMagnitude > 0.0001f
                ? right.normalized
                : Vector3.right;

            Vector3 direction =
                forward * clampedInput.y +
                right * clampedInput.x;

            return direction.sqrMagnitude > 1f
                ? direction.normalized
                : direction;
        }

        public bool AddModifier(MovementModifier modifier)
        {
            if (modifier == null)
            {
                return false;
            }

            if (modifiersById.ContainsKey(modifier.Id))
            {
                Debug.LogWarning(
                    $"[MOVEMENT] Já existe um modificador com o ID '{modifier.Id}'.",
                    this);

                return false;
            }

            movementModifiers.Add(modifier);
            modifiersById.Add(modifier.Id, modifier);
            MovementValuesChanged?.Invoke();
            return true;
        }

        public bool RemoveModifier(string modifierId)
        {
            if (string.IsNullOrWhiteSpace(modifierId))
            {
                return false;
            }

            if (!modifiersById.TryGetValue(modifierId, out MovementModifier modifier))
            {
                return false;
            }

            modifiersById.Remove(modifierId);
            bool removed = movementModifiers.Remove(modifier);

            if (removed)
            {
                MovementValuesChanged?.Invoke();
            }

            return removed;
        }

        public int RemoveModifiersFromSource(object source)
        {
            if (source == null)
            {
                return 0;
            }

            int totalRemoved = 0;

            for (int index = movementModifiers.Count - 1; index >= 0; index--)
            {
                MovementModifier modifier = movementModifiers[index];

                if (!Equals(modifier.Source, source))
                {
                    continue;
                }

                movementModifiers.RemoveAt(index);
                modifiersById.Remove(modifier.Id);
                totalRemoved++;
            }

            if (totalRemoved > 0)
            {
                MovementValuesChanged?.Invoke();
            }

            return totalRemoved;
        }

        public bool HasModifier(string modifierId)
        {
            return !string.IsNullOrWhiteSpace(modifierId) &&
                   modifiersById.ContainsKey(modifierId);
        }

        public bool AddOrUpdateSlow(object source, float slowPercent)
        {
            if (source == null)
            {
                return false;
            }

            float safeSlow = Mathf.Clamp(slowPercent, 0f, 0.95f);

            if (safeSlow <= 0f)
            {
                return RemoveSlow(source);
            }

            if (slowsBySource.TryGetValue(source, out float existingSlow) &&
                Mathf.Approximately(existingSlow, safeSlow))
            {
                return false;
            }

            slowsBySource[source] = safeSlow;
            RecalculateStrongestSlow();
            return true;
        }

        public bool RemoveSlow(object source)
        {
            if (source == null || !slowsBySource.Remove(source))
            {
                return false;
            }

            RecalculateStrongestSlow();
            return true;
        }

        public bool HasSlowFromSource(object source)
        {
            return source != null && slowsBySource.ContainsKey(source);
        }

        public float GetSlowFromSource(object source)
        {
            if (source == null)
            {
                return 0f;
            }

            return slowsBySource.TryGetValue(source, out float slowPercent)
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
            float oldStrongestSlow = strongestSlow;
            strongestSlow = 0f;

            if (!Mathf.Approximately(oldStrongestSlow, strongestSlow))
            {
                MovementValuesChanged?.Invoke();
            }
        }

        [ContextMenu("Log Current Movement Values")]
        public void LogCurrentMovementValues()
        {
            GetModifierTotals(
                out float flatValue,
                out float additivePercent,
                out float multiplicativeFactor);

            Debug.Log(
                $"[MOVEMENT VALUES] " +
                $"Modo: {(clickToMoveActive ? "ClickToMove" : "WASD")} | " +
                $"Velocidade-base: {moveSpeed:0.##} | " +
                $"Bônus fixo: {flatValue:0.##} | " +
                $"Bônus aditivo: {additivePercent * 100f:0.##}% | " +
                $"Multiplicador: {multiplicativeFactor:0.##}x | " +
                $"Velocidade modificada: {GetModifiedMoveSpeed():0.##} | " +
                $"Slow mais forte: {strongestSlow * 100f:0.##}% | " +
                $"Velocidade atual: {GetCurrentMoveSpeed():0.##}",
                this);
        }

        private Vector3 GetClickToMoveDirection(float deltaTime)
        {
            if (!hasClickDestination)
            {
                return Vector3.zero;
            }

            Vector3 offset = clickDestination - transform.position;
            offset.y = 0f;

            float distance = offset.magnitude;

            if (distance <= clickStoppingDistance)
            {
                CancelClickDestination();
                return Vector3.zero;
            }

            float currentSpeed = GetCurrentMoveSpeed();
            float maximumTravel = currentSpeed * Mathf.Max(0f, deltaTime);
            float allowedTravel = Mathf.Max(0f, distance - clickStoppingDistance);
            float inputMagnitude = maximumTravel > 0f
                ? Mathf.Clamp01(allowedTravel / maximumTravel)
                : 0f;

            return offset.normalized * inputMagnitude;
        }

        private void MoveWorldDirection(Vector3 direction, float deltaTime)
        {
            direction.y = 0f;

            if (direction.sqrMagnitude > 1f)
            {
                direction.Normalize();
            }

            float currentSpeed = GetCurrentMoveSpeed();
            Vector3 horizontalMotion = CanMove
                ? direction * currentSpeed
                : Vector3.zero;

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

        private Vector3 GetGravityMotion(float deltaTime)
        {
            if (characterController != null &&
                characterController.isGrounded &&
                verticalVelocity < 0f)
            {
                verticalVelocity = groundedStickForce;
            }
            else
            {
                verticalVelocity += gravity * deltaTime;
            }

            return Vector3.up * verticalVelocity;
        }

        private float GetModifiedMoveSpeed()
        {
            GetModifierTotals(
                out float flatValue,
                out float additivePercent,
                out float multiplicativeFactor);

            float modifiedSpeed = moveSpeed + flatValue;
            modifiedSpeed *= 1f + additivePercent;
            modifiedSpeed *= multiplicativeFactor;
            return Mathf.Max(0f, modifiedSpeed);
        }

        private float GetCurrentMoveSpeed()
        {
            float modifiedSpeed = GetModifiedMoveSpeed();
            float currentSpeed = modifiedSpeed * (1f - strongestSlow);
            return Mathf.Max(0f, currentSpeed);
        }

        private void GetModifierTotals(
            out float flatValue,
            out float additivePercent,
            out float multiplicativeFactor)
        {
            flatValue = 0f;
            additivePercent = 0f;
            multiplicativeFactor = 1f;

            for (int index = 0; index < movementModifiers.Count; index++)
            {
                MovementModifier modifier = movementModifiers[index];
                flatValue += modifier.FlatValue;
                additivePercent += modifier.AdditivePercent;
                multiplicativeFactor *= 1f + modifier.MultiplicativePercent;
            }
        }

        private void RecalculateStrongestSlow()
        {
            float oldStrongestSlow = strongestSlow;
            float newStrongestSlow = 0f;

            foreach (float slowPercent in slowsBySource.Values)
            {
                if (slowPercent > newStrongestSlow)
                {
                    newStrongestSlow = slowPercent;
                }
            }

            strongestSlow = Mathf.Clamp(newStrongestSlow, 0f, 0.95f);

            if (!Mathf.Approximately(oldStrongestSlow, strongestSlow))
            {
                MovementValuesChanged?.Invoke();
            }
        }

        private void RotateTowards(Vector3 direction, float deltaTime)
        {
            Quaternion targetRotation = Quaternion.LookRotation(direction, Vector3.up);

            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                targetRotation,
                Mathf.Clamp01(rotationSpeed * deltaTime));
        }

        private void CacheReferences()
        {
            actionState ??= GetComponent<ActionStateController>();
            characterController ??= GetComponent<CharacterController>();

            if (cameraTransform == null && Camera.main != null)
            {
                cameraTransform = Camera.main.transform;
            }
        }

        private void TrySetMovementModeByName(params string[] candidateNames)
        {
            if (candidateNames == null)
            {
                return;
            }

            for (int index = 0; index < candidateNames.Length; index++)
            {
                if (!Enum.TryParse(
                        candidateNames[index],
                        true,
                        out MovementMode parsedMode))
                {
                    continue;
                }

                movementMode = parsedMode;
                return;
            }
        }
    }
}
