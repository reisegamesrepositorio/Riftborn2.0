using Riftborn.Characters.ActionStates;
using Riftborn.Characters.Core;
using Riftborn.Damage;

namespace Riftborn.Characters.StatusEffects
{
    public sealed class SleepEffect :
        StatusEffectBase,
        IRemoveOnDamage
    {
        public SleepEffect(
            CharacterContext source,
            CharacterContext target,
            float duration = 4f)
            : base(
                id: "sleep",
                source: source,
                target: target,
                duration: duration,
                maxStacks: 1,
                tags:
                StatusEffectTag.Debuff |
                StatusEffectTag.CrowdControl |
                StatusEffectTag.HardControl |
                StatusEffectTag.Sleep)
        {
        }

        public override void OnApply(
            StatusEffectController controller)
        {
            Target?.ActionState?.AddBlock(
                source: this,
                permissions:
                ActionPermission.Move |
                ActionPermission.Attack |
                ActionPermission.Cast);

            Target?.Events?.RaiseControlApplied(this);
        }

        public override void OnRemove(
            StatusEffectController controller)
        {
            Target?.ActionState?.RemoveBlock(this);
        }

        public bool ShouldRemoveOnDamage(
            DamageResult result)
        {
            return result != null &&
                   result.FinalAmount > 0f;
        }
    }
}