using System;
using Riftborn.Characters.Core;
using UnityEngine;

namespace Riftborn.Characters.StatusEffects
{
    [Serializable]
    public abstract class StatusEffectBase
    {
        protected StatusEffectBase(
            string id,
            CharacterContext source,
            CharacterContext target,
            float duration,
            int maxStacks,
            StatusEffectTag tags,
            object stackSource = null)
        {
            Id = string.IsNullOrWhiteSpace(id)
                ? Guid.NewGuid().ToString("N")
                : id;

            Source = source;
            Target = target;

            Duration = Mathf.Max(0f, duration);
            RemainingTime = Duration;

            MaxStacks = Mathf.Max(1, maxStacks);
            Stacks = 1;

            Tags = tags;

            /*
             * Normalmente será o próprio CharacterContext da fonte.
             *
             * Caso o efeito venha de armadilha, objeto do cenário,
             * item ou outra origem, essa origem pode ser fornecida
             * explicitamente através de stackSource.
             *
             * Se não houver fonte, usamos a própria instância para
             * impedir que efeitos ambientais diferentes sejam fundidos.
             */
            StackSource =
                stackSource ??
                (object)source ??
                this;
        }

        public string Id { get; }

        public CharacterContext Source { get; }

        public CharacterContext Target { get; }

        public object StackSource { get; }

        public float Duration { get; }

        public float RemainingTime { get; private set; }

        public int Stacks { get; private set; }

        public int MaxStacks { get; }

        public StatusEffectTag Tags { get; }

        public bool IsExpired =>
            RemainingTime <= 0f;

        /// <summary>
        /// Por padrão, efeitos somente compartilham a mesma instância
        /// quando possuem o mesmo tipo e a mesma fonte.
        /// </summary>
        public virtual bool CanStackWith(
            StatusEffectBase incoming)
        {
            if (incoming == null)
            {
                return false;
            }

            if (GetType() != incoming.GetType())
            {
                return false;
            }

            return ReferenceEquals(
                StackSource,
                incoming.StackSource);
        }

        public virtual void OnApply(
            StatusEffectController controller)
        {
        }

        /// <summary>
        /// Método antigo mantido para compatibilidade com os efeitos
        /// que já sobrescrevem esta assinatura.
        /// </summary>
        public virtual void OnReapply(
            StatusEffectBase incoming)
        {
            if (incoming == null)
            {
                return;
            }

            RefreshDuration(incoming.Duration);
            AddStacks(incoming.Stacks);
        }

        /// <summary>
        /// Nova assinatura, com acesso ao controller do alvo.
        /// Por padrão, encaminha para o método antigo.
        /// </summary>
        public virtual void OnReapply(
            StatusEffectController controller,
            StatusEffectBase incoming)
        {
            OnReapply(incoming);
        }

        public virtual void Tick(
            StatusEffectController controller,
            float deltaTime)
        {
            float safeDeltaTime =
                Mathf.Max(0f, deltaTime);

            RemainingTime =
                Mathf.Max(
                    0f,
                    RemainingTime - safeDeltaTime);
        }

        public virtual void OnRemove(
            StatusEffectController controller)
        {
        }

        protected void SetStacks(int value)
        {
            Stacks = Mathf.Clamp(
                value,
                1,
                MaxStacks);
        }

        protected void AddStacks(int amount)
        {
            if (amount <= 0)
            {
                return;
            }

            SetStacks(Stacks + amount);
        }

        protected void ConsumeStacks(int amount)
        {
            if (amount <= 0)
            {
                return;
            }

            int newValue = Stacks - amount;

            if (newValue <= 0)
            {
                Expire();
                return;
            }

            SetStacks(newValue);
        }

        /// <summary>
        /// Mantém a maior duração entre a duração restante
        /// e a nova duração recebida.
        /// </summary>
        protected void RefreshDuration(float duration)
        {
            RemainingTime =
                Mathf.Max(
                    RemainingTime,
                    Mathf.Max(0f, duration));
        }

        /// <summary>
        /// Substitui diretamente o tempo restante.
        /// </summary>
        protected void SetRemainingTime(float duration)
        {
            RemainingTime =
                Mathf.Max(0f, duration);
        }

        protected void ExtendDuration(float amount)
        {
            if (amount <= 0f)
            {
                return;
            }

            RemainingTime += amount;
        }

        protected void Expire()
        {
            RemainingTime = 0f;
        }
    }
}