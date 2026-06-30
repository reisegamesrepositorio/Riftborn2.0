using Riftborn.Characters.Abilities;
using Riftborn.Characters.ActionStates;
using Riftborn.Characters.Animation;
using Riftborn.Characters.Combat;
using Riftborn.Characters.Defense;
using Riftborn.Characters.Equipment;
using Riftborn.Characters.Health;
using Riftborn.Characters.Input;
using Riftborn.Characters.Inventory;
using Riftborn.Characters.Movement;
using Riftborn.Characters.Progression;
using Riftborn.Characters.Resources;
using Riftborn.Characters.Runes;
using Riftborn.Characters.Stats;
using Riftborn.Characters.StatusEffects;
using Riftborn.Characters.Targeting;
using UnityEngine;

namespace Riftborn.Characters.Core
{
    /// <summary>
    /// Base comum dos controllers centrais.
    ///
    /// PlayerController e EnemyController são o próprio CharacterContext;
    /// portanto não existe mais um componente CharacterContext separado no
    /// GameObject. Os sistemas externos continuam trabalhando com
    /// CharacterContext sem precisar conhecer o tipo concreto da entidade.
    /// </summary>
    public abstract class CharacterContext : MonoBehaviour
    {
        [Header("Core Character Modules")]
        [SerializeField]
        protected CharacterStatsController stats = new();

        [SerializeField]
        protected HealthController health = new();

        [SerializeField]
        protected DefenseController defense = new();

        [SerializeField]
        protected ActionStateController actionState = new();

        [SerializeField]
        protected CharacterEvents characterEvents = new();

        [Header("Attached Character Systems")]
        [SerializeField]
        protected ResourceController resources = new();

        [SerializeField]
        protected MovementController movement = new();

        [SerializeField]
        protected CombatController combat = new();

        [SerializeField]
        protected TargetingController targeting = new();

        [SerializeField]
        protected AbilityController abilities = new();

        [SerializeField]
        protected StatusEffectController statusEffects = new();

        [SerializeField]
        protected InventoryController inventory = new();

        [SerializeField]
        protected EquipmentController equipment = new();

        [SerializeField]
        protected RuneController runes = new();

        [SerializeField]
        protected CharacterProgressionController progression = new();

        [SerializeField]
        protected CharacterAnimationController animationController;

        private bool coreInitialized;

        public CharacterStatsController Stats =>
            stats;

        public HealthController Health =>
            health;

        public DefenseController Defense =>
            defense;

        public ResourceController Resources =>
            resources;

        public MovementController Movement =>
            movement;

        public ActionStateController ActionState =>
            actionState;

        public CombatController Combat =>
            combat;

        public TargetingController Targeting =>
            targeting;

        public AbilityController Abilities =>
            abilities;

        public StatusEffectController StatusEffects =>
            statusEffects;

        public InventoryController Inventory =>
            inventory;

        public EquipmentController Equipment =>
            equipment;

        public RuneController Runes =>
            runes;

        public CharacterProgressionController Progression =>
            progression;

        public CharacterAnimationController AnimationController =>
            animationController;

        public CharacterEvents Events =>
            characterEvents;

        /// <summary>
        /// Deve ser chamado pelo controller concreto no Awake.
        /// </summary>
        protected void InitializeCharacterContext()
        {
            EnsureCoreModules();
            CacheAttachedSystems();

            stats.Initialize(this);
            defense.Initialize(this);
            actionState.Initialize();
            characterEvents.Initialize();

            health.Initialize(
                this,
                stats,
                statusEffects);

            resources.Initialize();
            targeting.Initialize(this);
            movement.Initialize(
                this,
                GetComponent<CharacterController>(),
                Camera.main != null ? Camera.main.transform : null);
            combat.Initialize(
                this,
                targeting,
                equipment);
            abilities.Initialize(this);
            statusEffects.Initialize(this);
            inventory.Initialize();
            equipment.Initialize(
                this,
                combat,
                resources,
                abilities,
                movement);
            progression.Initialize(this);

            coreInitialized = true;
        }

        /// <summary>
        /// Deve ser chamado pelo controller concreto no OnEnable.
        /// </summary>
        protected void EnableCharacterContext()
        {
            if (!coreInitialized)
            {
                InitializeCharacterContext();
            }

            CacheAttachedSystems();

            health.SetStatusEffects(
                statusEffects);

            health.Enable();
        }

        /// <summary>
        /// Deve ser chamado pelo controller concreto no OnDisable.
        /// </summary>
        protected void DisableCharacterContext()
        {
            health?.Disable();
        }

        /// <summary>
        /// Atualiza referências dos sistemas que ainda são componentes.
        /// Conforme a migração avançar, esses campos também serão convertidos
        /// em módulos internos e deixarão de usar GetComponent.
        /// </summary>
        protected void CacheAttachedSystems()
        {
            resources ??= new ResourceController();
            movement ??= new MovementController();
            combat ??= new CombatController();
            targeting ??= new TargetingController();
            abilities ??= new AbilityController();
            statusEffects ??= new StatusEffectController();
            inventory ??= new InventoryController();
            equipment ??= new EquipmentController();
            runes ??= new RuneController();
            progression ??= new CharacterProgressionController();
        }

        protected void ValidateCharacterContext()
        {
            EnsureCoreModules();

            stats.Validate();
            health.Validate();
            defense.Validate();
        }

        private void EnsureCoreModules()
        {
            stats ??=
                new CharacterStatsController();

            health ??=
                new HealthController();

            defense ??=
                new DefenseController();

            actionState ??=
                new ActionStateController();

            characterEvents ??=
                new CharacterEvents();
        }
    }
}
