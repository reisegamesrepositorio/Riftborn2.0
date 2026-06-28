using System;
using UnityEngine;
namespace Riftborn.Characters.Resources
{
    public enum ResourceType { Mana, Energy, Rage, Focus, Custom }
    public sealed class ResourceController : MonoBehaviour
    {
        [SerializeField] private ResourceType resourceType = ResourceType.Mana;
        [SerializeField] private float maxValue = 100f, currentValue = 100f, regenerationPerSecond;
        public event Action<float, float> ResourceChanged;
        public event Action<float> ResourceConsumed;
        public event Action<float> ResourceRestored;
        public ResourceType ResourceType => resourceType;
        public float CurrentValue => currentValue;
        public float MaxValue => maxValue;
        private void Update() { if (regenerationPerSecond > 0f && currentValue < maxValue) Restore(regenerationPerSecond * Time.deltaTime); }
        public bool CanConsume(float amount) => currentValue >= Mathf.Max(0f, amount);
        public bool Consume(float amount) { amount = Mathf.Max(0f, amount); if (!CanConsume(amount)) return false; SetCurrent(currentValue - amount); ResourceConsumed?.Invoke(amount); return true; }
        public void Restore(float amount) { amount = Mathf.Max(0f, amount); if (amount <= 0f) return; SetCurrent(currentValue + amount); ResourceRestored?.Invoke(amount); }
        private void SetCurrent(float value) { float old = currentValue; currentValue = Mathf.Clamp(value, 0f, maxValue); if (!Mathf.Approximately(old, currentValue)) ResourceChanged?.Invoke(currentValue, maxValue); }
    }
}
