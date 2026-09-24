namespace LimboDancer.Abstractions.Runtime;

public interface IGoalAdmissionPolicy
{
    public ValueTask<GoalAdmissionResult> AdmitAsync(
        Goal goal,
        CancellationToken cancellationToken = default);
}
