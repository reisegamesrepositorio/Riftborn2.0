using UnityEngine;
using UnityEngine.InputSystem;

namespace Riftborn.Cameras
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Camera))]
    public sealed class IsometricCameraFollow : MonoBehaviour
    {
        [Header("Target")]
        [SerializeField]
        private Transform target;

        [Header("Focus Point")]
        [Tooltip(
            "When enabled, the camera follows the target only on X/Z. " +
            "The focus height remains fixed, matching the Summoner's Rift-style setup.")]
        [SerializeField]
        private bool lockFocusHeight = true;

        [SerializeField]
        private float fixedFocusHeight = 0f;

        [SerializeField]
        private Vector3 focusOffset = Vector3.zero;

        [Header("Camera Geometry")]
        [SerializeField, Range(1f, 89f)]
        private float fieldOfView = 40f;

        [Tooltip("Downward angle measured from the horizontal.")]
        [SerializeField, Range(1f, 89f)]
        private float downwardAngle = 56f;

        [Tooltip(
            "Horizontal direction around the target. " +
            "Zero keeps the camera behind the target on negative world Z.")]
        [SerializeField]
        private float yaw = 0f;

        [Header("Two Zoom Positions")]
        [SerializeField, Min(0.1f)]
        private float nearBackDistance = 8f;

        [SerializeField, Min(0.1f)]
        private float farBackDistance = 18f;

        [SerializeField]
        private bool startZoomedOut = true;

        [SerializeField]
        private bool enableMouseWheelZoom = true;

        [Header("Smoothing")]
        [SerializeField, Min(0f)]
        private float followSmoothTime = 0.12f;

        [SerializeField, Min(0f)]
        private float zoomSmoothTime = 0.18f;

        [Header("References")]
        [SerializeField]
        private Camera controlledCamera;

        private Vector3 followVelocity;
        private float zoomVelocity;
        private float currentBackDistance;
        private float targetBackDistance;
        private bool initialized;

        public Transform Target =>
            target;

        public bool IsZoomedOut =>
            Mathf.Approximately(
                targetBackDistance,
                farBackDistance);

        public float CurrentBackDistance =>
            currentBackDistance;

        public Vector3 CurrentFocusPoint =>
            GetFocusPoint();

        private void Awake()
        {
            CacheReferences();
            Initialize();
        }

        private void OnEnable()
        {
            CacheReferences();
            Initialize();
        }

        private void LateUpdate()
        {
            if (target == null)
            {
                return;
            }

            HandleZoomInput();

            currentBackDistance =
                zoomSmoothTime <= 0f
                    ? targetBackDistance
                    : Mathf.SmoothDamp(
                        currentBackDistance,
                        targetBackDistance,
                        ref zoomVelocity,
                        zoomSmoothTime);

            Vector3 desiredPosition =
                CalculateCameraPosition(
                    GetFocusPoint(),
                    currentBackDistance);

            transform.position =
                followSmoothTime <= 0f
                    ? desiredPosition
                    : Vector3.SmoothDamp(
                        transform.position,
                        desiredPosition,
                        ref followVelocity,
                        followSmoothTime);

            transform.rotation =
                Quaternion.Euler(
                    downwardAngle,
                    yaw,
                    0f);
        }

        public void SetTarget(
            Transform newTarget,
            bool snapImmediately = true)
        {
            target =
                newTarget;

            followVelocity =
                Vector3.zero;

            if (snapImmediately)
            {
                SnapToTarget();
            }
        }

        public void SetZoomedOut(
            bool zoomedOut)
        {
            targetBackDistance =
                zoomedOut
                    ? farBackDistance
                    : nearBackDistance;
        }

        public void ToggleZoom()
        {
            SetZoomedOut(
                !IsZoomedOut);
        }

        [ContextMenu("Set Zoom: Near")]
        private void SetNearZoom()
        {
            SetZoomedOut(
                false);
        }

        [ContextMenu("Set Zoom: Far")]
        private void SetFarZoom()
        {
            SetZoomedOut(
                true);
        }

        [ContextMenu("Snap To Target")]
        public void SnapToTarget()
        {
            if (target == null)
            {
                return;
            }

            CacheReferences();
            Initialize();

            currentBackDistance =
                targetBackDistance;

            transform.position =
                CalculateCameraPosition(
                    GetFocusPoint(),
                    currentBackDistance);

            transform.rotation =
                Quaternion.Euler(
                    downwardAngle,
                    yaw,
                    0f);

            followVelocity =
                Vector3.zero;

            zoomVelocity =
                0f;
        }

        private void HandleZoomInput()
        {
            if (!enableMouseWheelZoom)
            {
                return;
            }

            Mouse mouse =
                Mouse.current;

            if (mouse == null)
            {
                return;
            }

            float scrollY =
                mouse.scroll.ReadValue().y;

            if (scrollY > 0.01f)
            {
                SetZoomedOut(
                    false);
            }
            else if (scrollY < -0.01f)
            {
                SetZoomedOut(
                    true);
            }
        }

        private Vector3 GetFocusPoint()
        {
            if (target == null)
            {
                return Vector3.zero;
            }

            Vector3 focusPoint =
                target.position +
                focusOffset;

            if (lockFocusHeight)
            {
                focusPoint.y =
                    fixedFocusHeight +
                    focusOffset.y;
            }

            return focusPoint;
        }

        private Vector3 CalculateCameraPosition(
            Vector3 focusPoint,
            float backDistance)
        {
            float height =
                backDistance *
                Mathf.Tan(
                    downwardAngle *
                    Mathf.Deg2Rad);

            Vector3 backwardDirection =
                Quaternion.Euler(
                    0f,
                    yaw,
                    0f) *
                Vector3.back;

            return focusPoint +
                   backwardDirection *
                   backDistance +
                   Vector3.up *
                   height;
        }

        private void Initialize()
        {
            if (initialized)
            {
                ApplyCameraSettings();
                return;
            }

            targetBackDistance =
                startZoomedOut
                    ? farBackDistance
                    : nearBackDistance;

            currentBackDistance =
                targetBackDistance;

            initialized =
                true;

            ApplyCameraSettings();
        }

        private void ApplyCameraSettings()
        {
            if (controlledCamera == null)
            {
                return;
            }

            controlledCamera.orthographic =
                false;

            controlledCamera.fieldOfView =
                fieldOfView;
        }

        private void CacheReferences()
        {
            controlledCamera ??=
                GetComponent<Camera>();
        }

        private void OnValidate()
        {
            fieldOfView =
                Mathf.Clamp(
                    fieldOfView,
                    1f,
                    179f);

            downwardAngle =
                Mathf.Clamp(
                    downwardAngle,
                    1f,
                    89f);

            nearBackDistance =
                Mathf.Max(
                    0.1f,
                    nearBackDistance);

            farBackDistance =
                Mathf.Max(
                    nearBackDistance,
                    farBackDistance);

            followSmoothTime =
                Mathf.Max(
                    0f,
                    followSmoothTime);

            zoomSmoothTime =
                Mathf.Max(
                    0f,
                    zoomSmoothTime);

            CacheReferences();
            ApplyCameraSettings();
        }
    }
}
