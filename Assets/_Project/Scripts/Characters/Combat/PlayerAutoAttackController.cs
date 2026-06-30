using System;
using Riftborn.Characters.Core;
using UnityEngine;

namespace Riftborn.Characters.Combat
{
    public enum AutoAttackDecision
    {
        None = 0,
        Disabled = 1,
        NoTarget = 2,
        InvalidTarget = 3,
        CombatUnavailable = 4,
        OutOfRange = 5,
        ReadyToAttack = 6
    }
    [Serializable]
    public sealed class PlayerAutoAttackController
    {
        [Header("Auto Attack")]
        [SerializeField]
        private bool autoAttackEnabled = true;

        [Tooltip(
            "Remove automaticamente o alvo quando ele " +
            "morrer ou deixar de ser válido.")]
        [SerializeField]
        private bool clearInvalidTarget = true;

        [Header("Runtime Debug")]
        [SerializeField]
        private CharacterContext currentAutoAttackTarget;

        [SerializeField]
        private bool waitingForRange;

        [SerializeField]
        private AutoAttackDecision currentDecision;

        [SerializeField]
        private bool showDebugLogs = true;

        private CharacterContext previousTarget;
        private bool warnedAboutUnavailableCombat;

        public event Action<bool> StopRequested;

        public bool AutoAttackEnabled =>
            autoAttackEnabled;

        public bool ClearInvalidTarget =>
            clearInvalidTarget;

        public CharacterContext CurrentAutoAttackTarget =>
            currentAutoAttackTarget;

        public bool WaitingForRange =>
            waitingForRange;

        public AutoAttackDecision CurrentDecision =>
            currentDecision;

        public void Initialize()
        {
            ResetRuntimeState();
        }

        public void Disable()
        {
            ResetRuntimeState();
        }

        public AutoAttackDecision Evaluate(
            CharacterContext selectedTarget,
            bool targetIsValid,
            bool combatAvailable,
            bool targetInRange,
            float distance,
            float attackRange)
        {
            if (!autoAttackEnabled)
            {
                ResetRuntimeState();
                return SetDecision(
                    AutoAttackDecision.Disabled);
            }

            HandleTargetChange(
                selectedTarget);

            if (selectedTarget == null)
            {
                currentAutoAttackTarget = null;
                waitingForRange = false;

                return SetDecision(
                    AutoAttackDecision.NoTarget);
            }

            if (!targetIsValid)
            {
                if (showDebugLogs)
                {
                    Debug.Log(
                        $"[AUTO ATTACK] O alvo " +
                        $"'{selectedTarget.name}' " +
                        "não é mais válido.", null);
                }

                currentAutoAttackTarget = null;
                waitingForRange = false;

                return SetDecision(
                    AutoAttackDecision.InvalidTarget);
            }

            currentAutoAttackTarget =
                selectedTarget;

            if (!combatAvailable)
            {
                if (!warnedAboutUnavailableCombat)
                {
                    Debug.LogWarning(
                        "[AUTO ATTACK] O sistema de combate " +
                        "do player não está disponível.", null);

                    warnedAboutUnavailableCombat = true;
                }

                return SetDecision(
                    AutoAttackDecision.CombatUnavailable);
            }

            warnedAboutUnavailableCombat = false;

            if (!targetInRange)
            {
                if (!waitingForRange)
                {
                    waitingForRange = true;

                    if (showDebugLogs)
                    {
                        Debug.Log(
                            $"[AUTO ATTACK] " +
                            $"{selectedTarget.name} está fora " +
                            $"do alcance. Distância: " +
                            $"{distance:0.##} | Alcance: " +
                            $"{attackRange:0.##}.", null);
                    }
                }

                return SetDecision(
                    AutoAttackDecision.OutOfRange);
            }

            if (waitingForRange &&
                showDebugLogs)
            {
                Debug.Log(
                    $"[AUTO ATTACK] " +
                    $"{selectedTarget.name} entrou no alcance.", null);
            }

            waitingForRange = false;

            return SetDecision(
                AutoAttackDecision.ReadyToAttack);
        }

        public void NotifyTargetChanged(
            CharacterContext newTarget)
        {
            HandleTargetChange(
                newTarget);
        }

        public void ReportAttackAttempt(
            bool attackExecuted)
        {
            /*
             * O CombatController controla cooldown e permissões.
             * Este método existe para o PlayerController devolver
             * o resultado da tentativa ao módulo de auto attack.
             */
            if (!attackExecuted)
            {
                return;
            }
        }

        public void SetAutoAttackEnabled(
            bool enabled)
        {
            autoAttackEnabled =
                enabled;

            if (!autoAttackEnabled)
            {
                ResetRuntimeState();
            }

            if (showDebugLogs)
            {
                Debug.Log(
                    $"[AUTO ATTACK] Auto ataque: " +
                    $"{(autoAttackEnabled ? "ATIVO" : "INATIVO")}.", null);
            }
        }

        public void StopAutoAttack(
            bool clearSelection)
        {
            ResetRuntimeState();

            StopRequested?.Invoke(
                clearSelection);

            if (showDebugLogs)
            {
                Debug.Log(
                    "[AUTO ATTACK] Ataque automático interrompido.", null);
            }
        }

        public void ResetRuntimeState()
        {
            previousTarget = null;
            currentAutoAttackTarget = null;
            waitingForRange = false;
            warnedAboutUnavailableCombat = false;
            currentDecision = AutoAttackDecision.None;
        }

        private void HandleTargetChange(
            CharacterContext selectedTarget)
        {
            if (ReferenceEquals(
                    selectedTarget,
                    previousTarget))
            {
                return;
            }

            previousTarget =
                selectedTarget;

            currentAutoAttackTarget =
                selectedTarget;

            waitingForRange = false;

            if (!showDebugLogs)
            {
                return;
            }

            if (selectedTarget == null)
            {
                Debug.Log(
                    "[AUTO ATTACK] Nenhum alvo selecionado.", null);

                return;
            }

            Debug.Log(
                $"[AUTO ATTACK] Novo alvo: " +
                $"{selectedTarget.name}.", null);
        }

        private AutoAttackDecision SetDecision(
            AutoAttackDecision decision)
        {
            currentDecision =
                decision;

            return decision;
        }
    }
}
