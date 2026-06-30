using System.Collections;
using System.Collections.Generic;
using Riftborn.Characters.Core;
using Riftborn.Damage;
using UnityEngine;

namespace Riftborn.Characters.Visual
{
    public sealed class CharacterHitFeedback : MonoBehaviour
    {
        private static readonly int BaseColorId =
            Shader.PropertyToID("_BaseColor");

        private static readonly int ColorId =
            Shader.PropertyToID("_Color");

        private static readonly int EmissionColorId =
            Shader.PropertyToID("_EmissionColor");

        [Header("Character")]
        [SerializeField]
        private CharacterContext character;

        [Header("Visual")]
        [Tooltip(
            "Objeto visual que vai reagir ao dano. " +
            "No EnemyDummy, arraste o objeto Visual.")]
        [SerializeField]
        private Transform visualTransform;

        [SerializeField]
        private Renderer[] targetRenderers;

        [Header("Color Flash")]
        [SerializeField]
        private bool useColorFlash = true;

        [SerializeField]
        private Color flashColor =
            Color.white;

        [SerializeField, Min(0.01f)]
        private float flashDuration = 0.12f;

        [Header("Scale Reaction")]
        [SerializeField]
        private bool useScaleReaction = true;

        [SerializeField, Range(0.5f, 1f)]
        private float compressedScale = 0.88f;

        [SerializeField, Min(0.01f)]
        private float compressionDuration = 0.05f;

        [SerializeField, Min(0.01f)]
        private float recoveryDuration = 0.1f;

        [Header("Animation")]
        [SerializeField]
        private bool playHurtAnimation = true;

        [Header("Debug")]
        [SerializeField]
        private bool showDebugLogs;

        private readonly List<MaterialState>
            materialStates = new();

        private Coroutine feedbackCoroutine;
        private Vector3 originalScale;
        private bool visualStateCached;

        private void Awake()
        {
            CacheReferences();
            PrepareMaterials();
        }

        private void OnEnable()
        {
            CacheReferences();
            PrepareMaterials();
        }

        private void OnDisable()
        {
            StopCurrentFeedback();
            RestoreVisual();
        }

        public void PlayHit(
            DamageResult damageResult)
        {
            CacheReferences();

            if (showDebugLogs)
            {
                Debug.Log(
                    $"[HIT FEEDBACK] {name} recebeu dano.",
                    this);
            }

            if (playHurtAnimation)
            {
                character?.AnimationController?.
                    PlayHurt();
            }

            StopCurrentFeedback();

            feedbackCoroutine =
                StartCoroutine(
                    PlayFeedback());
        }

        public void ResetFeedback()
        {
            StopCurrentFeedback();
            RestoreVisual();
        }

        private void CacheReferences()
        {
            character ??=
                GetComponent<CharacterContext>();

            if (visualTransform == null)
            {
                Transform visual =
                    transform.Find("Visual");

                visualTransform =
                    visual != null
                        ? visual
                        : transform;
            }

            if (targetRenderers == null ||
                targetRenderers.Length == 0)
            {
                targetRenderers =
                    visualTransform.GetComponentsInChildren<Renderer>(
                        includeInactive: true);
            }

            if (!visualStateCached)
            {
                originalScale =
                    visualTransform.localScale;

                visualStateCached =
                    true;
            }
        }

        private void PrepareMaterials()
        {
            materialStates.Clear();

            if (targetRenderers == null)
            {
                return;
            }

            foreach (Renderer targetRenderer in targetRenderers)
            {
                if (targetRenderer == null)
                {
                    continue;
                }

                Material[] materials =
                    targetRenderer.materials;

                foreach (Material material in materials)
                {
                    if (material == null)
                    {
                        continue;
                    }

                    bool hasBaseColor =
                        material.HasProperty(
                            BaseColorId);

                    bool hasColor =
                        material.HasProperty(
                            ColorId);

                    if (!hasBaseColor &&
                        !hasColor)
                    {
                        continue;
                    }

                    int colorPropertyId =
                        hasBaseColor
                            ? BaseColorId
                            : ColorId;

                    Color originalColor =
                        material.GetColor(
                            colorPropertyId);

                    bool hasEmission =
                        material.HasProperty(
                            EmissionColorId);

                    Color originalEmission =
                        hasEmission
                            ? material.GetColor(
                                EmissionColorId)
                            : Color.black;

                    materialStates.Add(
                        new MaterialState(
                            material,
                            colorPropertyId,
                            originalColor,
                            hasEmission,
                            originalEmission));
                }
            }

            if (showDebugLogs)
            {
                Debug.Log(
                    $"[HIT FEEDBACK] {name}: " +
                    $"{materialStates.Count} material(is) preparado(s).",
                    this);
            }
        }

        private IEnumerator PlayFeedback()
        {
            if (useColorFlash)
            {
                ApplyFlashColor();
            }

            if (useScaleReaction &&
                visualTransform != null)
            {
                Vector3 compressed =
                    new Vector3(
                        originalScale.x,
                        originalScale.y * compressedScale,
                        originalScale.z);

                yield return AnimateScale(
                    originalScale,
                    compressed,
                    compressionDuration);

                yield return AnimateScale(
                    compressed,
                    originalScale,
                    recoveryDuration);
            }
            else
            {
                yield return new WaitForSeconds(
                    flashDuration);
            }

            if (useColorFlash)
            {
                RestoreMaterials();
            }

            if (visualTransform != null)
            {
                visualTransform.localScale =
                    originalScale;
            }

            feedbackCoroutine =
                null;
        }

        private IEnumerator AnimateScale(
            Vector3 from,
            Vector3 to,
            float duration)
        {
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed +=
                    Time.deltaTime;

                float progress =
                    duration > 0f
                        ? Mathf.Clamp01(
                            elapsed / duration)
                        : 1f;

                visualTransform.localScale =
                    Vector3.Lerp(
                        from,
                        to,
                        progress);

                yield return null;
            }

            visualTransform.localScale =
                to;
        }

        private void ApplyFlashColor()
        {
            foreach (MaterialState state in materialStates)
            {
                if (state.Material == null)
                {
                    continue;
                }

                state.Material.SetColor(
                    state.ColorPropertyId,
                    flashColor);

                if (state.HasEmission)
                {
                    state.Material.EnableKeyword(
                        "_EMISSION");

                    state.Material.SetColor(
                        EmissionColorId,
                        flashColor * 1.5f);
                }
            }
        }

        private void RestoreMaterials()
        {
            foreach (MaterialState state in materialStates)
            {
                if (state.Material == null)
                {
                    continue;
                }

                state.Material.SetColor(
                    state.ColorPropertyId,
                    state.OriginalColor);

                if (state.HasEmission)
                {
                    state.Material.SetColor(
                        EmissionColorId,
                        state.OriginalEmission);
                }
            }
        }

        private void StopCurrentFeedback()
        {
            if (feedbackCoroutine == null)
            {
                return;
            }

            StopCoroutine(
                feedbackCoroutine);

            feedbackCoroutine =
                null;
        }

        private void RestoreVisual()
        {
            RestoreMaterials();

            if (visualTransform != null &&
                visualStateCached)
            {
                visualTransform.localScale =
                    originalScale;
            }
        }

        private void OnDestroy()
        {
            RestoreVisual();
        }

        private sealed class MaterialState
        {
            public MaterialState(
                Material material,
                int colorPropertyId,
                Color originalColor,
                bool hasEmission,
                Color originalEmission)
            {
                Material = material;
                ColorPropertyId = colorPropertyId;
                OriginalColor = originalColor;
                HasEmission = hasEmission;
                OriginalEmission = originalEmission;
            }

            public Material Material { get; }

            public int ColorPropertyId { get; }

            public Color OriginalColor { get; }

            public bool HasEmission { get; }

            public Color OriginalEmission { get; }
        }
    }
}
