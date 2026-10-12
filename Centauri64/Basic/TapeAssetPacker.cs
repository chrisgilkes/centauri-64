using System;
using System.Collections.Generic;

using Centauri64.Machine;
using Centauri64.Machine.Images;
using Centauri64.Machine.Sprites;

namespace Centauri64.Basic;

/// <summary>
/// Builds the portable asset set for SAVE from listing scan ∪ include list.
/// Dynamic string names need explicit INCLUDE.
/// </summary>
public static class TapeAssetPacker
{
    public const string BuiltInSpriteName = "PLAYER";

    public sealed class PackResult
    {
        public ImageAssetStore Images { get; } = new();
        public SpriteAssetStore Sprites { get; } = new();
        public List<string> MissingImages { get; } = new();
        public List<string> MissingSprites { get; } = new();
        public List<string> PackedImageNames { get; } = new();
        public List<string> PackedSpriteNames { get; } = new();
    }

    public static PackResult Pack(
        string listing,
        ImageAssetStore library,
        SpriteAssetStore sessionSprites,
        TapeAssetIncludes includes)
    {
        var result = new PackResult();
        ScanListing(listing, out var listedImages, out var listedSprites);

        var imageNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var name in listedImages)
            imageNames.Add(name);
        foreach (var name in includes.ImageNames)
            imageNames.Add(name);

        foreach (var name in imageNames)
        {
            var image = library.Find(name);
            if (image == null)
            {
                result.MissingImages.Add(name);
                continue;
            }

            // Clone so tape copies stay independent of later library edits.
            result.Images.Add(image.Clone(image.Name));
            result.PackedImageNames.Add(image.Name);
        }

        // Sprite Binder defs are session/tape-scoped baked runtime data.
        // Pack every non-built-in sprite, plus PLAYER when the listing references it.
        var packedSpriteKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var asset in sessionSprites.Assets)
        {
            var isBuiltIn = asset.Name.Equals(
                BuiltInSpriteName,
                StringComparison.OrdinalIgnoreCase);
            if (isBuiltIn && !listedSprites.Contains(asset.Name))
                continue;

            result.Sprites.Add(asset.Clone(asset.Name));
            result.PackedSpriteNames.Add(asset.Name);
            packedSpriteKeys.Add(asset.Name);
        }

        foreach (var name in listedSprites)
        {
            if (!packedSpriteKeys.Contains(name) &&
                !sessionSprites.Contains(name))
            {
                result.MissingSprites.Add(name);
            }
        }

        return result;
    }

    /// <summary>
    /// Collects literal string names after IMAGE / BG / FG / SPRITE keywords.
    /// SPRITEANIM names animations on an already-selected sprite — ignored.
    /// </summary>
    public static void ScanListing(
        string listing,
        out HashSet<string> images,
        out HashSet<string> sprites)
    {
        images = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        sprites = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        if (string.IsNullOrWhiteSpace(listing))
            return;

        var tokenizer = new Tokenizer();
        foreach (var rawLine in listing.Split('\n'))
        {
            var line = rawLine.Trim();
            if (line.Length == 0)
                continue;

            List<Token> tokens;
            try
            {
                tokens = tokenizer.Tokenize(line);
            }
            catch
            {
                continue;
            }

            for (var i = 0; i < tokens.Count; i++)
            {
                var token = tokens[i];
                if (token.Type is TokenType.Image or TokenType.Bg or TokenType.Fg)
                {
                    if (TryFindNextString(tokens, i + 1, out var name))
                        images.Add(name);
                }
                else if (token.Type == TokenType.Sprite)
                {
                    if (TryFindNextString(tokens, i + 1, out var name))
                        sprites.Add(name);
                }
            }
        }
    }

    private static bool TryFindNextString(
        IReadOnlyList<Token> tokens,
        int start,
        out string name)
    {
        name = "";
        for (var i = start; i < tokens.Count; i++)
        {
            var token = tokens[i];
            if (token.Type == TokenType.String)
            {
                name = (token.Text ?? "").Trim().ToUpperInvariant();
                return name.Length > 0;
            }

            if (token.Type is TokenType.Rem or TokenType.EndOfLine)
                return false;
        }

        return false;
    }
}
