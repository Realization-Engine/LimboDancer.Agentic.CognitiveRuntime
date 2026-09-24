using LimboDancer.Abstractions.Audit;
using LimboDancer.Abstractions.Actions;
using LimboDancer.Abstractions.Execution;

namespace LimboDancer.Runtime.Execution;

public sealed class AuditedActionExecutor : IActionExecutor
{
    private readonly IActionExecutor inner;
    private readonly IAuditSink auditSink;
    private readonly TimeProvider timeProvider;

    public AuditedActionExecutor(
        IActionExecutor inner,
        IAuditSink auditSink,
        TimeProvider? timeProvider = null)
    {
        this.inner = inner ?? throw new ArgumentNullException(nameof(inner));
        this.auditSink = auditSink ?? throw new ArgumentNullException(nameof(auditSink));
        this.timeProvider = timeProvider ?? TimeProvider.System;
    }

    public ActionId ActionId => inner.ActionId;

    public ExecutorBinding Binding => inner.Binding;

    public async Task<ActionExecutionResult> ExecuteAsync(
        AuthorizedAction action,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(action);
        cancellationToken.ThrowIfCancellationRequested();

        var startedAt = timeProvider.GetUtcNow();
        var startedTimestamp = timeProvider.GetTimestamp();
        await auditSink.WriteAsync(
                CreateAuditEvent(
                    AuditEventType.ExecutorStarted,
                    action,
                    startedAt,
                    outcomeCode: "Started"),
                cancellationToken)
            .ConfigureAwait(false);

        ActionExecutionResult? result;
        try
        {
            result = await inner.ExecuteAsync(action, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception exception)
        {
            await auditSink.WriteAsync(
                    CreateAuditEvent(
                        AuditEventType.ExecutorFailed,
                        action,
                        timeProvider.GetUtcNow(),
                        outcomeCode: exception is OperationCanceledException ? "Cancelled" : "Exception",
                        duration: timeProvider.GetElapsedTime(startedTimestamp)),
                    CancellationToken.None)
                .ConfigureAwait(false);
            throw;
        }

        if (result is null)
        {
            await auditSink.WriteAsync(
                    CreateAuditEvent(
                        AuditEventType.ExecutorFailed,
                        action,
                        timeProvider.GetUtcNow(),
                        outcomeCode: "InvalidResult",
                        duration: timeProvider.GetElapsedTime(startedTimestamp)),
                    CancellationToken.None)
                .ConfigureAwait(false);
            throw new InvalidOperationException("The action executor returned no result.");
        }

        await auditSink.WriteAsync(
                CreateAuditEvent(
                    result.Succeeded
                        ? AuditEventType.ExecutorCompleted
                        : AuditEventType.ExecutorFailed,
                    action,
                    timeProvider.GetUtcNow(),
                    outcomeCode: result.Succeeded ? "Succeeded" : "Failed",
                    executionCode: result.Code,
                    duration: timeProvider.GetElapsedTime(startedTimestamp)),
                CancellationToken.None)
            .ConfigureAwait(false);
        return result;
    }

    private static RuntimeAuditEvent CreateAuditEvent(
        AuditEventType eventType,
        AuthorizedAction action,
        DateTimeOffset occurredAt,
        string outcomeCode,
        string? executionCode = null,
        TimeSpan? duration = null) => new(
            Guid.NewGuid(),
            eventType,
            action.InvocationId,
            action.CorrelationId,
            action.TenantId,
            occurredAt,
            action.Selected.Candidate.Descriptor.Id,
            action.Selected.Candidate.Descriptor.Version,
            action.PrincipalId,
            action.Selected.Candidate.CandidateId,
            action.Selected.Origin,
            outcomeCode,
            authorizationId: action.AuthorizationId,
            executionCode: executionCode,
            duration: duration);
}
