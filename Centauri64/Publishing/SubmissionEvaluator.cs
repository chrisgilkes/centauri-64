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

        if (requirements.AllowedGenres is { Length: > 0 })
        {
            var names = new List<string>();
            var matched = false;

            foreach (var genre in requirements.AllowedGenres)
            {
                names.Add(genre.ToString().ToUpperInvariant());
                if (label.Genre == genre)
                    matched = true;
            }

            checks.Add(new SubmissionCheck(
                "GENRE " + string.Join("/", names),
                matched,
                matched
                    ? null
                    : "YOUR GENRE: " + label.Genre.ToString().ToUpperInvariant()));
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
                string.Join(" OR ", labels),
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
            var passed = analysis.LineCount >= requirements.MinimumLineCount;
            checks.Add(new SubmissionCheck(
                $"AT LEAST {requirements.MinimumLineCount} LINES",
                passed,
                "YOUR PROGRAM: " + analysis.LineCount));
        }

        if (requirements.MaximumLineCount > 0)
        {
            var passed = analysis.LineCount <= requirements.MaximumLineCount;
            var over = analysis.LineCount - requirements.MaximumLineCount;
            checks.Add(new SubmissionCheck(
                $"MAXIMUM {requirements.MaximumLineCount} LINES",
                passed,
                passed
                    ? "YOUR PROGRAM: " + analysis.LineCount
                    : "YOUR PROGRAM: " + analysis.LineCount +
                      "\n" + over + " LINE" + (over == 1 ? "" : "S") + " OVER LIMIT"));
        }

        if (requirements.MinimumStatementCount > 0)
        {
            var passed = analysis.StatementCount >= requirements.MinimumStatementCount;
            checks.Add(new SubmissionCheck(
                $"AT LEAST {requirements.MinimumStatementCount} STATEMENTS",
                passed,
                "YOUR PROGRAM: " + analysis.StatementCount));
        }

        if (requirements.MaximumStatementCount > 0)
        {
            var passed = analysis.StatementCount <= requirements.MaximumStatementCount;
            var over = analysis.StatementCount - requirements.MaximumStatementCount;
            checks.Add(new SubmissionCheck(
                $"MAXIMUM {requirements.MaximumStatementCount} STATEMENTS",
                passed,
                passed
                    ? "YOUR PROGRAM: " + analysis.StatementCount
                    : "YOUR PROGRAM: " + analysis.StatementCount +
                      "\n" + over + " STATEMENT" + (over == 1 ? "" : "S") + " OVER LIMIT"));
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
                DescribeCapability(flag),
                analysis.Has(flag)));
        }
    }

    private static string DescribeCapability(SoftwareCapability flag)
    {
        return flag switch
        {
            SoftwareCapability.Input => "PLAYER INPUT",
            SoftwareCapability.Networking => "NETWORKING",
            SoftwareCapability.Graphics => "GRAPHICS",
            SoftwareCapability.Sprites => "SPRITES",
            SoftwareCapability.Animation => "ANIMATION",
            SoftwareCapability.Sound => "SOUND",
            SoftwareCapability.Maps => "MAPS",
            SoftwareCapability.Strings => "STRINGS",
            SoftwareCapability.Text => "TEXT",
            SoftwareCapability.Random => "RANDOM",
            SoftwareCapability.Camera => "CAMERA",
            _ => flag.ToString().ToUpperInvariant()
        };
    }

    private static string DescribeCapabilities(SoftwareCapability capabilities)
    {
        var parts = new List<string>();

        foreach (SoftwareCapability flag in System.Enum.GetValues<SoftwareCapability>())
        {
            if (flag == SoftwareCapability.None)
                continue;

            if ((capabilities & flag) != 0)
                parts.Add(DescribeCapability(flag));
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
