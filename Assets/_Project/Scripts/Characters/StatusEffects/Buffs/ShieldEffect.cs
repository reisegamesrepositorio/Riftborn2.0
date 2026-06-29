using Riftborn.Characters.Core;
using Riftborn.Damage;
using UnityEngine;

namespace Riftborn.Characters.StatusEffects
{
    public sealed class ShieldEffect :
        StatusEffectBase,
        IDamageAbsorber
    {
        private float remainingAmount;

        public ShieldEffect(
            CharacterContext source,
            CharacterContext target,
            float duration = 5f,
            float amount = 25f)
            : base(
                id: "shield",
                source: source,
                target: target,
                duration: duration,
                maxStacks: 1,
                tags:
                    StatusEffectTag.Buff |
                    StatusEffectTag.Shield)
        {
            InitialAmount =
                Mathf.Max(0f, amount);

            remainingAmount =
                InitialAmount;
        }

        public float InitialAmount { get; }

        // Mantido para compatibilidade com o código anterior.
        public float Amount =>
            InitialAmount;

        public float RemainingAmount =>
            remainingAmount;

        public bool IsDepleted =>
            remainingAmount <= 0f;

        public float AbsorbDamage(
            DamageResult result,
            float incomingDamage)
        {
            float safeIncomingDamage =
                Mathf.Max(0f, incomingDamage);

            if (safeIncomingDamage <= 0f ||
                remainingAmount <= 0f)
            {
                return safeIncomingDamage;
            }

            float absorbedAmount =
                Mathf.Min(
                    remainingAmount,
                    safeIncomingDamage);

            remainingAmount -= absorbedAmount;

            float remainingDamage =
                safeIncomingDamage - absorbedAmount;

            if (remainingAmount <= 0f)
            {
                remainingAmount = 0f;
                Expire();
            }

            return remainingDamage;
        }

        public override void OnReapply(
            StatusEffectController controller,
            StatusEffectBase incoming)
        {
            if (incoming is not ShieldEffect incomingShield)
            {
                return;
            }

            RefreshDuration(
                incomingShield.Duration);

            /*
             * Reaplicar o escudo pela mesma fonte mantém
             * o maior valor entre o escudo restante e o novo.
             */
            remainingAmount =
                Mathf.Max(
                    remainingAmount,
                    incomingShield.InitialAmount);
        }
    }
}