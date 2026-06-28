using Riftborn.Characters.Core;
namespace Riftborn.Characters.StatusEffects { public sealed class SlowEffect : StatusEffectBase { public SlowEffect(CharacterContext source, CharacterContext target, float duration = 3f) : base("slow", source, target, duration, 1, StatusEffectTag.Debuff | StatusEffectTag.Slow) { } } }
