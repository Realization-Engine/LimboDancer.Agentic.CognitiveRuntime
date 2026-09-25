namespace LimboDancer.Domains.Asl.Units;

public enum UnitDiagnosticSeverity
{
    Error,
    Warning,
}

/// <summary>
/// A finding about a vocabulary pack, a unit document, or a placement set (Unit Display Design, section 9). An error
/// refuses the pack or document; a warning does not. <see cref="Path"/> names the place, such as
/// <c>units[2].faces.front.firepower</c>.
/// </summary>
public sealed record UnitDiagnostic(string Code, UnitDiagnosticSeverity Severity, string Message, string? Path = null)
{
    public static UnitDiagnostic Error(string code, string message, string? path = null) => new(code, UnitDiagnosticSeverity.Error, message, path);

    public static UnitDiagnostic Warning(string code, string message, string? path = null) => new(code, UnitDiagnosticSeverity.Warning, message, path);

    public override string ToString() => Path is null ? $"{Code} {Message}" : $"{Code} {Path}: {Message}";
}
