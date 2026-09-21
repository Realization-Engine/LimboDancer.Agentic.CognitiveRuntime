using LimboDancer.Abstractions.Diagnostics;

namespace LimboDancer.Abstractions.Audit;

public sealed class AuditDiagnosticFinding
{
    public AuditDiagnosticFinding(
        DiagnosticCheckId checkId,
        string checkVersion,
        DiagnosticOutcome outcome,
        DiagnosticSeverity severity,
        string code)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(checkId.Value);
        ArgumentException.ThrowIfNullOrWhiteSpace(checkVersion);
        ArgumentException.ThrowIfNullOrWhiteSpace(code);

        CheckId = checkId;
        CheckVersion = checkVersion;
        Outcome = outcome;
        Severity = severity;
        Code = code;
    }

    public DiagnosticCheckId CheckId
    {
        get;
    }

    public string CheckVersion
    {
        get;
    }

    public DiagnosticOutcome Outcome
    {
        get;
    }

    public DiagnosticSeverity Severity
    {
        get;
    }

    public string Code
    {
        get;
    }
}
