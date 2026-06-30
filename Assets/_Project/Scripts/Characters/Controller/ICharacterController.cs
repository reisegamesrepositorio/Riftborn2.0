using Riftborn.Characters.Core;
using Riftborn.Damage;

namespace Riftborn.Characters.Controllers
{
    public interface ICharacterController
    {
        CharacterContext ControlledCharacter { get; }

        bool IsAlive { get; }

        DamageApplicationResult ReceiveDamage(
            DamageResult result);

        DamageApplicationResult ProcessOutgoingDamage(
            DamageResult result);
    }
}
