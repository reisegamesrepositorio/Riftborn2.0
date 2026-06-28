using Riftborn.Characters.ActionStates;
using Riftborn.Characters.Core;
using Riftborn.Characters.Health;
using Riftborn.Damage;
namespace Riftborn.Characters.StatusEffects
{
    public sealed class SleepEffect : StatusEffectBase, IRemoveOnDamage
    {
        private StatusEffectController controller;
        private HealthController subscribedHealth;
        public SleepEffect(CharacterContext source, CharacterContext target, float duration = 4f) : base("sleep", source, target, duration, 1, StatusEffectTag.Debuff | StatusEffectTag.CrowdControl | StatusEffectTag.HardControl | StatusEffectTag.Sleep) { }
        public override void OnApply(StatusEffectController controller)
        {
            this.controller = controller;
            Target?.ActionState?.AddBlock(this, ActionPermission.Move | ActionPermission.Attack | ActionPermission.Cast);
            subscribedHealth = Target?.Health ?? controller.GetComponent<HealthController>();
            if (subscribedHealth != null) subscribedHealth.DamageTaken += HandleDamageTaken;
            Target?.Events?.RaiseControlApplied(this);
        }
        public override void OnRemove(StatusEffectController controller)
        {
            if (subscribedHealth != null) subscribedHealth.DamageTaken -= HandleDamageTaken;
            subscribedHealth = null;
            Target?.ActionState?.RemoveBlock(this);
            this.controller = null;
        }
        public bool ShouldRemoveOnDamage(DamageResult result) => result != null && result.FinalAmount > 0f;
        private void HandleDamageTaken(DamageResult result)
        {
            if (ShouldRemoveOnDamage(result)) controller?.Remove(this);
        }
    }
}
