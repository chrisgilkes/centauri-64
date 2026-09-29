using System;
using System.Linq;
using System.IO;
using System.Collections.Generic;
namespace Centauri64.Basic;

public sealed class ProgramStorage
{
    private readonly string _programDirectory;

    public ProgramStorage()
    {
        TapeFolder.EnsureExists();
        _programDirectory = TapeFolder.Location;
    }

    public void Save(
        string name,
        BasicProgram program)
    {
        var path = GetProgramPath(name);

        File.WriteAllLines(
            path,
            program.SourceLines);
    }

    public IEnumerable<string> Load(string name)
    {
        var path = GetProgramPath(name);

        if (!File.Exists(path))
        {
            throw new InvalidOperationException(
                "PROGRAM NOT FOUND");
        }

        return File.ReadLines(path);
    }

    public IEnumerable<string> GetPrograms()
    {
        return Directory
            .EnumerateFiles(_programDirectory, "*.bas")
            .Select(Path.GetFileNameWithoutExtension)
            .OrderBy(name => name);
    }

    public void Delete(string name)
    {
        var path = GetProgramPath(name);

        if (!File.Exists(path))
        {
            throw new InvalidOperationException(
                "PROGRAM NOT FOUND");
        }

        File.Delete(path);
        DeleteLabel(name);
        DeleteCover(name);
    }

    public TapeLabel LoadLabel(string name)
    {
        var path = GetLabelPath(name);

        if (!File.Exists(path))
            return new TapeLabel();

        var label = new TapeLabel();

        foreach (var line in File.ReadAllLines(path))
        {
            ReadLabelLine(label, line);
        }

        return label;
    }

    public void SaveLabel(string name, TapeLabel label)
    {
        var path = GetLabelPath(name);

        label.Description = Clean(
            label.Description,
            TapeLabel.MaxDescriptionLength);

        label.Author = Clean(
            label.Author,
            TapeLabel.MaxAuthorLength);

        File.WriteAllLines(
            path,
            new[]
            {
                "DESCRIPTION " + label.Description,
                "AUTHOR " + label.Author,
                "KIND " + label.Kind.ToString().ToUpperInvariant(),
                "MACHINE " + label.MachineVersion
            });
    }

    public void DeleteLabel(string name)
    {
        var path = GetLabelPath(name);

        if (File.Exists(path))
            File.Delete(path);
    }

    public TapeCover LoadCover(string name)
    {
        var path = GetCoverPath(name);
        var cover = new TapeCover();

        if (!File.Exists(path))
            return cover;

        var lines = File.ReadAllLines(path);

        if (lines.Length == 0)
            return cover;

        var header = lines[0].Trim();

        if (!header.StartsWith("COVER "))
            return cover;

        var y = 0;

        for (var i = 1; i < lines.Length && y < TapeCover.Height; i++)
        {
            var line = lines[i].Trim();

            if (line.Length == 0)
                continue;

            var values = line.Split(',');

            if (values.Length != TapeCover.Width)
                continue;

            for (var x = 0; x < TapeCover.Width; x++)
            {
                if (int.TryParse(values[x], out var colour))
                    cover.Pixels[y, x] = colour;
            }

            y++;
        }

        return cover;
    }

    public void SaveCover(string name, TapeCover cover)
    {
        var path = GetCoverPath(name);
        var lines = new string[TapeCover.Height + 1];
        lines[0] = $"COVER {TapeCover.Width} {TapeCover.Height}";

        for (var y = 0; y < TapeCover.Height; y++)
        {
            var row = new string[TapeCover.Width];

            for (var x = 0; x < TapeCover.Width; x++)
            {
                row[x] = cover.Pixels[y, x].ToString();
            }

            lines[y + 1] = string.Join(",", row);
        }

        File.WriteAllLines(path, lines);
    }

    public void DeleteCover(string name)
    {
        var path = GetCoverPath(name);

        if (File.Exists(path))
            File.Delete(path);
    }

    private string GetCoverPath(string name)
    {
        return Path.Combine(
            _programDirectory,
            ValidateName(name) + ".cover");
    }

    private string GetProgramPath(string name)
    {
        return Path.Combine(
            _programDirectory,
            ValidateName(name) + ".bas");
    }

    private string GetLabelPath(string name)
    {
        return Path.Combine(
            _programDirectory,
            ValidateName(name) + ".tape");
    }

    private static void ReadLabelLine(TapeLabel label, string line)
    {
        if (line.StartsWith("DESCRIPTION "))
        {
            label.Description = Clean(
                line["DESCRIPTION ".Length..],
                TapeLabel.MaxDescriptionLength);
            return;
        }

        if (line.StartsWith("AUTHOR "))
        {
            label.Author = Clean(
                line["AUTHOR ".Length..],
                TapeLabel.MaxAuthorLength);
            return;
        }

        if (line.StartsWith("KIND ") &&
            Enum.TryParse<TapeKind>(
                line["KIND ".Length..].Trim(),
                ignoreCase: true,
                out var kind))
        {
            label.Kind = kind;
            return;
        }

        if (line.StartsWith("MACHINE ") &&
            int.TryParse(line["MACHINE ".Length..].Trim(), out var version))
        {
            label.MachineVersion = version;
        }
    }

    private static string Clean(string value, int maxLength)
    {
        var cleaned = value
            .Replace("\r", string.Empty)
            .Replace("\n", string.Empty)
            .Trim()
            .ToUpperInvariant();

        if (cleaned.Length > maxLength)
            return cleaned[..maxLength];

        return cleaned;
    }

    private static string ValidateName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new InvalidOperationException(
                "Program name cannot be empty.");
        }

        if (name.Contains(".."))
        {
            throw new InvalidOperationException(
                "Invalid program name.");
        }

        foreach (var character in
                 Path.GetInvalidFileNameChars())
        {
            if (name.Contains(character))
            {
                throw new InvalidOperationException(
                    "Invalid program name.");
            }
        }

        return name.ToUpperInvariant();
    }
}