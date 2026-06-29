using Riftborn.Characters.Core;
using UnityEngine;

namespace Riftborn.Characters.StatusEffects
{
    public sealed class RegenerationEffect : StatusEffectBase
    {
        private readonly float healPerTick;
        private readonly float tickInterval;

        private float tickTimer;

        public RegenerationEffect(
            CharacterContext source,
            CharacterContext target,
            float duration = 5f,
            float healPerTick = 4f,
            float tickInterval = 1f)
            : base(
                id: "regeneration",
                source: source,
                target: target,
                duration: duration,
                maxStacks: 1,
                tags:
                    StatusEffectTag.Buff |
                    StatusEffectTag.HealOverTime)
        {
            this.healPerTick =
                Mathf.Max(0f, healPerTick);

            this.tickInterval =
                Mathf.Max(0.1f, tickInterval);
        }

        public float HealPerTick =>
            healPerTick;

        public float TickInterval =>
            tickInterval;

        public override void Tick(
            StatusEffectController controller,
            float deltaTime)
        {
            float safeDeltaTime =
                Mathf.Max(0f, deltaTime);

            if (safeDeltaTime <= 0f || IsExpired)
            {
                return;
            }

            float activeDeltaTime =
                Mathf.Min(
                    safeDeltaTime,
                    RemainingTime);

            tickTimer += activeDeltaTime;

            while (tickTimer >= tickInterval)
            {
                tickTimer -= tickInterval;

                float healing =
                    CalculateHealingPerTick();

                if (healing > 0f)
                {
                    Target?.Health?.Heal(healing);
                }
            }

            base.Tick(
                controller,
                safeDeltaTime);
        }

        private float CalculateHealingPerTick()
        {
            return healPerTick * Stacks;
        }
    }
}