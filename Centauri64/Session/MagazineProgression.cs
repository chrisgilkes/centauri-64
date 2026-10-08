using System;
using System.Collections.Generic;
using System.Linq;

namespace Centauri64.Session;

public enum MagazinePurchaseResult
{
    Purchased,
    AlreadyOwned,
    NotOnSale,
    CannotAfford,
    NoCareer
}

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

        var candidate = HighestOwnedNumber(career) + 1;
        if (issue.IssueNumber == candidate)
            return MilestoneMet(career, issue)
                ? MagazineIssueState.OnSale
                : MagazineIssueState.ComingNext;

        if (issue.IssueNumber == candidate + 1)
        {
            var onSale = MagazineCatalog.FindByNumber(candidate);
            if (onSale != null && MilestoneMet(career, onSale))
                return MagazineIssueState.ComingNext;
        }

        return MagazineIssueState.ComingLater;
    }

    /// <summary>
    /// Next issue is on sale after the previous issue is owned and, if set,
    /// <see cref="MagazineIssue.OnSaleAfterContractId"/> is completed.
    /// Fictional career time only — never wall-clock.
    /// </summary>
    public static bool MilestoneMet(CareerState career, MagazineIssue issue)
    {
        if (issue.IssueNumber <= 1)
            return false;

        if (HighestOwnedNumber(career) != issue.IssueNumber - 1)
            return false;

        if (string.IsNullOrWhiteSpace(issue.OnSaleAfterContractId))
            return true;

        return career.Progress.HasCompleted(issue.OnSaleAfterContractId);
    }

    public static int CalendarIssueNumber(CareerState? career, bool referenceLibrary)
    {
        if (referenceLibrary)
            return 10;

        if (career == null)
            return 1;

        var owned = Math.Max(1, HighestOwnedNumber(career));
        var next = MagazineCatalog.FindByNumber(owned + 1);
        if (next != null && MilestoneMet(career, next))
            return next.IssueNumber;

        return owned;
    }

    public static MagazinePurchaseResult Purchase(CareerState career, MagazineIssue issue)
    {
        if (Owns(career, issue.Id))
            return MagazinePurchaseResult.AlreadyOwned;

        if (StateOf(career, issue, referenceLibrary: false) != MagazineIssueState.OnSale)
            return MagazinePurchaseResult.NotOnSale;

        var price = Math.Max(0, issue.PricePennies);
        if (career.Progress.CashPennies < price)
            return MagazinePurchaseResult.CannotAfford;

        career.Progress.CashPennies -= price;
        OwnIssue(career, issue.Id);
        return MagazinePurchaseResult.Purchased;
    }

    public static MagazineGrantResult OwnIssue(
        CareerState career,
        string issueId,
        bool grantCoverSoftware = true)
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

        if (grantCoverSoftware &&
            issue.CoverTapeReady &&
            !string.IsNullOrWhiteSpace(issue.CoverGameId))
        {
            SoftwareGrant.GrantTapes(new[] { issue.CoverGameId });
        }

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
    public static MagazineGrantResult OwnThrough(
        CareerState career,
        int issueNumber,
        bool grantCoverSoftware = true)
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

            var step = OwnIssue(career, issue.Id, grantCoverSoftware);
            combined = Merge(combined, step);
        }

        return combined;
    }

    public static void EnsureStartingIssue(CareerState career) =>
        OwnThrough(career, 1);

    public static string EraLabel(CareerState? career, bool referenceLibrary)
    {
        if (referenceLibrary)
            return "YEAR ONE";

        var number = CalendarIssueNumber(career, referenceLibrary);
        var issue = MagazineCatalog.FindByNumber(number);
        return issue?.FictionalMonth ?? "JAN 1986";
    }

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
