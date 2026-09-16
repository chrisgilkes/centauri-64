using System;
using Microsoft.Xna.Framework;

namespace Centauri64.Machine;

public static class CentauriPalette
{
    private static readonly Color[] Colors =
    {
        new(0, 0, 0),
        new(255, 255, 255),
        new(136, 0, 0),
        new(170, 255, 238),
        new(204, 68, 204),
        new(0, 204, 85),
        new(0, 0, 170),
        new(238, 238, 119),
        new(221, 136, 85),
        new(102, 68, 0),
        new(255, 119, 119),
        new(51, 51, 51),
        new(119, 119, 119),
        new(170, 255, 102),
        new(0, 136, 255),
        new(187, 187, 187)
    };

    public static Color Get(int index)
    {
        if (index < 0 || index >= Colors.Length)
        {
            throw new ArgumentOutOfRangeException(
                nameof(index));
        }

        return Colors[index];
    }
}