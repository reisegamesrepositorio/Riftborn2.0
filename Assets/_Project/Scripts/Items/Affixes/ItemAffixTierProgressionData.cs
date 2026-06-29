using System.Collections.Generic;
using UnityEngine;

namespace Riftborn.Items
{
    public enum AffixProgressionCurve
    {
        Linear = 0,
        EaseIn = 1,
        EaseOut = 2,
        SmoothStep = 3
    }

    [CreateAssetMenu(
        fileName = "NewAffixTierProgression",
        menuName = "Riftborn/Items/Affix Tier Progression")]
    public sealed class ItemAffixTierProgressionData :
        ScriptableObject
    {
        private const int StrongestTier = 1;
        private const int WeakestTier = 10;
        private const int TierCount = 10;

        [Header("Values")]
        [Tooltip(
            "Valor máximo possível no Tier 10, " +
            "o tier mais fraco.")]
        [SerializeField]
        private float weakestTierValue = 1f;

        [Tooltip(
            "Valor máximo possível no Tier 1, " +
            "o tier mais forte.")]
        [SerializeField]
        private float strongestTierValue = 10f;

        [Tooltip(
            "Diferença entre o valor mínimo e máximo " +
            "de cada tier. O Tier 10 começa fechado no " +
            "valor mais fraco.")]
        [SerializeField, Min(0f)]
        private float rollRange = 1f;

        [Tooltip(
            "Passo utilizado para arredondar os limites " +
            "e o valor final sorteado. " +
            "Use 1 para números inteiros, " +
            "0.1 para uma casa decimal e " +
            "0.01 para percentuais armazenados como decimal.")]
        [SerializeField, Min(0f)]
        private float roundingStep = 1f;

        [SerializeField]
        private AffixProgressionCurve valueCurve =
            AffixProgressionCurve.Linear;

        [Header("Tier Weights")]
        [Tooltip(
            "Peso do Tier 10. Normalmente é o tier " +
            "mais comum.")]
        [SerializeField, Min(0f)]
        private float weakestTierWeight = 100f;

        [Tooltip(
            "Peso do Tier 1. Normalmente é o tier " +
            "mais raro.")]
        [SerializeField, Min(0f)]
        private float strongestTierWeight = 10f;

        [SerializeField]
        private AffixProgressionCurve weightCurve =
            AffixProgressionCurve.Linear;

        public float WeakestTierValue =>
            Mathf.Min(
                weakestTierValue,
                strongestTierValue);

        public float StrongestTierValue =>
            Mathf.Max(
                weakestTierValue,
                strongestTierValue);

        public float RollRange =>
            Mathf.Max(
                0f,
                rollRange);

        public float RoundingStep =>
            Mathf.Max(
                0f,
                roundingStep);

        public float WeakestTierWeight =>
            Mathf.Max(
                0f,
                weakestTierWeight);

        public float StrongestTierWeight =>
            Mathf.Max(
                0f,
                strongestTierWeight);

        public IReadOnlyList<ItemAffixTierDefinition>
            GenerateTierDefinitions()
        {
            List<ItemAffixTierDefinition> definitions =
                new(TierCount);

            for (int tier = StrongestTier;
                 tier <= WeakestTier;
                 tier++)
            {
                float strength =
                    GetTierStrength(
                        tier);

                float curvedStrength =
                    EvaluateCurve(
                        strength,
                        valueCurve);

                float tierMaximum =
                    Mathf.Lerp(
                        WeakestTierValue,
                        StrongestTierValue,
                        curvedStrength);

                tierMaximum =
                    RoundGeneratedValue(
                        tierMaximum);

                float tierMinimum;

                if (tier == WeakestTier)
                {
                    tierMinimum =
                        tierMaximum;
                }
                else
                {
                    tierMinimum =
                        Mathf.Max(
                            WeakestTierValue,
                            tierMaximum -
                            RollRange);

                    tierMinimum =
                        RoundGeneratedValue(
                            tierMinimum);
                }

                float weightStrength =
                    EvaluateCurve(
                        strength,
                        weightCurve);

                float tierWeight =
                    Mathf.Lerp(
                        WeakestTierWeight,
                        StrongestTierWeight,
                        weightStrength);

                tierWeight =
                    Mathf.Max(
                        0f,
                        tierWeight);

                ItemAffixTierDefinition definition =
                    new ItemAffixTierDefinition(
                        tier,
                        tierMinimum,
                        tierMaximum,
                        tierWeight);

                definitions.Add(
                    definition);
            }

            return definitions;
        }

        public float GetGeneratedMinimumValue(
            int tier)
        {
            int safeTier =
                Mathf.Clamp(
                    tier,
                    StrongestTier,
                    WeakestTier);

            float maximum =
                GetGeneratedMaximumValue(
                    safeTier);

            if (safeTier == WeakestTier)
            {
                return maximum;
            }

            float minimum =
                Mathf.Max(
                    WeakestTierValue,
                    maximum -
                    RollRange);

            return RoundGeneratedValue(
                minimum);
        }

        public float GetGeneratedMaximumValue(
            int tier)
        {
            int safeTier =
                Mathf.Clamp(
                    tier,
                    StrongestTier,
                    WeakestTier);

            float strength =
                GetTierStrength(
                    safeTier);

            float curvedStrength =
                EvaluateCurve(
                    strength,
                    valueCurve);

            float value =
                Mathf.Lerp(
                    WeakestTierValue,
                    StrongestTierValue,
                    curvedStrength);

            return RoundGeneratedValue(
                value);
        }

        public float GetGeneratedWeight(
            int tier)
        {
            int safeTier =
                Mathf.Clamp(
                    tier,
                    StrongestTier,
                    WeakestTier);

            float strength =
                GetTierStrength(
                    safeTier);

            float curvedStrength =
                EvaluateCurve(
                    strength,
                    weightCurve);

            float weight =
                Mathf.Lerp(
                    WeakestTierWeight,
                    StrongestTierWeight,
                    curvedStrength);

            return Mathf.Max(
                0f,
                weight);
        }

        public float RoundGeneratedValue(
            float value)
        {
            float step =
                RoundingStep;

            if (step <= 0f)
            {
                return value;
            }

            float roundedValue =
                Mathf.Round(
                    value /
                    step) *
                step;

            /*
             * Remove pequenos erros de precisão,
             * como 0.19999999 em vez de 0.2.
             */
            return Mathf.Round(
                       roundedValue *
                       10000f) /
                   10000f;
        }

        private static float GetTierStrength(
            int tier)
        {
            /*
             * Tier 10:
             * strength = 0
             *
             * Tier 1:
             * strength = 1
             */
            return Mathf.InverseLerp(
                WeakestTier,
                StrongestTier,
                tier);
        }

        private static float EvaluateCurve(
            float value,
            AffixProgressionCurve curve)
        {
            float normalizedValue =
                Mathf.Clamp01(
                    value);

            switch (curve)
            {
                case AffixProgressionCurve.EaseIn:
                    return normalizedValue *
                           normalizedValue;

                case AffixProgressionCurve.EaseOut:
                {
                    float inverted =
                        1f -
                        normalizedValue;

                    return 1f -
                           inverted *
                           inverted;
                }

                case AffixProgressionCurve.SmoothStep:
                    return normalizedValue *
                           normalizedValue *
                           (3f -
                            2f *
                            normalizedValue);

                case AffixProgressionCurve.Linear:
                default:
                    return normalizedValue;
            }
        }

        private void OnValidate()
        {
            if (weakestTierValue >
                strongestTierValue)
            {
                (
                    weakestTierValue,
                    strongestTierValue
                ) =
                (
                    strongestTierValue,
                    weakestTierValue
                );
            }

            rollRange =
                Mathf.Max(
                    0f,
                    rollRange);

            roundingStep =
                Mathf.Max(
                    0f,
                    roundingStep);

            weakestTierWeight =
                Mathf.Max(
                    0f,
                    weakestTierWeight);

            strongestTierWeight =
                Mathf.Max(
                    0f,
                    strongestTierWeight);
        }
    }
}