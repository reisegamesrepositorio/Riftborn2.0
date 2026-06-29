using Riftborn.Characters.Defense;
using UnityEngine;

namespace Riftborn.Damage
{
    public static class DamageCalculator
    {
        public static DamageResult Calculate(
            DamageRequest request,
            float? criticalRoll = null)
        {
            if (request == null)
            {
                return CreateEmptyResult();
            }

            float baseAmount =
                Mathf.Max(0f, request.BaseValue);

            float scaling =
                Mathf.Max(0f, request.Scaling);

            float scaledAmount =
                baseAmount * scaling;

            float criticalChance =
                Mathf.Clamp01(request.CriticalChance);

            float criticalMultiplier =
                Mathf.Max(1f, request.CriticalMultiplier);

            float roll = criticalRoll.HasValue
                ? Mathf.Clamp01(criticalRoll.Value)
                : Random.value;

            bool wasCritical =
                request.CanCrit &&
                criticalChance > 0f &&
                (criticalChance >= 1f ||
                 roll < criticalChance);

            float preMitigationAmount =
                wasCritical
                    ? scaledAmount * criticalMultiplier
                    : scaledAmount;

            DamageTag finalTags = request.Tags;

            if (wasCritical)
            {
                finalTags |= DamageTag.Critical;
            }

            float targetDefense =
                GetTargetDefense(request);

            float effectiveDefense =
                CalculateEffectiveDefense(
                    request,
                    targetDefense);

            float defenseMultiplier =
                request.Type == DamageType.True
                    ? 1f
                    : CalculateDefenseMultiplier(
                        effectiveDefense);

            float finalAmount =
                Mathf.Max(
                    0f,
                    preMitigationAmount *
                    defenseMultiplier);

            float mitigatedAmount =
                Mathf.Max(
                    0f,
                    preMitigationAmount -
                    finalAmount);

            float amplifiedAmount =
                Mathf.Max(
                    0f,
                    finalAmount -
                    preMitigationAmount);

            return new DamageResult(
                request,
                baseAmount,
                scaledAmount,
                preMitigationAmount,
                targetDefense,
                effectiveDefense,
                defenseMultiplier,
                mitigatedAmount,
                amplifiedAmount,
                finalAmount,
                wasCritical,
                finalTags);
        }

        public static float CalculateDefenseMultiplier(
            float defense)
        {
            if (defense >= 0f)
            {
                return 100f / (100f + defense);
            }

            return 2f -
                   (100f / (100f - defense));
        }

        private static float GetTargetDefense(
            DamageRequest request)
        {
            if (request.Type == DamageType.True ||
                request.Target?.Defense == null)
            {
                return 0f;
            }

            return request.Type switch
            {
                DamageType.Physical =>
                    request.Target.Defense.GetFinalValue(
                        DefenseType.Physical),

                DamageType.Magical =>
                    request.Target.Defense.GetFinalValue(
                        DefenseType.Magical),

                _ => 0f
            };
        }

        private static float CalculateEffectiveDefense(
            DamageRequest request,
            float targetDefense)
        {
            if (request.Type == DamageType.True)
            {
                return 0f;
            }

            // Penetração só atua sobre defesa positiva.
            // Defesa negativa já representa vulnerabilidade.
            if (targetDefense <= 0f)
            {
                return targetDefense;
            }

            float percentPenetration =
                Mathf.Clamp01(
                    request.PercentPenetration);

            float flatPenetration =
                Mathf.Max(
                    0f,
                    request.FlatPenetration);

            float effectiveDefense =
                targetDefense *
                (1f - percentPenetration);

            effectiveDefense -= flatPenetration;

            return effectiveDefense;
        }

        private static DamageResult CreateEmptyResult()
        {
            return new DamageResult(
                request: null,
                baseAmount: 0f,
                scaledAmount: 0f,
                preMitigationAmount: 0f,
                targetDefense: 0f,
                effectiveDefense: 0f,
                defenseMultiplier: 1f,
                mitigatedAmount: 0f,
                amplifiedAmount: 0f,
                finalAmount: 0f,
                wasCritical: false,
                finalTags: DamageTag.None);
        }
    }
}