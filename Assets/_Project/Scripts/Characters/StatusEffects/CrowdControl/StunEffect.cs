using Riftborn.Characters.ActionStates;
using Riftborn.Characters.Core;
namespace Riftborn.Characters.StatusEffects
{
    public sealed class StunEffect : StatusEffectBase
    {
        public StunEffect(CharacterContext source, CharacterContext target, float duration = 2f) : base("stun", source, target, duration, 1, StatusEffectTag.Debuff | StatusEffectTag.CrowdControl | StatusEffectTag.HardControl | StatusEffectTag.Stun) { }
        public override void OnApply(StatusEffectController controller) { Target?.ActionState?.AddBlock(this, ActionPermission.Move | ActionPermission.Attack | ActionPermission.Cast); Target?.Events?.RaiseControlApplied(this); }
        public override void OnRemove(StatusEffectController controller) { Target?.ActionState?.RemoveBlock(this); }
    }
}
