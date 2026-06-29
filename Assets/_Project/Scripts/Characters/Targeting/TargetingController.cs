using Riftborn.Characters.Core;
using UnityEngine;

namespace Riftborn.Characters.Targeting
{
    public sealed class TargetingController : MonoBehaviour
    {
        private CharacterContext ownerCharacter;
        private TargetHighlight currentHighlight;

        public CharacterContext CurrentTarget { get; private set; }

        public bool HasTarget =>
            IsValidTarget(CurrentTarget);

        public Transform CurrentTargetTransform =>
            CurrentTarget != null
                ? CurrentTarget.transform
                : null;

        private void Awake()
        {
            ownerCharacter =
                GetComponent<CharacterContext>();
        }

        public bool SetTarget(
            CharacterContext newTarget)
        {
            if (!IsValidTarget(newTarget))
            {
                return false;
            }

            if (CurrentTarget == newTarget)
            {
                return true;
            }

            UnsubscribeFromCurrentTarget();
            DisableCurrentHighlight();

            CurrentTarget = newTarget;

            currentHighlight =
                FindHighlight(CurrentTarget);

            if (currentHighlight != null)
            {
                currentHighlight.SetSelected(true);
            }

            if (CurrentTarget.Health != null)
            {
                CurrentTarget.Health.Died +=
                    HandleTargetDied;
            }

            Debug.Log(
                $"[TARGETING] Alvo definido: " +
                $"{CurrentTarget.name}",
                CurrentTarget);

            return true;
        }

        public bool SetTarget(
            GameObject targetObject)
        {
            if (targetObject == null)
            {
                return false;
            }

            CharacterContext targetCharacter =
                targetObject.GetComponentInParent<CharacterContext>();

            return SetTarget(targetCharacter);
        }

        public bool IsValidTarget(
            CharacterContext target)
        {
            if (target == null)
            {
                return false;
            }

            if (!target.gameObject.activeInHierarchy)
            {
                return false;
            }

            if (ownerCharacter != null &&
                target == ownerCharacter)
            {
                return false;
            }

            if (target.Health != null &&
                target.Health.IsDead)
            {
                return false;
            }

            return true;
        }

        public bool IsValidTarget(
            GameObject targetObject)
        {
            if (targetObject == null)
            {
                return false;
            }

            CharacterContext targetCharacter =
                targetObject.GetComponentInParent<CharacterContext>();

            return IsValidTarget(targetCharacter);
        }

        public bool IsValidTarget()
        {
            return IsValidTarget(CurrentTarget);
        }

        public void ClearTarget()
        {
            UnsubscribeFromCurrentTarget();
            DisableCurrentHighlight();

            CurrentTarget = null;
            currentHighlight = null;
        }

        private void HandleTargetDied()
        {
            string targetName =
                CurrentTarget != null
                    ? CurrentTarget.name
                    : "desconhecido";

            Debug.Log(
                $"[TARGETING] O alvo {targetName} morreu. " +
                "Seleção removida.",
                this);

            ClearTarget();
        }

        private void UnsubscribeFromCurrentTarget()
        {
            if (CurrentTarget == null ||
                CurrentTarget.Health == null)
            {
                return;
            }

            CurrentTarget.Health.Died -=
                HandleTargetDied;
        }

        private void DisableCurrentHighlight()
        {
            if (currentHighlight != null)
            {
                currentHighlight.SetSelected(false);
            }
        }

        private static TargetHighlight FindHighlight(
            CharacterContext target)
        {
            if (target == null)
            {
                return null;
            }

            TargetHighlight highlight =
                target.GetComponent<TargetHighlight>();

            if (highlight != null)
            {
                return highlight;
            }

            highlight =
                target.GetComponentInChildren<TargetHighlight>(
                    includeInactive: true);

            if (highlight != null)
            {
                return highlight;
            }

            return target.GetComponentInParent<TargetHighlight>();
        }

        private void OnDisable()
        {
            ClearTarget();
        }

        private void OnDestroy()
        {
            ClearTarget();
        }
    }
}