using System;
using System.IO;
using System.Linq;

using Centauri64.Basic;
using Centauri64.Publishing;
using Centauri64.Progression;

namespace Centauri64.Analysis;

/// <summary>
/// Headless smoke checks for the software analyser and submission rules.
/// Run with: Centauri64.exe --verify-analyser
/// </summary>
public static class AnalyserVerification
{
    public static int Run()
    {
        var failed = 0;
        var analyser = new SoftwareAnalyser();
        var submissions = new SubmissionService();
        var programs = new ProgramStorage();

        System.Console.WriteLine("SOFTWARE ANALYSER VERIFICATION");
        System.Console.WriteLine();

        SyncSeed("HELLO");
        SyncSeed("PONG");
        SyncSeed("ADVENTURE");

        // Shipped PONG has no cover — remove any leftover AppData cover.
        ClearCover("PONG");
        ClearCover("HELLO");

        var hello = analyser.AnalyseTape("HELLO");
        failed += Expect("HELLO valid", hello.ProgramValid);
        failed += Expect("HELLO lines=2", hello.LineCount == 2);
        failed += Expect("HELLO text", hello.UsesText);
        failed += Expect("HELLO no input", !hello.UsesInput);
        failed += Expect("HELLO no graphics", !hello.UsesGraphics);
        failed += Expect("HELLO no sprites", !hello.UsesSprites);
        failed += Expect("HELLO no sound", !hello.UsesSound);
        failed += Expect("HELLO no maps", !hello.UsesMaps);
        failed += Expect("HELLO no networking", !hello.UsesNetworking);
        failed += Expect("HELLO no cover", !hello.HasCover);
        failed += Expect("HELLO not custom cover", !hello.HasCustomCover);

        var first = PublisherCatalog.GetContract("cu-first-program")!;
        var helloLabel = programs.LoadLabel("HELLO");
        var firstResult = submissions.Evaluate(first, hello, helloLabel);
        failed += Expect("HELLO first program accepted", firstResult.Accepted);

        // Cover detection cases on a scratch name so demos stay clean.
        const string coverTape = "COVERTEST";
        SyncNamedFrom("HELLO", coverTape);

        ClearCover(coverTape);
        var noCover = analyser.AnalyseTape(coverTape);
        failed += Expect("1 no cover → CUSTOM NO", !noCover.HasCustomCover && !noCover.HasCover);
        failed += Expect("1 no cover → metrics 0", noCover.CoverChangedPixelCount == 0);

        WriteBlankCoverFile(coverTape);
        var blank = analyser.AnalyseTape(coverTape);
        failed += Expect("2 blank cover → PRESENT YES", blank.HasCover);
        failed += Expect("2 blank cover → CUSTOM NO", !blank.HasCustomCover);
        failed += Expect("2 blank cover → pixels 0", blank.CoverChangedPixelCount == 0);

        EnsureCover(coverTape, changedPixels: 1, colour: 7);
        var onePixel = analyser.AnalyseTape(coverTape);
        failed += Expect("3 one pixel → CUSTOM NO", !onePixel.HasCustomCover);
        failed += Expect("3 one pixel → PRESENT YES", onePixel.HasCover);
        failed += Expect(
            "3 one pixel → coverage > 0",
            onePixel.CoverCoveragePercent > 0 && onePixel.CoverCoveragePercent < 1);

        EnsureCover(coverTape, changedPixels: 12, colour: 1);
        var simple = analyser.AnalyseTape(coverTape);
        failed += Expect("4 simple cover → CUSTOM YES", simple.HasCustomCover);
        failed += Expect("4 simple cover → 1 colour", simple.CoverColourCount == 1);

        EnsureCover(coverTape, changedPixels: 400, colour: 7);
        PaintExtraColour(coverTape, colour: 3, count: 50);
        var detailed = analyser.AnalyseTape(coverTape);
        failed += Expect("5 detailed cover → CUSTOM YES", detailed.HasCustomCover);
        failed += Expect("5 detailed cover → colours > 1", detailed.CoverColourCount > 1);

        ClearCover(coverTape);
        DeleteTape(coverTape);

        // Adventure with a real saved custom cover for Quill.
        EnsureCover("ADVENTURE", changedPixels: 20, colour: 1);
        var adventure = analyser.AnalyseTape("ADVENTURE");
        failed += Expect("ADVENTURE strings", adventure.UsesStrings);
        failed += Expect("ADVENTURE input", adventure.UsesInput);
        failed += Expect("ADVENTURE no sprites", !adventure.UsesSprites);
        failed += Expect("ADVENTURE custom cover", adventure.HasCustomCover);

        var quill = PublisherCatalog.GetContract("quill-adventures")!;
        var adventureLabel = programs.LoadLabel("ADVENTURE");
        var quillResult = submissions.Evaluate(quill, adventure, adventureLabel);
        failed += Expect("ADVENTURE quill accepted", quillResult.Accepted);

        // PONG with no cover must fail Vector Crown on packaging only.
        ClearCover("PONG");
        var pong = analyser.AnalyseTape("PONG");
        failed += Expect("PONG input", pong.UsesInput);
        failed += Expect("PONG networking", pong.UsesNetworking);
        failed += Expect("PONG no cover file", !pong.HasCover);
        failed += Expect("PONG CUSTOM NO", !pong.HasCustomCover);
        failed += Expect("PONG pixels 0", pong.CoverChangedPixelCount == 0);

        var vector = PublisherCatalog.GetContract("vector-network")!;
        var pongLabel = programs.LoadLabel("PONG");
        var vectorBare = submissions.Evaluate(vector, pong, pongLabel);
        failed += Expect("PONG vector rejected without cover", !vectorBare.Accepted);
        failed += Expect(
            "PONG vector fails cover check",
            vectorBare.FailedChecks.Any(check =>
                check.Label.Contains("COVER", StringComparison.OrdinalIgnoreCase)));

        // With a custom cover, PONG should qualify.
        EnsureCover("PONG", changedPixels: 40, colour: 7);
        var pongPacked = analyser.AnalyseTape("PONG");
        failed += Expect("PONG packed CUSTOM YES", pongPacked.HasCustomCover);
        var vectorPacked = submissions.Evaluate(vector, pongPacked, pongLabel);
        failed += Expect("PONG vector accepted with cover", vectorPacked.Accepted);

        // Leave shipped PONG unpackaged (no cover) after verification.
        ClearCover("PONG");

        var storage = new PlayerProgressStorage();
        var previous = storage.Load();
        try
        {
            var progress = new PlayerProgress
            {
                CashPennies = previous.CashPennies,
                CompletedContractIds = previous.CompletedContractIds.ToList(),
                PendingRewards = previous.PendingRewards.ToList()
            };
            if (!progress.CompletedContractIds.Contains(first.Id))
                progress.CompletedContractIds.Add(first.Id);

            storage.Save(progress);
            var again = submissions.Evaluate(first, hello, helloLabel);
            failed += Expect("first program already completed", again.AlreadyCompleted);
            failed += Expect("first program not re-accepted", !again.Accepted);
        }
        finally
        {
            storage.Save(previous);
        }

        System.Console.WriteLine();
        System.Console.WriteLine(failed == 0 ? "ALL CHECKS PASSED" : $"FAILED: {failed}");
        return failed == 0 ? 0 : 1;
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

            // Seed has no sidecar — remove orphan AppData copies from prior runs.
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
        var tapeDest = Path.Combine(dir, destName + ".tape");
        if (File.Exists(tapeSource))
            File.Copy(tapeSource, tapeDest, overwrite: true);

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

    private static void ClearCover(string name)
    {
        new ProgramStorage().DeleteCover(name);
    }

    private static void WriteBlankCoverFile(string name)
    {
        // Explicit blank saved cover (all colour 0) — distinct from "no cover file".
        new ProgramStorage().SaveCover(name, new TapeCover());
    }

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
