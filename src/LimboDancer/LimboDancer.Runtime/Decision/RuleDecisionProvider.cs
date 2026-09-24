using LimboDancer.Abstractions.Actions;
using LimboDancer.Abstractions.Decision;

namespace LimboDancer.Runtime.Decision;

public sealed class RuleDecisionProvider : IDecisionProvider
{
    public const string Id = "runtime:decision/Rule";
    public const string Version = "1";

    public string ProviderId => Id;

    public string ProviderVersion => Version;

    public Task<DecisionResult> DecideAsync(
        DecisionContext context,
        IReadOnlyList<PermittedAction> candidates,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(candidates);
        cancellationToken.ThrowIfCancellationRequested();
        if (candidates.Any(static candidate => candidate is null))
        {
            throw new ArgumentException("Decision candidates cannot contain null values.", nameof(candidates));
        }

        var candidateIds = candidates
            .Select(static candidate => candidate.Candidate.CandidateId)
            .ToArray();
        if (candidateIds.Distinct(StringComparer.Ordinal).Count() != candidateIds.Length)
        {
            throw new ArgumentException("Decision candidates must have unique identifiers.", nameof(candidates));
        }

        var result = candidates.Count switch
        {
            0 => new DecisionResult(
                DecisionOutcome.Abstained,
                null,
                ProviderId,
                "decision.no_candidates",
                providerVersion: ProviderVersion),
            1 => new DecisionResult(
                DecisionOutcome.Selected,
                candidates[0].Candidate.CandidateId,
                ProviderId,
                "decision.single_candidate",
                providerVersion: ProviderVersion),
            _ => new DecisionResult(
                DecisionOutcome.Escalated,
                null,
                ProviderId,
                "decision.multiple_candidates",
                providerVersion: ProviderVersion),
        };
        return Task.FromResult(result);
    }
}
