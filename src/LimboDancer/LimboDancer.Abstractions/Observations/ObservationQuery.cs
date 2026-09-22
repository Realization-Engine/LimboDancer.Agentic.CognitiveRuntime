using System.Text.Json;
using LimboDancer.Abstractions.Domain;

namespace LimboDancer.Abstractions.Observations;

public sealed class ObservationQuery
{
    public ObservationQuery(
        string queryId,
        Guid tenantId,
        DomainPackageRef package,
        SemanticIdentifier kind,
        JsonElement parameters,
        int maxResults)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(queryId);
        ArgumentOutOfRangeException.ThrowIfEqual(tenantId, Guid.Empty);
        ArgumentNullException.ThrowIfNull(package);
        ArgumentException.ThrowIfNullOrWhiteSpace(kind.Value);
        if (kind.DomainId != package.DomainId)
        {
            throw new ArgumentException("Observation kind and package must belong to the same domain.", nameof(kind));
        }

        if (parameters.ValueKind != JsonValueKind.Object)
        {
            throw new ArgumentException("Observation query parameters must be a JSON object.", nameof(parameters));
        }

        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(maxResults);
        QueryId = queryId;
        TenantId = tenantId;
        Package = package;
        Kind = kind;
        Parameters = parameters.Clone();
        MaxResults = maxResults;
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

    public SemanticIdentifier Kind
    {
        get;
    }

    public JsonElement Parameters
    {
        get;
    }

    public int MaxResults
    {
        get;
    }
}
