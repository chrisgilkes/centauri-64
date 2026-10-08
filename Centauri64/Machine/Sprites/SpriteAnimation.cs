using System.Collections.Generic;

using Centauri64.Machine.Images;

namespace Centauri64.Machine.Sprites;

public sealed class SpriteAnimation
{
    private readonly List<SpriteFrame> _frames = new();

    public string Name { get; }

    /// <summary>
    /// Optional authoring link to ImageAsset name. Playback does not require .images.
    /// </summary>
    public string? SourceImageName { get; set; }

    public IReadOnlyList<SpriteFrame> Frames =>
        _frames;

    public SpriteAnimation(string name)
    {
        Name = name;
    }

    public SpriteFrame AddFrame()
    {
        var frame = new SpriteFrame();

        _frames.Add(frame);

        return frame;
    }

    /// <summary>
    /// Replaces all frames with baked copies of every image frame (transparency preserved).
    /// </summary>
    public void ReplaceFramesFromImage(ImageAsset image)
    {
        _frames.Clear();
        SourceImageName = image.Name;

        for (var i = 0; i < image.FrameCount; i++)
        {
            var source = image.GetFrame(i);
            var frame = new SpriteFrame();

            for (var y = 0; y < CentauriSprite.HEIGHT; y++)
            {
                for (var x = 0; x < CentauriSprite.WIDTH; x++)
                    frame.Pixels[y, x] = source.Pixels[y, x];
            }

            _frames.Add(frame);
        }

        if (_frames.Count == 0)
            AddFrame();
    }

    public SpriteFrame InsertFrameAfter(int index)
    {
        var frame = new SpriteFrame();
        var insertAt = index + 1;

        if (insertAt < 0)
            insertAt = 0;

        if (insertAt > _frames.Count)
            insertAt = _frames.Count;

        _frames.Insert(insertAt, frame);

        return frame;
    }

    public bool RemoveFrame(int index)
    {
        // An animation must always have at least one frame.
        if (_frames.Count <= 1)
            return false;

        if (index < 0 || index >= _frames.Count)
            return false;

        _frames.RemoveAt(index);
        return true;
    }

    public SpriteAnimation Clone(string name)
    {
        var animation =
            new SpriteAnimation(name)
            {
                SourceImageName = SourceImageName
            };

        foreach (var frame in _frames)
        {
            animation._frames.Add(
                frame.Clone());
        }

        return animation;
    }
}