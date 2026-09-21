namespace LimboDancer.Abstractions.State.Ontology;

public interface IOntologyResolver
{
    ValueTask<OntologyMapping?> ResolveAsync(
        TenantScope tenant,
        OntologyTermKind kind,
        string predicate,
        CancellationToken cancellationToken = default);

    ValueTask<OntologyMapping?> ResolveStorageNameAsync(
        TenantScope tenant,
        OntologyTermKind kind,
        string storageName,
        CancellationToken cancellationToken = default);
}
