using System;
using System.Collections.Generic;

namespace Centauri64.Analysis;

/// <summary>
/// Factual report about a tape/program.
/// Measures usage and content — never quality or taste.
/// </summary>
public sealed class SoftwareAnalysis
{
    public string TapeName { get; init; } = string.Empty;

    public bool ProgramValid { get; init; }

    public string? ValidationError { get; init; }

    public int LineCount { get; init; }

    public int StatementCount { get; init; }

    public int UniqueCommandCount { get; init; }

    public IReadOnlyList<string> CommandsUsed { get; init; } =
        Array.Empty<string>();

    public SoftwareCapability Capabilities { get; init; }

    public bool UsesText => Has(SoftwareCapability.Text);
    public bool UsesInput => Has(SoftwareCapability.Input);
    public bool UsesGraphics => Has(SoftwareCapability.Graphics);
    public bool UsesSprites => Has(SoftwareCapability.Sprites);
    public bool UsesAnimation => Has(SoftwareCapability.Animation);
    public bool UsesSound => Has(SoftwareCapability.Sound);
    public bool UsesMaps => Has(SoftwareCapability.Maps);
    public bool UsesStrings => Has(SoftwareCapability.Strings);
    public bool UsesRandom => Has(SoftwareCapability.Random);
    public bool UsesNetworking => Has(SoftwareCapability.Networking);
    public bool UsesCamera => Has(SoftwareCapability.Camera);
    public bool UsesImages => Has(SoftwareCapability.Images) || ImageCount > 0;
    public bool UsesBackgrounds => Has(SoftwareCapability.Backgrounds);
    public bool UsesForeground => Has(SoftwareCapability.Foreground);

    public int SpriteCount { get; init; }
    public int AnimationCount { get; init; }
    public int MapCount { get; init; }
    public int ImageCount { get; init; }
    public int ImageFrameCount { get; init; }
    public int GeneralImageCount { get; init; }
    public int SpriteImageCount { get; init; }
    public int TilesetImageCount { get; init; }
    public int BackgroundImageCount { get; init; }

    /// <summary>
    /// True when a cover asset file exists for the tape.
    /// </summary>
    public bool HasCover { get; init; }

    /// <summary>
    /// True when a saved cover has enough non-blank pixels to count as
    /// player-created artwork. Never an art-quality judgement.
    /// </summary>
    public bool HasCustomCover { get; init; }

    public int CoverChangedPixelCount { get; init; }

    /// <summary>
    /// Percentage of cover pixels that are non-blank (0–100).
    /// Fractional so small covers are not reported as 0%.
    /// </summary>
    public double CoverCoveragePercent { get; init; }

    public int CoverColourCount { get; init; }

    public bool Has(SoftwareCapability capability) =>
        (Capabilities & capability) != 0;

    public static SoftwareAnalysis Invalid(string tapeName, string error)
    {
        return new SoftwareAnalysis
        {
            TapeName = tapeName,
            ProgramValid = false,
            ValidationError = error,
            CommandsUsed = Array.Empty<string>()
        };
    }
}
