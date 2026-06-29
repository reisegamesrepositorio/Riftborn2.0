using System.Collections;
using System.Collections.Generic;
using Riftborn.Characters.Core;
using Riftborn.Characters.Health;
using UnityEngine;

namespace Riftborn.Characters.Visual
{
    public sealed class CharacterDeathFeedback : MonoBehaviour
    {
        [Header("Character")]
        [SerializeField]
        private CharacterContext character;

        [Header("Visual")]
        [Tooltip(
            "Objeto visual que será escondido na morte. " +
            "No EnemyDummy, arraste o objeto Visual.")]
        [SerializeField]
        private Transform visualRoot;

        [Header("Death Reaction")]
        [SerializeField]
        private bool playDeathAnimation = true;

        [SerializeField]
        private bool disableColliders = true;

        [SerializeField]
        private bool disableGameplayControllers = true;

        [SerializeField, Min(0f)]
        private float delayBeforeShrink = 0.15f;

        [SerializeField, Min(0f)]
        private float shrinkDuration = 0.25f;

        [Header("Optional Destruction")]
        [Tooltip(
            "Deixe desmarcado durante o protótipo para permitir revive.")]
        [SerializeField]
        private bool destroyAfterDeath;

        [SerializeField, Min(0f)]
        private float destroyDelay = 2f;

        private readonly List<ColliderState>
            colliderStates = new();

        private readonly List<BehaviourState>
            controllerStates = new();

        private HealthController health;
        private Coroutine deathCoroutine;
        private Vector3 originalVisualScale;
        private bool isSubscribed;
        private bool deathProcessed;

        private void Awake()
        {
            CacheReferences();
        }

        private void OnEnable()
        {
            CacheReferences();
            SubscribeToHealth();

            if (health != null &&
                health.IsDead)
            {
                HandleDied();
            }
        }

        private void OnDisable()
        {
            UnsubscribeFromHealth();
        }

        private void CacheReferences()
        {
            character ??=
                GetComponent<CharacterContext>();

            health =
                character != null
                    ? character.Health
                    : GetComponent<HealthController>();

            if (visualRoot == null)
            {
                visualRoot =
                    transform.Find("Visual");
            }

            if (visualRoot != null)
            {
                originalVisualScale =
                    visualRoot.localScale;
            }
        }

        private void SubscribeToHealth()
        {
            if (health == null ||
                isSubscribed)
            {
                return;
            }

            health.Died +=
                HandleDied;

            health.Revived +=
                HandleRevived;

            isSubscribed = true;
        }

        private void UnsubscribeFromHealth()
        {
            if (health == null ||
                !isSubscribed)
            {
                return;
            }

            health.Died -=
                HandleDied;

            health.Revived -=
                HandleRevived;

            isSubscribed = false;
        }

        private void HandleDied()
        {
            if (deathProcessed)
            {
                return;
            }

            deathProcessed = true;

            if (playDeathAnimation)
            {
                character?.AnimationController?.
                    PlayDeath();
            }

            character?.Targeting?.
                ClearTarget();

            if (disableColliders)
            {
                DisableCharacterColliders();
            }

            if (disableGameplayControllers)
            {
                DisableControllers();
            }

            if (deathCoroutine != null)
            {
                StopCoroutine(deathCoroutine);
            }

            deathCoroutine =
                StartCoroutine(PlayDeathReaction());
        }

        private void HandleRevived()
        {
            deathProcessed = false;

            if (deathCoroutine != null)
            {
                StopCoroutine(deathCoroutine);
                deathCoroutine = null;
            }

            RestoreVisual();
            RestoreColliders();
            RestoreControllers();
        }

        private IEnumerator PlayDeathReaction()
        {
            if (delayBeforeShrink > 0f)
            {
                yield return new WaitForSeconds(
                    delayBeforeShrink);
            }

            if (visualRoot != null)
            {
                Vector3 startingScale =
                    visualRoot.localScale;

                float elapsed = 0f;

                while (elapsed < shrinkDuration)
                {
                    elapsed +=
                        Time.deltaTime;

                    float progress =
                        shrinkDuration > 0f
                            ? Mathf.Clamp01(
                                elapsed /
                                shrinkDuration)
                            : 1f;

                    float easedProgress =
                        progress *
                        progress;

                    visualRoot.localScale =
                        Vector3.Lerp(
                            startingScale,
                            Vector3.zero,
                            easedProgress);

                    yield return null;
                }

                visualRoot.localScale =
                    Vector3.zero;

                visualRoot.gameObject.SetActive(
                    false);
            }

            deathCoroutine = null;

            if (!destroyAfterDeath)
            {
                yield break;
            }

            if (destroyDelay > 0f)
            {
                yield return new WaitForSeconds(
                    destroyDelay);
            }

            Destroy(gameObject);
        }

        private void DisableCharacterColliders()
        {
            colliderStates.Clear();

            Collider[] colliders =
                GetComponentsInChildren<Collider>(
                    includeInactive: true);

            foreach (Collider characterCollider in colliders)
            {
                if (characterCollider == null)
                {
                    continue;
                }

                colliderStates.Add(
                    new ColliderState(
                        characterCollider,
                        characterCollider.enabled));

                characterCollider.enabled =
                    false;
            }
        }

        private void RestoreColliders()
        {
            foreach (ColliderState state in colliderStates)
            {
                if (state.Collider == null)
                {
                    continue;
                }

                state.Collider.enabled =
                    state.WasEnabled;
            }

            colliderStates.Clear();
        }

        private void DisableControllers()
        {
            controllerStates.Clear();

            RememberAndDisable(
                character?.Movement);

            RememberAndDisable(
                character?.Combat);

            RememberAndDisable(
                character?.Abilities);

            RememberAndDisable(
                character?.Targeting);
        }

        private void RememberAndDisable(
            Behaviour controller)
        {
            if (controller == null)
            {
                return;
            }

            controllerStates.Add(
                new BehaviourState(
                    controller,
                    controller.enabled));

            controller.enabled =
                false;
        }

        private void RestoreControllers()
        {
            foreach (BehaviourState state in controllerStates)
            {
                if (state.Controller == null)
                {
                    continue;
                }

                state.Controller.enabled =
                    state.WasEnabled;
            }

            controllerStates.Clear();
        }

        private void RestoreVisual()
        {
            if (visualRoot == null)
            {
                return;
            }

            visualRoot.gameObject.SetActive(
                true);

            visualRoot.localScale =
                originalVisualScale;
        }

        private void OnDestroy()
        {
            UnsubscribeFromHealth();
        }

        private sealed class ColliderState
        {
            public ColliderState(
                Collider characterCollider,
                bool wasEnabled)
            {
                Collider =
                    characterCollider;

                WasEnabled =
                    wasEnabled;
            }

            public Collider Collider { get; }

            public bool WasEnabled { get; }
        }

        private sealed class BehaviourState
        {
            public BehaviourState(
                Behaviour controller,
                bool wasEnabled)
            {
                Controller =
                    controller;

                WasEnabled =
                    wasEnabled;
            }

            public Behaviour Controller { get; }

            public bool WasEnabled { get; }
        }
    }
}