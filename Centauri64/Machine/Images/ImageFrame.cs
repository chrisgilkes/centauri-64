using System;

namespace Centauri64.Machine.Images;

/// <summary>
/// One raster frame inside an ImageAsset. All frames share the parent size/mode.
/// Animation timing belongs to the future Sprite Builder — not here.
/// </summary>
public sealed class ImageFrame
{
    public int[,] Pixels { get; }

    public int Revision { get; private set; }

    public ImageFrame(int width, int height, int fillColour = ImageAsset.Transparent)
    {
        if (width < 1 || height < 1)
            throw new ArgumentOutOfRangeException(nameof(width));

        Pixels = new int[height, width];
        for (var y = 0; y < height; y++)
        {
            for (var x = 0; x < width; x++)
                Pixels[y, x] = fillColour;
        }
    }

    public ImageFrame Clone()
    {
        var height = Pixels.GetLength(0);
        var width = Pixels.GetLength(1);
        var copy = new ImageFrame(width, height);
        for (var y = 0; y < height; y++)
        {
            for (var x = 0; x < width; x++)
                copy.Pixels[y, x] = Pixels[y, x];
        }

        return copy;
    }

    public void Clear(int colour = ImageAsset.Transparent)
    {
        var height = Pixels.GetLength(0);
        var width = Pixels.GetLength(1);
        for (var y = 0; y < height; y++)
        {
            for (var x = 0; x < width; x++)
                Pixels[y, x] = colour;
        }

        MarkChanged();
    }

    public void MarkChanged() => Revision++;
}
