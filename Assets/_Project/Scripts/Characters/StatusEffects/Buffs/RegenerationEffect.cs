using Riftborn.Characters.Core;
namespace Riftborn.Characters.StatusEffects
{
    public sealed class RegenerationEffect : StatusEffectBase
    {
        private readonly float healPerTick, tickInterval;
        private float tickTimer;
        public RegenerationEffect(CharacterContext source, CharacterContext target, float duration = 5f, float healPerTick = 4f, float tickInterval = 1f) : base("regeneration", source, target, duration, 1, StatusEffectTag.Buff | StatusEffectTag.HealOverTime) { this.healPerTick = healPerTick; this.tickInterval = tickInterval; }
        public override void Tick(StatusEffectController controller, float deltaTime) { base.Tick(controller, deltaTime); tickTimer += deltaTime; while (tickTimer >= tickInterval && !IsExpired) { tickTimer -= tickInterval; Target?.Health?.Heal(healPerTick * Stacks); } }
    }
}
