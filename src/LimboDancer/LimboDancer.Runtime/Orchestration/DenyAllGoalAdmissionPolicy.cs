using LimboDancer.Abstractions.Runtime;

namespace LimboDancer.Runtime.Orchestration;

public sealed class DenyAllGoalAdmissionPolicy : IGoalAdmissionPolicy
{
    public ValueTask<GoalAdmissionResult> AdmitAsync(
        Goal goal,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(goal);
        cancellationToken.ThrowIfCancellationRequested();
        return ValueTask.FromResult(new GoalAdmissionResult(
            admitted: false,
            "admission.policy_not_configured"));
    }
}
