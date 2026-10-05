namespace Centauri64.Publishing;

/// <summary>
/// Legacy alias — prefer <see cref="OrganisationDefinition"/>.
/// </summary>
public sealed class SoftwarePublisher
{
    public string Id { get; init; } = string.Empty;

    public string Name { get; init; } = string.Empty;

    public string ShortDescription { get; init; } = string.Empty;

    public string AcceptanceMessage { get; init; } = string.Empty;

    public string RejectionMessage { get; init; } = string.Empty;

    public bool IsMagazine { get; init; }
}
