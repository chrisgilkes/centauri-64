using System;
using System.Linq;
using System.Collections.Generic;
using Centauri64.Console;
using Centauri64.Machine;
using Centauri64.Machine.Sprites;

namespace Centauri64.Basic;

public sealed partial class BasicMachine
{
    private const int INSTRUCTIONS_PER_FRAME = 1000;

    private readonly TextConsole _console;

    private readonly Tokenizer _tokenizer   = new();

    private readonly Parser _parser         = new();

    private readonly BasicProgram _program  = new();

    private readonly Interpreter _interpreter;

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

        ShowBootMessage();
    }

    private void ShowBootMessage()
    {
        var systemMemoryK = _machine.SystemMemoryBytes / 1024;

        _console.WriteLine("       CENTAURI64 PERSONAL COMPUTER");
        _console.WriteLine("");

        _console.WriteLine(
            $"       {systemMemoryK}K RAM   " +
            $"{_machine.BasicMemoryBytes} BASIC BYTES FREE");

        _console.WriteLine("");
        _console.WriteLine("READY.");
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

            if (source == "MEM")
            {
                ShowMemory();
                return;
            }

            if (source.StartsWith("EDIT "))
            {
                EditLine(source);
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
                ShowBootMessage();
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

}