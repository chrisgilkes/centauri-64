using System;
using System.Collections.Generic;

namespace Centauri64.Machine.Sprites;

public sealed class SpriteAssetStore
{
    private readonly Dictionary<string, SpriteAsset>    _assets = new();

    public IEnumerable<SpriteAsset> Assets => _assets.Values;

    public void Add(SpriteAsset asset)
    {
        _assets[asset.Name] = asset;
    }

    public SpriteAsset Get(string name)
    {
        if (!_assets.TryGetValue(
                name,
                out var asset))
        {
            throw new InvalidOperationException($"Unknown sprite {name}.");
        }

        return asset;
    }

    public bool Contains(string name)
    {
        return _assets.ContainsKey(name);
    }

    public void Clear()
    {
        _assets.Clear();
    }
}