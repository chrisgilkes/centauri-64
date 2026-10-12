using System;
using System.IO;

namespace Centauri64.Machine.Images;

/// <summary>
/// Persistent player artwork store, independent of BASIC tapes.
/// Artwork belongs to the computer; tapes get portable packed copies on SAVE.
/// </summary>
public static class ArtworkLibrary
{
    public const string LibraryFileName = "library";

    public static string Location { get; } = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "Centauri64",
        "Artwork");

    private static readonly ImageStorage Storage = new(Location);

    public static void EnsureExists() => Directory.CreateDirectory(Location);

    public static void Load(ImageAssetStore store)
    {
        EnsureExists();
        Storage.Load(LibraryFileName, store);
    }

    public static void Save(ImageAssetStore store)
    {
        EnsureExists();
        Storage.Save(LibraryFileName, store);
    }

    public static string LibraryPath =>
        Path.Combine(Location, LibraryFileName + ".images");
}
