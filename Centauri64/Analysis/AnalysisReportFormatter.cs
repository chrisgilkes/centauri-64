using System;
using System.Collections.Generic;
using System.Text;

namespace Centauri64.Analysis;

public static class AnalysisReportFormatter
{
    public static IReadOnlyList<string> FormatLines(SoftwareAnalysis analysis)
    {
        var lines = new List<string>();
        var title = string.IsNullOrWhiteSpace(analysis.TapeName)
            ? "CURRENT PROGRAM"
            : analysis.TapeName.ToUpperInvariant();

        lines.Add(title + " - PROGRAM REPORT");
        lines.Add("");

        if (!analysis.ProgramValid)
        {
            lines.Add("PROGRAM VALID       NO");
            lines.Add(analysis.ValidationError ?? "UNKNOWN ERROR");
            return lines;
        }

        lines.Add("PROGRAM");
        lines.Add($"LINES               {analysis.LineCount}");
        lines.Add($"STATEMENTS          {analysis.StatementCount}");
        lines.Add($"COMMANDS USED       {analysis.UniqueCommandCount}");
        lines.Add("");
        lines.Add("CAPABILITIES");
        lines.Add(Flag("TEXT", analysis.UsesText));
        lines.Add(Flag("INPUT", analysis.UsesInput));
        lines.Add(Flag("GRAPHICS", analysis.UsesGraphics));
        lines.Add(Flag("SPRITES", analysis.UsesSprites));
        lines.Add(Flag("ANIMATION", analysis.UsesAnimation));
        lines.Add(Flag("SOUND", analysis.UsesSound));
        lines.Add(Flag("MAPS", analysis.UsesMaps));
        lines.Add(Flag("STRINGS", analysis.UsesStrings));
        lines.Add(Flag("RANDOM", analysis.UsesRandom));
        lines.Add(Flag("NETWORKING", analysis.UsesNetworking));
        lines.Add(Flag("CAMERA", analysis.UsesCamera));
        lines.Add("");
        lines.Add("ASSETS");
        lines.Add($"SPRITES             {analysis.SpriteCount}");
        lines.Add($"ANIMATIONS          {analysis.AnimationCount}");
        lines.Add($"MAPS                {analysis.MapCount}");
        lines.Add("");
        lines.Add("COVER");
        lines.Add(Flag("PRESENT", analysis.HasCover));
        lines.Add(Flag("CUSTOM", analysis.HasCustomCover));
        lines.Add($"COLOURS             {analysis.CoverColourCount}");
        lines.Add($"COVERAGE            {FormatCoverage(analysis.CoverCoveragePercent)}");
        lines.Add($"PIXELS CHANGED      {analysis.CoverChangedPixelCount}");

        if (analysis.CommandsUsed.Count > 0)
        {
            lines.Add("");
            lines.Add("COMMANDS USED");

            foreach (var command in analysis.CommandsUsed)
                lines.Add(command);
        }

        return lines;
    }

    public static string FormatText(SoftwareAnalysis analysis)
    {
        var builder = new StringBuilder();

        foreach (var line in FormatLines(analysis))
            builder.AppendLine(line);

        return builder.ToString();
    }

    private static string Flag(string label, bool value)
    {
        return $"{label,-18} {(value ? "YES" : "NO")}";
    }

    private static string FormatCoverage(double percent)
    {
        if (percent <= 0)
            return "0%";

        if (percent < 1)
            return percent.ToString("0.##") + "%";

        if (percent < 10)
            return percent.ToString("0.#") + "%";

        return Math.Round(percent).ToString("0") + "%";
    }
}
