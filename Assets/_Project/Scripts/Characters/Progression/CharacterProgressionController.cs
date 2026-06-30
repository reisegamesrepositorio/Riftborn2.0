using System;
using Riftborn.Characters.Stats;
using UnityEngine;

namespace Riftborn.Characters.Progression
{
    public sealed class ExperienceChangedEventArgs : EventArgs
    {
        public ExperienceChangedEventArgs(
            int oldLevel,
            int oldExperience,
            int currentLevel,
            int currentExperience,
            int appliedExperience)
        {
            OldLevel = oldLevel;
            OldExperience = oldExperience;
            CurrentLevel = currentLevel;
            CurrentExperience = currentExperience;
            AppliedExperience = appliedExperience;
        }

        public int OldLevel { get; }

        public int OldExperience { get; }

        public int CurrentLevel { get; }

        public int CurrentExperience { get; }

        public int AppliedExperience { get; }
    }

    public sealed class LevelUpEventArgs : EventArgs
    {
        public LevelUpEventArgs(
            int oldLevel,
            int newLevel,
            int statPointsAwarded,
            int runePointsAwarded)
        {
            OldLevel = oldLevel;
            NewLevel = newLevel;
            StatPointsAwarded = statPointsAwarded;
            RunePointsAwarded = runePointsAwarded;
        }

        public int OldLevel { get; }

        public int NewLevel { get; }

        public int StatPointsAwarded { get; }

        public int RunePointsAwarded { get; }
    }

    public sealed class ProgressionPointsChangedEventArgs :
        EventArgs
    {
        public ProgressionPointsChangedEventArgs(
            int oldAvailableStatPoints,
            int availableStatPoints,
            int totalStatPointsEarned,
            int oldAvailableRunePoints,
            int availableRunePoints,
            int totalRunePointsEarned)
        {
            OldAvailableStatPoints =
                oldAvailableStatPoints;

            AvailableStatPoints =
                availableStatPoints;

            TotalStatPointsEarned =
                totalStatPointsEarned;

            OldAvailableRunePoints =
                oldAvailableRunePoints;

            AvailableRunePoints =
                availableRunePoints;

            TotalRunePointsEarned =
                totalRunePointsEarned;
        }

        public int OldAvailableStatPoints { get; }

        public int AvailableStatPoints { get; }

        public int TotalStatPointsEarned { get; }

        public int OldAvailableRunePoints { get; }

        public int AvailableRunePoints { get; }

        public int TotalRunePointsEarned { get; }
    }

    public sealed class StatPointSpentEventArgs :
        EventArgs
    {
        public StatPointSpentEventArgs(
            CharacterStat stat,
            float oldBaseValue,
            float newBaseValue,
            int remainingStatPoints)
        {
            Stat = stat;
            OldBaseValue = oldBaseValue;
            NewBaseValue = newBaseValue;
            RemainingStatPoints =
                remainingStatPoints;
        }

        public CharacterStat Stat { get; }

        public float OldBaseValue { get; }

        public float NewBaseValue { get; }

        public int RemainingStatPoints { get; }
    }

    public sealed class CharacterProgressionController :
        MonoBehaviour
    {
        private const int StatPointsPerLevel = 1;
        private const int RunePointLevelInterval = 5;
        private const float BaseStatIncreasePerPoint = 1f;

        [Header("Progression Data")]
        [SerializeField]
        private ExperienceCurveData experienceCurve;

        [Header("References")]
        [SerializeField]
        private CharacterStatsController stats;

        [Header("Starting State")]
        [SerializeField, Min(1)]
        private int startingLevel = 1;

        [SerializeField, Min(0)]
        private int startingExperience;

        [Header("Runtime Progression")]
        [SerializeField, Min(1)]
        private int currentLevel = 1;

        [SerializeField, Min(0)]
        private int currentExperience;

        [Header("Runtime Points")]
        [SerializeField, Min(0)]
        private int availableStatPoints;

        [SerializeField, Min(0)]
        private int totalStatPointsEarned;

        [SerializeField, Min(0)]
        private int availableRunePoints;

        [SerializeField, Min(0)]
        private int totalRunePointsEarned;

        [Header("Debug")]
        [SerializeField]
        private bool showDebugLogs = true;

        private bool initialized;

        public event Action<ExperienceChangedEventArgs>
            ExperienceChanged;

        public event Action<LevelUpEventArgs>
            LeveledUp;

        public event Action<ProgressionPointsChangedEventArgs>
            PointsChanged;

        public event Action<StatPointSpentEventArgs>
            StatPointSpent;

        public ExperienceCurveData ExperienceCurve =>
            experienceCurve;

        public int CurrentLevel
        {
            get
            {
                EnsureInitialized();
                return currentLevel;
            }
        }

        public int CurrentExperience
        {
            get
            {
                EnsureInitialized();
                return currentExperience;
            }
        }

        public int AvailableStatPoints
        {
            get
            {
                EnsureInitialized();
                return availableStatPoints;
            }
        }

        public int TotalStatPointsEarned
        {
            get
            {
                EnsureInitialized();
                return totalStatPointsEarned;
            }
        }

        public int TotalStatPointsSpent =>
            Mathf.Max(
                0,
                TotalStatPointsEarned -
                AvailableStatPoints);

        public int AvailableRunePoints
        {
            get
            {
                EnsureInitialized();
                return availableRunePoints;
            }
        }

        public int TotalRunePointsEarned
        {
            get
            {
                EnsureInitialized();
                return totalRunePointsEarned;
            }
        }

        public int MaximumLevel =>
            experienceCurve != null
                ? experienceCurve.MaximumLevel
                : 1;

        public bool IsMaximumLevel
        {
            get
            {
                EnsureInitialized();

                return experienceCurve == null ||
                       currentLevel >=
                       experienceCurve.MaximumLevel;
            }
        }

        public int ExperienceRequiredForNextLevel
        {
            get
            {
                EnsureInitialized();

                if (experienceCurve == null ||
                    IsMaximumLevel)
                {
                    return 0;
                }

                return experienceCurve
                    .GetExperienceRequiredForNextLevel(
                        currentLevel);
            }
        }

        public int ExperienceRemainingForNextLevel
        {
            get
            {
                int required =
                    ExperienceRequiredForNextLevel;

                return required > 0
                    ? Mathf.Max(
                        0,
                        required -
                        CurrentExperience)
                    : 0;
            }
        }

        public float LevelProgress01
        {
            get
            {
                int required =
                    ExperienceRequiredForNextLevel;

                if (required <= 0)
                {
                    return 1f;
                }

                return Mathf.Clamp01(
                    (float)CurrentExperience /
                    required);
            }
        }

        private void Awake()
        {
            CacheReferences();
            InitializeFromStartingState();
        }

        private void Reset()
        {
            CacheReferences();
        }

        private void OnValidate()
        {
            startingLevel =
                Mathf.Max(
                    1,
                    startingLevel);

            startingExperience =
                Mathf.Max(
                    0,
                    startingExperience);

            availableStatPoints =
                Mathf.Max(
                    0,
                    availableStatPoints);

            totalStatPointsEarned =
                Mathf.Max(
                    availableStatPoints,
                    totalStatPointsEarned);

            availableRunePoints =
                Mathf.Max(
                    0,
                    availableRunePoints);

            totalRunePointsEarned =
                Mathf.Max(
                    availableRunePoints,
                    totalRunePointsEarned);
        }

        public bool AddExperience(
            int amount)
        {
            EnsureInitialized();

            if (amount <= 0)
            {
                return false;
            }

            if (experienceCurve == null)
            {
                Debug.LogError(
                    $"[{nameof(CharacterProgressionController)}] " +
                    "No ExperienceCurveData is assigned.",
                    this);

                return false;
            }

            if (IsMaximumLevel)
            {
                return false;
            }

            int oldLevel =
                currentLevel;

            int oldExperience =
                currentExperience;

            int remainingExperience =
                amount;

            while (remainingExperience > 0 &&
                   currentLevel <
                   experienceCurve.MaximumLevel)
            {
                int requiredExperience =
                    experienceCurve
                        .GetExperienceRequiredForNextLevel(
                            currentLevel);

                if (requiredExperience <= 0)
                {
                    break;
                }

                int neededExperience =
                    Mathf.Max(
                        0,
                        requiredExperience -
                        currentExperience);

                if (remainingExperience <
                    neededExperience)
                {
                    currentExperience +=
                        remainingExperience;

                    remainingExperience = 0;

                    break;
                }

                remainingExperience -=
                    neededExperience;

                int previousLevel =
                    currentLevel;

                currentLevel++;
                currentExperience = 0;

                GrantRewardsForReachedLevel(
                    previousLevel,
                    currentLevel,
                    invokeLevelEvent: true);
            }

            if (currentLevel >=
                experienceCurve.MaximumLevel)
            {
                currentLevel =
                    experienceCurve.MaximumLevel;

                currentExperience = 0;
            }

            int appliedExperience =
                amount -
                remainingExperience;

            ExperienceChanged?.Invoke(
                new ExperienceChangedEventArgs(
                    oldLevel,
                    oldExperience,
                    currentLevel,
                    currentExperience,
                    appliedExperience));

            if (showDebugLogs)
            {
                Debug.Log(
                    $"[PROGRESSION] XP +{appliedExperience} | " +
                    $"Level: {currentLevel} | " +
                    $"XP: {currentExperience}/" +
                    $"{ExperienceRequiredForNextLevel} | " +
                    $"Stat Points: {availableStatPoints} | " +
                    $"Rune Points: {availableRunePoints}",
                    this);
            }

            return appliedExperience > 0;
        }

        public bool TrySpendStatPoint(
            CharacterStat stat)
        {
            EnsureInitialized();
            CacheReferences();

            if (availableStatPoints <= 0)
            {
                if (showDebugLogs)
                {
                    Debug.LogWarning(
                        "[PROGRESSION] No stat points are available.",
                        this);
                }

                return false;
            }

            if (stats == null)
            {
                Debug.LogError(
                    $"[{nameof(CharacterProgressionController)}] " +
                    $"No {nameof(CharacterStatsController)} " +
                    "is assigned.",
                    this);

                return false;
            }

            if (!Enum.IsDefined(
                    typeof(CharacterStat),
                    stat))
            {
                return false;
            }

            int oldAvailableStatPoints =
                availableStatPoints;

            int oldAvailableRunePoints =
                availableRunePoints;

            float oldBaseValue =
                stats.GetBaseValue(
                    stat);

            float newBaseValue =
                stats.AddBaseValue(
                    stat,
                    BaseStatIncreasePerPoint);

            availableStatPoints--;

            PointsChanged?.Invoke(
                new ProgressionPointsChangedEventArgs(
                    oldAvailableStatPoints,
                    availableStatPoints,
                    totalStatPointsEarned,
                    oldAvailableRunePoints,
                    availableRunePoints,
                    totalRunePointsEarned));

            StatPointSpent?.Invoke(
                new StatPointSpentEventArgs(
                    stat,
                    oldBaseValue,
                    newBaseValue,
                    availableStatPoints));

            if (showDebugLogs)
            {
                Debug.Log(
                    $"[PROGRESSION] Spent 1 point on {stat}. " +
                    $"Base: {oldBaseValue:0.##} -> " +
                    $"{newBaseValue:0.##} | " +
                    $"Remaining stat points: " +
                    $"{availableStatPoints}",
                    this);
            }

            return true;
        }

        public bool SetProgression(
            int level,
            int experience,
            bool notify = true,
            bool grantLevelRewards = true)
        {
            EnsureInitialized();

            if (experienceCurve == null)
            {
                return false;
            }

            int oldLevel =
                currentLevel;

            int oldExperience =
                currentExperience;

            int targetLevel =
                Mathf.Clamp(
                    level,
                    1,
                    experienceCurve.MaximumLevel);

            if (grantLevelRewards &&
                targetLevel >
                currentLevel)
            {
                for (int reachedLevel =
                         currentLevel + 1;
                     reachedLevel <= targetLevel;
                     reachedLevel++)
                {
                    GrantRewardsForReachedLevel(
                        reachedLevel - 1,
                        reachedLevel,
                        invokeLevelEvent: notify);
                }
            }

            currentLevel =
                targetLevel;

            currentExperience =
                Mathf.Max(
                    0,
                    experience);

            NormalizeProgression(
                invokeLevelEvents: notify,
                grantLevelRewards:
                    grantLevelRewards);

            if (notify)
            {
                ExperienceChanged?.Invoke(
                    new ExperienceChangedEventArgs(
                        oldLevel,
                        oldExperience,
                        currentLevel,
                        currentExperience,
                        0));
            }

            return true;
        }

        public bool SetPointState(
            int newAvailableStatPoints,
            int newTotalStatPointsEarned,
            int newAvailableRunePoints,
            int newTotalRunePointsEarned,
            bool notify = true)
        {
            EnsureInitialized();

            if (newAvailableStatPoints < 0 ||
                newTotalStatPointsEarned < 0 ||
                newAvailableRunePoints < 0 ||
                newTotalRunePointsEarned < 0 ||
                newAvailableStatPoints >
                newTotalStatPointsEarned ||
                newAvailableRunePoints >
                newTotalRunePointsEarned)
            {
                return false;
            }

            int oldAvailableStatPoints =
                availableStatPoints;

            int oldAvailableRunePoints =
                availableRunePoints;

            availableStatPoints =
                newAvailableStatPoints;

            totalStatPointsEarned =
                newTotalStatPointsEarned;

            availableRunePoints =
                newAvailableRunePoints;

            totalRunePointsEarned =
                newTotalRunePointsEarned;

            if (notify)
            {
                PointsChanged?.Invoke(
                    new ProgressionPointsChangedEventArgs(
                        oldAvailableStatPoints,
                        availableStatPoints,
                        totalStatPointsEarned,
                        oldAvailableRunePoints,
                        availableRunePoints,
                        totalRunePointsEarned));
            }

            return true;
        }

        [ContextMenu("Spend Stat Point: STR")]
        private void SpendPointOnSTR()
        {
            TrySpendStatPoint(
                CharacterStat.STR);
        }

        [ContextMenu("Spend Stat Point: DEX")]
        private void SpendPointOnDEX()
        {
            TrySpendStatPoint(
                CharacterStat.DEX);
        }

        [ContextMenu("Spend Stat Point: WIS")]
        private void SpendPointOnWIS()
        {
            TrySpendStatPoint(
                CharacterStat.WIS);
        }

        [ContextMenu("Spend Stat Point: ISP")]
        private void SpendPointOnISP()
        {
            TrySpendStatPoint(
                CharacterStat.ISP);
        }

        [ContextMenu("Spend Stat Point: FORT")]
        private void SpendPointOnFORT()
        {
            TrySpendStatPoint(
                CharacterStat.FORT);
        }

        [ContextMenu("Log Current Progression")]
        public void LogCurrentProgression()
        {
            EnsureInitialized();

            Debug.Log(
                $"[PROGRESSION VALUES] " +
                $"Level: {currentLevel}/" +
                $"{MaximumLevel} | " +
                $"XP: {currentExperience}/" +
                $"{ExperienceRequiredForNextLevel} | " +
                $"Remaining XP: " +
                $"{ExperienceRemainingForNextLevel} | " +
                $"Progress: " +
                $"{LevelProgress01 * 100f:0.##}% | " +
                $"Stat Points: " +
                $"{availableStatPoints} available / " +
                $"{totalStatPointsEarned} earned | " +
                $"Rune Points: " +
                $"{availableRunePoints} available / " +
                $"{totalRunePointsEarned} earned",
                this);
        }

        private void InitializeFromStartingState()
        {
            if (initialized)
            {
                return;
            }

            currentLevel =
                experienceCurve != null
                    ? Mathf.Clamp(
                        startingLevel,
                        1,
                        experienceCurve.MaximumLevel)
                    : Mathf.Max(
                        1,
                        startingLevel);

            currentExperience =
                Mathf.Max(
                    0,
                    startingExperience);

            totalStatPointsEarned =
                Mathf.Max(
                    0,
                    currentLevel - 1);

            availableStatPoints =
                totalStatPointsEarned;

            totalRunePointsEarned =
                CountRunePointsThroughLevel(
                    currentLevel);

            availableRunePoints =
                totalRunePointsEarned;

            initialized = true;

            NormalizeProgression(
                invokeLevelEvents: false,
                grantLevelRewards: true);

            if (experienceCurve == null)
            {
                Debug.LogWarning(
                    $"[{nameof(CharacterProgressionController)}] " +
                    "Assign an ExperienceCurveData asset.",
                    this);
            }
        }

        private void EnsureInitialized()
        {
            if (!initialized)
            {
                CacheReferences();
                InitializeFromStartingState();
            }
        }

        private void NormalizeProgression(
            bool invokeLevelEvents,
            bool grantLevelRewards)
        {
            if (experienceCurve == null)
            {
                currentLevel =
                    Mathf.Max(
                        1,
                        currentLevel);

                currentExperience =
                    Mathf.Max(
                        0,
                        currentExperience);

                return;
            }

            currentLevel =
                Mathf.Clamp(
                    currentLevel,
                    1,
                    experienceCurve.MaximumLevel);

            currentExperience =
                Mathf.Max(
                    0,
                    currentExperience);

            while (currentLevel <
                   experienceCurve.MaximumLevel)
            {
                int requiredExperience =
                    experienceCurve
                        .GetExperienceRequiredForNextLevel(
                            currentLevel);

                if (requiredExperience <= 0 ||
                    currentExperience <
                    requiredExperience)
                {
                    break;
                }

                currentExperience -=
                    requiredExperience;

                int previousLevel =
                    currentLevel;

                currentLevel++;

                if (grantLevelRewards)
                {
                    GrantRewardsForReachedLevel(
                        previousLevel,
                        currentLevel,
                        invokeLevelEvents);
                }
                else if (invokeLevelEvents)
                {
                    LeveledUp?.Invoke(
                        new LevelUpEventArgs(
                            previousLevel,
                            currentLevel,
                            0,
                            0));
                }
            }

            if (currentLevel >=
                experienceCurve.MaximumLevel)
            {
                currentLevel =
                    experienceCurve.MaximumLevel;

                currentExperience = 0;
            }
        }

        private void GrantRewardsForReachedLevel(
            int oldLevel,
            int newLevel,
            bool invokeLevelEvent)
        {
            int oldAvailableStatPoints =
                availableStatPoints;

            int oldAvailableRunePoints =
                availableRunePoints;

            int awardedStatPoints =
                StatPointsPerLevel;

            int awardedRunePoints =
                newLevel %
                RunePointLevelInterval == 0
                    ? 1
                    : 0;

            availableStatPoints +=
                awardedStatPoints;

            totalStatPointsEarned +=
                awardedStatPoints;

            availableRunePoints +=
                awardedRunePoints;

            totalRunePointsEarned +=
                awardedRunePoints;

            PointsChanged?.Invoke(
                new ProgressionPointsChangedEventArgs(
                    oldAvailableStatPoints,
                    availableStatPoints,
                    totalStatPointsEarned,
                    oldAvailableRunePoints,
                    availableRunePoints,
                    totalRunePointsEarned));

            if (invokeLevelEvent)
            {
                LeveledUp?.Invoke(
                    new LevelUpEventArgs(
                        oldLevel,
                        newLevel,
                        awardedStatPoints,
                        awardedRunePoints));
            }

            if (showDebugLogs)
            {
                Debug.Log(
                    $"[PROGRESSION] Level up: " +
                    $"{oldLevel} -> {newLevel} | " +
                    $"+{awardedStatPoints} Stat Point" +
                    $"{(awardedStatPoints == 1 ? string.Empty : "s")}" +
                    (awardedRunePoints > 0
                        ? $" | +{awardedRunePoints} Rune Point"
                        : string.Empty),
                    this);
            }
        }

        private static int CountRunePointsThroughLevel(
            int level)
        {
            return Mathf.Max(
                0,
                level /
                RunePointLevelInterval);
        }

        private void CacheReferences()
        {
            stats ??=
                GetComponent<CharacterStatsController>();
        }
    }
}
