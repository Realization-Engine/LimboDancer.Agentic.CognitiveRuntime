using System.Text.Json;
using Xunit;

namespace ASL.Turns.Tests;

public sealed class TurnPhaseCloseTests
{
    private static TurnClosePlanFacts Facts => new(TurnClosePlanStatus.Ready, true, true, ["safe to adjudicator"], ["phase-changed"]);

    [Theory]
    [InlineData(TurnClosePlanStatus.Ready, true, TurnCloseDisposition.Ready)]
    [InlineData(TurnClosePlanStatus.Ready, false, TurnCloseDisposition.ConsequencePending)]
    [InlineData(TurnClosePlanStatus.Refused, false, TurnCloseDisposition.Blocked)]
    [InlineData(TurnClosePlanStatus.Stale, false, TurnCloseDisposition.Stale)]
    public void AdjudicatorGetsExactSharedPlanClassification(TurnClosePlanStatus status, bool advances, TurnCloseDisposition expected)
    {
        var result = TurnPhaseClose.Describe(Facts with
        {
            Status = status,
            AdvancesPhase = advances
        }, true, false);
        Assert.Equal(expected, result.Disposition);
        Assert.Equal("safe to adjudicator", Assert.Single(result.Reasons));
        Assert.Equal("phase-changed", Assert.Single(result.AutomaticEffects));
        Assert.True(result.RequiresRoll);
        Assert.NotEmpty(result.EvaluationGaps);
    }

    [Fact]
    public void SideProjectionDoesNotExposeHiddenReadinessReasonsCountsOrRolls()
    {
        var first = TurnPhaseClose.Describe(Facts, false, false);
        var hiddenWorld = Facts with
        {
            Status = TurnClosePlanStatus.Refused,
            Reasons = ["hidden-unit"],
            Effects = ["secret-one", "secret-two"],
            RequiresRoll = false
        };
        Assert.Equal(JsonSerializer.Serialize(first), JsonSerializer.Serialize(TurnPhaseClose.Describe(hiddenWorld, false, false)));
        Assert.Equal(TurnCloseDisposition.GovernedCheckRequired, first.Disposition);
        Assert.Empty(first.AutomaticEffects);
        Assert.False(first.RequiresRoll);
        Assert.Equal(TurnCloseDisposition.ReadOnly, TurnPhaseClose.Describe(hiddenWorld, false, true).Disposition);
    }

    [Fact]
    public void InvalidInternalFactsFailOnlyAtTheAuthorizedBoundary()
    {
        Assert.Throws<ArgumentNullException>(() => TurnPhaseClose.Describe(null!, true, false));
        Assert.Throws<ArgumentException>(() => TurnPhaseClose.Describe(Facts with { Status = (TurnClosePlanStatus)999 }, true, false));
        Assert.Throws<ArgumentException>(() => TurnPhaseClose.Describe(Facts with { Reasons = default }, true, false));
        Assert.Throws<ArgumentException>(() => TurnPhaseClose.Describe(Facts with { Effects = default }, true, false));
    }
}
