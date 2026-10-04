using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Centauri64.Machine;

public sealed class SpriteRenderer
{
    public void Draw(
        SpriteBatch spriteBatch,
        Texture2D pixel,
        IReadOnlyList<CentauriSprite> sprites,
        int cameraX,
        int cameraY)
    {
        foreach (var sprite in sprites)
        {
            if (!sprite.Visible)
                continue;

            DrawSprite(
                spriteBatch,
                pixel,
                sprite,
                cameraX,
                cameraY);
        }
    }

    private static void DrawSprite(
        SpriteBatch spriteBatch,
        Texture2D pixel,
        CentauriSprite sprite,
        int cameraX,
        int cameraY)
    {
        for (var y = 0; y < CentauriSprite.HEIGHT; y++)
        {
            for (var x = 0; x < CentauriSprite.WIDTH; x++)
            {
                var colourIndex =
                    sprite.Pixels[y, x];

                if (colourIndex ==
                    CentauriSprite.TRANSPARENT)
                {
                    continue;
                }

                var colour =
                    CentauriPalette.Get(colourIndex);

                var px = sprite.FlipX
                    ? CentauriSprite.WIDTH - 1 - x
                    : x;

                spriteBatch.Draw(
                    pixel,
                    new Rectangle(
                        sprite.X + px - cameraX,
                        sprite.Y + y - cameraY,
                        1,
                        1),
                    colour);
            }
        }
    }
}
