using System;
using Riftborn.Damage;
using UnityEngine;
namespace Riftborn.Characters.Health
{
    public sealed class HealthChangedEventArgs : EventArgs { public HealthChangedEventArgs(float current, float max, float delta) { Current = current; Max = max; Delta = delta; } public float Current { get; } public float Max { get; } public float Delta { get; } }
    public sealed class HealthController : MonoBehaviour
    {
        [SerializeField] private float maxHealth = 100f;
        [SerializeField] private float currentHealth = 100f;
        [SerializeField] private bool startFullHealth = true;
        public event Action<HealthChangedEventArgs> HealthChanged;
        public event Action<DamageResult> DamageTaken;
        public event Action<float> Healed;
        public event Action Died;
        public float CurrentHealth => currentHealth;
        public float MaxHealth => maxHealth;
        public bool IsDead { get; private set; }
        private void Awake() { if (startFullHealth) currentHealth = maxHealth; }
        public void SetMaxHealth(float value, bool fillToMax = false) { maxHealth = Mathf.Max(1f, value); SetCurrentHealth(fillToMax ? maxHealth : Mathf.Min(currentHealth, maxHealth)); }
        public void ApplyDamage(DamageResult result) { if (result == null || IsDead) return; float damage = Mathf.Max(0f, result.FinalAmount); if (damage <= 0f) return; SetCurrentHealth(currentHealth - damage); DamageTaken?.Invoke(result); if (currentHealth <= 0f && !IsDead) { IsDead = true; Died?.Invoke(); } }
        public void Heal(float amount) { if (IsDead) return; float heal = Mathf.Max(0f, amount); if (heal <= 0f) return; SetCurrentHealth(currentHealth + heal); Healed?.Invoke(heal); }
        public void Revive(float healthAmount) { IsDead = false; SetCurrentHealth(Mathf.Clamp(healthAmount, 1f, maxHealth)); }
        private void SetCurrentHealth(float value) { float old = currentHealth; currentHealth = Mathf.Clamp(value, 0f, maxHealth); if (!Mathf.Approximately(old, currentHealth)) HealthChanged?.Invoke(new HealthChangedEventArgs(currentHealth, maxHealth, currentHealth - old)); }
    }
}
