namespace Centauri64.Session;

/// <summary>
/// Authoring / learning capabilities. Runtime still executes software that
/// uses a capability the player has not yet learned.
/// Planned values record curriculum; commands may not exist yet.
/// </summary>
public enum FeatureId
{
    CoreBasic,
    Graphics,
    Sprites,
    Maps,
    Images,
    Arrays,
    DataStatements,
    Networking,
    CustomAssets,
    LowLevelMachine
}

public enum MagazineIssueState
{
    Owned,
    ComingNext,
    ComingLater
}
