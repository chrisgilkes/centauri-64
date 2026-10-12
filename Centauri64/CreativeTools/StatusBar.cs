using System;

using Centauri64.Graphics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Centauri64.CreativeTools;

public sealed class StatusBar
{
    private const int CharWidth = 8;
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

        left ??= "";
        right ??= "";

        var rightPixels = right.Length * CharWidth;
        var gap = string.IsNullOrEmpty(right) ? 0 : 16;
        var maxLeftChars = Math.Max(0, (width - 12 - rightPixels - gap) / CharWidth);
        if (left.Length > maxLeftChars)
            left = maxLeftChars <= 3 ? left[..maxLeftChars] : left[..(maxLeftChars - 3)] + "...";

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
                new Vector2(width - 8 - rightPixels, _y + 4),
                CreativeUiTheme.Muted);
        }
    }
}
