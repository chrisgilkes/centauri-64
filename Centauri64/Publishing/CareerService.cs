using System;
using System.Collections.Generic;
using System.Linq;

using Centauri64.Analysis;
using Centauri64.Basic;
using Centauri64.Career;
using Centauri64.Progression;
using Centauri64.Session;

namespace Centauri64.Publishing;

public enum ContractAvailability
{
    Locked,
    Available,
    Pending,
    ResponseReady,
    Completed
}

public sealed class CareerService
{
    private readonly IPlayerProgressStore _storage;
    private readonly SoftwareAnalyser _analyser = new();

    public CareerService()
        : this(new FilePlayerProgressStore())
    {
    }

    public CareerService(IPlayerProgressStore storage)
    {
        _storage = storage;
    }

    public PlayerProgress LoadProgress()
    {
        var progress = _storage.Load();
        progress.MigrateLegacyRewards();
        return progress;
    }

    public void SaveProgress(PlayerProgress progress) =>
        _storage.Save(progress);

    /// <summary>
    /// Clears career progression only. Never deletes tapes, programs, or assets.
    /// Active Bedroom slot identity (name / bundle) is kept by the caller.
    /// </summary>
    public void ResetCareer()
    {
        CareerLog.Info("Resetting career progress");
        SaveProgress(new PlayerProgress());
        CareerLog.Info("Career reset complete");
    }

    public SubmissionResult Evaluate(
        SubmissionContract contract,
        SoftwareAnalysis analysis,
        TapeLabel label)
    {
        var result = SubmissionEvaluator.Evaluate(
            contract.Requirements,
            analysis,
            label);

        var progress = CareerProgressOverride.IsActive && GameSession.EffectiveCareer != null
            ? GameSession.EffectiveCareer.Progress
            : LoadProgress();

        if (!contract.Repeatable && progress.HasCompleted(contract.Id))
        {
            return new SubmissionResult
            {
                Accepted = false,
                AlreadyCompleted = true,
                Checks = result.Checks
            };
        }

        if (!contract.Repeatable && progress.HasPendingSubmission(contract.Id))
        {
            return new SubmissionResult
            {
                Accepted = false,
                AlreadyCompleted = true,
                Checks = result.Checks
            };
        }

        return result;
    }

    public SoftwareAnalysis AnalyseTape(string tapeName) =>
        _analyser.AnalyseTape(tapeName);

    public ContractAvailability GetAvailability(
        SubmissionContract contract,
        PlayerProgress? progress = null)
    {
        if (progress == null &&
            CareerProgressOverride.IsActive &&
            GameSession.EffectiveCareer != null)
        {
            progress = GameSession.EffectiveCareer.Progress;
        }
        else
        {
            progress ??= LoadProgress();
        }

        if (progress.HasCompleted(contract.Id))
            return ContractAvailability.Completed;

        var submission = progress.Submissions
            .LastOrDefault(s => s.ContractId == contract.Id);

        if (submission != null)
        {
            return submission.Status switch
            {
                SubmissionStatus.Pending => ContractAvailability.Pending,
                SubmissionStatus.ResponseReady => ContractAvailability.ResponseReady,
                SubmissionStatus.Accepted => ContractAvailability.ResponseReady,
                SubmissionStatus.Completed => ContractAvailability.Completed,
                _ => ContractAvailability.Pending
            };
        }

        if (!PrerequisitesMet(contract, progress))
            return ContractAvailability.Locked;

        return ContractAvailability.Available;
    }

    public MagazinePurchaseResult TryPurchaseIssue(string issueId)
    {
        if (CareerProgressOverride.IsActive)
            return MagazinePurchaseResult.NotOnSale;

        var career = GameSession.Career;
        if (career == null)
            return MagazinePurchaseResult.NoCareer;

        var issue = MagazineCatalog.Find(issueId);
        if (issue == null)
            return MagazinePurchaseResult.NotOnSale;

        var result = MagazineProgression.Purchase(career, issue);
        if (result == MagazinePurchaseResult.Purchased)
            SaveProgress(career.Progress);

        return result;
    }

    public IReadOnlyList<SubmissionContract> GetVisibleContracts(
        PlayerProgress? progress = null)
    {
        var career = GameSession.EffectiveCareer;
        if (CareerProgressOverride.IsActive && career != null)
            progress = career.Progress;
        else
            progress ??= LoadProgress();

        var highest = career == null
            ? 0
            : MagazineProgression.HighestOwnedNumber(career);
        var list = new List<SubmissionContract>();

        foreach (var contract in PublisherCatalog.Contracts)
        {
            var availability = GetAvailability(contract, progress);
            if (availability == ContractAvailability.Locked)
                continue;

            if (career != null && !IsOnTheMarket(contract, career, highest))
                continue;

            list.Add(contract);
        }

        return list;
    }

    public static bool IsOnTheMarket(
        SubmissionContract contract,
        CareerState career,
        int highestOwned)
    {
        var organisation = PublisherCatalog.GetOrganisation(contract.OrganisationId);
        if (organisation == null)
            return false;

        if (!PublisherIsPresent(organisation, career, highestOwned))
            return false;

        if (contract.AvailableFromIssue > 0 &&
            highestOwned < contract.AvailableFromIssue)
            return false;

        return true;
    }

    public static bool PublisherIsPresent(
        OrganisationDefinition organisation,
        CareerState career,
        int highestOwned)
    {
        if (career.UnlockedPublisherIds.Contains(
                organisation.Id,
                StringComparer.OrdinalIgnoreCase))
            return true;

        return highestOwned >= organisation.AvailableFromIssue;
    }

    public bool PrerequisitesMet(
        SubmissionContract contract,
        PlayerProgress progress)
    {
        if (contract.PrerequisiteContractIds.Length == 0)
            return true;

        foreach (var id in contract.PrerequisiteContractIds)
        {
            if (!progress.HasCompleted(id))
                return false;
        }

        return true;
    }

    /// <summary>
    /// Creates a pending submission. Does not award cash.
    /// </summary>
    public bool TrySubmit(
        SubmissionContract contract,
        string tapeName,
        TapeLabel label,
        SoftwareAnalysis analysis,
        out SubmissionResult result)
    {
        if (CareerProgressOverride.IsActive)
        {
            result = Evaluate(contract, analysis, label);
            return false;
        }

        result = Evaluate(contract, analysis, label);

        if (!result.Accepted || result.AlreadyCompleted)
            return false;

        var progress = LoadProgress();
        var organisation = PublisherCatalog.GetOrganisation(contract.OrganisationId);
        var title = string.IsNullOrWhiteSpace(label.Description)
            ? tapeName
            : label.Description;

        var record = new SubmissionRecord
        {
            Id = Guid.NewGuid().ToString("N"),
            ContractId = contract.Id,
            OrganisationId = contract.OrganisationId,
            TapeName = tapeName,
            SoftwareTitle = title.ToUpperInvariant(),
            RewardPence = contract.RewardPennies,
            Status = SubmissionStatus.Pending,
            WasAccepted = true
        };

        progress.Submissions.Add(record);
        SaveProgress(progress);

        CareerLog.Info(
            $"Submitted {tapeName} to {contract.Id} ({record.Id})");
        CareerLog.Info("Submission pending");

        return true;
    }

    /// <summary>
    /// Turns pending submissions into mail. Call when returning to the bedroom.
    /// </summary>
    public int DeliverPendingResponses()
    {
        var progress = LoadProgress();
        var delivered = 0;

        foreach (var submission in progress.Submissions)
        {
            if (submission.Status != SubmissionStatus.Pending)
                continue;

            var contract = PublisherCatalog.GetContract(submission.ContractId);
            var organisation = PublisherCatalog.GetOrganisation(submission.OrganisationId);

            if (contract == null || organisation == null)
                continue;

            if (string.IsNullOrEmpty(submission.OrganisationId) &&
                contract != null)
            {
                submission.OrganisationId = contract.OrganisationId;
            }

            var body = FormatLetter(
                organisation.AcceptanceBody,
                submission.SoftwareTitle,
                submission.RewardPence);

            // Neutral subject — never ACCEPTED / REJECTED / payment spoilers.
            var mail = new MailMessage
            {
                Id = Guid.NewGuid().ToString("N"),
                FromOrganisationId = organisation.Id,
                Subject = "RE: YOUR SOFTWARE SUBMISSION",
                Body = body,
                Read = false,
                RewardPence = submission.RewardPence,
                RewardClaimed = false,
                RelatedContractId = contract.Id,
                RelatedSubmissionId = submission.Id
            };

            progress.Mail.Add(mail);
            submission.Status = SubmissionStatus.ResponseReady;
            delivered++;

            CareerLog.Info($"Response ready: {contract.Id}");
            CareerLog.Info($"Created acceptance mail ({mail.Id})");
        }

        if (delivered > 0)
        {
            SaveProgress(progress);
            CareerLog.Info(
                $"Unread mail count: {progress.Mail.Count(m => !m.Read)}");
        }

        return delivered;
    }

    /// <summary>
    /// Marks mail read and claims enclosed payment exactly once.
    /// Call only when the player opens the message body.
    /// </summary>
    public bool OpenMail(
        string mailId,
        out int awardedPence,
        out int cashBefore,
        out int cashAfter)
    {
        awardedPence = 0;
        cashBefore = 0;
        cashAfter = 0;
        var progress = LoadProgress();
        var mail = progress.Mail.FirstOrDefault(m => m.Id == mailId);

        if (mail == null)
            return false;

        cashBefore = progress.CashPennies;
        cashAfter = progress.CashPennies;

        var wasUnread = !mail.Read;
        mail.Read = true;

        if (wasUnread)
            CareerLog.Info($"Opened message: {mail.Id} ({mail.Subject})");

        if (mail.RewardPence > 0 && !mail.RewardClaimed)
        {
            progress.CashPennies += mail.RewardPence;
            awardedPence = mail.RewardPence;
            cashAfter = progress.CashPennies;
            mail.RewardClaimed = true;

            if (!string.IsNullOrWhiteSpace(mail.RelatedContractId) &&
                !progress.HasCompleted(mail.RelatedContractId))
            {
                progress.CompletedContractIds.Add(mail.RelatedContractId);
                CareerLog.Info($"Contract completed: {mail.RelatedContractId}");
            }

            var submission = progress.Submissions
                .FirstOrDefault(s => s.Id == mail.RelatedSubmissionId);

            if (submission != null)
                submission.Status = SubmissionStatus.Completed;

            CareerLog.Info($"Claimed reward: {awardedPence}p");
            CareerLog.Info($"Cash: {cashBefore} -> {cashAfter}");
        }

        SaveProgress(progress);
        CareerLog.Info($"Unread mail count: {progress.Mail.Count(m => !m.Read)}");
        return true;
    }

    /// <summary>Compatibility overload.</summary>
    public bool OpenMail(string mailId, out int awardedPence) =>
        OpenMail(mailId, out awardedPence, out _, out _);

    public SubmissionRecord? GetLatestSubmission(
        string contractId,
        PlayerProgress? progress = null)
    {
        progress ??= LoadProgress();
        return progress.Submissions
            .LastOrDefault(s => s.ContractId == contractId);
    }

    public static string FormatLetter(
        string template,
        string title,
        int rewardPence)
    {
        var text = template
            .Replace("{TITLE}", title, StringComparison.OrdinalIgnoreCase)
            .Replace("{REWARD}", PlayerProgress.FormatPounds(rewardPence), StringComparison.OrdinalIgnoreCase);

        return text;
    }

    public static string DescribeRequirements(SubmissionRequirements requirements)
    {
        var lines = new List<string>();

        if (requirements.ProgramValid)
            lines.Add("PROGRAM VALID");

        if (requirements.RequiredKind.HasValue)
            lines.Add("TYPE " + requirements.RequiredKind.Value.ToString().ToUpperInvariant());

        if (requirements.RequiredGenre.HasValue &&
            requirements.RequiredGenre.Value != GameGenre.None)
        {
            lines.Add("GENRE " + requirements.RequiredGenre.Value.ToString().ToUpperInvariant());
        }

        if (requirements.AllowedGenres is { Length: > 0 })
        {
            lines.Add("GENRE " + string.Join("/",
                requirements.AllowedGenres.Select(g => g.ToString().ToUpperInvariant())));
        }

        if (requirements.RequiredPlayers.HasValue)
        {
            lines.Add(requirements.RequiredPlayers.Value switch
            {
                TapePlayers.TwoNetwork => "2 PLAYER NETWORK",
                TapePlayers.TwoLocal => "2 PLAYER LOCAL",
                _ => "1 PLAYER"
            });
        }

        foreach (SoftwareCapability flag in Enum.GetValues<SoftwareCapability>())
        {
            if (flag == SoftwareCapability.None)
                continue;

            if ((requirements.RequiredCapabilities & flag) != 0)
                lines.Add(flag.ToString().ToUpperInvariant());
        }

        if (requirements.AnyOfCapabilities is { Length: > 0 })
        {
            var parts = requirements.AnyOfCapabilities
                .Select(c => c.ToString().ToUpperInvariant().Replace(", ", "+"));
            lines.Add(string.Join(" OR ", parts));
        }

        if (requirements.RequiresCustomCover)
            lines.Add("CUSTOM COVER");

        if (requirements.MaximumLineCount > 0)
            lines.Add($"MAXIMUM {requirements.MaximumLineCount} LINES");

        if (requirements.MinimumLineCount > 0)
            lines.Add($"MINIMUM {requirements.MinimumLineCount} LINES");

        if (requirements.MaximumStatementCount > 0)
            lines.Add($"MAXIMUM {requirements.MaximumStatementCount} STATEMENTS");

        return string.Join("\n", lines);
    }
}
