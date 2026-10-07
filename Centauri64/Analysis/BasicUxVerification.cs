using System;
using System.Collections.Generic;

using Centauri64.Basic;
using Centauri64.Console;
using Centauri64.Machine;
using Centauri64.Network;

namespace Centauri64.Analysis;

/// <summary>
/// Headless checks for default BASIC execution + immediate mode.
/// Run with: Centauri64.exe --verify-basic-ux
/// </summary>
public static class BasicUxVerification
{
    public static int Run()
    {
        var failed = 0;
        System.Console.WriteLine("BASIC UX VERIFICATION");
        System.Console.WriteLine();

        failed += ImmediatePrint();
        failed += ImmediateExpressionAndAssignment();
        failed += ImmediatePaper();
        failed += StoredProgramStaysOnConsole();
        failed += ClsStaysOnConsole();
        failed += ModeOneHighRes();
        failed += ModeTwoArcade();
        failed += ModeZeroErrors();
        failed += ImmediateNotStored();

        System.Console.WriteLine();
        System.Console.WriteLine(failed == 0 ? "ALL BASIC UX CHECKS PASSED" : failed + " FAILED");
        return failed;
    }

    private static int ImmediatePrint()
    {
        var env = Create();
        env.Interpreter.ExecuteImmediate(
            env.Parser.ParseImmediate(env.Tokenizer.Tokenize("PRINT \"HELLO\""), "PRINT \"HELLO\""));

        return Expect("immediate PRINT HELLO", ConsoleContains(env.Console, "HELLO"));
    }

    private static int ImmediateExpressionAndAssignment()
    {
        var env = Create();
        env.Interpreter.ExecuteImmediate(
            env.Parser.ParseImmediate(env.Tokenizer.Tokenize("A=10"), "A=10"));
        env.Interpreter.ExecuteImmediate(
            env.Parser.ParseImmediate(env.Tokenizer.Tokenize("PRINT A"), "PRINT A"));
        env.Interpreter.ExecuteImmediate(
            env.Parser.ParseImmediate(env.Tokenizer.Tokenize("PRINT 2+2"), "PRINT 2+2"));

        var text = Capture(env.Console);
        return Expect("immediate assignment + PRINT 2+2",
            text.Contains("10") && text.Contains("4"));
    }

    private static int ImmediatePaper()
    {
        var env = Create();
        env.Machine.SetConsoleDefaults(16, 31);
        env.Machine.ResetDisplay();

        env.Interpreter.ExecuteImmediate(
            env.Parser.ParseImmediate(env.Tokenizer.Tokenize("PAPER 0"), "PAPER 0"));

        var afterPaper = env.Machine.PaperColour == 0 &&
                         env.Console.Background == 0;

        env.Machine.ResetDisplay();

        var afterReset = env.Machine.PaperColour == 31 &&
                         env.Console.Background == 31 &&
                         env.Console.Foreground == 16;

        return Expect("immediate PAPER + RESET restores defaults", afterPaper && afterReset);
    }

    private static int StoredProgramStaysOnConsole()
    {
        var env = Create();
        env.Program.StoreLine(
            env.Parser.ParseLine(env.Tokenizer.Tokenize("10 PRINT \"HELLO\""), "10 PRINT \"HELLO\""));
        env.Interpreter.Start(env.Program);

        var safety = 0;
        while (env.Interpreter.IsRunning && safety++ < 1000)
            env.Interpreter.ExecuteNextInstruction();

        var ok = env.Machine.DisplayMode == CentauriDisplayMode.Console &&
                 ConsoleContains(env.Console, "HELLO") &&
                 !ConsoleContains(env.ProgramConsole, "HELLO");
        return Expect("RUN without MODE stays on default BASIC console", ok);
    }

    private static int ClsStaysOnConsole()
    {
        var env = Create();
        env.Program.StoreLine(
            env.Parser.ParseLine(env.Tokenizer.Tokenize("10 CLS"), "10 CLS"));
        env.Program.StoreLine(
            env.Parser.ParseLine(env.Tokenizer.Tokenize("20 PRINT \"HELLO\""), "20 PRINT \"HELLO\""));
        env.Interpreter.Start(env.Program);

        var safety = 0;
        while (env.Interpreter.IsRunning && safety++ < 1000)
            env.Interpreter.ExecuteNextInstruction();

        var ok = env.Machine.DisplayMode == CentauriDisplayMode.Console &&
                 ConsoleContains(env.Console, "HELLO");
        return Expect("CLS without MODE clears default BASIC console", ok);
    }

    private static int ModeOneHighRes()
    {
        var env = Create();
        env.Program.StoreLine(
            env.Parser.ParseLine(env.Tokenizer.Tokenize("10 MODE 1"), "10 MODE 1"));
        env.Interpreter.Start(env.Program);
        env.Interpreter.ExecuteNextInstruction();

        return Expect("MODE 1 selects High Resolution",
            env.Machine.DisplayMode == CentauriDisplayMode.HighResolution &&
            env.Machine.ScreenWidth == 640);
    }

    private static int ModeTwoArcade()
    {
        var env = Create();
        env.Program.StoreLine(
            env.Parser.ParseLine(env.Tokenizer.Tokenize("10 MODE 2"), "10 MODE 2"));
        env.Interpreter.Start(env.Program);
        env.Interpreter.ExecuteNextInstruction();

        return Expect("MODE 2 selects Arcade",
            env.Machine.DisplayMode == CentauriDisplayMode.Arcade &&
            env.Machine.ScreenWidth == 320);
    }

    private static int ModeZeroErrors()
    {
        var env = Create();
        env.Program.StoreLine(
            env.Parser.ParseLine(env.Tokenizer.Tokenize("10 MODE 0"), "10 MODE 0"));
        env.Interpreter.Start(env.Program);

        try
        {
            env.Interpreter.ExecuteNextInstruction();
            return Expect("MODE 0 throws", false);
        }
        catch (InvalidOperationException ex)
        {
            return Expect("MODE 0 throws",
                ex.Message.Contains("Unsupported display mode", StringComparison.OrdinalIgnoreCase));
        }
    }

    private static int ImmediateNotStored()
    {
        var env = Create();
        env.Interpreter.ExecuteImmediate(
            env.Parser.ParseImmediate(env.Tokenizer.Tokenize("PRINT \"HELLO\""), "PRINT \"HELLO\""));

        return Expect("immediate PRINT not in listing", env.Program.GetLines().Count == 0);
    }

    private static Env Create()
    {
        var console = new TextConsole();
        var programConsole = new TextConsole();
        var machine = new CentauriMachine(console, programConsole);
        var network = new NetworkService();
        return new Env(
            console,
            programConsole,
            machine,
            network,
            new Interpreter(console, machine, network),
            new BasicProgram(),
            new Tokenizer(),
            new Parser());
    }

    private static bool ConsoleContains(TextConsole console, string needle)
    {
        return Capture(console).Contains(needle, StringComparison.OrdinalIgnoreCase);
    }

    private static string Capture(TextConsole console)
    {
        var lines = new List<string>();
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
                lines.Add(new string(chars).TrimEnd());
        }

        return string.Join('\n', lines);
    }

    private static int Expect(string label, bool condition)
    {
        System.Console.WriteLine((condition ? "PASS " : "FAIL ") + label);
        return condition ? 0 : 1;
    }

    private sealed class Env
    {
        public TextConsole Console { get; }
        public TextConsole ProgramConsole { get; }
        public CentauriMachine Machine { get; }
        public NetworkService Network { get; }
        public Interpreter Interpreter { get; }
        public BasicProgram Program { get; }
        public Tokenizer Tokenizer { get; }
        public Parser Parser { get; }

        public Env(
            TextConsole console,
            TextConsole programConsole,
            CentauriMachine machine,
            NetworkService network,
            Interpreter interpreter,
            BasicProgram program,
            Tokenizer tokenizer,
            Parser parser)
        {
            Console = console;
            ProgramConsole = programConsole;
            Machine = machine;
            Network = network;
            Interpreter = interpreter;
            Program = program;
            Tokenizer = tokenizer;
            Parser = parser;
        }
    }
}
