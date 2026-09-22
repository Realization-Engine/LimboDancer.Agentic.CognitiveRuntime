using LimboDancer.Abstractions.Actions;
using LimboDancer.Abstractions.Audit;
using LimboDancer.Abstractions.Decision;
using LimboDancer.Abstractions.Execution;

namespace LimboDancer.Runtime.Decision;

public sealed class DecisionPlane : IDecisionPlane
{
    private const string BoundaryProviderId = "runtime:decision/Boundary";
    private readonly IDecisionProvider provider;
    private readonly IAuditSink auditSink;
    private readonly TimeProvider timeProvider;

    public DecisionPlane(
        IDecisionProvider provider,
        IAuditSink auditSink,
        TimeProvider? timeProvider = null)
    {
        ArgumentNullException.ThrowIfNull(provider);
        ArgumentException.ThrowIfNullOrWhiteSpace(provider.ProviderId);
        if (provider.ProviderVersion is not null)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(provider.ProviderVersion);
        }

        ArgumentNullException.ThrowIfNull(auditSink);
        this.provider = provider;
        this.auditSink = auditSink;
        this.timeProvider = timeProvider ?? TimeProvider.System;
    }

    public async Task<DecisionPlaneResult> DecideAsync(
        DecisionContext context,
        IReadOnlyList<PermittedAction> candidates,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(candidates);
        cancellationToken.ThrowIfCancellationRequested();
        EnsureCandidates(candidates);
        if (candidates.Count == 0)
        {
            var emptyResult = new DecisionResult(
                DecisionOutcome.Abstained,
                null,
                BoundaryProviderId,
                "decision.no_permitted_candidates");
            await WriteAuditAsync(
                    AuditEventType.DecisionEvaluated,
                    context,
                    emptyResult,
                    reasonCodes: [emptyResult.ReasonCode],
                    cancellationToken)
                .ConfigureAwait(false);
            return new DecisionPlaneResult(emptyResult, selectedAction: null);
        }

        DecisionResult result;
        try
        {
            result = await provider.DecideAsync(context, candidates, cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception)
        {
            await WriteRejectedAuditAsync(context, "decision.provider_failed", cancellationToken)
                .ConfigureAwait(false);
            throw;
        }

        try
        {
            ArgumentNullException.ThrowIfNull(result);
            if (!string.Equals(result.ProviderId, provider.ProviderId, StringComparison.Ordinal)
                || !string.Equals(result.ProviderVersion, provider.ProviderVersion, StringComparison.Ordinal))
            {
                throw new InvalidOperationException("Decision provider identity does not match its result.");
            }

            DecisionResultValidator.Validate(result, candidates);
        }
        catch (Exception exception) when (exception is ArgumentException or InvalidOperationException)
        {
            await WriteRejectedAuditAsync(context, "decision.invalid_result", cancellationToken)
                .ConfigureAwait(false);
            throw new InvalidOperationException("Decision provider returned an invalid result.", exception);
        }

        SelectedAction? selectedAction = null;
        if (result.Outcome == DecisionOutcome.Selected)
        {
            var selected = candidates.Single(candidate => string.Equals(
                candidate.Candidate.CandidateId,
                result.SelectedCandidateId,
                StringComparison.Ordinal));
            selectedAction = new SelectedAction(selected.Candidate, result);
        }

        await WriteAuditAsync(
                AuditEventType.DecisionEvaluated,
                context,
                result,
                [result.ReasonCode],
                cancellationToken)
            .ConfigureAwait(false);
        return new DecisionPlaneResult(result, selectedAction);
    }

    private static void EnsureCandidates(IReadOnlyList<PermittedAction> candidates)
    {
        if (candidates.Any(static candidate => candidate is null))
        {
            throw new ArgumentException("Decision candidates cannot contain null values.", nameof(candidates));
        }

        var candidateIds = candidates.Select(static candidate => candidate.Candidate.CandidateId).ToArray();
        if (candidateIds.Distinct(StringComparer.Ordinal).Count() != candidateIds.Length)
        {
            throw new ArgumentException("Decision candidates must have unique identifiers.", nameof(candidates));
        }
    }

    private ValueTask WriteRejectedAuditAsync(
        DecisionContext context,
        string reasonCode,
        CancellationToken cancellationToken)
    {
        var rejected = new DecisionResult(
            DecisionOutcome.Abstained,
            null,
            provider.ProviderId,
            reasonCode,
            providerVersion: provider.ProviderVersion);
        return WriteAuditAsync(
            AuditEventType.DecisionRejected,
            context,
            rejected,
            [reasonCode],
            cancellationToken);
    }

    private ValueTask WriteAuditAsync(
        AuditEventType eventType,
        DecisionContext context,
        DecisionResult result,
        IReadOnlyList<string> reasonCodes,
        CancellationToken cancellationToken) => auditSink.WriteAsync(
            new RuntimeAuditEvent(
                Guid.NewGuid(),
                eventType,
                context.InvocationId,
                context.Goal.CorrelationId,
                context.Goal.TenantId,
                timeProvider.GetUtcNow(),
                candidateId: result.SelectedCandidateId,
                outcomeCode: result.Outcome.ToString(),
                reasonCodes: reasonCodes,
                goalId: context.Goal.Id,
                stepId: context.StepId,
                decisionOutcome: result.Outcome,
                decisionProviderId: result.ProviderId,
                decisionProviderVersion: result.ProviderVersion,
                decisionConfidence: result.Confidence),
            cancellationToken);
}
