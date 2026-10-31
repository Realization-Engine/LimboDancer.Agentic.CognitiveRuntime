using System.Text.Json.Nodes;
using Xunit;

namespace LimboDancer.Domains.Asl.Rules.Tests;

/// <summary>
/// A23.3 Placement and A23.4 Detonation, the Placed DC (pp. 90 and 91), as <see cref="ScenarioA1DemolitionChargeRules"/> decides them, the members that
/// forward to it, and the contracts <c>a23.3-dc-placement</c> and <c>a23.4-dc-detonation</c> (the A23.3 claim's design, RA23.1 to RA23.10).
/// </summary>
public sealed class ScenarioA1PlacedDcCalculatorTests
{
    private static readonly DcPlacementFacts Squad = new(true, true, "mph", true, true, false, false);

    [Fact]
    public void APlacementNeedsACarryingInfantryUnitAtAnAdjacentLocationInItsLosUnfiredAndUnpinned()
    {
        Assert.Null(ScenarioA1DemolitionChargeRules.PlacementRefusal(Squad));
        Assert.Contains("carrying Infantry", ScenarioA1DemolitionChargeRules.PlacementRefusal(Squad with
        {
            CarryingDc = false
        })!, StringComparison.Ordinal);
        Assert.Contains("carrying Infantry", ScenarioA1DemolitionChargeRules.PlacementRefusal(Squad with
        {
            Infantry = false
        })!, StringComparison.Ordinal);
        Assert.Contains("MPh", ScenarioA1DemolitionChargeRules.PlacementRefusal(Squad with
        {
            Phase = "afph"
        })!, StringComparison.Ordinal);
        Assert.Contains("ADJACENT", ScenarioA1DemolitionChargeRules.PlacementRefusal(Squad with
        {
            Adjacent = false
        })!, StringComparison.Ordinal);
        Assert.Contains("LOS", ScenarioA1DemolitionChargeRules.PlacementRefusal(Squad with
        {
            InLos = false
        })!, StringComparison.Ordinal);
        Assert.Contains("PFPh", ScenarioA1DemolitionChargeRules.PlacementRefusal(Squad with
        {
            FiredInPrepFire = true
        })!, StringComparison.Ordinal);
        Assert.Contains("pinned", ScenarioA1DemolitionChargeRules.PlacementRefusal(Squad with
        {
            PinnedThisMph = true
        })!, StringComparison.Ordinal);

        // An unknown fact is no permission.
        Assert.NotNull(ScenarioA1DemolitionChargeRules.PlacementRefusal(Squad with
        {
            InLos = null
        }));
        Assert.NotNull(ScenarioA1DemolitionChargeRules.PlacementRefusal(Squad with
        {
            PinnedThisMph = null
        }));

        // A23.3's example: two MF to enter the ground level of X5 from Y6 directly.
        Assert.Equal(2, ScenarioA1DemolitionChargeRules.PlacementCost(2));
        Assert.Throws<ArgumentOutOfRangeException>(() => ScenarioA1DemolitionChargeRules.PlacementCost(-1));
    }

    [Fact]
    public void WiringSettlementAndDeadlineAdaptersKeepTheBoundaries()
    {
        Assert.False(ScenarioA1DemolitionChargeRules.PlacementSettled(false, false, false));
        Assert.True(ScenarioA1DemolitionChargeRules.PlacementSettled(true, false, false));
        Assert.True(ScenarioA1DemolitionChargeRules.PlacementSettled(false, true, false));
        Assert.True(ScenarioA1DemolitionChargeRules.PlacementSettled(false, false, true));
        Assert.True(ScenarioA1DemolitionChargeRules.PlacementFailed(false, true, false));
        Assert.True(ScenarioA1DemolitionChargeRules.PlacementFailed(true, false, false));
        Assert.True(ScenarioA1DemolitionChargeRules.PlacementFailed(false, false, true));
        Assert.False(ScenarioA1DemolitionChargeRules.PlacementFailed(false, false, false));
        Assert.False(ScenarioA1DemolitionChargeRules.DetonationDue(true, false, false));
        Assert.True(ScenarioA1DemolitionChargeRules.DetonationDue(true, false, true));
        Assert.True(ScenarioA1DemolitionChargeRules.DetonationDue(true, true, false));
        Assert.False(ScenarioA1DemolitionChargeRules.DetonationDue(false, true, true));
        Assert.Contains("G11.8331", ScenarioA1DemolitionChargeRules.PlacementRefusal(Squad, true), StringComparison.Ordinal);
        Assert.Equal(ScenarioA1DemolitionChargeRules.PlacementRefusal(Squad), ScenarioA1DemolitionChargeRules.PlacementRefusal(Squad, false));
    }

    [Fact]
    public void AnEnemyAfvNeedsAPaatcOfAPlacerA116DoesNotExempt()
    {
        Assert.True(ScenarioA1DemolitionChargeRules.PlacementNeedsPaatc(true, false));
        Assert.False(ScenarioA1DemolitionChargeRules.PlacementNeedsPaatc(true, true));
        Assert.False(ScenarioA1DemolitionChargeRules.PlacementNeedsPaatc(false, false));
    }

    [Fact]
    public void ThePlacementIsOperableOnceSettledIfThePlacerSurvivedUnbrokenAndUnpinned()
    {
        // A8.1: until another unit begins movement (or the placer leaves, or the MPh ends) First Fire may still break it, so nothing is settled.
        Assert.Null(ScenarioA1DemolitionChargeRules.PlacementOperable(false, false));
        Assert.True(ScenarioA1DemolitionChargeRules.PlacementOperable(true, false));
        Assert.False(ScenarioA1DemolitionChargeRules.PlacementOperable(true, true));
        Assert.Equal(DcHeldBy.Unit, ScenarioA1DemolitionChargeRules.UnplacedDcStays(false));
        Assert.Equal(DcHeldBy.Location, ScenarioA1DemolitionChargeRules.UnplacedDcStays(true));
    }

    [Fact]
    public void TheDetonationWindowIsTheOwnersAfphOfThePlayerTurnItWasPlaced()
    {
        Assert.True(ScenarioA1DemolitionChargeRules.DetonationWindow("afph", true, true, true));
        Assert.False(ScenarioA1DemolitionChargeRules.DetonationWindow("dfph", true, true, true));
        Assert.False(ScenarioA1DemolitionChargeRules.DetonationWindow("afph", false, true, true));
        Assert.False(ScenarioA1DemolitionChargeRules.DetonationWindow("afph", true, false, true));
        Assert.False(ScenarioA1DemolitionChargeRules.DetonationWindow("afph", true, true, false));

        // A23.2's removal numbers: 12; 10 non-elite; 8 non-elite with a captured DC; 11 Inexperienced.
        foreach (var removal in new[] { 12, 11, 10, 8 })
        {
            Assert.False(ScenarioA1DemolitionChargeRules.DcMalfunction(removal - 1, removal));
            Assert.True(ScenarioA1DemolitionChargeRules.DcMalfunction(removal, removal));
        }

        Assert.True(ScenarioA1DemolitionChargeRules.RemovedAfterAttack);
        Assert.Equal(1, ScenarioA1DemolitionChargeRules.DcCxDrm(true));
        Assert.Equal(0, ScenarioA1DemolitionChargeRules.DcCxDrm(false));
    }

    [Fact]
    public void TheOldWindowMembersForwardWithTheSameAnswers()
    {
        foreach (var phase in new string?[] { "afph", "AFPh", "dfph", null })
        {
            foreach (var operable in new[] { true, false })
            {
                foreach (var ready in new[] { true, false })
                {
                    Assert.Equal(phase == "afph" && operable && ready, ScenarioA1SequenceCalculator.PlacedChargeDue(phase, operable, () => ready));
                }
            }
        }

        // TurnPlacedDcWindowRules: the window part answers as it did (outside the window for a wrong phase, turn, owner, or an inoperable DC).
        var occurrence = new TurnPhaseOccurrence("e1", 7, 2, "german", "afph");
        var facts = new TurnPlacedDcWindowFacts(occurrence, "dc", "o1", 2, "german", true, null);
        Assert.Equal(TurnDecisionTiming.NotEvaluated, TurnPlacedDcWindowRules.Assess(facts));
        Assert.Equal(TurnDecisionTiming.OutsideWindow, TurnPlacedDcWindowRules.Assess(facts with
        {
            Occurrence = occurrence with
            {
                Phase = "dfph"
            }
        }));
        Assert.Equal(TurnDecisionTiming.OutsideWindow, TurnPlacedDcWindowRules.Assess(facts with
        {
            OriginGameTurn = 1
        }));
        Assert.Equal(TurnDecisionTiming.OutsideWindow, TurnPlacedDcWindowRules.Assess(facts with
        {
            OriginOwner = "russian"
        }));
        Assert.Equal(TurnDecisionTiming.OutsideWindow, TurnPlacedDcWindowRules.Assess(facts with
        {
            Operable = false
        }));
    }

    private static RuleContract Placement => ScenarioA1RuleContracts.Find("a23.3-dc-placement")!;

    private static JsonObject Place() => new()
    {
        ["placerId"] = "g-squad",
        ["carryingDc"] = true,
        ["infantry"] = true,
        ["paatcExempt"] = false,
        ["phase"] = "MPh",
        ["adjacent"] = true,
        ["inLos"] = true,
        ["firedInPrepFire"] = false,
        ["pinnedThisMph"] = false,
        ["enemyAfvInTarget"] = false,
    };

    [Fact]
    public void ThePlacementContractDecidesAndHidesAConcealedPlacersFacts()
    {
        Assert.Equal("may-place", ScenarioA1RuleContracts.Evaluate(Placement, Place()).Outcome);
        var afv = Place();
        afv["enemyAfvInTarget"] = true;
        Assert.Equal("may-place-after-paatc", ScenarioA1RuleContracts.Evaluate(Placement, afv).Outcome);
        var fired = Place();
        fired["firedInPrepFire"] = true;
        Assert.Equal("refused", ScenarioA1RuleContracts.Evaluate(Placement, fired).Outcome);
        var unknown = Place();
        unknown["inLos"] = null;
        Assert.Equal(ContractVerdict.NotEvaluated, ScenarioA1RuleContracts.Evaluate(Placement, unknown).Outcome);

        // The opponent of a concealed placer does not know whether it carries a DC, is Infantry, or is exempt.
        foreach (var name in new[] { "carryingDc", "infantry", "paatcExempt" })
        {
            Assert.Equal((ContractDisclosure.Owner, "placerId"), Placement.Facts.Single(fact => fact.Name == name) is var fact ? (fact.Disclosure, fact.Concerns) : default);
            var hidden = Place();
            hidden[name] = null;
            Assert.Equal(ContractVerdict.NotEvaluated, ScenarioA1RuleContracts.Evaluate(Placement, hidden).Outcome);
        }
    }

    [Fact]
    public void TheDetonationContractDetonatesRemovesOrIsNotDue()
    {
        var contract = ScenarioA1RuleContracts.Find("a23.4-dc-detonation")!;
        JsonObject Ask(int dr, int removal = 12, string phase = "AFPh") => new()
        {
            ["operable"] = true,
            ["phase"] = phase,
            ["ownerPhasing"] = true,
            ["placedThisPlayerTurn"] = true,
            ["removalNumber"] = removal,
            ["originalDr"] = dr,
        };
        Assert.Equal("detonates", ScenarioA1RuleContracts.Evaluate(contract, Ask(11)).Outcome);
        Assert.Equal("removed", ScenarioA1RuleContracts.Evaluate(contract, Ask(12)).Outcome);
        Assert.Equal("removed", ScenarioA1RuleContracts.Evaluate(contract, Ask(8, 8)).Outcome);
        Assert.Equal("not-due", ScenarioA1RuleContracts.Evaluate(contract, Ask(5, phase: "DFPh")).Outcome);
        Assert.Equal(ContractVerdict.NotEvaluated, ScenarioA1RuleContracts.Evaluate(contract, Ask(13)).Outcome);
    }
}
