namespace LimboDancer.Abstractions.Domain;

public interface IDomainConclusionResolver
{
    public ValueTask<DomainConclusion> ConcludeAsync(
        DomainConclusionContext context,
        CancellationToken cancellationToken = default);
}
