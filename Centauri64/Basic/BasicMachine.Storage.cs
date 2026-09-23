using System;
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

        _storage.Save(name, _program);

        _spriteStorage.Save(name,_machine.SpriteAssets);

        _console.WriteLine("");
        _console.WriteLine($"SAVED {name}");
        _console.WriteLine("");
        _console.WriteLine("READY.");
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

        _console.WriteLine("");
        _console.WriteLine(
            $"DELETED {name.ToUpperInvariant()}");
        _console.WriteLine("");
        _console.WriteLine("READY.");
    }
}