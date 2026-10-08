using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

using Centauri64.Progression;

namespace Centauri64.Session;

public sealed class CareerSlotSummary
{
    public int Slot { get; init; }
    public bool Occupied { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Rank { get; init; } = string.Empty;
    public int CashPennies { get; init; }

    /// <summary>Owned Year One magazine issues present in the catalogue.</summary>
    public int MagazinesOwned { get; init; }

    /// <summary>Catalogue issue count (Year One denominator).</summary>
    public int MagazinesTotal { get; init; }

    /// <summary>Null when the save predates last-played tracking.</summary>
    public DateTime? LastPlayedUtc { get; init; }
}

public sealed class CareerRepository
{
    public const int SlotCount = 3;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true
    };

    private readonly string _folder;
    private readonly string _legacyPlayerPath;

    public CareerRepository()
    {
        var root = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Centauri64");

        Directory.CreateDirectory(root);
        _folder = Path.Combine(root, "Careers");
        Directory.CreateDirectory(_folder);
        _legacyPlayerPath = Path.Combine(root, "player.json");
        MigrateLegacyIfNeeded();
    }

    public IReadOnlyList<CareerSlotSummary> LoadSummaries()
    {
        var list = new List<CareerSlotSummary>(SlotCount);
        for (var slot = 1; slot <= SlotCount; slot++)
        {
            var state = TryLoad(slot);
            if (state == null)
            {
                list.Add(new CareerSlotSummary { Slot = slot, Occupied = false });
                continue;
            }

            list.Add(new CareerSlotSummary
            {
                Slot = slot,
                Occupied = true,
                Name = state.Name,
                Rank = state.Rank,
                CashPennies = state.Progress.CashPennies,
                MagazinesOwned = CountOwnedMagazines(state),
                MagazinesTotal = MagazineCatalog.Issues.Length,
                LastPlayedUtc = state.LastPlayedUtc
            });
        }

        return list;
    }

    /// <summary>
    /// Magazine progress = owned catalogue issues / Year One catalogue size.
    /// </summary>
    public static int CountOwnedMagazines(CareerState state)
    {
        var count = 0;
        foreach (var id in state.OwnedMagazineIds)
        {
            if (MagazineCatalog.Find(id) != null)
                count++;
        }

        return count;
    }

    public CareerState? TryLoad(int slot)
    {
        var path = SlotPath(slot);
        if (!File.Exists(path))
            return null;

        try
        {
            var json = File.ReadAllText(path);
            var state = JsonSerializer.Deserialize<CareerState>(json, JsonOptions);
            if (state == null || string.IsNullOrWhiteSpace(state.Name))
                return null;

            state.Progress ??= new PlayerProgress();
            state.Progress.MigrateLegacyRewards();
            state.MigrateMagazineFields();
            if (string.IsNullOrWhiteSpace(state.Rank))
                state.Rank = CareerRanks.Hobbyist;

            return state;
        }
        catch
        {
            return null;
        }
    }

    public void Save(int slot, CareerState state)
    {
        var path = SlotPath(slot);
        var json = JsonSerializer.Serialize(state, JsonOptions);
        var temp = path + ".tmp";
        File.WriteAllText(temp, json);
        File.Copy(temp, path, overwrite: true);
        File.Delete(temp);
    }

    public void TouchLastPlayed(int slot, CareerState state)
    {
        state.LastPlayedUtc = DateTime.UtcNow;
        Save(slot, state);
    }

    public void Delete(int slot)
    {
        var path = SlotPath(slot);
        if (File.Exists(path))
            File.Delete(path);

        var temp = path + ".tmp";
        if (File.Exists(temp))
            File.Delete(temp);
    }

    private string SlotPath(int slot) =>
        Path.Combine(_folder, $"slot{slot}.json");

    private void MigrateLegacyIfNeeded()
    {
        if (File.Exists(SlotPath(1)))
            return;

        if (!File.Exists(_legacyPlayerPath))
            return;

        try
        {
            var json = File.ReadAllText(_legacyPlayerPath);
            var progress = JsonSerializer.Deserialize<PlayerProgress>(json, JsonOptions);
            if (progress == null)
                return;

            progress.MigrateLegacyRewards();
            var migrated = CareerState.FromLegacy(progress);
            Save(1, migrated);
        }
        catch
        {
            // Leave the legacy file; start with empty slot 1.
        }
    }
}
