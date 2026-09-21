using System.Collections.Frozen;
using System.Text.Json;

namespace LimboDancer.Abstractions.Diagnostics;

public sealed class DiagnosticFinding
{
    public DiagnosticFinding(
        DiagnosticCheckId checkId,
        string checkVersion,
        DiagnosticOutcome outcome,
        DiagnosticSeverity severity,
        string code,
        string summary,
        DateTimeOffset timestamp,
        IEnumerable<KeyValuePair<string, JsonElement>>? evidence = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(checkId.Value);
        ArgumentException.ThrowIfNullOrWhiteSpace(checkVersion);
        ArgumentException.ThrowIfNullOrWhiteSpace(code);
        ArgumentException.ThrowIfNullOrWhiteSpace(summary);

        CheckId = checkId;
        CheckVersion = checkVersion;
        Outcome = outcome;
        Severity = severity;
        Code = code;
        Summary = summary;
        Timestamp = timestamp;
        Evidence = (evidence ?? [])
            .ToFrozenDictionary(
                static item => item.Key,
                static item => item.Value.Clone(),
                StringComparer.Ordinal);
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

    public string Summary
    {
        get;
    }

    public DateTimeOffset Timestamp
    {
        get;
    }

    public IReadOnlyDictionary<string, JsonElement> Evidence
    {
        get;
    }
}
