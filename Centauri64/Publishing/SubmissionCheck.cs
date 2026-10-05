namespace Centauri64.Publishing;

public sealed class SubmissionCheck
{
    public string Label { get; }

    public bool Passed { get; }

    /// <summary>
    /// Optional extra lines (e.g. YOUR PROGRAM: 537).
    /// </summary>
    public string? Detail { get; }

    public SubmissionCheck(string label, bool passed, string? detail = null)
    {
        Label = label;
        Passed = passed;
        Detail = detail;
    }
}
