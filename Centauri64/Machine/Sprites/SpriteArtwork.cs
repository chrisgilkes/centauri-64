using System;

using Centauri64.Machine.Images;

namespace Centauri64.Machine.Sprites;

/// <summary>
/// Issue #3 bridge: eligible ImageAsset artwork → baked SpriteAnimation frames.
/// Playback uses baked pixels only; image name is optional authoring metadata.
/// Eligibility is size only — any 16×16 image can be bound as hardware sprite art.
/// </summary>
public static class SpriteArtwork
{
    public static bool IsEligible(ImageAsset? image)
    {
        if (image == null)
            return false;

        return image.Width == CentauriSprite.WIDTH &&
               image.Height == CentauriSprite.HEIGHT;
    }

    public static string? EligibilityError(ImageAsset? image)
    {
        if (image == null)
            return "IMAGE NOT FOUND";

        if (image.Width != CentauriSprite.WIDTH ||
            image.Height != CentauriSprite.HEIGHT)
        {
            return "IMAGE MUST BE 16X16";
        }

        return null;
    }

    public static void BakeIntoAnimation(SpriteAnimation animation, ImageAsset image)
    {
        var error = EligibilityError(image);
        if (error != null)
            throw new InvalidOperationException(error);

        animation.ReplaceFramesFromImage(image);
    }
}
