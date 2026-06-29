using Riftborn.Characters.Core;
using Riftborn.Characters.Health;
using UnityEngine;
using UnityEngine.UI;

namespace Riftborn.UI.World
{
    [RequireComponent(typeof(CanvasGroup))]
    public sealed class WorldHealthBar : MonoBehaviour
    {
        [Header("Character")]
        [SerializeField]
        private CharacterContext character;

        [Header("UI")]
        [SerializeField]
        private Image fillImage;

        [Header("Camera")]
        [SerializeField]
        private Camera worldCamera;

        [SerializeField]
        private bool faceCamera = true;

        [Header("Visibility")]
        [SerializeField]
        private bool hideWhenDead = true;

        private HealthController health;
        private CanvasGroup canvasGroup;
        private bool isSubscribed;

        private void Awake()
        {
            CacheReferences();
        }

        private void OnEnable()
        {
            CacheReferences();
            SubscribeToHealth();
            Refresh();
        }

        private void OnDisable()
        {
            UnsubscribeFromHealth();
        }

        private void LateUpdate()
        {
            FaceCamera();
        }

        private void CacheReferences()
        {
            character ??=
                GetComponentInParent<CharacterContext>();

            health =
                character != null
                    ? character.Health
                    : null;

            canvasGroup ??=
                GetComponent<CanvasGroup>();

            worldCamera ??=
                Camera.main;
        }

        private void SubscribeToHealth()
        {
            if (health == null || isSubscribed)
            {
                return;
            }

            health.HealthChanged +=
                HandleHealthChanged;

            health.Died +=
                HandleDied;

            health.Revived +=
                HandleRevived;

            isSubscribed = true;
        }

        private void UnsubscribeFromHealth()
        {
            if (health == null || !isSubscribed)
            {
                return;
            }

            health.HealthChanged -=
                HandleHealthChanged;

            health.Died -=
                HandleDied;

            health.Revived -=
                HandleRevived;

            isSubscribed = false;
        }

        private void HandleHealthChanged(
            HealthChangedEventArgs eventArgs)
        {
            Refresh();
        }

        private void HandleDied()
        {
            Refresh();
        }

        private void HandleRevived()
        {
            Refresh();
        }

        private void Refresh()
        {
            if (health == null)
            {
                SetVisible(false);
                return;
            }

            if (fillImage != null)
            {
                fillImage.fillAmount =
                    Mathf.Clamp01(
                        health.HealthPercentage);
            }

            bool shouldBeVisible =
                !hideWhenDead ||
                !health.IsDead;

            SetVisible(shouldBeVisible);
        }

        private void SetVisible(bool visible)
        {
            if (canvasGroup == null)
            {
                return;
            }

            canvasGroup.alpha =
                visible ? 1f : 0f;

            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
        }

        private void FaceCamera()
        {
            if (!faceCamera)
            {
                return;
            }

            if (worldCamera == null)
            {
                worldCamera = Camera.main;
            }

            if (worldCamera == null)
            {
                return;
            }

            transform.rotation =
                worldCamera.transform.rotation;
        }

        private void OnDestroy()
        {
            UnsubscribeFromHealth();
        }
    }
}