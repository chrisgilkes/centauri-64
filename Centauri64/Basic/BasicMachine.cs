using System;
using Centauri64.Console;
using Centauri64.Machine;

namespace Centauri64.Basic;

public sealed class BasicMachine
{
    private readonly TextConsole _console;

    private readonly Tokenizer _tokenizer   = new();

    private readonly Parser _parser         = new();

    private readonly BasicProgram _program  = new();

    private readonly Interpreter _interpreter;

    private const int INSTRUCTIONS_PER_FRAME = 1000;

    public bool IsRunning =>_interpreter.IsRunning;

    public BasicMachine(TextConsole console, CentauriMachine machine)
    {
        _console = console;

        _interpreter = new Interpreter(console, machine);

        _console.LineEntered += OnLineEntered;
    }

    private void OnLineEntered(string source)
    {
        if(string.IsNullOrWhiteSpace(source))
        {
            return;
        }

        try
        {

            if(source == "RUN")
            {
                RunProgram();
                return;
            }

            if(source == "LIST")
            {
                ListProgram();
                return;
            }

            if(source == "NEW")
            {
                NewProgram();
                return;
            }

            if (source == "CLS")
            {
                ClearScreen();
                return;
            }

            if (TryDeleteLine(source))
            {
                return;
            }

            var tokens  = _tokenizer.Tokenize(source);

            var line    = _parser.ParseLine(tokens, source);

            _program.StoreLine(line);

        }
        catch(Exception exception)
        {
            _console.WriteLine($"?{exception.Message.ToUpperInvariant()}");
        }
    }

    private void RunProgram()
    {
        _interpreter.Start(_program);

        if (!_interpreter.IsRunning)
        {
            OnProgramFinished();
        }
    }

    public void Stop()
    {
        if (!_interpreter.IsRunning)
            return;

        _interpreter.Stop();

        _console.WriteLine("");
        _console.WriteLine("BREAK");
        _console.WriteLine("READY.");
    }

    public void Update()
    {
        if (!_interpreter.IsRunning)
            return;

        try
        {
            for (var i = 0;
                i < INSTRUCTIONS_PER_FRAME &&
                _interpreter.IsRunning;
                i++)
            {
                var yielded =
                    _interpreter.ExecuteNextInstruction();

                if (yielded)
                    break;
            }

            if (!_interpreter.IsRunning)
            {
                OnProgramFinished();
            }
        }
        catch (Exception exception)
        {
            _interpreter.Stop();

            _console.WriteLine(
                $"?{exception.Message.ToUpperInvariant()}");

            OnProgramFinished();
        }
    }

    private void OnProgramFinished()
    {
        _console.WriteLine("");
        _console.WriteLine("READY.");
    }

    private void ListProgram()
    {
        foreach(var source in _program.SourceLines)
        {
            _console.WriteLine(source);
        }

        _console.WriteLine("");
        _console.WriteLine("READY.");
    }

    private void NewProgram()
    {
        _program.Clear();

        _console.WriteLine("");
        _console.WriteLine("READY.");
    }

    private void ClearScreen()
    {
        _console.Clear();

        _console.WriteLine("");
        _console.WriteLine("READY.");
    }

    private bool TryDeleteLine(string source)
    {
        if (!int.TryParse(source, out var lineNumber))
            return false;

        _program.DeleteLine(lineNumber);

        return true;
    }
}