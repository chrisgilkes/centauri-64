using System.Collections.Generic;

namespace Centauri64.Session;

public sealed class FeatureAvailability
{
    private readonly bool _allReleased;
    private readonly HashSet<FeatureId> _unlocked;

    private FeatureAvailability(bool allReleased, HashSet<FeatureId> unlocked)
    {
        _allReleased = allReleased;
        _unlocked = unlocked;
    }

    public static FeatureAvailability AllReleased() =>
        new(true, new HashSet<FeatureId>());

    public static FeatureAvailability FromUnlocks(IEnumerable<FeatureId> unlocks)
    {
        var set = new HashSet<FeatureId> { FeatureId.CoreBasic };
        foreach (var id in unlocks)
            set.Add(id);

        return new FeatureAvailability(false, set);
    }

    public bool IsAvailable(FeatureId feature)
    {
        if (_allReleased)
            return true;

        if (feature == FeatureId.CoreBasic)
            return true;

        return _unlocked.Contains(feature);
    }
}
