using LimboDancer.Abstractions.Actions;
using LimboDancer.Abstractions.Decision;

namespace LimboDancer.Runtime.Decision;

public interface IDecisionPlane
{
    public Task<DecisionPlaneResult> DecideAsync(
        DecisionContext context,
        IReadOnlyList<PermittedAction> candidates,
        CancellationToken cancellationToken = default);
}
