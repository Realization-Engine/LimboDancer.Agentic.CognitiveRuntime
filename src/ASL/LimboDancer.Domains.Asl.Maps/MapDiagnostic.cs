namespace LimboDancer.Domains.Asl.Maps;

public enum MapDiagnosticSeverity
{
    Info,
    Warning,
    Error,
}

/// <summary>A coded finding from ingestion, validation, or rendering, such as <c>VASL-CAT-001</c>.</summary>
public sealed record MapDiagnostic(string Code, MapDiagnosticSeverity Severity, string Message);

/// <summary>
/// The outcome of one named fidelity check on a board, such as outline losslessness or a render hash. A gating check
/// decides whether the board is verified; an informational one, such as F3, is only reported.
/// </summary>
public sealed record FidelityCheck(string Name, bool Passed, string Detail, bool Gating = true);
