using TaxApi.Models;

namespace TaxApi.Services;

/// <summary>
/// Enforces the allowed status transition graph:
///   Submitted → UnderReview
///   UnderReview → Approved | Rejected
///   Approved → Filed
/// </summary>
public static class SubmissionWorkflow
{
    private static readonly Dictionary<SubmissionStatus, HashSet<SubmissionStatus>> Transitions = new()
    {
        [SubmissionStatus.Submitted]   = [SubmissionStatus.UnderReview],
        [SubmissionStatus.UnderReview] = [SubmissionStatus.Approved, SubmissionStatus.Rejected],
        [SubmissionStatus.Approved]    = [SubmissionStatus.Filed],
        [SubmissionStatus.Rejected]    = [],
        [SubmissionStatus.Filed]       = []
    };

    public static bool CanTransition(SubmissionStatus from, SubmissionStatus to)
        => Transitions.TryGetValue(from, out var allowed) && allowed.Contains(to);

    public static string[] AllowedNext(SubmissionStatus from)
        => Transitions.TryGetValue(from, out var allowed)
            ? [.. allowed.Select(s => s.ToString())]
            : [];
}
