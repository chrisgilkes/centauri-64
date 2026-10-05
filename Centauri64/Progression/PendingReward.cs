namespace Centauri64.Progression;

/// <summary>
/// Accepted submission waiting for mail/payment delivery.
/// Cash is not awarded until a later mail task consumes this.
/// </summary>
public sealed class PendingReward
{
    public string ContractId { get; set; } = string.Empty;

    public string PublisherName { get; set; } = string.Empty;

    public string ContractTitle { get; set; } = string.Empty;

    public string TapeName { get; set; } = string.Empty;

    public int AmountPennies { get; set; }
}
