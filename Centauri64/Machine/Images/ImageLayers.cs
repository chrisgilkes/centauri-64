namespace Centauri64.Machine.Images;

/// <summary>
/// Persistent game composition layers (BG0/BG1/FG).
/// IMAGE command is a blit into the graphics layer — not stored here.
/// </summary>
public sealed class ImageLayers
{
    public ImageAsset? Background0 { get; set; }

    public ImageAsset? Background1 { get; set; }

    public ImageAsset? Foreground { get; set; }

    public void ClearAll()
    {
        Background0 = null;
        Background1 = null;
        Foreground = null;
    }
}
