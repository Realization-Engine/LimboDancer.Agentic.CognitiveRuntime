using System.Collections.ObjectModel;

namespace LimboDancer.Abstractions.Domain;

public enum DomainEntityResolutionOutcome
{
    Resolved,
    Unresolved,
    Ambiguous,
}

public sealed class DomainEntityResolution
{
    public DomainEntityResolution(
        DomainEntityQuery query,
        DomainEntityResolutionOutcome outcome,
        IEnumerable<DomainEntityCandidate> candidates,
        IEnumerable<string> reasonCodes)
    {
        ArgumentNullException.ThrowIfNull(query);
        if (!Enum.IsDefined(outcome))
        {
            throw new ArgumentOutOfRangeException(nameof(outcome));
        }

        ArgumentNullException.ThrowIfNull(candidates);
        var candidateValues = candidates.ToArray();
        if (candidateValues.Any(candidate =>
                candidate is null
                || candidate.Identity.DomainId != query.Package.DomainId
                || candidate.CanonicalReference.Package != query.Package
                || candidate.Evidence.Package != query.Package
                || candidate.Evidence.TenantId != query.TenantId))
        {
            throw new ArgumentException(
                "Entity candidates must match the query tenant and exact package.",
                nameof(candidates));
        }

        var expectedCount = outcome switch
        {
            DomainEntityResolutionOutcome.Resolved => 1,
            DomainEntityResolutionOutcome.Unresolved => 0,
            DomainEntityResolutionOutcome.Ambiguous => 2,
            _ => throw new ArgumentOutOfRangeException(nameof(outcome)),
        };
        if (outcome == DomainEntityResolutionOutcome.Ambiguous
            ? candidateValues.Length < expectedCount
            : candidateValues.Length != expectedCount)
        {
            throw new ArgumentException("Entity candidate count does not match the resolution outcome.", nameof(candidates));
        }

        var candidateIds = candidateValues.Select(static candidate => candidate.Identity).ToArray();
        if (candidateIds.Distinct().Count() != candidateIds.Length)
        {
            throw new ArgumentException("Entity candidates must have unique identities.", nameof(candidates));
        }

        ArgumentNullException.ThrowIfNull(reasonCodes);
        var reasons = reasonCodes.ToArray();
        if (reasons.Length == 0 || reasons.Any(string.IsNullOrWhiteSpace))
        {
            throw new ArgumentException("Entity resolution requires non-empty reason codes.", nameof(reasonCodes));
        }

        Query = query;
        Outcome = outcome;
        Candidates = new ReadOnlyCollection<DomainEntityCandidate>(candidateValues);
        ReasonCodes = new ReadOnlyCollection<string>(reasons);
    }

    public DomainEntityQuery Query
    {
        get;
    }

    public DomainEntityResolutionOutcome Outcome
    {
        get;
    }

    public IReadOnlyList<DomainEntityCandidate> Candidates
    {
        get;
    }

    public IReadOnlyList<string> ReasonCodes
    {
        get;
    }
}
