using System.Text.Json.Nodes;
using Xunit;

namespace LimboDancer.Domains.Asl.Rules.Tests;

public sealed class ScenarioA1VehicleBreakdownResolutionTests
{
    private static readonly VehicleBreakdownUseFacts Circled = new(VehicleBreakdownWeapon.MainArmament, 11, null, true, false, false, false);

    [Fact]
    public void SupplementalUseDerivesLoweringsAndDistinguishesLowAmmoFromFailure()
    {
        var low = ScenarioA1VehicleBreakdownResolutionCalculator.Resolve(Circled, VehicleBreakdownUse.OrdnanceToHit, 11);
        Assert.Equal(VehicleBreakdownOutcome.LowAmmo, low.Outcome);
        Assert.False(low.Failed);
        Assert.True(low.CountsAsFiredOnDepletionFailure);
        Assert.False(ScenarioA1VehicleBreakdownResolutionCalculator.Resolve(Circled, VehicleBreakdownUse.OrdnanceToHit, 7).CountsAsFiredOnDepletionFailure);
        var combined = ScenarioA1VehicleBreakdownResolutionCalculator.Numbers(Circled with
        {
            LowAmmo = true,
            Captured = true,
            NonQualified = true,
            WinterReduction = 1,
            IntensiveFire = true
        }, VehicleBreakdownUse.OrdnanceToHit);
        Assert.Equal(new VehicleBreakdownNumbers(3, 6, true, true), combined);
        var shortage = Circled with
        {
            AmmunitionShortage = true
        };
        Assert.Equal(VehicleBreakdownOutcome.Disabled, ScenarioA1VehicleBreakdownResolutionCalculator.Resolve(shortage, VehicleBreakdownUse.Overrun, 10).Outcome);
        Assert.Equal(VehicleBreakdownOutcome.None, ScenarioA1VehicleBreakdownResolutionCalculator.Resolve(shortage, VehicleBreakdownUse.CloseCombat, 10).Outcome);
    }

    [Fact]
    public void AreaReadsOnePreShotNumberSetAndRetainsTheStrongerOutcome()
    {
        var low = ScenarioA1VehicleBreakdownResolutionCalculator.ResolveArea(Circled, 11, 11);
        Assert.Equal(VehicleBreakdownOutcome.LowAmmo, low.Outcome);
        Assert.False(low.Failed);
        var lowAndFailed = ScenarioA1VehicleBreakdownResolutionCalculator.ResolveArea(Circled, 11, 12);
        Assert.Equal(VehicleBreakdownOutcome.Malfunction, lowAndFailed.Outcome);
        Assert.True(lowAndFailed.LowAmmoGained);
        Assert.True(lowAndFailed.Failed);
        Assert.Equal(VehicleBreakdownOutcome.Disabled, ScenarioA1VehicleBreakdownResolutionCalculator.ResolveArea(Circled with
        {
            LowAmmo = true
        }, 10, 11).Outcome);
        Assert.Equal(VehicleBreakdownOutcome.None, ScenarioA1VehicleBreakdownResolutionCalculator.ResolveArea(Circled, 7, null).Outcome);
    }

    [Fact]
    public void SupplementalFactsAreRejectedBeforeDiceAreNeeded()
    {
        Assert.Null(ScenarioA1VehicleBreakdownResolutionCalculator.FactsRefusal(Circled, VehicleBreakdownUse.OrdnanceToHit));
        Assert.NotNull(ScenarioA1VehicleBreakdownResolutionCalculator.FactsRefusal(Circled with
        {
            PrintedB = 1
        }, VehicleBreakdownUse.OrdnanceToHit));
        Assert.NotNull(ScenarioA1VehicleBreakdownResolutionCalculator.FactsRefusal(Circled with
        {
            PrintedX = 13
        }, VehicleBreakdownUse.OrdnanceToHit));
        Assert.NotNull(ScenarioA1VehicleBreakdownResolutionCalculator.FactsRefusal(Circled with
        {
            WinterReduction = 3
        }, VehicleBreakdownUse.OrdnanceToHit));
    }

    [Fact]
    public void NewRepairNeedsAnExistingWeaponAndTheActualFriendlyHeroRider()
    {
        var weapon = new VehicleRepairFacts("tank", "aamg", true, false, true, false, HeroRider: true);
        var hero = new VehicleRepairHeroFacts("hero", true, true, true, "tank", true);
        Assert.False(ScenarioA1VehicleBreakdownResolutionCalculator.HeroRider("", hero with
        {
            CarrierId = ""
        }));
        Assert.NotNull(ScenarioA1VehicleBreakdownResolutionCalculator.RepairRefusal(new(weapon with
        {
            VehicleId = ""
        }, true)));
        Assert.Null(ScenarioA1VehicleBreakdownResolutionCalculator.RepairRefusal(new(weapon, true, Hero: hero)));
        Assert.NotNull(ScenarioA1VehicleBreakdownResolutionCalculator.RepairRefusal(new(weapon, true)));
        Assert.NotNull(ScenarioA1VehicleBreakdownResolutionCalculator.RepairRefusal(new(weapon, false, Hero: hero)));
        Assert.NotNull(ScenarioA1VehicleBreakdownResolutionCalculator.RepairRefusal(new(weapon, true, Hero: hero with
        {
            CarrierId = "other"
        })));
        Assert.NotNull(ScenarioA1VehicleBreakdownResolutionCalculator.RepairRefusal(new(weapon, true, Hero: hero with
        {
            SameSide = false
        })));
        Assert.NotNull(ScenarioA1VehicleBreakdownResolutionCalculator.RepairRefusal(new(weapon, true, Hero: hero with
        {
            Captured = true
        })));
        Assert.NotNull(ScenarioA1VehicleBreakdownResolutionCalculator.RepairRefusal(new(weapon, true, Hero: hero with
        {
            Active = false
        })));
        Assert.NotNull(ScenarioA1VehicleBreakdownResolutionCalculator.RepairRefusal(new(weapon, true, Hero: hero with
        {
            IsRider = false
        })));
        var sa = new VehicleRepairFacts("tank", "sa-1", true, false, false, false);
        Assert.Null(ScenarioA1VehicleBreakdownResolutionCalculator.RepairRefusal(new(sa, true, true)));
        Assert.NotNull(ScenarioA1VehicleBreakdownResolutionCalculator.RepairRefusal(new(sa, true, false)));
        Assert.Contains("sa-1", ScenarioA1VehicleBreakdownResolutionCalculator.RepairRefusal(new(sa with
        {
            RepairedThisPhase = true
        }, true, true))!, StringComparison.Ordinal);
        // The original entry point still accepts its recorded Boolean HeroRider fact.
        Assert.Null(ScenarioA1VehicleBreakdownCalculator.RepairRefusal(weapon));
    }

    [Fact]
    public void SupplementalContractDoesNotInterpretUnknownNumbersAsB12()
    {
        var contract = ScenarioA1RuleContracts.Find("d3.7-vehicle-breakdown-use")!;
        var facts = new JsonObject
        {
            ["vehicleId"] = "tank",
            ["numbersKnown"] = true,
            ["weapon"] = "MainArmament",
            ["printedB"] = 11,
            ["circled"] = true,
            ["noteExtends"] = false,
            ["lowAmmo"] = false,
            ["ammunitionShortage"] = false,
            ["captured"] = false,
            ["nonQualified"] = false,
            ["winterReduction"] = 0,
            ["intensiveFire"] = false,
            ["use"] = "OrdnanceToHit",
            ["originalDr"] = 11
        };
        Assert.Equal("low-ammo", ScenarioA1RuleContracts.Evaluate(contract, facts).Outcome);
        facts["numbersKnown"] = false;
        facts.Remove("printedB");
        Assert.Equal(ContractVerdict.NotEvaluated, ScenarioA1RuleContracts.Evaluate(contract, facts).Outcome);
        Assert.All(contract.Facts, fact =>
        {
            Assert.Equal(ContractDisclosure.Owner, fact.Disclosure);
            Assert.Equal("vehicleId", fact.Concerns);
        });
    }
}
