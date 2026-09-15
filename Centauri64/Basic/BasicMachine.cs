using System;
using Centauri64.Console;

namespace Centauri64.Basic;

public sealed class BasicMachine
{
    private readonly TextConsole _console;

    private readonly Tokenizer _tokenizer   = new();

    private readonly Parser _parser         = new();

    private readonly BasicProgram _program  = new();

    private readonly Interpreter _interpreter;

    public BasicMachine(TextConsole console)
    {
        _console = console;

        _interpreter = new Interpreter(console);

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
        _interpreter.Run(_program);

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