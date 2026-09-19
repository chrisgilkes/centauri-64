using Centauri64.Machine.Audio;

namespace Centauri64.Machine;

public sealed partial class CentauriMachine
{
    private readonly CentauriAudio _audio = new();

    public void Beep(int frequency, int durationMs)
    {
        _audio.Beep(frequency, durationMs);
    }
}