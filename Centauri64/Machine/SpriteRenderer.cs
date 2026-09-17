
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Centauri64.Machine;

public sealed class SpriteRenderer
{
    public void Draw(SpriteBatch spriteBatch,Texture2D pixel,IReadOnlyList<CentauriSprite> sprites)
    {
        foreach (var sprite in sprites)
        {
            if (!sprite.Visible)
                continue;

            DrawSprite(
                spriteBatch,
                pixel,
                sprite);
        }
    }

    private static void DrawSprite(
        SpriteBatch spriteBatch,
        Texture2D pixel,
        CentauriSprite sprite)
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

                spriteBatch.Draw(
                    pixel,
                    new Rectangle(
                        sprite.X + x,
                        sprite.Y + y,
                        1,
                        1),
                    colour);
            }
        }
    }
}