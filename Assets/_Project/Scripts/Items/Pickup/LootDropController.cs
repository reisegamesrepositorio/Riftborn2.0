using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

namespace Riftborn.Items
{
    [Serializable]
    public sealed class LootDropEntry
    {
        [SerializeField]
        private ItemGenerationProfile generationProfile;

        [SerializeField]
        private ItemGenerationDropTable generationDropTable;

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

        public ItemGenerationDropTable GenerationDropTable =>
            generationDropTable;

        public float DropChance =>
            Mathf.Clamp01(
                dropChance);

        public int MinimumRolls =>
            Mathf.Max(
                0,
                minimumRolls);

        public int MaximumRolls =>
            Mathf.Max(
                MinimumRolls,
                maximumRolls);

        public bool TrySelectGenerationProfile(
            out ItemGenerationProfile selectedProfile,
            bool logWarnings)
        {
            selectedProfile =
                null;

            if (generationDropTable != null)
            {
                return generationDropTable.TrySelectProfile(
                    out selectedProfile,
                    logWarnings);
            }

            selectedProfile =
                generationProfile;

            return selectedProfile != null;
        }

        public void Validate()
        {
            dropChance =
                Mathf.Clamp01(
                    dropChance);

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
        [FormerlySerializedAs("dropOnlyOnce")]
        [Tooltip(
            "Impede que a mesma vida processe loot mais de uma vez. " +
            "O EnemyController reinicia este estado no revive.")]
        [SerializeField]
        private bool dropOncePerLife = true;

        [Header("Debug")]
        [SerializeField]
        private bool showDebugLogs = true;

        private bool lootProcessedThisLife;

        public bool LootProcessedThisLife =>
            lootProcessedThisLife;

        private void Start()
        {
            if (showDebugLogs)
            {
                Debug.Log(
                    $"[LOOT] {name} inicializado | " +
                    $"Pickup: {(pickupPrefab != null ? "OK" : "NULL")} | " +
                    $"Entradas: {lootEntries.Count}",
                    this);
            }
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

        [ContextMenu("Reset Loot State")]
        public void ResetLootState()
        {
            lootProcessedThisLife =
                false;

            if (showDebugLogs)
            {
                Debug.Log(
                    $"[LOOT] Estado de loot de {name} foi reiniciado.",
                    this);
            }
        }

        public int DropLoot()
        {
            if (dropOncePerLife &&
                lootProcessedThisLife)
            {
                if (showDebugLogs)
                {
                    Debug.Log(
                        $"[LOOT] {name} já processou " +
                        "o loot desta vida.",
                        this);
                }

                return 0;
            }

            if (pickupPrefab == null)
            {
                Debug.LogError(
                    $"[LOOT] {name} não possui um " +
                    "WorldItemPickup configurado.",
                    this);

                return 0;
            }

            if (lootEntries == null ||
                lootEntries.Count == 0)
            {
                Debug.LogWarning(
                    $"[LOOT] {name} não possui entradas " +
                    "na tabela de loot.",
                    this);

                return 0;
            }

            lootProcessedThisLife =
                true;

            int generatedItemCount =
                0;

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

                if (!entry.TrySelectGenerationProfile(
                        out ItemGenerationProfile selectedProfile,
                        showDebugLogs))
                {
                    Debug.LogWarning(
                        $"[LOOT] Entrada {entryIndex} não possui " +
                        "perfil ou tabela válida.",
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
                                $"tentativa {rollIndex}: falhou " +
                                $"({chanceRoll:0.###} > " +
                                $"{entry.DropChance:0.###}).",
                                this);
                        }

                        continue;
                    }

                    if (!ItemGenerator.TryGenerate(
                            selectedProfile,
                            out ItemInstance itemInstance))
                    {
                        Debug.LogWarning(
                            $"[LOOT] Falha ao gerar item com " +
                            $"'{selectedProfile.name}'.",
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

            return generatedItemCount;
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
    }
}
