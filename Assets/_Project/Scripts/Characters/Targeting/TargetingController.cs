using System;
using Riftborn.Characters.Core;
using UnityEngine;
namespace Riftborn.Characters.Targeting
{
    public sealed class TargetingController : MonoBehaviour
    {
        [SerializeField] private CharacterContext currentTarget;
        public event Action<CharacterContext> TargetChanged;
        public CharacterContext CurrentTarget => currentTarget;
        public bool SetTarget(CharacterContext target) { if (target == currentTarget || !IsValidTarget(target)) return false; currentTarget = target; TargetChanged?.Invoke(currentTarget); GetComponent<CharacterContext>()?.Events?.RaiseTargetChanged(currentTarget); return true; }
        public void ClearTarget() { currentTarget = null; TargetChanged?.Invoke(null); }
        public bool IsValidTarget(CharacterContext target) => target != null && target.Health != null && !target.Health.IsDead;
    }
}
