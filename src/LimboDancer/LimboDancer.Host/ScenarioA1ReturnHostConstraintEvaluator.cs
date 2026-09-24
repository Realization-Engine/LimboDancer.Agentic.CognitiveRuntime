using LimboDancer.Abstractions.Execution;
using LimboDancer.Domains.Asl.Execution;
using LimboDancer.Domains.Asl.ScenarioA1;
using LimboDancer.Runtime.Execution;
using RuntimeExecutionContext = LimboDancer.Abstractions.Execution.ExecutionContext;

namespace LimboDancer.Host;

/// <summary>Preserves the host's existing fail-closed behavior for every other action.</summary>
internal sealed class ScenarioA1ReturnHostConstraintEvaluator(
    IScenarioA1ReturnStateStore store, IScenarioA1ReturnConclusionSource conclusions)
    : IActionConstraintEvaluator
{
    private readonly ScenarioA1ReturnConstraintEvaluator asl = new(store, conclusions);
    private readonly FailClosedActionConstraintEvaluator fallback = new();

    public Task<ConstraintEvaluationResult> EvaluateAsync(SelectedAction action,
        RuntimeExecutionContext context, CancellationToken cancellationToken = default) =>
        action.Candidate.Descriptor.Id == ScenarioA1ReturnAction.Id
            ? asl.EvaluateAsync(action, context, cancellationToken)
            : fallback.EvaluateAsync(action, context, cancellationToken);
}
