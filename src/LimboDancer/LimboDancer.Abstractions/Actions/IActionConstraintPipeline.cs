namespace LimboDancer.Abstractions.Actions;

public interface IActionConstraintPipeline
{
    public Task<ConstraintPipelineResult> EvaluateAsync(
        IReadOnlyList<ActionCandidate> candidates,
        ConstraintContext context,
        CancellationToken cancellationToken = default);
}
