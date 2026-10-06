namespace Centauri64.Machine.Images;

/// <summary>
/// Organisational usage hint for an Image asset.
/// Not a separate asset type — runtime validates size/mode technically.
/// Future Sprite Builder and Map Editor filter by these categories.
/// </summary>
public enum ImageCategory
{
    General = 0,
    Sprite = 1,
    Tileset = 2,
    Background = 3
}
