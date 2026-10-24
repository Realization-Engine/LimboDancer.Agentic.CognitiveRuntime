using LimboDancer.Domains.Asl.Rules;
using Xunit;

namespace LimboDancer.Domains.Asl.Rules.Tests;

/// <summary>
/// Pass 65, Passengers, Riders, and crews. Increment 2a: the crew kind (A1.123, D5.1; ruling R65.2), the vehicle crews of catalog 1.18.0
/// (p. 695; ruling R65.3), the relaxed catalog check (ruling R65.4), and the definition a crew that leaves its vehicle is placed as (ruling R65.19).
/// </summary>
public sealed class ScenarioA1Pass65RulesTests
{
    private static readonly ScenarioA1OrdnanceReference Ordnance = new ScenarioA1OrdnancePackage().Reference;

    [Fact]
    public void AVehicleCrewIsACrewAndASquadIsNot()
    {
        Assert.True(ScenarioA1Definitions.IsCrew("asl:crew"));
        Assert.True(ScenarioA1Definitions.IsCrew("asl:vehicle-crew"));
        Assert.False(ScenarioA1Definitions.IsCrew("asl:squad"));
        Assert.False(ScenarioA1Definitions.IsCrew("asl:half-squad"));
        Assert.False(ScenarioA1Definitions.IsCrew(null));
    }

    [Fact]
    public void AVehicleCrewStacksCountsAndFightsAsACrew()
    {
        // A1.123, D5.1: all of a crew's capabilities but its Strength Factor: a HS-equivalent in stacking, 2 VP, US# 2, CCV 4's group, an MMC.
        Assert.Equal(StackUnitKind.HalfSquadOrCrew, ScenarioA1Stacking.KindOf("asl:vehicle-crew"));
        Assert.Equal(2, ScenarioA1VictoryCalculator.VictoryPoints("asl:vehicle-crew", () => null, () => 0));
        Assert.Equal(2, ScenarioA1Definitions.UnitSize("asl:vehicle-crew"));
        Assert.Equal(0.5, ScenarioA1SetupCalculator.SquadEquivalent("asl:vehicle-crew"));
        Assert.True(ScenarioA1SetupCalculator.Mmc("asl:vehicle-crew"));
        Assert.Contains("asl:vehicle-crew", ScenarioA1Definitions.MmcKinds);

        // C11.2, C11.3: a vehicle crew manning a Gun keeps it Emplaced and is a qualified crew.
        Assert.True(ScenarioA1OrdnanceEligibility.Emplaced("asl:vehicle-crew", () => false));
        Assert.Null(ScenarioA1OrdnanceEligibility.NonQualified(true, "asl:vehicle-crew", false));
        Assert.False(ScenarioA1OrdnanceProjection.LosesEmplacement("asl:gun", "asl:vehicle-crew"));
    }

    [Fact]
    public void TheCatalogHoldsTheEightVehicleCrewsOfTheChart()
    {
        // The National Capabilities Chart (p. 695), the design's R65.3 table: FP-range-morale, broken morale.
        (string Nationality, int Morale, int Broken)[] crews =
            [("german", 7, 7), ("russian", 7, 7), ("british", 7, 7), ("american", 6, 7), ("finnish", 7, 7), ("italian", 6, 5), ("axis-minor", 6, 5), ("french", 7, 6)];
        foreach (var (nationality, morale, broken) in crews)
        {
            var crew = Ordnance.Fire.Definitions[nationality + "-vehicle-crew"];
            Assert.Equal(("asl:vehicle-crew", nationality, 1, 2, morale, broken, true), (crew.Kind, crew.Nationality, crew.Firepower, crew.Range, crew.Morale,
                crew.BrokenMorale, crew.SelfRally));
        }

        // D6.81 (p. 206): the Bren carrier carries four PP.
        Assert.Equal(4, Ordnance.Fire.Definitions["campaign-british-bren-carrier"].PassengerCapacity);
    }

    [Fact]
    public void ACrewLeavesItsVehicleAsItsNationalitysVehicleCrew()
    {
        var definitions = Ordnance.Fire.Definitions.Values;
        Assert.Equal("german-vehicle-crew", ScenarioA1Definitions.LeavingCrewDefinition(definitions, "german"));
        Assert.Equal("italian-vehicle-crew", ScenarioA1Definitions.LeavingCrewDefinition(definitions, "italian"));
        Assert.Equal("finnish-vehicle-crew", ScenarioA1Definitions.LeavingCrewDefinition(definitions, "finnish"));
        Assert.Null(ScenarioA1Definitions.LeavingCrewDefinition(definitions, "japanese"));
        Assert.Null(ScenarioA1Definitions.LeavingCrewDefinition(definitions, null));
    }

    [Fact]
    public void APackageReadsItsCatalogAtAnOlderMinorVersion()
    {
        const string catalog = "asl-scenario-a1";
        Assert.True(ScenarioA1Definitions.CatalogServes(catalog, "1.18.0", catalog, "1.18.0"));
        Assert.True(ScenarioA1Definitions.CatalogServes(catalog, "1.18.0", catalog, "1.17.0"));
        Assert.True(ScenarioA1Definitions.CatalogServes(catalog, "1.18.0", catalog, "1.1.0"));
        Assert.False(ScenarioA1Definitions.CatalogServes(catalog, "1.18.0", catalog, "1.19.0"));
        Assert.False(ScenarioA1Definitions.CatalogServes(catalog, "1.18.0", catalog, "2.0.0"));
        Assert.False(ScenarioA1Definitions.CatalogServes(catalog, "1.18.0", "asl-scenario-a1-synthetic", "1.0.0"));
        Assert.False(ScenarioA1Definitions.CatalogServes(catalog, "1.18.0", catalog, "not-a-version"));
        Assert.Null(ScenarioA1OrdnanceEligibility.CatalogRefusal(catalog, "1.18.0", catalog, "1.17.0"));
        Assert.StartsWith("play.cc-catalog", ScenarioA1CloseCombatFactBuilders.CatalogBar(catalog, "2.0.0", catalog, "1.18.0"), StringComparison.Ordinal);
    }
}
