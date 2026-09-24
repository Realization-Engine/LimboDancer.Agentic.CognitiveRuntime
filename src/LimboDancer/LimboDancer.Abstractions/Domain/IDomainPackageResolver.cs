namespace LimboDancer.Abstractions.Domain;

public interface IDomainPackageResolver
{
    public ValueTask<DomainPackageResolution> ResolveAsync(
        DomainPackageRef requested,
        CancellationToken cancellationToken = default);
}
