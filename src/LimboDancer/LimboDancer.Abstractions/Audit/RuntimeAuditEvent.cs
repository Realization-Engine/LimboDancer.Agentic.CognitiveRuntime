using System.Collections.ObjectModel;
using LimboDancer.Abstractions.Actions;
using LimboDancer.Abstractions.Decision;
using LimboDancer.Abstractions.Diagnostics;
using LimboDancer.Abstractions.Execution;
using LimboDancer.Abstractions.Runtime;

namespace LimboDancer.Abstractions.Audit;

public sealed class RuntimeAuditEvent
{
    public RuntimeAuditEvent(
        Guid auditId,
        AuditEventType eventType,
        RuntimeInvocationId invocationId,
        CorrelationId correlationId,
        Guid tenantId,
        DateTimeOffset occurredAt,
        ActionId? actionId = null,
        ActionVersion? actionVersion = null,
        string? principalId = null,
        string? candidateId = null,
        SelectionOrigin? selectionOrigin = null,
        string? outcomeCode = null,
        IEnumerable<string>? reasonCodes = null,
        AuditDiagnosticFinding? diagnosticFinding = null,
        DiagnosticDisposition? diagnosticDisposition = null,
        ExecutionGateOutcome? executionGateOutcome = null,
        string? authorizationId = null,
        string? executionCode = null,
        TimeSpan? duration = null,
        GoalId? goalId = null,
        StepId? stepId = null,
        DecisionOutcome? decisionOutcome = null,
        string? decisionProviderId = null,
        string? decisionProviderVersion = null,
        double? decisionConfidence = null)
    {
        ArgumentOutOfRangeException.ThrowIfEqual(auditId, Guid.Empty);
        ArgumentOutOfRangeException.ThrowIfEqual(invocationId.Value, Guid.Empty);
        ArgumentException.ThrowIfNullOrWhiteSpace(correlationId.Value);
        if (actionId.HasValue != actionVersion.HasValue)
        {
            throw new ArgumentException("Action identity and version must be supplied together.", nameof(actionVersion));
        }

        if (principalId is not null)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(principalId);
        }

        if (candidateId is not null)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(candidateId);
        }

        if (outcomeCode is not null)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(outcomeCode);
        }

        if (authorizationId is not null)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(authorizationId);
        }

        if (executionCode is not null)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(executionCode);
        }

        if (duration < TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(duration), duration, "Duration cannot be negative.");
        }

        if (goalId is { Value: var goalValue })
        {
            ArgumentOutOfRangeException.ThrowIfEqual(goalValue, Guid.Empty);
        }

        if (stepId is { Value: var stepValue })
        {
            ArgumentOutOfRangeException.ThrowIfEqual(stepValue, Guid.Empty);
        }

        if (decisionOutcome is { } outcome && !Enum.IsDefined(outcome))
        {
            throw new ArgumentOutOfRangeException(nameof(decisionOutcome));
        }

        if (decisionProviderId is not null)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(decisionProviderId);
        }

        if (decisionProviderVersion is not null)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(decisionProviderVersion);
        }

        if (decisionConfidence is < 0 or > 1
            || (decisionConfidence is { } confidenceValue && !double.IsFinite(confidenceValue)))
        {
            throw new ArgumentOutOfRangeException(nameof(decisionConfidence));
        }

        var reasons = (reasonCodes ?? []).ToArray();
        if (reasons.Any(string.IsNullOrWhiteSpace))
        {
            throw new ArgumentException("Reason codes cannot be empty.", nameof(reasonCodes));
        }

        AuditId = auditId;
        EventType = eventType;
        InvocationId = invocationId;
        CorrelationId = correlationId;
        TenantId = tenantId;
        OccurredAt = occurredAt;
        ActionId = actionId;
        ActionVersion = actionVersion;
        PrincipalId = principalId;
        CandidateId = candidateId;
        SelectionOrigin = selectionOrigin;
        OutcomeCode = outcomeCode;
        ReasonCodes = new ReadOnlyCollection<string>(reasons);
        DiagnosticFinding = diagnosticFinding;
        DiagnosticDisposition = diagnosticDisposition;
        ExecutionGateOutcome = executionGateOutcome;
        AuthorizationId = authorizationId;
        ExecutionCode = executionCode;
        Duration = duration;
        GoalId = goalId;
        StepId = stepId;
        DecisionOutcome = decisionOutcome;
        DecisionProviderId = decisionProviderId;
        DecisionProviderVersion = decisionProviderVersion;
        DecisionConfidence = decisionConfidence;
    }

    public Guid AuditId
    {
        get;
    }

    public AuditEventType EventType
    {
        get;
    }

    public RuntimeInvocationId InvocationId
    {
        get;
    }

    public CorrelationId CorrelationId
    {
        get;
    }

    public Guid TenantId
    {
        get;
    }

    public DateTimeOffset OccurredAt
    {
        get;
    }

    public ActionId? ActionId
    {
        get;
    }

    public ActionVersion? ActionVersion
    {
        get;
    }

    public string? PrincipalId
    {
        get;
    }

    public string? CandidateId
    {
        get;
    }

    public SelectionOrigin? SelectionOrigin
    {
        get;
    }

    public string? OutcomeCode
    {
        get;
    }

    public IReadOnlyList<string> ReasonCodes
    {
        get;
    }

    public AuditDiagnosticFinding? DiagnosticFinding
    {
        get;
    }

    public DiagnosticDisposition? DiagnosticDisposition
    {
        get;
    }

    public ExecutionGateOutcome? ExecutionGateOutcome
    {
        get;
    }

    public string? AuthorizationId
    {
        get;
    }

    public string? ExecutionCode
    {
        get;
    }

    public TimeSpan? Duration
    {
        get;
    }

    public GoalId? GoalId
    {
        get;
    }

    public StepId? StepId
    {
        get;
    }

    public DecisionOutcome? DecisionOutcome
    {
        get;
    }

    public string? DecisionProviderId
    {
        get;
    }

    public string? DecisionProviderVersion
    {
        get;
    }

    public double? DecisionConfidence
    {
        get;
    }
}
