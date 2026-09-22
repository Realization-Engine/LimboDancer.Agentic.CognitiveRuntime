using LimboDancer.Abstractions.Actions;
using LimboDancer.Abstractions.Observations;
using LimboDancer.Tests.Unit.Runtime;

namespace LimboDancer.Tests.Unit.Actions;

public sealed class AutonomousActionContractsTests
{
    [Fact]
    public void PermittedActionRequiresPassingResultsForItsCandidate()
    {
        var candidate = CreateCandidate("candidate-1");
        var passed = CreateConstraint("candidate-1", ConstraintOutcome.Passed);

        var permitted = new PermittedAction(candidate, [passed]);

        Assert.Same(candidate, permitted.Candidate);
        Assert.Throws<ArgumentException>(() => new PermittedAction(
            candidate,
            [CreateConstraint("candidate-1", ConstraintOutcome.Failed)]));
        Assert.Throws<ArgumentException>(() => new PermittedAction(
            candidate,
            [CreateConstraint("candidate-2", ConstraintOutcome.Passed)]));
    }

    [Fact]
    public void RejectedCandidateRequiresBlockingResult()
    {
        var candidate = CreateCandidate("candidate-1");

        Assert.Throws<ArgumentException>(() => new RejectedCandidate(
            candidate,
            [CreateConstraint("candidate-1", ConstraintOutcome.Passed)]));

        var rejected = new RejectedCandidate(
            candidate,
            [CreateConstraint("candidate-1", ConstraintOutcome.Indeterminate)]);
        Assert.Same(candidate, rejected.Candidate);
    }

    [Fact]
    public void PipelineRejectsDuplicateCandidateIdentity()
    {
        var candidate = CreateCandidate("candidate-1");
        var permitted = new PermittedAction(
            candidate,
            [CreateConstraint("candidate-1", ConstraintOutcome.Passed)]);
        var rejected = new RejectedCandidate(
            candidate,
            [CreateConstraint("candidate-1", ConstraintOutcome.Failed)]);

        Assert.Throws<ArgumentException>(() => new ConstraintPipelineResult([permitted], [rejected]));
    }

    [Fact]
    public void ResolutionContextRejectsCrossTenantObservation()
    {
        var goal = GoalContractsTests.CreateGoal();
        var observation = new Observation(
            "observation-1",
            new ObservationSource("test"),
            Guid.NewGuid(),
            DateTimeOffset.UtcNow,
            GoalContractsTests.ParseJson("{}"));

        Assert.Throws<ArgumentException>(() => new ActionResolutionContext(
            goal,
            LimboDancer.Abstractions.Runtime.StepId.New(),
            [observation]));
    }

    internal static ActionCandidate CreateCandidate(string candidateId) => new(
        candidateId,
        ActionRegistryTests.CreateDescriptor(),
        GoalContractsTests.ParseJson("{}"));

    internal static ConstraintResult CreateConstraint(
        string candidateId,
        ConstraintOutcome outcome) => new(
            candidateId,
            "constraint-1",
            ConstraintAuthorityClass.Semantic,
            outcome,
            $"constraint.{outcome.ToString().ToLowerInvariant()}");
}
