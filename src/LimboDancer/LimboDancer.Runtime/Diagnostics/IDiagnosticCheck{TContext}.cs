using LimboDancer.Abstractions.Diagnostics;

namespace LimboDancer.Runtime.Diagnostics;

public interface IDiagnosticCheck<in TContext> : IDiagnosticCheck
{
    public Task<DiagnosticFinding> EvaluateAsync(
        TContext context,
        CancellationToken cancellationToken = default);
}
