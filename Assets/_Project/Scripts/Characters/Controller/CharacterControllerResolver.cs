using Riftborn.Characters.Core;
using Riftborn.Damage;
using UnityEngine;

namespace Riftborn.Characters.Controllers
{
    public static class CharacterControllerResolver
    {
        public static bool TryResolve(
            CharacterContext character,
            out ICharacterController controller)
        {
            controller = null;

            if (character == null)
            {
                return false;
            }

            if (TryFindController(
                    character.GetComponents<MonoBehaviour>(),
                    out controller))
            {
                return true;
            }

            if (TryFindController(
                    character.GetComponentsInParent<MonoBehaviour>(
                        includeInactive: true),
                    out controller))
            {
                return true;
            }

            return TryFindController(
                character.GetComponentsInChildren<MonoBehaviour>(
                    includeInactive: true),
                out controller);
        }

        public static DamageApplicationResult RouteDamage(
            DamageResult result)
        {
            DamageRequest request =
                result?.Request;

            if (request == null ||
                request.Target == null)
            {
                return null;
            }

            if (request.Source != null &&
                TryResolve(
                    request.Source,
                    out ICharacterController sourceController))
            {
                return sourceController.ProcessOutgoingDamage(
                    result);
            }

            return DeliverToTarget(
                result);
        }

        public static DamageApplicationResult DeliverToTarget(
            DamageResult result)
        {
            CharacterContext target =
                result?
                    .Request?
                    .Target;

            if (target == null)
            {
                return null;
            }

            if (!TryResolve(
                    target,
                    out ICharacterController targetController))
            {
                Debug.LogError(
                    $"[CHARACTER ROUTER] '{target.name}' não possui " +
                    "PlayerController ou EnemyController para receber dano.",
                    target);

                return null;
            }

            return targetController.ReceiveDamage(
                result);
        }

        private static bool TryFindController(
            MonoBehaviour[] behaviours,
            out ICharacterController controller)
        {
            controller = null;

            if (behaviours == null)
            {
                return false;
            }

            for (int index = 0;
                 index < behaviours.Length;
                 index++)
            {
                MonoBehaviour behaviour =
                    behaviours[index];

                if (behaviour is not
                    ICharacterController candidate)
                {
                    continue;
                }

                controller = candidate;
                return true;
            }

            return false;
        }
    }
}
