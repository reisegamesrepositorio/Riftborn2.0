using System;
using Riftborn.Characters.Core;
using UnityEngine;
namespace Riftborn.Characters.StatusEffects
{
    [Serializable]
    public abstract class StatusEffectBase
    {
        protected StatusEffectBase(string id, CharacterContext source, CharacterContext target, float duration, int maxStacks, StatusEffectTag tags)
        { Id = string.IsNullOrWhiteSpace(id) ? Guid.NewGuid().ToString("N") : id; Source = source; Target = target; Duration = Mathf.Max(0f, duration); RemainingTime = Duration; MaxStacks = Mathf.Max(1, maxStacks); Stacks = 1; Tags = tags; }
        public string Id { get; }
        public CharacterContext Source { get; }
        public CharacterContext Target { get; }
        public float Duration { get; }
        public float RemainingTime { get; private set; }
        public int Stacks { get; private set; }
        public int MaxStacks { get; }
        public StatusEffectTag Tags { get; }
        public bool IsExpired => RemainingTime <= 0f;
        public virtual bool CanStackWith(StatusEffectBase incoming) => incoming != null && GetType() == incoming.GetType();
        public virtual void OnApply(StatusEffectController controller) { }
        public virtual void OnReapply(StatusEffectBase incoming) { RemainingTime = Mathf.Max(RemainingTime, incoming.Duration); Stacks = Mathf.Min(MaxStacks, Stacks + incoming.Stacks); }
        public virtual void Tick(StatusEffectController controller, float deltaTime) { RemainingTime = Mathf.Max(0f, RemainingTime - Mathf.Max(0f, deltaTime)); }
        public virtual void OnRemove(StatusEffectController controller) { }
    }
}
