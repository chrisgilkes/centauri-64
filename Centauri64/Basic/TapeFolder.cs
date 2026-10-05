using System;
using System.IO;

namespace Centauri64.Basic;

public static class TapeFolder
{
    public static string Location { get; } = Path.Combine(
        Environment.GetFolderPath(
            Environment.SpecialFolder.LocalApplicationData),
        "Centauri64",
        "Tapes");

    private static bool _ready;

    public static void EnsureExists()
    {
        if (_ready)
            return;

        Directory.CreateDirectory(Location);
        ImportLegacyTapes();
        _ready = true;
    }

    private static void ImportLegacyTapes()
    {
        var legacy = Path.Combine(
            AppContext.BaseDirectory,
            "Programs");

        if (!Directory.Exists(legacy))
            return;

        foreach (var source in Directory.EnumerateFiles(legacy))
        {
            var name = Path.GetFileName(source);
            var extension = Path.GetExtension(name);

            if (extension != ".bas" &&
                extension != ".sprites" &&
                extension != ".tape" &&
                extension != ".cover" &&
                extension != ".maps")
            {
                continue;
            }

            var destination = Path.Combine(Location, name);

            // Refresh shipped demos when the install copy is newer.
            // Player-made tapes (not in Programs/) are never overwritten.
            if (File.Exists(destination) &&
                File.GetLastWriteTimeUtc(destination) >=
                File.GetLastWriteTimeUtc(source))
            {
                continue;
            }

            File.Copy(source, destination, overwrite: true);
        }
    }
}
