using System;
using System.Collections.Generic;
using System.Linq;

namespace Centauri64.Publishing;

public sealed class SubmissionResult
{
    public bool Accepted { get; init; }

    public bool AlreadyCompleted { get; init; }

    public IReadOnlyList<SubmissionCheck> Checks { get; init; } =
        Array.Empty<SubmissionCheck>();

    public IEnumerable<SubmissionCheck> FailedChecks =>
        Checks.Where(check => !check.Passed);
}
