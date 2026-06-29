using System;
using System.Collections.Generic;
using Riftborn.Characters.Stats;
using UnityEngine;

namespace Riftborn.Items
{
    [Serializable]
    public sealed class ItemAffixTierDefinition
    {
        [SerializeField, Range(1, 10)]
        private int tier = 10;

        [SerializeField]
        private float minimumValue;

        [SerializeField]
        private float maximumValue;

        [Tooltip(
            "Peso deste tier no sorteio. " +
            "Quanto maior, mais comum ele será.")]
        [SerializeField, Min(0f)]
        private float rollWeight = 1f;

        public ItemAffixTierDefinition()
        {
        }

        public ItemAffixTierDefinition(
            int tier,
            float minimumValue,
            float maximumValue,
            float rollWeight)
        {
            Configure(
                tier,
                minimumValue,
                maximumValue,
                rollWeight);
        }

        public int Tier =>
            Mathf.Clamp(
                tier,
                1,
                10);

        public float MinimumValue =>
            Mathf.Min(
                minimumValue,
                maximumValue);

        public float MaximumValue =>
            Mathf.Max(
                minimumValue,
                maximumValue);

        public float RollWeight =>
            Mathf.Max(
                0f,
                rollWeight);

        public float RollValue()
        {
            if (Mathf.Approximately(
                    MinimumValue,
                    MaximumValue))
            {
                return MinimumValue;
            }

            return UnityEngine.Random.Range(
                MinimumValue,
                MaximumValue);
        }

        public void Configure(
            int newTier,
            float newMinimumValue,
            float newMaximumValue,
            float newRollWeight)
        {
            tier =
                Mathf.Clamp(
                    newTier,
                    1,
                    10);

            minimumValue =
                Mathf.Min(
                    newMinimumValue,
                    newMaximumValue);

            maximumValue =
                Mathf.Max(
                    newMinimumValue,
                    newMaximumValue);

            rollWeight =
                Mathf.Max(
                    0f,
                    newRollWeight);
        }

        public void Validate()
        {
            tier =
                Mathf.Clamp(
                    tier,
                    1,
                    10);

            rollWeight =
                Mathf.Max(
                    0f,
                    rollWeight);

            if (minimumValue <=
                maximumValue)
            {
                return;
            }

            (
                minimumValue,
                maximumValue
            ) =
            (
                maximumValue,
                minimumValue
            );
        }
    }

    [CreateAssetMenu(
        fileName = "NewItemAffix",
        menuName = "Riftborn/Items/Item Affix")]
    public sealed class ItemAffixData : ScriptableObject
    {
        [Header("Identity")]
        [SerializeField]
        private string affixId;

        [SerializeField]
        private string displayName;

        [SerializeField]
        private ItemAffixType affixType;

        [Header("Effect")]
        [SerializeField]
        private ItemAffixEffectType effectType;

        [SerializeField]
        private ItemAffixValueMode valueMode;

        [Tooltip(
            "Utilizado somente quando Effect Type " +
            "for Character Stat.")]
        [SerializeField]
        private CharacterStat characterStat;

        [Header("Tier Generation")]
        [Tooltip(
            "Perfil opcional utilizado para gerar " +
            "automaticamente os Tiers 1 até 10.")]
        [SerializeField]
        private ItemAffixTierProgressionData
            progressionProfile;

        [Header("Tiers")]
        [Tooltip(
            "Tier 1 é o mais forte. " +
            "Tier 10 é o mais fraco.")]
        [SerializeField]
        private List<ItemAffixTierDefinition> tiers =
            new();

        public string AffixId =>
            affixId;

        public string DisplayName =>
            string.IsNullOrWhiteSpace(displayName)
                ? name
                : displayName;

        public ItemAffixType AffixType =>
            affixType;

        public ItemAffixEffectType EffectType =>
            effectType;

        public ItemAffixValueMode ValueMode =>
            valueMode;

        public CharacterStat CharacterStat =>
            characterStat;

        public ItemAffixTierProgressionData
            ProgressionProfile =>
                progressionProfile;

        public IReadOnlyList<ItemAffixTierDefinition> Tiers =>
            tiers;

        public bool GenerateTiersFromProgression()
        {
            if (progressionProfile == null)
            {
                return false;
            }

            IReadOnlyList<ItemAffixTierDefinition>
                generatedDefinitions =
                    progressionProfile
                        .GenerateTierDefinitions();

            if (generatedDefinitions == null ||
                generatedDefinitions.Count == 0)
            {
                return false;
            }

            tiers =
                new List<ItemAffixTierDefinition>(
                    generatedDefinitions.Count);

            for (int index = 0;
                 index <
                 generatedDefinitions.Count;
                 index++)
            {
                ItemAffixTierDefinition source =
                    generatedDefinitions[index];

                if (source == null)
                {
                    continue;
                }

                ItemAffixTierDefinition copy =
                    new ItemAffixTierDefinition(
                        source.Tier,
                        source.MinimumValue,
                        source.MaximumValue,
                        source.RollWeight);

                tiers.Add(
                    copy);
            }

            ValidateTiers();

            return tiers.Count > 0;
        }

        public bool TryGetTier(
            int requestedTier,
            out ItemAffixTierDefinition definition)
        {
            definition = null;

            if (tiers == null)
            {
                return false;
            }

            for (int index = 0;
                 index < tiers.Count;
                 index++)
            {
                ItemAffixTierDefinition candidate =
                    tiers[index];

                if (candidate == null ||
                    candidate.Tier != requestedTier)
                {
                    continue;
                }

                definition =
                    candidate;

                return true;
            }

            return false;
        }

        public bool TryRollValue(
            int requestedTier,
            out float rolledValue)
        {
            rolledValue = 0f;

            if (!TryGetTier(
                    requestedTier,
                    out ItemAffixTierDefinition definition))
            {
                return false;
            }

            rolledValue =
                definition.RollValue();

            rolledValue =
                ApplyProgressionRounding(
                    rolledValue);

            return true;
        }

        public bool TryRollRandomTier(
            out ItemAffixTierDefinition selectedTier)
        {
            selectedTier = null;

            if (tiers == null ||
                tiers.Count == 0)
            {
                return false;
            }

            float totalWeight = 0f;

            for (int index = 0;
                 index < tiers.Count;
                 index++)
            {
                ItemAffixTierDefinition tierDefinition =
                    tiers[index];

                if (tierDefinition == null)
                {
                    continue;
                }

                totalWeight +=
                    tierDefinition.RollWeight;
            }

            if (totalWeight <= 0f)
            {
                return false;
            }

            float roll =
                UnityEngine.Random.value *
                totalWeight;

            float accumulatedWeight = 0f;

            for (int index = 0;
                 index < tiers.Count;
                 index++)
            {
                ItemAffixTierDefinition tierDefinition =
                    tiers[index];

                if (tierDefinition == null ||
                    tierDefinition.RollWeight <= 0f)
                {
                    continue;
                }

                accumulatedWeight +=
                    tierDefinition.RollWeight;

                if (roll >
                    accumulatedWeight)
                {
                    continue;
                }

                selectedTier =
                    tierDefinition;

                return true;
            }

            return false;
        }

        public bool TryCreateRandomRoll(
            out ItemAffixRoll affixRoll)
        {
            affixRoll = null;

            if (!TryRollRandomTier(
                    out ItemAffixTierDefinition selectedTier))
            {
                return false;
            }

            float rolledValue =
                selectedTier.RollValue();

            rolledValue =
                ApplyProgressionRounding(
                    rolledValue);

            affixRoll =
                new ItemAffixRoll(
                    this,
                    selectedTier.Tier,
                    rolledValue);

            return true;
        }

        private float ApplyProgressionRounding(
            float value)
        {
            if (progressionProfile == null)
            {
                return value;
            }

            return progressionProfile
                .RoundGeneratedValue(
                    value);
        }

        private void OnValidate()
        {
            ValidateTiers();
        }

        private void ValidateTiers()
        {
            tiers ??=
                new List<ItemAffixTierDefinition>();

            HashSet<int> usedTiers =
                new();

            for (int index =
                     tiers.Count - 1;
                 index >= 0;
                 index--)
            {
                ItemAffixTierDefinition definition =
                    tiers[index];

                if (definition == null)
                {
                    tiers.RemoveAt(
                        index);

                    continue;
                }

                definition.Validate();

                if (!usedTiers.Add(
                        definition.Tier))
                {
                    Debug.LogWarning(
                        $"O afixo '{name}' possui mais de uma " +
                        $"definição para o Tier " +
                        $"{definition.Tier}.",
                        this);
                }
            }

            tiers.Sort(
                (left, right) =>
                    left.Tier.CompareTo(
                        right.Tier));
        }
    }
}