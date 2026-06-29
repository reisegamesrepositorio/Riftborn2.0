using System;
using UnityEngine;

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

        public float Delta => Current - OldCurrent;

        public float MaxDelta => Max - OldMax;
    }

    public sealed class ResourceController : MonoBehaviour
    {
        [Header("Resource")]
        [SerializeField]
        private ResourceType resourceType = ResourceType.Mana;

        [SerializeField]
        private InitialResourceState initialState =
            InitialResourceState.Full;

        [Header("Values")]
        [SerializeField, Min(1f)]
        private float maxValue = 100f;

        [SerializeField, Min(0f)]
        private float currentValue = 100f;

        [SerializeField, Min(0f)]
        private float regenerationPerSecond;

        /// <summary>
        /// Evento simples mantido para compatibilidade.
        /// Entrega o valor atual e o valor máximo.
        /// </summary>
        public event Action<float, float> ResourceChanged;

        /// <summary>
        /// Evento detalhado com valores antigos, novos e deltas.
        /// </summary>
        public event Action<ResourceChangedEventArgs>
            ResourceStateChanged;

        /// <summary>
        /// Informa quanto recurso foi realmente consumido.
        /// </summary>
        public event Action<float> ResourceConsumed;

        /// <summary>
        /// Informa quanto recurso foi realmente restaurado.
        /// </summary>
        public event Action<float> ResourceRestored;

        public ResourceType ResourceType => resourceType;

        public float CurrentValue => currentValue;

        public float MaxValue => maxValue;

        public float RegenerationPerSecond =>
            regenerationPerSecond;

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
                regenerationPerSecond * Time.deltaTime);
        }

        private void OnValidate()
        {
            maxValue = Mathf.Max(1f, maxValue);
            currentValue = Mathf.Clamp(
                currentValue,
                0f,
                maxValue);

            regenerationPerSecond =
                Mathf.Max(0f, regenerationPerSecond);
        }

        public bool CanConsume(float amount)
        {
            float requestedAmount =
                Mathf.Max(0f, amount);

            return currentValue >= requestedAmount;
        }

        public bool Consume(float amount)
        {
            float requestedAmount =
                Mathf.Max(0f, amount);

            if (requestedAmount <= 0f)
            {
                return true;
            }

            if (!CanConsume(requestedAmount))
            {
                return false;
            }

            float oldCurrentValue = currentValue;

            SetResourceState(
                currentValue - requestedAmount,
                maxValue);

            float effectiveConsumption =
                oldCurrentValue - currentValue;

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
                Mathf.Max(0f, amount);

            if (requestedAmount <= 0f)
            {
                return 0f;
            }

            float oldCurrentValue = currentValue;

            SetResourceState(
                currentValue + requestedAmount,
                maxValue);

            float effectiveRestoration =
                currentValue - oldCurrentValue;

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
            float newMaxValue =
                Mathf.Max(1f, value);

            float newCurrentValue =
                fillToMax
                    ? newMaxValue
                    : Mathf.Min(
                        currentValue,
                        newMaxValue);

            SetResourceState(
                newCurrentValue,
                newMaxValue);
        }

        public void SetRegenerationPerSecond(float value)
        {
            regenerationPerSecond =
                Mathf.Max(0f, value);
        }

        public void SetResourceType(
            ResourceType newResourceType)
        {
            resourceType = newResourceType;
        }

        public void FillToMaximum()
        {
            Restore(maxValue - currentValue);
        }

        public void Empty()
        {
            if (currentValue <= 0f)
            {
                return;
            }

            float oldCurrentValue = currentValue;

            SetResourceState(
                0f,
                maxValue);

            float effectiveConsumption =
                oldCurrentValue - currentValue;

            if (effectiveConsumption > 0f)
            {
                ResourceConsumed?.Invoke(
                    effectiveConsumption);
            }
        }

        private void InitializeResource()
        {
            maxValue = Mathf.Max(1f, maxValue);

            currentValue = initialState switch
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

                _ => Mathf.Clamp(
                    currentValue,
                    0f,
                    maxValue)
            };
        }

        private void SetResourceState(
            float newCurrentValue,
            float newMaxValue)
        {
            float oldCurrentValue = currentValue;
            float oldMaxValue = maxValue;

            maxValue = Mathf.Max(
                1f,
                newMaxValue);

            currentValue = Mathf.Clamp(
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

            if (!currentChanged && !maxChanged)
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