using System;
using Microsoft.Xna.Framework;

namespace Centauri64.Machine;

public static class CentauriPalette
{
    public const int MAX_COLORS = 32;

    private static readonly Color[] Colors =
    {
        // 0-15: Original Centauri palette
        new(0, 0, 0),         // 00 Black
        new(255, 255, 255),   // 01 White
        new(136, 0, 0),       // 02 Red
        new(170, 255, 238),   // 03 Cyan
        new(204, 68, 204),    // 04 Purple
        new(0, 204, 85),      // 05 Green
        new(0, 0, 170),       // 06 Blue
        new(238, 238, 119),   // 07 Yellow
        new(221, 136, 85),    // 08 Orange
        new(102, 68, 0),      // 09 Brown
        new(255, 119, 119),   // 10 Light Red
        new(51, 51, 51),      // 11 Dark Grey
        new(119, 119, 119),   // 12 Grey
        new(170, 255, 102),   // 13 Light Green
        new(0, 136, 255),     // 14 Light Blue
        new(187, 187, 187),   // 15 Light Grey

        // 16-31: Extended Centauri64 palette
        new(20, 30, 55),      // 16 Midnight Blue
        new(35, 55, 90),      // 17 Navy
        new(40, 100, 120),    // 18 Teal
        new(70, 180, 170),    // 19 Aqua
        new(25, 90, 55),      // 20 Dark Green
        new(110, 160, 70),    // 21 Moss Green
        new(90, 55, 35),      // 22 Dark Brown
        new(175, 110, 65),    // 23 Tan
        new(105, 45, 80),     // 24 Plum
        new(170, 80, 120),    // 25 Rose
        new(110, 70, 170),    // 26 Violet
        new(165, 130, 220),   // 27 Lavender
        new(220, 170, 90),    // 28 Gold
        new(255, 205, 150),   // 29 Peach
        new(150, 190, 210),   // 30 Steel Blue
        new(225, 225, 205)    // 31 Warm White
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