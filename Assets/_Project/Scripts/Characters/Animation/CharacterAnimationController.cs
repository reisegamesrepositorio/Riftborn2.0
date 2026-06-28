using UnityEngine;
namespace Riftborn.Characters.Animation
{
    public sealed class CharacterAnimationController : MonoBehaviour
    {
        [SerializeField] private Animator animator;
        private static readonly int MoveSpeedHash = Animator.StringToHash("MoveSpeed"), AttackHash = Animator.StringToHash("Attack"), AbilityHash = Animator.StringToHash("Ability"), HurtHash = Animator.StringToHash("Hurt"), DeathHash = Animator.StringToHash("Death"), ControlledHash = Animator.StringToHash("Controlled");
        private void Awake() { animator ??= GetComponentInChildren<Animator>(); }
        public void SetMovement(float speed) => animator?.SetFloat(MoveSpeedHash, speed);
        public void PlayAttack() => animator?.SetTrigger(AttackHash);
        public void PlayAbility() => animator?.SetTrigger(AbilityHash);
        public void PlayHurt() => animator?.SetTrigger(HurtHash);
        public void PlayDeath() => animator?.SetTrigger(DeathHash);
        public void SetControlled(bool controlled) => animator?.SetBool(ControlledHash, controlled);
    }
}
