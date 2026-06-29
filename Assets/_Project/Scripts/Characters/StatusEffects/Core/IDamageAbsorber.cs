using Riftborn.Damage;

namespace Riftborn.Characters.StatusEffects
{
    public interface IDamageAbsorber
    {
        /// <summary>
        /// Recebe o dano que ainda precisa ser processado
        /// e retorna o dano restante após a absorção.
        /// </summary>
        float AbsorbDamage(
            DamageResult result,
            float incomingDamage);
    }
}