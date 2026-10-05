using System.Collections.Generic;

using Centauri64.Analysis;
using Centauri64.Basic;

namespace Centauri64.Publishing;

public static class SubmissionEvaluator
{
    public static SubmissionResult Evaluate(
        SubmissionRequirements requirements,
        SoftwareAnalysis analysis,
        TapeLabel label)
    {
        var checks = new List<SubmissionCheck>();

        if (requirements.ProgramValid)
        {
            checks.Add(new SubmissionCheck(
                "PROGRAM VALID",
                analysis.ProgramValid));
        }

        if (requirements.RequiredKind.HasValue)
        {
            checks.Add(new SubmissionCheck(
                "TYPE " + requirements.RequiredKind.Value.ToString().ToUpperInvariant(),
                label.Kind == requirements.RequiredKind.Value));
        }

        if (requirements.RequiredGenre.HasValue &&
            requirements.RequiredGenre.Value != GameGenre.None)
        {
            checks.Add(new SubmissionCheck(
                "GENRE " + requirements.RequiredGenre.Value.ToString().ToUpperInvariant(),
                label.Genre == requirements.RequiredGenre.Value));
        }

        if (requirements.RequiredPlayers.HasValue)
        {
            checks.Add(new SubmissionCheck(
                "PLAYERS " + FormatPlayers(requirements.RequiredPlayers.Value),
                label.Players == requirements.RequiredPlayers.Value));
        }

        AddCapabilityChecks(checks, requirements.RequiredCapabilities, analysis);

        if (requirements.AnyOfCapabilities is { Length: > 0 })
        {
            var anyPassed = false;
            var labels = new List<string>();

            foreach (var group in requirements.AnyOfCapabilities)
            {
                labels.Add(DescribeCapabilities(group));
                if (analysis.Has(group))
                    anyPassed = true;
            }

            checks.Add(new SubmissionCheck(
                "ANY OF " + string.Join(" OR ", labels),
                anyPassed));
        }

        if (requirements.RequiresCustomCover)
        {
            checks.Add(new SubmissionCheck(
                "CUSTOM COVER REQUIRED",
                analysis.HasCustomCover));
        }

        if (requirements.MinimumLineCount > 0)
        {
            checks.Add(new SubmissionCheck(
                $"AT LEAST {requirements.MinimumLineCount} LINES",
                analysis.LineCount >= requirements.MinimumLineCount));
        }

        if (requirements.MinimumUniqueCommands > 0)
        {
            checks.Add(new SubmissionCheck(
                $"AT LEAST {requirements.MinimumUniqueCommands} COMMANDS",
                analysis.UniqueCommandCount >= requirements.MinimumUniqueCommands));
        }

        if (requirements.MinimumSpriteCount > 0)
        {
            checks.Add(new SubmissionCheck(
                $"AT LEAST {requirements.MinimumSpriteCount} SPRITES",
                analysis.SpriteCount >= requirements.MinimumSpriteCount));
        }

        if (requirements.MinimumAnimationCount > 0)
        {
            checks.Add(new SubmissionCheck(
                $"AT LEAST {requirements.MinimumAnimationCount} ANIMATIONS",
                analysis.AnimationCount >= requirements.MinimumAnimationCount));
        }

        if (requirements.MinimumMapCount > 0)
        {
            checks.Add(new SubmissionCheck(
                $"AT LEAST {requirements.MinimumMapCount} MAPS",
                analysis.MapCount >= requirements.MinimumMapCount));
        }

        if (requirements.MinimumCoverColours > 0)
        {
            checks.Add(new SubmissionCheck(
                $"AT LEAST {requirements.MinimumCoverColours} COVER COLOURS",
                analysis.CoverColourCount >= requirements.MinimumCoverColours));
        }

        if (requirements.MaximumCoverColours > 0)
        {
            checks.Add(new SubmissionCheck(
                $"AT MOST {requirements.MaximumCoverColours} COVER COLOURS",
                analysis.CoverColourCount <= requirements.MaximumCoverColours));
        }

        var accepted = true;

        foreach (var check in checks)
        {
            if (!check.Passed)
            {
                accepted = false;
                break;
            }
        }

        return new SubmissionResult
        {
            Accepted = accepted,
            Checks = checks
        };
    }

    private static void AddCapabilityChecks(
        List<SubmissionCheck> checks,
        SoftwareCapability required,
        SoftwareAnalysis analysis)
    {
        foreach (SoftwareCapability flag in System.Enum.GetValues<SoftwareCapability>())
        {
            if (flag == SoftwareCapability.None)
                continue;

            if ((required & flag) == 0)
                continue;

            checks.Add(new SubmissionCheck(
                "USES " + flag.ToString().ToUpperInvariant(),
                analysis.Has(flag)));
        }
    }

    private static string DescribeCapabilities(SoftwareCapability capabilities)
    {
        var parts = new List<string>();

        foreach (SoftwareCapability flag in System.Enum.GetValues<SoftwareCapability>())
        {
            if (flag == SoftwareCapability.None)
                continue;

            if ((capabilities & flag) != 0)
                parts.Add(flag.ToString().ToUpperInvariant());
        }

        return string.Join("+", parts);
    }

    private static string FormatPlayers(TapePlayers players)
    {
        return players switch
        {
            TapePlayers.TwoNetwork => "2 PLAYER NETWORK",
            TapePlayers.TwoLocal => "2 PLAYER LOCAL",
            _ => "1 PLAYER"
        };
    }
}
