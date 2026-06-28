namespace Riftborn.Damage
{
    public sealed class DamageResult
    {
        public DamageResult(DamageRequest request, float finalAmount, bool wasCritical) { Request = request; FinalAmount = finalAmount; WasCritical = wasCritical; }
        public DamageRequest Request { get; }
        public float FinalAmount { get; }
        public bool WasCritical { get; }
    }
}
