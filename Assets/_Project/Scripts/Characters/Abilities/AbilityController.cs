using System;
using System.Collections.Generic;
using Riftborn.Characters.ActionStates;
using Riftborn.Characters.Core;
using UnityEngine;
namespace Riftborn.Characters.Abilities
{
    public sealed class AbilityController : MonoBehaviour
    {
        [SerializeField] private AbilityBase[] equippedAbilities = new AbilityBase[8];
        [SerializeField] private ActionStateController actionState;
        private readonly Dictionary<int, float> cooldownEnds = new();
        private CharacterContext context;
        public event Action<int, AbilityBase> AbilityUsed;
        private void Awake() { context = GetComponent<CharacterContext>(); actionState ??= GetComponent<ActionStateController>(); }
        public bool TryUse(int slot, CharacterContext target)
        {
            if (!CanUse(slot)) return false;
            AbilityBase ability = equippedAbilities[slot];
            if (ability.ResourceCost > 0f && context.Resources != null && !context.Resources.Consume(ability.ResourceCost)) return false;
            if (!ability.Execute(context, target)) return false;
            cooldownEnds[slot] = Time.time + ability.Cooldown; AbilityUsed?.Invoke(slot, ability); context.Events?.RaiseAbilityUsed(ability); return true;
        }
        public bool CanUse(int slot) { if (actionState != null && !actionState.CanCast) return false; if (slot < 0 || slot >= equippedAbilities.Length || equippedAbilities[slot] == null) return false; return !cooldownEnds.TryGetValue(slot, out float end) || Time.time >= end; }
    }
}
