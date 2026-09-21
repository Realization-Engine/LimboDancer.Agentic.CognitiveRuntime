using LimboDancer.Abstractions.Execution;

namespace LimboDancer.Runtime.Execution;

public interface IActionConstraintEvaluator
{
    public Task<ConstraintEvaluationResult> EvaluateAsync(
        SelectedAction action,
        LimboDancer.Abstractions.Execution.ExecutionContext context,
        CancellationToken cancellationToken = default);
}
