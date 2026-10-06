using System;
using System.Collections.Generic;
using System.Linq;

namespace Centauri64.Machine.Images;

public sealed class ImageAssetStore
{
    private readonly List<ImageAsset> _images = new();

    public IReadOnlyList<ImageAsset> Images => _images;

    public int Count => _images.Count;

    public void Clear() => _images.Clear();

    public ImageAsset? Find(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return null;

        var key = name.Trim().ToUpperInvariant();
        return _images.FirstOrDefault(image => image.Name == key);
    }

    public bool Contains(string name) => Find(name) != null;

    public void Add(ImageAsset image)
    {
        if (Find(image.Name) != null)
            throw new InvalidOperationException("IMAGE NAME ALREADY EXISTS");

        _images.Add(image);
    }

    public bool Remove(string name)
    {
        var image = Find(name);
        if (image == null)
            return false;

        _images.Remove(image);
        return true;
    }

    public string UniqueName(string baseName)
    {
        var root = string.IsNullOrWhiteSpace(baseName)
            ? "IMAGE"
            : baseName.Trim().ToUpperInvariant();

        if (Find(root) == null)
            return root;

        for (var i = 2; i < 1000; i++)
        {
            var candidate = root + i;
            if (Find(candidate) == null)
                return candidate;
        }

        return root + Guid.NewGuid().ToString("N")[..4].ToUpperInvariant();
    }
}
