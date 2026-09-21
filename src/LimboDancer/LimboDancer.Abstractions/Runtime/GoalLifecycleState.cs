namespace LimboDancer.Abstractions.Runtime;

public enum GoalLifecycleState
{
    Admitted,
    Observing,
    Reasoning,
    Resolving,
    Constraining,
    Deciding,
    AwaitingConfirmation,
    Gating,
    Executing,
    Verifying,
    Completed,
    Abstained,
    Escalated,
    Failed,
    Cancelled,
}
