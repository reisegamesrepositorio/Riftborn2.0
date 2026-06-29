using Riftborn.Characters.Core;
using UnityEngine;

namespace Riftborn.Characters.Abilities
{
    public abstract class AbilityBase : ScriptableObject
    {
        [Header("Identity")]
        [SerializeField]
        private string abilityId;

        [Header("Usage")]
        [SerializeField, Min(0f)]
        private float cooldown = 1f;

        [SerializeField, Min(0f)]
        private float resourceCost;

        public string AbilityId =>
            abilityId;

        public float Cooldown =>
            cooldown;

        public float ResourceCost =>
            resourceCost;

        /// <summary>
        /// Faz todas as validações específicas da habilidade
        /// antes de consumir recurso ou iniciar cooldown.
        ///
        /// Habilidades concretas podem sobrescrever este método
        /// para validar alcance, alvo, estado e outras condições.
        /// </summary>
        public virtual bool CanExecute(
            CharacterContext caster,
            CharacterContext target)
        {
            if (caster == null)
            {
                return false;
            }

            if (caster.Health != null &&
                caster.Health.IsDead)
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// Executa a habilidade.
        ///
        /// Deve retornar true somente quando a habilidade
        /// tiver sido executada com sucesso.
        /// </summary>
        public abstract bool Execute(
            CharacterContext caster,
            CharacterContext target);

        protected virtual void OnValidate()
        {
            cooldown =
                Mathf.Max(0f, cooldown);

            resourceCost =
                Mathf.Max(0f, resourceCost);
        }
    }
}