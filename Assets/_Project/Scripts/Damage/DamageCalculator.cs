using UnityEngine;
namespace Riftborn.Damage
{
    public static class DamageCalculator
    {
        public static DamageResult Calculate(DamageRequest request)
        {
            if (request == null) return new DamageResult(null, 0f, false);
            float amount = Mathf.Max(0f, request.BaseValue * request.Scaling);
            bool critical = request.CanCrit && Random.value <= Mathf.Clamp01(request.CriticalChance);
            if (critical) { amount *= Mathf.Max(1f, request.CriticalMultiplier); request.Tags |= DamageTag.Critical; }
            return new DamageResult(request, amount, critical);
        }
    }
}
