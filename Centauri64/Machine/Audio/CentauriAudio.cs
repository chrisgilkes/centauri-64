using System;
using Microsoft.Xna.Framework.Audio;

namespace Centauri64.Machine.Audio;

public sealed class CentauriAudio
{
    private const int SampleRate = 44100;

    public void Beep(int frequency, int durationMs)
    {
        if (frequency <= 0 || durationMs <= 0)
            return;

        var sampleCount =
            SampleRate * durationMs / 1000;

        var samples =
            new short[sampleCount];

        for (var i = 0; i < sampleCount; i++)
        {
            var time =
                i / (double)SampleRate;

            var wave =
                Math.Sin(
                    2.0 *
                    Math.PI *
                    frequency *
                    time);

            samples[i] =
                (short)(wave * short.MaxValue * 0.20);
        }

        var data =
            new byte[samples.Length * sizeof(short)];

        Buffer.BlockCopy(
            samples,
            0,
            data,
            0,
            data.Length);

        var sound =
            new SoundEffect(
                data,
                SampleRate,
                AudioChannels.Mono);

        sound.Play();
    }
}