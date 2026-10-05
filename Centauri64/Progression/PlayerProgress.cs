using System.Collections.Generic;
using System.Linq;

namespace Centauri64.Progression;

public sealed class PlayerProgress
{
    /// <summary>
    /// Cash balance in pennies (£2.00 = 200).
    /// </summary>
    public int CashPennies { get; set; }

    public List<string> CompletedContractIds { get; set; } = new();

    public List<SubmissionRecord> Submissions { get; set; } = new();

    public List<MailMessage> Mail { get; set; } = new();

    /// <summary>
    /// Legacy field kept for older player.json files.
    /// Migrated into Submissions/Mail on load.
    /// </summary>
    public List<PendingReward> PendingRewards { get; set; } = new();

    public static string FormatPounds(int pennies)
    {
        var pounds = pennies / 100;
        var pence = System.Math.Abs(pennies % 100);
        return $"£{pounds}.{pence:00}";
    }

    public bool HasCompleted(string contractId) =>
        CompletedContractIds.Contains(contractId);

    public bool HasUnreadMail =>
        Mail.Any(message => !message.Read);

    public bool HasUnclaimedReward =>
        Mail.Any(message => message.RewardPence > 0 && !message.RewardClaimed);

    public bool HasPendingSubmission(string contractId) =>
        Submissions.Any(submission =>
            submission.ContractId == contractId &&
            submission.Status is SubmissionStatus.Pending or SubmissionStatus.ResponseReady);

    public void MigrateLegacyRewards()
    {
        if (PendingRewards.Count == 0)
            return;

        foreach (var reward in PendingRewards)
        {
            if (Submissions.Any(s =>
                    s.ContractId == reward.ContractId &&
                    s.TapeName == reward.TapeName))
            {
                continue;
            }

            var submissionId = System.Guid.NewGuid().ToString("N");
            Submissions.Add(new SubmissionRecord
            {
                Id = submissionId,
                ContractId = reward.ContractId,
                OrganisationId = string.Empty,
                TapeName = reward.TapeName,
                SoftwareTitle = reward.TapeName,
                RewardPence = reward.AmountPennies,
                Status = SubmissionStatus.ResponseReady,
                WasAccepted = true
            });

            Mail.Add(new MailMessage
            {
                Id = System.Guid.NewGuid().ToString("N"),
                FromOrganisationId = string.Empty,
                Subject = "RE: YOUR SOFTWARE SUBMISSION",
                Body = reward.PublisherName + "\n\nPAYMENT ENCLOSED:\n\n          " +
                       FormatPounds(reward.AmountPennies),
                Read = false,
                RewardPence = reward.AmountPennies,
                RewardClaimed = false,
                RelatedContractId = reward.ContractId,
                RelatedSubmissionId = submissionId
            });
        }

        PendingRewards.Clear();
    }
}
