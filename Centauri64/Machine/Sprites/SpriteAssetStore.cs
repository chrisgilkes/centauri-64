using System;
using System.Linq;
using System.Collections.Generic;

namespace Centauri64.Machine.Sprites;

public sealed class SpriteAssetStore
{
    private readonly Dictionary<string, SpriteAsset>    _assets = new();

    public IReadOnlyList<SpriteAsset> Assets =>_assets.Values.OrderBy(asset => asset.Name).ToList();

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

    public bool Remove(string name)
    {
        return _assets.Remove(name);
    }

    public bool Rename(string oldName, string newName)
    {
        if (oldName == newName)
            return true;

        if (!_assets.TryGetValue(oldName, out var asset))
            return false;

        if (_assets.ContainsKey(newName))
            return false;

        _assets.Remove(oldName);
        asset.Rename(newName);
        _assets[newName] = asset;

        return true;
    }

    public void Clear()
    {
        _assets.Clear();
    }
}