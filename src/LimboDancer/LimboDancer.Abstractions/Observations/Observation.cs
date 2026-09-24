using System.Text.Json;
using LimboDancer.Abstractions.Domain;

namespace LimboDancer.Abstractions.Observations;

public sealed class Observation
{
    public Observation(
        string observationId,
        ObservationSource source,
        Guid tenantId,
        DateTimeOffset observedAt,
        JsonElement data,
        string? resourceId = null,
        string? version = null,
        string? provenance = null,
        DomainPackageRef? domainPackage = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(observationId);
        ArgumentNullException.ThrowIfNull(source);
        ArgumentOutOfRangeException.ThrowIfEqual(tenantId, Guid.Empty);
        ValidateOptional(resourceId, nameof(resourceId));
        ValidateOptional(version, nameof(version));
        ValidateOptional(provenance, nameof(provenance));
        if (data.ValueKind == JsonValueKind.Undefined)
        {
            throw new ArgumentException("Observation data must be defined.", nameof(data));
        }

        ObservationId = observationId;
        Source = source;
        TenantId = tenantId;
        ObservedAt = observedAt;
        ResourceId = resourceId;
        Version = version;
        Data = data.Clone();
        Provenance = provenance;
        DomainPackage = domainPackage;
    }

    public string ObservationId
    {
        get;
    }

    public ObservationSource Source
    {
        get;
    }

    public Guid TenantId
    {
        get;
    }

    public DateTimeOffset ObservedAt
    {
        get;
    }

    public string? ResourceId
    {
        get;
    }

    public string? Version
    {
        get;
    }

    public JsonElement Data
    {
        get;
    }

    public string? Provenance
    {
        get;
    }

    public DomainPackageRef? DomainPackage
    {
        get;
    }

    private static void ValidateOptional(string? value, string parameterName)
    {
        if (value is not null)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(value, parameterName);
        }
    }
}
