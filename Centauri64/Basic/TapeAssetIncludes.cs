using System;
using System.Collections.Generic;
using System.IO;

namespace Centauri64.Basic;

/// <summary>
/// Per-tape explicit INCLUDE list for Art Studio images.
/// Stored as {TAPE}.assets beside the .bas / .images sidecars.
/// </summary>
public sealed class TapeAssetIncludes
{
    private readonly HashSet<string> _images =
        new(StringComparer.OrdinalIgnoreCase);

    public IReadOnlyCollection<string> ImageNames => _images;

    public bool Contains(string imageName)
    {
        if (string.IsNullOrWhiteSpace(imageName))
            return false;

        return _images.Contains(imageName.Trim().ToUpperInvariant());
    }

    public bool Toggle(string imageName)
    {
        var key = imageName.Trim().ToUpperInvariant();
        if (_images.Remove(key))
            return false;

        _images.Add(key);
        return true;
    }

    public void Add(string imageName)
    {
        if (string.IsNullOrWhiteSpace(imageName))
            return;

        _images.Add(imageName.Trim().ToUpperInvariant());
    }

    public void Remove(string imageName)
    {
        if (string.IsNullOrWhiteSpace(imageName))
            return;

        _images.Remove(imageName.Trim().ToUpperInvariant());
    }

    public void Clear() => _images.Clear();

    public void Save(string tapeName)
    {
        TapeFolder.EnsureExists();
        var path = GetPath(tapeName);

        if (_images.Count == 0)
        {
            if (File.Exists(path))
                File.Delete(path);
            return;
        }

        using var writer = new StreamWriter(path);
        foreach (var name in _images)
            writer.WriteLine($"INCLUDE {name}");
    }

    public static TapeAssetIncludes Load(string tapeName)
    {
        var result = new TapeAssetIncludes();
        TapeFolder.EnsureExists();
        var path = GetPath(tapeName);
        if (!File.Exists(path))
            return result;

        foreach (var raw in File.ReadAllLines(path))
        {
            var line = raw.Trim();
            if (line.StartsWith("INCLUDE ", StringComparison.OrdinalIgnoreCase))
            {
                var name = line["INCLUDE ".Length..].Trim().ToUpperInvariant();
                if (name.Length > 0)
                    result._images.Add(name);
            }
        }

        return result;
    }

    public static void Delete(string tapeName)
    {
        var path = GetPath(tapeName);
        if (File.Exists(path))
            File.Delete(path);
    }

    private static string GetPath(string tapeName)
    {
        if (string.IsNullOrWhiteSpace(tapeName))
            throw new InvalidOperationException("INVALID TAPE NAME");

        return Path.Combine(
            TapeFolder.Location,
            tapeName.Trim().ToUpperInvariant() + ".assets");
    }
}
