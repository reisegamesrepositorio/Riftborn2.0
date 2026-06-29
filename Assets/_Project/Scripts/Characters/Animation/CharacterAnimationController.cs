using UnityEngine;

namespace Riftborn.Characters.Animation
{
    public sealed class CharacterAnimationController : MonoBehaviour
    {
        [SerializeField]
        private Animator animator;

        private static readonly int MoveSpeedHash =
            Animator.StringToHash("MoveSpeed");

        private static readonly int AttackHash =
            Animator.StringToHash("Attack");

        private static readonly int AbilityHash =
            Animator.StringToHash("Ability");

        private static readonly int HurtHash =
            Animator.StringToHash("Hurt");

        private static readonly int DeathHash =
            Animator.StringToHash("Death");

        private static readonly int ControlledHash =
            Animator.StringToHash("Controlled");

        public bool HasAnimator =>
            animator != null;

        private void Awake()
        {
            CacheAnimator();
        }

        private void Reset()
        {
            CacheAnimator();
        }

        public void SetMovement(float speed)
        {
            if (!TryGetAnimator())
            {
                return;
            }

            animator.SetFloat(
                MoveSpeedHash,
                speed);
        }

        public void PlayAttack()
        {
            if (!TryGetAnimator())
            {
                return;
            }

            animator.SetTrigger(AttackHash);
        }

        public void PlayAbility()
        {
            if (!TryGetAnimator())
            {
                return;
            }

            animator.SetTrigger(AbilityHash);
        }

        public void PlayHurt()
        {
            if (!TryGetAnimator())
            {
                return;
            }

            animator.SetTrigger(HurtHash);
        }

        public void PlayDeath()
        {
            if (!TryGetAnimator())
            {
                return;
            }

            animator.SetTrigger(DeathHash);
        }

        public void SetControlled(bool controlled)
        {
            if (!TryGetAnimator())
            {
                return;
            }

            animator.SetBool(
                ControlledHash,
                controlled);
        }

        private bool TryGetAnimator()
        {
            if (animator != null)
            {
                return true;
            }

            CacheAnimator();

            return animator != null;
        }

        private void CacheAnimator()
        {
            if (animator != null)
            {
                return;
            }

            animator =
                GetComponentInChildren<Animator>(
                    includeInactive: true);
        }
    }
}