namespace Centauri64.Settings;

/// <summary>
/// Loads/saves settings and exposes the live instance for the session.
/// </summary>
public sealed class SettingsService
{
    private readonly SettingsStorage _storage = new();

    public CentauriSettings Current { get; private set; } =
        CentauriSettings.CreateDefaults();

    public CentauriSettings Load()
    {
        Current = _storage.Load();
        return Current;
    }

    public void Save() => _storage.Save(Current);

    public void Save(CentauriSettings settings)
    {
        Current = settings;
        _storage.Save(settings);
    }

    public void ReplaceCurrent(CentauriSettings settings) =>
        Current = settings;
}
