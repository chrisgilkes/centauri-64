using System;
using System.Collections.Generic;
using System.Linq;

namespace Centauri64.Machine.Maps;

public sealed class MapAssetStore
{
    private readonly Dictionary<string, MapAsset> _maps = new();

    public IReadOnlyList<MapAsset> Maps =>
        _maps.Values.OrderBy(map => map.Name).ToList();

    public void Add(MapAsset map)
    {
        _maps[map.Name] = map;
    }

    public MapAsset Get(string name)
    {
        if (!_maps.TryGetValue(name, out var map))
        {
            throw new InvalidOperationException(
                $"Unknown map {name}.");
        }

        return map;
    }

    public bool Contains(string name)
    {
        return _maps.ContainsKey(name);
    }

    public bool TryGet(string name, out MapAsset map)
    {
        return _maps.TryGetValue(name, out map!);
    }

    public bool Remove(string name)
    {
        return _maps.Remove(name);
    }

    public bool Rename(string oldName, string newName)
    {
        if (oldName == newName)
            return true;

        if (!_maps.TryGetValue(oldName, out var map))
            return false;

        if (_maps.ContainsKey(newName))
            return false;

        _maps.Remove(oldName);
        map.Rename(newName);
        _maps[newName] = map;

        return true;
    }

    public void Clear()
    {
        _maps.Clear();
    }
}
