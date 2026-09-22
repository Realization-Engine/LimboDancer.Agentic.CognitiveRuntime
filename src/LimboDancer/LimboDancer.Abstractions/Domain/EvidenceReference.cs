namespace LimboDancer.Abstractions.Domain;

public enum EvidenceKind
{
    Observation,
    CanonicalSource,
    Calculation,
}

public sealed record EvidenceReference
{
    public EvidenceReference(
        string evidenceId,
        EvidenceKind kind,
        Guid tenantId,
        DomainPackageRef package,
        string resourceId,
        string? version,
        string provenance)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(evidenceId);
        if (!Enum.IsDefined(kind))
        {
            throw new ArgumentOutOfRangeException(nameof(kind));
        }

        ArgumentOutOfRangeException.ThrowIfEqual(tenantId, Guid.Empty);
        ArgumentNullException.ThrowIfNull(package);
        ArgumentException.ThrowIfNullOrWhiteSpace(resourceId);
        if (version is not null)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(version);
        }

        ArgumentException.ThrowIfNullOrWhiteSpace(provenance);
        EvidenceId = evidenceId;
        Kind = kind;
        TenantId = tenantId;
        Package = package;
        ResourceId = resourceId;
        Version = version;
        Provenance = provenance;
    }

    public string EvidenceId
    {
        get;
    }

    public EvidenceKind Kind
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

    public string ResourceId
    {
        get;
    }

    public string? Version
    {
        get;
    }

    public string Provenance
    {
        get;
    }
}
