using System;
using Riftborn.Characters.Abilities;
using Riftborn.Characters.Combat;
using Riftborn.Characters.Core;
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
using Riftborn.Damage;
using Riftborn.Items;
using UnityEngine;

namespace Riftborn.Characters.Controllers
{
    public enum PlayerLifeState
    {
        Alive = 0,
        Dead = 1
    }

    [DisallowMultipleComponent]
    public sealed class PlayerController : CharacterContext, ICharacterController
    {
        [Header("Player Systems")]
        [SerializeField]
        private PlayerAutoAttackController autoAttack = new();

        [SerializeField]
        private PlayerInputReader inputReader = new();

        [SerializeField]
        private PlayerInputModeController inputMode = new();

        [SerializeField]
        private WasdPlayerInput wasdInput = new();

        [SerializeField]
        private ClickMovePlayerInput clickMoveInput = new();

        [Header("Runtime")]
        [SerializeField]
        private PlayerLifeState lifeState =
            PlayerLifeState.Alive;

        [SerializeField]
        private bool showDebugLogs = true;

        private bool eventsSubscribed;
        private CharacterContext subscribedTarget;

        private CharacterContext context =>
            this;

        public event Action<
            PlayerLifeState,
            PlayerLifeState> LifeStateChanged;

        public event Action<
            CharacterContext,
            CharacterContext> TargetChanged;

        public event Action<HealthChangedEventArgs>
            HealthChanged;

        public event Action<DamageResult>
            DamageTaken;

        public event Action<DamageResult, DamageApplicationResult>
            DamageDealt;

        public event Action<bool>
            BasicAttackProcessed;

        public event Action<int, bool>
            AbilityProcessed;

        public event Action<float, float>
            ResourceChanged;

        public event Action<ResourceChangedEventArgs>
            ResourceStateChanged;

        public event Action<float>
            ResourceConsumed;

        public event Action<float>
            ResourceRestored;

        public event Action<float, float>
            ResourceRegenerationChanged;

        public event Action
            ResourceValuesChanged;

        public event Action<StatusEffectBase>
            StatusApplied;

        public event Action<StatusEffectBase>
            StatusRemoved;

        public event Action<StatusEffectBase>
            StatusReapplied;

        public event Action
            InventoryChanged;

        public event Action<int, ItemInstance>
            InventorySlotChanged;

        public event Action<EquipmentSlot, ItemInstance>
            EquipmentInstanceChanged;

        public event Action<EquipmentSlot, EquipmentItemData>
            EquipmentChanged;

        public event Action<ExperienceChangedEventArgs>
            ExperienceChanged;

        public event Action<LevelUpEventArgs>
            LeveledUp;

        public event Action<ProgressionPointsChangedEventArgs>
            ProgressionPointsChanged;

        public event Action<StatPointSpentEventArgs>
            StatPointSpent;

        public event Action<RunePageData>
            RunePageEquipped;

        public event Action<RunePageData>
            RunePageRemoved;

        public event Action<RuneSelection>
            RuneEffectRegistered;

        public event Action<RuneSelection>
            RuneEffectRemoved;

        public event Action Died;
        public event Action Revived;

        public PlayerLifeState LifeState =>
            lifeState;

        public CharacterContext ControlledCharacter =>
            this;

        public bool IsAlive =>
            lifeState == PlayerLifeState.Alive &&
            health != null &&
            !health.IsDead;

        public CharacterContext CurrentTarget =>
            targeting != null
                ? targeting.CurrentTarget
                : null;

        public bool HasTarget =>
            targeting != null &&
            targeting.HasTarget;

        public bool AutoAttackEnabled =>
            autoAttack != null &&
            autoAttack.AutoAttackEnabled;

        public ResourceType CurrentResourceType =>
            resources != null
                ? resources.ResourceType
                : ResourceType.Custom;

        public float CurrentResource =>
            resources != null
                ? resources.CurrentValue
                : 0f;

        public float MaximumResource =>
            resources != null
                ? resources.MaxValue
                : 0f;

        public float ResourcePercentage =>
            resources != null
                ? resources.ResourcePercentage
                : 0f;

        public int InventorySlotCount =>
            inventory != null
                ? inventory.SlotCount
                : 0;

        public int OccupiedInventorySlotCount =>
            inventory != null
                ? inventory.OccupiedSlotCount
                : 0;

        public int EmptyInventorySlotCount =>
            inventory != null
                ? inventory.EmptySlotCount
                : 0;

        public int CurrentLevel =>
            progression != null
                ? progression.CurrentLevel
                : 1;

        public int CurrentExperience =>
            progression != null
                ? progression.CurrentExperience
                : 0;

        public int AvailableStatPoints =>
            progression != null
                ? progression.AvailableStatPoints
                : 0;

        public int AvailableRunePoints =>
            progression != null
                ? progression.AvailableRunePoints
                : 0;

        private void Awake()
        {
            InitializeCharacterContext();
            CacheReferences();
            InitializePlayerModules();
            SynchronizeLifeState();
        }

        private void OnEnable()
        {
            EnableCharacterContext();
            CacheReferences();
            SubscribeToEvents();
            InitializePlayerModules();
            SynchronizeLifeState();
        }

        private void OnDisable()
        {
            UnsubscribeFromEvents();
            StopMovement();
            autoAttack?.ResetRuntimeState();
            DisableCharacterContext();
        }

        private void Reset()
        {
            CacheAttachedSystems();
            CacheReferences();
            ValidateCharacterContext();
        }

        private void Update()
        {
            float deltaTime = Time.deltaTime;

            resources?.Tick(deltaTime);
            statusEffects?.Tick(deltaTime);
            movement?.Tick(deltaTime);
            inputReader?.Tick();
            inputMode?.Tick();

            if (inputMode == null ||
                inputMode.CurrentMode == PlayerInputMode.Wasd)
            {
                wasdInput?.Tick();
            }
            else
            {
                clickMoveInput?.Tick();
            }

            TickAutoAttack();
        }

        public bool ActivateWasdMode()
        {
            CacheReferences();

            if (movement == null)
            {
                LogMissingSystem(
                    nameof(MovementController));

                return false;
            }

            movement.ActivateWasdMode();
            return true;
        }

        public bool ActivateClickMoveMode()
        {
            CacheReferences();

            if (movement == null)
            {
                LogMissingSystem(
                    nameof(MovementController));

                return false;
            }

            movement.ActivateClickToMoveMode();
            return true;
        }

        public bool SetMoveInput(Vector2 input)
        {
            CacheReferences();

            if (movement == null)
            {
                LogMissingSystem(
                    nameof(MovementController));

                return false;
            }

            if (!IsAlive)
            {
                movement.SetMoveInput(
                    Vector2.zero);

                return false;
            }

            movement.SetMoveInput(
                input);

            return true;
        }

        public bool SetClickDestination(
            Vector3 worldPosition)
        {
            CacheReferences();

            if (!IsAlive ||
                movement == null)
            {
                return false;
            }

            return movement.SetClickDestination(
                worldPosition);
        }

        public void CancelClickDestination()
        {
            CacheReferences();
            movement?.CancelClickDestination();
        }

        public void StopMovement()
        {
            CacheReferences();

            movement?.SetMoveInput(
                Vector2.zero);

            movement?.CancelClickDestination();
        }

        public bool IsValidTarget(
            CharacterContext candidate)
        {
            CacheReferences();

            return IsAlive &&
                   targeting != null &&
                   targeting.IsValidTarget(
                       candidate);
        }

        public bool TrySelectTarget(
            CharacterContext newTarget)
        {
            CacheReferences();

            if (!IsAlive ||
                targeting == null)
            {
                return false;
            }

            return targeting.SetTarget(
                newTarget);
        }

        public void ClearTarget()
        {
            CacheReferences();
            targeting?.ClearTarget();
        }

        public DamageApplicationResult ReceiveDamage(
            DamageResult result)
        {
            CacheReferences();

            DamageRequest request =
                result?.Request;

            if (request == null ||
                health == null ||
                health.IsDead ||
                lifeState != PlayerLifeState.Alive)
            {
                return null;
            }

            if (request.Target != null &&
                !ReferenceEquals(
                    request.Target,
                    context))
            {
                return null;
            }

            DamageApplicationResult application =
                health.ApplyDamage(
                    result);

            if (application != null &&
                application.DamagedHealth)
            {
                context?.Events?.
                    RaiseDamageTaken(
                        result);
            }

            return application;
        }

        public DamageApplicationResult ProcessOutgoingDamage(
            DamageResult result)
        {
            CacheReferences();

            DamageRequest request =
                result?.Request;

            if (request == null)
            {
                return null;
            }

            if (request.Source != null &&
                !ReferenceEquals(
                    request.Source,
                    context))
            {
                return null;
            }

            DamageApplicationResult application =
                CharacterControllerResolver.DeliverToTarget(
                    result);

            if (application == null)
            {
                return null;
            }

            context?.Events?.
                RaiseDamageDealt(
                    result);

            if (result.WasCritical)
            {
                context?.Events?.
                    RaiseCriticalHit(
                        result);
            }

            if (request.Origin ==
                DamageOrigin.BasicAttack)
            {
                combat?.NotifyBasicAttackResolved(
                    result,
                    application);
            }

            DamageDealt?.Invoke(
                result,
                application);

            return application;
        }

        public bool RequestBasicAttack()
        {
            CacheReferences();

            bool succeeded =
                TryExecuteBasicAttack(
                    CurrentTarget);

            BasicAttackProcessed?.Invoke(
                succeeded);

            return succeeded;
        }

        public bool RequestAbility(int slot)
        {
            CacheReferences();

            bool succeeded =
                IsAlive &&
                abilities != null &&                abilities.TryUse(
                    slot,
                    CurrentTarget);

            AbilityProcessed?.Invoke(
                slot,
                succeeded);

            return succeeded;
        }

        public void SetAutoAttackEnabled(
            bool enabled)
        {
            CacheReferences();
            autoAttack?.SetAutoAttackEnabled(
                enabled);
        }

        public void StopAutoAttack(
            bool clearSelection)
        {
            CacheReferences();

            autoAttack?.ResetRuntimeState();

            if (clearSelection)
            {
                targeting?.ClearTarget();
            }
        }

        public bool CanConsumeResource(float amount)
        {
            CacheReferences();

            return resources != null &&
                   resources.CanConsume(
                       amount);
        }

        public bool RequestConsumeResource(
            float amount)
        {
            CacheReferences();

            if (!IsAlive ||
                resources == null)
            {
                return false;
            }

            return resources.Consume(
                amount);
        }

        public float RequestRestoreResource(
            float amount)
        {
            CacheReferences();

            return resources != null
                ? resources.Restore(
                    amount)
                : 0f;
        }

        public bool RequestFillResource()
        {
            CacheReferences();

            if (resources == null)
            {
                return false;
            }

            resources.FillToMaximum();
            return true;
        }

        public bool RequestEmptyResource()
        {
            CacheReferences();

            if (resources == null)
            {
                return false;
            }

            resources.Empty();
            return true;
        }

        public bool RequestApplyStatus(
            StatusEffectBase effect)
        {
            CacheReferences();

            return IsAlive &&
                   statusEffects != null &&
                   statusEffects.Apply(
                       effect);
        }

        public bool RequestRemoveStatus(
            StatusEffectBase effect)
        {
            CacheReferences();

            return statusEffects != null &&
                   statusEffects.Remove(
                       effect);
        }

        public int RequestCleanseStatus(
            StatusEffectTag tags)
        {
            CacheReferences();

            return statusEffects != null
                ? statusEffects.Cleanse(
                    tags)
                : 0;
        }

        public int RequestClearStatusEffects()
        {
            CacheReferences();

            return statusEffects != null
                ? statusEffects.ClearAllEffects()
                : 0;
        }

        public bool HasStatus(
            StatusEffectTag tags)
        {
            CacheReferences();

            return statusEffects != null &&
                   statusEffects.Has(
                       tags);
        }

        public int CountStatusEffects(
            StatusEffectTag tags)
        {
            CacheReferences();

            return statusEffects != null
                ? statusEffects.Count(
                    tags)
                : 0;
        }

        public ItemInstance GetInventoryItem(
            int slot)
        {
            CacheReferences();

            return inventory?.GetItemInstance(
                slot);
        }

        public bool IsInventorySlotOccupied(
            int slot)
        {
            CacheReferences();

            return inventory != null &&
                   inventory.IsOccupied(
                       slot);
        }

        public bool CanAddToInventory(
            ItemInstance itemInstance)
        {
            CacheReferences();

            return inventory != null &&
                   inventory.CanAdd(
                       itemInstance);
        }

        public bool RequestAddToInventory(
            ItemInstance itemInstance)
        {
            CacheReferences();

            return inventory != null &&
                   inventory.Add(
                       itemInstance);
        }

        public bool RequestAddToInventory(
            ItemData item,
            int quantity)
        {
            CacheReferences();

            return inventory != null &&
                   inventory.Add(
                       item,
                       quantity);
        }

        public bool RequestRemoveInventoryItem(
            int slot,
            int quantity)
        {
            CacheReferences();

            return inventory != null &&
                   inventory.RemoveAt(
                       slot,
                       quantity);
        }

        public ItemInstance RequestTakeInventoryItem(
            int slot)
        {
            CacheReferences();

            return inventory?.TakeAt(
                slot);
        }

        public bool RequestMoveInventoryItem(
            int fromSlot,
            int toSlot)
        {
            CacheReferences();

            return inventory != null &&
                   inventory.Move(
                       fromSlot,
                       toSlot);
        }

        public bool RequestClearInventory()
        {
            CacheReferences();

            if (inventory == null)
            {
                return false;
            }

            inventory.Clear();
            return true;
        }

        public bool RequestEquip(
            ItemInstance itemInstance)
        {
            CacheReferences();

            return equipment != null &&
                   equipment.Equip(
                       itemInstance);
        }

        public bool RequestUnequip(
            EquipmentSlot slot)
        {
            CacheReferences();

            return equipment != null &&
                   equipment.Unequip(
                       slot);
        }

        public ItemInstance GetEquippedItem(
            EquipmentSlot slot)
        {
            CacheReferences();

            return equipment?.GetEquippedInstance(
                slot);
        }

        public bool RequestEquipFromInventory(
            int inventorySlot)
        {
            CacheReferences();

            if (inventory == null ||
                equipment == null)
            {
                return false;
            }

            ItemInstance candidate =
                inventory.GetItemInstance(
                    inventorySlot);

            if (candidate == null ||
                !candidate.IsValid ||
                candidate.Item is not
                    EquipmentItemData equipmentData)
            {
                return false;
            }

            EquipmentSlot equipmentSlot =
                equipmentData.Slot;

            ItemInstance previousItem =
                equipment.GetEquippedInstance(
                    equipmentSlot);

            ItemInstance removedCandidate =
                inventory.TakeAt(
                    inventorySlot);

            if (removedCandidate == null)
            {
                return false;
            }

            if (!equipment.Equip(
                    removedCandidate))
            {
                bool candidateReturned =
                    inventory.Add(
                        removedCandidate);

                if (!candidateReturned)
                {
                    LogCriticalTransactionFailure(
                        "não foi possível devolver o item ao " +
                        "inventário após falha ao equipar");
                }

                return false;
            }

            if (previousItem == null)
            {
                return true;
            }

            if (inventory.Add(
                    previousItem))
            {
                return true;
            }

            bool previousRestored =
                equipment.Equip(
                    previousItem);

            bool candidateReturnedAfterRollback =
                inventory.Add(
                    removedCandidate);

            if (!previousRestored ||
                !candidateReturnedAfterRollback)
            {
                LogCriticalTransactionFailure(
                    "o rollback de troca de equipamento falhou");
            }

            return false;
        }

        public bool RequestUnequipToInventory(
            EquipmentSlot slot)
        {
            CacheReferences();

            if (inventory == null ||
                equipment == null)
            {
                return false;
            }

            ItemInstance equippedItem =
                equipment.GetEquippedInstance(
                    slot);

            if (equippedItem == null ||
                !inventory.CanAdd(
                    equippedItem))
            {
                return false;
            }

            if (!equipment.Unequip(
                    slot))
            {
                return false;
            }

            if (inventory.Add(
                    equippedItem))
            {
                return true;
            }

            bool rollbackSucceeded =
                equipment.Equip(
                    equippedItem);

            if (!rollbackSucceeded)
            {
                LogCriticalTransactionFailure(
                    "não foi possível restaurar o equipamento " +
                    "após falha ao devolvê-lo ao inventário");
            }

            return false;
        }

        public bool RequestAddExperience(
            int amount)
        {
            CacheReferences();

            return progression != null &&
                   progression.AddExperience(
                       amount);
        }

        public bool RequestSpendStatPoint(
            CharacterStat stat)
        {
            CacheReferences();

            return progression != null &&
                   progression.TrySpendStatPoint(
                       stat);
        }

        public bool RequestEquipRunePage(
            RunePageData page)
        {
            CacheReferences();

            return runes != null &&
                   runes.EquipPage(
                       page);
        }

        public bool RequestRemoveRunePage()
        {
            CacheReferences();

            if (runes == null)
            {
                return false;
            }

            runes.RemovePage();
            return true;
        }


        private void InitializePlayerModules()
        {
            autoAttack ??= new PlayerAutoAttackController();
            inputReader ??= new PlayerInputReader();
            inputMode ??= new PlayerInputModeController();
            wasdInput ??= new WasdPlayerInput();
            clickMoveInput ??= new ClickMovePlayerInput();

            autoAttack.Initialize();
            wasdInput.Initialize(this);
            clickMoveInput.Initialize(this);
            inputReader.Initialize(this, this);
            inputMode.Initialize(
                this,
                wasdInput,
                clickMoveInput);
        }

        private void TickAutoAttack()
        {
            if (!IsAlive ||
                autoAttack == null ||                !autoAttack.AutoAttackEnabled)
            {
                autoAttack?.ResetRuntimeState();
                return;
            }

            CharacterContext selectedTarget =
                CurrentTarget;

            bool targetIsValid =
                targeting != null &&
                targeting.IsValidTarget(
                    selectedTarget);

            bool combatAvailable =
                combat != null;

            bool targetInRange =
                targetIsValid &&
                combatAvailable &&
                combat.IsTargetInRange(
                    selectedTarget);

            float distance =
                selectedTarget != null
                    ? Vector3.Distance(
                        transform.position,
                        selectedTarget.transform.position)
                    : 0f;

            float attackRange =
                combat != null
                    ? combat.AttackRange
                    : 0f;

            AutoAttackDecision decision =
                autoAttack.Evaluate(
                    selectedTarget,
                    targetIsValid,
                    combatAvailable,
                    targetInRange,
                    distance,
                    attackRange);

            switch (decision)
            {
                case AutoAttackDecision.InvalidTarget:
                    if (autoAttack.ClearInvalidTarget)
                    {
                        targeting?.ClearTarget();
                    }
                    break;

                case AutoAttackDecision.ReadyToAttack:
                    bool succeeded =
                        TryExecuteBasicAttack(
                            selectedTarget);

                    autoAttack.ReportAttackAttempt(
                        succeeded);
                    break;
            }
        }

        private bool TryExecuteBasicAttack(
            CharacterContext target)
        {
            if (!IsAlive ||
                combat == null)
            {
                return false;
            }

            if (!combat.TryCreateBasicAttack(
                    target,
                    out DamageResult result))
            {
                return false;
            }

            return ProcessOutgoingDamage(
                       result) != null;
        }

        private void SubscribeToEvents()
        {
            if (eventsSubscribed)
            {
                return;
            }

            if (health != null)
            {
                health.HealthChanged +=
                    HandleHealthChanged;

                health.DamageTaken +=
                    HandleDamageTaken;

                health.Died +=
                    HandleDied;

                health.Revived +=
                    HandleRevived;
            }

            if (targeting != null)
            {
                targeting.TargetChanged +=
                    HandleTargetChanged;

                SubscribeToSelectedTarget(
                    targeting.CurrentTarget);
            }

            if (autoAttack != null)
            {
                autoAttack.StopRequested +=
                    HandleAutoAttackStopRequested;
            }

            if (resources != null)
            {
                resources.ResourceChanged +=
                    HandleResourceChanged;

                resources.ResourceStateChanged +=
                    HandleResourceStateChanged;

                resources.ResourceConsumed +=
                    HandleResourceConsumed;

                resources.ResourceRestored +=
                    HandleResourceRestored;

                resources.RegenerationChanged +=
                    HandleResourceRegenerationChanged;

                resources.ResourceValuesChanged +=
                    HandleResourceValuesChanged;
            }

            if (statusEffects != null)
            {
                statusEffects.StatusApplied +=
                    HandleStatusApplied;

                statusEffects.StatusRemoved +=
                    HandleStatusRemoved;

                statusEffects.StatusReapplied +=
                    HandleStatusReapplied;
            }

            if (inventory != null)
            {
                inventory.InventoryChanged +=
                    HandleInventoryChanged;

                inventory.SlotChanged +=
                    HandleInventorySlotChanged;
            }

            if (equipment != null)
            {
                equipment.EquipmentInstanceChanged +=
                    HandleEquipmentInstanceChanged;

                equipment.EquipmentChanged +=
                    HandleEquipmentChanged;
            }

            if (progression != null)
            {
                progression.ExperienceChanged +=
                    HandleExperienceChanged;

                progression.LeveledUp +=
                    HandleLeveledUp;

                progression.PointsChanged +=
                    HandleProgressionPointsChanged;

                progression.StatPointSpent +=
                    HandleStatPointSpent;
            }

            if (runes != null)
            {
                runes.PageEquipped +=
                    HandleRunePageEquipped;

                runes.PageRemoved +=
                    HandleRunePageRemoved;

                runes.RuneEffectRegistered +=
                    HandleRuneEffectRegistered;

                runes.RuneEffectRemoved +=
                    HandleRuneEffectRemoved;
            }

            eventsSubscribed = true;
        }

        private void UnsubscribeFromEvents()
        {
            if (!eventsSubscribed)
            {
                return;
            }

            if (health != null)
            {
                health.HealthChanged -=
                    HandleHealthChanged;

                health.DamageTaken -=
                    HandleDamageTaken;

                health.Died -=
                    HandleDied;

                health.Revived -=
                    HandleRevived;
            }

            if (targeting != null)
            {
                targeting.TargetChanged -=
                    HandleTargetChanged;
            }

            UnsubscribeFromSelectedTarget();

            if (autoAttack != null)
            {
                autoAttack.StopRequested -=
                    HandleAutoAttackStopRequested;
            }

            if (resources != null)
            {
                resources.ResourceChanged -=
                    HandleResourceChanged;

                resources.ResourceStateChanged -=
                    HandleResourceStateChanged;

                resources.ResourceConsumed -=
                    HandleResourceConsumed;

                resources.ResourceRestored -=
                    HandleResourceRestored;

                resources.RegenerationChanged -=
                    HandleResourceRegenerationChanged;

                resources.ResourceValuesChanged -=
                    HandleResourceValuesChanged;
            }

            if (statusEffects != null)
            {
                statusEffects.StatusApplied -=
                    HandleStatusApplied;

                statusEffects.StatusRemoved -=
                    HandleStatusRemoved;

                statusEffects.StatusReapplied -=
                    HandleStatusReapplied;
            }

            if (inventory != null)
            {
                inventory.InventoryChanged -=
                    HandleInventoryChanged;

                inventory.SlotChanged -=
                    HandleInventorySlotChanged;
            }

            if (equipment != null)
            {
                equipment.EquipmentInstanceChanged -=
                    HandleEquipmentInstanceChanged;

                equipment.EquipmentChanged -=
                    HandleEquipmentChanged;
            }

            if (progression != null)
            {
                progression.ExperienceChanged -=
                    HandleExperienceChanged;

                progression.LeveledUp -=
                    HandleLeveledUp;

                progression.PointsChanged -=
                    HandleProgressionPointsChanged;

                progression.StatPointSpent -=
                    HandleStatPointSpent;
            }

            if (runes != null)
            {
                runes.PageEquipped -=
                    HandleRunePageEquipped;

                runes.PageRemoved -=
                    HandleRunePageRemoved;

                runes.RuneEffectRegistered -=
                    HandleRuneEffectRegistered;

                runes.RuneEffectRemoved -=
                    HandleRuneEffectRemoved;
            }

            eventsSubscribed = false;
        }

        private void HandleHealthChanged(
            HealthChangedEventArgs eventArgs)
        {
            HealthChanged?.Invoke(
                eventArgs);
        }

        private void HandleDamageTaken(
            DamageResult result)
        {
            statusEffects?.NotifyDamageTaken(
                result);

            DamageTaken?.Invoke(
                result);
        }

        private void HandleDied()
        {
            StopMovement();
            autoAttack?.ResetRuntimeState();
            targeting?.ClearTarget();

            SetLifeState(
                PlayerLifeState.Dead);

            Died?.Invoke();

            if (showDebugLogs)
            {
                Debug.Log(
                    $"[PLAYER CONTROLLER] {name} morreu.",
                    this);
            }
        }

        private void HandleRevived()
        {
            StopMovement();
            autoAttack?.ResetRuntimeState();

            SetLifeState(
                PlayerLifeState.Alive);

            Revived?.Invoke();

            if (showDebugLogs)
            {
                Debug.Log(
                    $"[PLAYER CONTROLLER] {name} renasceu.",
                    this);
            }
        }

        private void HandleTargetChanged(
            CharacterContext previousTarget,
            CharacterContext newTarget)
        {
            UnsubscribeFromSelectedTarget();
            SubscribeToSelectedTarget(
                newTarget);

            autoAttack?.NotifyTargetChanged(
                newTarget);

            TargetChanged?.Invoke(
                previousTarget,
                newTarget);
        }

        private void SubscribeToSelectedTarget(
            CharacterContext selectedTarget)
        {
            if (selectedTarget == null ||
                selectedTarget.Health == null)
            {
                subscribedTarget = null;
                return;
            }

            subscribedTarget =
                selectedTarget;

            subscribedTarget.Health.Died +=
                HandleSelectedTargetDied;
        }

        private void UnsubscribeFromSelectedTarget()
        {
            if (subscribedTarget == null ||
                subscribedTarget.Health == null)
            {
                subscribedTarget = null;
                return;
            }

            subscribedTarget.Health.Died -=
                HandleSelectedTargetDied;

            subscribedTarget = null;
        }

        private void HandleSelectedTargetDied()
        {
            if (showDebugLogs &&
                subscribedTarget != null)
            {
                Debug.Log(
                    $"[PLAYER CONTROLLER] O alvo " +
                    $"{subscribedTarget.name} morreu. " +
                    "Solicitando remoção da seleção.",
                    this);
            }

            targeting?.ClearTarget();
        }

        private void HandleAutoAttackStopRequested(
            bool clearSelection)
        {
            if (clearSelection)
            {
                targeting?.ClearTarget();
            }
        }

        private void HandleResourceChanged(
            float current,
            float maximum)
        {
            ResourceChanged?.Invoke(
                current,
                maximum);
        }

        private void HandleResourceStateChanged(
            ResourceChangedEventArgs eventArgs)
        {
            ResourceStateChanged?.Invoke(
                eventArgs);
        }

        private void HandleResourceConsumed(
            float amount)
        {
            ResourceConsumed?.Invoke(
                amount);
        }

        private void HandleResourceRestored(
            float amount)
        {
            ResourceRestored?.Invoke(
                amount);
        }

        private void HandleResourceRegenerationChanged(
            float previousValue,
            float newValue)
        {
            ResourceRegenerationChanged?.Invoke(
                previousValue,
                newValue);
        }

        private void HandleResourceValuesChanged()
        {
            ResourceValuesChanged?.Invoke();
        }

        private void HandleStatusApplied(
            StatusEffectBase effect)
        {
            StatusApplied?.Invoke(
                effect);
        }

        private void HandleStatusRemoved(
            StatusEffectBase effect)
        {
            StatusRemoved?.Invoke(
                effect);
        }

        private void HandleStatusReapplied(
            StatusEffectBase effect)
        {
            StatusReapplied?.Invoke(
                effect);
        }

        private void HandleInventoryChanged()
        {
            InventoryChanged?.Invoke();
        }

        private void HandleInventorySlotChanged(
            int slot,
            ItemInstance itemInstance)
        {
            InventorySlotChanged?.Invoke(
                slot,
                itemInstance);
        }

        private void HandleEquipmentInstanceChanged(
            EquipmentSlot slot,
            ItemInstance itemInstance)
        {
            EquipmentInstanceChanged?.Invoke(
                slot,
                itemInstance);
        }

        private void HandleEquipmentChanged(
            EquipmentSlot slot,
            EquipmentItemData equipmentData)
        {
            EquipmentChanged?.Invoke(
                slot,
                equipmentData);
        }

        private void HandleExperienceChanged(
            ExperienceChangedEventArgs eventArgs)
        {
            ExperienceChanged?.Invoke(
                eventArgs);
        }

        private void HandleLeveledUp(
            LevelUpEventArgs eventArgs)
        {
            LeveledUp?.Invoke(
                eventArgs);
        }

        private void HandleProgressionPointsChanged(
            ProgressionPointsChangedEventArgs eventArgs)
        {
            ProgressionPointsChanged?.Invoke(
                eventArgs);
        }

        private void HandleStatPointSpent(
            StatPointSpentEventArgs eventArgs)
        {
            StatPointSpent?.Invoke(
                eventArgs);
        }

        private void HandleRunePageEquipped(
            RunePageData page)
        {
            RunePageEquipped?.Invoke(
                page);
        }

        private void HandleRunePageRemoved(
            RunePageData page)
        {
            RunePageRemoved?.Invoke(
                page);
        }

        private void HandleRuneEffectRegistered(
            RuneSelection selection)
        {
            RuneEffectRegistered?.Invoke(
                selection);
        }

        private void HandleRuneEffectRemoved(
            RuneSelection selection)
        {
            RuneEffectRemoved?.Invoke(
                selection);
        }

        private void SynchronizeLifeState()
        {
            PlayerLifeState correctState =
                health != null &&
                health.IsDead
                    ? PlayerLifeState.Dead
                    : PlayerLifeState.Alive;

            SetLifeState(
                correctState);
        }

        private void SetLifeState(
            PlayerLifeState newState)
        {
            if (lifeState == newState)
            {
                return;
            }

            PlayerLifeState previousState =
                lifeState;

            lifeState =
                newState;

            LifeStateChanged?.Invoke(
                previousState,
                newState);
        }

        private void CacheReferences()
        {
            CacheAttachedSystems();

            movement ??=
                context?.Movement;



            targeting ??=
                context?.Targeting;



            combat ??=
                context?.Combat;



            abilities ??=
                context?.Abilities;



            autoAttack ??= new PlayerAutoAttackController();

            resources ??=
                context?.Resources;



            statusEffects ??=
                context?.StatusEffects;



            inventory ??=
                context?.Inventory;



            equipment ??=
                context?.Equipment;



            progression ??=
                context?.Progression;



            runes ??=
                context?.Runes;


        }

        private void OnValidate()
        {
            ValidateCharacterContext();
        }

        private void LogMissingSystem(
            string systemName)
        {
            if (!showDebugLogs)
            {
                return;
            }

            Debug.LogWarning(
                $"[PLAYER CONTROLLER] {name} não encontrou " +
                $"{systemName}.",
                this);
        }

        private void LogCriticalTransactionFailure(
            string message)
        {
            Debug.LogError(
                $"[PLAYER CONTROLLER] {name}: {message}. " +
                "O estado de inventário/equipamento precisa ser " +
                "verificado manualmente.",
                this);
        }
    }
}
