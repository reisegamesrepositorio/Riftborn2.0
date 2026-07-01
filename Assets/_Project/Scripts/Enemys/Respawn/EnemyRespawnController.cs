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
    [Serializable]
    public sealed class EnemyRespawnController
    {
        [Header("Respawn")]
        [SerializeField, Min(0f)]
        private float respawnDelay = 5f;

        [SerializeField, Range(0.01f, 1f)]
        private float respawnHealthPercent = 1f;

        [Tooltip(
            "Se estiver vazio, utiliza a posi√ß√£o inicial do inimigo. " +
            "N√£o coloque o pr√≥prio inimigo neste campo.")]
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
            "Procura o ch√£o abaixo do ponto de respawn antes " +
            "de informar o EnemyController.")]
        [SerializeField]
        private bool snapRespawnToGround = true;

        [Tooltip(
            "Configure preferencialmente apenas a Layer utilizada pelo ch√£o.")]
        [SerializeField]
        private LayerMask groundMask = ~0;

        [SerializeField, Min(0.1f)]
        private float groundProbeHeight = 5f;

        [SerializeField, Min(0.1f)]
        private float groundProbeDepth = 20f;

        [Tooltip(
            "Pequena dist√¢ncia entre a base do CharacterController e o ch√£o.")]
        [SerializeField, Min(0f)]
        private float groundClearance = 0.05f;

        [Tooltip(
            "Se nenhum ch√£o for encontrado, continua tentando em vez " +
            "de devolver uma posi√ß√£o insegura.")]
        [SerializeField]
        private bool requireGroundForRespawn = true;

        [SerializeField, Min(0.1f)]
        private float groundRetryInterval = 1f;

        [Header("References")]
        [SerializeField]
        private CharacterController characterController;

        [NonSerialized]
        private Transform ownerTransform;

        [NonSerialized]
        private string ownerName;

        [Header("Runtime Debug")]
        [SerializeField]
        private bool isWaitingForRespawn;

        [SerializeField]
        private bool showDebugLogs = true;

        private Vector3 initialSpawnPosition;
        private Quaternion initialSpawnRotation;
        private float respawnTimer;

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

        public void Initialize(Transform owner, CharacterController controller)
        {
            ownerTransform = owner;
            ownerName = owner != null ? owner.name : "Enemy";
            characterController = controller != null ? controller : characterController;

            if (ownerTransform != null)
            {
                CacheInitialSpawn();
            }
        }

        public void Disable()
        {
            CancelRespawn();
        }

        public void Validate()
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
                    $"[ENEMY RESPAWN] {ownerName}: prepara√ß√£o iniciada. " +
                    $"Tempo: {respawnDelay:0.##}s.", ownerTransform);
            }

            respawnTimer =
                Mathf.Max(0f, respawnDelay);

            if (respawnTimer <= 0f)
            {
                TryCompleteRespawn();
            }

            return true;
        }

        public void Tick(float deltaTime)
        {
            if (!isWaitingForRespawn)
            {
                return;
            }

            respawnTimer -=
                Mathf.Max(0f, deltaTime);

            if (respawnTimer <= 0f)
            {
                TryCompleteRespawn();
            }
        }

        public void CancelRespawn()
        {
            respawnTimer = 0f;

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

        private void TryCompleteRespawn()
        {
            if (ownerTransform == null)
            {
                Debug.LogError(
                    $"[ENEMY RESPAWN] {ownerName} tentou respawn antes de Initialize.");

                CancelRespawn();
                return;
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

                if (!foundGround &&
                    requireGroundForRespawn)
                {
                    respawnTimer =
                        Mathf.Max(
                            0.01f,
                            groundRetryInterval);

                    Debug.LogError(
                        $"[ENEMY RESPAWN] {ownerName} n„o encontrou ch„o " +
                        $"abaixo do ponto {requestedPosition}. " +
                        "Verifique Ground Mask e a posiÁ„o do spawn. " +
                        "Nova tentativa ser· feita.", ownerTransform);

                    return;
                }

                if (!foundGround)
                {
                    safePosition =
                        requestedPosition;

                    Debug.LogWarning(
                        $"[ENEMY RESPAWN] {ownerName} n„o encontrou ch„o. " +
                        "A posiÁ„o configurada ser· informada sem ajuste.", ownerTransform);
                }
            }

            isWaitingForRespawn =
                false;

            respawnTimer = 0f;

            EnemyRespawnResult result =
                new EnemyRespawnResult(
                    safePosition,
                    requestedRotation,
                    respawnHealthPercent);

            if (showDebugLogs)
            {
                Debug.Log(
                    $"[ENEMY RESPAWN] {ownerName}: posiÁ„o segura pronta | " +
                    $"PosiÁ„o: {result.Position} | " +
                    $"Vida: {result.HealthPercent * 100f:0.##}%.", ownerTransform);
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

                if (hitTransform == ownerTransform ||
                    hitTransform.IsChildOf(ownerTransform))
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
                        ownerTransform.lossyScale.y);

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
                    $"[ENEMY RESPAWN] Ch√£o encontrado para {ownerName} | " +
                    $"Hit: {groundHit.collider.name} | " +
                    $"Altura: {groundHit.point.y:0.###} | " +
                    $"Posi√ß√£o segura: {groundedPosition}.", ownerTransform);
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
                    ownerTransform))
            {
                return initialSpawnPosition;
            }

            return respawnPoint.position;
        }

        private Quaternion ResolveRequestedRotation()
        {
            if (!restoreSpawnRotation)
            {
                return ownerTransform.rotation;
            }

            if (respawnPoint == null ||
                ReferenceEquals(
                    respawnPoint,
                    ownerTransform))
            {
                return initialSpawnRotation;
            }

            return respawnPoint.rotation;
        }

        private void CacheInitialSpawn()
        {
            if (ownerTransform == null)
            {
                return;
            }

            initialSpawnPosition =
                ownerTransform.position;

            initialSpawnRotation =
                ownerTransform.rotation;
        }

        private void CacheReferences()
        {
        }
    }
}
