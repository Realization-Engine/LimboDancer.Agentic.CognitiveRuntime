using System.Collections.ObjectModel;

namespace LimboDancer.Abstractions.Decision;

public sealed class DecisionResult
{
    public DecisionResult(
        DecisionOutcome outcome,
        string? selectedCandidateId,
        string providerId,
        string reasonCode,
        double? confidence = null,
        IEnumerable<KeyValuePair<string, double>>? distribution = null,
        string? providerVersion = null,
        TimeSpan? latency = null,
        decimal? cost = null,
        DecisionTokenUsage? tokenUsage = null)
    {
        if (!Enum.IsDefined(outcome))
        {
            throw new ArgumentOutOfRangeException(nameof(outcome));
        }

        if (outcome == DecisionOutcome.Selected)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(selectedCandidateId);
        }
        else if (selectedCandidateId is not null)
        {
            throw new ArgumentException("Only a selected Decision may identify a candidate.", nameof(selectedCandidateId));
        }

        ArgumentException.ThrowIfNullOrWhiteSpace(providerId);
        ArgumentException.ThrowIfNullOrWhiteSpace(reasonCode);
        if (confidence is < 0 or > 1 || (confidence is { } confidenceValue && !double.IsFinite(confidenceValue)))
        {
            throw new ArgumentOutOfRangeException(nameof(confidence));
        }

        if (providerVersion is not null)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(providerVersion);
        }

        if (latency is { } latencyValue && latencyValue < TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(latency));
        }

        ArgumentOutOfRangeException.ThrowIfNegative(cost ?? 0);
        var distributionValues = new Dictionary<string, double>(StringComparer.Ordinal);
        foreach (var item in distribution ?? [])
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(item.Key);
            if (!double.IsFinite(item.Value) || item.Value is < 0 or > 1)
            {
                throw new ArgumentOutOfRangeException(nameof(distribution));
            }

            if (!distributionValues.TryAdd(item.Key, item.Value))
            {
                throw new ArgumentException($"Decision distribution candidate '{item.Key}' is duplicated.", nameof(distribution));
            }
        }

        Outcome = outcome;
        SelectedCandidateId = selectedCandidateId;
        ProviderId = providerId;
        ProviderVersion = providerVersion;
        ReasonCode = reasonCode;
        Confidence = confidence;
        Distribution = new ReadOnlyDictionary<string, double>(distributionValues);
        Latency = latency;
        Cost = cost;
        TokenUsage = tokenUsage;
    }

    public DecisionOutcome Outcome
    {
        get;
    }

    public string? SelectedCandidateId
    {
        get;
    }

    public double? Confidence
    {
        get;
    }

    public IReadOnlyDictionary<string, double> Distribution
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

    public TimeSpan? Latency
    {
        get;
    }

    public decimal? Cost
    {
        get;
    }

    public DecisionTokenUsage? TokenUsage
    {
        get;
    }
}
