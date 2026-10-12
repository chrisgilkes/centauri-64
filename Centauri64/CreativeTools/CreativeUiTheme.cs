using Microsoft.Xna.Framework;

namespace Centauri64.CreativeTools;

/// <summary>
/// Shared creative-tool chrome. Neutrals are shared; accents switch per tool.
/// Edit <see cref="ImageAccent"/> / <see cref="SpriteAccent"/> to recolour tools.
/// </summary>
public static class CreativeUiTheme
{
    // --- Shared neutrals ---
    public static Color Panel = new(24, 28, 48);
    public static Color PanelLight = new(40, 48, 72);
    public static Color Border = new(90, 110, 150);
    public static Color Highlight = new(232, 205, 92);
    public static Color Text = new(230, 230, 235);
    public static Color Muted = new(140, 150, 170);
    public static Color Danger = new(220, 80, 80);
    public static Color CanvasChrome = new(16, 18, 28);

    // --- Per-tool accents (edit these) ---
    public static Color ImageAccent = new(91, 214, 205);
    public static Color ImageMenuHot = new(60, 80, 120);

    public static Color SpriteAccent = new(232, 140, 48);
    public static Color SpriteMenuHot = new(120, 70, 30);

    // --- Active colours used by MenuBar / ToolBar / panels ---
    public static Color Accent = ImageAccent;
    public static Color MenuHot = ImageMenuHot;

    public static void UseImageTool()
    {
        Accent = ImageAccent;
        MenuHot = ImageMenuHot;
    }

    public static void UseSpriteTool()
    {
        Accent = SpriteAccent;
        MenuHot = SpriteMenuHot;
    }
}
