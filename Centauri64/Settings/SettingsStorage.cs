using System;
using System.Diagnostics;
using System.IO;
using System.Text.Json;

namespace Centauri64.Settings;

public sealed class SettingsStorage
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        Converters = { new System.Text.Json.Serialization.JsonStringEnumConverter() }
    };

    private readonly string _path;

    public SettingsStorage()
    {
        var folder = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Centauri64");

        Directory.CreateDirectory(folder);
        _path = Path.Combine(folder, "settings.json");
    }

    public string FilePath => _path;

    public CentauriSettings Load()
    {
        if (!File.Exists(_path))
            return CentauriSettings.CreateDefaults();

        try
        {
            var json = File.ReadAllText(_path);
            var settings = JsonSerializer.Deserialize<CentauriSettings>(json, JsonOptions);
            return settings ?? CentauriSettings.CreateDefaults();
        }
        catch (Exception exception)
        {
            Debug.WriteLine($"[Settings] Failed to load settings: {exception.Message}");
            System.Console.WriteLine($"[Settings] Failed to load settings — using defaults.");
            return CentauriSettings.CreateDefaults();
        }
    }

    public void Save(CentauriSettings settings)
    {
        try
        {
            var json = JsonSerializer.Serialize(settings, JsonOptions);
            File.WriteAllText(_path, json);
        }
        catch (Exception exception)
        {
            Debug.WriteLine($"[Settings] Failed to save settings: {exception.Message}");
            System.Console.WriteLine($"[Settings] Failed to save settings.");
        }
    }
}
