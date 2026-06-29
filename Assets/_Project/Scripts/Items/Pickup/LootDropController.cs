using System;
using System.Collections.Generic;
using Riftborn.Characters.Core;
using Riftborn.Characters.Health;
using UnityEngine;

namespace Riftborn.Items
{
    [Serializable]
    public sealed class LootDropEntry
    {
        [SerializeField]
        private ItemGenerationProfile generationProfile;

        [Tooltip(
            "Chance de gerar o item em cada tentativa. " +
            "1 representa 100%.")]
        [SerializeField, Range(0f, 1f)]
        private float dropChance = 1f;

        [SerializeField, Min(0)]
        private int minimumRolls = 1;

        [SerializeField, Min(0)]
        private int maximumRolls = 1;

        public ItemGenerationProfile GenerationProfile =>
            generationProfile;

        public float DropChance =>
            Mathf.Clamp01(dropChance);

        public int MinimumRolls =>
            Mathf.Max(
                0,
                minimumRolls);

        public int MaximumRolls =>
            Mathf.Max(
                MinimumRolls,
                maximumRolls);

        public void Validate()
        {
            dropChance =
                Mathf.Clamp01(dropChance);

            minimumRolls =
                Mathf.Max(
                    0,
                    minimumRolls);

            maximumRolls =
                Mathf.Max(
                    minimumRolls,
                    maximumRolls);
        }
    }

    public sealed class LootDropController : MonoBehaviour
    {
        [Header("Character")]
        [SerializeField]
        private CharacterContext character;

        [Header("World Pickup")]
        [SerializeField]
        private WorldItemPickup pickupPrefab;

        [SerializeField]
        private Transform dropPoint;

        [Header("Drop Position")]
        [SerializeField, Min(0f)]
        private float horizontalScatterRadius = 0.6f;

        [SerializeField]
        private float verticalOffset = 0.15f;

        [Header("Loot Table")]
        [SerializeField]
        private List<LootDropEntry> lootEntries =
            new();

        [Header("Rules")]
        [SerializeField]
        private bool dropOnlyOnce = true;

        [Header("Debug")]
        [SerializeField]
        private bool showDebugLogs = true;

        private HealthController health;

        private bool isSubscribed;
        private bool lootDropped;

        private void Awake()
        {
            CacheReferences();
        }

        private void OnEnable()
        {
            CacheReferences();
            SubscribeToHealth();
        }

        /*
         * Start acontece depois dos Awakes.
         * Fazemos uma segunda tentativa porque o CharacterContext
         * pode ainda não ter guardado o HealthController durante
         * o primeiro OnEnable.
         */
        private void Start()
        {
            CacheReferences();
            SubscribeToHealth();

            if (showDebugLogs)
            {
                Debug.Log(
                    $"[LOOT] {name} inicializado | " +
                    $"Health: {(health != null ? "OK" : "NULL")} | " +
                    $"Pickup Prefab: " +
                    $"{(pickupPrefab != null ? "OK" : "NULL")} | " +
                    $"Entradas: {lootEntries.Count}",
                    this);
            }
        }

        private void OnDisable()
        {
            UnsubscribeFromHealth();
        }

        private void OnValidate()
        {
            horizontalScatterRadius =
                Mathf.Max(
                    0f,
                    horizontalScatterRadius);

            lootEntries ??=
                new List<LootDropEntry>();

            for (int index = 0;
                 index < lootEntries.Count;
                 index++)
            {
                lootEntries[index]?.Validate();
            }
        }

        [ContextMenu("Drop Loot Test")]
        public void DropLootTest()
        {
            DropLoot();
        }

        private void CacheReferences()
        {
            character ??=
                GetComponent<CharacterContext>();

            /*
             * Primeiro tenta pelo CharacterContext.
             * Caso ele ainda não tenha sido inicializado,
             * procura diretamente no mesmo GameObject.
             */
            if (health == null &&
                character != null)
            {
                health =
                    character.Health;
            }

            health ??=
                GetComponent<HealthController>();
        }

        private void SubscribeToHealth()
        {
            if (isSubscribed)
            {
                return;
            }

            if (health == null)
            {
                if (showDebugLogs)
                {
                    Debug.LogWarning(
                        $"[LOOT] {name} não encontrou " +
                        "HealthController e não pôde se conectar " +
                        "ao evento de morte.",
                        this);
                }

                return;
            }

            health.Died +=
                HandleCharacterDied;

            health.Revived +=
                HandleCharacterRevived;

            isSubscribed = true;

            if (showDebugLogs)
            {
                Debug.Log(
                    $"[LOOT] {name} conectado ao evento Died.",
                    this);
            }
        }

        private void UnsubscribeFromHealth()
        {
            if (!isSubscribed ||
                health == null)
            {
                return;
            }

            health.Died -=
                HandleCharacterDied;

            health.Revived -=
                HandleCharacterRevived;

            isSubscribed = false;
        }

        private void HandleCharacterDied()
        {
            if (showDebugLogs)
            {
                Debug.Log(
                    $"[LOOT] Morte detectada em {name}. " +
                    "Processando tabela de loot.",
                    this);
            }

            DropLoot();
        }

        private void HandleCharacterRevived()
        {
            if (!dropOnlyOnce)
            {
                lootDropped = false;
            }
        }

        private void DropLoot()
        {
            if (dropOnlyOnce &&
                lootDropped)
            {
                if (showDebugLogs)
                {
                    Debug.Log(
                        $"[LOOT] {name} já processou o loot.",
                        this);
                }

                return;
            }

            if (pickupPrefab == null)
            {
                Debug.LogError(
                    $"[LOOT] {name} não possui um " +
                    "WorldItemPickup configurado.",
                    this);

                return;
            }

            if (lootEntries == null ||
                lootEntries.Count == 0)
            {
                Debug.LogWarning(
                    $"[LOOT] {name} não possui entradas " +
                    "na tabela de loot.",
                    this);

                return;
            }

            lootDropped = true;

            int generatedItemCount = 0;

            for (int entryIndex = 0;
                 entryIndex < lootEntries.Count;
                 entryIndex++)
            {
                LootDropEntry entry =
                    lootEntries[entryIndex];

                if (entry == null)
                {
                    Debug.LogWarning(
                        $"[LOOT] Entrada {entryIndex} está vazia.",
                        this);

                    continue;
                }

                if (entry.GenerationProfile == null)
                {
                    Debug.LogWarning(
                        $"[LOOT] Entrada {entryIndex} não possui " +
                        "ItemGenerationProfile.",
                        this);

                    continue;
                }

                int rollCount =
                    UnityEngine.Random.Range(
                        entry.MinimumRolls,
                        entry.MaximumRolls + 1);

                for (int rollIndex = 0;
                     rollIndex < rollCount;
                     rollIndex++)
                {
                    float chanceRoll =
                        UnityEngine.Random.value;

                    if (chanceRoll >
                        entry.DropChance)
                    {
                        if (showDebugLogs)
                        {
                            Debug.Log(
                                $"[LOOT] Entrada {entryIndex}, " +
                                $"tentativa {rollIndex}: " +
                                $"falhou na chance " +
                                $"({chanceRoll:0.###} > " +
                                $"{entry.DropChance:0.###}).",
                                this);
                        }

                        continue;
                    }

                    if (!ItemGenerator.TryGenerate(
                            entry.GenerationProfile,
                            out ItemInstance itemInstance))
                    {
                        Debug.LogWarning(
                            $"[LOOT] Não foi possível gerar item " +
                            $"com o perfil " +
                            $"'{entry.GenerationProfile.name}'.",
                            this);

                        continue;
                    }

                    if (SpawnWorldItem(
                            itemInstance,
                            generatedItemCount))
                    {
                        generatedItemCount++;
                    }
                }
            }

            Debug.Log(
                $"[LOOT] {name} gerou " +
                $"{generatedItemCount} item(ns).",
                this);
        }

        private bool SpawnWorldItem(
            ItemInstance itemInstance,
            int itemIndex)
        {
            if (itemInstance == null ||
                !itemInstance.IsValid)
            {
                return false;
            }

            Vector3 basePosition =
                dropPoint != null
                    ? dropPoint.position
                    : transform.position;

            Vector2 randomCircle =
                UnityEngine.Random.insideUnitCircle *
                horizontalScatterRadius;

            Vector3 spawnPosition =
                basePosition +
                new Vector3(
                    randomCircle.x,
                    verticalOffset,
                    randomCircle.y);

            WorldItemPickup pickupInstance =
                Instantiate(
                    pickupPrefab,
                    spawnPosition,
                    Quaternion.identity);

            if (pickupInstance == null)
            {
                Debug.LogError(
                    $"[LOOT] Não foi possível criar o pickup de " +
                    $"'{itemInstance.DisplayName}'.",
                    this);

                return false;
            }

            pickupInstance.Initialize(
                itemInstance);

            Debug.Log(
                $"[LOOT] Drop criado: " +
                $"{itemInstance.DisplayName} | " +
                $"ID: {itemInstance.InstanceId} | " +
                $"Índice: {itemIndex} | " +
                $"Posição: {spawnPosition}",
                pickupInstance);

            return true;
        }

        private void OnDestroy()
        {
            UnsubscribeFromHealth();
        }
    }
}