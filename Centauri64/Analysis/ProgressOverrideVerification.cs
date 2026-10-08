using Centauri64.Session;

namespace Centauri64.Analysis;

/// <summary>
/// Headless checks for non-destructive career progression override.
/// Run with: Centauri64.exe --verify-progress-override
/// </summary>
public static class ProgressOverrideVerification
{
    public static int Run()
    {
        var failed = 0;
        System.Console.WriteLine("PROGRESS OVERRIDE VERIFICATION");
        System.Console.WriteLine();

        failed += PreComputerHasNoMachine();
        failed += StartIssue2HasGraphicsNotSprites();
        failed += CompleteIssue3HasSprites();
        failed += MovingBackRelocks();
        failed += DoesNotMutateSavedCareer();
        failed += DisableRestoresSavedFeatures();

        System.Console.WriteLine();
        System.Console.WriteLine(
            failed == 0 ? "ALL PROGRESS OVERRIDE CHECKS PASSED" : failed + " FAILED");
        return failed;
    }

    private static int PreComputerHasNoMachine()
    {
        var sim = CareerProgressOverride.Build(0, ProgressionStage.StartOfIssue);
        return Expect(
            "issue 0 = pre-computer",
            !sim.HasComputer &&
            !sim.HasFeature(FeatureId.Graphics) &&
            sim.OwnedMagazineIds.Count == 0);
    }

    private static int StartIssue2HasGraphicsNotSprites()
    {
        var sim = CareerProgressOverride.Build(2, ProgressionStage.StartOfIssue);
        var issue2 = MagazineCatalog.FindByNumber(2)!;
        var state = MagazineProgression.StateOf(sim, issue2, false);
        return Expect(
            "start issue 2: owns #1, #2 on sale, no graphics yet",
            sim.HasComputer &&
            MagazineProgression.HighestOwnedNumber(sim) == 1 &&
            state == MagazineIssueState.OnSale &&
            !sim.HasFeature(FeatureId.Graphics) &&
            !sim.HasFeature(FeatureId.Sprites));
    }

    private static int CompleteIssue3HasSprites()
    {
        var sim = CareerProgressOverride.Build(3, ProgressionStage.IssueCompleted);
        return Expect(
            "complete issue 3: sprites unlocked",
            sim.HasFeature(FeatureId.Graphics) &&
            sim.HasFeature(FeatureId.Sprites) &&
            MagazineProgression.HighestOwnedNumber(sim) == 3);
    }

    private static int MovingBackRelocks()
    {
        var high = CareerProgressOverride.Build(5, ProgressionStage.IssueCompleted);
        var low = CareerProgressOverride.Build(2, ProgressionStage.StartOfIssue);
        return Expect(
            "lower stage re-locks later features",
            high.HasFeature(FeatureId.Images) &&
            !low.HasFeature(FeatureId.Images) &&
            !low.HasFeature(FeatureId.Sprites) &&
            !low.HasFeature(FeatureId.Graphics));
    }

    private static int DoesNotMutateSavedCareer()
    {
        var saved = CareerState.CreateNew("SAVE");
        saved.HasComputer = true;
        saved.BundleSoftwareGranted = true;
        MagazineProgression.OwnThrough(saved, 1, grantCoverSoftware: false);
        var cash = saved.Progress.CashPennies;
        var owned = saved.OwnedMagazineIds.Count;
        var features = saved.UnlockedFeatures.Count;

        GameSession.EnterBedroom(0, saved);
        GameSession.ApplyProgressOverride(true, 5, ProgressionStage.IssueCompleted);

        var unchanged =
            saved.Progress.CashPennies == cash &&
            saved.OwnedMagazineIds.Count == owned &&
            saved.UnlockedFeatures.Count == features &&
            !saved.HasFeature(FeatureId.Images) &&
            FeatureGate.Current.IsAvailable(FeatureId.Images);

        GameSession.Clear();
        return Expect("override does not mutate saved career", unchanged);
    }

    private static int DisableRestoresSavedFeatures()
    {
        var saved = CareerState.CreateNew("REAL");
        saved.HasComputer = true;
        saved.BundleSoftwareGranted = true;
        MagazineProgression.OwnThrough(saved, 1, grantCoverSoftware: false);

        GameSession.EnterBedroom(0, saved);
        GameSession.ApplyProgressOverride(true, 3, ProgressionStage.IssueCompleted);
        var overridden = FeatureGate.Current.IsAvailable(FeatureId.Sprites);

        GameSession.ApplyProgressOverride(false, 0, ProgressionStage.StartOfIssue);
        var restored = !FeatureGate.Current.IsAvailable(FeatureId.Sprites) &&
                       FeatureGate.Current.IsAvailable(FeatureId.CoreBasic);

        GameSession.Clear();
        return Expect("disable restores saved feature gates", overridden && restored);
    }

    private static int Expect(string label, bool condition)
    {
        System.Console.WriteLine((condition ? "PASS " : "FAIL ") + label);
        return condition ? 0 : 1;
    }
}
