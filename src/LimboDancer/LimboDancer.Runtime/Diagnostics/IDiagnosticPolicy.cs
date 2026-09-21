using LimboDancer.Abstractions.Diagnostics;

namespace LimboDancer.Runtime.Diagnostics;

public interface IDiagnosticPolicy
{
    public DiagnosticDisposition Evaluate(
        DiagnosticFinding finding,
        DiagnosticPolicyContext context);
}
