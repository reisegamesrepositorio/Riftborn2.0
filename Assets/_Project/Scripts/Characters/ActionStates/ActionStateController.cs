using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
namespace Riftborn.Characters.ActionStates
{
    [Flags]
    public enum ActionPermission { None = 0, Move = 1 << 0, Attack = 1 << 1, Cast = 1 << 2, UseItems = 1 << 3, Interact = 1 << 4 }
    public sealed class ActionStateController : MonoBehaviour
    {
        private readonly Dictionary<object, ActionPermission> blockersBySource = new();
        public event Action<ActionPermission> PermissionsChanged;
        public bool CanMove => !IsBlocked(ActionPermission.Move);
        public bool CanAttack => !IsBlocked(ActionPermission.Attack);
        public bool CanCast => !IsBlocked(ActionPermission.Cast);
        public bool CanUseItems => !IsBlocked(ActionPermission.UseItems);
        public bool CanInteract => !IsBlocked(ActionPermission.Interact);
        public void AddBlock(object source, ActionPermission permissions) { if (source == null || permissions == ActionPermission.None) return; blockersBySource[source] = permissions; PermissionsChanged?.Invoke(GetBlockedPermissions()); }
        public bool RemoveBlock(object source) { if (source == null || !blockersBySource.Remove(source)) return false; PermissionsChanged?.Invoke(GetBlockedPermissions()); return true; }
        public bool IsBlocked(ActionPermission permission) => blockersBySource.Values.Any(blocked => (blocked & permission) != 0);
        public ActionPermission GetBlockedPermissions() { ActionPermission blocked = ActionPermission.None; foreach (var permissions in blockersBySource.Values) blocked |= permissions; return blocked; }
    }
}
