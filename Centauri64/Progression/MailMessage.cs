namespace Centauri64.Progression;

public enum SubmissionStatus
{
    Pending,
    ResponseReady,
    Accepted,
    Rejected,
    Completed
}

public sealed class SubmissionRecord
{
    public string Id { get; set; } = string.Empty;

    public string ContractId { get; set; } = string.Empty;

    public string OrganisationId { get; set; } = string.Empty;

    public string TapeName { get; set; } = string.Empty;

    public string SoftwareTitle { get; set; } = string.Empty;

    public int RewardPence { get; set; }

    public SubmissionStatus Status { get; set; } = SubmissionStatus.Pending;

    public bool WasAccepted { get; set; } = true;
}

public sealed class MailMessage
{
    public string Id { get; set; } = string.Empty;

    public string FromOrganisationId { get; set; } = string.Empty;

    public string Subject { get; set; } = string.Empty;

    public string Body { get; set; } = string.Empty;

    public bool Read { get; set; }

    public int RewardPence { get; set; }

    public bool RewardClaimed { get; set; }

    public string RelatedContractId { get; set; } = string.Empty;

    public string RelatedSubmissionId { get; set; } = string.Empty;
}
