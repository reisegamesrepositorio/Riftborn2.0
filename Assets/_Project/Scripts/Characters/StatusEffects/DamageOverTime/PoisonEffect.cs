using Riftborn.Characters.Core;
using Riftborn.Damage;
namespace Riftborn.Characters.StatusEffects { public sealed class PoisonEffect : DamageOverTimeEffect { public PoisonEffect(CharacterContext source, CharacterContext target, float duration = 8f, float damagePerTick = 3f, float tickInterval = 1f, int maxStacks = 5) : base("poison", source, target, duration, maxStacks, StatusEffectTag.Poison | StatusEffectTag.Magical, damagePerTick, tickInterval, DamageType.Magical) { } } }
