using System;
using System.Collections.Generic;
using System.IO;

using Centauri64.Basic;

namespace Centauri64.Session;

/// <summary>
/// Installs entitled software into the shared tape library once.
/// Future shop purchases can call the same grant path.
/// </summary>
public static class SoftwareGrant
{
    public static void GrantTapes(IEnumerable<string> tapeNames)
    {
        var sourceDir = Path.Combine(AppContext.BaseDirectory, "Programs");
        var destDir = TapeFolder.Location;
        Directory.CreateDirectory(destDir);

        foreach (var name in tapeNames)
        {
            if (string.IsNullOrWhiteSpace(name))
                continue;

            var destBas = Path.Combine(destDir, name + ".bas");
            if (File.Exists(destBas))
                continue;

            foreach (var extension in new[]
                     { ".bas", ".tape", ".cover", ".sprites", ".maps" })
            {
                var source = Path.Combine(sourceDir, name + extension);
                if (!File.Exists(source))
                    continue;

                File.Copy(source, Path.Combine(destDir, name + extension), overwrite: false);
            }
        }
    }
}
