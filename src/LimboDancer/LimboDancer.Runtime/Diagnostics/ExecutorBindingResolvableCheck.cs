using LimboDancer.Abstractions.Diagnostics;
using LimboDancer.Runtime.Actions;

namespace LimboDancer.Runtime.Diagnostics;

public sealed class ExecutorBindingResolvableCheck : IDiagnosticCheck<DiagnosticContext>
{
    private readonly IActionExecutorResolver executorResolver;
    private readonly TimeProvider timeProvider;

    public ExecutorBindingResolvableCheck(
        IActionExecutorResolver executorResolver,
        TimeProvider? timeProvider = null)
    {
        this.executorResolver = executorResolver ?? throw new ArgumentNullException(nameof(executorResolver));
        this.timeProvider = timeProvider ?? TimeProvider.System;
    }

    public DiagnosticCheckId Id => WellKnownDiagnosticChecks.ExecutorBindingResolvable;

    public string Version => WellKnownDiagnosticChecks.InitialVersion;

    public DiagnosticPosition Position => DiagnosticPosition.PreFlight;

    public Task<DiagnosticFinding> EvaluateAsync(
        DiagnosticContext context,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);
        cancellationToken.ThrowIfCancellationRequested();

        var descriptor = context.Descriptor;
        var resolved = descriptor is not null
            && executorResolver.TryResolve(descriptor.Executor, out var executor)
            && executor.ActionId == descriptor.Id;

        return Task.FromResult(new DiagnosticFinding(
            Id,
            Version,
            resolved ? DiagnosticOutcome.Pass : DiagnosticOutcome.Fail,
            resolved ? DiagnosticSeverity.Info : DiagnosticSeverity.Critical,
            resolved ? "executor.resolved" : "executor.unresolved",
            resolved
                ? "The action executor binding resolves to the matching runtime action."
                : "The action executor binding is missing or resolves to a different action.",
            timeProvider.GetUtcNow()));
    }
}
