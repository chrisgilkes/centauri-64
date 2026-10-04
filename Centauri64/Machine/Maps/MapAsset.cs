using System;
using System.Collections.Generic;

namespace Centauri64.Machine.Maps;

public sealed class MapAsset
{
    public string Name { get; private set; }

    public int Columns { get; private set; }

    public int Rows { get; private set; }

    public int[] Cells { get; private set; }

    public Dictionary<int, string> Definitions { get; } = new();

    public MapAsset(string name, int columns, int rows)
    {
        if (columns <= 0 || rows <= 0)
        {
            throw new InvalidOperationException(
                "Map size must be greater than zero.");
        }

        Name = name;
        Columns = columns;
        Rows = rows;
        Cells = new int[columns * rows];
    }

    public void Rename(string name)
    {
        Name = name;
    }

    public void Resize(int columns, int rows)
    {
        if (columns <= 0 || rows <= 0)
        {
            throw new InvalidOperationException(
                "Map size must be greater than zero.");
        }

        var next = new int[columns * rows];
        var copyColumns = Math.Min(Columns, columns);
        var copyRows = Math.Min(Rows, rows);

        for (var row = 0; row < copyRows; row++)
        {
            for (var column = 0; column < copyColumns; column++)
            {
                next[(row * columns) + column] =
                    Cells[(row * Columns) + column];
            }
        }

        Columns = columns;
        Rows = rows;
        Cells = next;
    }

    public int GetCell(int column, int row)
    {
        if (column < 0 || column >= Columns ||
            row < 0 || row >= Rows)
        {
            return 0;
        }

        return Cells[(row * Columns) + column];
    }

    public void SetCell(int column, int row, int id)
    {
        if (column < 0 || column >= Columns ||
            row < 0 || row >= Rows)
        {
            return;
        }

        Cells[(row * Columns) + column] = id;
    }

    public MapAsset Clone(string newName)
    {
        var copy = new MapAsset(newName, Columns, Rows);
        Array.Copy(Cells, copy.Cells, Cells.Length);

        foreach (var pair in Definitions)
        {
            copy.Definitions[pair.Key] = pair.Value;
        }

        return copy;
    }
}
