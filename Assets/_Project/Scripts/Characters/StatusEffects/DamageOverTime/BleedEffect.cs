using Riftborn.Characters.Core;
using Riftborn.Damage;
namespace Riftborn.Characters.StatusEffects { public sealed class BleedEffect : DamageOverTimeEffect { public BleedEffect(CharacterContext source, CharacterContext target, float duration = 6f, float damagePerTick = 4f, float tickInterval = 1f, int maxStacks = 5) : base("bleed", source, target, duration, maxStacks, StatusEffectTag.Bleed | StatusEffectTag.Physical, damagePerTick, tickInterval, DamageType.Physical) { } } }
