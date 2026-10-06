using System;
using System.Collections.Generic;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Centauri64.Game;

/// <summary>
/// High-resolution print typography for physical/meta screens.
/// Rasterized at content-build time from a system serif (see Content/Fonts).
/// </summary>
public sealed class PrintFonts
{
    public PrintFonts(SpriteFont title, SpriteFont body, SpriteFont caption)
    {
        Title = title;
        Body = body;
        Caption = caption;
    }

    public SpriteFont Title { get; }
    public SpriteFont Body { get; }
    public SpriteFont Caption { get; }

    public void Draw(
        SpriteBatch spriteBatch,
        SpriteFont font,
        string text,
        int x,
        int y,
        Color colour)
    {
        if (string.IsNullOrEmpty(text))
            return;

        // SpriteFont throws on glyphs outside its CharacterRegions.
        spriteBatch.DrawString(font, Sanitize(font, text), new Vector2(x, y), colour);
    }

    public Vector2 Measure(SpriteFont font, string text)
    {
        var safe = Sanitize(font, text);
        return string.IsNullOrEmpty(safe) ? Vector2.Zero : font.MeasureString(safe);
    }

    public static string Sanitize(SpriteFont font, string text)
    {
        if (string.IsNullOrEmpty(text))
            return text;

        for (var i = 0; i < text.Length; i++)
        {
            if (font.Characters.Contains(text[i]))
                continue;

            var chars = text.ToCharArray();
            for (var j = i; j < chars.Length; j++)
            {
                if (!font.Characters.Contains(chars[j]))
                    chars[j] = Substitute(chars[j]);
            }

            return new string(chars);
        }

        return text;
    }

    private static char Substitute(char value) =>
        value switch
        {
            '•' or '·' => '*',
            '—' or '–' or '−' => '-',
            '…' => '.',
            '\u2018' or '\u2019' => '\'',
            '\u201C' or '\u201D' => '"',
            _ => '?'
        };

    public static string[] Wrap(SpriteFont font, string text, float maxWidth)
    {
        if (string.IsNullOrWhiteSpace(text))
            return Array.Empty<string>();

        text = Sanitize(font, text.Replace('\n', ' '));
        var words = text.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var lines = new List<string>();
        var current = "";
        foreach (var word in words)
        {
            var next = current.Length == 0 ? word : current + " " + word;
            if (current.Length > 0 && font.MeasureString(next).X > maxWidth)
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
}
