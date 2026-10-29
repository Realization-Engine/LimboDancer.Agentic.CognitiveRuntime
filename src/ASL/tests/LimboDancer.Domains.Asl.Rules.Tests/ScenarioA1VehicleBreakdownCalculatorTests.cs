using System.Text.Json.Nodes;
using Xunit;

namespace LimboDancer.Domains.Asl.Rules.Tests;

/// <summary>
/// D3.7 Malfunction and D3.71 Low Ammo B# (pp. 201 and 202), as <see cref="ScenarioA1VehicleBreakdownCalculator"/> decides them, the old members that
/// forward to it, and the contract <c>d3.7-vehicle-breakdown</c> (the D3.7 claim's design, RD3.1 to RD3.7).
/// </summary>
public sealed class ScenarioA1VehicleBreakdownCalculatorTests
{
    private static BreakdownNumberFacts Ma(int? b = null, bool circled = false, bool lowAmmo = false, bool shortage = false, int lowersB = 0, int lowersBX = 0, int? x = null) =>
        new(VehicleBreakdownWeapon.MainArmament, b, x, circled, false, lowAmmo, shortage, lowersB, lowersBX);

    private static VehicleBreakdownOutcome On(int dr, BreakdownNumberFacts facts, VehicleBreakdownDr kind = VehicleBreakdownDr.ToHit) =>
        ScenarioA1VehicleBreakdownCalculator.Outcome(dr, ScenarioA1VehicleBreakdownCalculator.Numbers(facts), kind);

    [Fact]
    public void TheIs3ExampleStepByStep()
    {
        // D3.71's example: B11 circled. A 12 malfunctions it, an 11 gives Low Ammo.
        var fresh = Ma(11, circled: true);
        Assert.Equal(new VehicleBreakdownNumbers(11, null, true, false), ScenarioA1VehicleBreakdownCalculator.Numbers(fresh));
        Assert.Equal(VehicleBreakdownOutcome.Malfunction, On(12, fresh));
        Assert.Equal(VehicleBreakdownOutcome.LowAmmo, On(11, fresh));
        Assert.Equal(VehicleBreakdownOutcome.None, On(10, fresh));

        // After Low Ammo: X11 and B10; an 11 or 12 Disables it, a 10 malfunctions it.
        var low = fresh with
        {
            LowAmmo = true
        };
        Assert.Equal(new VehicleBreakdownNumbers(10, 11, true, true), ScenarioA1VehicleBreakdownCalculator.Numbers(low));
        Assert.Equal(VehicleBreakdownOutcome.Disabled, On(12, low));
        Assert.Equal(VehicleBreakdownOutcome.Disabled, On(11, low));
        Assert.Equal(VehicleBreakdownOutcome.Malfunction, On(10, low));
        Assert.Equal(VehicleBreakdownOutcome.None, On(9, low));
    }

    [Fact]
    public void AnAmmunitionShortageAndTheAElevenLoweringsComposeInOrder()
    {
        // A19.131 and G10.41: an IS-3 under Ammunition Shortage is circled B10 with Low Ammo, so X10 and B9.
        Assert.Equal(new VehicleBreakdownNumbers(9, 10, true, true), ScenarioA1VehicleBreakdownCalculator.Numbers(Ma(11, circled: true, shortage: true)));
        // An uncircled B12 under Ammunition Shortage is a circled B11.
        Assert.Equal(new VehicleBreakdownNumbers(11, null, true, false), ScenarioA1VehicleBreakdownCalculator.Numbers(Ma(shortage: true)));
        // A.11: a B12 captured (A21.11, B#/X# two lower) is B10 and X12; Intensive Fire (B# only) is B10 and X12 too.
        Assert.Equal(new VehicleBreakdownNumbers(10, 12, false, false), ScenarioA1VehicleBreakdownCalculator.Numbers(Ma(lowersBX: 2)));
        Assert.Equal(new VehicleBreakdownNumbers(10, 12, false, false), ScenarioA1VehicleBreakdownCalculator.Numbers(Ma(lowersB: 2)));
        // Low Ammo then captured: X11 and B10 become X9 and B8; with Intensive Fire instead, X11 and B8.
        Assert.Equal(new VehicleBreakdownNumbers(8, 9, true, true), ScenarioA1VehicleBreakdownCalculator.Numbers(Ma(11, circled: true, lowAmmo: true, lowersBX: 2)));
        Assert.Equal(new VehicleBreakdownNumbers(8, 11, true, true), ScenarioA1VehicleBreakdownCalculator.Numbers(Ma(11, circled: true, lowAmmo: true, lowersB: 2)));
        // A printed X# (D3.7) is kept and lowered with the B#/X# lowerings.
        Assert.Equal(new VehicleBreakdownNumbers(10, 11, false, false), ScenarioA1VehicleBreakdownCalculator.Numbers(Ma(10, x: 11)));
        Assert.Equal(VehicleBreakdownOutcome.Disabled, On(11, Ma(10, x: 11)));

        // A Low Ammo counter on an uncircled B# (E10 work, G10.41) turns it into the X# too (the build's review, finding 1): B12 becomes X12 and B11.
        Assert.Equal(new VehicleBreakdownNumbers(11, 12, false, true), ScenarioA1VehicleBreakdownCalculator.Numbers(Ma(lowAmmo: true)));
        Assert.Equal(VehicleBreakdownOutcome.Malfunction, On(11, Ma(lowAmmo: true)));
        Assert.Equal(VehicleBreakdownOutcome.Disabled, On(12, Ma(lowAmmo: true)));
    }

    [Fact]
    public void ThePrintedNumbersAreTheMasAloneUnlessANoteExtendsThem()
    {
        var bmg = new BreakdownNumberFacts(VehicleBreakdownWeapon.BowMg, 10, null, true, false, false, true);
        Assert.Equal(new VehicleBreakdownNumbers(12, null, false, false), ScenarioA1VehicleBreakdownCalculator.Numbers(bmg));
        Assert.Equal(new VehicleBreakdownNumbers(10, null, true, false), ScenarioA1VehicleBreakdownCalculator.Numbers(bmg with
        {
            NoteExtends = true
        }));

        // An MG on its IFT DR breaks down on 12; a captured BMG (A21.11) at 10, X12.
        Assert.Equal(VehicleBreakdownOutcome.None, On(11, bmg, VehicleBreakdownDr.Ift));
        Assert.Equal(VehicleBreakdownOutcome.Malfunction, On(12, bmg with
        {
            Circled = false
        }, VehicleBreakdownDr.Ift));
        var captured = bmg with
        {
            LowersBX = 2,
            AmmunitionShortage = false
        };
        Assert.Equal(VehicleBreakdownOutcome.Malfunction, On(10, captured, VehicleBreakdownDr.Ift));
        Assert.Equal(VehicleBreakdownOutcome.Disabled, On(12, captured, VehicleBreakdownDr.Ift));

        // D3.71: a circled B# gives Low Ammo on the Gun's IFT DR too (IFE, Area Target Type), never on a CC DR.
        Assert.Equal(VehicleBreakdownOutcome.LowAmmo, On(11, Ma(11, circled: true), VehicleBreakdownDr.Ift));
        Assert.Equal(VehicleBreakdownOutcome.Malfunction, On(11, Ma(11, circled: true), VehicleBreakdownDr.CloseCombat));
        Assert.Throws<ArgumentOutOfRangeException>(() => On(13, Ma()));
    }

    private static VehicleRepairFacts Repair(string weapon = "ma") => new("tank", weapon, true, false, false, false);

    [Fact]
    public void RepairNeedsAMalfunctionedWeaponOncePerRallyPhaseAndAnUnimpairedCrewOrForAnAamgAHeroRider()
    {
        Assert.Null(ScenarioA1VehicleBreakdownCalculator.RepairRefusal(Repair()));
        Assert.Contains("disabled", ScenarioA1VehicleBreakdownCalculator.RepairRefusal(Repair() with
        {
            Disabled = true
        })!, StringComparison.Ordinal);
        Assert.Contains("not malfunctioned", ScenarioA1VehicleBreakdownCalculator.RepairRefusal(Repair() with
        {
            Malfunctioned = false
        })!, StringComparison.Ordinal);
        Assert.Contains("this RPh", ScenarioA1VehicleBreakdownCalculator.RepairRefusal(Repair() with
        {
            RepairedThisPhase = true
        })!, StringComparison.Ordinal);
        Assert.Contains("shocked", ScenarioA1VehicleBreakdownCalculator.RepairRefusal(Repair() with
        {
            CrewImpaired = true
        })!, StringComparison.Ordinal);

        // An AAMG: a CE crew not impaired, or a Hero Rider, who repairs it even while the crew is Stunned (RD3.4).
        Assert.NotNull(ScenarioA1VehicleBreakdownCalculator.RepairRefusal(Repair("aamg")));
        Assert.Null(ScenarioA1VehicleBreakdownCalculator.RepairRefusal(Repair("aamg") with
        {
            CrewExposed = true
        }));
        Assert.NotNull(ScenarioA1VehicleBreakdownCalculator.RepairRefusal(Repair("aamg") with
        {
            CrewExposed = true,
            CrewImpaired = true
        }));
        Assert.Null(ScenarioA1VehicleBreakdownCalculator.RepairRefusal(Repair("aamg") with
        {
            CrewImpaired = true,
            HeroRider = true
        }));

        Assert.Equal(RepairOutcome.Repaired, ScenarioA1VehicleBreakdownCalculator.RepairResult(1));
        Assert.Equal(RepairOutcome.Eliminated, ScenarioA1VehicleBreakdownCalculator.RepairResult(6));
        Assert.Equal(RepairOutcome.NoChange, ScenarioA1VehicleBreakdownCalculator.RepairResult(3));
    }

    [Fact]
    public void TheOldRepairMembersForwardWithTheSameAnswers()
    {
        for (var dr = 1; dr <= 6; dr++)
        {
            Assert.Equal(ScenarioA1VehicleBreakdownCalculator.RepairResult(dr), ScenarioA1RallyRules.VehicleMgRepairResult(dr));
        }

        foreach (var weapon in new[] { "ma", "bmg", "cmg", "aamg", "gun" })
        {
            foreach (var flags in Enumerable.Range(0, 16))
            {
                bool Bit(int bit) => (flags & (1 << bit)) != 0;
                Assert.Equal(OldVehicleWeaponRepairBar("tank", weapon, Bit(0), Bit(1), Bit(2), Bit(3)),
                    ScenarioA1RallyRules.VehicleWeaponRepairBar("tank", weapon, Bit(0), Bit(1), Bit(2), Bit(3)));
            }
        }
    }

    // The old body of ScenarioA1RallyRules.VehicleWeaponRepairBar, from main before the D3.7 claim, as the oracle the forward must equal (the build's
    // review, finding 6).
    private static string? OldVehicleWeaponRepairBar(string vehicleId, string weapon, bool malfunctioned, bool disabled, bool crewImpaired, bool repairedThisPhase) =>
        !ScenarioA1RallyRules.IsVehicleWeapon(weapon) ? $"play.repair-weapon: {vehicleId} has no weapon named '{weapon}' to repair (D3.7)"
        : disabled ? $"play.repair-weapon: {vehicleId}'s {ScenarioA1RallyRules.VehicleWeaponName(weapon)} is disabled for good (D3.7)"
        : !malfunctioned ? $"play.repair-weapon: {vehicleId}'s {ScenarioA1RallyRules.VehicleWeaponName(weapon)} is not malfunctioned (D3.7)"
        : repairedThisPhase ? $"play.repair-unit: {vehicleId}'s {ScenarioA1RallyRules.VehicleWeaponName(weapon)} has had its repair attempt this RPh (D3.7)"
        : crewImpaired
            ? $"play.repair-unit: {vehicleId}'s crew repairs nothing while it is shocked, stunned, or Recalled in this Player Turn, which is treated as a Stun, or while the vehicle is Abandoned (D3.7, D5.341)"
        : null;

    [Fact]
    public void ARecallNeedsTheMaAndEverySecondaryArmamentDisabled()
    {
        Assert.True(ScenarioA1VehicleBreakdownCalculator.Recalled(true, false, null, false));
        Assert.False(ScenarioA1VehicleBreakdownCalculator.Recalled(true, true, false, false));
        Assert.False(ScenarioA1VehicleBreakdownCalculator.Recalled(true, true, null, false));
        Assert.True(ScenarioA1VehicleBreakdownCalculator.Recalled(true, true, true, false));
        Assert.False(ScenarioA1VehicleBreakdownCalculator.Recalled(true, false, null, true));
        Assert.False(ScenarioA1VehicleBreakdownCalculator.Recalled(false, false, null, false));
    }

    private static RuleContract Contract => ScenarioA1RuleContracts.Find("d3.7-vehicle-breakdown")!;

    private static JsonObject Is3(int dr, bool lowAmmo = false) => new()
    {
        ["vehicleId"] = "is-3",
        ["weapon"] = "MA",
        ["printedB"] = 11,
        ["circled"] = true,
        ["noteExtends"] = false,
        ["lowAmmo"] = lowAmmo,
        ["ammunitionShortage"] = false,
        ["lowersB"] = 0,
        ["lowersBX"] = 0,
        ["drKind"] = "ToHit",
        ["originalDr"] = dr,
    };

    [Fact]
    public void TheContractReadsTheIs3Example()
    {
        Assert.Equal("low-ammo", ScenarioA1RuleContracts.Evaluate(Contract, Is3(11)).Outcome);
        Assert.Equal("malfunction", ScenarioA1RuleContracts.Evaluate(Contract, Is3(12)).Outcome);
        Assert.Equal("disabled", ScenarioA1RuleContracts.Evaluate(Contract, Is3(11, true)).Outcome);
        Assert.Contains("X11", ScenarioA1RuleContracts.Evaluate(Contract, Is3(11, true)).Reasons[0].Text, StringComparison.Ordinal);
        Assert.Equal("none", ScenarioA1RuleContracts.Evaluate(Contract, Is3(9, true)).Outcome);

        var bad = Is3(13);
        Assert.Equal(ContractVerdict.NotEvaluated, ScenarioA1RuleContracts.Evaluate(Contract, bad).Outcome);
        bad = Is3(11);
        bad["lowersB"] = 1.5m;
        Assert.Equal(ContractVerdict.NotEvaluated, ScenarioA1RuleContracts.Evaluate(Contract, bad).Outcome);
        bad = Is3(11);
        bad["lowersBX"] = 7;
        Assert.Equal(ContractVerdict.NotEvaluated, ScenarioA1RuleContracts.Evaluate(Contract, bad).Outcome);
        bad = Is3(11);
        bad["printedB"] = 1;
        Assert.Equal(ContractVerdict.NotEvaluated, ScenarioA1RuleContracts.Evaluate(Contract, bad).Outcome);

        // A19.131: an Ammunition Shortage has no effect on CC; on a To Hit DR it gives the IS-3 X10.
        var shortage = Is3(10);
        shortage["ammunitionShortage"] = true;
        Assert.Equal("disabled", ScenarioA1RuleContracts.Evaluate(Contract, shortage).Outcome);
        shortage["drKind"] = "CloseCombat";
        Assert.Equal("none", ScenarioA1RuleContracts.Evaluate(Contract, shortage).Outcome);
        Assert.All(Contract.Facts, fact => Assert.Equal(ContractDisclosure.All, fact.Disclosure));
    }
}
