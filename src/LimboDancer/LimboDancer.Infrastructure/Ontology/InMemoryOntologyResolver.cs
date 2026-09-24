using LimboDancer.Abstractions.State;
using LimboDancer.Abstractions.State.Ontology;

namespace LimboDancer.Infrastructure.Ontology;

public sealed class InMemoryOntologyResolver : IOntologyResolver
{
    private readonly object syncRoot = new();
    private readonly Dictionary<MappingKey, OntologyMapping> byPredicate = [];
    private readonly Dictionary<MappingKey, OntologyMapping> byStorageName = [];

    public void Register(TenantScope tenant, OntologyMapping mapping)
    {
        ArgumentNullException.ThrowIfNull(mapping);
        ArgumentException.ThrowIfNullOrWhiteSpace(mapping.Predicate);
        ArgumentException.ThrowIfNullOrWhiteSpace(mapping.StorageName);
        tenant.ThrowIfInvalid();

        var predicateKey = new MappingKey(tenant.TenantId, mapping.Kind, mapping.Predicate);
        var storageKey = new MappingKey(tenant.TenantId, mapping.Kind, mapping.StorageName);
        lock (syncRoot)
        {
            if (byPredicate.ContainsKey(predicateKey) || byStorageName.ContainsKey(storageKey))
            {
                throw new InvalidOperationException("An ontology predicate or storage name is already registered in this tenant scope.");
            }

            byPredicate.Add(predicateKey, mapping);
            byStorageName.Add(storageKey, mapping);
        }
    }

    public ValueTask<OntologyMapping?> ResolveAsync(
        TenantScope tenant,
        OntologyTermKind kind,
        string predicate,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(predicate);
        tenant.ThrowIfInvalid();
        cancellationToken.ThrowIfCancellationRequested();
        lock (syncRoot)
        {
            byPredicate.TryGetValue(new MappingKey(tenant.TenantId, kind, predicate), out var mapping);
            return ValueTask.FromResult<OntologyMapping?>(mapping);
        }
    }

    public ValueTask<OntologyMapping?> ResolveStorageNameAsync(
        TenantScope tenant,
        OntologyTermKind kind,
        string storageName,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(storageName);
        tenant.ThrowIfInvalid();
        cancellationToken.ThrowIfCancellationRequested();
        lock (syncRoot)
        {
            byStorageName.TryGetValue(new MappingKey(tenant.TenantId, kind, storageName), out var mapping);
            return ValueTask.FromResult<OntologyMapping?>(mapping);
        }
    }

    private readonly record struct MappingKey(Guid TenantId, OntologyTermKind Kind, string Value);
}
