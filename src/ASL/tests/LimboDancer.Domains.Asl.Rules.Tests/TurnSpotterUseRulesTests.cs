using Xunit;

namespace LimboDancer.Domains.Asl.Rules.Tests;

public sealed class TurnSpotterUseRulesTests
{
    [Fact]
    public void ASharedSpotterRequiresOneLocationAndTargetThroughoutThePlayerTurn()
    {
        TurnSpotterUse[] first = [new("m1", "A1:0", "B3:0")];
        Assert.True(TurnSpotterUseRules.Compatible([], "m1", null, "B3:0"));
        Assert.True(TurnSpotterUseRules.Compatible(first, "m1", "A2:0", "B4:0"));
        Assert.True(TurnSpotterUseRules.Compatible(first, "m2", "A1:0", "B3:0"));
        Assert.False(TurnSpotterUseRules.Compatible(first, "m2", "A1:1", "B3:0"));
        Assert.False(TurnSpotterUseRules.Compatible(first, "m2", "A1:0", "B4:0"));
        Assert.False(TurnSpotterUseRules.Compatible(first, "m2", null, "B3:0"));
        Assert.False(TurnSpotterUseRules.Compatible([new("m1", null, "B3:0")], "m2", "A1:0", "B3:0"));
        TurnSpotterUse[] shared = [.. first, new("m2", "A1:0", "B3:0")];
        Assert.True(TurnSpotterUseRules.Compatible(shared, "m1", "A1:0", "B3:0"));
        Assert.False(TurnSpotterUseRules.Compatible(shared, "m1", "A1:0", "B4:0"));
        Assert.False(TurnSpotterUseRules.Compatible([.. first, new("m1", "A1:0", "B4:0")], "m2", "A1:0", "B4:0"));
        Assert.Throws<ArgumentNullException>(() => TurnSpotterUseRules.Compatible(null!, "m1", "A1:0", "B3:0"));
        Assert.Throws<ArgumentException>(() => TurnSpotterUseRules.Compatible([], " ", "A1:0", "B3:0"));
        Assert.Throws<ArgumentException>(() => TurnSpotterUseRules.Compatible([], "m1", "A1:0", " "));
    }
}
