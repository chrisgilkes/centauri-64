using Centauri64.Analysis;
using Centauri64.Basic;
using Centauri64.Progression;

namespace Centauri64.Publishing;

public sealed class SubmissionService
{
    private readonly PlayerProgressStorage _storage = new();

    public PlayerProgress LoadProgress() => _storage.Load();

    public SubmissionResult Evaluate(
        SubmissionContract contract,
        SoftwareAnalysis analysis,
        TapeLabel label)
    {
        var result = SubmissionEvaluator.Evaluate(
            contract.Requirements,
            analysis,
            label);

        var progress = _storage.Load();

        if (!contract.Repeatable && progress.HasCompleted(contract.Id))
        {
            return new SubmissionResult
            {
                Accepted = false,
                AlreadyCompleted = true,
                Checks = result.Checks
            };
        }

        return result;
    }

    /// <summary>
    /// Records a successful non-repeatable completion and queues a pending
    /// reward. Does not add cash immediately — mail delivery comes later.
    /// </summary>
    public bool TryAccept(
        SubmissionContract contract,
        SoftwareAnalysis analysis,
        TapeLabel label,
        string tapeName,
        out SubmissionResult result)
    {
        result = Evaluate(contract, analysis, label);

        if (!result.Accepted || result.AlreadyCompleted)
            return false;

        var progress = _storage.Load();
        var publisher = PublisherCatalog.GetPublisher(contract.PublisherId);

        if (!contract.Repeatable)
            progress.CompletedContractIds.Add(contract.Id);

        progress.PendingRewards.Add(new PendingReward
        {
            ContractId = contract.Id,
            PublisherName = publisher?.Name ?? contract.PublisherId,
            ContractTitle = contract.Title,
            TapeName = tapeName,
            AmountPennies = contract.RewardPennies
        });

        _storage.Save(progress);
        return true;
    }
}
