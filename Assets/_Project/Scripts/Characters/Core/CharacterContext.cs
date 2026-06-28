using Riftborn.Characters.Abilities;
using Riftborn.Characters.ActionStates;
using Riftborn.Characters.Animation;
using Riftborn.Characters.Combat;
using Riftborn.Characters.Equipment;
using Riftborn.Characters.Health;
using Riftborn.Characters.Inventory;
using Riftborn.Characters.Movement;
using Riftborn.Characters.Resources;
using Riftborn.Characters.Runes;
using Riftborn.Characters.Stats;
using Riftborn.Characters.StatusEffects;
using Riftborn.Characters.Targeting;
using UnityEngine;
namespace Riftborn.Characters.Core
{
    public sealed class CharacterContext : MonoBehaviour
    {
        [SerializeField] private CharacterStatsController stats;
        [SerializeField] private HealthController health;
        [SerializeField] private ResourceController resources;
        [SerializeField] private MovementController movement;
        [SerializeField] private ActionStateController actionState;
        [SerializeField] private CombatController combat;
        [SerializeField] private TargetingController targeting;
        [SerializeField] private AbilityController abilities;
        [SerializeField] private StatusEffectController statusEffects;
        [SerializeField] private InventoryController inventory;
        [SerializeField] private EquipmentController equipment;
        [SerializeField] private RuneController runes;
        [SerializeField] private CharacterAnimationController animationController;
        [SerializeField] private CharacterEvents characterEvents;
        public CharacterStatsController Stats => stats;
        public HealthController Health => health;
        public ResourceController Resources => resources;
        public MovementController Movement => movement;
        public ActionStateController ActionState => actionState;
        public CombatController Combat => combat;
        public TargetingController Targeting => targeting;
        public AbilityController Abilities => abilities;
        public StatusEffectController StatusEffects => statusEffects;
        public InventoryController Inventory => inventory;
        public EquipmentController Equipment => equipment;
        public RuneController Runes => runes;
        public CharacterAnimationController AnimationController => animationController;
        public CharacterEvents Events => characterEvents;
        private void Awake() { CacheMissingReferences(); ValidateReferences(); }
        private void Reset() { CacheMissingReferences(); }
        private void CacheMissingReferences()
        {
            stats ??= GetComponent<CharacterStatsController>(); health ??= GetComponent<HealthController>(); resources ??= GetComponent<ResourceController>(); movement ??= GetComponent<MovementController>(); actionState ??= GetComponent<ActionStateController>(); combat ??= GetComponent<CombatController>(); targeting ??= GetComponent<TargetingController>(); abilities ??= GetComponent<AbilityController>(); statusEffects ??= GetComponent<StatusEffectController>(); inventory ??= GetComponent<InventoryController>(); equipment ??= GetComponent<EquipmentController>(); runes ??= GetComponent<RuneController>(); animationController ??= GetComponent<CharacterAnimationController>(); characterEvents ??= GetComponent<CharacterEvents>();
        }
        private void ValidateReferences()
        {
            if (health == null) Debug.LogWarning($"{name} has no HealthController.", this);
            if (actionState == null) Debug.LogWarning($"{name} has no ActionStateController.", this);
            if (characterEvents == null) Debug.LogWarning($"{name} has no CharacterEvents.", this);
        }
    }
}
