using Riftborn.Characters.Core;
using UnityEngine;
namespace Riftborn.Characters.Abilities
{
    public abstract class AbilityBase : ScriptableObject
    {
        [SerializeField] private string abilityId;
        [SerializeField] private float cooldown = 1f, resourceCost;
        public string AbilityId => abilityId;
        public float Cooldown => cooldown;
        public float ResourceCost => resourceCost;
        public abstract bool Execute(CharacterContext caster, CharacterContext target);
    }
}
