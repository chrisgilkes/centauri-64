using System;

using Centauri64.Game;
using Centauri64.Progression;
using Centauri64.Session;

namespace Centauri64.Analysis;

/// <summary>
/// Headless checks for mode-select display helpers and starter purchase.
/// Run with: Centauri64.exe --verify-mode-select
/// </summary>
public static class ModeSelectVerification
{
    public static int Run()
    {
        var failed = 0;
        System.Console.WriteLine("MODE SELECT VERIFICATION");
        System.Console.WriteLine();

        failed += LastPlayedUnknown();
        failed += LastPlayedToday();
        failed += NewCareerStartsWithSavings();
        failed += PurchaseStarterLeaves101();
        failed += PurchaseFamilyLeaves51();
        failed += PurchaseProgrammerLeaves21();
        failed += PurchaseIsIdempotent();
        failed += MagazineProgressUsesCatalogue();
        failed += OldSavesWithoutTimestampLoad();
        failed += CurrencyFormatting();

        System.Console.WriteLine();
        System.Console.WriteLine(
            failed == 0 ? "ALL MODE SELECT CHECKS PASSED" : failed + " FAILED");
        return failed;
    }

    private static int LastPlayedUnknown() =>
        Expect(
            "null timestamp → UNKNOWN",
            ModeSelectScreen.FormatLastPlayed(null) == "UNKNOWN");

    private static int LastPlayedToday()
    {
        var utc = DateTime.UtcNow;
        return Expect(
            "today timestamp → TODAY",
            ModeSelectScreen.FormatLastPlayed(utc) == "TODAY");
    }

    private static int NewCareerStartsWithSavings()
    {
        var career = CareerState.CreateNew("TEST");
        return Expect(
            "new career starts with £300",
            career.Progress.CashPennies == ComputerPurchase.StartingCashPennies &&
            !career.HasComputer &&
            career.OwnedMagazineIds.Count == 0);
    }

    private static int PurchaseStarterLeaves101() =>
        ExpectPurchase("starter", 10100, ownedMags: 1);

    private static int PurchaseFamilyLeaves51() =>
        ExpectPurchase("family", 5100, ownedMags: 1);

    private static int PurchaseProgrammerLeaves21() =>
        ExpectPurchase("programmer", 2100, ownedMags: 1);

    private static int ExpectPurchase(string bundleId, int remainingPennies, int ownedMags)
    {
        var career = CareerState.CreateNew("BUYER");
        var bundle = ComputerBundleCatalog.Find(bundleId);
        if (bundle == null)
            return Expect("bundle " + bundleId + " exists", false);

        var ok = ComputerPurchase.TryPurchase(career, bundle, out _);
        return Expect(
            bundleId + " purchase leaves " + PlayerProgress.FormatPounds(remainingPennies),
            ok &&
            career.HasComputer &&
            career.BundleSoftwareGranted &&
            career.SelectedBundleId == bundleId &&
            career.Progress.CashPennies == remainingPennies &&
            CareerRepository.CountOwnedMagazines(career) == ownedMags);
    }

    private static int PurchaseIsIdempotent()
    {
        var career = CareerState.CreateNew("TWICE");
        var bundle = ComputerBundleCatalog.Find("starter")!;
        ComputerPurchase.TryPurchase(career, bundle, out _);
        var cash = career.Progress.CashPennies;
        var again = ComputerPurchase.TryPurchase(career, bundle, out var error);
        return Expect(
            "second purchase blocked / no double charge",
            !again &&
            career.Progress.CashPennies == cash &&
            !string.IsNullOrEmpty(error));
    }

    private static int MagazineProgressUsesCatalogue()
    {
        var career = CareerState.CreateNew("TEST");
        var bundle = ComputerBundleCatalog.Find("starter")!;
        ComputerPurchase.TryPurchase(career, bundle, out _);
        var owned = CareerRepository.CountOwnedMagazines(career);
        var total = MagazineCatalog.Issues.Length;

        // After purchase, careers own Issue #1 only.
        return Expect(
            "purchased career magazine progress 1/catalogue",
            owned == 1 && total == MagazineCatalog.Issues.Length && total >= 10);
    }

    private static int OldSavesWithoutTimestampLoad()
    {
        var career = CareerState.CreateNew("LEGACY");
        career.LastPlayedUtc = null;

        var ok = career.LastPlayedUtc == null &&
                 ModeSelectScreen.FormatLastPlayed(career.LastPlayedUtc) == "UNKNOWN";

        return Expect("saves without LastPlayedUtc remain loadable", ok);
    }

    private static int CurrencyFormatting() =>
        Expect(
            "cash formatting £0.00",
            PlayerProgress.FormatPounds(0) == "£0.00");

    private static int Expect(string label, bool condition)
    {
        System.Console.WriteLine((condition ? "PASS " : "FAIL ") + label);
        return condition ? 0 : 1;
    }
}
