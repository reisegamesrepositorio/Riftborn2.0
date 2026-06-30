using System;
using UnityEngine;

namespace Riftborn.Characters.Progression
{
    [CreateAssetMenu(
        fileName = "ExperienceCurve_Default",
        menuName = "Riftborn/Progression/Experience Curve")]
    public sealed class ExperienceCurveData : ScriptableObject
    {
        [Header("Level Limits")]
        [SerializeField, Min(2)]
        private int maximumLevel = 50;

        [Header("Experience Formula")]
        [SerializeField, Min(1)]
        private int baseExperience = 100;

        [SerializeField, Min(0)]
        private int linearIncreasePerLevel = 50;

        [SerializeField, Min(1f)]
        private float exponentialGrowth = 1f;

        [SerializeField]
        private AnimationCurve levelMultiplier =
            AnimationCurve.Linear(
                1f,
                1f,
                50f,
                1f);

        public int MaximumLevel =>
            maximumLevel;

        public int GetExperienceRequiredForNextLevel(
            int currentLevel)
        {
            if (currentLevel < 1 ||
                currentLevel >= maximumLevel)
            {
                return 0;
            }

            double linearValue =
                baseExperience +
                (double)linearIncreasePerLevel *
                (currentLevel - 1);

            double exponentialValue =
                Math.Pow(
                    exponentialGrowth,
                    currentLevel - 1);

            float curveMultiplier =
                levelMultiplier != null
                    ? Mathf.Max(
                        0.01f,
                        levelMultiplier.Evaluate(
                            currentLevel))
                    : 1f;

            double calculatedValue =
                linearValue *
                exponentialValue *
                curveMultiplier;

            calculatedValue =
                Math.Max(
                    1d,
                    calculatedValue);

            calculatedValue =
                Math.Min(
                    int.MaxValue,
                    calculatedValue);

            return Mathf.Max(
                1,
                (int)Math.Round(
                    calculatedValue,
                    MidpointRounding.AwayFromZero));
        }

        private void OnValidate()
        {
            maximumLevel =
                Mathf.Max(
                    2,
                    maximumLevel);

            baseExperience =
                Mathf.Max(
                    1,
                    baseExperience);

            linearIncreasePerLevel =
                Mathf.Max(
                    0,
                    linearIncreasePerLevel);

            exponentialGrowth =
                Mathf.Max(
                    1f,
                    exponentialGrowth);

            levelMultiplier ??=
                AnimationCurve.Linear(
                    1f,
                    1f,
                    maximumLevel,
                    1f);
        }
    }
}
