using System;
using System.Collections.Generic;
using System.Linq;

namespace Centauri64.Session;

public readonly struct MagazineGrantResult
{
    public IReadOnlyList<FeatureId> NewFeatures { get; init; }
    public IReadOnlyList<string> NewPublishers { get; init; }
    public IReadOnlyList<string> NewContracts { get; init; }
    public bool NewlyOwned { get; init; }

    public bool AnyNew =>
        NewlyOwned ||
        NewFeatures.Count > 0 ||
        NewPublishers.Count > 0 ||
        NewContracts.Count > 0;
}

public static class MagazineProgression
{
    public static MagazineIssue FirstIssue =>
        MagazineCatalog.FindByNumber(1) ?? MagazineCatalog.Issues[0];

    public static bool Owns(CareerState career, string issueId) =>
        career.OwnedMagazineIds.Contains(issueId, StringComparer.OrdinalIgnoreCase);

    public static int HighestOwnedNumber(CareerState career)
    {
        var highest = 0;
        foreach (var id in career.OwnedMagazineIds)
        {
            var issue = MagazineCatalog.Find(id);
            if (issue != null && issue.IssueNumber > highest)
                highest = issue.IssueNumber;
        }

        return highest;
    }

    public static MagazineIssueState StateOf(CareerState? career, MagazineIssue issue, bool referenceLibrary)
    {
        if (referenceLibrary)
            return MagazineIssueState.Owned;

        if (career == null)
            return MagazineIssueState.ComingLater;

        if (Owns(career, issue.Id))
            return MagazineIssueState.Owned;

        var nextNumber = HighestOwnedNumber(career) + 1;
        if (issue.IssueNumber == nextNumber)
            return MagazineIssueState.ComingNext;

        return MagazineIssueState.ComingLater;
    }

    public static MagazineGrantResult OwnIssue(CareerState career, string issueId)
    {
        var issue = MagazineCatalog.Find(issueId);
        if (issue == null)
            return default;

        var newlyOwned = false;
        if (!Owns(career, issue.Id))
        {
            career.OwnedMagazineIds.Add(issue.Id);
            newlyOwned = true;
        }

        var features = new List<FeatureId>();
        foreach (var feature in issue.GrantedFeatures)
        {
            if (career.Unlock(feature))
                features.Add(feature);
        }

        foreach (var section in issue.ManualSectionsUnlocked)
        {
            if (!career.UnlockedManualSections.Contains(section, StringComparer.OrdinalIgnoreCase))
                career.UnlockedManualSections.Add(section);
        }

        var publishers = AddUnique(career.UnlockedPublisherIds, issue.PublisherUnlockIds);
        var contracts = AddUnique(career.UnlockedContractIds, issue.ContractUnlockIds);

        if (issue.CoverTapeReady && !string.IsNullOrWhiteSpace(issue.CoverGameId))
            SoftwareGrant.GrantTapes(new[] { issue.CoverGameId });

        return new MagazineGrantResult
        {
            NewlyOwned = newlyOwned,
            NewFeatures = features,
            NewPublishers = publishers,
            NewContracts = contracts
        };
    }

    /// <summary>
    /// Owns issues 1..N. Idempotent. Used by debug progression and onboarding.
    /// </summary>
    public static MagazineGrantResult OwnThrough(CareerState career, int issueNumber)
    {
        var combined = new MagazineGrantResult
        {
            NewFeatures = Array.Empty<FeatureId>(),
            NewPublishers = Array.Empty<string>(),
            NewContracts = Array.Empty<string>()
        };

        foreach (var issue in MagazineCatalog.Issues.OrderBy(item => item.IssueNumber))
        {
            if (issue.IssueNumber > issueNumber)
                break;

            var step = OwnIssue(career, issue.Id);
            combined = Merge(combined, step);
        }

        return combined;
    }

    public static void EnsureStartingIssue(CareerState career) =>
        OwnThrough(career, 1);

    private static IReadOnlyList<string> AddUnique(List<string> target, IReadOnlyList<string> incoming)
    {
        var added = new List<string>();
        foreach (var id in incoming)
        {
            if (string.IsNullOrWhiteSpace(id))
                continue;
            if (target.Contains(id, StringComparer.OrdinalIgnoreCase))
                continue;
            target.Add(id);
            added.Add(id);
        }

        return added;
    }

    private static MagazineGrantResult Merge(MagazineGrantResult a, MagazineGrantResult b)
    {
        return new MagazineGrantResult
        {
            NewlyOwned = a.NewlyOwned || b.NewlyOwned,
            NewFeatures = a.NewFeatures.Concat(b.NewFeatures).Distinct().ToList(),
            NewPublishers = a.NewPublishers.Concat(b.NewPublishers).ToList(),
            NewContracts = a.NewContracts.Concat(b.NewContracts).ToList()
        };
    }
}
