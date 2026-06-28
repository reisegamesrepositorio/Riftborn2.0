using Riftborn.Characters.Core;
namespace Riftborn.Damage
{
    public sealed class DamageRequest
    {
        public CharacterContext Source { get; set; }
        public CharacterContext Target { get; set; }
        public float BaseValue { get; set; }
        public DamageType Type { get; set; }
        public DamageTag Tags { get; set; }
        public float Scaling { get; set; } = 1f;
        public bool CanCrit { get; set; }
        public float CriticalChance { get; set; }
        public float CriticalMultiplier { get; set; } = 1.5f;
        public DamageOrigin Origin { get; set; }
        public object OriginObject { get; set; }
    }
}
