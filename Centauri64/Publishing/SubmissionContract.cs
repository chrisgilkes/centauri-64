namespace Centauri64.Publishing;

public sealed class SubmissionContract
{
    public string Id { get; init; } = string.Empty;

    public string PublisherId { get; init; } = string.Empty;

    public string Title { get; init; } = string.Empty;

    public string Summary { get; init; } = string.Empty;

    public SubmissionRequirements Requirements { get; init; } = new();

    /// <summary>
    /// Reward in pennies (£2.00 = 200).
    /// </summary>
    public int RewardPennies { get; init; }

    public bool Repeatable { get; init; }
}
