using LimboDancer.Domains.Asl.Maps;

namespace LimboDancer.Domains.Asl.MapStudio.Services;

/// <summary>Diagnostics of one code and severity, counted, with their messages for a tooltip.</summary>
public sealed record DiagnosticGroup(string Code, MapDiagnosticSeverity Severity, int Count, IReadOnlyList<string> Messages)
{
    public string Label => (Count > 1 ? $"{Count} × " : string.Empty) + $"{Code} ({Severity.ToString().ToLowerInvariant()})";
}

public static class DiagnosticSummary
{
    /// <summary>Groups diagnostics by code and severity, most severe first, then by code.</summary>
    public static IReadOnlyList<DiagnosticGroup> Group(IEnumerable<MapDiagnostic> diagnostics)
    {
        ArgumentNullException.ThrowIfNull(diagnostics);
        return diagnostics
            .GroupBy(diagnostic => (diagnostic.Code, diagnostic.Severity))
            .Select(group => new DiagnosticGroup(group.Key.Code, group.Key.Severity, group.Count(), group.Select(diagnostic => diagnostic.Message).ToArray()))
            .OrderByDescending(group => group.Severity)
            .ThenBy(group => group.Code, StringComparer.Ordinal)
            .ToArray();
    }
}
