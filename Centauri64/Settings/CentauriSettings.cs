namespace Centauri64.Settings;

public enum WindowModeSetting
{
    Windowed,
    Fullscreen
}

public enum ScalingModeSetting
{
    PixelPerfect,
    FitWindow
}

public enum CrtPreset
{
    Off,
    Subtle,
    NineteenEightySix
}

public enum EditorExperience
{
    Modern,
    Classic
}

/// <summary>
/// Persistent user preferences — separate from career progression.
/// </summary>
public sealed class CentauriSettings
{
    public WindowModeSetting WindowMode { get; set; } = WindowModeSetting.Windowed;

    /// <summary>Integer scale of the 640×480 development display (1–5).</summary>
    public int WindowScale { get; set; } = 2;

    public ScalingModeSetting ScalingMode { get; set; } = ScalingModeSetting.PixelPerfect;

    public bool VSync { get; set; } = true;

    public CrtPreset CrtFilter { get; set; } = CrtPreset.Subtle;

    public EditorExperience EditorExperience { get; set; } = EditorExperience.Modern;

    // Consumed fully by upcoming Creative Tools / editor work.
    public bool AutoIndent { get; set; } = true;

    public bool SyntaxColours { get; set; } = true;

    public bool LineHighlight { get; set; } = true;

    public bool Tooltips { get; set; } = true;

    public bool Grid { get; set; } = true;

    /// <summary>0–100</summary>
    public int MasterVolume { get; set; } = 100;

    /// <summary>0–100 — reserved for future music; stored now.</summary>
    public int MusicVolume { get; set; } = 70;

    /// <summary>0–100 — drives beep / SFX amplitude.</summary>
    public int SfxVolume { get; set; } = 100;

    /// <summary>
    /// Application onboarding — not career state. Reset Career does not clear this.
    /// </summary>
    public bool HasSeenIntroduction { get; set; }

    /// <summary>
    /// Mode-select remembered option: 0–2 = career slots, 3 = Hardcore.
    /// </summary>
    public int LastModeSelectOption { get; set; }

    public static CentauriSettings CreateDefaults() => new();

    public float EffectiveSfxGain()
    {
        var master = ClampPercent(MasterVolume) / 100f;
        var sfx = ClampPercent(SfxVolume) / 100f;
        return master * sfx;
    }

    public static int ClampPercent(int value) =>
        value < 0 ? 0 : value > 100 ? 100 : value;

    public static int ClampScale(int scale, int maxScale)
    {
        if (maxScale < 1)
            maxScale = 1;

        if (scale < 1)
            scale = 1;

        if (scale > maxScale)
            scale = maxScale;

        return scale;
    }
}
