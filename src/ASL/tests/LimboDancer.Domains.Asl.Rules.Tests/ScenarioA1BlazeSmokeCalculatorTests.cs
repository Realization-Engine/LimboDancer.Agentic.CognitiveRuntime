using System.Text.Json.Nodes;
using Xunit;

namespace LimboDancer.Domains.Asl.Rules.Tests;

/// <summary>
/// B25.2 Smoke (p. 143), as <see cref="ScenarioA1BlazeSmokeCalculator"/> decides it, and the contract <c>b25.2-blaze-smoke</c> (the B25.2 claim's
/// design, RB25.1 to RB25.8).
/// </summary>
public sealed class ScenarioA1BlazeSmokeCalculatorTests
{
    [Theory]
    [InlineData(BlazeKind.WreckBlaze, WindForce.None, 2, 4)]
    [InlineData(BlazeKind.WreckBlaze, WindForce.MildBreeze, 2, 2)]
    [InlineData(BlazeKind.WreckBlaze, WindForce.HeavyWinds, 0, 0)]
    [InlineData(BlazeKind.TerrainBlaze, WindForce.None, 3, 4)]
    [InlineData(BlazeKind.TerrainBlaze, WindForce.MildBreeze, 3, 2)]
    [InlineData(BlazeKind.TerrainBlaze, WindForce.HeavyWinds, 0, 0)]
    [InlineData(BlazeKind.Flame, WindForce.None, 0, 0)]
    [InlineData(BlazeKind.Flame, WindForce.MildBreeze, 0, 0)]
    [InlineData(BlazeKind.Flame, WindForce.HeavyWinds, 0, 0)]
    public void EachBlazeHasItsHindranceAndHeight(BlazeKind kind, WindForce wind, int hindrance, int height)
    {
        Assert.Equal(hindrance, ScenarioA1BlazeSmokeCalculator.Hindrance(kind, wind));
        Assert.Equal(height, ScenarioA1BlazeSmokeCalculator.Height(kind, wind));
    }

    [Fact]
    public void AnUnknownWindLeavesEveryAnswerOpenAndIndoorsThereIsNoWind()
    {
        Assert.Null(ScenarioA1BlazeSmokeCalculator.Hindrance(BlazeKind.WreckBlaze, WindForce.Unknown));
        Assert.Null(ScenarioA1BlazeSmokeCalculator.Height(BlazeKind.WreckBlaze, WindForce.Unknown));
        Assert.Null(ScenarioA1BlazeSmokeCalculator.WreckBlazeLos(false, WindForce.Unknown));
        Assert.Null(ScenarioA1BlazeSmokeCalculator.DriftCounters(BlazeKind.WreckBlaze, WindForce.Unknown, false));

        // B25.63: No Wind in a non-rooftop Location of an Interior Building hex, whatever the scenario's wind.
        Assert.Equal(WindForce.None, ScenarioA1BlazeSmokeCalculator.WindFelt(WindForce.HeavyWinds, true));
        Assert.Equal(WindForce.HeavyWinds, ScenarioA1BlazeSmokeCalculator.WindFelt(WindForce.HeavyWinds, false));
        Assert.Equal(WindForce.None, ScenarioA1BlazeSmokeCalculator.WindFelt(WindForce.Unknown, true));
        Assert.Equal(WindForce.Unknown, ScenarioA1BlazeSmokeCalculator.WindFelt(WindForce.Unknown, false));
        foreach (var kind in Enum.GetValues<BlazeKind>())
        {
            Assert.Null(ScenarioA1BlazeSmokeCalculator.Hindrance(kind, WindForce.Unknown));
            Assert.Null(ScenarioA1BlazeSmokeCalculator.Height(kind, WindForce.Unknown));
        }
        Assert.Throws<ArgumentException>(() => ScenarioA1BlazeSmokeCalculator.Hindrance((BlazeKind)9, WindForce.None));
    }

    [Fact]
    public void ABurningWrecksSmokeReplacesItsHindranceExceptOnAFireLaneOrInHeavyWinds()
    {
        Assert.Equal(new WreckBlazeLosVerdict(true, false), ScenarioA1BlazeSmokeCalculator.WreckBlazeLos(false, WindForce.None));
        Assert.Equal(new WreckBlazeLosVerdict(true, false), ScenarioA1BlazeSmokeCalculator.WreckBlazeLos(false, WindForce.MildBreeze));
        Assert.Equal(new WreckBlazeLosVerdict(false, true), ScenarioA1BlazeSmokeCalculator.WreckBlazeLos(true, WindForce.None));
        Assert.Equal(new WreckBlazeLosVerdict(false, true), ScenarioA1BlazeSmokeCalculator.WreckBlazeLos(false, WindForce.HeavyWinds));
        Assert.True(ScenarioA1BlazeSmokeCalculator.KeepsWreckHindranceInTerrainBlaze);
    }

    [Fact]
    public void ABlazeDriftsAsManyCountersAsItsHindranceInAMildBreezeAndNotInRain()
    {
        Assert.Equal(2, ScenarioA1BlazeSmokeCalculator.DriftCounters(BlazeKind.WreckBlaze, WindForce.MildBreeze, false));
        Assert.Equal(3, ScenarioA1BlazeSmokeCalculator.DriftCounters(BlazeKind.TerrainBlaze, WindForce.MildBreeze, false));
        Assert.Equal(0, ScenarioA1BlazeSmokeCalculator.DriftCounters(BlazeKind.TerrainBlaze, WindForce.MildBreeze, true));
        Assert.Equal(0, ScenarioA1BlazeSmokeCalculator.DriftCounters(BlazeKind.WreckBlaze, WindForce.None, false));
        Assert.Equal(0, ScenarioA1BlazeSmokeCalculator.DriftCounters(BlazeKind.Flame, WindForce.MildBreeze, false));
    }

    [Fact]
    public void AWreckBlazeAndAGrenadeInOneLocationCapAtThree()
    {
        // A24.2's by-value total takes the Blaze's +2 beside a grenade's +2: +3, and A24.8's +1 for fire traced out of it.
        var blaze = ScenarioA1BlazeSmokeCalculator.Hindrance(BlazeKind.WreckBlaze, WindForce.None)!.Value;
        Assert.Equal(3, ScenarioA1VehicleSightRules.SmokeDrm([blaze, 2], false, true, static () => false));
        Assert.Equal(4, ScenarioA1VehicleSightRules.SmokeDrm([blaze, 2], true, false, static () => false));
    }

    private static RuleContract Contract => ScenarioA1RuleContracts.Find("b25.2-blaze-smoke")!;

    private static ContractVerdict Ask(string kind, string wind, bool interior = false, bool fireLane = false, bool rain = false) =>
        ScenarioA1RuleContracts.Evaluate(Contract, new JsonObject
        {
            ["kind"] = kind,
            ["wind"] = wind,
            ["interiorNonRooftop"] = interior,
            ["fireLaneResidualAttack"] = fireLane,
            ["rain"] = rain,
        });

    [Fact]
    public void TheContractSaysWhatTheBlazeAddsToALos()
    {
        Assert.Equal("smoke", Ask("WreckBlaze", "None").Outcome);
        Assert.Contains("+2, 4 levels", Ask("WreckBlaze", "None").Reasons[0].Text, StringComparison.Ordinal);
        Assert.Contains("drift 3", Ask("TerrainBlaze", "MildBreeze").Reasons[0].Text, StringComparison.Ordinal);
        Assert.DoesNotContain("drift", Ask("TerrainBlaze", "MildBreeze", rain: true).Reasons[0].Text, StringComparison.Ordinal);
        Assert.Equal("smoke", Ask("TerrainBlaze", "MildBreeze", rain: true).Outcome);
        Assert.Equal("wreck-only", Ask("WreckBlaze", "None", fireLane: true).Outcome);
        Assert.Equal("wreck-only", Ask("WreckBlaze", "HeavyWinds").Outcome);
        Assert.Equal("none", Ask("TerrainBlaze", "HeavyWinds").Outcome);
        Assert.Equal("none", Ask("Flame", "None").Outcome);
        Assert.Equal("smoke", Ask("TerrainBlaze", "HeavyWinds", interior: true).Outcome);
        Assert.Equal(ContractVerdict.NotEvaluated, Ask("TerrainBlaze", "None", fireLane: true).Outcome);
        Assert.Equal(ContractVerdict.NotEvaluated, Ask("Flame", "None", fireLane: true).Outcome);
        Assert.All(Contract.Facts, fact => Assert.Equal(ContractDisclosure.All, fact.Disclosure));
    }
}
