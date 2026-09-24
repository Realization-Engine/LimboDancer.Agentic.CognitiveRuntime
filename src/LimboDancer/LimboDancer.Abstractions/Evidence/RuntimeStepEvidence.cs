using System.Collections.ObjectModel;
using LimboDancer.Abstractions.Actions;
using LimboDancer.Abstractions.Decision;
using LimboDancer.Abstractions.Execution;
using LimboDancer.Abstractions.Observations;
using LimboDancer.Abstractions.Runtime;
using LimboDancer.Abstractions.Verification;

namespace LimboDancer.Abstractions.Evidence;

public sealed class RuntimeStepEvidence
{
    public RuntimeStepEvidence(
        Guid evidenceId,
        RuntimeInvocationId invocationId,
        Goal goal,
        StepId stepId,
        RuntimeBudget budget,
        DateTimeOffset recordedAt,
        IEnumerable<Observation>? observations = null,
        IEnumerable<ActionCandidate>? candidates = null,
        IEnumerable<PermittedAction>? permittedCandidates = null,
        IEnumerable<RejectedCandidate>? rejectedCandidates = null,
        DecisionResult? decision = null,
        GateEvidence? gate = null,
        ExecutionEvidence? execution = null,
        EffectVerificationResult? verification = null)
    {
        ArgumentOutOfRangeException.ThrowIfEqual(evidenceId, Guid.Empty);
        ArgumentOutOfRangeException.ThrowIfEqual(invocationId.Value, Guid.Empty);
        ArgumentNullException.ThrowIfNull(goal);
        ArgumentOutOfRangeException.ThrowIfEqual(stepId.Value, Guid.Empty);
        ArgumentNullException.ThrowIfNull(budget);

        var observationValues = Copy(observations, nameof(observations));
        if (observationValues.Any(item => item.TenantId != goal.TenantId))
        {
            throw new ArgumentException("Replay observations must match the Goal tenant.", nameof(observations));
        }

        var candidateValues = Copy(candidates, nameof(candidates));
        var permittedValues = Copy(permittedCandidates, nameof(permittedCandidates));
        var rejectedValues = Copy(rejectedCandidates, nameof(rejectedCandidates));
        EnsureCandidatePartition(candidateValues, permittedValues, rejectedValues);
        EnsureDecision(decision, permittedValues);
        if (execution is not null && gate?.Outcome != ExecutionGateOutcome.Authorized)
        {
            throw new ArgumentException("Execution evidence requires an authorized gate result.", nameof(execution));
        }

        if (verification is not null && execution is null)
        {
            throw new ArgumentException("Verification evidence requires an execution result.", nameof(verification));
        }

        EvidenceId = evidenceId;
        InvocationId = invocationId;
        Goal = goal;
        StepId = stepId;
        Budget = budget;
        RecordedAt = recordedAt;
        Observations = observationValues;
        Candidates = candidateValues;
        PermittedCandidates = permittedValues;
        RejectedCandidates = rejectedValues;
        Decision = decision;
        Gate = gate;
        Execution = execution;
        Verification = verification;
    }

    public Guid EvidenceId
    {
        get;
    }

    public RuntimeInvocationId InvocationId
    {
        get;
    }

    public Goal Goal
    {
        get;
    }

    public StepId StepId
    {
        get;
    }

    public RuntimeBudget Budget
    {
        get;
    }

    public DateTimeOffset RecordedAt
    {
        get;
    }

    public IReadOnlyList<Observation> Observations
    {
        get;
    }

    public IReadOnlyList<ActionCandidate> Candidates
    {
        get;
    }

    public IReadOnlyList<PermittedAction> PermittedCandidates
    {
        get;
    }

    public IReadOnlyList<RejectedCandidate> RejectedCandidates
    {
        get;
    }

    public DecisionResult? Decision
    {
        get;
    }

    public GateEvidence? Gate
    {
        get;
    }

    public ExecutionEvidence? Execution
    {
        get;
    }

    public EffectVerificationResult? Verification
    {
        get;
    }

    private static ReadOnlyCollection<T> Copy<T>(IEnumerable<T>? values, string parameterName)
        where T : class
    {
        var result = (values ?? []).ToArray();
        if (result.Any(static item => item is null))
        {
            throw new ArgumentException("Replay evidence collections cannot contain null values.", parameterName);
        }

        return new ReadOnlyCollection<T>(result);
    }

    private static void EnsureCandidatePartition(
        IReadOnlyList<ActionCandidate> candidates,
        IReadOnlyList<PermittedAction> permitted,
        IReadOnlyList<RejectedCandidate> rejected)
    {
        var candidateIds = candidates.Select(static item => item.CandidateId).ToArray();
        if (candidateIds.Distinct(StringComparer.Ordinal).Count() != candidateIds.Length)
        {
            throw new ArgumentException("Replay candidates must have unique identifiers.", nameof(candidates));
        }

        var constrainedIds = permitted
            .Select(static item => item.Candidate.CandidateId)
            .Concat(rejected.Select(static item => item.Candidate.CandidateId))
            .ToArray();
        if (constrainedIds.Distinct(StringComparer.Ordinal).Count() != constrainedIds.Length)
        {
            throw new ArgumentException(
                "A replay candidate cannot be both permitted and rejected.",
                nameof(permitted));
        }

        if (constrainedIds.Length != 0
            && !candidateIds.Order(StringComparer.Ordinal).SequenceEqual(
                constrainedIds.Order(StringComparer.Ordinal),
                StringComparer.Ordinal))
        {
            throw new ArgumentException(
                "Permitted and rejected replay candidates must partition the candidate set.",
                nameof(permitted));
        }
    }

    private static void EnsureDecision(
        DecisionResult? decision,
        ReadOnlyCollection<PermittedAction> permitted)
    {
        if (decision is null)
        {
            return;
        }

        if (permitted.Count == 0)
        {
            throw new ArgumentException("Decision evidence requires permitted candidates.", nameof(decision));
        }

        if (decision.SelectedCandidateId is not null
            && !permitted.Any(item => string.Equals(
                item.Candidate.CandidateId,
                decision.SelectedCandidateId,
                StringComparison.Ordinal)))
        {
            throw new ArgumentException(
                "The selected Decision candidate must be present in the permitted set.",
                nameof(decision));
        }
    }
}
