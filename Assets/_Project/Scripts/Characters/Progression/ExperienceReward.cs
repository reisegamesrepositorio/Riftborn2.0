using Riftborn.Characters.Core;
using UnityEngine;

namespace Riftborn.Characters.Progression
{
    public sealed class ExperienceReward : MonoBehaviour
    {
        [Header("Reward")]
        [SerializeField, Min(0)]
        private int experienceAmount = 20;

        [Header("References")]
        [SerializeField]
        private CharacterContext ownerContext;

        [Header("Debug")]
        [SerializeField]
        private bool showDebugLogs = true;

        private bool rewardProcessedForCurrentLife;

        public int ExperienceAmount =>
            experienceAmount;

        public bool RewardProcessedForCurrentLife =>
            rewardProcessedForCurrentLife;

        private void Awake()
        {
            CacheReferences();
        }

        private void OnValidate()
        {
            experienceAmount =
                Mathf.Max(
                    0,
                    experienceAmount);
        }

        public bool GrantExperienceTo(
            CharacterContext receiver)
        {
            if (rewardProcessedForCurrentLife)
            {
                return false;
            }

            rewardProcessedForCurrentLife =
                true;

            if (experienceAmount <= 0)
            {
                return false;
            }

            if (receiver == null)
            {
                LogWarning(
                    "[EXPERIENCE REWARD] Nenhum recebedor válido foi informado.");

                return false;
            }

            if (ownerContext != null &&
                ReferenceEquals(
                    receiver,
                    ownerContext))
            {
                LogWarning(
                    "[EXPERIENCE REWARD] Autoeliminação não concede experiência.");

                return false;
            }

            CharacterProgressionController progression =
                receiver.Progression;

            progression ??=
                receiver.GetComponent<
                    CharacterProgressionController>();

            if (progression == null)
            {
                LogWarning(
                    $"[EXPERIENCE REWARD] " +
                    $"'{receiver.name}' não possui " +
                    $"{nameof(CharacterProgressionController)}.");

                return false;
            }

            bool granted =
                progression.AddExperience(
                    experienceAmount);

            if (!granted)
            {
                LogWarning(
                    $"[EXPERIENCE REWARD] Não foi possível conceder " +
                    $"{experienceAmount} XP para '{receiver.name}'.");

                return false;
            }

            if (showDebugLogs)
            {
                Debug.Log(
                    $"[EXPERIENCE REWARD] " +
                    $"{name} concedeu {experienceAmount} XP " +
                    $"para {receiver.name}.",
                    this);
            }

            return true;
        }

        public void ResetRewardState()
        {
            rewardProcessedForCurrentLife =
                false;
        }

        private void CacheReferences()
        {
            ownerContext ??=
                GetComponent<CharacterContext>();
        }

        private void LogWarning(
            string message)
        {
            if (showDebugLogs)
            {
                Debug.LogWarning(
                    message,
                    this);
            }
        }
    }
}
