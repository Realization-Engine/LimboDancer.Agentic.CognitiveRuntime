namespace LimboDancer.Abstractions.State.Ontology;

public interface IOntologyResolver
{
    public ValueTask<OntologyMapping?> ResolveAsync(
        TenantScope tenant,
        OntologyTermKind kind,
        string predicate,
        CancellationToken cancellationToken = default);

    public ValueTask<OntologyMapping?> ResolveStorageNameAsync(
        TenantScope tenant,
        OntologyTermKind kind,
        string storageName,
        CancellationToken cancellationToken = default);
}
