using System;

namespace Centauri64.Basic;

/// <summary>
/// Cassette inlay cover artwork. Indexed to the Centauri64 32-colour palette.
/// Logical canvas is deliberately low-resolution; UI displays with integer
/// nearest-neighbour scaling.
/// </summary>
public sealed class TapeCover
{
    public const int Width = 40;

    public const int Height = 56;

    /// <summary>Previous cover resolution. Loaded covers are downsampled once.</summary>
    public const int LegacyWidth = 80;

    /// <summary>Previous cover resolution. Loaded covers are downsampled once.</summary>
    public const int LegacyHeight = 112;

    public int[,] Pixels { get; }

    public TapeCover()
    {
        Pixels = new int[Height, Width];
    }

    public bool HasArt
    {
        get
        {
            for (var y = 0; y < Height; y++)
            {
                for (var x = 0; x < Width; x++)
                {
                    if (Pixels[y, x] != 0)
                        return true;
                }
            }

            return false;
        }
    }

    public TapeCover Clone()
    {
        var copy = new TapeCover();
        CopyPixels(Pixels, copy.Pixels);
        return copy;
    }

    public void Clear()
    {
        for (var y = 0; y < Height; y++)
        {
            for (var x = 0; x < Width; x++)
            {
                Pixels[y, x] = 0;
            }
        }
    }

    public static void CopyPixels(int[,] source, int[,] destination)
    {
        for (var y = 0; y < Height; y++)
        {
            for (var x = 0; x < Width; x++)
            {
                destination[y, x] = source[y, x];
            }
        }
    }

    /// <summary>
    /// Deterministic 2×2 → 1 downsample for legacy 80×112 covers.
    /// Takes the top-left palette index of each block (no smoothing).
    /// </summary>
    public static void DownsampleLegacy(int[,] legacy, int[,] destination)
    {
        if (legacy.GetLength(0) < LegacyHeight ||
            legacy.GetLength(1) < LegacyWidth)
        {
            throw new ArgumentException("Legacy cover buffer is too small.");
        }

        if (destination.GetLength(0) < Height ||
            destination.GetLength(1) < Width)
        {
            throw new ArgumentException("Destination cover buffer is too small.");
        }

        for (var y = 0; y < Height; y++)
        {
            for (var x = 0; x < Width; x++)
            {
                destination[y, x] = legacy[y * 2, x * 2];
            }
        }
    }
}
