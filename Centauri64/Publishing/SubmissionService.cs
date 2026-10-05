using Centauri64.Analysis;
using Centauri64.Basic;
using Centauri64.Progression;

namespace Centauri64.Publishing;

/// <summary>
/// Compatibility wrapper around <see cref="CareerService"/>.
/// </summary>
public sealed class SubmissionService
{
    private readonly CareerService _career = new();

    public PlayerProgress LoadProgress() => _career.LoadProgress();

    public SubmissionResult Evaluate(
        SubmissionContract contract,
        SoftwareAnalysis analysis,
        TapeLabel label) =>
        _career.Evaluate(contract, analysis, label);

    public bool TryAccept(
        SubmissionContract contract,
        SoftwareAnalysis analysis,
        TapeLabel label,
        string tapeName,
        out SubmissionResult result) =>
        _career.TrySubmit(contract, tapeName, label, analysis, out result);
}
