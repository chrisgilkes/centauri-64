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

    /// <summary>Saved career for the active bedroom slot. Never replaced by override.</summary>
    public static CareerState? Career { get; private set; }

    /// <summary>
    /// Career used for unlock / availability queries.
    /// When developer override is active, this is a simulated in-memory career.
    /// </summary>
    public static CareerState? EffectiveCareer =>
        CareerProgressOverride.IsActive
            ? CareerProgressOverride.Simulated
            : Career;

    public static bool IsBedroom =>
        Experience == PlayExperience.BedroomCoder && Career != null;

    public static bool IsHardcore =>
        Experience == PlayExperience.HardcoreCoder;

    public static void Clear()
    {
        Experience = PlayExperience.None;
        ActiveSlot = null;
        Career = null;
        CareerProgressOverride.Disable();
        FeatureGate.Current = FeatureAvailability.AllReleased();
    }

    public static void EnterHardcore()
    {
        Experience = PlayExperience.HardcoreCoder;
        ActiveSlot = null;
        Career = null;
        CareerProgressOverride.Disable();
        FeatureGate.Current = FeatureAvailability.AllReleased();
    }

    public static void EnterBedroom(int slot, CareerState career)
    {
        Experience = PlayExperience.BedroomCoder;
        ActiveSlot = slot;
        Career = career;
        CareerProgressOverride.Disable();
        RefreshFeatures();
    }

    public static void RefreshFeatures()
    {
        if (Experience != PlayExperience.BedroomCoder)
        {
            FeatureGate.Current = FeatureAvailability.AllReleased();
            return;
        }

        var career = EffectiveCareer;
        if (career == null)
        {
            FeatureGate.Current = FeatureAvailability.AllReleased();
            return;
        }

        FeatureGate.Current = FeatureAvailability.FromUnlocks(career.GetUnlockedFeatures());
    }

    /// <summary>
    /// Apply developer progression override and refresh feature gates.
    /// Does not touch the saved career.
    /// </summary>
    public static void ApplyProgressOverride(bool enabled, int issueNumber, ProgressionStage stage)
    {
        if (Experience != PlayExperience.BedroomCoder || Career == null)
        {
            CareerProgressOverride.Disable();
            RefreshFeatures();
            return;
        }

        CareerProgressOverride.Set(enabled, issueNumber, stage);
        RefreshFeatures();
    }

    public static PlayerProgress ActiveProgress() =>
        EffectiveCareer?.Progress ?? Career?.Progress ?? new PlayerProgress();
}
