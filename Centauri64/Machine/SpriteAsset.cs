using System;
using System.Collections.Generic;
using Centauri64.Machine.Sprites;

namespace Centauri64.Machine;

public sealed class SpriteAsset
{
    private readonly Dictionary<string, SpriteAnimation>
        _animations = new();

    public string Name { get; }

    public IReadOnlyDictionary<string, SpriteAnimation>
        Animations => _animations;

    public SpriteAsset(string name)
    {
        Name = name;
    }

    public SpriteAnimation AddAnimation(string name)
    {
        var animation =
            new SpriteAnimation(name);

        _animations[name] = animation;

        return animation;
    }

    public SpriteAnimation GetAnimation(string name)
    {
        if (!_animations.TryGetValue(
                name,
                out var animation))
        {
            throw new InvalidOperationException(
                $"Unknown animation {name}.");
        }

        return animation;
    }
}