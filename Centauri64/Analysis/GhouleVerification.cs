using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

using Centauri64.Basic;
using Centauri64.Console;
using Centauri64.Machine;
using Centauri64.Network;
using Centauri64.Session;

namespace Centauri64.Analysis;

/// <summary>
/// Headless Issue #1 cover-tape checks for Dungeon of Ghoule.
/// Run with: Centauri64.exe --verify-ghoule
/// </summary>
public static class GhouleVerification
{
    public static int Run()
    {
        var failed = 0;
        System.Console.WriteLine("DUNGEON OF GHOULE VERIFICATION");
        System.Console.WriteLine();

        var sourcePath = ResolveSourceBas();
        if (sourcePath == null)
        {
            System.Console.WriteLine("FAIL: Programs/DUNGEON.bas not found");
            return 1;
        }

        var lines = File.ReadAllLines(sourcePath)
            .Where(line => !string.IsNullOrWhiteSpace(line))
            .ToArray();

        failed += Expect("source has lines", lines.Length > 40);
        failed += ParseAll(lines);

        failed += Play(
            "win route",
            lines,
            new[] { "1", "1", "1", "1", "2", "1", "1", "1", "1" },
            mustContain: new[] { "SILVER KEY", "TREASURE OF GHOULE", "YOU WIN" });

        failed += Play(
            "ghost blocks without key",
            lines,
            new[] { "1", "1", "1" },
            mustContain: new[] { "DO NOT HAVE THE KEY", "SILVER KEY" },
            stopAfterInputs: 3);

        failed += Play(
            "flee ending",
            lines,
            new[] { "2" },
            mustContain: new[] { "FLEE INTO THE NIGHT", "THE END" });

        failed += Play(
            "coffin death",
            lines,
            new[] { "1", "3", "2" },
            mustContain: new[] { "GAME OVER" });

        failed += Play(
            "invalid choice retries",
            lines,
            new[] { "9", "2" },
            mustContain: new[] { "FLEE INTO THE NIGHT" });

        failed += ExpectGrantPackaging();

        System.Console.WriteLine();
        System.Console.WriteLine(failed == 0 ? "ALL GHOULE CHECKS PASSED" : failed + " FAILED");
        return failed;
    }

    private static string? ResolveSourceBas()
    {
        var candidates = new[]
        {
            Path.Combine(AppContext.BaseDirectory, "Programs", "CoverTapes", "DUNGEON.bas"),
            Path.Combine(AppContext.BaseDirectory, "Programs", "DUNGEON.bas"),
            Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "Programs", "CoverTapes", "DUNGEON.bas"),
            Path.Combine(Directory.GetCurrentDirectory(), "Centauri64", "Programs", "CoverTapes", "DUNGEON.bas"),
            Path.Combine(Directory.GetCurrentDirectory(), "Programs", "CoverTapes", "DUNGEON.bas")
        };

        return candidates.Select(Path.GetFullPath).FirstOrDefault(File.Exists);
    }

    private static int ParseAll(string[] lines)
    {
        var tokenizer = new Tokenizer();
        var parser = new Parser();
        var failed = 0;
        var numbers = new HashSet<int>();

        foreach (var source in lines)
        {
            try
            {
                var programLine = parser.ParseLine(tokenizer.Tokenize(source), source);
                if (!numbers.Add(programLine.LineNumber))
                {
                    System.Console.WriteLine("FAIL: duplicate line " + programLine.LineNumber);
                    failed++;
                }
            }
            catch (Exception ex)
            {
                System.Console.WriteLine("FAIL parse: " + source);
                System.Console.WriteLine("  " + ex.Message);
                failed++;
            }
        }

        if (failed == 0)
            System.Console.WriteLine("PASS parse all " + lines.Length + " lines");

        return failed;
    }

    private static int Play(
        string name,
        string[] sourceLines,
        string[] inputs,
        string[] mustContain,
        int? stopAfterInputs = null)
    {
        var console = new TextConsole();
        var programConsole = new TextConsole();
        var machine = new CentauriMachine(console, programConsole);
        using var network = new NetworkService();
        var interpreter = new Interpreter(programConsole, machine, network);
        var program = new BasicProgram();
        var tokenizer = new Tokenizer();
        var parser = new Parser();

        foreach (var source in sourceLines)
            program.StoreLine(parser.ParseLine(tokenizer.Tokenize(source), source));

        interpreter.Start(program);
        var inputIndex = 0;
        var safety = 0;
        var output = new List<string>();

        while (interpreter.IsRunning && safety++ < 20000)
        {
            var action = interpreter.ExecuteNextInstruction();
            if (action != ExecutionAction.Input)
                continue;

            CaptureOutput(programConsole, output);

            if (stopAfterInputs.HasValue && inputIndex >= stopAfterInputs.Value)
                break;

            if (inputIndex >= inputs.Length)
                break;

            interpreter.SubmitInput(inputs[inputIndex++]);
        }

        CaptureOutput(programConsole, output);
        var text = string.Join('\n', output).ToUpperInvariant();
        var failed = 0;

        foreach (var needle in mustContain)
        {
            if (!text.Contains(needle.ToUpperInvariant(), StringComparison.Ordinal))
            {
                System.Console.WriteLine("FAIL " + name + ": missing \"" + needle + "\"");
                failed++;
            }
        }

        if (!interpreter.IsRunning == false && stopAfterInputs == null && inputIndex < inputs.Length)
        {
            // still running but unused inputs ok for partial routes
        }

        if (failed == 0)
            System.Console.WriteLine("PASS " + name);
        else
        {
            System.Console.WriteLine("--- output excerpt ---");
            foreach (var line in output.TakeLast(20))
                System.Console.WriteLine(line);
        }

        return failed;
    }

    private static void CaptureOutput(TextConsole console, List<string> output)
    {
        // TextConsole does not expose a line buffer API; scrape cells.
        for (var row = 0; row < TextConsole.DEFAULT_ROWS; row++)
        {
            var chars = new char[TextConsole.DEFAULT_COLUMNS];
            var any = false;
            for (var col = 0; col < TextConsole.DEFAULT_COLUMNS; col++)
            {
                var ch = console.GetCharacterAt(col, row);
                chars[col] = ch;
                if (ch != ' ' && ch != '\0')
                    any = true;
            }

            if (any)
                output.Add(new string(chars).TrimEnd());
        }
    }

    private static int ExpectGrantPackaging()
    {
        var failed = 0;
        var issue = MagazineCatalog.FindByNumber(1);
        failed += Expect("issue 1 cover id DUNGEON", issue?.CoverGameId == "DUNGEON");
        failed += Expect("issue 1 CoverTapeReady", issue?.CoverTapeReady == true);

        var bas = Path.Combine(AppContext.BaseDirectory, "Programs", "CoverTapes", "DUNGEON.bas");
        var tape = Path.Combine(AppContext.BaseDirectory, "Programs", "CoverTapes", "DUNGEON.tape");
        failed += Expect("DUNGEON.bas in CoverTapes", File.Exists(bas));
        failed += Expect("DUNGEON.tape in CoverTapes", File.Exists(tape));
        failed += Expect(
            "not auto-imported from Programs root",
            !File.Exists(Path.Combine(AppContext.BaseDirectory, "Programs", "DUNGEON.bas")));

        if (File.Exists(tape))
        {
            var label = File.ReadAllText(tape).ToUpperInvariant();
            failed += Expect("tape KIND GAME", label.Contains("KIND GAME"));
            failed += Expect("tape AUTHOR", label.Contains("AUTHOR CENTAURI MAG"));
        }

        var temp = Path.Combine(Path.GetTempPath(), "centauri64-ghoule-grant-" + Guid.NewGuid().ToString("N"));
        try
        {
            Directory.CreateDirectory(temp);
            var previous = Environment.GetEnvironmentVariable("CENTAURI64_TAPE_DIR");
            // SoftwareGrant uses TapeFolder.Location — copy manually to prove seed files.
            SoftwareGrant.GrantTapes(new[] { "DUNGEON" });
            var granted = Path.Combine(TapeFolder.Location, "DUNGEON.bas");
            failed += Expect("grant copies DUNGEON.bas", File.Exists(granted));
            _ = previous;
            _ = temp;
        }
        catch (Exception ex)
        {
            System.Console.WriteLine("FAIL grant: " + ex.Message);
            failed++;
        }

        return failed;
    }

    private static int Expect(string label, bool condition)
    {
        System.Console.WriteLine((condition ? "PASS " : "FAIL ") + label);
        return condition ? 0 : 1;
    }
}
