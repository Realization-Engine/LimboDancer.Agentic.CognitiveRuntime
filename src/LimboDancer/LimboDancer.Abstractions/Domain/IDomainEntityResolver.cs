namespace LimboDancer.Abstractions.Domain;

public interface IDomainEntityResolver
{
    public ValueTask<DomainEntityResolution> ResolveAsync(
        DomainEntityQuery query,
        CancellationToken cancellationToken = default);
}
