namespace Riftborn.Damage
{
    public sealed class DamageResult
    {
        /// <summary>
        /// Construtor completo utilizado pelo DamageCalculator.
        /// </summary>
        public DamageResult(
            DamageRequest request,
            float baseAmount,
            float scaledAmount,
            float preMitigationAmount,
            float targetDefense,
            float effectiveDefense,
            float defenseMultiplier,
            float mitigatedAmount,
            float amplifiedAmount,
            float finalAmount,
            bool wasCritical,
            DamageTag finalTags)
        {
            Request = request;
            BaseAmount = baseAmount;
            ScaledAmount = scaledAmount;
            PreMitigationAmount = preMitigationAmount;
            TargetDefense = targetDefense;
            EffectiveDefense = effectiveDefense;
            DefenseMultiplier = defenseMultiplier;
            MitigatedAmount = mitigatedAmount;
            AmplifiedAmount = amplifiedAmount;
            FinalAmount = finalAmount;
            WasCritical = wasCritical;
            FinalTags = finalTags;
        }

        /// <summary>
        /// Construtor simplificado mantido para compatibilidade
        /// com testes e sistemas que já recebam um dano calculado.
        /// </summary>
        public DamageResult(
            DamageRequest request,
            float finalAmount,
            bool wasCritical)
            : this(
                request: request,
                baseAmount: finalAmount,
                scaledAmount: finalAmount,
                preMitigationAmount: finalAmount,
                targetDefense: 0f,
                effectiveDefense: 0f,
                defenseMultiplier: 1f,
                mitigatedAmount: 0f,
                amplifiedAmount: 0f,
                finalAmount: finalAmount,
                wasCritical: wasCritical,
                finalTags: BuildCompatibilityTags(
                    request,
                    wasCritical))
        {
        }

        public DamageRequest Request { get; }

        public float BaseAmount { get; }

        public float ScaledAmount { get; }

        public float PreMitigationAmount { get; }

        public float TargetDefense { get; }

        public float EffectiveDefense { get; }

        public float DefenseMultiplier { get; }

        public float MitigatedAmount { get; }

        public float AmplifiedAmount { get; }

        public float FinalAmount { get; }

        public bool WasCritical { get; }

        public DamageTag FinalTags { get; }

        private static DamageTag BuildCompatibilityTags(
            DamageRequest request,
            bool wasCritical)
        {
            DamageTag tags =
                request?.Tags ?? DamageTag.None;

            if (wasCritical)
            {
                tags |= DamageTag.Critical;
            }

            return tags;
        }
    }
}