using System;
using Microsoft.Xna.Framework.Audio;

namespace Centauri64.Machine.Audio;

public sealed class CentauriAudio
{
    private const int SampleRate = 44100;

    private SoundEffect? _effect;
    private SoundEffectInstance? _voice;

    public void Beep(int frequency, int durationMs)
    {
        Silence();

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

        _effect =
            new SoundEffect(
                data,
                SampleRate,
                AudioChannels.Mono);

        _voice = _effect.CreateInstance();
        _voice.Play();
    }

    public void Silence()
    {
        if (_voice != null)
        {
            _voice.Stop();
            _voice.Dispose();
            _voice = null;
        }

        if (_effect != null)
        {
            _effect.Dispose();
            _effect = null;
        }
    }
}