using System;
using System.Collections.Generic;
using System.IO;

using Centauri64.Basic;
using Centauri64.Basic.Syntax;
using Centauri64.Console;
using Centauri64.Machine;
using Centauri64.Network;

namespace Centauri64.Analysis;

/// <summary>
/// Headless checks for CLS presentation bursts (including CLS → GOSUB draw).
/// Run with: Centauri64.exe --verify-presentation
/// </summary>
public static class PresentationVerification
{
    private const int MaxInstructionsPerFrame = 700;

    public static int Run()
    {
        var failed = 0;
        System.Console.WriteLine("PRESENTATION VERIFICATION");
        System.Console.WriteLine();

        failed += DirectDrawSameFrame();
        failed += DrawThroughGosubSameFrame();
        failed += MultipleDrawGosubsSameFrame();
        failed += ClsInsideGosubStillWorks();
        failed += OrdinaryGosubReturn();
        failed += ExplicitYieldStillEndsFrame();
        failed += InstructionCapStillApplies();
        failed += PongStyleDrawGosub();
        failed += LunarPrototypeParsesAndDraws();
        failed += LunarCleanLoopParsesAndDraws();

        System.Console.WriteLine();
        System.Console.WriteLine(
            failed == 0 ? "ALL PRESENTATION CHECKS PASSED" : failed + " FAILED");
        return failed;
    }

    private static int DirectDrawSameFrame()
    {
        var env = Load(
            "10 MODE 1",
            "20 PAPER 0",
            "30 CLS",
            "40 RECT 10,10,20,20,15,FILL",
            "50 YIELD",
            "60 END");

        var action = RunUntilYieldOrStop(env);

        return Expect(
            "A direct CLS+RECT before yield",
            action == ExecutionAction.Yield &&
            env.Machine.RetainedRectangleCount == 1);
    }

    private static int DrawThroughGosubSameFrame()
    {
        var env = Load(
            "10 MODE 1",
            "20 PAPER 0",
            "30 CLS",
            "40 GOSUB 1000",
            "50 YIELD",
            "60 END",
            "1000 RECT 10,10,20,20,15,FILL",
            "1010 RETURN");

        var action = RunUntilYieldOrStop(env);

        return Expect(
            "B CLS+GOSUB draw before yield (no blank frame)",
            action == ExecutionAction.Yield &&
            env.Machine.RetainedRectangleCount == 1);
    }

    private static int MultipleDrawGosubsSameFrame()
    {
        var env = Load(
            "10 MODE 1",
            "20 PAPER 0",
            "30 CLS",
            "40 GOSUB 1000",
            "50 GOSUB 2000",
            "60 PRINTAT 8,8,\"HUD\"",
            "70 YIELD",
            "80 END",
            "1000 RECT 0,0,40,40,12,FILL",
            "1010 RETURN",
            "2000 LINE 0,0,40,40,15",
            "2010 RETURN");

        var action = RunUntilYieldOrStop(env);

        return Expect(
            "C multiple draw GOSUBs + PRINTAT one frame",
            action == ExecutionAction.Yield &&
            env.Machine.RetainedRectangleCount == 1 &&
            env.Machine.RetainedLineCount == 1 &&
            env.Machine.RetainedTextCount == 1);
    }

    private static int ClsInsideGosubStillWorks()
    {
        var env = Load(
            "10 MODE 1",
            "20 PAPER 0",
            "30 GOSUB 1000",
            "40 YIELD",
            "50 END",
            "1000 CLS",
            "1010 RECT 10,10,20,20,15,FILL",
            "1020 RETURN");

        var action = RunUntilYieldOrStop(env);

        return Expect(
            "D CLS-inside-GOSUB safe pattern",
            action == ExecutionAction.Yield &&
            env.Machine.RetainedRectangleCount == 1);
    }

    private static int OrdinaryGosubReturn()
    {
        var env = Load(
            "10 A=0",
            "20 GOSUB 100",
            "30 PRINT A",
            "40 END",
            "100 A=42",
            "110 RETURN");

        RunUntilStop(env);

        return Expect(
            "E ordinary GOSUB/RETURN",
            ConsoleContains(env.Console, "42"));
    }

    private static int ExplicitYieldStillEndsFrame()
    {
        var env = Load(
            "10 MODE 1",
            "20 PAPER 0",
            "30 CLS",
            "40 GOSUB 1000",
            "50 YIELD",
            "60 RECT 100,100,10,10,7,FILL",
            "70 END",
            "1000 RECT 10,10,20,20,15,FILL",
            "1010 RETURN");

        var first = RunUntilYieldOrStop(env);
        var rectsAfterYield = env.Machine.RetainedRectangleCount;

        // Next slice should execute the post-YIELD RECT.
        RunUntilStop(env);

        return Expect(
            "explicit YIELD ends BASIC frame",
            first == ExecutionAction.Yield &&
            rectsAfterYield == 1 &&
            env.Machine.RetainedRectangleCount == 2);
    }

    private static int InstructionCapStillApplies()
    {
        // Busy loop with no YIELD / presentation break — host slice must stop
        // at MAX_INSTRUCTIONS_PER_FRAME and leave the program running.
        var env = Load(
            "10 A=0",
            "20 A=A+1",
            "30 GOTO 20");

        env.Interpreter.Start(env.Program);

        var steps = 0;
        ExecutionAction last = ExecutionAction.Continue;

        for (var i = 0; i < MaxInstructionsPerFrame; i++)
        {
            if (!env.Interpreter.IsRunning)
                break;

            last = env.Interpreter.ExecuteNextInstruction();
            steps++;

            if (last == ExecutionAction.Yield ||
                last == ExecutionAction.Wait ||
                last == ExecutionAction.Input)
            {
                break;
            }
        }

        return Expect(
            "700-instruction safety cap still applies",
            env.Interpreter.IsRunning &&
            steps == MaxInstructionsPerFrame &&
            last != ExecutionAction.Yield);
    }

    private static int PongStyleDrawGosub()
    {
        // Mirrors PONG's GOSUB-starts-with-CLS draw burst (Programs/PONG.bas ~4000).
        var env = Load(
            "10 MODE 2",
            "20 PAPER 0",
            "30 P1Y=88",
            "40 P2Y=88",
            "50 BX=152",
            "60 BY=112",
            "70 S1=0",
            "80 S2=0",
            "90 PW=6",
            "100 PH=28",
            "110 BS=4",
            "120 GOSUB 4000",
            "130 YIELD",
            "140 END",
            "4000 CLS",
            "4010 RECT 0,0,320,8,1,FILL",
            "4020 RECT 0,232,320,8,1,FILL",
            "4030 RECT 8,P1Y,PW,PH,7,FILL",
            "4040 RECT 306,P2Y,PW,PH,7,FILL",
            "4050 RECT BX,BY,BS,BS,15,FILL",
            "4060 PRINTAT 40,16,S1",
            "4070 PRINTAT 260,16,S2",
            "4080 RETURN");

        var action = RunUntilYieldOrStop(env);

        return Expect(
            "F PONG-style draw GOSUB",
            action == ExecutionAction.Yield &&
            env.Machine.RetainedRectangleCount == 5 &&
            env.Machine.RetainedTextCount == 2);
    }

    private static int LunarPrototypeParsesAndDraws()
    {
        var path = FindAuditFile("11_lunar_rescue_prototype.bas");
        if (path == null)
            return Expect("lunar prototype file present", false);

        var env = Create();
        foreach (var raw in File.ReadAllLines(path))
        {
            var line = raw.Trim();
            if (line.Length == 0)
                continue;

            env.Program.StoreLine(
                env.Parser.ParseLine(env.Tokenizer.Tokenize(line), line));
        }

        env.Interpreter.Start(env.Program);

        // Drive several host frames so reset + first draw burst can complete.
        for (var frame = 0; frame < 8; frame++)
            RunOneHostFrame(env);

        return Expect(
            "lunar audit prototype draws after runtime change",
            env.Interpreter.IsRunning &&
            env.Machine.RetainedRectangleCount >= 3 &&
            env.Machine.RetainedTextCount >= 1);
    }

    private static int LunarCleanLoopParsesAndDraws()
    {
        var path = FindAuditFile("13_lunar_rescue_clean_loop.bas");
        if (path == null)
            return Expect("lunar clean loop file present", false);

        var env = Create();
        foreach (var raw in File.ReadAllLines(path))
        {
            var line = raw.Trim();
            if (line.Length == 0)
                continue;

            env.Program.StoreLine(
                env.Parser.ParseLine(env.Tokenizer.Tokenize(line), line));
        }

        env.Interpreter.Start(env.Program);

        for (var frame = 0; frame < 8; frame++)
            RunOneHostFrame(env);

        return Expect(
            "lunar clean CLS→GOSUB loop draws",
            env.Interpreter.IsRunning &&
            env.Machine.RetainedRectangleCount >= 3 &&
            env.Machine.RetainedTextCount >= 1);
    }

    private static string? FindAuditFile(string name)
    {
        var candidates = new[]
        {
            Path.Combine(AppContext.BaseDirectory, "Issue02Audit", name),
            Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "Issue02Audit", name),
            Path.Combine(Directory.GetCurrentDirectory(), "Centauri64", "Issue02Audit", name),
            Path.Combine(Directory.GetCurrentDirectory(), "Issue02Audit", name)
        };

        foreach (var path in candidates)
        {
            if (File.Exists(path))
                return Path.GetFullPath(path);
        }

        return null;
    }

    private static Env Load(params string[] lines)
    {
        var env = Create();
        foreach (var line in lines)
        {
            env.Program.StoreLine(
                env.Parser.ParseLine(env.Tokenizer.Tokenize(line), line));
        }

        env.Interpreter.Start(env.Program);
        return env;
    }

    private static ExecutionAction RunUntilYieldOrStop(Env env)
    {
        ExecutionAction last = ExecutionAction.Continue;

        for (var i = 0; i < MaxInstructionsPerFrame; i++)
        {
            if (!env.Interpreter.IsRunning)
                return last;

            last = env.Interpreter.ExecuteNextInstruction();

            if (last == ExecutionAction.Yield ||
                last == ExecutionAction.Wait ||
                last == ExecutionAction.Input)
            {
                return last;
            }
        }

        return last;
    }

    private static void RunOneHostFrame(Env env)
    {
        for (var i = 0; i < MaxInstructionsPerFrame; i++)
        {
            if (!env.Interpreter.IsRunning)
                return;

            var action = env.Interpreter.ExecuteNextInstruction();

            if (action == ExecutionAction.Yield ||
                action == ExecutionAction.Wait ||
                action == ExecutionAction.Input)
            {
                return;
            }
        }
    }

    private static void RunUntilStop(Env env)
    {
        var safety = 0;
        while (env.Interpreter.IsRunning && safety++ < 20000)
        {
            var action = env.Interpreter.ExecuteNextInstruction();
            if (action == ExecutionAction.Wait ||
                action == ExecutionAction.Input)
            {
                break;
            }
        }
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

        return string.Join('\n', lines)
            .Contains(needle, StringComparison.OrdinalIgnoreCase);
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
