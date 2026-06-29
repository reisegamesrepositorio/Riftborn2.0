using System;
using System.Collections.Generic;
using Riftborn.Characters.ActionStates;
using Riftborn.Characters.Core;
using Riftborn.Characters.Equipment;
using Riftborn.Characters.Stats;
using Riftborn.Characters.Targeting;
using Riftborn.Damage;
using Riftborn.Items;
using UnityEngine;

namespace Riftborn.Characters.Combat
{
    public sealed class CombatController : MonoBehaviour
    {
        [Header("Unarmed / Fallback Damage")]
        [SerializeField, Min(0f)]
        private float basicAttackBaseDamage = 10f;

        [Header("Attribute Scaling")]
        [SerializeField, Min(0f)]
        private float damagePerSTR = 1f;

        [Header("Attack Speed")]
        [SerializeField, Min(0.05f)]
        private float baseAttackInterval = 1.5f;

        [SerializeField, Min(0f)]
        private float attackSpeedPerDEX = 0.02f;

        [Header("Range")]
        [SerializeField, Min(0f)]
        private float attackRange = 2f;

        [Header("Critical")]
        [SerializeField, Range(0f, 1f)]
        private float criticalChance = 0.05f;

        [SerializeField, Min(1f)]
        private float criticalMultiplier = 1.5f;

        [Header("References")]
        [SerializeField]
        private ActionStateController actionState;

        [SerializeField]
        private TargetingController targeting;

        [SerializeField]
        private CharacterStatsController stats;

        [SerializeField]
        private EquipmentController equipment;

        private readonly Dictionary<
            CombatModifierType,
            List<CombatModifier>>
            modifiersByType = new();

        private readonly Dictionary<string, CombatModifier>
            modifiersById =
                new(StringComparer.Ordinal);

        private CharacterContext context;
        private float nextAttackTime;
        private bool modifiersInitialized;

        public event Action BasicAttackStarted;

        public event Action<DamageResult> BasicAttackHit;

        public event Action CombatValuesChanged;

        public float BasicAttackDamage
        {
            get
            {
                CalculateBasicAttackDamageRange(
                    out float minimumDamage,
                    out float maximumDamage);

                return (
                    minimumDamage +
                    maximumDamage) *
                    0.5f;
            }
        }

        public float MinimumBasicAttackDamage
        {
            get
            {
                CalculateBasicAttackDamageRange(
                    out float minimumDamage,
                    out _);

                return minimumDamage;
            }
        }

        public float MaximumBasicAttackDamage
        {
            get
            {
                CalculateBasicAttackDamageRange(
                    out _,
                    out float maximumDamage);

                return maximumDamage;
            }
        }

        public float CurrentAttackInterval =>
            CalculateAttackInterval();

        public float CurrentCriticalChance =>
            CalculateCriticalChance();

        public float CurrentCriticalMultiplier =>
            CalculateCriticalMultiplier();

        public float AttackRange =>
            attackRange;

        public float RemainingAttackCooldown =>
            Mathf.Max(
                0f,
                nextAttackTime - Time.time);

        public ItemInstance EquippedWeapon =>
            equipment != null
                ? equipment.GetEquippedInstance(
                    EquipmentSlot.Weapon)
                : null;

        private void Awake()
        {
            CacheReferences();
            EnsureModifiersInitialized();
        }

        public bool TryBasicAttack(
            CharacterContext target = null)
        {
            CacheReferences();

            target ??=
                targeting?.CurrentTarget;

            if (!CanAttack(target))
            {
                return false;
            }

            nextAttackTime =
                Time.time +
                CalculateAttackInterval();

            BasicAttackStarted?.Invoke();

            context?.Events?.
                RaiseBasicAttackStarted();

            DamageRequest request =
                CreateBasicAttackRequest(target);

            DamageResult damageResult =
                DamageCalculator.Calculate(request);

            DamageApplicationResult applicationResult =
                target.Health.ApplyDamage(
                    damageResult);

            BasicAttackHit?.Invoke(
                damageResult);

            context?.Events?.
                RaiseBasicAttackHit(
                    damageResult);

            if (applicationResult != null)
            {
                context?.Events?.
                    RaiseDamageDealt(
                        damageResult);
            }

            if (applicationResult != null &&
                applicationResult.DamagedHealth)
            {
                target.Events?.
                    RaiseDamageTaken(
                        damageResult);
            }

            if (damageResult.WasCritical)
            {
                context?.Events?.
                    RaiseCriticalHit(
                        damageResult);
            }

            Debug.Log(
                $"[COMBAT] Ataque básico causou " +
                $"{damageResult.FinalAmount:0.##} de dano. " +
                $"Faixa atual: " +
                $"{MinimumBasicAttackDamage:0.##}–" +
                $"{MaximumBasicAttackDamage:0.##}. " +
                $"Crítico: " +
                $"{(damageResult.WasCritical ? "SIM" : "NÃO")}.",
                this);

            return true;
        }

        public bool CanAttack(
            CharacterContext target)
        {
            CacheReferences();

            if (context == null ||
                context.Health == null ||
                context.Health.IsDead)
            {
                return false;
            }

            if (actionState != null &&
                !actionState.CanAttack)
            {
                return false;
            }

            if (Time.time < nextAttackTime)
            {
                return false;
            }

            if (target == null ||
                ReferenceEquals(target, context))
            {
                return false;
            }

            if (target.Health == null ||
                target.Health.IsDead)
            {
                return false;
            }

            if (targeting != null &&
                !targeting.IsValidTarget(target))
            {
                return false;
            }

            return IsTargetInRange(target);
        }

        public bool IsTargetInRange(
            CharacterContext target)
        {
            if (target == null)
            {
                return false;
            }

            float distance =
                Vector3.Distance(
                    transform.position,
                    target.transform.position);

            return distance <= attackRange;
        }

        public bool AddModifier(
            CombatModifier modifier)
        {
            if (modifier == null)
            {
                return false;
            }

            EnsureModifiersInitialized();

            if (modifiersById.ContainsKey(
                    modifier.Id))
            {
                Debug.LogWarning(
                    $"[COMBAT] Já existe um modificador " +
                    $"com o ID '{modifier.Id}'.",
                    this);

                return false;
            }

            modifiersByType[
                    modifier.ModifierType]
                .Add(modifier);

            modifiersById.Add(
                modifier.Id,
                modifier);

            CombatValuesChanged?.Invoke();

            return true;
        }

        public bool RemoveModifier(
            string modifierId)
        {
            if (string.IsNullOrWhiteSpace(
                    modifierId))
            {
                return false;
            }

            EnsureModifiersInitialized();

            if (!modifiersById.TryGetValue(
                    modifierId,
                    out CombatModifier modifier))
            {
                return false;
            }

            modifiersById.Remove(
                modifierId);

            bool removed =
                modifiersByType[
                        modifier.ModifierType]
                    .Remove(modifier);

            if (removed)
            {
                CombatValuesChanged?.Invoke();
            }

            return removed;
        }

        public int RemoveModifiersFromSource(
            object source)
        {
            if (source == null)
            {
                return 0;
            }

            EnsureModifiersInitialized();

            int removedCount = 0;

            foreach (
                KeyValuePair<
                    CombatModifierType,
                    List<CombatModifier>> pair
                in modifiersByType)
            {
                List<CombatModifier> modifiers =
                    pair.Value;

                for (int index =
                         modifiers.Count - 1;
                     index >= 0;
                     index--)
                {
                    CombatModifier modifier =
                        modifiers[index];

                    if (!Equals(
                            modifier.Source,
                            source))
                    {
                        continue;
                    }

                    modifiers.RemoveAt(index);

                    modifiersById.Remove(
                        modifier.Id);

                    removedCount++;
                }
            }

            if (removedCount > 0)
            {
                CombatValuesChanged?.Invoke();
            }

            return removedCount;
        }

        public bool HasModifier(
            string modifierId)
        {
            if (string.IsNullOrWhiteSpace(
                    modifierId))
            {
                return false;
            }

            EnsureModifiersInitialized();

            return modifiersById.ContainsKey(
                modifierId);
        }

        private DamageRequest CreateBasicAttackRequest(
            CharacterContext target)
        {
            return new DamageRequest
            {
                Source = context,
                Target = target,

                BaseValue =
                    RollBasicAttackDamage(),

                Type =
                    DamageType.Physical,

                Tags =
                    DamageTag.BasicAttack |
                    DamageTag.SingleTarget,

                Scaling = 1f,

                CanCrit = true,

                CriticalChance =
                    CalculateCriticalChance(),

                CriticalMultiplier =
                    CalculateCriticalMultiplier(),

                Origin =
                    DamageOrigin.BasicAttack,

                OriginObject =
                    EquippedWeapon != null
                        ? EquippedWeapon
                        : this
            };
        }

        private float RollBasicAttackDamage()
        {
            CalculateBasicAttackDamageRange(
                out float minimumDamage,
                out float maximumDamage);

            if (Mathf.Approximately(
                    minimumDamage,
                    maximumDamage))
            {
                return minimumDamage;
            }

            return UnityEngine.Random.Range(
                minimumDamage,
                maximumDamage);
        }

        private void CalculateBasicAttackDamageRange(
            out float minimumDamage,
            out float maximumDamage)
        {
            GetBaseWeaponDamageRange(
                out float baseMinimumDamage,
                out float baseMaximumDamage);

            float strengthDamage =
                CalculateStrengthDamageBonus();

            float rawMinimum =
                baseMinimumDamage +
                strengthDamage;

            float rawMaximum =
                baseMaximumDamage +
                strengthDamage;

            minimumDamage =
                ApplyModifiers(
                    CombatModifierType.BasicAttackDamage,
                    rawMinimum);

            maximumDamage =
                ApplyModifiers(
                    CombatModifierType.BasicAttackDamage,
                    rawMaximum);

            minimumDamage =
                Mathf.Max(
                    0f,
                    minimumDamage);

            maximumDamage =
                Mathf.Max(
                    minimumDamage,
                    maximumDamage);
        }

        private void GetBaseWeaponDamageRange(
            out float minimumDamage,
            out float maximumDamage)
        {
            minimumDamage =
                basicAttackBaseDamage;

            maximumDamage =
                basicAttackBaseDamage;

            ItemInstance weaponInstance =
                EquippedWeapon;

            if (weaponInstance?.Item
                is not WeaponItemData weaponData)
            {
                return;
            }

            Vector2 finalWeaponDamage =
                weaponData.GetFinalDamageRange(
                    weaponInstance.Rarity);

            minimumDamage =
                finalWeaponDamage.x;

            maximumDamage =
                finalWeaponDamage.y;
        }

        private float CalculateStrengthDamageBonus()
        {
            float finalSTR =
                stats != null
                    ? stats.GetFinalValue(
                        CharacterStat.STR)
                    : 0f;

            return Mathf.Max(
                0f,
                finalSTR *
                damagePerSTR);
        }

        private float CalculateAttackInterval()
        {
            float finalDEX =
                stats != null
                    ? stats.GetFinalValue(
                        CharacterStat.DEX)
                    : 0f;

            float attackSpeedMultiplier =
                1f +
                Mathf.Max(
                    0f,
                    finalDEX) *
                attackSpeedPerDEX;

            GetModifierTotals(
                CombatModifierType.AttackSpeed,
                out float flatValue,
                out float additivePercent,
                out float multiplicativeFactor);

            attackSpeedMultiplier +=
                flatValue;

            attackSpeedMultiplier *=
                1f +
                additivePercent;

            attackSpeedMultiplier *=
                multiplicativeFactor;

            attackSpeedMultiplier =
                Mathf.Max(
                    0.1f,
                    attackSpeedMultiplier);

            return baseAttackInterval /
                   attackSpeedMultiplier;
        }

        private float CalculateCriticalChance()
        {
            float finalChance =
                ApplyModifiers(
                    CombatModifierType.CriticalChance,
                    criticalChance);

            return Mathf.Clamp01(
                finalChance);
        }

        private float CalculateCriticalMultiplier()
        {
            float finalMultiplier =
                ApplyModifiers(
                    CombatModifierType.CriticalMultiplier,
                    criticalMultiplier);

            return Mathf.Max(
                1f,
                finalMultiplier);
        }

        private float ApplyModifiers(
            CombatModifierType modifierType,
            float baseValue)
        {
            GetModifierTotals(
                modifierType,
                out float flatValue,
                out float additivePercent,
                out float multiplicativeFactor);

            float finalValue =
                baseValue +
                flatValue;

            finalValue *=
                1f +
                additivePercent;

            finalValue *=
                multiplicativeFactor;

            return finalValue;
        }

        private void GetModifierTotals(
            CombatModifierType modifierType,
            out float flatValue,
            out float additivePercent,
            out float multiplicativeFactor)
        {
            EnsureModifiersInitialized();

            flatValue = 0f;
            additivePercent = 0f;
            multiplicativeFactor = 1f;

            List<CombatModifier> modifiers =
                modifiersByType[modifierType];

            for (int index = 0;
                 index < modifiers.Count;
                 index++)
            {
                CombatModifier modifier =
                    modifiers[index];

                flatValue +=
                    modifier.FlatValue;

                additivePercent +=
                    modifier.AdditivePercent;

                multiplicativeFactor *=
                    1f +
                    modifier.MultiplicativePercent;
            }
        }

        private void EnsureModifiersInitialized()
        {
            if (modifiersInitialized)
            {
                return;
            }

            foreach (
                CombatModifierType modifierType
                in Enum.GetValues(
                    typeof(CombatModifierType)))
            {
                modifiersByType[modifierType] =
                    new List<CombatModifier>();
            }

            modifiersInitialized = true;
        }

        private void CacheReferences()
        {
            context ??=
                GetComponent<CharacterContext>();

            actionState ??=
                GetComponent<ActionStateController>();

            targeting ??=
                GetComponent<TargetingController>();

            stats ??=
                GetComponent<CharacterStatsController>();

            equipment ??=
                GetComponent<EquipmentController>();
        }

        private void OnValidate()
        {
            basicAttackBaseDamage =
                Mathf.Max(
                    0f,
                    basicAttackBaseDamage);

            damagePerSTR =
                Mathf.Max(
                    0f,
                    damagePerSTR);

            baseAttackInterval =
                Mathf.Max(
                    0.05f,
                    baseAttackInterval);

            attackSpeedPerDEX =
                Mathf.Max(
                    0f,
                    attackSpeedPerDEX);

            attackRange =
                Mathf.Max(
                    0f,
                    attackRange);

            criticalChance =
                Mathf.Clamp01(
                    criticalChance);

            criticalMultiplier =
                Mathf.Max(
                    1f,
                    criticalMultiplier);
        }
    }
}