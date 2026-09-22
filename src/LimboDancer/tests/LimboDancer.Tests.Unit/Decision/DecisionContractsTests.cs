using LimboDancer.Abstractions.Actions;
using LimboDancer.Abstractions.Decision;
using LimboDancer.Abstractions.Execution;
using LimboDancer.Runtime.Decision;
using LimboDancer.Tests.Unit.Actions;

namespace LimboDancer.Tests.Unit.Decision;

public sealed class DecisionContractsTests
{
    [Fact]
    public void ProviderSelectionMustBelongToPermittedSet()
    {
        var permitted = CreatePermitted("candidate-1");
        var result = Selected("candidate-2");

        var exception = Assert.Throws<InvalidOperationException>(
            () => DecisionResultValidator.Validate(result, [permitted]));

        Assert.Contains("unknown candidate", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void ProviderDistributionCannotNameUnknownCandidate()
    {
        var permitted = CreatePermitted("candidate-1");
        var result = new DecisionResult(
            DecisionOutcome.Abstained,
            null,
            "provider-1",
            "insufficient-evidence",
            distribution: [new KeyValuePair<string, double>("candidate-2", 0.5)]);

        Assert.Throws<InvalidOperationException>(
            () => DecisionResultValidator.Validate(result, [permitted]));
    }

    [Fact]
    public void ValidDecisionSelectionRetainsEvidenceButDoesNotCreateAuthorization()
    {
        var permitted = CreatePermitted("candidate-1");
        var decision = Selected("candidate-1");

        DecisionResultValidator.Validate(decision, [permitted]);

        var selection = new SelectedAction(permitted.Candidate, decision);

        Assert.Equal(SelectionOrigin.DecisionProvider, selection.Origin);
        Assert.Same(decision, selection.Decision);
        Assert.Throws<ArgumentException>(() => new SelectedAction(
            permitted.Candidate,
            SelectionOrigin.DecisionProvider));
        Assert.DoesNotContain(
            typeof(DecisionResult).GetProperties(),
            property => property.PropertyType == typeof(AuthorizedAction));
    }

    [Fact]
    public void NonSelectedDecisionCannotIdentifyCandidate()
    {
        Assert.Throws<ArgumentException>(() => new DecisionResult(
            DecisionOutcome.Abstained,
            "candidate-1",
            "provider-1",
            "abstained"));
    }

    [Fact]
    public void DecisionProviderConsumesOnlyPermittedActions()
    {
        var method = typeof(IDecisionProvider).GetMethod(nameof(IDecisionProvider.DecideAsync));
        var candidatesParameter = Assert.Single(method!.GetParameters(), parameter => parameter.Name == "candidates");

        Assert.Equal(typeof(IReadOnlyList<PermittedAction>), candidatesParameter.ParameterType);
        Assert.NotNull(typeof(IDecisionProvider).GetProperty(nameof(IDecisionProvider.ProviderId)));
    }

    private static PermittedAction CreatePermitted(string candidateId) => new(
        AutonomousActionContractsTests.CreateCandidate(candidateId),
        [AutonomousActionContractsTests.CreateConstraint(candidateId, ConstraintOutcome.Passed)]);

    private static DecisionResult Selected(string candidateId) => new(
        DecisionOutcome.Selected,
        candidateId,
        "provider-1",
        "selected",
        confidence: 0.9);
}
