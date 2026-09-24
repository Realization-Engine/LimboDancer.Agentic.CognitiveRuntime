using LimboDancer.Abstractions.Reasoning;

namespace LimboDancer.Runtime.Reasoning;

public interface IReasoningEngine
{
    public Task<ReasoningEvaluation> ReasonAsync(
        ReasoningContext context,
        CancellationToken cancellationToken = default);
}
