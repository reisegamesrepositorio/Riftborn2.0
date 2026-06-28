using System;
using System.Collections.Generic;
using System.Linq;
using Riftborn.Characters.Core;
using Riftborn.Characters.Health;
using Riftborn.Damage;
using UnityEngine;
namespace Riftborn.Characters.StatusEffects
{
    public sealed class StatusEffectController : MonoBehaviour
    {
        private readonly List<StatusEffectBase> activeEffects = new();
        private CharacterContext context;
        private HealthController health;
        private bool subscribedToHealth;
        public event Action<StatusEffectBase> StatusApplied, StatusRemoved;
        public IReadOnlyList<StatusEffectBase> ActiveEffects => activeEffects;
        public CharacterContext Context { get { EnsureReferences(); return context; } }
        private void Awake() { EnsureReferences(); }
        private void OnDestroy() { UnsubscribeFromHealth(); }
        private void Update() { EnsureReferences(); UpdateEffects(Time.deltaTime); }
        public bool Apply(StatusEffectBase effect)
        {
            EnsureReferences();
            if (effect == null || IsImmuneTo(effect.Tags)) return false;
            StatusEffectBase existing = activeEffects.FirstOrDefault(active => active.CanStackWith(effect));
            if (existing != null) { existing.OnReapply(effect); return true; }
            activeEffects.Add(effect); effect.OnApply(this); StatusApplied?.Invoke(effect); context?.Events?.RaiseStatusApplied(effect); return true;
        }
        public bool Remove(StatusEffectBase effect)
        {
            if (effect == null || !activeEffects.Remove(effect)) return false;
            effect.OnRemove(this); StatusRemoved?.Invoke(effect); context?.Events?.RaiseStatusRemoved(effect); return true;
        }
        public int Cleanse(StatusEffectTag tags) { var toRemove = activeEffects.Where(e => (e.Tags & tags) != 0).ToList(); foreach (var effect in toRemove) Remove(effect); return toRemove.Count; }
        public bool Has(StatusEffectTag tags) => activeEffects.Any(effect => (effect.Tags & tags) != 0);
        public void UpdateEffects(float deltaTime) { for (int i = activeEffects.Count - 1; i >= 0; i--) { var effect = activeEffects[i]; effect.Tick(this, deltaTime); if (effect.IsExpired) Remove(effect); } }
        public void ApplyDamage(DamageRequest request) { DamageResult result = DamageCalculator.Calculate(request); request.Target?.Health?.ApplyDamage(result); request.Source?.Events?.RaiseDamageDealt(result); request.Target?.Events?.RaiseDamageTaken(result); }
        private bool IsImmuneTo(StatusEffectTag tags) => false;
        private void EnsureReferences()
        {
            context ??= GetComponent<CharacterContext>();
            health ??= GetComponent<HealthController>();
            if (!subscribedToHealth && health != null)
            {
                health.DamageTaken += HandleDamageTaken;
                subscribedToHealth = true;
            }
        }
        private void UnsubscribeFromHealth()
        {
            if (subscribedToHealth && health != null)
            {
                health.DamageTaken -= HandleDamageTaken;
                subscribedToHealth = false;
            }
        }
        private void HandleDamageTaken(DamageResult result) { foreach (var effect in activeEffects.ToList()) if (effect is IRemoveOnDamage removable && removable.ShouldRemoveOnDamage(result)) Remove(effect); }
    }
}
