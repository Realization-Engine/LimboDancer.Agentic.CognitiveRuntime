using System.Collections.ObjectModel;
using LimboDancer.Abstractions.Diagnostics;

namespace LimboDancer.Abstractions.Actions;

public sealed class DiagnosticProfile
{
    public static DiagnosticProfile Empty
    {
        get;
    } = new([]);

    public DiagnosticProfile(IEnumerable<DiagnosticReference> checks)
    {
        ArgumentNullException.ThrowIfNull(checks);

        var values = checks.ToArray();
        if (values.Any(static check => check is null))
        {
            throw new ArgumentException("Diagnostic references cannot be null.", nameof(checks));
        }

        Checks = new ReadOnlyCollection<DiagnosticReference>(values);
    }

    public IReadOnlyList<DiagnosticReference> Checks
    {
        get;
    }
}
