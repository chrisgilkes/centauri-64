using System;
using System.Collections.Generic;
using System.Linq;

using Centauri64.Publishing;

namespace Centauri64.Session;

public enum ProgressionStage
{
    StartOfIssue,
    IssueCompleted
}

/// <summary>
/// In-memory career progression simulation for developer testing.
/// Never writes to the saved career slot.
/// </summary>
public static class CareerProgressOverride
{
    private static CareerState? _simulated;

    public static bool Enabled { get; private set; }

    public static int IssueNumber { get; private set; }

    public static ProgressionStage Stage { get; private set; } =
        ProgressionStage.StartOfIssue;

    public static bool IsActive => Enabled && _simulated != null;

    public static CareerState? Simulated => IsActive ? _simulated : null;

    /// <summary>Highest magazine issue number in the catalogue (currently 10).</summary>
    public static int MaxIssueNumber =>
        MagazineCatalog.Issues.Length == 0
            ? 0
            : MagazineCatalog.Issues.Max(issue => issue.IssueNumber);

    public static void Set(bool enabled, int issueNumber, ProgressionStage stage)
    {
        Enabled = enabled;
        IssueNumber = Math.Clamp(issueNumber, 0, MaxIssueNumber);
        Stage = stage;
        _simulated = enabled ? Build(IssueNumber, Stage) : null;
    }

    public static void Disable()
    {
        Enabled = false;
        _simulated = null;
    }

    public static void Rebuild()
    {
        if (!Enabled)
        {
            _simulated = null;
            return;
        }

        _simulated = Build(IssueNumber, Stage);
    }

    public static string StatusLabel()
    {
        if (!IsActive)
            return string.Empty;

        if (IssueNumber <= 0)
            return "DEV: PRE-COMPUTER";

        var stage = Stage == ProgressionStage.IssueCompleted ? "COMPLETE" : "START";
        return $"DEV: ISSUE {IssueNumber} {stage}";
    }

    /// <summary>
    /// Builds a throwaway career that matches real unlock rules for the stage.
    /// Cover-tape software grants are skipped so the shared library is untouched.
    /// </summary>
    public static CareerState Build(int issueNumber, ProgressionStage stage)
    {
        if (issueNumber <= 0)
        {
            var pre = CareerState.CreateNew("DEV");
            pre.Progress.CashPennies = ComputerPurchase.StartingCashPennies;
            return pre;
        }

        var career = CareerState.CreateNew("DEV");
        career.HasComputer = true;
        career.BundleSoftwareGranted = true;
        career.SelectedBundleId = "starter";
        career.Progress.CashPennies = 10100;

        if (stage == ProgressionStage.StartOfIssue)
        {
            if (issueNumber == 1)
            {
                // First mag granted with the computer; challenges not finished.
                MagazineProgression.OwnThrough(career, 1, grantCoverSoftware: false);
            }
            else
            {
                MagazineProgression.OwnThrough(career, issueNumber - 1, grantCoverSoftware: false);
                CompleteCareerContractsForSaleOf(career, issueNumber);
            }
        }
        else
        {
            MagazineProgression.OwnThrough(career, issueNumber, grantCoverSoftware: false);
            CompleteCareerContractsThroughOwned(career, issueNumber);
        }

        return career;
    }

    /// <summary>
    /// Mark career-chain contracts complete so <paramref name="issueNumber"/> is OnSale.
    /// </summary>
    private static void CompleteCareerContractsForSaleOf(CareerState career, int issueNumber)
    {
        var issue = MagazineCatalog.FindByNumber(issueNumber);
        if (issue == null)
            return;

        if (!string.IsNullOrWhiteSpace(issue.OnSaleAfterContractId))
            CompleteCareerChainThrough(career, issue.OnSaleAfterContractId);
        else
            CompleteCareerChainThroughHighestAvailable(career, issueNumber - 1);
    }

    /// <summary>
    /// After completing issue N, finish contracts that gate issue N+1 (and prior chain).
    /// </summary>
    private static void CompleteCareerContractsThroughOwned(CareerState career, int ownedIssue)
    {
        var next = MagazineCatalog.FindByNumber(ownedIssue + 1);
        if (next != null && !string.IsNullOrWhiteSpace(next.OnSaleAfterContractId))
        {
            CompleteCareerChainThrough(career, next.OnSaleAfterContractId);
            return;
        }

        CompleteCareerChainThroughHighestAvailable(career, ownedIssue);
    }

    private static void CompleteCareerChainThroughHighestAvailable(CareerState career, int ownedIssue)
    {
        // Complete every career_* contract whose AvailableFromIssue <= ownedIssue,
        // respecting catalogue order so prerequisites stay coherent.
        foreach (var contract in PublisherCatalog.Contracts)
        {
            if (!contract.Id.StartsWith("career_", StringComparison.OrdinalIgnoreCase))
                continue;

            if (contract.AvailableFromIssue > ownedIssue)
                continue;

            MarkCompleted(career, contract.Id);
        }
    }

    private static void CompleteCareerChainThrough(CareerState career, string finalContractId)
    {
        foreach (var contract in PublisherCatalog.Contracts)
        {
            if (!contract.Id.StartsWith("career_", StringComparison.OrdinalIgnoreCase))
                continue;

            MarkCompleted(career, contract.Id);
            if (contract.Id.Equals(finalContractId, StringComparison.OrdinalIgnoreCase))
                break;
        }
    }

    private static void MarkCompleted(CareerState career, string contractId)
    {
        if (string.IsNullOrWhiteSpace(contractId))
            return;

        if (!career.Progress.CompletedContractIds.Contains(contractId, StringComparer.OrdinalIgnoreCase))
            career.Progress.CompletedContractIds.Add(contractId);
    }

    public static IReadOnlyList<(string Label, string Status)> Diagnostics()
    {
        var career = Simulated;
        var rows = new List<(string, string)>();

        if (career == null)
            return rows;

        void Feature(string label, FeatureId id) =>
            rows.Add((label, career.HasFeature(id) ? "UNLOCKED" : "LOCKED"));

        Feature("SPRITE EDITOR", FeatureId.Sprites);
        Feature("IMAGE EDITOR", FeatureId.Images);
        Feature("MAP EDITOR", FeatureId.Maps);
        Feature("GRAPHICS BASIC", FeatureId.Graphics);
        Feature("NETWORKING", FeatureId.Networking);

        rows.Add(("HAS COMPUTER", career.HasComputer ? "YES" : "NO"));

        for (var n = 1; n <= MaxIssueNumber; n++)
        {
            var issue = MagazineCatalog.FindByNumber(n);
            if (issue == null)
                continue;

            var state = MagazineProgression.StateOf(career, issue, referenceLibrary: false);
            rows.Add(("MAGAZINE #" + n, state.ToString().ToUpperInvariant()));
        }

        var highest = MagazineProgression.HighestOwnedNumber(career);
        rows.Add((
            "SOFTWARE HOUSE CONTRACTS",
            career.HasComputer && highest >= 1 ? "AVAILABLE" : "LOCKED"));

        return rows;
    }
}
