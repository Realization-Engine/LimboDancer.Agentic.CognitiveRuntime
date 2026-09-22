using LimboDancer.Abstractions.Execution;

namespace LimboDancer.Runtime.Execution;

/// <summary>
/// Authorizes descriptors that declare no preconditions and fails closed until a
/// concrete evaluator is registered for descriptors that do.
/// </summary>
public sealed class FailClosedActionConstraintEvaluator : IActionConstraintEvaluator
{
    public Task<ConstraintEvaluationResult> EvaluateAsync(
        SelectedAction action,
        LimboDancer.Abstractions.Execution.ExecutionContext context,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(action);
        ArgumentNullException.ThrowIfNull(context);
        cancellationToken.ThrowIfCancellationRequested();

        return Task.FromResult(action.Candidate.Descriptor.Preconditions.Count == 0
            ? new ConstraintEvaluationResult(ConstraintEvaluationOutcome.Satisfied)
            : new ConstraintEvaluationResult(
                ConstraintEvaluationOutcome.Indeterminate,
                ["precondition.evaluator_unavailable"]));
    }
}
