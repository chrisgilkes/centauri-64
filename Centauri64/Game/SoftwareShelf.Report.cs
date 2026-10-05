using System.Collections.Generic;

using Centauri64.Analysis;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace Centauri64.Game;

public sealed partial class SoftwareShelf
{
    private readonly SoftwareAnalyser _analyser = new();
    private bool _showingReport;
    private IReadOnlyList<string> _reportLines = System.Array.Empty<string>();
    private int _reportScroll;

    private void OpenReport()
    {
        if (_tapes.Count == 0)
            return;

        var analysis = _analyser.AnalyseTape(_tapes[_selected]);
        _reportLines = AnalysisReportFormatter.FormatLines(analysis);
        _reportScroll = 0;
        _showingReport = true;
    }

    private void CloseReport()
    {
        _showingReport = false;
        _reportLines = System.Array.Empty<string>();
        _reportScroll = 0;
    }

    private void UpdateReport(KeyboardState keyboard)
    {
        if (Pressed(keyboard, Keys.Escape) ||
            Pressed(keyboard, Keys.Enter) ||
            Pressed(keyboard, Keys.A))
        {
            CloseReport();
            return;
        }

        if (Pressed(keyboard, Keys.Up))
            _reportScroll = System.Math.Max(0, _reportScroll - 1);

        if (Pressed(keyboard, Keys.Down))
        {
            var maxScroll = System.Math.Max(0, _reportLines.Count - 18);
            _reportScroll = System.Math.Min(maxScroll, _reportScroll + 1);
        }
    }

    private void DrawReport(SpriteBatch spriteBatch)
    {
        DrawText(spriteBatch, "SOFTWARE REPORT", 256, 24, Cream);
        DrawText(spriteBatch, _tapes[_selected], 48, 44, Cyan);

        var y = 80;
        var end = System.Math.Min(_reportScroll + 18, _reportLines.Count);

        for (var i = _reportScroll; i < end; i++)
        {
            var line = _reportLines[i];
            var colour = line.EndsWith("YES")
                ? Yellow
                : line.EndsWith("NO")
                    ? Muted
                    : Cream;

            if (IsReportHeading(line))
                colour = Cyan;

            DrawText(spriteBatch, line, 48, y, colour);
            y += 16;
        }

        DrawBox(
            spriteBatch,
            new Rectangle(16, 424, 608, 24),
            Cyan);

        DrawText(
            spriteBatch,
            "UP DOWN SCROLL    ESC BACK",
            48,
            432,
            Dark);
    }

    private static bool IsReportHeading(string line)
    {
        if (line is "PROGRAM" or "CAPABILITIES" or "ASSETS" or "COVER" or "COMMANDS USED")
            return true;

        return line.Contains("PROGRAM REPORT");
    }
}
