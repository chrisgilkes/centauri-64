namespace Centauri64.Machine;

public sealed class SpriteAsset
{
    public string Name { get; }

    public int[,] Pixels { get; }

    public SpriteAsset(string name)
    {
        Name = name;

        Pixels = new int[
            CentauriSprite.HEIGHT,
            CentauriSprite.WIDTH];

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