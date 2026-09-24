namespace Centauri64.Machine;

public sealed class CentauriSprite
{
    public const int WIDTH = 16;
    public const int HEIGHT = 16;

    public int X { get; set; }
    public int Y { get; set; }

    public bool Visible { get; set; }

    public int[,] Pixels { get; }

    public string? AssetName { get; set; }

    public string AnimationName { get; set; } = "DEFAULT";

    public int AnimationFrame { get; set; }

    public float AnimationTimer { get; set; }

    public bool AnimationPlaying { get; set; } = true;

    public bool AnimationLoop { get; set; } = true;

    public string? PreviousAnimationName { get; set; }

    public const int TRANSPARENT = -1;

    public CentauriSprite()
    {
        Pixels = new int[HEIGHT, WIDTH];

        for (var y = 0; y < HEIGHT; y++)
        {
            for (var x = 0; x < WIDTH; x++)
            {
                Pixels[y, x] = TRANSPARENT;
            }
        }
    }
    
}