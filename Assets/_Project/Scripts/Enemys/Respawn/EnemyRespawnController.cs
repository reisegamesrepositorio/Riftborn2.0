using System;
using System.Collections;
using UnityEngine;

namespace Riftborn.Enemies.Respawn
{
    public readonly struct EnemyRespawnResult
    {
        public EnemyRespawnResult(
            Vector3 position,
            Quaternion rotation,
            float healthPercent)
        {
            Position = position;
            Rotation = rotation;
            HealthPercent = healthPercent;
        }

        public Vector3 Position { get; }

        public Quaternion Rotation { get; }

        public float HealthPercent { get; }
    }

    [DisallowMultipleComponent]
    public sealed class EnemyRespawnController : MonoBehaviour
    {
        [Header("Respawn")]
        [SerializeField, Min(0f)]
        private float respawnDelay = 5f;

        [SerializeField, Range(0.01f, 1f)]
        private float respawnHealthPercent = 1f;

        [Tooltip(
            "Se estiver vazio, utiliza a posição inicial do inimigo. " +
            "Não coloque o próprio inimigo neste campo.")]
        [SerializeField]
        private Transform respawnPoint;

        [SerializeField]
        private bool restoreSpawnRotation = true;

        [SerializeField]
        private bool clearStatusEffectsOnRespawn = true;

        [SerializeField]
        private bool clearActionBlocksOnRespawn = true;

        [Header("Ground Safety")]
        [Tooltip(
            "Procura o chão abaixo do ponto de respawn antes " +
            "de informar o EnemyController.")]
        [SerializeField]
        private bool snapRespawnToGround = true;

        [Tooltip(
            "Configure preferencialmente apenas a Layer utilizada pelo chão.")]
        [SerializeField]
        private LayerMask groundMask = ~0;

        [SerializeField, Min(0.1f)]
        private float groundProbeHeight = 5f;

        [SerializeField, Min(0.1f)]
        private float groundProbeDepth = 20f;

        [Tooltip(
            "Pequena distância entre a base do CharacterController e o chão.")]
        [SerializeField, Min(0f)]
        private float groundClearance = 0.05f;

        [Tooltip(
            "Se nenhum chão for encontrado, continua tentando em vez " +
            "de devolver uma posição insegura.")]
        [SerializeField]
        private bool requireGroundForRespawn = true;

        [SerializeField, Min(0.1f)]
        private float groundRetryInterval = 1f;

        [Header("References")]
        [SerializeField]
        private CharacterController characterController;

        [Header("Runtime Debug")]
        [SerializeField]
        private bool isWaitingForRespawn;

        [SerializeField]
        private bool showDebugLogs = true;

        private Vector3 initialSpawnPosition;
        private Quaternion initialSpawnRotation;
        private Coroutine respawnCoroutine;

        public event Action<EnemyRespawnResult>
            RespawnReady;

        public bool IsWaitingForRespawn =>
            isWaitingForRespawn;

        public bool ClearStatusEffectsOnRespawn =>
            clearStatusEffectsOnRespawn;

        public bool ClearActionBlocksOnRespawn =>
            clearActionBlocksOnRespawn;

        public Vector3 InitialSpawnPosition =>
            initialSpawnPosition;

        public Quaternion InitialSpawnRotation =>
            initialSpawnRotation;

        private void Awake()
        {
            CacheReferences();
            CacheInitialSpawn();
        }

        private void Reset()
        {
            CacheReferences();
        }

        private void OnDisable()
        {
            CancelRespawn();
        }

        private void OnValidate()
        {
            respawnDelay =
                Mathf.Max(
                    0f,
                    respawnDelay);

            respawnHealthPercent =
                Mathf.Clamp(
                    respawnHealthPercent,
                    0.01f,
                    1f);

            groundProbeHeight =
                Mathf.Max(
                    0.1f,
                    groundProbeHeight);

            groundProbeDepth =
                Mathf.Max(
                    0.1f,
                    groundProbeDepth);

            groundClearance =
                Mathf.Max(
                    0f,
                    groundClearance);

            groundRetryInterval =
                Mathf.Max(
                    0.1f,
                    groundRetryInterval);
        }

        public bool BeginRespawn()
        {
            if (isWaitingForRespawn)
            {
                return false;
            }

            isWaitingForRespawn =
                true;

            if (showDebugLogs)
            {
                Debug.Log(
                    $"[ENEMY RESPAWN] {name}: preparação iniciada. " +
                    $"Tempo: {respawnDelay:0.##}s.",
                    this);
            }

            respawnCoroutine =
                StartCoroutine(
                    RespawnRoutine());

            return true;
        }

        public void CancelRespawn()
        {
            if (respawnCoroutine != null)
            {
                StopCoroutine(
                    respawnCoroutine);

                respawnCoroutine =
                    null;
            }

            isWaitingForRespawn =
                false;
        }

        public void SetInitialSpawn(
            Vector3 position,
            Quaternion rotation)
        {
            initialSpawnPosition =
                position;

            initialSpawnRotation =
                rotation;
        }

        private IEnumerator RespawnRoutine()
        {
            if (respawnDelay > 0f)
            {
                yield return new WaitForSeconds(
                    respawnDelay);
            }

            Vector3 requestedPosition =
                ResolveRequestedPosition();

            Quaternion requestedRotation =
                ResolveRequestedRotation();

            Vector3 safePosition =
                requestedPosition;

            if (snapRespawnToGround)
            {
                bool foundGround =
                    TryGetGroundedRespawnPosition(
                        requestedPosition,
                        out safePosition);

                while (!foundGround &&
                       requireGroundForRespawn)
                {
                    Debug.LogError(
                        $"[ENEMY RESPAWN] {name} não encontrou chão " +
                        $"abaixo do ponto {requestedPosition}. " +
                        "Verifique Ground Mask e a posição do spawn. " +
                        "Nova tentativa será feita.",
                        this);

                    yield return new WaitForSeconds(
                        groundRetryInterval);

                    foundGround =
                        TryGetGroundedRespawnPosition(
                            requestedPosition,
                            out safePosition);
                }

                if (!foundGround)
                {
                    safePosition =
                        requestedPosition;

                    Debug.LogWarning(
                        $"[ENEMY RESPAWN] {name} não encontrou chão. " +
                        "A posição configurada será informada sem ajuste.",
                        this);
                }
            }

            isWaitingForRespawn =
                false;

            respawnCoroutine =
                null;

            EnemyRespawnResult result =
                new EnemyRespawnResult(
                    safePosition,
                    requestedRotation,
                    respawnHealthPercent);

            if (showDebugLogs)
            {
                Debug.Log(
                    $"[ENEMY RESPAWN] {name}: posição segura pronta | " +
                    $"Posição: {result.Position} | " +
                    $"Vida: {result.HealthPercent * 100f:0.##}%.",
                    this);
            }

            RespawnReady?.Invoke(
                result);
        }

        private bool TryGetGroundedRespawnPosition(
            Vector3 requestedPosition,
            out Vector3 groundedPosition)
        {
            groundedPosition =
                requestedPosition;

            Vector3 rayOrigin =
                requestedPosition +
                Vector3.up *
                groundProbeHeight;

            float rayDistance =
                groundProbeHeight +
                groundProbeDepth;

            RaycastHit[] hits =
                Physics.RaycastAll(
                    rayOrigin,
                    Vector3.down,
                    rayDistance,
                    groundMask,
                    QueryTriggerInteraction.Ignore);

            Array.Sort(
                hits,
                CompareHitsByDistance);

            RaycastHit groundHit =
                default;

            bool foundGround =
                false;

            for (int index = 0;
                 index < hits.Length;
                 index++)
            {
                RaycastHit candidate =
                    hits[index];

                if (candidate.collider == null)
                {
                    continue;
                }

                Transform hitTransform =
                    candidate.collider.transform;

                if (hitTransform == transform ||
                    hitTransform.IsChildOf(transform))
                {
                    continue;
                }

                groundHit =
                    candidate;

                foundGround =
                    true;

                break;
            }

            if (!foundGround)
            {
                return false;
            }

            float bottomOffset = 0f;

            if (characterController != null)
            {
                float scaleY =
                    Mathf.Abs(
                        transform.lossyScale.y);

                float localBottom =
                    characterController.center.y -
                    characterController.height *
                    0.5f;

                bottomOffset =
                    localBottom *
                    scaleY;
            }

            groundedPosition =
                new Vector3(
                    requestedPosition.x,
                    groundHit.point.y -
                    bottomOffset +
                    groundClearance,
                    requestedPosition.z);

            if (showDebugLogs)
            {
                Debug.Log(
                    $"[ENEMY RESPAWN] Chão encontrado para {name} | " +
                    $"Hit: {groundHit.collider.name} | " +
                    $"Altura: {groundHit.point.y:0.###} | " +
                    $"Posição segura: {groundedPosition}.",
                    this);
            }

            return true;
        }


        private static int CompareHitsByDistance(
            RaycastHit first,
            RaycastHit second)
        {
            return first.distance.CompareTo(
                second.distance);
        }

        private Vector3 ResolveRequestedPosition()
        {
            if (respawnPoint == null ||
                ReferenceEquals(
                    respawnPoint,
                    transform))
            {
                return initialSpawnPosition;
            }

            return respawnPoint.position;
        }

        private Quaternion ResolveRequestedRotation()
        {
            if (!restoreSpawnRotation)
            {
                return transform.rotation;
            }

            if (respawnPoint == null ||
                ReferenceEquals(
                    respawnPoint,
                    transform))
            {
                return initialSpawnRotation;
            }

            return respawnPoint.rotation;
        }

        private void CacheInitialSpawn()
        {
            initialSpawnPosition =
                transform.position;

            initialSpawnRotation =
                transform.rotation;
        }

        private void CacheReferences()
        {
            characterController ??=
                GetComponent<CharacterController>();
        }
    }
}
