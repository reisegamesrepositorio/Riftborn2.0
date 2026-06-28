using UnityEngine;
namespace Riftborn.Items
{
    public class ItemData : ScriptableObject
    {
        [SerializeField] private string itemId, displayName;
        [SerializeField] private int maxStack = 1;
        public string ItemId => itemId;
        public string DisplayName => displayName;
        public int MaxStack => Mathf.Max(1, maxStack);
    }
}
