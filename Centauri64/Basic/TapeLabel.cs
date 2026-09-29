namespace Centauri64.Basic;

public sealed class TapeLabel
{
    public const int CurrentMachineVersion = 1;

    public const int MaxDescriptionLength = 40;

    public const int MaxAuthorLength = 16;

    public string Description { get; set; } = string.Empty;

    public string Author { get; set; } = string.Empty;

    public TapeKind Kind { get; set; } = TapeKind.Game;

    public int MachineVersion { get; set; }
}
