using LimboDancer.Abstractions.Diagnostics;

namespace LimboDancer.Runtime.Diagnostics;

public sealed class TenantContextPresentCheck : IDiagnosticCheck<DiagnosticContext>
{
    private readonly TimeProvider timeProvider;

    public TenantContextPresentCheck(TimeProvider? timeProvider = null)
    {
        this.timeProvider = timeProvider ?? TimeProvider.System;
    }

    public DiagnosticCheckId Id => WellKnownDiagnosticChecks.TenantContextPresent;

    public string Version => WellKnownDiagnosticChecks.InitialVersion;

    public DiagnosticPosition Position => DiagnosticPosition.PreFlight;

    public Task<DiagnosticFinding> EvaluateAsync(
        DiagnosticContext context,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);
        cancellationToken.ThrowIfCancellationRequested();

        var present = context.TenantId != Guid.Empty;
        return Task.FromResult(new DiagnosticFinding(
            Id,
            Version,
            present ? DiagnosticOutcome.Pass : DiagnosticOutcome.Fail,
            present ? DiagnosticSeverity.Info : DiagnosticSeverity.Critical,
            present ? "tenant.present" : "tenant.missing",
            present ? "Tenant context is present." : "Tenant context is missing.",
            timeProvider.GetUtcNow()));
    }
}
