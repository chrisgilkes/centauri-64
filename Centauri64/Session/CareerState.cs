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

    /// <summary>
    /// UTC time of last Bedroom session start/save. Null on older saves.
    /// </summary>
    public DateTime? LastPlayedUtc { get; set; }

    public string SelectedBundleId { get; set; } = string.Empty;

    /// <summary>
    /// True after the starter Centauri64 bundle has been purchased.
    /// Older saves with BundleSoftwareGranted are treated as owning a computer.
    /// </summary>
    public bool HasComputer { get; set; }

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

    /// <summary>
    /// New Bedroom career before the first computer purchase (£300 saved).
    /// Magazines and bundle software are granted when the pack is bought.
    /// </summary>
    public static CareerState CreateNew(string name)
    {
        return new CareerState
        {
            Name = name.Trim().ToUpperInvariant(),
            Rank = CareerRanks.Hobbyist,
            SelectedBundleId = string.Empty,
            HasComputer = false,
            BundleSoftwareGranted = false,
            UnlockedFeatures = new List<string> { nameof(FeatureId.CoreBasic) },
            Progress = new PlayerProgress
            {
                CashPennies = ComputerPurchase.StartingCashPennies
            }
        };
    }

    /// <summary>
    /// Test/helper: create a career already bound to a bundle id without purchasing.
    /// Prefer <see cref="CreateNew(string)"/> + <see cref="ComputerPurchase.TryPurchase"/> in game flow.
    /// </summary>
    public static CareerState CreateNew(string name, string bundleId)
    {
        var career = CreateNew(name);
        if (!string.IsNullOrWhiteSpace(bundleId))
            career.SelectedBundleId = bundleId;

        return career;
    }

    public static CareerState FromLegacy(PlayerProgress progress)
    {
        var career = new CareerState
        {
            Name = "CODER",
            Rank = CareerRanks.Hobbyist,
            SelectedBundleId = ComputerBundleCatalog.LegacyId,
            HasComputer = true,
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

        if (OwnedMagazineIds.Count == 0 && HasComputer)
            MagazineProgression.EnsureStartingIssue(this);
        else if (OwnedMagazineIds.Count > 0)
            MagazineProgression.OwnThrough(this, MagazineProgression.HighestOwnedNumber(this));

        // Older careers that already received bundle software own a computer.
        if (!HasComputer && BundleSoftwareGranted)
            HasComputer = true;
        else if (!HasComputer &&
                 !string.IsNullOrWhiteSpace(SelectedBundleId) &&
                 OwnedMagazineIds.Count > 0)
        {
            HasComputer = true;
        }
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
        HasComputer = true;
        BundleSoftwareGranted = true;
        MagazineProgression.EnsureStartingIssue(this);
    }
}

public static class CareerRanks
{
    public const string Hobbyist = "HOBBYIST";
}
