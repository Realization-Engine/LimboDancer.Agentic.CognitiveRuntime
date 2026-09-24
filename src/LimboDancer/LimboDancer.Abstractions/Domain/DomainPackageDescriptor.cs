using System.Collections.ObjectModel;

namespace LimboDancer.Abstractions.Domain;

public sealed class DomainPackageDescriptor
{
    public DomainPackageDescriptor(
        DomainPackageRef identity,
        IEnumerable<CanonicalReference> canonicalSources)
    {
        ArgumentNullException.ThrowIfNull(identity);
        ArgumentNullException.ThrowIfNull(canonicalSources);
        var sources = canonicalSources.ToArray();
        if (sources.Any(source => source is null || source.Package != identity))
        {
            throw new ArgumentException(
                "Canonical sources must be non-null and identify the exact package.",
                nameof(canonicalSources));
        }

        Identity = identity;
        CanonicalSources = new ReadOnlyCollection<CanonicalReference>(sources);
    }

    public DomainPackageRef Identity
    {
        get;
    }

    public IReadOnlyList<CanonicalReference> CanonicalSources
    {
        get;
    }
}
