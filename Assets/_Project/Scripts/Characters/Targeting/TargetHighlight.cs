using UnityEngine;

namespace Riftborn.Characters.Targeting
{
    public class TargetHighlight : MonoBehaviour
    {
        [SerializeField] private GameObject outlineObject;

        public void SetSelected(bool selected)
        {
            if (outlineObject != null)
            {
                outlineObject.SetActive(selected);
            }
        }
    }
}