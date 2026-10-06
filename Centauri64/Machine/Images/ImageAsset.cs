using System;
using System.Collections.Generic;

namespace Centauri64.Machine.Images;

/// <summary>
/// General-purpose raster/pixel-art asset for a tape.
/// Used for illustrations, sprite artwork, tilesets, and backgrounds.
/// The Image Editor draws frames; Sprite Builder / Map Editor consume them later.
/// </summary>
public sealed class ImageAsset
{
    public const int Transparent = -1;
    public const int MaxFrames = 64;

    private readonly List<ImageFrame> _frames = new();

    public string Name { get; set; }

    public CentauriDisplayMode Mode { get; }

    public int Width { get; }

    public int Height { get; }

    public ImageCategory Category { get; set; }

    public IReadOnlyList<ImageFrame> Frames => _frames;

    public int FrameCount => _frames.Count;

    public int CurrentFrameIndex { get; private set; }

    public ImageFrame CurrentFrame => _frames[CurrentFrameIndex];

    /// <summary>Pixels of the currently selected frame (editor convenience).</summary>
    public int[,] Pixels => CurrentFrame.Pixels;

    /// <summary>Revision of the current frame (texture/thumbnail dirty flag).</summary>
    public int Revision => CurrentFrame.Revision;

    public ImageAsset(
        string name,
        CentauriDisplayMode mode,
        int width,
        int height,
        ImageCategory category = ImageCategory.General,
        int fillColour = Transparent,
        int frameCount = 1)
    {
        Name = name.Trim().ToUpperInvariant();
        Mode = mode;
        Width = width;
        Height = height;
        Category = category;

        ValidateSize(mode, width, height);

        var frames = Math.Clamp(frameCount, 0, MaxFrames);
        if (frames == 0)
            frames = 1;

        for (var i = 0; i < frames; i++)
            _frames.Add(new ImageFrame(width, height, fillColour));
    }

    /// <summary>Internal constructor used when frames will be supplied immediately.</summary>
    private ImageAsset(
        string name,
        CentauriDisplayMode mode,
        int width,
        int height,
        ImageCategory category,
        bool emptyFrames)
    {
        Name = name.Trim().ToUpperInvariant();
        Mode = mode;
        Width = width;
        Height = height;
        Category = category;
        ValidateSize(mode, width, height);
        if (!emptyFrames)
            _frames.Add(new ImageFrame(width, height));
    }

    public static void ValidateSize(CentauriDisplayMode mode, int width, int height)
    {
        if (width < 1 || height < 1)
            throw new InvalidOperationException("IMAGE SIZE MUST BE AT LEAST 1X1");

        var maxW = ModeWidth(mode);
        var maxH = ModeHeight(mode);
        if (width > maxW || height > maxH)
            throw new InvalidOperationException($"IMAGE SIZE MUST FIT {maxW}X{maxH}");
    }

    public static int ModeWidth(CentauriDisplayMode mode) =>
        mode == CentauriDisplayMode.Arcade
            ? CentauriMachine.ARCADE_WIDTH
            : CentauriMachine.SCREEN_WIDTH;

    public static int ModeHeight(CentauriDisplayMode mode) =>
        mode == CentauriDisplayMode.Arcade
            ? CentauriMachine.ARCADE_HEIGHT
            : CentauriMachine.SCREEN_HEIGHT;

    public bool IsFullScreen =>
        Width == ModeWidth(Mode) && Height == ModeHeight(Mode);

    public void SelectFrame(int index)
    {
        if (index < 0 || index >= _frames.Count)
            throw new InvalidOperationException("FRAME NOT FOUND");

        CurrentFrameIndex = index;
    }

    public ImageFrame GetFrame(int index)
    {
        if (index < 0 || index >= _frames.Count)
            throw new InvalidOperationException("FRAME NOT FOUND");

        return _frames[index];
    }

    public ImageFrame AddFrame(int fillColour = Transparent)
    {
        if (_frames.Count >= MaxFrames)
            throw new InvalidOperationException("TOO MANY FRAMES");

        var frame = new ImageFrame(Width, Height, fillColour);
        _frames.Add(frame);
        CurrentFrameIndex = _frames.Count - 1;
        return frame;
    }

    public ImageFrame DuplicateFrame(int index)
    {
        if (index < 0 || index >= _frames.Count)
            throw new InvalidOperationException("FRAME NOT FOUND");

        if (_frames.Count >= MaxFrames)
            throw new InvalidOperationException("TOO MANY FRAMES");

        var copy = _frames[index].Clone();
        _frames.Insert(index + 1, copy);
        CurrentFrameIndex = index + 1;
        return copy;
    }

    public void DeleteFrame(int index)
    {
        if (_frames.Count <= 1)
            throw new InvalidOperationException("CANNOT DELETE LAST FRAME");

        if (index < 0 || index >= _frames.Count)
            throw new InvalidOperationException("FRAME NOT FOUND");

        _frames.RemoveAt(index);
        if (CurrentFrameIndex >= _frames.Count)
            CurrentFrameIndex = _frames.Count - 1;
        else if (CurrentFrameIndex > index)
            CurrentFrameIndex--;
    }

    public ImageAsset Clone(string newName)
    {
        var copy = new ImageAsset(newName, Mode, Width, Height, Category, emptyFrames: true);
        foreach (var frame in _frames)
            copy._frames.Add(frame.Clone());

        copy.CurrentFrameIndex = 0;
        return copy;
    }

    public void Clear()
    {
        CurrentFrame.Clear(Transparent);
    }

    public void MarkChanged() => CurrentFrame.MarkChanged();

    public int GetPixel(int x, int y)
    {
        if (x < 0 || y < 0 || x >= Width || y >= Height)
            return Transparent;

        return Pixels[y, x];
    }

    public void SetPixel(int x, int y, int colour)
    {
        if (x < 0 || y < 0 || x >= Width || y >= Height)
            return;

        Pixels[y, x] = colour;
    }
}
