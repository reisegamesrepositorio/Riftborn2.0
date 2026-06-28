using Riftborn.Characters.Core;
using Riftborn.Damage;
namespace Riftborn.Characters.StatusEffects { public sealed class BurnEffect : DamageOverTimeEffect { public BurnEffect(CharacterContext source, CharacterContext target, float duration = 5f, float damagePerTick = 5f, float tickInterval = 1f, int maxStacks = 3) : base("burn", source, target, duration, maxStacks, StatusEffectTag.Burn | StatusEffectTag.Magical, damagePerTick, tickInterval, DamageType.Magical) { } } }
