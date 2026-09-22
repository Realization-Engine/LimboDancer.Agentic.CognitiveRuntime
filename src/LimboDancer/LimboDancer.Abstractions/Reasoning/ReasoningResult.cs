using System.Collections.ObjectModel;
using System.Text.Json;
using LimboDancer.Abstractions.Actions;
using LimboDancer.Abstractions.Observations;

namespace LimboDancer.Abstractions.Reasoning;

public sealed class ReasoningResult
{
    public ReasoningResult(
        ReasoningDisposition disposition,
        string providerId,
        string reasonCode,
        SemanticActionIntent? intent = null,
        IEnumerable<ObservationQuery>? observationRequests = null,
        JsonElement? output = null,
        string? providerVersion = null)
    {
        if (!Enum.IsDefined(disposition))
        {
            throw new ArgumentOutOfRangeException(nameof(disposition));
        }

        ArgumentException.ThrowIfNullOrWhiteSpace(providerId);
        ArgumentException.ThrowIfNullOrWhiteSpace(reasonCode);
        if (providerVersion is not null)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(providerVersion);
        }

        var requests = (observationRequests ?? []).ToArray();
        if (requests.Any(static request => request is null))
        {
            throw new ArgumentException("Observation requests cannot contain null values.", nameof(observationRequests));
        }

        var validShape = disposition switch
        {
            ReasoningDisposition.ProposedAction => intent is not null && requests.Length == 0 && output is null,
            ReasoningDisposition.ObservationRequired => intent is null && requests.Length != 0 && output is null,
            ReasoningDisposition.Completed => intent is null && requests.Length == 0 && output is not null,
            ReasoningDisposition.Abstained => intent is null && requests.Length == 0 && output is null,
            _ => false,
        };
        if (!validShape)
        {
            throw new ArgumentException("Reasoning result payload does not match its disposition.");
        }

        Disposition = disposition;
        ProviderId = providerId;
        ProviderVersion = providerVersion;
        ReasonCode = reasonCode;
        Intent = intent;
        ObservationRequests = new ReadOnlyCollection<ObservationQuery>(requests);
        Output = output?.Clone();
    }

    public ReasoningDisposition Disposition
    {
        get;
    }

    public string ProviderId
    {
        get;
    }

    public string? ProviderVersion
    {
        get;
    }

    public string ReasonCode
    {
        get;
    }

    public SemanticActionIntent? Intent
    {
        get;
    }

    public IReadOnlyList<ObservationQuery> ObservationRequests
    {
        get;
    }

    public JsonElement? Output
    {
        get;
    }
}
