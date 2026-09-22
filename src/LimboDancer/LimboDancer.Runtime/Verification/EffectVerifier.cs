using System.Collections.ObjectModel;
using LimboDancer.Abstractions.Actions;
using LimboDancer.Abstractions.Audit;
using LimboDancer.Abstractions.Execution;
using LimboDancer.Abstractions.Verification;
using LimboDancer.Runtime.Execution;

namespace LimboDancer.Runtime.Verification;

public sealed class EffectVerifier : IEffectVerifier
{
    private readonly ReadOnlyDictionary<string, IEffectEvaluator> evaluators;
    private readonly IAuditSink auditSink;
    private readonly TimeProvider timeProvider;

    public EffectVerifier(
        IEnumerable<IEffectEvaluator> evaluators,
        IAuditSink auditSink,
        TimeProvider? timeProvider = null)
    {
        ArgumentNullException.ThrowIfNull(evaluators);
        ArgumentNullException.ThrowIfNull(auditSink);
        var registered = new Dictionary<string, IEffectEvaluator>(StringComparer.Ordinal);
        foreach (var evaluator in evaluators)
        {
            ArgumentNullException.ThrowIfNull(evaluator);
            ArgumentException.ThrowIfNullOrWhiteSpace(evaluator.EffectType);
            if (!registered.TryAdd(evaluator.EffectType, evaluator))
            {
                throw new InvalidOperationException(
                    $"Effect evaluator '{evaluator.EffectType}' is already registered.");
            }
        }

        this.evaluators = new ReadOnlyDictionary<string, IEffectEvaluator>(registered);
        this.auditSink = auditSink;
        this.timeProvider = timeProvider ?? TimeProvider.System;
    }

    public async Task<EffectVerificationResult> VerifyAsync(
        AuthorizedAction action,
        ActionExecutionResult execution,
        IReadOnlyList<EffectDescriptor> expectedEffects,
        VerificationContext context,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(action);
        ArgumentNullException.ThrowIfNull(execution);
        ArgumentNullException.ThrowIfNull(expectedEffects);
        ArgumentNullException.ThrowIfNull(context);
        cancellationToken.ThrowIfCancellationRequested();
        EnsureContext(action, expectedEffects, context);

        var findings = new List<EffectVerificationFinding>();
        foreach (var effect in expectedEffects)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (!evaluators.TryGetValue(effect.EffectType, out var evaluator))
            {
                findings.Add(new EffectVerificationFinding(
                    effect.Id,
                    VerificationStatus.Unverifiable,
                    "verification.evaluator_unavailable"));
                continue;
            }

            try
            {
                var finding = await evaluator
                    .EvaluateAsync(action, execution, effect, context, cancellationToken)
                    .ConfigureAwait(false);
                ArgumentNullException.ThrowIfNull(finding);
                if (!string.Equals(finding.EffectId, effect.Id, StringComparison.Ordinal))
                {
                    throw new InvalidOperationException("Effect evaluator returned a mismatched effect identifier.");
                }

                findings.Add(finding);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception)
            {
                findings.Add(new EffectVerificationFinding(
                    effect.Id,
                    VerificationStatus.Unverifiable,
                    "verification.evaluator_failed"));
            }
        }

        var status = Aggregate(findings);
        var result = new EffectVerificationResult(status, ReasonCode(status), findings);
        await auditSink.WriteAsync(
                new RuntimeAuditEvent(
                    Guid.NewGuid(),
                    AuditEventType.EffectVerificationEvaluated,
                    action.InvocationId,
                    action.CorrelationId,
                    action.TenantId,
                    timeProvider.GetUtcNow(),
                    action.Selected.Candidate.Descriptor.Id,
                    action.Selected.Candidate.Descriptor.Version,
                    action.PrincipalId,
                    action.Selected.Candidate.CandidateId,
                    action.Selected.Origin,
                    result.Status.ToString(),
                    new[] { result.ReasonCode }.Concat(
                        result.Findings.Select(static finding => finding.ReasonCode)),
                    authorizationId: action.AuthorizationId,
                    executionCode: execution.Code,
                    goalId: context.Goal.Id,
                    stepId: context.StepId),
                cancellationToken)
            .ConfigureAwait(false);
        return result;
    }

    private static void EnsureContext(
        AuthorizedAction action,
        IReadOnlyList<EffectDescriptor> expectedEffects,
        VerificationContext context)
    {
        if (action.InvocationId != context.InvocationId
            || action.TenantId != context.Goal.TenantId)
        {
            throw new ArgumentException("Verification identities must match the authorized action.", nameof(context));
        }

        if (expectedEffects.Any(static effect => effect is null))
        {
            throw new ArgumentException("Expected effects cannot contain null values.", nameof(expectedEffects));
        }

        var effectIds = expectedEffects.Select(static effect => effect.Id).ToArray();
        if (effectIds.Distinct(StringComparer.Ordinal).Count() != effectIds.Length)
        {
            throw new ArgumentException("Expected effects must have unique identifiers.", nameof(expectedEffects));
        }
    }

    private static VerificationStatus Aggregate(IReadOnlyList<EffectVerificationFinding> findings)
    {
        if (findings.Count == 0
            || findings.All(static finding => finding.Status == VerificationStatus.Unverifiable))
        {
            return VerificationStatus.Unverifiable;
        }

        if (findings.Any(static finding => finding.Status == VerificationStatus.Contradicted))
        {
            return VerificationStatus.Contradicted;
        }

        if (findings.All(static finding => finding.Status == VerificationStatus.Verified))
        {
            return VerificationStatus.Verified;
        }

        return VerificationStatus.PartiallyVerified;
    }

    private static string ReasonCode(VerificationStatus status) => status switch
    {
        VerificationStatus.Verified => "verification.verified",
        VerificationStatus.PartiallyVerified => "verification.partially_verified",
        VerificationStatus.Unverifiable => "verification.unverifiable",
        VerificationStatus.Contradicted => "verification.contradicted",
        _ => throw new ArgumentOutOfRangeException(nameof(status), status, null),
    };
}
