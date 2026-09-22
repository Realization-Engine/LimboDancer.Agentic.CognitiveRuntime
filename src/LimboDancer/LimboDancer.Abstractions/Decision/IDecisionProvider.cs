using LimboDancer.Abstractions.Actions;

namespace LimboDancer.Abstractions.Decision;

public interface IDecisionProvider
{
    public string ProviderId
    {
        get;
    }

    public string? ProviderVersion
    {
        get;
    }

    public Task<DecisionResult> DecideAsync(
        DecisionContext context,
        IReadOnlyList<PermittedAction> candidates,
        CancellationToken cancellationToken = default);
}
