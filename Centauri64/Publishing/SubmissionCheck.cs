namespace Centauri64.Publishing;

public sealed class SubmissionCheck
{
    public string Label { get; init; } = string.Empty;

    public bool Passed { get; init; }

    public SubmissionCheck(string label, bool passed)
    {
        Label = label;
        Passed = passed;
    }
}
