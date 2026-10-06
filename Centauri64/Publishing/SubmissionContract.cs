namespace Centauri64.Publishing;

public sealed class SubmissionContract
{
    public string Id { get; init; } = string.Empty;

    public string OrganisationId { get; init; } = string.Empty;

    /// <summary>Legacy alias for OrganisationId.</summary>
    public string PublisherId
    {
        get => OrganisationId;
        init => OrganisationId = value;
    }

    public string Title { get; init; } = string.Empty;

    public string Subtitle { get; init; } = string.Empty;

    /// <summary>
    /// Advert / challenge copy shown on the opportunity screen.
    /// Must never contain acceptance or rejection letter text.
    /// </summary>
    public string AdvertText { get; init; } = string.Empty;

    /// <summary>Legacy alias for AdvertText.</summary>
    public string Description
    {
        get => AdvertText;
        init => AdvertText = value;
    }

    public string Summary
    {
        get => string.IsNullOrWhiteSpace(AdvertText) ? Subtitle : AdvertText;
        init
        {
            if (string.IsNullOrWhiteSpace(AdvertText))
                AdvertText = value;
            else
                Subtitle = value;
        }
    }

    public SubmissionRequirements Requirements { get; init; } = new();

    /// <summary>
    /// Reward in pennies (£2.00 = 200).
    /// </summary>
    public int RewardPennies { get; init; }

    public bool Repeatable { get; init; }

    /// <summary>
    /// Extra magazine-issue gate (1–10). 0 means publisher presence only.
    /// </summary>
    public int AvailableFromIssue { get; init; }

    /// <summary>
    /// Contract IDs that must be completed before this one is available.
    /// </summary>
    public string[] PrerequisiteContractIds { get; init; } =
        System.Array.Empty<string>();
}
