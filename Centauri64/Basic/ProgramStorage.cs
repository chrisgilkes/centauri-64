using System;
using System.IO;
using System.Collections.Generic;
namespace Centauri64.Basic;

public sealed class ProgramStorage
{
    private readonly string _programDirectory;

    public ProgramStorage()
    {
        _programDirectory = Path.Combine(
            AppContext.BaseDirectory,
            "Programs");

        Directory.CreateDirectory(_programDirectory);
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

    private string GetProgramPath(string name)
    {
        var validName = ValidateName(name);

        return Path.Combine(
            _programDirectory,
            validName + ".bas");
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