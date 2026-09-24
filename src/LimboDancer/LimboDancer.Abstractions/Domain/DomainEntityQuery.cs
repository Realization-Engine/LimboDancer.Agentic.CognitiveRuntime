namespace LimboDancer.Abstractions.Domain;

public sealed class DomainEntityQuery
{
    public DomainEntityQuery(
        string queryId,
        Guid tenantId,
        DomainPackageRef package,
        SemanticIdentifier expectedKind,
        string reference)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(queryId);
        ArgumentOutOfRangeException.ThrowIfEqual(tenantId, Guid.Empty);
        ArgumentNullException.ThrowIfNull(package);
        ArgumentException.ThrowIfNullOrWhiteSpace(expectedKind.Value);
        if (expectedKind.DomainId != package.DomainId)
        {
            throw new ArgumentException("Expected entity kind and package must belong to the same domain.", nameof(expectedKind));
        }

        ArgumentException.ThrowIfNullOrWhiteSpace(reference);
        QueryId = queryId;
        TenantId = tenantId;
        Package = package;
        ExpectedKind = expectedKind;
        Reference = reference;
    }

    public string QueryId
    {
        get;
    }

    public Guid TenantId
    {
        get;
    }

    public DomainPackageRef Package
    {
        get;
    }

    public SemanticIdentifier ExpectedKind
    {
        get;
    }

    public string Reference
    {
        get;
    }
}
