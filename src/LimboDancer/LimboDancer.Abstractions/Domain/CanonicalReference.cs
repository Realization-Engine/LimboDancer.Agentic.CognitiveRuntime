namespace LimboDancer.Abstractions.Domain;

public sealed record CanonicalReference
{
    public CanonicalReference(
        DomainPackageRef package,
        string sourceId,
        string elementId,
        string? version = null)
    {
        ArgumentNullException.ThrowIfNull(package);
        ArgumentException.ThrowIfNullOrWhiteSpace(sourceId);
        ArgumentException.ThrowIfNullOrWhiteSpace(elementId);
        if (version is not null)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(version);
        }

        Package = package;
        SourceId = sourceId;
        ElementId = elementId;
        Version = version;
    }

    public DomainPackageRef Package
    {
        get;
    }

    public string SourceId
    {
        get;
    }

    public string ElementId
    {
        get;
    }

    public string? Version
    {
        get;
    }
}
