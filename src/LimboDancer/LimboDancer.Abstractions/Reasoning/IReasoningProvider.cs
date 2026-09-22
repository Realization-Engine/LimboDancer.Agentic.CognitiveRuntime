namespace LimboDancer.Abstractions.Reasoning;

public interface IReasoningProvider
{
    public string ProviderId
    {
        get;
    }

    public string? ProviderVersion
    {
        get;
    }

    public Task<ReasoningResult> ReasonAsync(
        ReasoningContext context,
        CancellationToken cancellationToken = default);
}
