using System.Text.Json.Nodes;
using Xunit;

namespace LimboDancer.Domains.Asl.Rules.Tests;

public sealed class ScenarioA1VehicleBreakdownResolutionTests
{
    [Fact]
    public void HeroRiderUsesHisWoundedMoraleAndKeepsTheSharedChecksOutcome()
    {
        Assert.Null(ScenarioA1RiderRules.MoraleBar("hero", 9, null, true, 8));
        Assert.NotNull(ScenarioA1RiderRules.MoraleBar("hero", 9, null, true, null));
        Assert.NotNull(ScenarioA1RiderRules.MoraleBar("legacy", 9, null));
        var passed = new FireCheck("MC", [4, 5], 9, [], 9, 9, true, "passed");
        Assert.Equal("passed", ScenarioA1MoraleCheck.BailOutResult(passed, false, false, false, true));
        Assert.Equal("pinned", ScenarioA1MoraleCheck.BailOutResult(passed, false, false));
        Assert.Equal("wounded", ScenarioA1MoraleCheck.BailOutResult(passed with
        {
            Passed = false,
            Consequence = "wounded"
        }, false, false, false, true));
        Assert.Equal("eliminated", ScenarioA1MoraleCheck.BailOutResult(passed with
        {
            Passed = false,
            Consequence = "eliminated"
        }, false, false, false, true));
    }

    [Fact]
    public void DisabledArmamentCannotFireAgain()
    {
        Assert.Null(ScenarioA1VehicleBreakdownResolutionCalculator.WeaponUseRefusal(false));
        Assert.NotNull(ScenarioA1VehicleBreakdownResolutionCalculator.WeaponUseRefusal(true));
    }

    [Fact]
    public void RecallFromFireAppliesTheSameCompleteTransitionAsRepair()
    {
        var changes = ScenarioA1VehicleBreakdownResolutionCalculator.DisabledChanges("ma", true).ToDictionary(item => item.Condition, item => item.Value);
        Assert.True(changes[UnitCondition.Disabled]);
        Assert.True(changes[UnitCondition.Recalled]);
        Assert.True(changes[UnitCondition.ButtonedUp]);
        Assert.False(changes[UnitCondition.Motion]);
        Assert.False(changes[UnitCondition.Stunned]);
        Assert.False(changes[UnitCondition.StunRecovery]);
        Assert.Single(ScenarioA1VehicleBreakdownResolutionCalculator.DisabledChanges("ma", false));
    }

    [Theory]
    [InlineData(VehicleBreakdownOutcome.None, true)]
    [InlineData(VehicleBreakdownOutcome.Malfunction, false)]
    [InlineData(VehicleBreakdownOutcome.LowAmmo, false)]
    [InlineData(VehicleBreakdownOutcome.Disabled, false)]
    public void CurrentDepletionProjectionKeepsEveryBreakdownException(VehicleBreakdownOutcome outcome, bool neverFired)
    {
        var current = new VehicleBreakdownResolved(new(11, null, true, false), outcome);
        Assert.Equal(neverFired, ScenarioA1VehicleBreakdownResolutionCalculator.NeverFired("none", false, false, current));
        Assert.False(ScenarioA1VehicleBreakdownResolutionCalculator.NeverFired("depleted", false, false, current));
        Assert.True(ScenarioA1VehicleBreakdownResolutionCalculator.NeverFired("none", false, false, null));
        Assert.False(ScenarioA1VehicleBreakdownResolutionCalculator.NeverFired("none", true, false, null));
        Assert.False(ScenarioA1VehicleBreakdownResolutionCalculator.NeverFired("none", false, true, null));
    }

    [Theory]
    [InlineData(0, 0, true)]
    [InlineData(1, 0, false)]
    [InlineData(0, 1, false)]
    public void PassengerOrTowingCapabilityPreventsRecall(int passengers, int towing, bool recalled)
    {
        Assert.Equal(recalled, ScenarioA1VehicleBreakdownResolutionCalculator.Recalled(true, false, null, passengers, towing));
        Assert.False(ScenarioA1VehicleBreakdownResolutionCalculator.Recalled(true, true, null, passengers, towing));
    }

    [Fact]
    public void RecordedBreakdownEvidenceRequiresEveryNestedFact()
    {
        var json = System.Text.Json.JsonSerializer.SerializeToNode(Circled)!.AsObject();
        foreach (var property in json.Select(item => item.Key).ToArray())
        {
            var incomplete = json.DeepClone().AsObject();
            Assert.True(incomplete.Remove(property));
            Assert.Throws<System.Text.Json.JsonException>(() =>
                System.Text.Json.JsonSerializer.Deserialize<VehicleBreakdownUseFacts>(incomplete.ToJsonString()));
        }
        Assert.Throws<System.Text.Json.JsonException>(() =>
            System.Text.Json.JsonSerializer.Deserialize<FireVehicleBreakdownFacts>("{}"));
    }

    [Theory]
    [InlineData(null, false, 0)]
    [InlineData(ScenarioA1CapturedVehicleRules.VehicleCrew, false, 0)]
    [InlineData(ScenarioA1CapturedVehicleRules.VehicleCrew, true, 1)]
    [InlineData(ScenarioA1CapturedVehicleRules.InfantryCrew, false, 1)]
    [InlineData(ScenarioA1CapturedVehicleRules.InfantryCrew, true, 1)]
    [InlineData(ScenarioA1CapturedVehicleRules.Smc, false, 1)]
    [InlineData(ScenarioA1CapturedVehicleRules.Smc, true, 1)]
    [InlineData(ScenarioA1CapturedVehicleRules.HalfSquad, false, 1)]
    [InlineData(ScenarioA1CapturedVehicleRules.HalfSquad, true, 2)]
    public void VehicleCrewFactsPreserveTheA2122Penalty(string? manning, bool captured, int multiple)
    {
        var facts = ScenarioA1VehicleBreakdownResolutionCalculator.WithVehicleCrew(Circled, manning, captured);
        Assert.Equal(multiple, ScenarioA1NonQualifiedUseCalculator.Multiple(facts.Captured, facts.NonQualified));
        Assert.Equal(11 - multiple * 2, ScenarioA1VehicleBreakdownResolutionCalculator.Numbers(facts, VehicleBreakdownUse.OrdnanceToHit).BNumber);
    }

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
