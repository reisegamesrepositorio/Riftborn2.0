using System;
using Riftborn.Characters.Core;
using UnityEngine;

namespace Riftborn.Characters.Targeting
{
    [Serializable]
    public sealed class TargetingController
    {
        private CharacterContext ownerCharacter;
        private TargetHighlight currentHighlight;

        public event Action<
            CharacterContext,
            CharacterContext> TargetChanged;

        public CharacterContext CurrentTarget { get; private set; }

        public bool HasTarget =>
            IsValidTarget(CurrentTarget);

        public Transform CurrentTargetTransform =>
            CurrentTarget != null
                ? CurrentTarget.transform
                : null;

        public void Initialize(CharacterContext owner)
        {
            ownerCharacter = owner;
        }

        public bool SetTarget(
            CharacterContext newTarget)
        {
            if (!IsValidTarget(newTarget))
            {
                return false;
            }

            if (ReferenceEquals(
                    CurrentTarget,
                    newTarget))
            {
                return true;
            }

            CharacterContext previousTarget =
                CurrentTarget;

            DisableCurrentHighlight();

            CurrentTarget =
                newTarget;

            currentHighlight =
                FindHighlight(CurrentTarget);

            if (currentHighlight != null)
            {
                currentHighlight.SetSelected(
                    true);
            }

            Debug.Log(
                $"[TARGETING] Alvo definido: " +
                $"{CurrentTarget.name}",
                CurrentTarget);

            TargetChanged?.Invoke(
                previousTarget,
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
                targetObject.GetComponentInParent<
                    CharacterContext>();

            return SetTarget(
                targetCharacter);
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
                ReferenceEquals(
                    target,
                    ownerCharacter))
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
                targetObject.GetComponentInParent<
                    CharacterContext>();

            return IsValidTarget(
                targetCharacter);
        }

        public bool IsValidTarget()
        {
            return IsValidTarget(
                CurrentTarget);
        }

        public void ClearTarget()
        {
            if (CurrentTarget == null &&
                currentHighlight == null)
            {
                return;
            }

            CharacterContext previousTarget =
                CurrentTarget;

            DisableCurrentHighlight();

            CurrentTarget = null;
            currentHighlight = null;

            TargetChanged?.Invoke(
                previousTarget,
                null);
        }

        private void DisableCurrentHighlight()
        {
            if (currentHighlight != null)
            {
                currentHighlight.SetSelected(
                    false);
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
                target.GetComponentInChildren<
                    TargetHighlight>(
                    includeInactive: true);

            if (highlight != null)
            {
                return highlight;
            }

            return target.GetComponentInParent<
                TargetHighlight>();
        }

        public void Disable()
        {
            ClearTarget();
        }
    }
}
