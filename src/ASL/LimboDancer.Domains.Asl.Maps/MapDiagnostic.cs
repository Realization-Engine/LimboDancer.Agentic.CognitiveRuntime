namespace LimboDancer.Domains.Asl.Maps;

public enum MapDiagnosticSeverity
{
    Info,
    Warning,
    Error,
}

/// <summary>A coded finding from ingestion, validation, or rendering, such as <c>VASL-CAT-001</c>.</summary>
public sealed record MapDiagnostic(string Code, MapDiagnosticSeverity Severity, string Message);
