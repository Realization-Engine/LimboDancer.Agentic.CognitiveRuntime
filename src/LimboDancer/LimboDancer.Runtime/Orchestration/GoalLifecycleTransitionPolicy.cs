using LimboDancer.Abstractions.Runtime;

namespace LimboDancer.Runtime.Orchestration;

public static class GoalLifecycleTransitionPolicy
{
    public static bool CanTransition(GoalLifecycleState from, GoalLifecycleState to)
    {
        if (!Enum.IsDefined(from))
        {
            throw new ArgumentOutOfRangeException(nameof(from));
        }

        if (!Enum.IsDefined(to))
        {
            throw new ArgumentOutOfRangeException(nameof(to));
        }

        if (IsTerminal(from))
        {
            return false;
        }

        if (to is GoalLifecycleState.Failed or GoalLifecycleState.Cancelled)
        {
            return true;
        }

        return from switch
        {
            GoalLifecycleState.Admitted => to == GoalLifecycleState.Observing,
            GoalLifecycleState.Observing => to == GoalLifecycleState.Reasoning,
            GoalLifecycleState.Reasoning => to is GoalLifecycleState.Observing
                or GoalLifecycleState.Resolving
                or GoalLifecycleState.Completed
                or GoalLifecycleState.Abstained,
            GoalLifecycleState.Resolving => to is GoalLifecycleState.Constraining
                or GoalLifecycleState.Completed
                or GoalLifecycleState.Abstained,
            GoalLifecycleState.Constraining => to is GoalLifecycleState.Deciding
                or GoalLifecycleState.Abstained,
            GoalLifecycleState.Deciding => to is GoalLifecycleState.Gating
                or GoalLifecycleState.Abstained
                or GoalLifecycleState.Escalated,
            GoalLifecycleState.Gating => to is GoalLifecycleState.Executing
                or GoalLifecycleState.AwaitingConfirmation
                or GoalLifecycleState.Abstained
                or GoalLifecycleState.Escalated
                or GoalLifecycleState.Observing,
            GoalLifecycleState.AwaitingConfirmation => to is GoalLifecycleState.Gating
                or GoalLifecycleState.Abstained
                or GoalLifecycleState.Escalated,
            GoalLifecycleState.Executing => to == GoalLifecycleState.Verifying,
            GoalLifecycleState.Verifying => to is GoalLifecycleState.Completed
                or GoalLifecycleState.Reasoning
                or GoalLifecycleState.Escalated,
            _ => false,
        };
    }

    public static void EnsureTransition(GoalLifecycleState from, GoalLifecycleState to)
    {
        if (!CanTransition(from, to))
        {
            throw new InvalidOperationException($"Goal lifecycle transition '{from}' to '{to}' is not permitted.");
        }
    }

    private static bool IsTerminal(GoalLifecycleState state) => state is
        GoalLifecycleState.Completed
        or GoalLifecycleState.Abstained
        or GoalLifecycleState.Escalated
        or GoalLifecycleState.Failed
        or GoalLifecycleState.Cancelled;
}
