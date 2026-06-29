using Riftborn.Characters.Core;
using Riftborn.Damage;
using UnityEngine;

namespace Riftborn.Characters.StatusEffects
{
    public abstract class DamageOverTimeEffect : StatusEffectBase
    {
        private readonly float damagePerTick;
        private readonly float tickInterval;
        private readonly DamageType damageType;

        private float tickTimer;

        protected DamageOverTimeEffect(
            string id,
            CharacterContext source,
            CharacterContext target,
            float duration,
            int maxStacks,
            StatusEffectTag tags,
            float damagePerTick,
            float tickInterval,
            DamageType damageType)
            : base(
                id,
                source,
                target,
                duration,
                maxStacks,
                tags |
                StatusEffectTag.Debuff |
                StatusEffectTag.DamageOverTime)
        {
            this.damagePerTick =
                Mathf.Max(0f, damagePerTick);

            this.tickInterval =
                Mathf.Max(0.1f, tickInterval);

            this.damageType = damageType;
        }

        public float BaseDamagePerTick =>
            damagePerTick;

        public float TickInterval =>
            tickInterval;

        public DamageType DamageType =>
            damageType;

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

            /*
             * Somente o tempo em que o efeito ainda estava ativo
             * pode contribuir para o próximo tick.
             *
             * Isso também permite que o último tick aconteça
             * exatamente no momento em que o efeito expira.
             */
            float activeDeltaTime =
                Mathf.Min(
                    safeDeltaTime,
                    RemainingTime);

            tickTimer += activeDeltaTime;

            while (tickTimer >= tickInterval)
            {
                tickTimer -= tickInterval;

                DamageRequest request =
                    CreateDamageRequest();

                if (request != null)
                {
                    controller?.ApplyDamage(request);
                }
            }

            /*
             * A duração é reduzida depois do processamento dos ticks.
             * Assim um efeito de 5 segundos com intervalo de 1 segundo
             * causa corretamente 5 ticks.
             */
            base.Tick(
                controller,
                safeDeltaTime);
        }

        protected virtual float CalculateDamagePerTick()
        {
            return damagePerTick * Stacks;
        }

        protected virtual DamageRequest CreateDamageRequest()
        {
            return new DamageRequest
            {
                Source = Source,
                Target = Target,

                BaseValue =
                    CalculateDamagePerTick(),

                Type = damageType,

                Tags =
                    DamageTag.DamageOverTime |
                    DamageTag.StatusEffect,

                Scaling = 1f,

                CanCrit = false,

                Origin =
                    DamageOrigin.StatusEffect,

                OriginObject = this
            };
        }
    }
}