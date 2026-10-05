using System.Collections.Generic;

namespace Centauri64.Progression;

public sealed class PlayerProgress
{
    /// <summary>
    /// Cash balance in pennies (£2.00 = 200).
    /// </summary>
    public int CashPennies { get; set; }

    public List<string> CompletedContractIds { get; set; } = new();

    public List<PendingReward> PendingRewards { get; set; } = new();

    public static string FormatPounds(int pennies)
    {
        var pounds = pennies / 100;
        var pence = System.Math.Abs(pennies % 100);
        return $"£{pounds}.{pence:00}";
    }

    public bool HasCompleted(string contractId) =>
        CompletedContractIds.Contains(contractId);
}
