using Riftborn.Characters.ActionStates;
using Riftborn.Characters.Core;
namespace Riftborn.Characters.StatusEffects
{
    public sealed class RootEffect : StatusEffectBase
    {
        public RootEffect(CharacterContext source, CharacterContext target, float duration = 2f) : base("root", source, target, duration, 1, StatusEffectTag.Debuff | StatusEffectTag.CrowdControl | StatusEffectTag.Root) { }
        public override void OnApply(StatusEffectController controller) { Target?.ActionState?.AddBlock(this, ActionPermission.Move); Target?.Events?.RaiseControlApplied(this); }
        public override void OnRemove(StatusEffectController controller) { Target?.ActionState?.RemoveBlock(this); }
    }
}
