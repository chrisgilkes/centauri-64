using System;
using System.IO;
using System.Text;

using Centauri64.Basic;

namespace Centauri64.Machine.Maps;

public sealed class MapStorage
{
    private readonly string _programDirectory;

    public MapStorage()
    {
        TapeFolder.EnsureExists();
        _programDirectory = TapeFolder.Location;
    }

    public void Save(string name, MapAssetStore maps)
    {
        var path = GetMapPath(name);

        using var writer = new StreamWriter(path);

        foreach (var map in maps.Maps)
        {
            writer.WriteLine($"MAP {map.Name}");
            writer.WriteLine($"SIZE {map.Columns} {map.Rows}");

            foreach (var pair in map.Definitions)
            {
                writer.WriteLine($"TDEF {pair.Key} {pair.Value}");
            }

            for (var row = 0; row < map.Rows; row++)
            {
                var line = new StringBuilder("ROW ");

                for (var column = 0; column < map.Columns; column++)
                {
                    if (column > 0)
                        line.Append(',');

                    line.Append(map.Cells[(row * map.Columns) + column]);
                }

                writer.WriteLine(line.ToString());
            }
        }
    }

    public void Load(string name, MapAssetStore maps)
    {
        var path = GetMapPath(name);

        maps.Clear();

        if (!File.Exists(path))
            return;

        var lines = File.ReadAllLines(path);
        string? pendingName = null;
        MapAsset? current = null;
        var rowIndex = 0;

        foreach (var raw in lines)
        {
            var line = raw.Trim();

            if (line.Length == 0)
                continue;

            if (line.StartsWith("MAP "))
            {
                pendingName = line["MAP ".Length..].Trim();
                current = null;
                rowIndex = 0;
                continue;
            }

            if (line.StartsWith("SIZE "))
            {
                var parts = line["SIZE ".Length..]
                    .Split(' ', StringSplitOptions.RemoveEmptyEntries);

                if (parts.Length != 2 ||
                    pendingName == null)
                {
                    continue;
                }

                if (!int.TryParse(parts[0], out var columns) ||
                    !int.TryParse(parts[1], out var rows) ||
                    columns <= 0 ||
                    rows <= 0)
                {
                    continue;
                }

                current = new MapAsset(pendingName, columns, rows);
                maps.Add(current);
                rowIndex = 0;
                continue;
            }

            if (current == null)
                continue;

            if (line.StartsWith("TDEF "))
            {
                var parts = line["TDEF ".Length..]
                    .Split(' ', StringSplitOptions.RemoveEmptyEntries);

                if (parts.Length < 2)
                    continue;

                if (!int.TryParse(parts[0], out var id))
                    continue;

                current.Definitions[id] = parts[1];
                continue;
            }

            if (!line.StartsWith("ROW "))
                continue;

            if (rowIndex >= current.Rows)
                continue;

            var values = line["ROW ".Length..].Split(',');

            if (values.Length != current.Columns)
                continue;

            for (var column = 0; column < current.Columns; column++)
            {
                if (int.TryParse(values[column], out var id))
                {
                    current.Cells[(rowIndex * current.Columns) + column] = id;
                }
            }

            rowIndex++;
        }
    }

    public void Delete(string name)
    {
        var path = GetMapPath(name);

        if (File.Exists(path))
            File.Delete(path);
    }

    private string GetMapPath(string name)
    {
        return Path.Combine(
            _programDirectory,
            ValidateName(name) + ".maps");
    }

    private static string ValidateName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new InvalidOperationException(
                "Map save name cannot be empty.");
        }

        if (name.Contains(".."))
        {
            throw new InvalidOperationException(
                "Invalid map save name.");
        }

        foreach (var character in Path.GetInvalidFileNameChars())
        {
            if (name.Contains(character))
            {
                throw new InvalidOperationException(
                    "Invalid map save name.");
            }
        }

        return name.ToUpperInvariant();
    }
}
