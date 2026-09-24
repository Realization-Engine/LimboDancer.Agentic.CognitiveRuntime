using LimboDancer.Abstractions.Actions;
using LimboDancer.Abstractions.Decision;

namespace LimboDancer.Runtime.Decision;

public static class DecisionResultValidator
{
    public static void Validate(
        DecisionResult result,
        IReadOnlyList<PermittedAction> candidates)
    {
        ArgumentNullException.ThrowIfNull(result);
        ArgumentNullException.ThrowIfNull(candidates);
        if (candidates.Any(static candidate => candidate is null))
        {
            throw new ArgumentException("Decision candidates cannot contain null values.", nameof(candidates));
        }

        var candidateIds = candidates
            .Select(static candidate => candidate.Candidate.CandidateId)
            .ToArray();
        var knownCandidates = candidateIds.ToHashSet(StringComparer.Ordinal);
        if (knownCandidates.Count != candidateIds.Length)
        {
            throw new ArgumentException("Decision candidates must have unique identifiers.", nameof(candidates));
        }

        if (result.Outcome == DecisionOutcome.Selected
            && !knownCandidates.Contains(result.SelectedCandidateId!))
        {
            throw new InvalidOperationException(
                $"Decision provider selected unknown candidate '{result.SelectedCandidateId}'.");
        }

        var unknownDistributionCandidate = result.Distribution.Keys
            .FirstOrDefault(candidateId => !knownCandidates.Contains(candidateId));
        if (unknownDistributionCandidate is not null)
        {
            throw new InvalidOperationException(
                $"Decision provider scored unknown candidate '{unknownDistributionCandidate}'.");
        }
    }
}
