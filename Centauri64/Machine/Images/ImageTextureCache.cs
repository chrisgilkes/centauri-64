using System;
using System.Collections.Generic;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Centauri64.Machine.Images;

/// <summary>
/// Uploads frame pixel data to a Texture2D only when that frame's Revision changes.
/// </summary>
public sealed class ImageTextureCache : IDisposable
{
    private readonly Dictionary<(ImageAsset Image, int Frame), Entry> _entries = new();

    private sealed class Entry
    {
        public Texture2D? Texture;
        public int Revision = int.MinValue;
        public Color[]? Buffer;
    }

    public Texture2D GetTexture(GraphicsDevice device, ImageAsset image, int frameIndex = -1)
    {
        if (frameIndex < 0)
            frameIndex = image.CurrentFrameIndex;

        var frame = image.GetFrame(frameIndex);
        var key = (image, frameIndex);

        if (!_entries.TryGetValue(key, out var entry))
        {
            entry = new Entry();
            _entries[key] = entry;
        }

        if (entry.Texture == null ||
            entry.Texture.Width != image.Width ||
            entry.Texture.Height != image.Height)
        {
            entry.Texture?.Dispose();
            entry.Texture = new Texture2D(device, image.Width, image.Height);
            entry.Buffer = new Color[image.Width * image.Height];
            entry.Revision = int.MinValue;
        }

        if (entry.Revision != frame.Revision)
        {
            FillBuffer(frame.Pixels, image.Width, image.Height, entry.Buffer!);
            entry.Texture.SetData(entry.Buffer);
            entry.Revision = frame.Revision;
        }

        return entry.Texture;
    }

    public void Clear()
    {
        foreach (var entry in _entries.Values)
            entry.Texture?.Dispose();

        _entries.Clear();
    }

    public void Dispose() => Clear();

    private static void FillBuffer(int[,] pixels, int width, int height, Color[] buffer)
    {
        var i = 0;
        for (var y = 0; y < height; y++)
        {
            for (var x = 0; x < width; x++)
            {
                var colour = pixels[y, x];
                buffer[i++] = colour < 0
                    ? Color.Transparent
                    : CentauriPalette.Get(colour);
            }
        }
    }
}
