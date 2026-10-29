using Xunit;

namespace LimboDancer.Domains.Asl.Rules.Tests;

/// <summary>C9.31's sharing of one Spotter among mortars, as <see cref="ScenarioA1SpotterCalculator.SharesWith"/> decides it (ruling RC93.5).</summary>
public sealed class ScenarioA1SpotterSharingTests
{
    [Fact]
    public void ASharedSpotterRequiresOneLocationAndTargetThroughoutThePlayerTurn()
    {
        SpotterUse[] first = [new("m1", "A1:0", "B3:0")];
        Assert.True(ScenarioA1SpotterCalculator.SharesWith([], "m1", null, "B3:0"));
        Assert.True(ScenarioA1SpotterCalculator.SharesWith(first, "m1", "A2:0", "B4:0"));
        Assert.True(ScenarioA1SpotterCalculator.SharesWith(first, "m2", "A1:0", "B3:0"));
        Assert.False(ScenarioA1SpotterCalculator.SharesWith(first, "m2", "A1:1", "B3:0"));
        Assert.False(ScenarioA1SpotterCalculator.SharesWith(first, "m2", "A1:0", "B4:0"));
        Assert.False(ScenarioA1SpotterCalculator.SharesWith(first, "m2", null, "B3:0"));
        Assert.False(ScenarioA1SpotterCalculator.SharesWith([new("m1", null, "B3:0")], "m2", "A1:0", "B3:0"));
        SpotterUse[] shared = [.. first, new("m2", "A1:0", "B3:0")];
        Assert.True(ScenarioA1SpotterCalculator.SharesWith(shared, "m1", "A1:0", "B3:0"));
        Assert.False(ScenarioA1SpotterCalculator.SharesWith(shared, "m1", "A1:0", "B4:0"));
        Assert.False(ScenarioA1SpotterCalculator.SharesWith([.. first, new("m1", "A1:0", "B4:0")], "m2", "A1:0", "B4:0"));
        Assert.Throws<ArgumentNullException>(() => ScenarioA1SpotterCalculator.SharesWith(null!, "m1", "A1:0", "B3:0"));
        Assert.Throws<ArgumentException>(() => ScenarioA1SpotterCalculator.SharesWith([], " ", "A1:0", "B3:0"));
        Assert.Throws<ArgumentException>(() => ScenarioA1SpotterCalculator.SharesWith([], "m1", "A1:0", " "));
    }
}
