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
}