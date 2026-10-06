using System;
using System.Collections.Generic;
using System.Linq;

using Centauri64.Analysis;
using Centauri64.Basic;
using Centauri64.Publishing;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Centauri64.Game;

/// <summary>
/// Lightweight measured layout for high-resolution print UI.
/// Variable-length copy flows; no fixed bitmap-font line steps.
/// </summary>
public sealed class PrintLayout
{
    private readonly PrintFonts _print;
    private readonly Texture2D _pixel;
    private readonly SpriteBatch _spriteBatch;
    private int _x;
    private int _width;
    private int _y;
    private readonly int _bottom;

    public PrintLayout(
        SpriteBatch spriteBatch,
        Texture2D pixel,
        PrintFonts print,
        int x,
        int y,
        int width,
        int bottom = 880)
    {
        _spriteBatch = spriteBatch;
        _pixel = pixel;
        _print = print;
        _x = x;
        _y = y;
        _width = width;
        _bottom = bottom;
    }

    public int Y => _y;
    public int X => _x;
    public int Width => _width;

    public void MoveTo(int x, int y, int width)
    {
        _x = x;
        _y = y;
        _width = width;
    }

    public void Space(int pixels) => _y += pixels;

    public void Rule(Color? colour = null)
    {
        var ink = colour ?? PrintTheme.Rule;
        _spriteBatch.Draw(_pixel, new Rectangle(_x, _y, _width, 1), ink);
        _y += 12;
    }

    public void Heading(string text, Color? colour = null)
    {
        DrawStyle(text, _print.Title, colour ?? PrintTheme.Ink, after: 10);
    }

    public void Section(string text, Color? colour = null)
    {
        DrawStyle(text, _print.Caption, colour ?? PrintTheme.Masthead, after: 8);
    }

    public void Label(string text, Color? colour = null)
    {
        DrawStyle(text, _print.Caption, colour ?? PrintTheme.InkMuted, after: 6);
    }

    public void BodyLine(string text, Color? colour = null)
    {
        DrawStyle(text, _print.Body, colour ?? PrintTheme.Ink, after: 6);
    }

    public void CaptionLine(string text, Color? colour = null)
    {
        DrawStyle(text, _print.Caption, colour ?? PrintTheme.InkMuted, after: 4);
    }

    public void Paragraph(string text, Color? colour = null, int maxLines = 40)
    {
        if (string.IsNullOrWhiteSpace(text))
            return;

        foreach (var block in text.Replace("\r\n", "\n").Split('\n'))
        {
            if (string.IsNullOrWhiteSpace(block))
            {
                _y += 10;
                continue;
            }

            var lines = PrintFonts.Wrap(_print.Body, block.Trim(), _width);
            var drawn = 0;
            foreach (var line in lines)
            {
                if (drawn >= maxLines || _y > _bottom)
                    return;

                DrawStyle(line, _print.Body, colour ?? PrintTheme.Ink, after: 4);
                drawn++;
            }

            _y += 6;
        }
    }

    public void BulletList(IEnumerable<string> items, Color? colour = null, string marker = "* ")
    {
        foreach (var item in items)
        {
            if (string.IsNullOrWhiteSpace(item))
                continue;

            var prefix = marker;
            var lines = PrintFonts.Wrap(_print.Body, prefix + item.Trim(), _width);
            var first = true;
            foreach (var line in lines)
            {
                if (_y > _bottom)
                    return;

                DrawStyle(line, _print.Body, colour ?? PrintTheme.Ink, after: 4);
                if (first)
                    first = false;
            }

            _y += 4;
        }
    }

    public void PriceCallout(string label, string price, int boxWidth = 220)
    {
        var height = 96;
        var bounds = new Rectangle(_x, _y, Math.Min(boxWidth, _width), height);
        PrintTheme.Box(_spriteBatch, _pixel, bounds, PrintTheme.Highlight);
        PrintTheme.Frame(_spriteBatch, _pixel, bounds, PrintTheme.Masthead, 2);
        _print.Draw(_spriteBatch, _print.Caption, label, bounds.X + 16, bounds.Y + 14, PrintTheme.Masthead);
        _print.Draw(_spriteBatch, _print.Title, price, bounds.X + 16, bounds.Y + 42, PrintTheme.Ink);
        _y = bounds.Bottom + 16;
    }

    public void BoxFrame(Rectangle bounds, Color fill, Color border, int thickness = 2)
    {
        PrintTheme.Box(_spriteBatch, _pixel, bounds, fill);
        PrintTheme.Frame(_spriteBatch, _pixel, bounds, border, thickness);
    }

    private void DrawStyle(string text, SpriteFont font, Color colour, int after)
    {
        if (string.IsNullOrEmpty(text) || _y > _bottom)
            return;

        var size = _print.Measure(font, text);
        _print.Draw(_spriteBatch, font, text, _x, _y, colour);
        _y += Math.Max((int)Math.Ceiling(size.Y), font.LineSpacing) + after;
    }
}

/// <summary>
/// Player-facing requirement copy for printed adverts. Presentation only.
/// </summary>
public static class PrintRequirements
{
    public static IReadOnlyList<string> Describe(SubmissionRequirements requirements)
    {
        var lines = new List<string>();

        if (requirements.ProgramValid)
            lines.Add("A working Centauri64 BASIC program");

        if (requirements.RequiredKind == TapeKind.Game)
            lines.Add("A real game, not just a short demo");
        else if (requirements.RequiredKind.HasValue)
            lines.Add("Software type: " + FriendlyKind(requirements.RequiredKind.Value));

        if (requirements.RequiredGenre.HasValue &&
            requirements.RequiredGenre.Value != GameGenre.None)
        {
            lines.Add("Genre: " + requirements.RequiredGenre.Value.ToString().ToUpperInvariant());
        }

        if (requirements.AllowedGenres is { Length: > 0 })
        {
            lines.Add("Genre: " + string.Join(" / ",
                requirements.AllowedGenres.Select(g => g.ToString().ToUpperInvariant())));
        }

        if (requirements.RequiredPlayers.HasValue)
        {
            lines.Add(requirements.RequiredPlayers.Value switch
            {
                TapePlayers.TwoNetwork => "Two players over the Centauri network",
                TapePlayers.TwoLocal => "Two players on one machine",
                _ => "One player"
            });
        }

        foreach (SoftwareCapability flag in Enum.GetValues<SoftwareCapability>())
        {
            if (flag == SoftwareCapability.None)
                continue;
            if ((requirements.RequiredCapabilities & flag) == 0)
                continue;
            lines.Add(FriendlyCapability(flag));
        }

        if (requirements.AnyOfCapabilities is { Length: > 0 })
        {
            var parts = requirements.AnyOfCapabilities
                .Select(DescribeCapabilityGroup)
                .Where(s => !string.IsNullOrWhiteSpace(s))
                .ToArray();
            if (parts.Length > 0)
                lines.Add(string.Join(" or ", parts));
        }

        if (requirements.RequiresCustomCover)
            lines.Add("A custom cassette cover");

        if (requirements.MinimumLineCount > 0)
            lines.Add("At least " + requirements.MinimumLineCount + " lines");

        if (requirements.MaximumLineCount > 0)
            lines.Add("No more than " + requirements.MaximumLineCount + " lines");

        if (requirements.MaximumStatementCount > 0)
            lines.Add("No more than " + requirements.MaximumStatementCount + " statements");

        if (lines.Count == 0)
            lines.Add("Something that actually runs on a Centauri64");

        return lines;
    }

    public static string FriendlyCheckLabel(string label)
    {
        if (string.IsNullOrWhiteSpace(label))
            return label;

        if (label.Equals("PROGRAM VALID", StringComparison.OrdinalIgnoreCase))
            return "PROGRAM LOADS AND RUNS";
        if (label.StartsWith("TYPE ", StringComparison.OrdinalIgnoreCase))
            return "CORRECT SOFTWARE TYPE";
        if (label.Contains("INPUT", StringComparison.OrdinalIgnoreCase))
            return "USES KEYBOARD INPUT";
        if (label.Contains("SPRITE", StringComparison.OrdinalIgnoreCase))
            return "USES SPRITES";
        if (label.Contains("GRAPHIC", StringComparison.OrdinalIgnoreCase))
            return "INCLUDES GRAPHICS";
        if (label.Contains("NETWORK", StringComparison.OrdinalIgnoreCase))
            return "NETWORK READY";
        if (label.Contains("COVER", StringComparison.OrdinalIgnoreCase))
            return "CUSTOM CASSETTE COVER";
        if (label.Contains(" OR ", StringComparison.OrdinalIgnoreCase))
            return "GRAPHICS OR SPRITES";

        return label;
    }

    private static string FriendlyKind(TapeKind kind) =>
        kind.ToString().ToUpperInvariant();

    private static string FriendlyCapability(SoftwareCapability flag) =>
        flag switch
        {
            SoftwareCapability.Input => "Uses keyboard input",
            SoftwareCapability.Graphics => "Includes graphics",
            SoftwareCapability.Sprites => "Uses sprites",
            SoftwareCapability.Text => "Uses text on screen",
            SoftwareCapability.Strings => "Uses strings",
            SoftwareCapability.Sound => "Uses sound",
            SoftwareCapability.Maps => "Uses maps",
            SoftwareCapability.Images => "Uses images",
            SoftwareCapability.Networking => "Works over the Centauri network",
            SoftwareCapability.Animation => "Uses animation",
            SoftwareCapability.Random => "Uses random numbers",
            SoftwareCapability.Camera => "Uses the camera",
            SoftwareCapability.Backgrounds => "Uses backgrounds",
            SoftwareCapability.Foreground => "Uses foreground images",
            _ => flag.ToString().Replace('_', ' ')
        };

    private static string DescribeCapabilityGroup(SoftwareCapability group)
    {
        var parts = new List<string>();
        foreach (SoftwareCapability flag in Enum.GetValues<SoftwareCapability>())
        {
            if (flag == SoftwareCapability.None)
                continue;
            if ((group & flag) != 0)
                parts.Add(FriendlyCapability(flag).ToLowerInvariant());
        }

        return parts.Count == 0 ? string.Empty : string.Join(" and ", parts);
    }
}
