namespace Centauri64.Machine;

/// <summary>
/// Active presentation for program output.
/// <see cref="Console"/> is the default BASIC environment (not a MODE number).
/// BASIC <c>MODE 1</c> selects <see cref="HighResolution"/>;
/// BASIC <c>MODE 2</c> selects <see cref="Arcade"/>.
/// </summary>
public enum CentauriDisplayMode
{
    /// <summary>Default BASIC editor/console environment. Not selected by MODE.</summary>
    Console = 0,

    /// <summary>640×480 high-resolution graphics. BASIC MODE 1.</summary>
    HighResolution = 1,

    /// <summary>320×240 arcade graphics. BASIC MODE 2.</summary>
    Arcade = 2
}
