using UnityEngine;

namespace Riftborn.Items
{
    [CreateAssetMenu(
        fileName = "NewItem",
        menuName = "Riftborn/Items/Item")]
    public class ItemData : ScriptableObject
    {
        [Header("Identity")]
        [SerializeField]
        private string itemId;

        [SerializeField]
        private string displayName;

        [SerializeField, TextArea]
        private string description;

        [Header("Classification")]
        [SerializeField]
        private ItemType itemType =
            ItemType.Miscellaneous;

        [Header("Presentation")]
        [SerializeField]
        private Sprite icon;

        [Header("Stacking")]
        [SerializeField, Min(1)]
        private int maxStack = 1;

        public string ItemId =>
            itemId;

        public string DisplayName =>
            string.IsNullOrWhiteSpace(displayName)
                ? name
                : displayName;

        public string Description =>
            description;

        public ItemType ItemType =>
            itemType;

        public Sprite Icon =>
            icon;

        public int MaxStack =>
            Mathf.Max(
                1,
                maxStack);

        public bool IsStackable =>
            MaxStack > 1;

        protected virtual void OnValidate()
        {
            maxStack =
                Mathf.Max(
                    1,
                    maxStack);
        }
    }
}