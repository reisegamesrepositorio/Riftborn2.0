using System;
using Riftborn.Characters.ActionStates;
using Riftborn.Characters.Core;
using Riftborn.Characters.Targeting;
using Riftborn.Damage;
using UnityEngine;
namespace Riftborn.Characters.Combat
{
    public sealed class CombatController : MonoBehaviour
    {
        [SerializeField] private float basicAttackDamage = 10f, attackInterval = 1.5f, attackRange = 2f;
        [SerializeField] private ActionStateController actionState;
        [SerializeField] private TargetingController targeting;
        private float nextAttackTime;
        private CharacterContext context;
        public event Action BasicAttackStarted;
        public event Action<DamageResult> BasicAttackHit;
        private void Awake() { context = GetComponent<CharacterContext>(); actionState ??= GetComponent<ActionStateController>(); targeting ??= GetComponent<TargetingController>(); }
        public bool TryBasicAttack(CharacterContext target = null)
        {
            target ??= targeting?.CurrentTarget;
            if (!CanAttack(target)) return false;
            nextAttackTime = Time.time + attackInterval; BasicAttackStarted?.Invoke(); context?.Events?.RaiseBasicAttackStarted();
            var request = new DamageRequest { Source = context, Target = target, BaseValue = basicAttackDamage, Type = DamageType.Physical, Tags = DamageTag.BasicAttack | DamageTag.SingleTarget, Scaling = 1f, CanCrit = true, CriticalChance = 0.05f, CriticalMultiplier = 1.5f, Origin = DamageOrigin.BasicAttack, OriginObject = this };
            DamageResult result = DamageCalculator.Calculate(request);
            target.Health.ApplyDamage(result); BasicAttackHit?.Invoke(result); context?.Events?.RaiseBasicAttackHit(result); context?.Events?.RaiseDamageDealt(result); target.Events?.RaiseDamageTaken(result); if (result.WasCritical) context?.Events?.RaiseCriticalHit(result); return true;
        }
        public bool CanAttack(CharacterContext target)
        {
            if (actionState != null && !actionState.CanAttack) return false;
            if (Time.time < nextAttackTime || target == null || target.Health == null || target.Health.IsDead) return false;
            return Vector3.Distance(transform.position, target.transform.position) <= attackRange;
        }
    }
}
