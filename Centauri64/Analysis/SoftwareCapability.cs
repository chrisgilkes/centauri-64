using System;

namespace Centauri64.Analysis;

/// <summary>
/// Factual techniques detected in a tape.
/// Add new values when BASIC gains major feature areas
/// (e.g. Backgrounds, Parallax, Music).
/// When adding a BASIC command, register it in
/// <see cref="CapabilityCatalog"/>.
/// </summary>
[Flags]
public enum SoftwareCapability
{
    None = 0,
    Text = 1 << 0,
    Input = 1 << 1,
    Graphics = 1 << 2,
    Sprites = 1 << 3,
    Animation = 1 << 4,
    Sound = 1 << 5,
    Maps = 1 << 6,
    Strings = 1 << 7,
    Random = 1 << 8,
    Networking = 1 << 9,
    Camera = 1 << 10
}
