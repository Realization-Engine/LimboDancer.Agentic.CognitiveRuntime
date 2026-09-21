using LimboDancer.Abstractions.Audit;
using LimboDancer.Runtime.Actions;

namespace LimboDancer.Runtime.Directed;

public sealed class DirectedActionRuntime : IDirectedActionRuntime
{
    private readonly IActionBindingRegistry bindingRegistry;
    private readonly IActionRegistry actionRegistry;
    private readonly IActionExecutorResolver executorResolver;
    private readonly IAuditSink auditSink;
    private readonly TimeProvider timeProvider;

    public DirectedActionRuntime(
        IActionBindingRegistry bindingRegistry,
        IActionRegistry actionRegistry,
        IActionExecutorResolver executorResolver,
        IAuditSink auditSink,
        TimeProvider? timeProvider = null)
    {
        this.bindingRegistry = bindingRegistry ?? throw new ArgumentNullException(nameof(bindingRegistry));
        this.actionRegistry = actionRegistry ?? throw new ArgumentNullException(nameof(actionRegistry));
        this.executorResolver = executorResolver ?? throw new ArgumentNullException(nameof(executorResolver));
        this.auditSink = auditSink ?? throw new ArgumentNullException(nameof(auditSink));
        this.timeProvider = timeProvider ?? TimeProvider.System;
    }

    public async ValueTask<DirectedActionResult> ResolveAsync(
        DirectedActionRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        cancellationToken.ThrowIfCancellationRequested();
        await auditSink.WriteAsync(
                new RuntimeAuditEvent(
                    Guid.NewGuid(),
                    AuditEventType.InvocationAdmitted,
                    request.InvocationId,
                    request.CorrelationId,
                    request.TenantId,
                    timeProvider.GetUtcNow()),
                cancellationToken)
            .ConfigureAwait(false);

        if (!bindingRegistry.TryResolve(request.Protocol, request.ExternalActionName, out var binding))
        {
            return new DirectedActionResult(DirectedActionResolution.UnknownBinding, null, null);
        }

        if (!actionRegistry.TryGet(binding.ActionId, binding.Version, out var descriptor))
        {
            return new DirectedActionResult(DirectedActionResolution.UnknownAction, binding, null);
        }

        await auditSink.WriteAsync(
                new RuntimeAuditEvent(
                    Guid.NewGuid(),
                    AuditEventType.ActionResolved,
                    request.InvocationId,
                    request.CorrelationId,
                    request.TenantId,
                    timeProvider.GetUtcNow(),
                    descriptor.Id,
                    descriptor.Version,
                    outcomeCode: DirectedActionResolution.Resolved.ToString()),
                cancellationToken)
            .ConfigureAwait(false);

        if (!executorResolver.TryResolve(descriptor.Executor, out var executor)
            || executor.ActionId != descriptor.Id)
        {
            return new DirectedActionResult(DirectedActionResolution.MissingExecutor, binding, descriptor);
        }

        return new DirectedActionResult(DirectedActionResolution.Resolved, binding, descriptor);
    }
}
