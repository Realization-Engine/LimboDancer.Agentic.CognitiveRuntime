using System.Collections.ObjectModel;

namespace LimboDancer.Abstractions.Actions;

public sealed class DiagnosticProfile
{
    public static DiagnosticProfile Empty { get; } = new([]);

    public DiagnosticProfile(IEnumerable<string> checkIds)
    {
        ArgumentNullException.ThrowIfNull(checkIds);

        var values = checkIds.ToArray();
        if (values.Any(string.IsNullOrWhiteSpace))
        {
            throw new ArgumentException("Diagnostic check identifiers cannot be empty.", nameof(checkIds));
        }

        CheckIds = new ReadOnlyCollection<string>(values);
    }

    public IReadOnlyList<string> CheckIds { get; }
}
