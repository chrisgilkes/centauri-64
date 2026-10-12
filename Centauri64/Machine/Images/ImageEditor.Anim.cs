using System;

using Centauri64.Machine;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

namespace Centauri64.Machine.Images;

public sealed partial class ImageEditor
{
    private bool _animPlaying;
    private int _previewFrameIndex;
    private float _previewTimer;

    /// <summary>Animation Mode starts automatically when an image has 2+ frames.</summary>
    private bool IsAnimMode =>
        _image != null && _image.FrameCount >= 2;

    private void UpdateAnimPreview(GameTime gameTime)
    {
        // Corner preview always loops in Anim Mode; PLAY only drives the main canvas.
        if (!IsAnimMode || _image == null)
            return;

        var frame = _image.GetFrame(
            Math.Clamp(_previewFrameIndex, 0, _image.FrameCount - 1));
        var frameTime = frame.DurationSeconds > 0f
            ? frame.DurationSeconds
            : ImageFrame.DefaultDurationSeconds;

        _previewTimer += (float)gameTime.ElapsedGameTime.TotalSeconds;
        while (_previewTimer >= frameTime)
        {
            _previewTimer -= frameTime;
            _previewFrameIndex++;
            if (_previewFrameIndex >= _image.FrameCount)
                _previewFrameIndex = 0;

            frame = _image.GetFrame(_previewFrameIndex);
            frameTime = frame.DurationSeconds > 0f
                ? frame.DurationSeconds
                : ImageFrame.DefaultDurationSeconds;
        }
    }

    private void ToggleAnimPlay()
    {
        if (!IsAnimMode || _image == null)
        {
            _statusTip = "ADD A SECOND FRAME FOR ANIM MODE";
            return;
        }

        _animPlaying = !_animPlaying;
        if (_animPlaying)
        {
            _previewFrameIndex = _image.CurrentFrameIndex;
            _previewTimer = 0f;
            _statusTip = "ANIM PLAY";
        }
        else
        {
            _statusTip = "ANIM STOP";
        }
    }

    private void StopAnimPlay()
    {
        _animPlaying = false;
        _previewTimer = 0f;
    }

    private int DisplayFrameIndex =>
        _animPlaying && IsAnimMode
            ? _previewFrameIndex
            : (_image?.CurrentFrameIndex ?? 0);

    private void AdjustFrameDuration(float delta)
    {
        if (_image == null || !IsAnimMode)
            return;

        var frame = _image.CurrentFrame;
        var next = Math.Clamp(frame.DurationSeconds + delta, 0.025f, 2f);
        // Snap to hundredths for readable UI.
        next = MathF.Round(next * 100f) / 100f;
        if (next < 0.025f)
            next = 0.025f;

        frame.DurationSeconds = next;
        _dirty = true;
        _statusTip = $"FRAME {_image.CurrentFrameIndex + 1} DUR {FormatDuration(next)}S";
    }

    private static string FormatDuration(float seconds) =>
        seconds.ToString("0.##");

    private void FlipHorizontal()
    {
        if (_image == null)
            return;

        PushUndo();
        var pixels = _image.Pixels;
        var width = _image.Width;
        var height = _image.Height;

        for (var y = 0; y < height; y++)
        {
            for (var x = 0; x < width / 2; x++)
            {
                var oppositeX = width - 1 - x;
                var temp = pixels[y, x];
                pixels[y, x] = pixels[y, oppositeX];
                pixels[y, oppositeX] = temp;
            }
        }

        _image.MarkChanged();
        _dirty = true;
        _statusTip = "FLIP HORIZONTAL";
    }

    private void FlipVertical()
    {
        if (_image == null)
            return;

        PushUndo();
        var pixels = _image.Pixels;
        var width = _image.Width;
        var height = _image.Height;

        for (var y = 0; y < height / 2; y++)
        {
            var oppositeY = height - 1 - y;
            for (var x = 0; x < width; x++)
            {
                var temp = pixels[y, x];
                pixels[y, x] = pixels[oppositeY, x];
                pixels[oppositeY, x] = temp;
            }
        }

        _image.MarkChanged();
        _dirty = true;
        _statusTip = "FLIP VERTICAL";
    }

    private void NudgeFrame(int offsetX, int offsetY)
    {
        if (_image == null)
            return;

        PushUndo();
        var width = _image.Width;
        var height = _image.Height;
        var source = _image.Pixels;
        var shifted = new int[height, width];

        for (var y = 0; y < height; y++)
        {
            for (var x = 0; x < width; x++)
                shifted[y, x] = ImageAsset.Transparent;
        }

        for (var y = 0; y < height; y++)
        {
            for (var x = 0; x < width; x++)
            {
                var newX = x + offsetX;
                var newY = y + offsetY;
                if (newX < 0 || newX >= width || newY < 0 || newY >= height)
                    continue;

                shifted[newY, newX] = source[y, x];
            }
        }

        for (var y = 0; y < height; y++)
        {
            for (var x = 0; x < width; x++)
                source[y, x] = shifted[y, x];
        }

        _image.MarkChanged();
        _dirty = true;
        _statusTip = "NUDGED FRAME";
    }
}
