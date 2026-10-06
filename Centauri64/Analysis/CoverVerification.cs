using System;
using System.IO;

using Centauri64.Basic;

namespace Centauri64.Analysis;

/// <summary>
/// Headless checks for the 40x56 tape cover format.
/// Run with: Centauri64.exe --verify-cover
/// </summary>
public static class CoverVerification
{
    public static int Run()
    {
        var failed = 0;
        System.Console.WriteLine("TAPE COVER FORMAT VERIFICATION");
        System.Console.WriteLine();

        failed += Expect("canonical width 40", TapeCover.Width == 40);
        failed += Expect("canonical height 56", TapeCover.Height == 56);

        var storage = new ProgramStorage();
        const string name = "COVERFMT";

        try
        {
            storage.Delete(name);
        }
        catch
        {
            // Tape may not exist yet.
        }

        storage.DeleteCover(name);

        var blank = new TapeCover();
        blank.Pixels[0, 0] = 7;
        blank.Pixels[0, TapeCover.Width - 1] = 8;
        blank.Pixels[TapeCover.Height - 1, 0] = 9;
        blank.Pixels[TapeCover.Height - 1, TapeCover.Width - 1] = 10;
        blank.Pixels[TapeCover.Height / 2, TapeCover.Width / 2] = 15;
        storage.SaveCover(name, blank);

        var reloaded = storage.LoadCover(name);
        failed += Expect("corner 0,0", reloaded.Pixels[0, 0] == 7);
        failed += Expect("corner 39,0", reloaded.Pixels[0, 39] == 8);
        failed += Expect("corner 0,55", reloaded.Pixels[55, 0] == 9);
        failed += Expect("corner 39,55", reloaded.Pixels[55, 39] == 10);
        failed += Expect("centre-ish", reloaded.Pixels[28, 20] == 15);

        var header = File.ReadAllLines(Path.Combine(TapeFolder.Location, "COVERFMT.cover"))[0];
        failed += Expect("save header COVER 40 56", header == "COVER 40 56");

        // Legacy 80x112 → 40x56 downsample (top-left of each 2x2).
        var legacyPath = Path.Combine(TapeFolder.Location, "COVERLEGACY.cover");
        WriteLegacyCover(legacyPath);
        var legacy = storage.LoadCover("COVERLEGACY");
        failed += Expect("legacy downsample 0,0", legacy.Pixels[0, 0] == 3);
        failed += Expect("legacy downsample 1,0", legacy.Pixels[0, 1] == 5);
        failed += Expect("legacy downsample 0,1", legacy.Pixels[1, 0] == 7);
        failed += Expect("legacy no crash", true);

        // Malformed cover → blank, no throw.
        File.WriteAllText(
            Path.Combine(TapeFolder.Location, "COVERBAD.cover"),
            "COVER NOT A SIZE\n1,2,3\n");
        var bad = storage.LoadCover("COVERBAD");
        failed += Expect("malformed yields blank", !bad.HasArt);

        storage.DeleteCover(name);
        storage.DeleteCover("COVERLEGACY");
        storage.DeleteCover("COVERBAD");

        System.Console.WriteLine();
        System.Console.WriteLine(failed == 0 ? "ALL COVER CHECKS PASSED" : failed + " FAILED");
        return failed;
    }

    private static void WriteLegacyCover(string path)
    {
        var lines = new string[TapeCover.LegacyHeight + 1];
        lines[0] = "COVER 80 112";

        for (var y = 0; y < TapeCover.LegacyHeight; y++)
        {
            var row = new string[TapeCover.LegacyWidth];
            for (var x = 0; x < TapeCover.LegacyWidth; x++)
            {
                // Distinct colours in the four corners of the first few 2x2 blocks.
                if (y == 0 && x == 0) row[x] = "3";
                else if (y == 0 && x == 1) row[x] = "4";
                else if (y == 1 && x == 0) row[x] = "4";
                else if (y == 1 && x == 1) row[x] = "4";
                else if (y == 0 && x == 2) row[x] = "5";
                else if (y == 2 && x == 0) row[x] = "7";
                else row[x] = "0";
            }

            lines[y + 1] = string.Join(",", row);
        }

        File.WriteAllLines(path, lines);
    }

    private static int Expect(string label, bool condition)
    {
        System.Console.WriteLine((condition ? "PASS " : "FAIL ") + label);
        return condition ? 0 : 1;
    }
}
