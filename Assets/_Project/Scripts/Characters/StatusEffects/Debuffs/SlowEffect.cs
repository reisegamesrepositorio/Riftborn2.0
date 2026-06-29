using Riftborn.Characters.Core;
using UnityEngine;

namespace Riftborn.Characters.StatusEffects
{
    public sealed class SlowEffect : StatusEffectBase
    {
        private float slowPercent;

        public SlowEffect(
            CharacterContext source,
            CharacterContext target,
            float duration = 3f,
            float slowPercent = 0.25f)
            : base(
                id: "slow",
                source: source,
                target: target,
                duration: duration,
                maxStacks: 1,
                tags:
                StatusEffectTag.Debuff |
                StatusEffectTag.Slow)
        {
            this.slowPercent =
                Mathf.Clamp(
                    slowPercent,
                    0f,
                    0.95f);
        }

        public float SlowPercent =>
            slowPercent;

        public override void OnApply(
            StatusEffectController controller)
        {
            Target?.Movement?.AddOrUpdateSlow(
                source: this,
                slowPercent: slowPercent);
        }

        public override void OnReapply(
            StatusEffectController controller,
            StatusEffectBase incoming)
        {
            if (incoming is not SlowEffect incomingSlow)
            {
                return;
            }

            RefreshDuration(
                incomingSlow.Duration);

            slowPercent =
                Mathf.Max(
                    slowPercent,
                    incomingSlow.slowPercent);

            Target?.Movement?.AddOrUpdateSlow(
                source: this,
                slowPercent: slowPercent);
        }

        public override void OnRemove(
            StatusEffectController controller)
        {
            Target?.Movement?.RemoveSlow(this);
        }
    }
}