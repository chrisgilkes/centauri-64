using Centauri64.Basic.Syntax;

namespace Centauri64.Session;

/// <summary>
/// Session-wide authoring gate. Default is fully unlocked so headless tests
/// and Hardcore never trip career restrictions.
/// </summary>
public static class FeatureGate
{
    public static FeatureAvailability Current { get; set; } =
        FeatureAvailability.AllReleased();

    public static FeatureId? RequiredFor(Statement statement)
    {
        return statement switch
        {
            PlotStatement or LineStatement or RectStatement or CircleStatement
                => FeatureId.Graphics,

            SpriteStatement or SpritePositionStatement or SpriteShowStatement
                or SpriteHideStatement or SpriteFlipStatement
                or SpriteAnimationStatement => FeatureId.Sprites,

            TdefStatement or MapStatement or LoadMapStatement
                or CameraStatement or CamOffStatement => FeatureId.Maps,

            ImageStatement or BgStatement or FgStatement => FeatureId.Images,

            NetHostStatement or NetJoinStatement or NetWaitStatement
                or NetLeaveStatement or NetSendStatement => FeatureId.Networking,

            _ => null
        };
    }

    public static string LockedMessage(FeatureId feature)
    {
        return feature switch
        {
            FeatureId.Graphics => "BASIC GRAPHICS NOT YET AVAILABLE",
            FeatureId.Sprites => "SPRITE GRAPHICS NOT YET AVAILABLE",
            FeatureId.Maps => "MAP GRAPHICS NOT YET AVAILABLE",
            FeatureId.Images => "IMAGE GRAPHICS NOT YET AVAILABLE",
            FeatureId.Networking => "NETWORKING NOT YET AVAILABLE",
            _ => "FEATURE NOT YET AVAILABLE"
        };
    }

    public static string ToolLockedNotice(FeatureId feature)
    {
        var tool = feature switch
        {
            FeatureId.Sprites => "SPRITE DESIGNER",
            FeatureId.Maps => "MAP EDITOR",
            FeatureId.Images => "IMAGE EDITOR",
            _ => "THIS TOOL"
        };

        return tool + " NOT YET AVAILABLE\n\nLOOK OUT FOR AN UPCOMING ISSUE OF\nCENTAURI64 MAGAZINE.";
    }

    /// <summary>
    /// Full Image Editor (Issue #5) — all categories and sizes.
    /// </summary>
    public static bool CanOpenFullImageEditor() =>
        Current.IsAvailable(FeatureId.Images);

    /// <summary>
    /// Restricted Sprite Artwork mode (Issue #3) or full Images (Issue #5).
    /// Does not unlock FeatureId.Images globally.
    /// </summary>
    public static bool CanOpenSpriteArtworkEditor() =>
        Current.IsAvailable(FeatureId.Sprites) ||
        Current.IsAvailable(FeatureId.Images);
}
