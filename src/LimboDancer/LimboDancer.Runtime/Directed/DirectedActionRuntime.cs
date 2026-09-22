using LimboDancer.Abstractions.Audit;
using LimboDancer.Abstractions.Actions;
using LimboDancer.Abstractions.Execution;
using LimboDancer.Runtime.Actions;
using LimboDancer.Runtime.Execution;
using RuntimeExecutionContext = LimboDancer.Abstractions.Execution.ExecutionContext;

namespace LimboDancer.Runtime.Directed;

public sealed class DirectedActionRuntime : IDirectedActionRuntime
{
    private readonly IActionBindingRegistry bindingRegistry;
    private readonly IActionRegistry actionRegistry;
    private readonly IActionExecutorResolver executorResolver;
    private readonly IExecutionGate executionGate;
    private readonly IAuditSink auditSink;
    private readonly TimeProvider timeProvider;

    public DirectedActionRuntime(
        IActionBindingRegistry bindingRegistry,
        IActionRegistry actionRegistry,
        IActionExecutorResolver executorResolver,
        IExecutionGate executionGate,
        IAuditSink auditSink,
        TimeProvider? timeProvider = null)
    {
        this.bindingRegistry = bindingRegistry ?? throw new ArgumentNullException(nameof(bindingRegistry));
        this.actionRegistry = actionRegistry ?? throw new ArgumentNullException(nameof(actionRegistry));
        this.executorResolver = executorResolver ?? throw new ArgumentNullException(nameof(executorResolver));
        this.executionGate = executionGate ?? throw new ArgumentNullException(nameof(executionGate));
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

    public async ValueTask<DirectedActionExecutionResult> ExecuteAsync(
        DirectedActionRequest request,
        RuntimeExecutionContext context,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(context);
        EnsureContextMatchesRequest(request, context);
        cancellationToken.ThrowIfCancellationRequested();

        var resolved = await ResolveAsync(request, cancellationToken).ConfigureAwait(false);
        if (!resolved.IsExecutable || resolved.Descriptor is null)
        {
            return new DirectedActionExecutionResult(
                resolved.Resolution,
                reasonCodes: [ResolutionReason(resolved.Resolution)]);
        }

        var selected = new SelectedAction(
            new ActionCandidate(
                Guid.NewGuid().ToString("N"),
                resolved.Descriptor,
                request.Arguments),
            SelectionOrigin.DirectedCaller);
        var gateResult = await executionGate
            .AuthorizeAsync(selected, context, cancellationToken)
            .ConfigureAwait(false);
        if (gateResult.Outcome != ExecutionGateOutcome.Authorized
            || gateResult.AuthorizedAction is null)
        {
            return new DirectedActionExecutionResult(
                resolved.Resolution,
                gateResult.Outcome,
                reasonCodes: gateResult.ReasonCodes);
        }

        if (!executorResolver.TryResolve(resolved.Descriptor.Executor, out var executor)
            || executor.ActionId != resolved.Descriptor.Id)
        {
            return new DirectedActionExecutionResult(
                DirectedActionResolution.MissingExecutor,
                reasonCodes: ["executor.unresolved"]);
        }

        var execution = await new AuditedActionExecutor(executor, auditSink, timeProvider)
            .ExecuteAsync(gateResult.AuthorizedAction, cancellationToken)
            .ConfigureAwait(false);
        return new DirectedActionExecutionResult(
            resolved.Resolution,
            gateResult.Outcome,
            execution,
            execution.Succeeded ? [] : [execution.Code]);
    }

    private static void EnsureContextMatchesRequest(
        DirectedActionRequest request,
        RuntimeExecutionContext context)
    {
        if (request.InvocationId != context.InvocationId
            || request.CorrelationId != context.CorrelationId
            || request.TenantId != context.TenantId)
        {
            throw new ArgumentException("The execution context identities must match the directed request.", nameof(context));
        }
    }

    private static string ResolutionReason(DirectedActionResolution resolution) => resolution switch
    {
        DirectedActionResolution.UnknownBinding => "binding.unknown",
        DirectedActionResolution.UnknownAction => "action.unknown",
        DirectedActionResolution.MissingExecutor => "executor.unresolved",
        _ => "resolution.invalid",
    };
}
