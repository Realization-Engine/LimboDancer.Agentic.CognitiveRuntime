using LimboDancer.Abstractions.Diagnostics;
using LimboDancer.Runtime.Actions;

namespace LimboDancer.Runtime.Diagnostics;

public sealed class ActionDescriptorCurrentCheck : IDiagnosticCheck<DiagnosticContext>
{
    private readonly IActionRegistry actionRegistry;
    private readonly TimeProvider timeProvider;

    public ActionDescriptorCurrentCheck(
        IActionRegistry actionRegistry,
        TimeProvider? timeProvider = null)
    {
        this.actionRegistry = actionRegistry ?? throw new ArgumentNullException(nameof(actionRegistry));
        this.timeProvider = timeProvider ?? TimeProvider.System;
    }

    public DiagnosticCheckId Id => WellKnownDiagnosticChecks.ActionDescriptorCurrent;

    public string Version => WellKnownDiagnosticChecks.InitialVersion;

    public DiagnosticPosition Position => DiagnosticPosition.PreFlight;

    public Task<DiagnosticFinding> EvaluateAsync(
        DiagnosticContext context,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);
        cancellationToken.ThrowIfCancellationRequested();

        var descriptor = context.Descriptor;
        var current = descriptor is not null
            && actionRegistry.TryGet(descriptor.Id, descriptor.Version, out var registered)
            && ReferenceEquals(descriptor, registered);

        return Task.FromResult(new DiagnosticFinding(
            Id,
            Version,
            current ? DiagnosticOutcome.Pass : DiagnosticOutcome.Fail,
            current ? DiagnosticSeverity.Info : DiagnosticSeverity.Critical,
            current ? "descriptor.current" : "descriptor.not_current",
            current
                ? "The action descriptor is the currently registered authoritative instance."
                : "The action descriptor is missing, unregistered, or not authoritative.",
            timeProvider.GetUtcNow()));
    }
}
