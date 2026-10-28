using Xunit;

namespace LimboDancer.Domains.Asl.Rules.Tests;

public sealed class TurnSpotterRulesTests
{
    [Theory]
    [InlineData(true, true, false, false, true)]
    [InlineData(false, true, false, false, false)]
    [InlineData(true, false, false, false, false)]
    [InlineData(true, true, true, false, false)]
    [InlineData(true, true, false, true, false)]
    public void RetentionUsesTheExistingCommandConditions(bool active, bool sameSide, bool broken, bool captured, bool expected)
    {
        Assert.Equal(expected, ScenarioA1OrdnanceEligibility.RetainsMortarSpotter(active, sameSide, broken, captured));
    }
}
