using System;
using System.Collections.Generic;
using Riftborn.Characters.Abilities;
using Riftborn.Characters.Combat;
using Riftborn.Characters.Core;
using Riftborn.Characters.Defense;
using Riftborn.Characters.Health;
using Riftborn.Characters.Movement;
using Riftborn.Characters.Resources;
using Riftborn.Characters.Stats;
using Riftborn.Items;
using UnityEngine;

namespace Riftborn.Characters.Equipment
{
    [Serializable]
    public sealed class EquipmentController
    {
        [Header("References")]
        [NonSerialized]
        private CharacterContext context;

        [NonSerialized]
        private CharacterStatsController stats;

        [NonSerialized]
        private DefenseController defense;

        [SerializeField]
        private CombatController combat;

        [NonSerialized]
        private HealthController health;

        [SerializeField]
        private ResourceController resources;

        [SerializeField]
        private AbilityController abilities;

        [SerializeField]
        private MovementController movement;

        private readonly Dictionary<EquipmentSlot, ItemInstance>
            equipped = new();

        public event Action<EquipmentSlot, ItemInstance>
            EquipmentInstanceChanged;

        public event Action<EquipmentSlot, EquipmentItemData>
            EquipmentChanged;

        public void Initialize(CharacterContext owner, CombatController combatModule, ResourceController resourceModule, AbilityController abilityModule, MovementController movementModule)
        {
            context = owner;
            stats = owner?.Stats;
            defense = owner?.Defense;
            health = owner?.Health;
            combat = combatModule ?? combat;
            resources = resourceModule ?? resources;
            abilities = abilityModule ?? abilities;
            movement = movementModule ?? movement;
            ValidateReferences();
        }

        public bool Equip(
            ItemInstance itemInstance)
        {
            CacheReferences();

            if (itemInstance == null ||
                !itemInstance.IsValid)
            {
                return false;
            }

            EquipmentItemData equipmentData =
                itemInstance.Item
                    as EquipmentItemData;

            if (equipmentData == null)
            {
                Debug.LogWarning(
                    $"[EQUIPMENT] O item " +
                    $"'{itemInstance.DisplayName}' " +
                    "não é equipável.", context);

                return false;
            }

            EquipmentSlot slot =
                equipmentData.Slot;

            ItemInstance previousInstance =
                GetEquippedInstance(
                    slot);

            if (ReferenceEquals(
                    previousInstance,
                    itemInstance))
            {
                return true;
            }

            if (previousInstance != null)
            {
                RemoveAppliedModifiers(
                    previousInstance);

                equipped.Remove(
                    slot);
            }

            if (!TryApplyItemModifiers(
                    itemInstance,
                    equipmentData))
            {
                RemoveAppliedModifiers(
                    itemInstance);

                bool previousRestored =
                    TryRestorePreviousEquipment(
                        slot,
                        previousInstance);

                if (!previousRestored)
                {
                    Debug.LogError(
                        $"[EQUIPMENT] Falha ao restaurar " +
                        $"o item anterior do slot {slot}.", context);
                }

                Debug.LogError(
                    $"[EQUIPMENT] Não foi possível equipar " +
                    $"'{itemInstance.DisplayName}'.", context);

                return false;
            }

            equipped[slot] =
                itemInstance;

            NotifyEquipmentChanged(
                slot,
                itemInstance);

            Debug.Log(
                $"[EQUIPMENT] Equipado: " +
                $"{itemInstance.DisplayName} | " +
                $"Slot: {slot} | " +
                $"ID: {itemInstance.InstanceId}", context);

            return true;
        }

        public bool Equip(
            EquipmentItemData item)
        {
            if (item == null)
            {
                return false;
            }

            ItemInstance temporaryInstance =
                new ItemInstance(
                    item,
                    quantity: 1,
                    rarity: null);

            return Equip(
                temporaryInstance);
        }

        public bool Unequip(
            EquipmentSlot slot)
        {
            if (!equipped.TryGetValue(
                    slot,
                    out ItemInstance itemInstance))
            {
                return false;
            }

            RemoveAppliedModifiers(
                itemInstance);

            equipped.Remove(
                slot);

            NotifyEquipmentChanged(
                slot,
                null);

            Debug.Log(
                $"[EQUIPMENT] Slot {slot} desequipado.", context);

            return true;
        }

        public ItemInstance GetEquippedInstance(
            EquipmentSlot slot)
        {
            equipped.TryGetValue(
                slot,
                out ItemInstance itemInstance);

            return itemInstance;
        }

        public EquipmentItemData GetEquippedItem(
            EquipmentSlot slot)
        {
            ItemInstance itemInstance =
                GetEquippedInstance(
                    slot);

            return itemInstance?.Item
                as EquipmentItemData;
        }

        public bool IsSlotOccupied(
            EquipmentSlot slot)
        {
            return equipped.ContainsKey(
                slot);
        }

        private bool TryApplyItemModifiers(
            ItemInstance itemInstance,
            EquipmentItemData equipmentData)
        {
            if (itemInstance == null ||
                equipmentData == null)
            {
                return false;
            }

            if (!ApplyBaseStatModifiers(
                    itemInstance,
                    equipmentData))
            {
                return false;
            }

            if (!ApplyBaseDefenseModifiers(
                    itemInstance,
                    equipmentData))
            {
                return false;
            }

            if (!ApplyAffixCollection(
                    itemInstance,
                    itemInstance.Prefixes,
                    "prefix"))
            {
                return false;
            }

            if (!ApplyAffixCollection(
                    itemInstance,
                    itemInstance.Suffixes,
                    "suffix"))
            {
                return false;
            }

            return true;
        }

        private bool ApplyBaseStatModifiers(
            ItemInstance itemInstance,
            EquipmentItemData equipmentData)
        {
            if (equipmentData.StatModifiers == null)
            {
                return true;
            }

            for (int index = 0;
                 index <
                 equipmentData.StatModifiers.Count;
                 index++)
            {
                StatModifierDefinition definition =
                    equipmentData.StatModifiers[index];

                if (definition == null)
                {
                    continue;
                }

                if (stats == null)
                {
                    return false;
                }

                string modifierId =
                    $"equipment:{itemInstance.InstanceId}:" +
                    $"base-stat:{index}:{definition.Stat}";

                StatModifier modifier =
                    new StatModifier(
                        id: modifierId,
                        source: itemInstance,
                        stat: definition.Stat,
                        flatValue:
                            definition.FlatValue,
                        additivePercent:
                            definition.AdditivePercent,
                        multiplicativePercent:
                            definition.MultiplicativePercent);

                if (!stats.AddModifier(
                        modifier))
                {
                    return false;
                }
            }

            return true;
        }

        private bool ApplyBaseDefenseModifiers(
            ItemInstance itemInstance,
            EquipmentItemData equipmentData)
        {
            if (equipmentData.DefenseModifiers == null)
            {
                return true;
            }

            for (int index = 0;
                 index <
                 equipmentData.DefenseModifiers.Count;
                 index++)
            {
                DefenseModifierDefinition definition =
                    equipmentData.DefenseModifiers[index];

                if (definition == null)
                {
                    continue;
                }

                if (defense == null)
                {
                    return false;
                }

                string modifierId =
                    $"equipment:{itemInstance.InstanceId}:" +
                    $"base-defense:{index}:" +
                    $"{definition.DefenseType}";

                DefenseModifier modifier =
                    new DefenseModifier(
                        id: modifierId,
                        source: itemInstance,
                        defenseType:
                            definition.DefenseType,
                        flatValue:
                            definition.FlatValue,
                        additivePercent:
                            definition.AdditivePercent,
                        multiplicativePercent:
                            definition.MultiplicativePercent);

                if (!defense.AddModifier(
                        modifier))
                {
                    return false;
                }
            }

            return true;
        }

        private bool ApplyAffixCollection(
            ItemInstance itemInstance,
            IReadOnlyList<ItemAffixRoll> affixes,
            string collectionName)
        {
            if (affixes == null)
            {
                return true;
            }

            for (int index = 0;
                 index < affixes.Count;
                 index++)
            {
                ItemAffixRoll roll =
                    affixes[index];

                if (roll == null ||
                    !roll.IsValid ||
                    roll.Affix == null)
                {
                    Debug.LogError(
                        $"[EQUIPMENT] Afixo inválido encontrado em " +
                        $"'{itemInstance.DisplayName}'.", context);

                    return false;
                }

                if (!ApplyAffix(
                        itemInstance,
                        roll,
                        collectionName,
                        index))
                {
                    return false;
                }
            }

            return true;
        }

        private bool ApplyAffix(
            ItemInstance itemInstance,
            ItemAffixRoll roll,
            string collectionName,
            int index)
        {
            ItemAffixData affix =
                roll.Affix;

            string modifierId =
                $"equipment:{itemInstance.InstanceId}:" +
                $"{collectionName}:{index}:" +
                $"{affix.AffixId}:T{roll.Tier}";

            switch (affix.EffectType)
            {
                case ItemAffixEffectType.CharacterStat:
                    return ApplyCharacterStatAffix(
                        itemInstance,
                        roll,
                        modifierId);

                case ItemAffixEffectType.PhysicalDefense:
                    return ApplyDefenseAffix(
                        itemInstance,
                        roll,
                        modifierId,
                        DefenseType.Physical);

                case ItemAffixEffectType.MagicalDefense:
                    return ApplyDefenseAffix(
                        itemInstance,
                        roll,
                        modifierId,
                        DefenseType.Magical);

                case ItemAffixEffectType.BasicAttackDamage:
                    return ApplyCombatAffix(
                        itemInstance,
                        roll,
                        modifierId,
                        CombatModifierType.BasicAttackDamage);

                case ItemAffixEffectType.AttackSpeed:
                    return ApplyCombatAffix(
                        itemInstance,
                        roll,
                        modifierId,
                        CombatModifierType.AttackSpeed);

                case ItemAffixEffectType.CriticalChance:
                    return ApplyCombatAffix(
                        itemInstance,
                        roll,
                        modifierId,
                        CombatModifierType.CriticalChance);

                case ItemAffixEffectType.CriticalMultiplier:
                    return ApplyCombatAffix(
                        itemInstance,
                        roll,
                        modifierId,
                        CombatModifierType.CriticalMultiplier);

                case ItemAffixEffectType.MaximumHealth:
                    return ApplyHealthAffix(
                        itemInstance,
                        roll,
                        modifierId);

                case ItemAffixEffectType.MaximumResource:
                    return ApplyResourceAffix(
                        itemInstance,
                        roll,
                        modifierId,
                        ResourceModifierType.MaximumResource);

                case ItemAffixEffectType.ResourceRegeneration:
                    return ApplyResourceAffix(
                        itemInstance,
                        roll,
                        modifierId,
                        ResourceModifierType.Regeneration);

                case ItemAffixEffectType.AbilityDamage:
                    return ApplyAbilityAffix(
                        itemInstance,
                        roll,
                        modifierId,
                        AbilityModifierType.AbilityDamage);

                case ItemAffixEffectType.MovementSpeed:
                    return ApplyMovementAffix(
                        itemInstance,
                        roll,
                        modifierId);

                case ItemAffixEffectType.CooldownReduction:
                    return ApplyAbilityAffix(
                        itemInstance,
                        roll,
                        modifierId,
                        AbilityModifierType.CooldownReduction);

                case ItemAffixEffectType.ResourceCostReduction:
                    return ApplyAbilityAffix(
                        itemInstance,
                        roll,
                        modifierId,
                        AbilityModifierType.ResourceCostReduction);

                default:
                    Debug.LogWarning(
                        $"[EQUIPMENT] O efeito " +
                        $"'{affix.EffectType}' do afixo " +
                        $"'{affix.DisplayName}' ainda não possui " +
                        "um sistema de aplicação conectado.", context);

                    return true;
            }
        }

        private bool ApplyCharacterStatAffix(
            ItemInstance itemInstance,
            ItemAffixRoll roll,
            string modifierId)
        {
            if (stats == null ||
                roll?.Affix == null)
            {
                return false;
            }

            ConvertAffixValue(
                roll,
                out float flatValue,
                out float additivePercent,
                out float multiplicativePercent);

            StatModifier modifier =
                new StatModifier(
                    id: modifierId,
                    source: itemInstance,
                    stat:
                        roll.Affix.CharacterStat,
                    flatValue:
                        flatValue,
                    additivePercent:
                        additivePercent,
                    multiplicativePercent:
                        multiplicativePercent);

            return stats.AddModifier(
                modifier);
        }

        private bool ApplyDefenseAffix(
            ItemInstance itemInstance,
            ItemAffixRoll roll,
            string modifierId,
            DefenseType defenseType)
        {
            if (defense == null)
            {
                return false;
            }

            ConvertAffixValue(
                roll,
                out float flatValue,
                out float additivePercent,
                out float multiplicativePercent);

            DefenseModifier modifier =
                new DefenseModifier(
                    id: modifierId,
                    source: itemInstance,
                    defenseType:
                        defenseType,
                    flatValue:
                        flatValue,
                    additivePercent:
                        additivePercent,
                    multiplicativePercent:
                        multiplicativePercent);

            return defense.AddModifier(
                modifier);
        }

        private bool ApplyCombatAffix(
            ItemInstance itemInstance,
            ItemAffixRoll roll,
            string modifierId,
            CombatModifierType modifierType)
        {
            if (combat == null)
            {
                return false;
            }

            ConvertAffixValue(
                roll,
                out float flatValue,
                out float additivePercent,
                out float multiplicativePercent);

            CombatModifier modifier =
                new CombatModifier(
                    id: modifierId,
                    source: itemInstance,
                    modifierType:
                        modifierType,
                    flatValue:
                        flatValue,
                    additivePercent:
                        additivePercent,
                    multiplicativePercent:
                        multiplicativePercent);

            return combat.AddModifier(
                modifier);
        }

        private bool ApplyHealthAffix(
            ItemInstance itemInstance,
            ItemAffixRoll roll,
            string modifierId)
        {
            if (health == null)
            {
                return false;
            }

            ConvertAffixValue(
                roll,
                out float flatValue,
                out float additivePercent,
                out float multiplicativePercent);

            HealthModifier modifier =
                new HealthModifier(
                    id: modifierId,
                    source: itemInstance,
                    flatValue:
                        flatValue,
                    additivePercent:
                        additivePercent,
                    multiplicativePercent:
                        multiplicativePercent);

            return health.AddModifier(
                modifier);
        }

        private bool ApplyResourceAffix(
            ItemInstance itemInstance,
            ItemAffixRoll roll,
            string modifierId,
            ResourceModifierType modifierType)
        {
            if (resources == null)
            {
                return false;
            }

            ConvertAffixValue(
                roll,
                out float flatValue,
                out float additivePercent,
                out float multiplicativePercent);

            ResourceModifier modifier =
                new ResourceModifier(
                    id: modifierId,
                    source: itemInstance,
                    modifierType:
                        modifierType,
                    flatValue:
                        flatValue,
                    additivePercent:
                        additivePercent,
                    multiplicativePercent:
                        multiplicativePercent);

            return resources.AddModifier(
                modifier);
        }

        private bool ApplyAbilityAffix(
            ItemInstance itemInstance,
            ItemAffixRoll roll,
            string modifierId,
            AbilityModifierType modifierType)
        {
            if (abilities == null)
            {
                return false;
            }

            ConvertAffixValue(
                roll,
                out float flatValue,
                out float additivePercent,
                out float multiplicativePercent);

            AbilityModifier modifier =
                new AbilityModifier(
                    id: modifierId,
                    source: itemInstance,
                    modifierType:
                        modifierType,
                    flatValue:
                        flatValue,
                    additivePercent:
                        additivePercent,
                    multiplicativePercent:
                        multiplicativePercent);

            return abilities.AddModifier(
                modifier);
        }

        private bool ApplyMovementAffix(
            ItemInstance itemInstance,
            ItemAffixRoll roll,
            string modifierId)
        {
            if (movement == null)
            {
                return false;
            }

            ConvertAffixValue(
                roll,
                out float flatValue,
                out float additivePercent,
                out float multiplicativePercent);

            MovementModifier modifier =
                new MovementModifier(
                    id: modifierId,
                    source: itemInstance,
                    flatValue:
                        flatValue,
                    additivePercent:
                        additivePercent,
                    multiplicativePercent:
                        multiplicativePercent);

            return movement.AddModifier(
                modifier);
        }

        private static void ConvertAffixValue(
            ItemAffixRoll roll,
            out float flatValue,
            out float additivePercent,
            out float multiplicativePercent)
        {
            flatValue = 0f;
            additivePercent = 0f;
            multiplicativePercent = 0f;

            if (roll?.Affix == null)
            {
                return;
            }

            switch (roll.Affix.ValueMode)
            {
                case ItemAffixValueMode.Flat:
                    flatValue =
                        roll.RolledValue;
                    break;

                case ItemAffixValueMode.AdditivePercent:
                    additivePercent =
                        roll.RolledValue;
                    break;

                case ItemAffixValueMode.MultiplicativePercent:
                    multiplicativePercent =
                        roll.RolledValue;
                    break;
            }
        }

        private void RemoveAppliedModifiers(
            ItemInstance itemInstance)
        {
            if (itemInstance == null)
            {
                return;
            }

            stats?.RemoveModifiersFromSource(
                itemInstance);

            defense?.RemoveModifiersFromSource(
                itemInstance);

            combat?.RemoveModifiersFromSource(
                itemInstance);

            health?.RemoveModifiersFromSource(
                itemInstance);

            resources?.RemoveModifiersFromSource(
                itemInstance);

            abilities?.RemoveModifiersFromSource(
                itemInstance);

            movement?.RemoveModifiersFromSource(
                itemInstance);
        }

        private bool TryRestorePreviousEquipment(
            EquipmentSlot slot,
            ItemInstance previousInstance)
        {
            if (previousInstance == null)
            {
                return true;
            }

            EquipmentItemData previousData =
                previousInstance.Item
                    as EquipmentItemData;

            if (previousData == null)
            {
                return false;
            }

            if (!TryApplyItemModifiers(
                    previousInstance,
                    previousData))
            {
                RemoveAppliedModifiers(
                    previousInstance);

                return false;
            }

            equipped[slot] =
                previousInstance;

            return true;
        }

        private void NotifyEquipmentChanged(
            EquipmentSlot slot,
            ItemInstance itemInstance)
        {
            EquipmentInstanceChanged?.Invoke(
                slot,
                itemInstance);

            EquipmentItemData equipmentData =
                itemInstance?.Item
                    as EquipmentItemData;

            EquipmentChanged?.Invoke(
                slot,
                equipmentData);
        }

        private void CacheReferences()
        {
            stats ??= context?.Stats;
            defense ??= context?.Defense;
            health ??= context?.Health;
        }

        private void ValidateReferences()
        {
            if (stats == null)
            {
                Debug.LogError(
                    $"{nameof(EquipmentController)} requires a " +
                    $"{nameof(CharacterStatsController)}.", context);
            }

            if (defense == null)
            {
                Debug.LogError(
                    $"{nameof(EquipmentController)} requires a " +
                    $"{nameof(DefenseController)}.", context);
            }

            if (combat == null)
            {
                Debug.LogError(
                    $"{nameof(EquipmentController)} requires a " +
                    $"{nameof(CombatController)}.", context);
            }

            if (health == null)
            {
                Debug.LogError(
                    $"{nameof(EquipmentController)} requires a " +
                    $"{nameof(HealthController)}.", context);
            }

            if (resources == null)
            {
                Debug.LogError(
                    $"{nameof(EquipmentController)} requires a " +
                    $"{nameof(ResourceController)}.", context);
            }

            if (abilities == null)
            {
                Debug.LogError(
                    $"{nameof(EquipmentController)} requires an " +
                    $"{nameof(AbilityController)}.", context);
            }

            if (movement == null)
            {
                Debug.LogError(
                    $"{nameof(EquipmentController)} requires a " +
                    $"{nameof(MovementController)}.", context);
            }
        }
    }
}