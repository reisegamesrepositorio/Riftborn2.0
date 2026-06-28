using Riftborn.Characters.ActionStates;
using Riftborn.Characters.Core;
namespace Riftborn.Characters.StatusEffects
{
    public sealed class SilenceEffect : StatusEffectBase
    {
        public SilenceEffect(CharacterContext source, CharacterContext target, float duration = 2f) : base("silence", source, target, duration, 1, StatusEffectTag.Debuff | StatusEffectTag.CrowdControl | StatusEffectTag.Silence) { }
        public override void OnApply(StatusEffectController controller) { Target?.ActionState?.AddBlock(this, ActionPermission.Cast); Target?.Events?.RaiseControlApplied(this); }
        public override void OnRemove(StatusEffectController controller) { Target?.ActionState?.RemoveBlock(this); }
    }
}
