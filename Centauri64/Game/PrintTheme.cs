using System;
using System.Collections.Generic;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Centauri64.Game;

/// <summary>
/// 1986 magazine print presentation. Not the bedroom/computer UI palette.
/// </summary>
public static class PrintTheme
{
    public static readonly Color Paper = new(232, 220, 190);
    public static readonly Color PaperDark = new(210, 196, 164);
    public static readonly Color PaperEdge = new(168, 148, 112);
    public static readonly Color Ink = new(28, 24, 20);
    public static readonly Color InkMuted = new(88, 72, 56);
    public static readonly Color Rule = new(48, 40, 32);
    public static readonly Color Masthead = new(136, 0, 0);
    public static readonly Color SpotBlue = new(0, 0, 120);
    public static readonly Color Highlight = new(255, 236, 180);
    public static readonly Color Stamp = new(160, 24, 24);

    public static void FillPaper(SpriteBatch spriteBatch, Texture2D pixel)
    {
        FillPaper(spriteBatch, pixel, MetaUi.Width, MetaUi.Height);
    }

    public static void FillPaper(SpriteBatch spriteBatch, Texture2D pixel, int width, int height)
    {
        spriteBatch.Draw(pixel, new Rectangle(0, 0, width, height), Paper);
        spriteBatch.Draw(pixel, new Rectangle(0, 0, width, 12), PaperEdge);
        spriteBatch.Draw(pixel, new Rectangle(0, height - 12, width, 12), PaperEdge);
        spriteBatch.Draw(pixel, new Rectangle(0, 0, 12, height), PaperEdge);
        spriteBatch.Draw(pixel, new Rectangle(width - 12, 0, 12, height), PaperEdge);
        spriteBatch.Draw(pixel, new Rectangle(20, 20, width - 40, 2), Rule);
        spriteBatch.Draw(pixel, new Rectangle(20, height - 22, width - 40, 2), Rule);
    }

    public static void DrawMasthead(
        SpriteBatch spriteBatch,
        Texture2D pixel,
        PrintFonts print,
        string kicker,
        string title,
        string right)
    {
        spriteBatch.Draw(pixel, new Rectangle(40, 28, MetaUi.Width - 80, 72), Masthead);
        print.Draw(spriteBatch, print.Caption, kicker, 56, 36, Paper);
        print.Draw(spriteBatch, print.Title, title, 56, 52, Color.White);
        if (!string.IsNullOrEmpty(right))
        {
            var size = print.Measure(print.Caption, right);
            print.Draw(
                spriteBatch,
                print.Caption,
                right,
                MetaUi.Width - 56 - (int)size.X,
                56,
                Paper);
        }
    }

    public static void RuleH(SpriteBatch spriteBatch, Texture2D pixel, int x, int y, int width) =>
        spriteBatch.Draw(pixel, new Rectangle(x, y, width, 1), Rule);

    public static void Frame(
        SpriteBatch spriteBatch,
        Texture2D pixel,
        Rectangle bounds,
        Color ink,
        int thickness = 1)
    {
        spriteBatch.Draw(pixel, new Rectangle(bounds.X, bounds.Y, bounds.Width, thickness), ink);
        spriteBatch.Draw(pixel, new Rectangle(bounds.X, bounds.Bottom - thickness, bounds.Width, thickness), ink);
        spriteBatch.Draw(pixel, new Rectangle(bounds.X, bounds.Y, thickness, bounds.Height), ink);
        spriteBatch.Draw(pixel, new Rectangle(bounds.Right - thickness, bounds.Y, thickness, bounds.Height), ink);
    }

    public static void Box(
        SpriteBatch spriteBatch,
        Texture2D pixel,
        Rectangle bounds,
        Color fill)
    {
        spriteBatch.Draw(pixel, bounds, fill);
    }

    public static void Footer(
        SpriteBatch spriteBatch,
        Texture2D pixel,
        PrintFonts print,
        string text)
    {
        var bar = new Rectangle(40, MetaUi.Height - 56, MetaUi.Width - 80, 32);
        spriteBatch.Draw(pixel, bar, PaperDark);
        Frame(spriteBatch, pixel, bar, Rule);
        print.Draw(spriteBatch, print.Caption, text, 52, bar.Y + 8, Ink);
    }

    public static void DrawStamp(
        SpriteBatch spriteBatch,
        Texture2D pixel,
        PrintFonts print,
        Rectangle bounds,
        string text)
    {
        var size = print.Measure(print.Caption, text);
        var width = (int)size.X + 16;
        var x = bounds.Right - width - 12;
        var y = bounds.Y + 10;
        var stamp = new Rectangle(x, y, width, (int)size.Y + 10);
        spriteBatch.Draw(pixel, stamp, Stamp);
        print.Draw(spriteBatch, print.Caption, text, x + 8, y + 4, Color.White);
    }

    public static string[] Wrap(string text, int width)
    {
        if (string.IsNullOrWhiteSpace(text))
            return Array.Empty<string>();

        var words = text.Replace('\n', ' ').Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var lines = new List<string>();
        var current = "";
        foreach (var word in words)
        {
            var next = current.Length == 0 ? word : current + " " + word;
            if (next.Length > width && current.Length > 0)
            {
                lines.Add(current);
                current = word;
            }
            else
            {
                current = next;
            }
        }

        if (current.Length > 0)
            lines.Add(current);

        return lines.ToArray();
    }

    public static string WrapOne(string text, int width)
    {
        var lines = Wrap(text, width);
        return lines.Length == 0 ? text : lines[0];
    }
}
