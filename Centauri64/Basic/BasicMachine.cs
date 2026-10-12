using System;
using System.Linq;
using System.Collections.Generic;
using Centauri64.Console;
using Centauri64.Machine;
using Centauri64.Machine.Sprites;
using Centauri64.Machine.Maps;
using Centauri64.Machine.Images;
using Centauri64.Network;

namespace Centauri64.Basic;

public sealed partial class BasicMachine
{
    private const int MAX_INSTRUCTIONS_PER_FRAME = 700;

    private readonly TextConsole _console;

    private readonly Tokenizer _tokenizer   = new();

    private readonly Parser _parser         = new();

    private readonly BasicProgram _program  = new();

    private readonly Interpreter _interpreter;

    public bool IsRunning =>_interpreter.IsRunning;

    private readonly ProgramStorage _storage;

    private readonly SpriteStorage _spriteStorage;

    private readonly MapStorage _mapStorage;

    private readonly ImageStorage _imageStorage;

    private readonly CentauriMachine _machine;

    private readonly NetworkService _network;

    private readonly BasicEditorTheme _editorTheme;

    private readonly BasicSourceRenderer _sourceRenderer;

    private string _programId = Guid.NewGuid().ToString("N");

    private int _programVersion = TapeLabel.DefaultProgramVersion;

    private string? _tapeName;

    private TapeAssetIncludes _assetIncludes = new();

    public BasicMachine(
        TextConsole console,
        CentauriMachine machine,
        NetworkService network)
    {
        _console        = console;

        _machine        = machine;
        _network        = network;

        _editorTheme    = new BasicEditorTheme();

        _machine.SetConsoleDefaults(
            _editorTheme.TextColour,
            _editorTheme.BackgroundColour);

        _console.Background = _editorTheme.BackgroundColour;
        _console.Foreground = _editorTheme.TextColour;
        _console.Clear();

        _sourceRenderer = new BasicSourceRenderer(_tokenizer,_editorTheme);

        _interpreter    = new Interpreter(console, machine, network);
        SyncProgramIdentity();

        _storage        = new ProgramStorage();

        _spriteStorage  = new SpriteStorage();
        _mapStorage     = new MapStorage();
        _imageStorage   = new ImageStorage();

        _machine.ImageEditor.SetListingLookup(GetProgramSourceText);
        WireAssetPersistence();

        _console.LineEntered += OnLineEntered;
        _console.InputChanged += UpdateInputHighlighting;

        ShowBootMessage();
    }

    private void SyncProgramIdentity()
    {
        _interpreter.SetProgramIdentity(_programId, _programVersion);
    }

    private static string NewProgramId()
    {
        return Guid.NewGuid().ToString("N");
    }

    private string GetProgramSourceText()
    {
        return string.Join('\n', _program.SourceLines);
    }

    public string? CurrentTapeName => _tapeName;

    private void WireAssetPersistence()
    {
        _machine.ImageEditor.GetTapeName = () => _tapeName;
        _machine.ImageEditor.PersistToLibrary = () =>
            _machine.PersistArtworkLibrary();
        _machine.ImageEditor.IsIncludedOnTape = name =>
            _tapeName != null && _assetIncludes.Contains(name);
        _machine.ImageEditor.ToggleIncludeOnTape = name =>
        {
            if (_tapeName == null)
                return null;

            var included = _assetIncludes.Toggle(name);
            _assetIncludes.Save(_tapeName);
            return included;
        };
        _machine.ImageEditor.OnImageCommitted = name =>
        {
            // Session dep: when a tape is loaded, new/edited artwork is auto-included.
            if (_tapeName == null || string.IsNullOrWhiteSpace(name))
                return;

            _assetIncludes.Add(name);
            _assetIncludes.Save(_tapeName);
        };
        _machine.ImageEditor.OnImageDeleted = name =>
        {
            if (_tapeName == null || string.IsNullOrWhiteSpace(name))
                return;

            _assetIncludes.Remove(name);
            _assetIncludes.Save(_tapeName);
        };

        _machine.SpriteEditor.GetTapeName = () => _tapeName;
        _machine.SpriteEditor.PersistToTape = name =>
        {
            // Binder still writes session sprites to the open tape so work is not lost
            // before SAVE packing; SAVE rewrites with the packed portable set.
            _spriteStorage.Save(name, _machine.SpriteAssets);
            _machine.SpriteEditor.MarkSaved();
        };
    }

    private void EnsureLabelIdentity(TapeLabel label, bool forceNewId = false)
    {
        if (forceNewId || string.IsNullOrWhiteSpace(label.ProgramId))
            label.ProgramId = NewProgramId();

        if (label.ProgramVersion <= 0)
            label.ProgramVersion = TapeLabel.DefaultProgramVersion;

        if (!Enum.IsDefined(label.Players) || label.Players <= 0)
            label.Players = TapePlayers.One;
    }

    public  void ShowBootMessage()
    {
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

        // Program INPUT uses the same editor console in default BASIC mode.
        if (IsWaitingForInput)
        {
            SubmitInput(source);
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

            if (source == "ANALYSE" || source == "ANALYZE")
            {
                AnalyseCurrentProgram();
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

            // Numbered lines are stored; unnumbered statements run immediately.
            if (tokens.Count > 0 && tokens[0].Type == TokenType.Number)
            {
                var line = _parser.ParseLine(tokens, source);
                _program.StoreLine(line);
                return;
            }

            var statement = _parser.ParseImmediate(tokens, source);
            _interpreter.ExecuteImmediate(statement);
        }
        catch(Exception exception)
        {
            _console.WriteLine($"?{exception.Message.ToUpperInvariant()}");
        }
    }

}