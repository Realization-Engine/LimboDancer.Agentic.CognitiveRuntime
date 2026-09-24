using System.Collections.ObjectModel;
using LimboDancer.Abstractions.Domain;

namespace LimboDancer.Runtime.Domain;

public sealed class DomainPackageRegistry : IDomainPackageResolver
{
    private readonly ReadOnlyDictionary<DomainPackageRef, DomainPackageDescriptor> packages;

    public DomainPackageRegistry(IEnumerable<DomainPackageDescriptor> packages)
    {
        ArgumentNullException.ThrowIfNull(packages);
        var registered = new Dictionary<DomainPackageRef, DomainPackageDescriptor>();
        foreach (var package in packages)
        {
            ArgumentNullException.ThrowIfNull(package);
            if (!registered.TryAdd(package.Identity, package))
            {
                throw new InvalidOperationException(
                    $"Domain package '{package.Identity.PackageId}' version '{package.Identity.Version}' is already registered.");
            }
        }

        this.packages = new ReadOnlyDictionary<DomainPackageRef, DomainPackageDescriptor>(registered);
    }

    public ValueTask<DomainPackageResolution> ResolveAsync(
        DomainPackageRef requested,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(requested);
        cancellationToken.ThrowIfCancellationRequested();
        return packages.TryGetValue(requested, out var package)
            ? ValueTask.FromResult(new DomainPackageResolution(
                requested,
                DomainPackageResolutionOutcome.Resolved,
                package,
                "domain.package.resolved"))
            : ValueTask.FromResult(new DomainPackageResolution(
                requested,
                DomainPackageResolutionOutcome.Unavailable,
                null,
                "domain.package.unavailable"));
    }
}
