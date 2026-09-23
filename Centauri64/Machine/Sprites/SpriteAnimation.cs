using System.Collections.Generic;

namespace Centauri64.Machine.Sprites;

public sealed class SpriteAnimation
{
    private readonly List<SpriteFrame> _frames = new();

    public string Name { get; }

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
}