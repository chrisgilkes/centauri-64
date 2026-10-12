using Centauri64.Machine.Images;

namespace Centauri64.Machine.Sprites;

public sealed class SpriteFrame
{
    public int[,] Pixels { get; }

    /// <summary>How long this frame is shown during animation playback.</summary>
    public float DurationSeconds { get; set; } = ImageFrame.DefaultDurationSeconds;

    public SpriteFrame()
    {
        Pixels = new int[
            CentauriSprite.HEIGHT,
            CentauriSprite.WIDTH];

        Clear();
    }

    public void Clear()
    {
        for (var y = 0;
             y < CentauriSprite.HEIGHT;
             y++)
        {
            for (var x = 0;
                 x < CentauriSprite.WIDTH;
                 x++)
            {
                Pixels[y, x] =
                    CentauriSprite.TRANSPARENT;
            }
        }
    }

    public SpriteFrame Clone()
    {
        var frame =
            new SpriteFrame
            {
                DurationSeconds = DurationSeconds
            };

        for (var y = 0;
            y < CentauriSprite.HEIGHT;
            y++)
        {
            for (var x = 0;
                x < CentauriSprite.WIDTH;
                x++)
            {
                frame.Pixels[y, x] =
                    Pixels[y, x];
            }
        }

        return frame;
    }
}
