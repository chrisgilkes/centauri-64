using System;

namespace Centauri64.Session;

/// <summary>
/// First Centauri64 bundle purchase for a Bedroom career.
/// Idempotent: repeated calls do not double-charge or re-grant.
/// </summary>
public static class ComputerPurchase
{
    public const int StartingCashPennies = 30000;

    public static string MartinThanks(ComputerBundle bundle) =>
        bundle.Id.ToLowerInvariant() switch
        {
            "starter" => "GOOD CHOICE! PLENTY LEFT OVER FOR A FEW GAMES.",
            "family" => "THE FAMILY PACK! THAT SHOULD KEEP EVERYONE BUSY.",
            "programmer" => "A PROGRAMMER, ARE WE? GOOD LAD. DON'T FORGET TO SAVE YOUR WORK!",
            _ => "NICE ONE. GET THAT MACHINE HOME AND SWITCH IT ON!"
        };

    /// <summary>
    /// Deducts price, marks the computer owned, grants software + Issue #1 magazine.
    /// </summary>
    public static bool TryPurchase(CareerState career, ComputerBundle bundle, out string error)
    {
        error = string.Empty;

        if (career.HasComputer || career.BundleSoftwareGranted)
        {
            error = "YOU ALREADY OWN A CENTAURI64.";
            return false;
        }

        if (bundle.PricePennies <= 0)
        {
            error = "INVALID PACKAGE.";
            return false;
        }

        if (career.Progress.CashPennies < bundle.PricePennies)
        {
            error = "NOT ENOUGH MONEY.";
            return false;
        }

        career.Progress.CashPennies -= bundle.PricePennies;
        career.SelectedBundleId = bundle.Id;
        career.HasComputer = true;

        SoftwareGrant.GrantTapes(bundle.SoftwareTapes);
        career.BundleSoftwareGranted = true;

        // Every bundle includes the introductory Issue #1 magazine.
        MagazineProgression.EnsureStartingIssue(career);

        return true;
    }
}
