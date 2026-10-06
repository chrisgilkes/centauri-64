using System;
using System.Globalization;
using System.IO;
using System.Collections.Generic;

using Centauri64.Basic;

namespace Centauri64.Machine.Images;

/// <summary>
/// Tape sidecar format (.images).
/// Legacy files (no CATEGORY/FRAME headers) load as one frame;
/// full-screen legacy images are categorised BACKGROUND.
/// </summary>
public sealed class ImageStorage
{
    private readonly string _programDirectory;

    public ImageStorage()
    {
        TapeFolder.EnsureExists();
        _programDirectory = TapeFolder.Location;
    }

    public void Save(string name, ImageAssetStore assets)
    {
        var path = GetPath(name);

        if (assets.Count == 0)
        {
            if (File.Exists(path))
                File.Delete(path);
            return;
        }

        using var writer = new StreamWriter(path);

        foreach (var image in assets.Images)
        {
            writer.WriteLine($"IMAGE {image.Name}");
            writer.WriteLine($"MODE {(int)image.Mode}");
            writer.WriteLine($"SIZE {image.Width} {image.Height}");
            writer.WriteLine($"CATEGORY {(int)image.Category}");
            writer.WriteLine($"FRAMES {image.FrameCount}");

            for (var f = 0; f < image.FrameCount; f++)
            {
                writer.WriteLine($"FRAME {f}");
                WriteFrame(writer, image.GetFrame(f).Pixels, image.Width, image.Height);
            }
        }
    }

    public void Load(string name, ImageAssetStore assets)
    {
        assets.Clear();
        var path = GetPath(name);

        if (!File.Exists(path))
            return;

        string? pendingName = null;
        var pendingMode = CentauriDisplayMode.HighResolution;
        var pendingCategory = ImageCategory.General;
        var pendingWidth = 0;
        var pendingHeight = 0;
        var hasCategory = false;
        var frameBuffers = new List<int[,]>();
        int[,]? building = null;
        var row = 0;

        void FlushPending()
        {
            if (pendingName == null)
                return;

            var width = pendingWidth > 0 ? pendingWidth : ImageAsset.ModeWidth(pendingMode);
            var height = pendingHeight > 0 ? pendingHeight : ImageAsset.ModeHeight(pendingMode);

            if (building != null)
            {
                frameBuffers.Add(building);
                building = null;
            }

            if (frameBuffers.Count == 0)
            {
                pendingName = null;
                return;
            }

            var category = pendingCategory;
            if (!hasCategory &&
                width == ImageAsset.ModeWidth(pendingMode) &&
                height == ImageAsset.ModeHeight(pendingMode))
            {
                category = ImageCategory.Background;
            }

            var image = new ImageAsset(
                pendingName,
                pendingMode,
                width,
                height,
                category,
                ImageAsset.Transparent,
                1);

            // Replace first frame, then add remaining.
            CopyPixels(frameBuffers[0], image.GetFrame(0).Pixels, width, height);
            image.GetFrame(0).MarkChanged();

            for (var i = 1; i < frameBuffers.Count; i++)
            {
                var frame = image.AddFrame();
                CopyPixels(frameBuffers[i], frame.Pixels, width, height);
                frame.MarkChanged();
            }

            image.SelectFrame(0);
            assets.Add(image);

            pendingName = null;
            pendingMode = CentauriDisplayMode.HighResolution;
            pendingCategory = ImageCategory.General;
            pendingWidth = 0;
            pendingHeight = 0;
            hasCategory = false;
            frameBuffers.Clear();
            building = null;
            row = 0;
        }

        foreach (var raw in File.ReadAllLines(path))
        {
            var line = raw.Trim();
            if (line.Length == 0)
                continue;

            if (line.StartsWith("IMAGE ", StringComparison.OrdinalIgnoreCase))
            {
                FlushPending();
                pendingName = line["IMAGE ".Length..].Trim().ToUpperInvariant();
                continue;
            }

            if (pendingName == null)
                continue;

            if (line.StartsWith("MODE ", StringComparison.OrdinalIgnoreCase) &&
                int.TryParse(line["MODE ".Length..].Trim(), out var modeValue) &&
                Enum.IsDefined(typeof(CentauriDisplayMode), modeValue))
            {
                pendingMode = (CentauriDisplayMode)modeValue;
                continue;
            }

            if (line.StartsWith("CATEGORY ", StringComparison.OrdinalIgnoreCase) &&
                int.TryParse(line["CATEGORY ".Length..].Trim(), out var catValue) &&
                Enum.IsDefined(typeof(ImageCategory), catValue))
            {
                pendingCategory = (ImageCategory)catValue;
                hasCategory = true;
                continue;
            }

            if (line.StartsWith("SIZE ", StringComparison.OrdinalIgnoreCase))
            {
                var parts = line["SIZE ".Length..].Trim().Split(
                    ' ',
                    StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length >= 2 &&
                    int.TryParse(parts[0], out var w) &&
                    int.TryParse(parts[1], out var h))
                {
                    pendingWidth = w;
                    pendingHeight = h;
                }

                continue;
            }

            if (line.StartsWith("FRAMES ", StringComparison.OrdinalIgnoreCase))
                continue;

            if (line.StartsWith("FRAME ", StringComparison.OrdinalIgnoreCase))
            {
                if (building != null)
                    frameBuffers.Add(building);

                var width = pendingWidth > 0 ? pendingWidth : ImageAsset.ModeWidth(pendingMode);
                var height = pendingHeight > 0 ? pendingHeight : ImageAsset.ModeHeight(pendingMode);
                building = new int[height, width];
                for (var y = 0; y < height; y++)
                {
                    for (var x = 0; x < width; x++)
                        building[y, x] = ImageAsset.Transparent;
                }

                row = 0;
                continue;
            }

            if (!LooksLikePixelRow(line))
                continue;

            var widthNow = pendingWidth > 0 ? pendingWidth : ImageAsset.ModeWidth(pendingMode);
            var heightNow = pendingHeight > 0 ? pendingHeight : ImageAsset.ModeHeight(pendingMode);

            // Legacy file: pixel rows without FRAME header.
            if (building == null)
            {
                building = new int[heightNow, widthNow];
                for (var y = 0; y < heightNow; y++)
                {
                    for (var x = 0; x < widthNow; x++)
                        building[y, x] = ImageAsset.Transparent;
                }

                row = 0;
            }

            var cells = line.Split(',');
            if (cells.Length != widthNow || row >= heightNow)
                continue;

            for (var x = 0; x < widthNow; x++)
            {
                if (int.TryParse(
                        cells[x],
                        NumberStyles.Integer,
                        CultureInfo.InvariantCulture,
                        out var colour))
                {
                    building[row, x] = colour;
                }
            }

            row++;
        }

        FlushPending();
    }

    private static void WriteFrame(StreamWriter writer, int[,] pixels, int width, int height)
    {
        for (var y = 0; y < height; y++)
        {
            for (var x = 0; x < width; x++)
            {
                if (x > 0)
                    writer.Write(",");

                writer.Write(pixels[y, x]);
            }

            writer.WriteLine();
        }
    }

    private static void CopyPixels(int[,] source, int[,] destination, int width, int height)
    {
        for (var y = 0; y < height; y++)
        {
            for (var x = 0; x < width; x++)
                destination[y, x] = source[y, x];
        }
    }

    private static bool LooksLikePixelRow(string line)
    {
        if (line.Length == 0 || char.IsLetter(line[0]))
            return false;

        return line.Contains(',') ||
               int.TryParse(line, NumberStyles.Integer, CultureInfo.InvariantCulture, out _);
    }

    public void Delete(string name)
    {
        var path = GetPath(name);
        if (File.Exists(path))
            File.Delete(path);
    }

    private string GetPath(string name)
    {
        return Path.Combine(_programDirectory, ValidateName(name) + ".images");
    }

    private static string ValidateName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new InvalidOperationException("INVALID NAME");

        return name.Trim().ToUpperInvariant();
    }
}
