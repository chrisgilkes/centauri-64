namespace Centauri64.Basic;

public sealed class TapeLabel
{
    public const int CurrentMachineVersion = 1;

    public const int DefaultProgramVersion = 1;

    public const int MaxDescriptionLength = 40;

    public const int MaxAuthorLength = 16;

    public string Description { get; set; } = string.Empty;

    public string Author { get; set; } = string.Empty;

    public TapeKind Kind { get; set; } = TapeKind.Game;

    public GameGenre Genre { get; set; } = GameGenre.None;

    public TapePlayers Players { get; set; } = TapePlayers.One;

    public int MachineVersion { get; set; }

    /// <summary>
    /// Persistent local identity for this tape/program.
    /// Used for Network BASIC matchmaking.
    /// </summary>
    public string ProgramId { get; set; } = string.Empty;

    /// <summary>
    /// Program content version for network compatibility checks.
    /// </summary>
    public int ProgramVersion { get; set; } = DefaultProgramVersion;
}
