using Riftborn.Characters.Core;
namespace Riftborn.Characters.StatusEffects { public sealed class ShieldEffect : StatusEffectBase { public ShieldEffect(CharacterContext source, CharacterContext target, float duration = 5f, float amount = 25f) : base("shield", source, target, duration, 1, StatusEffectTag.Buff | StatusEffectTag.Shield) { Amount = amount; } public float Amount { get; } } }
