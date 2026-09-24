using LimboDancer.Abstractions.Diagnostics;

namespace LimboDancer.Runtime.Diagnostics;

public sealed class DiagnosticPolicy : IDiagnosticPolicy
{
    public DiagnosticDisposition Evaluate(
        DiagnosticFinding finding,
        DiagnosticPolicyContext context)
    {
        ArgumentNullException.ThrowIfNull(finding);
        ArgumentNullException.ThrowIfNull(context);

        if (context.Reference.IsHardInvariant
            && finding.Outcome is DiagnosticOutcome.Fail or DiagnosticOutcome.Indeterminate)
        {
            return DiagnosticDisposition.Block;
        }

        return finding.Outcome switch
        {
            DiagnosticOutcome.Pass => DiagnosticDisposition.Continue,
            DiagnosticOutcome.Fail when finding.Severity is DiagnosticSeverity.Error or DiagnosticSeverity.Critical =>
                DiagnosticDisposition.Block,
            DiagnosticOutcome.Indeterminate when finding.Severity is DiagnosticSeverity.Error or DiagnosticSeverity.Critical =>
                DiagnosticDisposition.Escalate,
            _ => DiagnosticDisposition.ContinueDegraded,
        };
    }
}
