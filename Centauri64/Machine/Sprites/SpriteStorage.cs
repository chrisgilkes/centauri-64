using System;
using System.IO;

namespace Centauri64.Machine.Sprites;

public sealed class SpriteStorage
{
    private readonly string _programDirectory;

    public SpriteStorage()
    {
        _programDirectory = Path.Combine(
            AppContext.BaseDirectory,
            "Programs");

        Directory.CreateDirectory(
            _programDirectory);
    }

    public void Save(
        string name,
        SpriteAssetStore assets)
    {
        var path =
            GetSpritePath(name);

        using var writer =
            new StreamWriter(path);

        foreach (var asset in assets.Assets)
        {
            writer.WriteLine(
                $"SPRITE {asset.Name}");

            foreach (var animationPair in
                     asset.Animations)
            {
                var animation =
                    animationPair.Value;

                writer.WriteLine(
                    $"ANIMATION {animation.Name}");

                foreach (var frame in animation.Frames)
                {
                    writer.WriteLine("FRAME");

                    for (var y = 0;
                         y < CentauriSprite.HEIGHT;
                         y++)
                    {
                        for (var x = 0;
                             x < CentauriSprite.WIDTH;
                             x++)
                        {
                            if (x > 0)
                                writer.Write(",");

                            writer.Write(
                                frame.Pixels[y, x]);
                        }

                        writer.WriteLine();
                    }
                }
            }
        }
    }

    private string GetSpritePath(string name)
    {
        var validName =
            ValidateName(name);

        return Path.Combine(
            _programDirectory,
            validName + ".sprites");
    }

    private static string ValidateName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new InvalidOperationException(
                "Sprite save name cannot be empty.");
        }

        if (name.Contains(".."))
        {
            throw new InvalidOperationException(
                "Invalid sprite save name.");
        }

        foreach (var character in
                 Path.GetInvalidFileNameChars())
        {
            if (name.Contains(character))
            {
                throw new InvalidOperationException(
                    "Invalid sprite save name.");
            }
        }

        return name.ToUpperInvariant();
    }

    public void Delete(string name)
    {
        var path = GetSpritePath(name);

        if (File.Exists(path))
        {
            File.Delete(path);
        }
    }

    public void Load(string name,SpriteAssetStore assets)
    {
        var path =
            GetSpritePath(name);

        if (!File.Exists(path))
            return;

        var lines =
            File.ReadAllLines(path);

        assets.Clear();

        SpriteAsset? currentAsset = null;
        SpriteAnimation? currentAnimation = null;

        var lineIndex = 0;

        while (lineIndex < lines.Length)
        {
            var line =
                lines[lineIndex].Trim();

            if (line.StartsWith("SPRITE "))
            {
                var assetName =
                    line["SPRITE ".Length..];

                currentAsset =
                    new SpriteAsset(assetName);

                assets.Add(currentAsset);

                currentAnimation = null;

                lineIndex++;
                continue;
            }

            if (line.StartsWith("ANIMATION "))
            {
                if (currentAsset == null)
                {
                    throw new InvalidOperationException(
                        "Animation without sprite.");
                }

                var animationName =
                    line["ANIMATION ".Length..];

                currentAnimation =
                    currentAsset.AddAnimation(
                        animationName);

                lineIndex++;
                continue;
            }

            if (line == "FRAME")
            {
                if (currentAnimation == null)
                {
                    throw new InvalidOperationException(
                        "Frame without animation.");
                }

                var frame =
                    currentAnimation.AddFrame();

                lineIndex++;

                for (var y = 0;
                    y < CentauriSprite.HEIGHT;
                    y++)
                {
                    if (lineIndex >= lines.Length)
                    {
                        throw new InvalidOperationException(
                            "Incomplete sprite frame.");
                    }

                    var values =
                        lines[lineIndex]
                            .Split(',');

                    if (values.Length !=
                        CentauriSprite.WIDTH)
                    {
                        throw new InvalidOperationException(
                            "Invalid sprite row.");
                    }

                    for (var x = 0;
                        x < CentauriSprite.WIDTH;
                        x++)
                    {
                        frame.Pixels[y, x] =
                            int.Parse(values[x]);
                    }

                    lineIndex++;
                }

                continue;
            }

            lineIndex++;
        }
    }
}