using System.Text.Json.Nodes;
using Xunit;

namespace LimboDancer.Domains.Asl.Rules.Tests;

/// <summary>
/// A24.5 SMOKE Strength (p. 92), as <see cref="ScenarioA1SmokeStrengthCalculator"/> decides it, the old count members that forward to it, and the contract
/// <c>a24.5-smoke-placement</c> (the A24.5 claim's design, RA24.1 to RA24.9).
/// </summary>
public sealed class ScenarioA1SmokeStrengthCalculatorTests
{
    private const TurnSmokeChemical Smoke = TurnSmokeChemical.Conventional;
    private const TurnSmokeChemical Wp = TurnSmokeChemical.WhitePhosphorus;

    [Theory]
    [InlineData(TurnSmokeSource.Ordnance, Smoke, TurnSmokeStrength.Full, 3)]
    [InlineData(TurnSmokeSource.Ordnance, Smoke, TurnSmokeStrength.Dispersed, 2)]
    [InlineData(TurnSmokeSource.OffboardArtillery, Wp, TurnSmokeStrength.Full, 2)]
    [InlineData(TurnSmokeSource.OffboardArtillery, Wp, TurnSmokeStrength.Dispersed, 1)]
    [InlineData(TurnSmokeSource.InfantryGrenade, Smoke, TurnSmokeStrength.Unknown, 2)]
    [InlineData(TurnSmokeSource.InfantryGrenade, Wp, TurnSmokeStrength.Unknown, 1)]
    [InlineData(TurnSmokeSource.VehicleGrenade, Smoke, TurnSmokeStrength.Unknown, 2)]
    [InlineData(TurnSmokeSource.SmokeDischarger, Smoke, TurnSmokeStrength.Dispersed, 2)]
    [InlineData(TurnSmokeSource.SmokePots, Smoke, TurnSmokeStrength.Unknown, 2)]
    [InlineData(TurnSmokeSource.Drift, Smoke, TurnSmokeStrength.Dispersed, 2)]
    [InlineData(TurnSmokeSource.Drift, Wp, TurnSmokeStrength.Dispersed, 1)]
    public void EachSourceHasItsHindrance(TurnSmokeSource source, TurnSmokeChemical chemical, TurnSmokeStrength strength, int expected) =>
        Assert.Equal(expected, ScenarioA1SmokeStrengthCalculator.Hindrance(source, chemical, strength));

    [Theory]
    [InlineData(TurnSmokeSource.InfantryGrenade, Smoke, TurnSmokeStrength.Dispersed)]
    [InlineData(TurnSmokeSource.Ordnance, Smoke, TurnSmokeStrength.Unknown)]
    [InlineData(TurnSmokeSource.Ordnance, TurnSmokeChemical.Unknown, TurnSmokeStrength.Full)]
    [InlineData(TurnSmokeSource.SmokeDischarger, Wp, TurnSmokeStrength.Dispersed)]
    [InlineData(TurnSmokeSource.SmokeMortar, Smoke, TurnSmokeStrength.Full)]
    [InlineData(TurnSmokeSource.Drift, Smoke, TurnSmokeStrength.Full)]
    [InlineData(TurnSmokeSource.Blaze, Smoke, TurnSmokeStrength.Full)]
    [InlineData(TurnSmokeSource.Cave, Smoke, TurnSmokeStrength.Dispersed)]
    [InlineData(TurnSmokeSource.Napalm, Smoke, TurnSmokeStrength.Dispersed)]
    [InlineData(TurnSmokeSource.Unknown, Smoke, TurnSmokeStrength.Full)]
    public void AnUnknownInconsistentOrOtherRulesSourceHasNoValue(TurnSmokeSource source, TurnSmokeChemical chemical, TurnSmokeStrength strength) =>
        Assert.Null(ScenarioA1SmokeStrengthCalculator.Hindrance(source, chemical, strength));

    [Fact]
    public void PlacementStrengthTakesTheFullSideOnlyForTheFirstAttackOfItsPrepFire()
    {
        Assert.Equal((TurnSmokeStrength.Full, (int?)3), Side(TurnSmokeSource.Ordnance, Smoke, true, true));
        Assert.Equal((TurnSmokeStrength.Dispersed, (int?)2), Side(TurnSmokeSource.Ordnance, Smoke, true, false));
        Assert.Equal((TurnSmokeStrength.Dispersed, (int?)2), Side(TurnSmokeSource.Ordnance, Smoke, false, null));
        Assert.Equal((TurnSmokeStrength.Unknown, (int?)null), Side(TurnSmokeSource.Ordnance, Smoke, true, null));
        Assert.Equal((TurnSmokeStrength.Full, (int?)2), Side(TurnSmokeSource.OffboardArtillery, Wp, true, true));
        // D13.31 to D13.34: a dispenser is always Dispersed, even when the window is unknown (the design's review, finding 4).
        Assert.Equal((TurnSmokeStrength.Dispersed, (int?)2), Side(TurnSmokeSource.SmokeDischarger, Smoke, true, null));
        Assert.Equal((TurnSmokeStrength.Unknown, (int?)null), Side(TurnSmokeSource.InfantryGrenade, Smoke, true, true));
        Assert.Throws<ArgumentException>(() => ScenarioA1SmokeStrengthCalculator.PlacementStrength((TurnSmokeSource)99, Smoke, true, true));
        Assert.Throws<ArgumentException>(() => ScenarioA1SmokeStrengthCalculator.Hindrance(TurnSmokeSource.Ordnance, Smoke, (TurnSmokeStrength)99));
    }

    [Fact]
    public void InitialStrengthForwardsWithTheSameAnswerOnEveryInput()
    {
        foreach (var source in Enum.GetValues<TurnSmokeSource>())
        {
            foreach (var chemical in Enum.GetValues<TurnSmokeChemical>())
            {
                foreach (var prep in new[] { true, false })
                {
                    foreach (var before in new bool?[] { true, false, null })
                    {
                        Assert.Equal(ScenarioA1SmokeStrengthCalculator.PlacementStrength(source, chemical, prep, before),
                            TurnWhiteSmokeRules.InitialStrength(source, chemical, prep, before));
                    }
                }
            }
        }

        Assert.Throws<ArgumentException>(() => TurnWhiteSmokeRules.InitialStrength(TurnSmokeSource.Ordnance, (TurnSmokeChemical)99, true, true));
    }

    [Fact]
    public void TheCountMembersAnswerAsTheirByValueOverloadsAndAsBefore()
    {
        Func<bool> crossedNever = static () => throw new InvalidOperationException("crossed is read only when neither end is in the Location");
        for (var count = -1; count <= 3; count++)
        {
            var values = ScenarioA1SmokeStrengthCalculator.CountedSources(count);
            var old = Math.Min(3, 2 * count);
            Assert.Equal(old + 1, ScenarioA1VehicleSightRules.SmokeDrm(count, true, false, crossedNever));
            Assert.Equal(old, ScenarioA1VehicleSightRules.SmokeDrm(count, false, true, crossedNever));
            Assert.Equal(old, ScenarioA1VehicleSightRules.SmokeDrm(count, false, false, static () => true));
            Assert.Equal(0, ScenarioA1VehicleSightRules.SmokeDrm(count, false, false, static () => false));
            Assert.Equal(ScenarioA1VehicleSightRules.SmokeDrm(values, true, false, crossedNever), ScenarioA1VehicleSightRules.SmokeDrm(count, true, false, crossedNever));
            Assert.Equal(count == 0 ? 0 : old + 1, ScenarioA1OverrunCalculator.OverrunSmoke(count));
            Assert.Equal((count > 0 ? Math.Min(3, 2 * count) + 1 : 0) + 1, ScenarioA1VehicleSightRules.OwnHexHindrance(count, true));
            Assert.Equal(Math.Max(0, (count > 0 ? Math.Min(3, 2 * count) : 0)), ScenarioA1Sniper.TargetTem(null, count));
        }

        // By value: a WP grenade's +1 and a full ordnance SMOKE's +3 cap at +3 (A24.2), and the outgoing +1 is added after the cap (A24.8).
        Assert.Equal(4, ScenarioA1VehicleSightRules.SmokeDrm([1, 3], true, false, static () => false));
        Assert.Equal(1, ScenarioA1VehicleSightRules.SmokeDrm([1], false, true, static () => false));
        Assert.Equal(2, ScenarioA1OverrunCalculator.OverrunSmoke([1]));
        Assert.Equal(2, ScenarioA1VehicleSightRules.OwnHexHindrance([1], false));
        Assert.Equal(0, ScenarioA1VehicleSightRules.OwnHexHindrance([], false));
    }

    [Fact]
    public void ResidualFireReadsTheTargetsSmokeByValueAsBefore()
    {
        var attack = new FireAttack("PFPh", "phasing", true, "bd01:G5:0", "bd01:F5:0", [], null, 1, true, null, null, "open-ground", [], 2, null);
        Assert.Null(ScenarioA1FireMapRules.ResidualMapFacts(attack, "open-ground", 0).Los);
        Assert.Equal(2, ScenarioA1FireMapRules.ResidualMapFacts(attack, "open-ground", 1).Los!.HindranceDrm);
        Assert.Equal(3, ScenarioA1FireMapRules.ResidualMapFacts(attack, "open-ground", 2).Los!.HindranceDrm);
        Assert.Equal(1, ScenarioA1FireMapRules.ResidualMapFacts(attack, "open-ground", [1]).Los!.HindranceDrm);
    }

    private static RuleContract Contract => ScenarioA1RuleContracts.Find("a24.5-smoke-placement")!;

    private static ContractVerdict Ask(string source, string chemical, bool prep, bool? before) => ScenarioA1RuleContracts.Evaluate(Contract, new JsonObject
    {
        ["source"] = source,
        ["chemical"] = chemical,
        ["ownerPrepFire"] = prep,
        ["beforeNonSmokeFire"] = before,
    });

    [Fact]
    public void TheContractGivesTheSideAndItsHindrance()
    {
        Assert.Equal("full", Ask("Ordnance", "Conventional", true, true).Outcome);
        Assert.Contains("+3", Ask("Ordnance", "Conventional", true, true).Reasons[0].Text, StringComparison.Ordinal);
        Assert.Equal("dispersed", Ask("Ordnance", "Conventional", true, false).Outcome);
        Assert.Equal(ContractVerdict.NotEvaluated, Ask("Ordnance", "Conventional", true, null).Outcome);
        Assert.Equal(("full", "A24.11"), (Ask("InfantryGrenade", "WhitePhosphorus", false, null).Outcome, Ask("InfantryGrenade", "WhitePhosphorus", false, null).Reasons[0].Rule));
        Assert.Contains("+1", Ask("InfantryGrenade", "WhitePhosphorus", false, null).Reasons[0].Text, StringComparison.Ordinal);
        Assert.Equal(("dispersed", "D13.31"), (Ask("SmokeDischarger", "Conventional", true, true).Outcome, Ask("SmokeDischarger", "Conventional", true, true).Reasons[0].Rule));
        Assert.Equal("dispersed", Ask("Drift", "Conventional", false, null).Outcome);
        Assert.Equal(ContractVerdict.NotEvaluated, Ask("Blaze", "Conventional", true, true).Outcome);
        Assert.All(Contract.Facts, fact => Assert.Equal(ContractDisclosure.All, fact.Disclosure));
    }

    private static (TurnSmokeStrength, int?) Side(TurnSmokeSource source, TurnSmokeChemical chemical, bool prep, bool? before)
    {
        var placement = ScenarioA1SmokeStrengthCalculator.PlacementStrength(source, chemical, prep, before);
        return (placement.Strength, placement.Hindrance);
    }
}
