using Centauri64.Progression;

namespace Centauri64.Session;

/// <summary>
/// Current session only. Never persisted as a career field.
/// </summary>
public static class GameSession
{
    public static PlayExperience Experience { get; private set; } =
        PlayExperience.None;

    public static int? ActiveSlot { get; private set; }

    public static CareerState? Career { get; private set; }

    public static bool IsBedroom =>
        Experience == PlayExperience.BedroomCoder && Career != null;

    public static bool IsHardcore =>
        Experience == PlayExperience.HardcoreCoder;

    public static void Clear()
    {
        Experience = PlayExperience.None;
        ActiveSlot = null;
        Career = null;
        FeatureGate.Current = FeatureAvailability.AllReleased();
    }

    public static void EnterHardcore()
    {
        Experience = PlayExperience.HardcoreCoder;
        ActiveSlot = null;
        Career = null;
        FeatureGate.Current = FeatureAvailability.AllReleased();
    }

    public static void EnterBedroom(int slot, CareerState career)
    {
        Experience = PlayExperience.BedroomCoder;
        ActiveSlot = slot;
        Career = career;
        FeatureGate.Current = FeatureAvailability.FromUnlocks(career.GetUnlockedFeatures());
    }

    public static void RefreshFeatures()
    {
        if (Career == null || Experience != PlayExperience.BedroomCoder)
        {
            FeatureGate.Current = FeatureAvailability.AllReleased();
            return;
        }

        FeatureGate.Current = FeatureAvailability.FromUnlocks(Career.GetUnlockedFeatures());
    }

    public static PlayerProgress ActiveProgress() =>
        Career?.Progress ?? new PlayerProgress();
}
