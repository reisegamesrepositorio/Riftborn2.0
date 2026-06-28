using System;
using Riftborn.Characters.Health;
using Riftborn.Characters.StatusEffects;
using Riftborn.Damage;
using UnityEngine;
namespace Riftborn.Characters.Core
{
    public sealed class CharacterEvents : MonoBehaviour
    {
        public event Action OnBasicAttackStarted;
        public event Action<DamageResult> OnBasicAttackHit, OnDamageDealt, OnDamageTaken, OnCriticalHit;
        public event Action<object> OnAbilityUsed, OnControlApplied;
        public event Action<float> OnHealApplied, OnShieldApplied;
        public event Action<StatusEffectBase> OnStatusApplied, OnStatusRemoved;
        public event Action<CharacterContext> OnTargetChanged;
        public event Action<HealthChangedEventArgs> OnHealthChanged;
        public event Action OnDeath;
        public void RaiseBasicAttackStarted() => OnBasicAttackStarted?.Invoke();
        public void RaiseBasicAttackHit(DamageResult result) => OnBasicAttackHit?.Invoke(result);
        public void RaiseAbilityUsed(object ability) => OnAbilityUsed?.Invoke(ability);
        public void RaiseDamageDealt(DamageResult result) => OnDamageDealt?.Invoke(result);
        public void RaiseDamageTaken(DamageResult result) => OnDamageTaken?.Invoke(result);
        public void RaiseCriticalHit(DamageResult result) => OnCriticalHit?.Invoke(result);
        public void RaiseHealApplied(float amount) => OnHealApplied?.Invoke(amount);
        public void RaiseShieldApplied(float amount) => OnShieldApplied?.Invoke(amount);
        public void RaiseStatusApplied(StatusEffectBase effect) => OnStatusApplied?.Invoke(effect);
        public void RaiseStatusRemoved(StatusEffectBase effect) => OnStatusRemoved?.Invoke(effect);
        public void RaiseControlApplied(object source) => OnControlApplied?.Invoke(source);
        public void RaiseTargetChanged(CharacterContext target) => OnTargetChanged?.Invoke(target);
        public void RaiseHealthChanged(HealthChangedEventArgs args) => OnHealthChanged?.Invoke(args);
        public void RaiseDeath() => OnDeath?.Invoke();
    }
}
