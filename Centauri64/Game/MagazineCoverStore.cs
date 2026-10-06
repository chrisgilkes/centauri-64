using System;
using System.Collections.Generic;
using System.IO;

using Centauri64.Session;
using Microsoft.Xna.Framework.Graphics;

namespace Centauri64.Game;

/// <summary>
/// Loads optional magazine cover textures.
/// Drop PNG or JPEG at Content/MagazineCovers/issue01.png … issue10.png
/// (copied beside the executable). Missing files use the print fallback.
/// </summary>
public sealed class MagazineCoverStore
{
    public const string FolderName = "MagazineCovers";

    private static readonly string[] Extensions = { ".png", ".jpg", ".jpeg" };

    private readonly GraphicsDevice _device;
    private readonly Dictionary<string, Texture2D?> _cache = new(StringComparer.OrdinalIgnoreCase);

    public MagazineCoverStore(GraphicsDevice device)
    {
        _device = device;
    }

    public static string AssetIdFor(MagazineIssue issue)
    {
        if (!string.IsNullOrWhiteSpace(issue.CoverAssetId))
            return issue.CoverAssetId;

        return $"issue{issue.IssueNumber:00}";
    }

    public Texture2D? Get(MagazineIssue issue)
    {
        var id = AssetIdFor(issue);
        if (_cache.TryGetValue(id, out var cached))
            return cached;

        Texture2D? texture = null;
        var folder = Path.Combine(AppContext.BaseDirectory, "Content", FolderName);
        foreach (var extension in Extensions)
        {
            var path = Path.Combine(folder, id + extension);
            if (!File.Exists(path))
                continue;

            try
            {
                using var stream = File.OpenRead(path);
                texture = Texture2D.FromStream(_device, stream);
                break;
            }
            catch
            {
                texture = null;
            }
        }

        _cache[id] = texture;
        return texture;
    }
}
