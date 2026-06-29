using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace Riftborn.Items
{
    [Serializable]
    public sealed class WeightedItemGenerationProfileEntry
    {
        [SerializeField]
        private ItemGenerationProfile profile;

        [SerializeField, Min(0f)]
        private float weight = 1f;

        [SerializeField]
        private bool enabled = true;

        public ItemGenerationProfile Profile =>
            profile;

        public float Weight =>
            Mathf.Max(0f, weight);

        public bool Enabled =>
            enabled;

        public void Validate()
        {
            weight =
                Mathf.Max(0f, weight);
        }
    }

    public sealed class ItemGenerationDropTableValidationResult
    {
        private readonly List<ItemGenerationProfile> validProfiles =
            new();

        private readonly List<string> warnings =
            new();

        public IReadOnlyList<ItemGenerationProfile> ValidProfiles =>
            validProfiles;

        public IReadOnlyList<string> Warnings =>
            warnings;

        public int TotalEntries { get; internal set; }
        public float TotalValidWeight { get; internal set; }

        public bool HasWarnings =>
            warnings.Count > 0;

        internal void AddValidProfile(
            ItemGenerationProfile profile,
            float weight)
        {
            validProfiles.Add(profile);
            TotalValidWeight +=
                Mathf.Max(0f, weight);
        }

        internal void AddWarning(
            string warning)
        {
            if (!string.IsNullOrWhiteSpace(warning))
            {
                warnings.Add(warning);
            }
        }

        public string BuildDiagnosticText(
            ItemGenerationDropTable table)
        {
            StringBuilder builder =
                new StringBuilder();

            builder.AppendLine(
                $"[LOOT TABLE VALIDATION] {table?.name ?? "NULL"}");

            builder.AppendLine(
                $"Entradas totais: {TotalEntries}");

            builder.AppendLine(
                $"Entradas validas: {ValidProfiles.Count}");

            builder.AppendLine(
                $"Peso total valido: {TotalValidWeight:0.###}");

            for (int index = 0;
                 index < warnings.Count;
                 index++)
            {
                builder.AppendLine(
                    $"WARNING: {warnings[index]}");
            }

            return builder.ToString();
        }
    }

    [CreateAssetMenu(
        fileName = "NewItemGenerationDropTable",
        menuName = "Riftborn/Items/Item Generation Drop Table")]
    public sealed class ItemGenerationDropTable :
        ScriptableObject
    {
        [SerializeField]
        private List<WeightedItemGenerationProfileEntry> entries =
            new();

        public IReadOnlyList<WeightedItemGenerationProfileEntry> Entries =>
            entries;

        public ItemGenerationDropTableValidationResult ValidateTable()
        {
            ItemGenerationDropTableValidationResult result =
                new ItemGenerationDropTableValidationResult();

            if (entries == null)
            {
                result.TotalEntries = 0;
                return result;
            }

            result.TotalEntries =
                entries.Count;

            HashSet<ItemGenerationProfile> usedProfiles =
                new HashSet<ItemGenerationProfile>();

            for (int index = 0;
                 index < entries.Count;
                 index++)
            {
                WeightedItemGenerationProfileEntry entry =
                    entries[index];

                if (entry == null)
                {
                    result.AddWarning(
                        $"Entrada {index} esta vazia.");

                    continue;
                }

                if (!entry.Enabled)
                {
                    result.AddWarning(
                        $"Entrada {index} esta desabilitada.");

                    continue;
                }

                if (entry.Profile == null)
                {
                    result.AddWarning(
                        $"Entrada {index} nao possui ItemGenerationProfile.");

                    continue;
                }

                if (entry.Weight <= 0f)
                {
                    result.AddWarning(
                        $"Entrada {index} possui peso invalido ({entry.Weight:0.###}).");

                    continue;
                }

                if (!usedProfiles.Add(entry.Profile))
                {
                    result.AddWarning(
                        $"Entrada {index} duplica o perfil '{entry.Profile.name}' " +
                        "e sera ignorada para nao aumentar a chance.");

                    continue;
                }

                if (entry.Profile.Item == null)
                {
                    result.AddWarning(
                        $"Entrada {index} usa o perfil '{entry.Profile.name}' sem item configurado.");

                    continue;
                }

                result.AddValidProfile(
                    entry.Profile,
                    entry.Weight);
            }

            return result;
        }

        public bool TrySelectProfile(
            out ItemGenerationProfile selectedProfile,
            bool logWarnings = false)
        {
            selectedProfile = null;

            ItemGenerationDropTableValidationResult validation =
                ValidateTable();

            if (logWarnings &&
                validation.HasWarnings)
            {
                Debug.LogWarning(
                    validation.BuildDiagnosticText(this),
                    this);
            }

            if (validation.ValidProfiles.Count == 0 ||
                validation.TotalValidWeight <= 0f)
            {
                if (logWarnings)
                {
                    Debug.LogWarning(
                        $"[LOOT TABLE] '{name}' nao possui entradas validas.",
                        this);
                }

                return false;
            }

            float roll =
                UnityEngine.Random.value *
                validation.TotalValidWeight;

            float accumulatedWeight = 0f;
            HashSet<ItemGenerationProfile> usedProfiles =
                new HashSet<ItemGenerationProfile>();

            for (int index = 0;
                 index < entries.Count;
                 index++)
            {
                WeightedItemGenerationProfileEntry entry =
                    entries[index];

                if (entry == null ||
                    !entry.Enabled ||
                    entry.Profile == null ||
                    entry.Profile.Item == null ||
                    entry.Weight <= 0f ||
                    !usedProfiles.Add(entry.Profile))
                {
                    continue;
                }

                accumulatedWeight +=
                    entry.Weight;

                if (roll > accumulatedWeight)
                {
                    continue;
                }

                selectedProfile =
                    entry.Profile;

                return true;
            }

            selectedProfile =
                validation.ValidProfiles[validation.ValidProfiles.Count - 1];

            return selectedProfile != null;
        }

        private void OnValidate()
        {
            entries ??=
                new List<WeightedItemGenerationProfileEntry>();

            for (int index = 0;
                 index < entries.Count;
                 index++)
            {
                entries[index]?.Validate();
            }
        }
    }
}
