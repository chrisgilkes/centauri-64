using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

using Centauri64.Graphics;

namespace Centauri64.Game;

/// <summary>
/// Dark system/boot presentation. Distinct from the Bedroom palette.
/// </summary>
internal static class SystemUi
{
    public static readonly Color Background = new(4, 4, 6);
    public static readonly Color Panel = new(14, 14, 16);
    public static readonly Color Highlight = new(22, 28, 20);
    public static readonly Color Green = new(72, 196, 88);
    public static readonly Color Amber = new(220, 176, 64);
    public static readonly Color Text = new(214, 214, 206);
    public static readonly Color Muted = new(108, 108, 116);
    public static readonly Color Line = new(36, 72, 44);
}

internal static class BootUi
{
    public static readonly Color Background = new(10, 18, 28);
    public static readonly Color Header = new(36, 72, 110);
    public static readonly Color Cyan = new(91, 214, 205);
    public static readonly Color Cream = new(238, 232, 190);
    public static readonly Color Yellow = new(232, 205, 92);
    public static readonly Color Muted = new(130, 165, 170);
    public static readonly Color Dark = new(14, 28, 38);
    public static readonly Color Panel = new(18, 36, 52);
    public static readonly Color Highlight = new(48, 92, 128);

    public static void DrawBox(SpriteBatch spriteBatch, Texture2D pixel, Rectangle rectangle, Color colour)
    {
        spriteBatch.Draw(pixel, rectangle, colour);
    }

    public static void DrawText(BitmapFont font, SpriteBatch spriteBatch, string text, int x, int y, Color colour)
    {
        font.Draw(spriteBatch, text, new Vector2(x, y), colour);
    }

    public static void DrawBorder(SpriteBatch spriteBatch, Texture2D pixel, Rectangle rectangle, Color colour)
    {
        spriteBatch.Draw(pixel, new Rectangle(rectangle.X, rectangle.Y, rectangle.Width, 2), colour);
        spriteBatch.Draw(pixel, new Rectangle(rectangle.X, rectangle.Bottom - 2, rectangle.Width, 2), colour);
        spriteBatch.Draw(pixel, new Rectangle(rectangle.X, rectangle.Y, 2, rectangle.Height), colour);
        spriteBatch.Draw(pixel, new Rectangle(rectangle.Right - 2, rectangle.Y, 2, rectangle.Height), colour);
    }
}
