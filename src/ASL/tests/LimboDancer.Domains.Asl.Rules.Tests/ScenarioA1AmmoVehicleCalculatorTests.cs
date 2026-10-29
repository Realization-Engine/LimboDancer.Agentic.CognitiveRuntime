using System.Text.Json.Nodes;
using Xunit;

namespace LimboDancer.Domains.Asl.Rules.Tests;

/// <summary>
/// E10 Ammo Vehicles (p. 244), as <see cref="ScenarioA1AmmoVehicleCalculator"/> decides it, the old members that forward to it, and the contracts
/// <c>e10.2-benefit</c> and <c>e10.3-replenishment</c> (the E10 claim's design, RE10.1 to RE10.11).
/// </summary>
public sealed class ScenarioA1AmmoVehicleCalculatorTests
{
    private static readonly TurnAmmoArmament Gun = new(75m, TurnAmmoBarrel.LongBarrel);
    private static readonly TurnAmmoSupplyFacts Supply = new("truck", "german", true, true, true, Gun);
    private static readonly TurnAmmoRecipientFacts Tank = new("tank", "german", true, true, true, true, true, true, Gun);

    [Theory]
    [InlineData(14, 75, TurnAmmoEligibility.Ineligible)]
    [InlineData(15, 75, TurnAmmoEligibility.Eligible)]
    [InlineData(19, 99, TurnAmmoEligibility.Eligible)]
    [InlineData(19, 100, TurnAmmoEligibility.Ineligible)]
    [InlineData(20, 100, TurnAmmoEligibility.Eligible)]
    public void SelectionNeedsFifteenPassengerPointsOrTwentyFromAHundredMillimetres(int pp, int caliber, TurnAmmoEligibility expected)
    {
        var facts = new TurnAmmoVehicleSelection(true, true, true, false, null, pp, caliber);
        Assert.Equal(expected, ScenarioA1AmmoVehicleCalculator.Selection(facts).State);
        Assert.Equal(ScenarioA1AmmoVehicleCalculator.Selection(facts), TurnAmmoEligibilityRules.SelectVehicle(facts));
    }

    [Fact]
    public void PortageHasNoPassengerRoomAndTheSupplyStaysUnlessImmobileOrDepleted()
    {
        Assert.Equal(0, ScenarioA1AmmoVehicleCalculator.PassengerCapacity(20, true));
        Assert.Equal(20, TurnWorkEffects.PassengerCapacity(20, false));
        Assert.Null(ScenarioA1AmmoVehicleCalculator.SupplyRemovalRefusal(true, false));
        Assert.Null(ScenarioA1AmmoVehicleCalculator.SupplyRemovalRefusal(false, true));
        Assert.Contains("E10.12", ScenarioA1AmmoVehicleCalculator.SupplyRemovalRefusal(false, false)!, StringComparison.Ordinal);
        Assert.Contains("Prove", ScenarioA1AmmoVehicleCalculator.SupplyRemovalRefusal(null, false)!, StringComparison.Ordinal);

        // D.7: a Mired, Abandoned, Shocked, or Stunned vehicle, or one whose crew broke, is Immobile; the old relocation check reads two of them.
        Assert.True(ScenarioA1AmmoVehicleCalculator.Immobile(new(false, true, false, false, false, false, false)));
        Assert.Null(ScenarioA1AmmoVehicleCalculator.Immobile(new(false, null, false, false, false, false, false)));
        Assert.False(TurnWorkEffects.CanRelocateSupply(false, false));
        Assert.True(ScenarioA1AmmoVehicleCalculator.DumpImmobile(true, false));
    }

    private static AmmoBenefitFacts Shot(TurnAmmoRecipientFacts? firer = null, IReadOnlyList<string>? served = null, bool? bounding = false) =>
        new(Supply, firer ?? Tank, served ?? [], bounding);

    [Fact]
    public void TheBenefitNeedsTheCorrectAmmoVehicleACeFirerInReachAndAtMostTwoServed()
    {
        Assert.Equal(TurnAmmoEligibility.Eligible, ScenarioA1AmmoVehicleCalculator.Benefit(Shot()).State);
        Assert.Null(ScenarioA1AmmoVehicleCalculator.BenefitRefusal(Shot(served: ["tank", "other"])));
        Assert.Contains("no more than two", ScenarioA1AmmoVehicleCalculator.BenefitRefusal(Shot(served: ["other", "third"]))!, StringComparison.Ordinal);
        Assert.NotNull(ScenarioA1AmmoVehicleCalculator.BenefitRefusal(Shot(Tank with
        {
            CrewExposed = false
        })));
        Assert.NotNull(ScenarioA1AmmoVehicleCalculator.BenefitRefusal(Shot(Tank with
        {
            SameOrAccessibleLocation = false
        })));
        Assert.NotNull(ScenarioA1AmmoVehicleCalculator.BenefitRefusal(Shot(Tank with
        {
            Armament = new(75m, TurnAmmoBarrel.Standard)
        })));
        Assert.NotNull(ScenarioA1AmmoVehicleCalculator.BenefitRefusal(Shot(Tank with
        {
            Owner = "russian"
        })));
        Assert.NotNull(ScenarioA1AmmoVehicleCalculator.BenefitRefusal(Shot(bounding: true)));
        Assert.Equal(TurnAmmoEligibility.NotEvaluated, ScenarioA1AmmoVehicleCalculator.Benefit(Shot(bounding: null)).State);
        Assert.Equal(TurnAmmoEligibility.NotEvaluated, ScenarioA1AmmoVehicleCalculator.Benefit(Shot(Tank with
        {
            CrewExposed = null
        })).State);

        // E10.21: a Prep Fire benefit bars the Ammo Vehicle's MP in the next MPh; the AFPh is allowed.
        Assert.True(ScenarioA1AmmoVehicleCalculator.BenefitBarsMovement("pfph"));
        Assert.False(ScenarioA1AmmoVehicleCalculator.BenefitBarsMovement("afph"));
    }

    [Fact]
    public void WithTheBenefitTheGunBreaksDownAsANormalB12WithNoXNumber()
    {
        // The served list counts other vehicles once each, never the firer.
        Assert.Null(ScenarioA1AmmoVehicleCalculator.BenefitRefusal(Shot(served: ["tank"])));
        Assert.Null(ScenarioA1AmmoVehicleCalculator.BenefitRefusal(Shot(served: ["other", "other"])));
        Assert.Null(ScenarioA1AmmoVehicleCalculator.ReplenishmentTurns(null));
        Assert.Contains("Prove", ScenarioA1AmmoVehicleCalculator.SupplyRemovalRefusal(null, null)!, StringComparison.Ordinal);

        Assert.Equal(new AmmoBreakdown(12, null), ScenarioA1AmmoVehicleCalculator.BreakdownNumber(10, null, true));
        Assert.Equal(new AmmoBreakdown(12, null), ScenarioA1AmmoVehicleCalculator.BreakdownNumber(9, 10, true));
        Assert.Equal(new AmmoBreakdown(9, 10), ScenarioA1AmmoVehicleCalculator.BreakdownNumber(9, 10, false));
    }

    [Theory]
    [InlineData(-1, null)]
    [InlineData(0, null)]
    [InlineData(1, 1)]
    [InlineData(8, 1)]
    [InlineData(9, 1)]
    [InlineData(10, 2)]
    [InlineData(11, 3)]
    [InlineData(12, null)]
    public void ReplenishmentTakesGameTurnsByTheCircledBNumber(int b, int? turns)
    {
        Assert.Equal(turns, ScenarioA1AmmoVehicleCalculator.ReplenishmentTurns(b));
        Assert.Equal(turns, TurnWorkRules.Evaluate(TurnWorkKind.AmmoReplenishment, b, 0, true, true, TurnWorkWindow.FirstRally).RequiredGameTurns);
        Assert.Equal(4, TurnWorkRules.Evaluate(TurnWorkKind.AmmoTransfer, b, 0, true, true, TurnWorkWindow.FirstRally).RequiredGameTurns);
    }

    private static AmmoReplenishmentFacts Start(TurnAmmoRecipientFacts? recipient = null, int count = 1) =>
        new(Supply, [.. Enumerable.Range(0, count).Select(index => (recipient ?? Tank) with { Id = $"tank-{index}" })], true, true, true, false);

    [Fact]
    public void ReplenishmentNeedsLowAmmoCeReachTiAndAtMostTwoAtOnce()
    {
        Assert.Null(ScenarioA1AmmoVehicleCalculator.ReplenishmentRefusal(Start()));
        Assert.Null(ScenarioA1AmmoVehicleCalculator.ReplenishmentRefusal(Start(count: 2)));
        Assert.NotNull(ScenarioA1AmmoVehicleCalculator.ReplenishmentRefusal(Start(count: 3)));
        Assert.NotNull(ScenarioA1AmmoVehicleCalculator.ReplenishmentRefusal(Start(Tank with
        {
            LowAmmo = false
        })));
        Assert.NotNull(ScenarioA1AmmoVehicleCalculator.ReplenishmentRefusal(Start(Tank with
        {
            CrewExposed = false
        })));
        Assert.Contains("TI", ScenarioA1AmmoVehicleCalculator.ReplenishmentRefusal(Start() with
        {
            SupplyTi = false
        })!, StringComparison.Ordinal);
        Assert.Contains("Passengers", ScenarioA1AmmoVehicleCalculator.ReplenishmentRefusal(Start() with
        {
            SupplyHasPassengers = true
        })!, StringComparison.Ordinal);
        Assert.Null(ScenarioA1AmmoVehicleCalculator.ReplenishmentRefusal(Start() with
        {
            SupplyTi = null,
            SupplyHasPassengers = null,
            IsDump = true
        }));
        Assert.Equal(TurnAmmoEligibility.NotEvaluated, ScenarioA1AmmoVehicleCalculator.Replenishment(Start() with
        {
            RecipientsTi = null
        }).State);

        // An Accessible Location serves (E10.3), where the old ready check asks one Location.
        Assert.False(TurnWorkEffects.ReplenishmentReady(true, true, false, true, false));
        Assert.True(ScenarioA1AmmoVehicleCalculator.SupplyExhausted(12));
        Assert.False(ScenarioA1AmmoVehicleCalculator.SupplyExhausted(11));
        Assert.True(TurnWorkLifecycle.RequiresAmmunitionDr(TurnWorkKind.AmmoReplenishment));
        Assert.False(TurnWorkLifecycle.RequiresAmmunitionDr(TurnWorkKind.AmmoTransfer));
    }

    [Fact]
    public void AReplenishmentRestoresOnlyALowAmmoVehiclesDepletedAmmunition()
    {
        var restored = ScenarioA1AmmoVehicleCalculator.Restored(true, ["WP", "s7"]);
        Assert.True(restored.LowAmmoEnded);
        Assert.Equal(["WP", "s7"], restored.AmmoTypes);
        Assert.False(ScenarioA1AmmoVehicleCalculator.Restored(false, ["WP"]).LowAmmoEnded);
        Assert.Empty(ScenarioA1AmmoVehicleCalculator.Restored(false, ["WP"]).AmmoTypes);
    }

    [Fact]
    public void ImmobilizationTransferOffloadAndTheMovementBar()
    {
        // E10.4 speaks only of the unarmored Ammo Vehicle; whether another vehicle is flipped is D8.1's (the build's review, finding 1).
        Assert.True(ScenarioA1AmmoVehicleCalculator.KeepsSupplyingWhenImmobilized(false, true));
        Assert.False(ScenarioA1AmmoVehicleCalculator.KeepsSupplyingWhenImmobilized(true, true));
        Assert.False(ScenarioA1AmmoVehicleCalculator.KeepsSupplyingWhenImmobilized(false, false));

        var transfer = new AmmoTransferFacts(true, false, null, true, true, true, false);
        Assert.Null(ScenarioA1AmmoVehicleCalculator.TransferRefusal(transfer));
        Assert.NotNull(ScenarioA1AmmoVehicleCalculator.TransferRefusal(transfer with
        {
            SourceImmobile = false
        }));
        Assert.Equal(TurnAmmoEligibility.NotEvaluated, ScenarioA1AmmoVehicleCalculator.Transfer(transfer with
        {
            SourceImmobile = null
        }).State);
        Assert.NotNull(ScenarioA1AmmoVehicleCalculator.TransferRefusal(transfer with
        {
            DestinationSameOrAccessible = false
        }));
        Assert.Contains("E10.11", ScenarioA1AmmoVehicleCalculator.TransferRefusal(transfer with
        {
            DestinationQualifies = false
        })!, StringComparison.Ordinal);
        Assert.NotNull(ScenarioA1AmmoVehicleCalculator.TransferRefusal(transfer with
        {
            DestinationHasSupply = true
        }));

        // RE10.6: an offload to a Dump needs an Immobile vehicle too, and forms in its own Location.
        var offload = new AmmoTransferFacts(true, true, true, null, null, null, null);
        Assert.Null(ScenarioA1AmmoVehicleCalculator.TransferRefusal(offload));
        Assert.NotNull(ScenarioA1AmmoVehicleCalculator.TransferRefusal(offload with
        {
            SourceImmobile = false
        }));
        Assert.NotNull(ScenarioA1AmmoVehicleCalculator.TransferRefusal(offload with
        {
            DumpInSourceLocation = false
        }));

        Assert.True(ScenarioA1AmmoVehicleCalculator.WreckAfterTransfer(true));
        Assert.Equal(ScenarioA1AmmoVehicleCalculator.WreckAfterTransfer(false), TurnWorkEffects.WreckAfterRelocation(false));
        Assert.True(ScenarioA1AmmoVehicleCalculator.MovementBarred(true));
    }

    [Fact]
    public void ABurningAmmoVehicleExplodesAsAGoliathWithNoTarget()
    {
        var explosion = ScenarioA1AmmoVehicleCalculator.Explosion(true, true);
        Assert.Equal((true, true, 36, 16), (explosion.Explodes, explosion.WreckRemoved, explosion.HexFp, explosion.AdjacentFp));
        Assert.Contains("weakest rear AF", explosion.SameHexAfvPosition, StringComparison.Ordinal);
        Assert.Equal("Poorly Positioned", explosion.AdjacentAfvPosition);
        Assert.False(ScenarioA1AmmoVehicleCalculator.Explosion(false, true).Explodes);
        Assert.False(ScenarioA1AmmoVehicleCalculator.Explosion(true, false).Explodes);
    }

    private static JsonObject Supplied() => new()
    {
        ["supplyId"] = "g-supply",
        ["supplyOwner"] = "german",
        ["supplyActive"] = true,
        ["supplyAuthorized"] = true,
        ["supplyAvailable"] = true,
        ["supplyCaliberMm"] = 75,
        ["supplyBarrel"] = "LongBarrel",
    };

    [Fact]
    public void TheBenefitContractGivesB12OrThePrintedB()
    {
        var contract = ScenarioA1RuleContracts.Find("e10.2-benefit")!;
        var facts = Supplied();
        facts["firerId"] = "g-tank";
        facts["firerOwner"] = "german";
        facts["firerActive"] = true;
        facts["firerHasBNumber"] = true;
        facts["firerCrewExposed"] = true;
        facts["firerSameOrAccessible"] = true;
        facts["firerCaliberMm"] = 75;
        facts["firerBarrel"] = "LongBarrel";
        facts["servedOthers"] = 0;
        facts["boundingFirstFire"] = false;
        facts["bNumber"] = 10;
        Assert.Equal("b12", ScenarioA1RuleContracts.Evaluate(contract, facts).Outcome);
        facts["boundingFirstFire"] = true;
        Assert.Equal("printed-b", ScenarioA1RuleContracts.Evaluate(contract, facts).Outcome);
        facts["boundingFirstFire"] = false;
        facts["servedOthers"] = 2;
        Assert.Equal("printed-b", ScenarioA1RuleContracts.Evaluate(contract, facts).Outcome);
        facts["servedOthers"] = 1;
        Assert.Equal("b12", ScenarioA1RuleContracts.Evaluate(contract, facts).Outcome);
        facts["servedOthers"] = 1.5m;
        Assert.Equal(ContractVerdict.NotEvaluated, ScenarioA1RuleContracts.Evaluate(contract, facts).Outcome);
        facts["servedOthers"] = 1;
        facts["firerCrewExposed"] = null;
        Assert.Equal(ContractVerdict.NotEvaluated, ScenarioA1RuleContracts.Evaluate(contract, facts).Outcome);
        Assert.All(contract.Facts, fact => Assert.Equal(ContractDisclosure.All, fact.Disclosure));
    }

    [Fact]
    public void TheReplenishmentContractMayStartOrIsRefused()
    {
        var contract = ScenarioA1RuleContracts.Find("e10.3-replenishment")!;
        var facts = Supplied();
        facts["recipientId"] = "g-tank";
        facts["recipientOwner"] = "german";
        facts["recipientActive"] = true;
        facts["recipientHasBNumber"] = true;
        facts["recipientLowAmmo"] = true;
        facts["recipientCrewExposed"] = true;
        facts["recipientSameOrAccessible"] = true;
        facts["recipientCaliberMm"] = 75;
        facts["recipientBarrel"] = "LongBarrel";
        facts["isDump"] = false;
        facts["otherRecipients"] = 1;
        facts["recipientTi"] = true;
        facts["supplyTi"] = true;
        facts["supplyHasPassengers"] = false;
        Assert.Equal("may-start", ScenarioA1RuleContracts.Evaluate(contract, facts).Outcome);

        // The two-vehicle limit (the build's review, finding 3): a third is refused.
        facts["otherRecipients"] = 2;
        Assert.Equal("refused", ScenarioA1RuleContracts.Evaluate(contract, facts).Outcome);
        facts["otherRecipients"] = 0;

        // A Dump is TI trivially and carries no Passengers.
        facts["isDump"] = true;
        facts["supplyTi"] = null;
        facts["supplyHasPassengers"] = null;
        Assert.Equal("may-start", ScenarioA1RuleContracts.Evaluate(contract, facts).Outcome);
        facts["isDump"] = false;
        facts["supplyTi"] = true;
        facts["supplyHasPassengers"] = false;
        facts["recipientLowAmmo"] = false;
        Assert.Equal("refused", ScenarioA1RuleContracts.Evaluate(contract, facts).Outcome);
        facts["recipientLowAmmo"] = null;
        Assert.Equal(ContractVerdict.NotEvaluated, ScenarioA1RuleContracts.Evaluate(contract, facts).Outcome);
        Assert.All(contract.Facts, fact => Assert.Equal(ContractDisclosure.All, fact.Disclosure));
    }
}
