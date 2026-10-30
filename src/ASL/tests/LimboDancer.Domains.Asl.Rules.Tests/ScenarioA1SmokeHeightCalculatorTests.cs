using LimboDancer.Domains.Asl.Rules;
using Xunit;

namespace LimboDancer.Domains.Asl.Rules.Tests;

public sealed class ScenarioA1SmokeHeightCalculatorTests
{
    private static SmokeLosEnd End(decimal level, bool cellar = false) => new(level, 0, cellar, false, false, 0);

    [Fact]
    public void ExactLocationShortcutLeavesOtherLevelsInTheSameHexForTheHeightRead()
    {
        Assert.Null(ScenarioA1SmokeHeightCalculator.ExactLocation(true, false, true, true));
        Assert.Null(ScenarioA1SmokeHeightCalculator.ExactLocation(false, true, true, true));
        Assert.Equal(new SmokeLosVerdict(true, true, true), ScenarioA1SmokeHeightCalculator.ExactLocation(true, true, true, true));
        Assert.Equal(new SmokeLosVerdict(true, true, false), ScenarioA1SmokeHeightCalculator.ExactLocation(true, false, true, false));
        Assert.Equal(new SmokeLosVerdict(true, false, true), ScenarioA1SmokeHeightCalculator.ExactLocation(false, true, false, true));
        Assert.Equal(new SmokeLosVerdict(true, true, true), ScenarioA1SmokeHeightCalculator.Intersect(
            new(0, 0, 2, End(0), End(1), 0, 0, false, true, true)));
    }

    [Fact]
    public void RooftopAdjustmentAndReversedLosUseTheSamePhysicalBoundary()
    {
        var roof = new SmokeLosEnd(2, -0.5m, false, false, false, 0);
        Assert.Equal(new SmokeLosVerdict(true, true, false), ScenarioA1SmokeHeightCalculator.Intersect(
            new(0, 0, 2, roof, End(0), 0, 3, false, true, false)));
        Assert.Equal(new SmokeLosVerdict(true, false, true), ScenarioA1SmokeHeightCalculator.Intersect(
            new(0, 0, 2, End(0), roof, 3, 0, false, false, true)));
    }

    [Theory]
    [InlineData(TurnSmokeChemical.Conventional, 2)]
    [InlineData(TurnSmokeChemical.WhitePhosphorus, 4)]
    public void SmokeHeightIsSeparateFromItsStrength(TurnSmokeChemical chemical, int expected) =>
        Assert.Equal(expected, ScenarioA1SmokeHeightCalculator.Height(TurnSmokeSource.InfantryGrenade, chemical));

    [Theory]
    [InlineData(0, 0, 1, 1, true)]
    [InlineData(2, 2, 1, 1, false)]
    [InlineData(3, 0, 1, 2, true)]
    [InlineData(3, 0, 1, 3, false)]
    [InlineData(3, 0, 5, 3, true)]
    [InlineData(4, 0, 1, 2, false)]
    [InlineData(0, 3, 2, 1, true)]
    public void InterveningSmokeUsesBlindHexesAndAnExclusiveTop(int source, int target, int from, int to, bool expected)
    {
        var verdict = ScenarioA1SmokeHeightCalculator.Intersect(new(0, 0, 2, End(source), End(target), from, to, true, false, false));
        Assert.Equal(new SmokeLosVerdict(expected, false, false), verdict);
    }

    [Theory]
    [InlineData(2, 1)]
    [InlineData(3, 12)]
    public void LookingDownThroughOwnSmokeIsNotLookingOutOfIt(int source, int range) =>
        Assert.Equal(new SmokeLosVerdict(true, false, false), ScenarioA1SmokeHeightCalculator.Intersect(
            new(0, 0, 2, End(source), End(0), 0, range, false, true, false)));

    [Fact]
    public void CellarLosThroughGroundSmokeDoesNotAcquireTheOutgoingPenalty() =>
        Assert.Equal(new SmokeLosVerdict(true, false, false), ScenarioA1SmokeHeightCalculator.Intersect(
            new(0, 0, 2, End(-1, true), End(2), 0, 4, false, true, false)));

    [Fact]
    public void ElevatedSmokeDoesNotFillTheLevelsBelowItsSource()
    {
        Assert.Equal(new SmokeLosVerdict(false, false, false), ScenarioA1SmokeHeightCalculator.Intersect(
            new(2, 0, 2, End(0), End(1), 1, 2, true, false, false)));
        Assert.Null(ScenarioA1SmokeHeightCalculator.Intersect(new(2, 0, 2, End(1), End(4), 0, 2, false, true, false)));
    }

    [Fact]
    public void OnlyAnEndpointWithinTheActualColumnGetsTheOutgoingPenalty() =>
        Assert.Equal(new SmokeLosVerdict(true, true, false), ScenarioA1SmokeHeightCalculator.Intersect(
            new(0, 0, 2, End(1), End(0), 0, 3, false, true, false)));
}
