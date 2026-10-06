using Centauri64.Machine.Audio;

namespace Centauri64.Machine;

public sealed partial class CentauriMachine
{
    private readonly CentauriAudio _audio = new();

    public void Beep(int frequency, int durationMs)
    {
        _audio.Beep(frequency, durationMs);
    }

    public void Silence()
    {
        _audio.Silence();
    }

    /// <summary>
    /// Sets combined master × SFX gain for procedural beeps (0–1).
    /// </summary>
    public void SetAudioVolume(float volume)
    {
        if (volume < 0f)
            volume = 0f;

        if (volume > 1f)
            volume = 1f;

        _audio.Volume = volume;
    }
}