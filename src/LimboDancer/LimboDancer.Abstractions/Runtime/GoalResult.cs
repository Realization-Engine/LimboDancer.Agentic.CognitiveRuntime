using System.Text.Json;
using LimboDancer.Abstractions.Domain;

namespace LimboDancer.Abstractions.Runtime;

public sealed class GoalResult
{
    public GoalResult(
        GoalId goalId,
        GoalLifecycleState terminalState,
        TerminalReason reason,
        JsonElement? output = null,
        DomainConclusion? conclusion = null)
    {
        ArgumentOutOfRangeException.ThrowIfEqual(goalId.Value, Guid.Empty);
        ArgumentNullException.ThrowIfNull(reason);
        if (!IsTerminal(terminalState))
        {
            throw new ArgumentException("Goal results require a terminal lifecycle state.", nameof(terminalState));
        }

        if (output is not null && conclusion is not null)
        {
            throw new ArgumentException("A Goal result cannot contain both ordinary output and a domain conclusion.");
        }

        if (conclusion is not null)
        {
            var expectedState = conclusion.Disposition == ConclusionDisposition.Abstained
                ? GoalLifecycleState.Abstained
                : GoalLifecycleState.Completed;
            if (terminalState != expectedState)
            {
                throw new ArgumentException(
                    "The terminal lifecycle state does not match the conclusion disposition.",
                    nameof(terminalState));
            }
        }

        GoalId = goalId;
        TerminalState = terminalState;
        Reason = reason;
        Output = output?.Clone();
        Conclusion = conclusion;
    }

    public GoalId GoalId
    {
        get;
    }

    public GoalLifecycleState TerminalState
    {
        get;
    }

    public TerminalReason Reason
    {
        get;
    }

    public JsonElement? Output
    {
        get;
    }

    public DomainConclusion? Conclusion
    {
        get;
    }

    private static bool IsTerminal(GoalLifecycleState state) => state is
        GoalLifecycleState.Completed
        or GoalLifecycleState.Abstained
        or GoalLifecycleState.Escalated
        or GoalLifecycleState.Failed
        or GoalLifecycleState.Cancelled;
}
