using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

namespace Riftborn.Characters.Resources
{
    public enum ResourceType
    {
        Mana = 0,
        Energy = 1,
        Rage = 2,
        Focus = 3,
        Custom = 4
    }

    public enum InitialResourceState
    {
        Full = 0,
        Empty = 1,
        Custom = 2
    }

    public sealed class ResourceChangedEventArgs : EventArgs
    {
        public ResourceChangedEventArgs(
            float oldCurrent,
            float current,
            float oldMax,
            float max)
        {
            OldCurrent = oldCurrent;
            Current = current;
            OldMax = oldMax;
            Max = max;
        }

        public float OldCurrent { get; }

        public float Current { get; }

        public float OldMax { get; }

        public float Max { get; }

        public float Delta =>
            Current - OldCurrent;

        public float MaxDelta =>
            Max - OldMax;
    }

    public sealed class ResourceController : MonoBehaviour
    {
        [Header("Resource")]
        [SerializeField]
        private ResourceType resourceType =
            ResourceType.Mana;

        [SerializeField]
        private InitialResourceState initialState =
            InitialResourceState.Full;

        [Header("Base Values")]
        [FormerlySerializedAs("maxValue")]
        [SerializeField, Min(1f)]
        private float baseMaxValue = 100f;

        [SerializeField, Min(0f)]
        private float currentValue = 100f;

        [FormerlySerializedAs("regenerationPerSecond")]
        [SerializeField, Min(0f)]
        private float baseRegenerationPerSecond;

        private readonly Dictionary<
            ResourceModifierType,
            List<ResourceModifier>>
            modifiersByType = new();

        private readonly Dictionary<string, ResourceModifier>
            modifiersById =
                new(StringComparer.Ordinal);

        private float maxValue;
        private float regenerationPerSecond;
        private bool modifiersInitialized;

        public event Action<float, float>
            ResourceChanged;

        public event Action<ResourceChangedEventArgs>
            ResourceStateChanged;

        public event Action<float>
            ResourceConsumed;

        public event Action<float>
            ResourceRestored;

        public event Action<float, float>
            RegenerationChanged;

        public event Action ResourceValuesChanged;

        public ResourceType ResourceType =>
            resourceType;

        public float CurrentValue =>
            currentValue;

        public float MaxValue =>
            maxValue;

        public float BaseMaxValue =>
            baseMaxValue;

        public float RegenerationPerSecond =>
            regenerationPerSecond;

        public float BaseRegenerationPerSecond =>
            baseRegenerationPerSecond;

        public float ResourcePercentage =>
            maxValue > 0f
                ? currentValue / maxValue
                : 0f;

        public bool IsEmpty =>
            currentValue <= 0f;

        public bool IsFull =>
            currentValue >= maxValue;

        private void Awake()
        {
            EnsureModifiersInitialized();
            InitializeResource();
        }

        private void Update()
        {
            if (regenerationPerSecond <= 0f ||
                currentValue >= maxValue)
            {
                return;
            }

            Restore(
                regenerationPerSecond *
                Time.deltaTime);
        }

        private void OnValidate()
        {
            baseMaxValue =
                Mathf.Max(
                    1f,
                    baseMaxValue);

            baseRegenerationPerSecond =
                Mathf.Max(
                    0f,
                    baseRegenerationPerSecond);

            currentValue =
                Mathf.Clamp(
                    currentValue,
                    0f,
                    baseMaxValue);
        }

        public bool CanConsume(float amount)
        {
            float requestedAmount =
                Mathf.Max(
                    0f,
                    amount);

            return currentValue >=
                   requestedAmount;
        }

        public bool Consume(float amount)
        {
            float requestedAmount =
                Mathf.Max(
                    0f,
                    amount);

            if (requestedAmount <= 0f)
            {
                return true;
            }

            if (!CanConsume(requestedAmount))
            {
                return false;
            }

            float oldCurrentValue =
                currentValue;

            SetResourceState(
                currentValue -
                requestedAmount,
                maxValue);

            float effectiveConsumption =
                oldCurrentValue -
                currentValue;

            if (effectiveConsumption > 0f)
            {
                ResourceConsumed?.Invoke(
                    effectiveConsumption);
            }

            return true;
        }

        public float Restore(float amount)
        {
            float requestedAmount =
                Mathf.Max(
                    0f,
                    amount);

            if (requestedAmount <= 0f)
            {
                return 0f;
            }

            float oldCurrentValue =
                currentValue;

            SetResourceState(
                currentValue +
                requestedAmount,
                maxValue);

            float effectiveRestoration =
                currentValue -
                oldCurrentValue;

            if (effectiveRestoration > 0f)
            {
                ResourceRestored?.Invoke(
                    effectiveRestoration);
            }

            return effectiveRestoration;
        }

        public void SetMaxValue(
            float value,
            bool fillToMax = false)
        {
            baseMaxValue =
                Mathf.Max(
                    1f,
                    value);

            RecalculateResourceValues(
                fillToMax);
        }

        public void SetRegenerationPerSecond(
            float value)
        {
            baseRegenerationPerSecond =
                Mathf.Max(
                    0f,
                    value);

            RecalculateResourceValues(
                fillToMax: false);
        }

        public void SetResourceType(
            ResourceType newResourceType)
        {
            resourceType =
                newResourceType;
        }

        [ContextMenu("Fill Resource To Maximum")]
        public void FillToMaximum()
        {
            Restore(
                maxValue -
                currentValue);
        }

        public void Empty()
        {
            if (currentValue <= 0f)
            {
                return;
            }

            float oldCurrentValue =
                currentValue;

            SetResourceState(
                0f,
                maxValue);

            float effectiveConsumption =
                oldCurrentValue -
                currentValue;

            if (effectiveConsumption > 0f)
            {
                ResourceConsumed?.Invoke(
                    effectiveConsumption);
            }
        }

        public bool AddModifier(
            ResourceModifier modifier)
        {
            if (modifier == null)
            {
                return false;
            }

            EnsureModifiersInitialized();

            if (modifiersById.ContainsKey(
                    modifier.Id))
            {
                Debug.LogWarning(
                    $"[RESOURCE] Já existe um modificador " +
                    $"com o ID '{modifier.Id}'.",
                    this);

                return false;
            }

            modifiersByType[
                    modifier.ModifierType]
                .Add(modifier);

            modifiersById.Add(
                modifier.Id,
                modifier);

            RecalculateResourceValues(
                fillToMax: false);

            return true;
        }

        public bool RemoveModifier(
            string modifierId)
        {
            if (string.IsNullOrWhiteSpace(
                    modifierId))
            {
                return false;
            }

            EnsureModifiersInitialized();

            if (!modifiersById.TryGetValue(
                    modifierId,
                    out ResourceModifier modifier))
            {
                return false;
            }

            modifiersById.Remove(
                modifierId);

            bool removed =
                modifiersByType[
                        modifier.ModifierType]
                    .Remove(modifier);

            if (removed)
            {
                RecalculateResourceValues(
                    fillToMax: false);
            }

            return removed;
        }

        public int RemoveModifiersFromSource(
            object source)
        {
            if (source == null)
            {
                return 0;
            }

            EnsureModifiersInitialized();

            int totalRemoved = 0;

            foreach (
                KeyValuePair<
                    ResourceModifierType,
                    List<ResourceModifier>> pair
                in modifiersByType)
            {
                List<ResourceModifier> modifiers =
                    pair.Value;

                for (int index =
                         modifiers.Count - 1;
                     index >= 0;
                     index--)
                {
                    ResourceModifier modifier =
                        modifiers[index];

                    if (!Equals(
                            modifier.Source,
                            source))
                    {
                        continue;
                    }

                    modifiers.RemoveAt(index);

                    modifiersById.Remove(
                        modifier.Id);

                    totalRemoved++;
                }
            }

            if (totalRemoved > 0)
            {
                RecalculateResourceValues(
                    fillToMax: false);
            }

            return totalRemoved;
        }

        public bool HasModifier(
            string modifierId)
        {
            if (string.IsNullOrWhiteSpace(
                    modifierId))
            {
                return false;
            }

            EnsureModifiersInitialized();

            return modifiersById.ContainsKey(
                modifierId);
        }

        [ContextMenu("Log Current Resource Values")]
        public void LogCurrentResourceValues()
        {
            GetModifierTotals(
                ResourceModifierType.MaximumResource,
                out float maximumFlat,
                out float maximumAdditive,
                out float maximumMultiplicative);

            GetModifierTotals(
                ResourceModifierType.Regeneration,
                out float regenerationFlat,
                out float regenerationAdditive,
                out float regenerationMultiplicative);

            Debug.Log(
                $"[RESOURCE VALUES] " +
                $"Tipo: {resourceType} | " +
                $"Recurso: {currentValue:0.##}/" +
                $"{maxValue:0.##} | " +
                $"Máximo-base: {baseMaxValue:0.##} | " +
                $"Bônus máximo fixo: {maximumFlat:0.##} | " +
                $"Bônus máximo aditivo: " +
                $"{maximumAdditive * 100f:0.##}% | " +
                $"Multiplicador máximo: " +
                $"{maximumMultiplicative:0.##}x | " +
                $"Regeneração: " +
                $"{regenerationPerSecond:0.##}/s | " +
                $"Regeneração-base: " +
                $"{baseRegenerationPerSecond:0.##}/s | " +
                $"Bônus regeneração fixo: " +
                $"{regenerationFlat:0.##}/s | " +
                $"Bônus regeneração aditivo: " +
                $"{regenerationAdditive * 100f:0.##}% | " +
                $"Multiplicador regeneração: " +
                $"{regenerationMultiplicative:0.##}x",
                this);
        }

        private void InitializeResource()
        {
            maxValue =
                CalculateMaximumResource();

            regenerationPerSecond =
                CalculateRegeneration();

            currentValue =
                initialState switch
                {
                    InitialResourceState.Full =>
                        maxValue,

                    InitialResourceState.Empty =>
                        0f,

                    InitialResourceState.Custom =>
                        Mathf.Clamp(
                            currentValue,
                            0f,
                            maxValue),

                    _ =>
                        Mathf.Clamp(
                            currentValue,
                            0f,
                            maxValue)
                };
        }

        private void RecalculateResourceValues(
            bool fillToMax)
        {
            float oldRegeneration =
                regenerationPerSecond;

            float newMaximum =
                CalculateMaximumResource();

            float newRegeneration =
                CalculateRegeneration();

            /*
             * Preserva a quantidade atual para impedir
             * recuperação gratuita ao trocar equipamentos.
             */
            float newCurrent =
                fillToMax
                    ? newMaximum
                    : Mathf.Min(
                        currentValue,
                        newMaximum);

            SetResourceState(
                newCurrent,
                newMaximum);

            regenerationPerSecond =
                newRegeneration;

            if (!Mathf.Approximately(
                    oldRegeneration,
                    regenerationPerSecond))
            {
                RegenerationChanged?.Invoke(
                    oldRegeneration,
                    regenerationPerSecond);
            }

            ResourceValuesChanged?.Invoke();
        }

        private float CalculateMaximumResource()
        {
            float finalValue =
                ApplyModifiers(
                    ResourceModifierType.MaximumResource,
                    baseMaxValue);

            return Mathf.Max(
                1f,
                finalValue);
        }

        private float CalculateRegeneration()
        {
            float finalValue =
                ApplyModifiers(
                    ResourceModifierType.Regeneration,
                    baseRegenerationPerSecond);

            return Mathf.Max(
                0f,
                finalValue);
        }

        private float ApplyModifiers(
            ResourceModifierType modifierType,
            float baseValue)
        {
            GetModifierTotals(
                modifierType,
                out float flatValue,
                out float additivePercent,
                out float multiplicativeFactor);

            float finalValue =
                baseValue +
                flatValue;

            finalValue *=
                1f +
                additivePercent;

            finalValue *=
                multiplicativeFactor;

            return finalValue;
        }

        private void GetModifierTotals(
            ResourceModifierType modifierType,
            out float flatValue,
            out float additivePercent,
            out float multiplicativeFactor)
        {
            EnsureModifiersInitialized();

            flatValue = 0f;
            additivePercent = 0f;
            multiplicativeFactor = 1f;

            List<ResourceModifier> modifiers =
                modifiersByType[
                    modifierType];

            for (int index = 0;
                 index < modifiers.Count;
                 index++)
            {
                ResourceModifier modifier =
                    modifiers[index];

                flatValue +=
                    modifier.FlatValue;

                additivePercent +=
                    modifier.AdditivePercent;

                multiplicativeFactor *=
                    1f +
                    modifier.MultiplicativePercent;
            }
        }

        private void EnsureModifiersInitialized()
        {
            if (modifiersInitialized)
            {
                return;
            }

            foreach (
                ResourceModifierType modifierType
                in Enum.GetValues(
                    typeof(ResourceModifierType)))
            {
                modifiersByType[modifierType] =
                    new List<ResourceModifier>();
            }

            modifiersInitialized = true;
        }

        private void SetResourceState(
            float newCurrentValue,
            float newMaxValue)
        {
            float oldCurrentValue =
                currentValue;

            float oldMaxValue =
                maxValue;

            maxValue =
                Mathf.Max(
                    1f,
                    newMaxValue);

            currentValue =
                Mathf.Clamp(
                    newCurrentValue,
                    0f,
                    maxValue);

            bool currentChanged =
                !Mathf.Approximately(
                    oldCurrentValue,
                    currentValue);

            bool maxChanged =
                !Mathf.Approximately(
                    oldMaxValue,
                    maxValue);

            if (!currentChanged &&
                !maxChanged)
            {
                return;
            }

            ResourceChanged?.Invoke(
                currentValue,
                maxValue);

            ResourceStateChanged?.Invoke(
                new ResourceChangedEventArgs(
                    oldCurrentValue,
                    currentValue,
                    oldMaxValue,
                    maxValue));
        }
    }
}