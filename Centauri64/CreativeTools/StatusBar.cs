using Centauri64.Graphics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Centauri64.CreativeTools;

public sealed class StatusBar
{
    private readonly int _y;
    private readonly int _height;

    public StatusBar(int y, int height = 16)
    {
        _y = y;
        _height = height;
    }

    public void Draw(
        SpriteBatch spriteBatch,
        BitmapFont font,
        Texture2D pixel,
        int width,
        string left,
        string right = "")
    {
        spriteBatch.Draw(
            pixel,
            new Rectangle(0, _y, width, _height),
            CreativeUiTheme.Panel);

        if (!string.IsNullOrEmpty(left))
        {
            font.Draw(
                spriteBatch,
                left,
                new Vector2(6, _y + 4),
                CreativeUiTheme.Text);
        }

        if (!string.IsNullOrEmpty(right))
        {
            font.Draw(
                spriteBatch,
                right,
                new Vector2(width - 8 - right.Length * 8, _y + 4),
                CreativeUiTheme.Muted);
        }
    }
}
