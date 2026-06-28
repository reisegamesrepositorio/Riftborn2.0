using NUnit.Framework;
using Riftborn.Characters.ActionStates;
using Riftborn.Characters.Movement;
using UnityEngine;

namespace Riftborn.Tests
{
    public sealed class MovementControllerTests
    {
        [Test]
        public void CameraRelativeDirectionNormalizesDiagonals()
        {
            var go = new GameObject("mover");
            var movement = go.AddComponent<MovementController>();

            Vector3 direction = movement.GetCameraRelativeDirection(new Vector2(1f, 1f));

            Assert.LessOrEqual(direction.magnitude, 1.001f);
            Object.DestroyImmediate(go);
        }

        [Test]
        public void MovementStopsWhenActionStateBlocksMove()
        {
            var go = new GameObject("mover");
            var state = go.AddComponent<ActionStateController>();
            var movement = go.AddComponent<MovementController>();
            object root = new object();

            state.AddBlock(root, ActionPermission.Move);
            movement.SetMoveInput(Vector2.up);
            movement.TickMovement(0.1f);

            Assert.AreEqual(0f, go.transform.position.x, 0.001f);
            Assert.AreEqual(0f, go.transform.position.z, 0.001f);
            Object.DestroyImmediate(go);
        }
    }
}
