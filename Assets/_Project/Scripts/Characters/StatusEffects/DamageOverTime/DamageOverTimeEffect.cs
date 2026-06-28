using Riftborn.Characters.Core;
using Riftborn.Damage;
using UnityEngine;
namespace Riftborn.Characters.StatusEffects
{
    public abstract class DamageOverTimeEffect : StatusEffectBase
    {
        private readonly float damagePerTick, tickInterval;
        private readonly DamageType damageType;
        private float tickTimer;
        protected DamageOverTimeEffect(string id, CharacterContext source, CharacterContext target, float duration, int maxStacks, StatusEffectTag tags, float damagePerTick, float tickInterval, DamageType damageType) : base(id, source, target, duration, maxStacks, tags | StatusEffectTag.Debuff | StatusEffectTag.DamageOverTime)
        { this.damagePerTick = Mathf.Max(0f, damagePerTick); this.tickInterval = Mathf.Max(0.1f, tickInterval); this.damageType = damageType; }
        public override void Tick(StatusEffectController controller, float deltaTime)
        {
            base.Tick(controller, deltaTime); tickTimer += deltaTime;
            while (tickTimer >= tickInterval && !IsExpired) { tickTimer -= tickInterval; controller.ApplyDamage(CreateDamageRequest()); }
        }
        protected virtual DamageRequest CreateDamageRequest() => new DamageRequest { Source = Source, Target = Target, BaseValue = damagePerTick * Stacks, Type = damageType, Tags = DamageTag.DamageOverTime | DamageTag.StatusEffect, Scaling = 1f, Origin = DamageOrigin.StatusEffect, OriginObject = this };
    }
}
