using System;
using System.IO;
using System.Text.Json;

namespace Centauri64.Progression;

public sealed class PlayerProgressStorage
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true
    };

    private readonly string _path;

    public PlayerProgressStorage()
    {
        var folder = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Centauri64");

        Directory.CreateDirectory(folder);
        _path = Path.Combine(folder, "player.json");
    }

    public PlayerProgress Load()
    {
        if (!File.Exists(_path))
            return new PlayerProgress();

        try
        {
            var json = File.ReadAllText(_path);
            return JsonSerializer.Deserialize<PlayerProgress>(json, JsonOptions)
                   ?? new PlayerProgress();
        }
        catch
        {
            return new PlayerProgress();
        }
    }

    public void Save(PlayerProgress progress)
    {
        var json = JsonSerializer.Serialize(progress, JsonOptions);
        File.WriteAllText(_path, json);
    }
}
