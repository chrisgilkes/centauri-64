using System;
using System.Linq;
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

    public bool ContainsAnimation(string name)
    {
        return _animations.ContainsKey(name);
    }

    public bool RemoveAnimation(string name)
    {
        // A sprite must always have at least one animation.
        if (_animations.Count <= 1)
            return false;

        return _animations.Remove(name);
    }

    public IReadOnlyList<SpriteAnimation> AnimationList =>
        _animations.Values
            .OrderBy(animation => animation.Name)
            .ToList();


    public SpriteAnimation? DuplicateAnimation(string sourceName,string newName)
    {
        if (!_animations.TryGetValue(
                sourceName,
                out var source))
        {
            return null;
        }

        if (_animations.ContainsKey(newName))
        {
            return null;
        }

        var animation =
            source.Clone(newName);

        _animations.Add(
            newName,
            animation);

        return animation;
    }
}