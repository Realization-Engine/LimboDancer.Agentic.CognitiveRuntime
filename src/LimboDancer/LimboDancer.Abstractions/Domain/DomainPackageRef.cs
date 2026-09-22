namespace LimboDancer.Abstractions.Domain;

public sealed record DomainPackageRef
{
    public DomainPackageRef(DomainId domainId, string packageId, string version)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(domainId.Value);
        ArgumentException.ThrowIfNullOrWhiteSpace(packageId);
        ArgumentException.ThrowIfNullOrWhiteSpace(version);
        DomainId = domainId;
        PackageId = packageId;
        Version = version;
    }

    public DomainId DomainId
    {
        get;
    }

    public string PackageId
    {
        get;
    }

    public string Version
    {
        get;
    }
}
