namespace Riftborn.Damage
{
    public sealed class DamageApplicationResult
    {
        public DamageApplicationResult(
            DamageResult damageResult,
            float incomingDamage,
            float absorbedDamage,
            float damageAfterShields,
            float healthDamage,
            float oldHealth,
            float newHealth)
        {
            DamageResult = damageResult;
            IncomingDamage = incomingDamage;
            AbsorbedDamage = absorbedDamage;
            DamageAfterShields = damageAfterShields;
            HealthDamage = healthDamage;
            OldHealth = oldHealth;
            NewHealth = newHealth;
        }

        public DamageResult DamageResult { get; }

        /// <summary>
        /// Dano final produzido pelo DamageCalculator,
        /// antes dos escudos.
        /// </summary>
        public float IncomingDamage { get; }

        /// <summary>
        /// Quantidade absorvida pelos escudos.
        /// </summary>
        public float AbsorbedDamage { get; }

        /// <summary>
        /// Dano restante depois dos escudos,
        /// antes do limite da vida atual.
        /// </summary>
        public float DamageAfterShields { get; }

        /// <summary>
        /// Perda real de vida.
        /// </summary>
        public float HealthDamage { get; }

        public float OldHealth { get; }

        public float NewHealth { get; }

        public float OverkillDamage =>
            UnityEngine.Mathf.Max(
                0f,
                DamageAfterShields - OldHealth);

        public bool WasFullyAbsorbed =>
            IncomingDamage > 0f &&
            DamageAfterShields <= 0f;

        public bool DamagedHealth =>
            HealthDamage > 0f;

        public bool KilledTarget =>
            OldHealth > 0f &&
            NewHealth <= 0f &&
            HealthDamage > 0f;
    }
}