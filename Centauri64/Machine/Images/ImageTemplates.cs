using System;
using System.Collections.Generic;

namespace Centauri64.Machine.Images;

public sealed class ImageTemplate
{
    public string Label { get; }
    public int Width { get; }
    public int Height { get; }
    public ImageCategory Category { get; }
    public bool IsFullScreen { get; }
    public bool IsCustom { get; }
    public int FillColour { get; }

    private ImageTemplate(
        string label,
        int width,
        int height,
        ImageCategory category,
        bool isFullScreen = false,
        bool isCustom = false,
        int fillColour = ImageAsset.Transparent)
    {
        Label = label;
        Width = width;
        Height = height;
        Category = category;
        IsFullScreen = isFullScreen;
        IsCustom = isCustom;
        FillColour = fillColour;
    }

    public static IReadOnlyList<ImageTemplate> ForMode(CentauriDisplayMode mode)
    {
        var fullW = ImageAsset.ModeWidth(mode);
        var fullH = ImageAsset.ModeHeight(mode);
        var illustW = Math.Min(160, fullW);
        var illustH = Math.Min(100, fullH);
        var wideH = Math.Min(100, fullH);

        return new[]
        {
            new ImageTemplate("8x8 TILE", 8, 8, ImageCategory.Tileset),
            new ImageTemplate("16x16 SPRITE", 16, 16, ImageCategory.Sprite),
            new ImageTemplate("32x32 SPRITE", 32, 32, ImageCategory.Sprite),
            new ImageTemplate("64x64 IMAGE", 64, 64, ImageCategory.General),
            new ImageTemplate($"{illustW}x{illustH} ILLUST.", illustW, illustH, ImageCategory.General),
            new ImageTemplate($"{fullW}x{wideH} WIDE", fullW, wideH, ImageCategory.General),
            new ImageTemplate(
                "FULL SCREEN BG",
                fullW,
                fullH,
                ImageCategory.Background,
                isFullScreen: true,
                fillColour: 0),
            new ImageTemplate("CUSTOM", 16, 16, ImageCategory.General, isCustom: true)
        };
    }
}
