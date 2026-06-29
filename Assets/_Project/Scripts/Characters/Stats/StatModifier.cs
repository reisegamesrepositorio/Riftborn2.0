using System;

namespace Riftborn.Characters.Stats
{
    /// <summary>
    /// Representa uma modificação temporária ou permanente aplicada
    /// a um atributo primário durante a execução do jogo.
    /// </summary>
    public sealed class StatModifier
    {
        public StatModifier(
            string id,
            object source,
            CharacterStat stat,
            float flatValue = 0f,
            float additivePercent = 0f,
            float multiplicativePercent = 0f)
        {
            Id = string.IsNullOrWhiteSpace(id)
                ? Guid.NewGuid().ToString("N")
                : id;

            Source = source;
            Stat = stat;
            FlatValue = flatValue;
            AdditivePercent = additivePercent;
            MultiplicativePercent = multiplicativePercent;
        }

        /// <summary>
        /// Identificador único desta instância de modificador.
        /// </summary>
        public string Id { get; }

        /// <summary>
        /// Objeto responsável por criar o modificador.
        /// Pode ser um item, runa, buff, debuff ou habilidade.
        /// </summary>
        public object Source { get; }

        public CharacterStat Stat { get; }

        /// <summary>
        /// Valor somado diretamente ao atributo.
        /// Exemplo: 5 representa +5 STR.
        /// </summary>
        public float FlatValue { get; }

        /// <summary>
        /// Percentual somado aos outros percentuais aditivos.
        /// Exemplo: 0.10 representa +10%.
        /// </summary>
        public float AdditivePercent { get; }

        /// <summary>
        /// Percentual aplicado como multiplicador independente.
        /// Exemplo: 0.20 representa multiplicar por 1.20.
        /// </summary>
        public float MultiplicativePercent { get; }
    }
}