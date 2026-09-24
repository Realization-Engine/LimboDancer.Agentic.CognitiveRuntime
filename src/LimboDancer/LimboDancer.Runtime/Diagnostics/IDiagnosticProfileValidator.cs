using LimboDancer.Abstractions.Actions;

namespace LimboDancer.Runtime.Diagnostics;

public interface IDiagnosticProfileValidator
{
    public void EnsureRequiredChecksResolvable(DiagnosticProfile profile);
}
