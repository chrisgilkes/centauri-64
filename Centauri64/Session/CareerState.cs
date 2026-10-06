using System;
using System.Collections.Generic;
using System.Linq;

using Centauri64.Progression;

namespace Centauri64.Session;

public sealed class CareerState
{
    public int SchemaVersion { get; set; } = 1;

    public string Name { get; set; } = string.Empty;

    public string Rank { get; set; } = CareerRanks.Hobbyist;

    public string SelectedBundleId { get; set; } = string.Empty;

    public bool BundleSoftwareGranted { get; set; }

    public List<string> UnlockedFeatures { get; set; } = new()
    {
        nameof(FeatureId.CoreBasic)
    };

    public List<string> OwnedMagazineIds { get; set; } = new();

    /// <summary>Legacy field. Migrated into OwnedMagazineIds on load.</summary>
    public List<string> ReadMagazineIds { get; set; } = new();

    public List<string> UnlockedManualSections { get; set; } = new();

    public List<string> UnlockedPublisherIds { get; set; } = new();

    public List<string> UnlockedContractIds { get; set; } = new();

    public PlayerProgress Progress { get; set; } = new();

    public static CareerState CreateNew(string name, string bundleId)
    {
        var career = new CareerState
        {
            Name = name.Trim().ToUpperInvariant(),
            Rank = CareerRanks.Hobbyist,
            SelectedBundleId = bundleId,
            BundleSoftwareGranted = false,
            UnlockedFeatures = new List<string> { nameof(FeatureId.CoreBasic) },
            Progress = new PlayerProgress()
        };

        MagazineProgression.EnsureStartingIssue(career);
        return career;
    }

    public static CareerState FromLegacy(PlayerProgress progress)
    {
        var career = new CareerState
        {
            Name = "CODER",
            Rank = CareerRanks.Hobbyist,
            SelectedBundleId = ComputerBundleCatalog.LegacyId,
            BundleSoftwareGranted = true,
            UnlockedFeatures = new List<string>
            {
                nameof(FeatureId.CoreBasic),
                nameof(FeatureId.Graphics),
                nameof(FeatureId.Sprites),
                nameof(FeatureId.Maps),
                nameof(FeatureId.Images),
                nameof(FeatureId.Networking)
            },
            Progress = progress
        };

        MagazineProgression.OwnThrough(career, 10);
        return career;
    }

    public void MigrateMagazineFields()
    {
        OwnedMagazineIds ??= new List<string>();
        ReadMagazineIds ??= new List<string>();
        UnlockedManualSections ??= new List<string>();
        UnlockedPublisherIds ??= new List<string>();
        UnlockedContractIds ??= new List<string>();
        UnlockedFeatures ??= new List<string> { nameof(FeatureId.CoreBasic) };

        foreach (var id in ReadMagazineIds)
        {
            if (!OwnedMagazineIds.Contains(id, StringComparer.OrdinalIgnoreCase))
                OwnedMagazineIds.Add(id);
        }

        if (OwnedMagazineIds.Count == 0)
            MagazineProgression.EnsureStartingIssue(this);
        else
            MagazineProgression.OwnThrough(this, MagazineProgression.HighestOwnedNumber(this));
    }

    public IEnumerable<FeatureId> GetUnlockedFeatures()
    {
        foreach (var name in UnlockedFeatures)
        {
            if (Enum.TryParse<FeatureId>(name, ignoreCase: true, out var id))
                yield return id;
        }
    }

    public bool HasFeature(FeatureId feature) =>
        feature == FeatureId.CoreBasic ||
        UnlockedFeatures.Contains(feature.ToString(), StringComparer.OrdinalIgnoreCase);

    public bool Unlock(FeatureId feature)
    {
        var key = feature.ToString();
        if (UnlockedFeatures.Contains(key, StringComparer.OrdinalIgnoreCase))
            return false;

        UnlockedFeatures.Add(key);
        return true;
    }

    public IReadOnlyList<FeatureId> ApplyMagazine(string magazineId) =>
        MagazineProgression.OwnIssue(this, magazineId).NewFeatures;

    public void ResetProgressKeepIdentity()
    {
        Rank = CareerRanks.Hobbyist;
        UnlockedFeatures = new List<string> { nameof(FeatureId.CoreBasic) };
        OwnedMagazineIds = new List<string>();
        ReadMagazineIds = new List<string>();
        UnlockedManualSections = new List<string>();
        UnlockedPublisherIds = new List<string>();
        UnlockedContractIds = new List<string>();
        Progress = new PlayerProgress();
        BundleSoftwareGranted = true;
        MagazineProgression.EnsureStartingIssue(this);
    }
}

public static class CareerRanks
{
    public const string Hobbyist = "HOBBYIST";
}
