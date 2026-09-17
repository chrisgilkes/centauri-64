namespace Centauri64.Machine.Sprites;

public sealed class SpriteFrame
{
    public int[,] Pixels { get; }

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
}