using Riftborn.Characters.ActionStates;
using UnityEngine;
namespace Riftborn.Characters.Movement
{
    public sealed class MovementController : MonoBehaviour
    {
        [SerializeField] private float moveSpeed = 5f;
        [SerializeField] private ActionStateController actionState;
        public float MoveSpeed => moveSpeed;
        public bool CanMove => actionState == null || actionState.CanMove;
        private void Awake() { actionState ??= GetComponent<ActionStateController>(); }
        public void Move(Vector3 direction, float deltaTime) { if (!CanMove || direction.sqrMagnitude <= 0f) return; transform.position += direction.normalized * moveSpeed * deltaTime; }
        public void Teleport(Vector3 position) { transform.position = position; }
    }
}
