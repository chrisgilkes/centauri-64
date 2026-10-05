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

    /// <summary>
    /// Declared player count for software metadata (1 or 2 for V1).
    /// </summary>
    public int Players { get; set; } = 1;
}
