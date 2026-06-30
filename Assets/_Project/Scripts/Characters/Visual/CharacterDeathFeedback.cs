using System.Collections;
using System.Collections.Generic;
using Riftborn.Characters.Core;
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

        [SerializeField, Min(0f)]
        private float delayBeforeShrink = 0.15f;

        [SerializeField, Min(0f)]
        private float shrinkDuration = 0.25f;

        [Header("Optional Destruction")]
        [Tooltip(
            "Deixe desmarcado para inimigos que possuem respawn.")]
        [SerializeField]
        private bool destroyAfterDeath;

        [SerializeField, Min(0f)]
        private float destroyDelay = 2f;

        private readonly List<ColliderState>
            colliderStates = new();

        private Coroutine deathCoroutine;
        private Vector3 originalVisualScale;
        private bool visualStateCached;
        private bool deathProcessed;

        public bool DeathProcessed =>
            deathProcessed;

        private void Awake()
        {
            CacheReferences();
        }

        private void OnValidate()
        {
            delayBeforeShrink =
                Mathf.Max(
                    0f,
                    delayBeforeShrink);

            shrinkDuration =
                Mathf.Max(
                    0f,
                    shrinkDuration);

            destroyDelay =
                Mathf.Max(
                    0f,
                    destroyDelay);
        }

        public void PlayDeath()
        {
            CacheReferences();

            if (deathProcessed)
            {
                return;
            }

            deathProcessed =
                true;

            if (playDeathAnimation)
            {
                character?.AnimationController?.
                    PlayDeath();
            }

            if (disableColliders)
            {
                DisableCharacterColliders();
            }

            if (deathCoroutine != null)
            {
                StopCoroutine(
                    deathCoroutine);
            }

            deathCoroutine =
                StartCoroutine(
                    PlayDeathReaction());
        }

        public void RestoreAfterRevive()
        {
            deathProcessed =
                false;

            if (deathCoroutine != null)
            {
                StopCoroutine(
                    deathCoroutine);

                deathCoroutine =
                    null;
            }

            RestoreVisual();
            RestoreColliders();
        }

        private void CacheReferences()
        {
            character ??=
                GetComponent<CharacterContext>();

            if (visualRoot == null)
            {
                visualRoot =
                    transform.Find("Visual");
            }

            if (visualRoot != null &&
                !visualStateCached)
            {
                originalVisualScale =
                    visualRoot.localScale;

                visualStateCached =
                    true;
            }
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

            deathCoroutine =
                null;

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

        private void RestoreVisual()
        {
            if (visualRoot == null)
            {
                return;
            }

            visualRoot.gameObject.SetActive(
                true);

            if (visualStateCached)
            {
                visualRoot.localScale =
                    originalVisualScale;
            }
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
    }
}
