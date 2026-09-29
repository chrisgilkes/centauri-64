namespace Centauri64.Basic;

public sealed class TapeCover
{
    public const int Width = 80;

    public const int Height = 112;

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
}
