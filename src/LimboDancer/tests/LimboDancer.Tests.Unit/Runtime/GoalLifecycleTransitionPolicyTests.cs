using LimboDancer.Abstractions.Runtime;
using LimboDancer.Runtime.Orchestration;

namespace LimboDancer.Tests.Unit.Runtime;

public sealed class GoalLifecycleTransitionPolicyTests
{
    [Fact]
    public void AutonomousExecutionPathIsValid()
    {
        GoalLifecycleState[] path =
        [
            GoalLifecycleState.Admitted,
            GoalLifecycleState.Observing,
            GoalLifecycleState.Reasoning,
            GoalLifecycleState.Resolving,
            GoalLifecycleState.Constraining,
            GoalLifecycleState.Deciding,
            GoalLifecycleState.Gating,
            GoalLifecycleState.Executing,
            GoalLifecycleState.Verifying,
            GoalLifecycleState.Completed,
        ];

        foreach (var transition in path.Zip(path.Skip(1)))
        {
            Assert.True(GoalLifecycleTransitionPolicy.CanTransition(transition.First, transition.Second));
        }
    }

    [Theory]
    [InlineData(GoalLifecycleState.Resolving, GoalLifecycleState.Completed)]
    [InlineData(GoalLifecycleState.Resolving, GoalLifecycleState.Abstained)]
    [InlineData(GoalLifecycleState.Constraining, GoalLifecycleState.Abstained)]
    [InlineData(GoalLifecycleState.Deciding, GoalLifecycleState.Escalated)]
    [InlineData(GoalLifecycleState.Reasoning, GoalLifecycleState.Observing)]
    [InlineData(GoalLifecycleState.Reasoning, GoalLifecycleState.Completed)]
    [InlineData(GoalLifecycleState.Gating, GoalLifecycleState.AwaitingConfirmation)]
    [InlineData(GoalLifecycleState.Gating, GoalLifecycleState.Abstained)]
    [InlineData(GoalLifecycleState.AwaitingConfirmation, GoalLifecycleState.Gating)]
    [InlineData(GoalLifecycleState.AwaitingConfirmation, GoalLifecycleState.Escalated)]
    [InlineData(GoalLifecycleState.Verifying, GoalLifecycleState.Reasoning)]
    public void BoundedBranchesAreValid(GoalLifecycleState from, GoalLifecycleState to)
    {
        Assert.True(GoalLifecycleTransitionPolicy.CanTransition(from, to));
    }

    [Theory]
    [InlineData(GoalLifecycleState.Completed)]
    [InlineData(GoalLifecycleState.Abstained)]
    [InlineData(GoalLifecycleState.Escalated)]
    [InlineData(GoalLifecycleState.Failed)]
    [InlineData(GoalLifecycleState.Cancelled)]
    public void TerminalStatesHaveNoOutgoingTransitions(GoalLifecycleState terminalState)
    {
        Assert.All(
            Enum.GetValues<GoalLifecycleState>(),
            next => Assert.False(GoalLifecycleTransitionPolicy.CanTransition(terminalState, next)));
    }

    [Fact]
    public void InvalidTransitionFailsClosed()
    {
        Assert.False(GoalLifecycleTransitionPolicy.CanTransition(
            GoalLifecycleState.Observing,
            GoalLifecycleState.Executing));
        Assert.Throws<InvalidOperationException>(() => GoalLifecycleTransitionPolicy.EnsureTransition(
            GoalLifecycleState.Observing,
            GoalLifecycleState.Executing));
    }
}
