namespace Centauri64.Publishing;

public enum OrganisationType
{
    Magazine,
    SoftwareHouse
}

public enum PrintAdvertStyle
{
    Plain,
    Bold,
    Ornate,
    Technical
}

/// <summary>
/// Fictional magazine or software house.
/// Identity is <see cref="Id"/> — display <see cref="Name"/> may change freely.
///
/// Text ownership:
/// - Tagline / Description = organisation flavour (never response text)
/// - SubmissionReceivedText = shown only after the player confirms sending a tape
/// - AcceptanceBody / RejectionBody = shown only inside opened mail
/// </summary>
public sealed class OrganisationDefinition
{
    public string Id { get; init; } = string.Empty;

    public string Name { get; init; } = string.Empty;

    public string ShortName { get; init; } = string.Empty;

    public OrganisationType Type { get; init; }

    /// <summary>
    /// First Year One magazine issue number at which this house appears
    /// in classifieds. Career magazine ownership drives availability.
    /// </summary>
    public int AvailableFromIssue { get; init; } = 1;

    public PrintAdvertStyle AdvertStyle { get; init; } = PrintAdvertStyle.Plain;

    /// <summary>Centauri palette index used as print spot colour (0–31).</summary>
    public int PrintAccentIndex { get; init; } = 2;

    /// <summary>Optional future arrival copy. Not shown as mail in this phase.</summary>
    public string ArrivalNotice { get; init; } = string.Empty;

    public string Tagline { get; init; } = string.Empty;

    public string Description { get; init; } = string.Empty;

    /// <summary>Shown only after a successful send confirmation.</summary>
    public string SubmissionReceivedText { get; init; } =
        "YOUR TAPE HAS BEEN SENT.\nGOOD LUCK!";

    /// <summary>Letter body only — never shown on contract screens.</summary>
    public string AcceptanceBody { get; init; } =
        "THANK YOU FOR YOUR SUBMISSION.\nWE'D LIKE TO PUBLISH IT.\n\nPAYMENT ENCLOSED:\n{REWARD}";

    /// <summary>Letter body only — never shown on contract screens.</summary>
    public string RejectionBody { get; init; } =
        "THANK YOU FOR YOUR SUBMISSION.\nIT ISN'T QUITE WHAT WE NEED RIGHT NOW.";

    /// <summary>Legacy alias for AcceptanceBody.</summary>
    public string AcceptanceText
    {
        get => AcceptanceBody;
        init => AcceptanceBody = value;
    }

    /// <summary>Legacy alias for RejectionBody.</summary>
    public string RejectionText
    {
        get => RejectionBody;
        init => RejectionBody = value;
    }
}
