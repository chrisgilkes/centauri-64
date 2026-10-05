using Centauri64.Analysis;
using Centauri64.Basic;

namespace Centauri64.Publishing;

public sealed class SubmissionRequirements
{
    public bool ProgramValid { get; init; } = true;

    public SoftwareCapability RequiredCapabilities { get; init; }

    /// <summary>
    /// At least one of these capability groups must be present.
    /// Empty means no OR-group requirement.
    /// </summary>
    public SoftwareCapability[]? AnyOfCapabilities { get; init; }

    public TapeKind? RequiredKind { get; init; }

    public GameGenre? RequiredGenre { get; init; }

    /// <summary>
    /// If set, the tape genre must be one of these values.
    /// </summary>
    public GameGenre[]? AllowedGenres { get; init; }

    public TapePlayers? RequiredPlayers { get; init; }

    public bool RequiresCustomCover { get; init; }

    public int MinimumLineCount { get; init; }

    public int MaximumLineCount { get; init; }

    public int MinimumStatementCount { get; init; }

    public int MaximumStatementCount { get; init; }

    public int MinimumUniqueCommands { get; init; }

    public int MinimumSpriteCount { get; init; }

    public int MinimumAnimationCount { get; init; }

    public int MinimumMapCount { get; init; }

    public int MinimumCoverColours { get; init; }

    public int MaximumCoverColours { get; init; }
}
