using System;
using System.Collections.Generic;

namespace Riftborn.Characters.ActionStates
{
    [Flags]
    public enum ActionPermission
    {
        None = 0,

        Move = 1 << 0,
        Attack = 1 << 1,
        Cast = 1 << 2,
        UseItems = 1 << 3,
        Interact = 1 << 4,

        All =
            Move |
            Attack |
            Cast |
            UseItems |
            Interact
    }

    [Serializable]
    public sealed class ActionStateController
    {
        private Dictionary<object, ActionPermission>
            blockersBySource = new();

        private ActionPermission blockedPermissions =
            ActionPermission.None;

        public event Action<ActionPermission>
            PermissionsChanged;

        public bool CanMove =>
            !IsBlocked(ActionPermission.Move);

        public bool CanAttack =>
            !IsBlocked(ActionPermission.Attack);

        public bool CanCast =>
            !IsBlocked(ActionPermission.Cast);

        public bool CanUseItems =>
            !IsBlocked(ActionPermission.UseItems);

        public bool CanInteract =>
            !IsBlocked(ActionPermission.Interact);

        public ActionPermission BlockedPermissions =>
            blockedPermissions;

        public void Initialize()
        {
            blockersBySource ??=
                new Dictionary<object, ActionPermission>();

            RecalculateBlockedPermissions();
        }

        public bool AddBlock(
            object source,
            ActionPermission permissions)
        {
            if (source == null)
            {
                return false;
            }

            permissions &=
                ActionPermission.All;

            if (permissions ==
                ActionPermission.None)
            {
                return false;
            }

            if (blockersBySource.TryGetValue(
                    source,
                    out ActionPermission existingPermissions) &&
                existingPermissions == permissions)
            {
                return false;
            }

            blockersBySource[source] =
                permissions;

            RecalculateBlockedPermissions();

            return true;
        }

        public bool RemoveBlock(
            object source)
        {
            if (source == null ||
                !blockersBySource.Remove(source))
            {
                return false;
            }

            RecalculateBlockedPermissions();

            return true;
        }

        public bool IsBlocked(
            ActionPermission permissions)
        {
            if (permissions ==
                ActionPermission.None)
            {
                return false;
            }

            return (blockedPermissions & permissions) !=
                   ActionPermission.None;
        }

        public bool AreAllBlocked(
            ActionPermission permissions)
        {
            if (permissions ==
                ActionPermission.None)
            {
                return false;
            }

            return (blockedPermissions & permissions) ==
                   permissions;
        }

        public ActionPermission
            GetBlockedPermissions()
        {
            return blockedPermissions;
        }

        public bool HasBlockFromSource(
            object source)
        {
            return source != null &&
                   blockersBySource.ContainsKey(
                       source);
        }

        public ActionPermission GetBlockFromSource(
            object source)
        {
            if (source == null)
            {
                return ActionPermission.None;
            }

            return blockersBySource.TryGetValue(
                source,
                out ActionPermission permissions)
                    ? permissions
                    : ActionPermission.None;
        }

        public bool ClearAllBlocks()
        {
            if (blockersBySource.Count == 0)
            {
                return false;
            }

            blockersBySource.Clear();
            RecalculateBlockedPermissions();

            return true;
        }

        private void RecalculateBlockedPermissions()
        {
            blockersBySource ??=
                new Dictionary<object, ActionPermission>();

            ActionPermission previousPermissions =
                blockedPermissions;

            ActionPermission newPermissions =
                ActionPermission.None;

            foreach (
                ActionPermission permissions
                in blockersBySource.Values)
            {
                newPermissions |=
                    permissions;
            }

            blockedPermissions =
                newPermissions;

            if (previousPermissions ==
                blockedPermissions)
            {
                return;
            }

            PermissionsChanged?.Invoke(
                blockedPermissions);
        }
    }
}
