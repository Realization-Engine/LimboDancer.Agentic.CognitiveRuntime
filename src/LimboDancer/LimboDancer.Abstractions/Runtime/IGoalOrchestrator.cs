namespace LimboDancer.Abstractions.Runtime;

public interface IGoalOrchestrator
{
    public Task<GoalResult> RunAsync(
        Goal goal,
        CancellationToken cancellationToken = default);
}
