using System.Collections.ObjectModel;

namespace LimboDancer.Abstractions.Observations;

public sealed class ObservationAcquisitionResult
{
    public ObservationAcquisitionResult(
        ObservationQuery query,
        IEnumerable<Observation> observations,
        IEnumerable<string>? reasonCodes = null)
    {
        ArgumentNullException.ThrowIfNull(query);
        ArgumentNullException.ThrowIfNull(observations);
        var observationValues = observations.ToArray();
        if (observationValues.Length > query.MaxResults)
        {
            throw new ArgumentException("Observation count exceeds the query bound.", nameof(observations));
        }

        if (observationValues.Any(item =>
                item is null
                || item.TenantId != query.TenantId
                || item.DomainPackage != query.Package))
        {
            throw new ArgumentException(
                "Observations must be non-null and match the query tenant and exact package.",
                nameof(observations));
        }

        var observationIds = observationValues.Select(static item => item.ObservationId).ToArray();
        if (observationIds.Distinct(StringComparer.Ordinal).Count() != observationIds.Length)
        {
            throw new ArgumentException("Observation identifiers must be unique.", nameof(observations));
        }

        var reasons = (reasonCodes ?? []).ToArray();
        if (reasons.Any(string.IsNullOrWhiteSpace))
        {
            throw new ArgumentException("Reason codes cannot be empty.", nameof(reasonCodes));
        }

        Query = query;
        Observations = new ReadOnlyCollection<Observation>(observationValues);
        ReasonCodes = new ReadOnlyCollection<string>(reasons);
    }

    public ObservationQuery Query
    {
        get;
    }

    public IReadOnlyList<Observation> Observations
    {
        get;
    }

    public IReadOnlyList<string> ReasonCodes
    {
        get;
    }
}
