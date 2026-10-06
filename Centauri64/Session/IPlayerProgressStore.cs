using Centauri64.Progression;

namespace Centauri64.Session;

public interface IPlayerProgressStore
{
    PlayerProgress Load();
    void Save(PlayerProgress progress);
}

public sealed class FilePlayerProgressStore : IPlayerProgressStore
{
    private readonly PlayerProgressStorage _storage = new();

    public PlayerProgress Load() => _storage.Load();

    public void Save(PlayerProgress progress) => _storage.Save(progress);
}

public sealed class SessionCareerProgressStore : IPlayerProgressStore
{
    private readonly CareerRepository _repository;

    public SessionCareerProgressStore(CareerRepository repository)
    {
        _repository = repository;
    }

    public PlayerProgress Load()
    {
        if (GameSession.Career == null)
            return new PlayerProgress();

        GameSession.Career.Progress.MigrateLegacyRewards();
        return GameSession.Career.Progress;
    }

    public void Save(PlayerProgress progress)
    {
        if (GameSession.Career == null || GameSession.ActiveSlot == null)
            return;

        GameSession.Career.Progress = progress;
        _repository.Save(GameSession.ActiveSlot.Value, GameSession.Career);
    }
}
