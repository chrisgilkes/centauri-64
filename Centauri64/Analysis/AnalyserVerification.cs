using System;
using System.IO;
using System.Linq;

using Centauri64.Basic;
using Centauri64.Progression;
using Centauri64.Publishing;

namespace Centauri64.Analysis;

/// <summary>
/// Headless smoke checks for analyser + career loop.
/// Run with: Centauri64.exe --verify-analyser
/// </summary>
public static class AnalyserVerification
{
    public static int Run()
    {
        var failed = 0;
        var analyser = new SoftwareAnalyser();
        var career = new CareerService();
        var programs = new ProgramStorage();

        System.Console.WriteLine("SOFTWARE ANALYSER + CAREER VERIFICATION");
        System.Console.WriteLine();

        // Isolate progress for this run.
        ResetProgress();

        SyncSeed("HELLO");
        SyncSeed("PONG");
        SyncSeed("ADVENTURE");
        ClearCover("PONG");
        ClearCover("HELLO");

        // --- Cover detection ---
        const string coverTape = "COVERTEST";
        SyncNamedFrom("HELLO", coverTape);
        ClearCover(coverTape);

        var noCover = analyser.AnalyseTape(coverTape);
        failed += Expect("1 no cover → CUSTOM NO", !noCover.HasCustomCover && !noCover.HasCover);

        WriteBlankCoverFile(coverTape);
        var blank = analyser.AnalyseTape(coverTape);
        failed += Expect("2 blank → CUSTOM NO", blank.HasCover && !blank.HasCustomCover);

        EnsureCover(coverTape, 1, 7);
        var one = analyser.AnalyseTape(coverTape);
        failed += Expect("3 one pixel → CUSTOM NO", !one.HasCustomCover);
        failed += Expect("3 coverage > 0", one.CoverCoveragePercent > 0);

        EnsureCover(coverTape, 12, 1);
        failed += Expect("4 simple → CUSTOM YES", analyser.AnalyseTape(coverTape).HasCustomCover);

        EnsureCover(coverTape, 400, 7);
        PaintExtraColour(coverTape, 3, 50);
        failed += Expect("5 detailed → CUSTOM YES", analyser.AnalyseTape(coverTape).HasCustomCover);
        ClearCover(coverTape);
        DeleteTape(coverTape);

        // --- HELLO career loop ---
        var hello = analyser.AnalyseTape("HELLO");
        failed += Expect("HELLO valid", hello.ProgramValid);
        failed += Expect("HELLO text", hello.UsesText);

        var first = PublisherCatalog.GetContract("career_first_program")!;
        var helloLabel = programs.LoadLabel("HELLO");
        var firstEval = career.Evaluate(first, hello, helloLabel);
        failed += Expect("HELLO qualifies first program", firstEval.Accepted);

        failed += Expect(
            "submit HELLO",
            career.TrySubmit(first, "HELLO", helloLabel, hello, out _));

        var afterSubmit = career.LoadProgress();
        failed += Expect("cash still 0 after submit", afterSubmit.CashPennies == 0);
        failed += Expect(
            "pending submission",
            afterSubmit.Submissions.Any(s => s.ContractId == first.Id &&
                                             s.Status == SubmissionStatus.Pending));

        var delivered = career.DeliverPendingResponses();
        failed += Expect("mail delivered", delivered == 1);

        var withMail = career.LoadProgress();
        var mail = withMail.Mail.Single();
        failed += Expect("mail unread", !mail.Read);
        failed += Expect("mail reward 200", mail.RewardPence == 200);

        failed += Expect("open mail", career.OpenMail(mail.Id, out var awarded));
        failed += Expect("awarded £2", awarded == 200);

        var afterClaim = career.LoadProgress();
        failed += Expect("cash £2", afterClaim.CashPennies == 200);
        failed += Expect("first completed", afterClaim.HasCompleted(first.Id));

        var again = career.Evaluate(first, hello, helloLabel);
        failed += Expect("no second £2", again.AlreadyCompleted && !again.Accepted);

        // Reload persistence
        var reloaded = career.LoadProgress();
        failed += Expect("cash persists", reloaded.CashPennies == 200);
        failed += Expect("completion persists", reloaded.HasCompleted(first.Id));

        // --- Line limit ---
        var adventureLimit = PublisherCatalog.GetContract("adventure_1000_lines")!;
        failed += Expect(
            "900 lines under limit",
            EvaluateLineCount(adventureLimit, 900).Accepted ||
            EvaluateLineCount(adventureLimit, 900).Checks.Any(c =>
                c.Label.Contains("MAXIMUM") && c.Passed));
        var over = EvaluateLineCount(adventureLimit, 1001);
        failed += Expect(
            "1001 lines over limit",
            over.Checks.Any(c => c.Label.Contains("MAXIMUM") && !c.Passed));

        // --- Adventure (no graphics) ---
        EnsureCover("ADVENTURE", 20, 1);
        var adventure = analyser.AnalyseTape("ADVENTURE");
        failed += Expect("adventure strings+input", adventure.UsesStrings && adventure.UsesInput);
        failed += Expect("adventure no sprites needed", !adventure.UsesSprites);

        // Unlock path: complete prerequisites manually for adventure contract visibility test
        CompleteThrough("career_first_game");
        var adventureContract = PublisherCatalog.GetContract("adventure_first_adventure")!;
        var adventureLabel = programs.LoadLabel("ADVENTURE");
        var adventureEval = career.Evaluate(adventureContract, adventure, adventureLabel);
        failed += Expect("adventure qualifies", adventureEval.Accepted);

        // --- PONG without cover ---
        ClearCover("PONG");
        var pongBare = analyser.AnalyseTape("PONG");
        var network = PublisherCatalog.GetContract("technical_network_game")!;
        var pongLabel = programs.LoadLabel("PONG");
        var bareEval = career.Evaluate(network, pongBare, pongLabel);
        failed += Expect("PONG bare rejected", !bareEval.Accepted);
        failed += Expect(
            "PONG bare cover fail",
            bareEval.FailedChecks.Any(c => c.Label.Contains("COVER")));

        // --- PONG with cover ---
        EnsureCover("PONG", 40, 7);
        var pong = analyser.AnalyseTape("PONG");
        failed += Expect("PONG networking", pong.UsesNetworking && pong.UsesInput);
        var packedEval = career.Evaluate(network, pong, pongLabel);
        failed += Expect("PONG qualifies network", packedEval.Accepted);

        failed += Expect(
            "submit PONG",
            career.TrySubmit(network, "PONG", pongLabel, pong, out _));
        career.DeliverPendingResponses();
        var pongMail = career.LoadProgress().Mail
            .First(m => m.RelatedContractId == network.Id);
        career.OpenMail(pongMail.Id, out var pongPay);
        failed += Expect("PONG pay £25", pongPay == 2500);

        var finalCash = career.LoadProgress().CashPennies;
        // first £2 + unlocked path may have claimed more if CompleteThrough claimed; we only claim first+pong
        // CompleteThrough only marks completed ids without cash.
        failed += Expect("cash £27", finalCash == 2700);

        ClearCover("PONG");

        System.Console.WriteLine();
        System.Console.WriteLine(failed == 0 ? "ALL CHECKS PASSED" : $"FAILED: {failed}");
        return failed == 0 ? 0 : 1;
    }

    private static SubmissionResult EvaluateLineCount(
        SubmissionContract contract,
        int lineCount)
    {
        var analysis = new SoftwareAnalysis
        {
            ProgramValid = true,
            LineCount = lineCount,
            StatementCount = lineCount,
            Capabilities =
                SoftwareCapability.Text |
                SoftwareCapability.Input |
                SoftwareCapability.Strings,
            HasCover = true,
            HasCustomCover = true,
            CoverChangedPixelCount = 20,
            CoverColourCount = 1
        };

        var label = new TapeLabel
        {
            Kind = TapeKind.Game,
            Genre = GameGenre.Adventure
        };

        return SubmissionEvaluator.Evaluate(contract.Requirements, analysis, label);
    }

    private static void CompleteThrough(string contractId)
    {
        var career = new CareerService();
        var progress = career.LoadProgress();
        var chain = new[]
        {
            "career_first_program",
            "career_interactive_program",
            "career_graphical_program",
            "career_first_game"
        };

        foreach (var id in chain)
        {
            if (!progress.HasCompleted(id))
                progress.CompletedContractIds.Add(id);

            if (id == contractId)
                break;
        }

        career.SaveProgress(progress);
    }

    private static void ResetProgress()
    {
        var path = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Centauri64",
            "player.json");

        if (File.Exists(path))
            File.Delete(path);
    }

    private static int Expect(string label, bool condition)
    {
        System.Console.WriteLine((condition ? "  OK  " : " FAIL ") + label);
        return condition ? 0 : 1;
    }

    private static void SyncSeed(string name)
    {
        var sourceDir = Path.Combine(AppContext.BaseDirectory, "Programs");
        var destDir = TapeFolder.Location;
        Directory.CreateDirectory(destDir);

        foreach (var extension in new[] { ".bas", ".tape", ".cover", ".sprites", ".maps" })
        {
            var source = Path.Combine(sourceDir, name + extension);
            var destination = Path.Combine(destDir, name + extension);

            if (File.Exists(source))
            {
                File.Copy(source, destination, overwrite: true);
                continue;
            }

            if (extension == ".cover" && File.Exists(destination))
                File.Delete(destination);
        }
    }

    private static void SyncNamedFrom(string sourceName, string destName)
    {
        var dir = TapeFolder.Location;
        File.Copy(
            Path.Combine(dir, sourceName + ".bas"),
            Path.Combine(dir, destName + ".bas"),
            overwrite: true);

        var tapeSource = Path.Combine(dir, sourceName + ".tape");
        if (File.Exists(tapeSource))
            File.Copy(tapeSource, Path.Combine(dir, destName + ".tape"), overwrite: true);

        ClearCover(destName);
    }

    private static void DeleteTape(string name)
    {
        var dir = TapeFolder.Location;
        foreach (var extension in new[] { ".bas", ".tape", ".cover", ".sprites", ".maps" })
        {
            var path = Path.Combine(dir, name + extension);
            if (File.Exists(path))
                File.Delete(path);
        }
    }

    private static void ClearCover(string name) =>
        new ProgramStorage().DeleteCover(name);

    private static void WriteBlankCoverFile(string name) =>
        new ProgramStorage().SaveCover(name, new TapeCover());

    private static void EnsureCover(string name, int changedPixels, int colour)
    {
        var cover = new TapeCover();
        var written = 0;

        for (var y = 0; y < TapeCover.Height && written < changedPixels; y++)
        {
            for (var x = 0; x < TapeCover.Width && written < changedPixels; x++)
            {
                cover.Pixels[y, x] = colour;
                written++;
            }
        }

        new ProgramStorage().SaveCover(name, cover);
    }

    private static void PaintExtraColour(string name, int colour, int count)
    {
        var storage = new ProgramStorage();
        var cover = storage.LoadCover(name);
        var written = 0;

        for (var y = TapeCover.Height - 1; y >= 0 && written < count; y--)
        {
            for (var x = TapeCover.Width - 1; x >= 0 && written < count; x--)
            {
                if (cover.Pixels[y, x] != 0)
                    continue;

                cover.Pixels[y, x] = colour;
                written++;
            }
        }

        storage.SaveCover(name, cover);
    }
}
