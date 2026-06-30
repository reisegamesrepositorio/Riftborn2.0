#if false
using NUnit.Framework;
using Riftborn.Characters.ActionStates;
using Riftborn.Characters.Core;
using Riftborn.Characters.Health;
using Riftborn.Characters.StatusEffects;
using Riftborn.Damage;
using UnityEngine;
namespace Riftborn.Tests
{
    public sealed class HealthActionStateAndStatusTests
    {
        [Test]
        public void HealthAppliesDamageAndHealing()
        {
            var go = new GameObject("health");
            var health = go.AddComponent<HealthController>();
            health.SetMaxHealth(100f, true);
            health.ApplyDamage(new DamageResult(new DamageRequest(), 25f, false));
            health.Heal(10f);
            Assert.AreEqual(85f, health.CurrentHealth, 0.001f);
            Object.DestroyImmediate(go);
        }
        [Test]
        public void ActionBlocksAreRemovedBySourceOnly()
        {
            var go = new GameObject("state");
            var state = go.AddComponent<ActionStateController>();
            object stun = new object(); object root = new object();
            state.AddBlock(stun, ActionPermission.Move | ActionPermission.Attack | ActionPermission.Cast);
            state.AddBlock(root, ActionPermission.Move);
            state.RemoveBlock(stun);
            Assert.IsFalse(state.CanMove);
            Assert.IsTrue(state.CanAttack);
            Assert.IsTrue(state.CanCast);
            Object.DestroyImmediate(go);
        }
        [Test]
        public void SleepIsRemovedWhenDamageIsTaken()
        {
            GameObject targetObject = CreateCharacter("target");
            var target = targetObject.GetComponent<CharacterContext>();
            var sleep = new SleepEffect(null, target, 10f);
            target.StatusEffects.Apply(sleep);
            target.Health.ApplyDamage(new DamageResult(new DamageRequest { Target = target }, 1f, false));
            Assert.IsFalse(target.StatusEffects.Has(StatusEffectTag.Sleep));
            Object.DestroyImmediate(targetObject);
        }
        private static GameObject CreateCharacter(string name)
        {
            var go = new GameObject(name);
            go.AddComponent<HealthController>(); go.AddComponent<ActionStateController>(); go.AddComponent<CharacterEvents>(); go.AddComponent<StatusEffectController>(); go.AddComponent<CharacterContext>();
            return go;
        }
    }
}

#endif
