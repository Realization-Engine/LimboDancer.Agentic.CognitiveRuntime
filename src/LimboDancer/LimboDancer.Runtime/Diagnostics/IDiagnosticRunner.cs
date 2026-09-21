using LimboDancer.Abstractions.Actions;
using LimboDancer.Abstractions.Diagnostics;

namespace LimboDancer.Runtime.Diagnostics;

public interface IDiagnosticRunner
{
    public Task<IReadOnlyList<DiagnosticFinding>> RunAsync(
        DiagnosticContext context,
        DiagnosticProfile profile,
        CancellationToken cancellationToken = default);
}
