using System;
using System.Linq;
using System.Collections.Generic;
using Centauri64.Console;
using Centauri64.Machine;
using Centauri64.Machine.Sprites;

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

    private readonly ProgramStorage _storage;

    private readonly SpriteStorage _spriteStorage;

    private readonly CentauriMachine _machine;

    private readonly BasicEditorTheme _editorTheme;

    private readonly BasicSourceRenderer _sourceRenderer;

    public BasicMachine(TextConsole console, CentauriMachine machine)
    {
        _console        = console;

        _machine        = machine;

        _editorTheme    = new BasicEditorTheme();

        _console.Background = _editorTheme.BackgroundColour;
        _console.Foreground = _editorTheme.TextColour;
        _console.Clear();

        _sourceRenderer = new BasicSourceRenderer(_tokenizer,_editorTheme);

        _interpreter    = new Interpreter(console, machine);

        _storage        = new ProgramStorage();

        _spriteStorage  = new SpriteStorage();

        _console.LineEntered += OnLineEntered;
        _console.InputChanged += UpdateInputHighlighting;

    }

    private void WriteSystemMessage(string text)
    {
        _console.WriteLine(text,_editorTheme.SystemColour);
    }

    private void WriteError(string text)
    {
        _console.WriteLine(text,_editorTheme.ErrorColour);
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

            if (source == "LIST" ||
                source.StartsWith("LIST "))
            {
                ListProgram(source);
                return;
            }

            if (source == "DIR")
            {
                ListPrograms();
                return;
            }

            if (source.StartsWith("DELETE "))
            {
                DeleteProgram(source);
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

            if (source == "RESET")
            {
                _machine.ResetDisplay();
                _console.WriteLine("");
                _console.WriteLine("READY.");
                return;
            }

            if (source.StartsWith("SAVE "))
            {
                SaveProgram(source);
                return;
            }

            if (source.StartsWith("LOAD "))
            {
                LoadProgram(source);
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

    private void LoadProgram(string source)
    {
        var argument = source["LOAD ".Length..].Trim();

        if (argument.Length < 2 ||
            argument[0] != '"' ||
            argument[^1] != '"')
        {
            throw new InvalidOperationException(
                "EXPECTED PROGRAM NAME");
        }

        var name = argument[1..^1];

        var sourceLines = _storage.Load(name);

        var loadedProgram = new BasicProgram();

        foreach (var sourceLine in sourceLines)
        {
            var tokens =
                _tokenizer.Tokenize(sourceLine);

            var line =
                _parser.ParseLine(
                    tokens,
                    sourceLine);

            loadedProgram.StoreLine(line);
        }

        _program.Clear();

        foreach (var line in loadedProgram.Lines)
        {
            _program.StoreLine(line);
        }


        _spriteStorage.Load(name,_machine.SpriteAssets);

        _console.WriteLine("");
        _console.WriteLine($"LOADED {name}");
        _console.WriteLine("");
        _console.WriteLine("READY.");
    }

    private void SaveProgram(string source)
    {
        var argument = source["SAVE ".Length..].Trim();

        if (argument.Length < 2 ||
            argument[0] != '"' ||
            argument[^1] != '"')
        {
            throw new InvalidOperationException(
                "EXPECTED PROGRAM NAME");
        }

        var name = argument[1..^1];

        _storage.Save(name,_program);

        _spriteStorage.Save(name,_machine.SpriteAssets);

        _console.WriteLine("");
        _console.WriteLine($"SAVED {name}");
        _console.WriteLine("");
        _console.WriteLine("READY.");
    }

    private void RunProgram()
    {
        _interpreter.Start(_program);

        if (!_interpreter.IsRunning)
        {
            OnProgramFinished();
        }
    }

    private void ListPrograms()
    {
        var programs = _storage.GetPrograms().ToList();

        _console.WriteLine("");

        foreach (var program in programs)
        {
            _console.WriteLine(program);
        }

        _console.WriteLine("");

        _console.WriteLine($"{programs.Count} PROGRAM{(programs.Count == 1 ? "" : "S")}");

        _console.WriteLine("");
        _console.WriteLine("READY.");
    }

    private void DeleteProgram(string source)
    {
        var argument = source["DELETE ".Length..].Trim();

        if (argument.Length < 2 ||
            argument[0] != '"' ||
            argument[^1] != '"')
        {
            throw new InvalidOperationException("EXPECTED PROGRAM NAME");
        }

        var name = argument[1..^1];

        _storage.Delete(name);
        _spriteStorage.Delete(name);

        _console.WriteLine("");
        _console.WriteLine($"DELETED {name.ToUpperInvariant()}");
        _console.WriteLine("");
        _console.WriteLine("READY.");
    }

    public void Stop()
    {
        if (!_interpreter.IsRunning)
            return;

        _interpreter.Stop();

        _machine.HideAllSprites();
        _machine.ResetDisplay();

        _console.WriteLine("BREAK");
        _console.WriteLine("");
        _console.WriteLine("READY.");
    }

    public void Update()
    {
        if (!_interpreter.IsRunning)
            return;

        try
        {
            for (var i = 0; i < INSTRUCTIONS_PER_FRAME && _interpreter.IsRunning; i++)
            {
                var action = _interpreter.ExecuteNextInstruction();

                if (action == ExecutionAction.Yield ||
                    action == ExecutionAction.Wait)
                {
                    break;
                }
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

    private void UpdateInputHighlighting()
    {
        var source = _console.GetCurrentLine();

        _sourceRenderer.ColourExistingLine(_console,source,_console.CursorRow,0);
    }

    private void OnProgramFinished()
    {
        _console.WriteLine("");
        _console.WriteLine("READY.");
    }

    private void ListProgram(string source)
    {
        var argument = source["LIST".Length..].Trim();

        if (string.IsNullOrEmpty(argument))
        {
            foreach (var line in _program.Lines)
            {
                _sourceRenderer.WriteLine(_console,line.Source);
            }

            _console.WriteLine("");
            _console.WriteLine("READY.");
            return;
        }

        if (int.TryParse(argument, out var lineNumber))
        {
            foreach (var line in _program.Lines)
            {
                if (line.LineNumber == lineNumber)
                {
                    _sourceRenderer.WriteLine(_console,line.Source);
                    break;
                }
            }

            _console.WriteLine("");
            _console.WriteLine("READY.");
            return;
        }

        var parts = argument.Split('-', 2);

        if (parts.Length == 2 &&
            int.TryParse(parts[0], out var startLine) &&
            int.TryParse(parts[1], out var endLine))
        {
            if (startLine > endLine)
            {
                throw new InvalidOperationException("BAD LIST RANGE");
            }

            foreach (var line in _program.Lines)
            {
                if (line.LineNumber >= startLine &&
                    line.LineNumber <= endLine)
                {
                    _sourceRenderer.WriteLine(_console,line.Source);
                }
            }

            _console.WriteLine("");
            _console.WriteLine("READY.");
            return;
        }

        throw new InvalidOperationException("BAD LIST RANGE");
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