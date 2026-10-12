using System;
using System.Collections.Generic;
using System.Linq;

namespace Centauri64.Basic;

public sealed partial class BasicMachine
{
    private void LoadProgram(string source)
    {
        var argument = source["LOAD ".Length..].Trim();

        if (argument.Length < 2 ||
            argument[0] != '"' ||
            argument[^1] != '"')
        {
            throw new InvalidOperationException("EXPECTED PROGRAM NAME");
        }

        var name = argument[1..^1];

        var sourceLines = _storage.Load(name);

        var loadedProgram = new BasicProgram();

        foreach (var sourceLine in sourceLines)
        {
            if (string.IsNullOrWhiteSpace(sourceLine))
                continue;

            var tokens = _tokenizer.Tokenize(sourceLine);

            var line = _parser.ParseLine(tokens,sourceLine);

            loadedProgram.StoreLine(line);
        }

        _program.Clear();

        foreach (var line in loadedProgram.Lines)
        {
            _program.StoreLine(line);
        }

        _spriteStorage.Load(name,_machine.SpriteAssets);
        _machine.EnsureBuiltInSpriteAssets();
        _machine.SpriteEditor.MarkSaved();

        _mapStorage.Load(name, _machine.MapAssets);
        _machine.MapEditor.MarkSaved();

        // Tape .images are runtime copies — do not overwrite the local artwork library.
        _imageStorage.Load(name, _machine.ImageAssets);
        _machine.ClearImageLayers();

        _assetIncludes = TapeAssetIncludes.Load(name);
        _tapeName = name;

        var label = _storage.LoadLabel(name);
        EnsureLabelIdentity(label);
        _storage.SaveLabel(name, label);

        _programId = label.ProgramId;
        _programVersion = label.ProgramVersion;
        SyncProgramIdentity();
        _network.Leave();

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
            throw new InvalidOperationException("EXPECTED PROGRAM NAME");
        }

        var name = argument[1..^1];

        // Ensure library edits are on disk before packing.
        _machine.PersistArtworkLibrary();

        var listing = GetProgramSourceText();
        var pack = TapeAssetPacker.Pack(
            listing,
            _machine.LocalArtwork,
            _machine.SpriteAssets,
            _assetIncludes);

        if (pack.MissingImages.Count > 0 || pack.MissingSprites.Count > 0)
        {
            _console.WriteLine("");
            foreach (var missing in pack.MissingImages)
                _console.WriteLine($"MISSING IMAGE {missing}");
            foreach (var missing in pack.MissingSprites)
                _console.WriteLine($"MISSING SPRITE {missing}");
            _console.WriteLine("FIX REFERENCES OR INCLUDE ARTWORK, THEN SAVE AGAIN.");
            _console.WriteLine("");
            _console.WriteLine("READY.");
            return;
        }

        _storage.Save(name, _program);

        // Portable baked sprites + packed images (library clones) — not live library files.
        _spriteStorage.Save(name, pack.Sprites);
        _machine.SpriteEditor.MarkSaved();

        _machine.ImageAssets.Clear();
        foreach (var image in pack.Images.Images)
            _machine.ImageAssets.Add(image);
        _imageStorage.Save(name, pack.Images);

        _mapStorage.Save(name, _machine.MapAssets);
        _machine.MapEditor.MarkSaved();

        _assetIncludes.Save(name);

        var label = _storage.LoadLabel(name);
        var sameTape = _tapeName != null &&
            string.Equals(_tapeName, name, StringComparison.OrdinalIgnoreCase);

        if (string.IsNullOrWhiteSpace(label.ProgramId))
        {
            if (sameTape || _tapeName == null)
                label.ProgramId = _programId;
            else
                label.ProgramId = NewProgramId();
        }

        if (label.ProgramVersion <= 0)
        {
            label.ProgramVersion = sameTape || _tapeName == null
                ? _programVersion
                : TapeLabel.DefaultProgramVersion;
        }

        EnsureLabelIdentity(label);
        label.MachineVersion = TapeLabel.CurrentMachineVersion;
        _storage.SaveLabel(name, label);

        _programId = label.ProgramId;
        _programVersion = label.ProgramVersion;
        _tapeName = name;
        SyncProgramIdentity();

        _console.WriteLine("");
        _console.WriteLine($"SAVED {name}");
        _console.WriteLine("");
        _console.WriteLine("READY.");
    }

    public IReadOnlyList<string> GetTapeNames()
    {
        return _storage.GetPrograms().ToList();
    }

    public TapeLabel GetTapeLabel(string name)
    {
        return _storage.LoadLabel(name);
    }

    public void SaveTapeLabel(string name, TapeLabel label)
    {
        if (label.MachineVersion <= 0)
            label.MachineVersion = TapeLabel.CurrentMachineVersion;

        EnsureLabelIdentity(label);
        _storage.SaveLabel(name, label);
    }

    public TapeCover GetTapeCover(string name)
    {
        return _storage.LoadCover(name);
    }

    public void SaveTapeCover(string name, TapeCover cover)
    {
        if (cover.HasArt)
        {
            _storage.SaveCover(name, cover);
            return;
        }

        _storage.DeleteCover(name);
    }

    private void ListPrograms()
    {
        var programs = _storage.GetPrograms().ToList();

        _console.WriteLine("");

        foreach (var program in programs)
        {
            _console.WriteLine(program, 18);
        }

        _console.WriteLine("");

        _console.WriteLine(
            $"{programs.Count} PROGRAM" +
            $"{(programs.Count == 1 ? "" : "S")}");

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
            throw new InvalidOperationException(
                "EXPECTED PROGRAM NAME");
        }

        var name = argument[1..^1];

        _storage.Delete(name);
        _spriteStorage.Delete(name);
        _mapStorage.Delete(name);
        _imageStorage.Delete(name);
        TapeAssetIncludes.Delete(name);

        if (_tapeName != null &&
            string.Equals(_tapeName, name, StringComparison.OrdinalIgnoreCase))
        {
            _tapeName = null;
            _assetIncludes = new TapeAssetIncludes();
        }

        _console.WriteLine("");
        _console.WriteLine(
            $"DELETED {name.ToUpperInvariant()}");
        _console.WriteLine("");
        _console.WriteLine("READY.");
    }
}