using System.Text.Json.Nodes;
using Xunit;

namespace LimboDancer.Domains.Asl.Rules.Tests;

/// <summary>
/// A8.4 and A8.41 (p. 61), A9.3 (p. 63), and C2.241 (p. 168), as <see cref="ScenarioA1FinalFireCalculator"/> decides them, and the contract
/// <c>a8.41-final-fire</c> (the A8.41 claim's design, section 3, RA8.1 to RA8.8).
/// </summary>
public sealed class ScenarioA1FinalFireCalculatorTests
{
    [Fact]
    public void AnAvailableMaFinalFireKeepsTheVehicleInTheFirePicker()
    {
        var spent = new FireSpentFacts("dfph", true, false, true, () => false, () => false, () => false)
        {
            VehicleMgUnfired = () => false,
        };
        var ma = new FinalFireFacts(FinalFireWeapon.Mg, FireMarker.FirstFire, null, false, true, true, false, false, true);
        Assert.True(ScenarioA1FireEligibility.FireSpent(spent));
        Assert.False(ScenarioA1FireEligibility.FireSpent(spent, ma));
        Assert.True(ScenarioA1FireEligibility.FireSpent(spent, ma with
        {
            Marker = FireMarker.FinalFire
        }));
        Assert.True(ScenarioA1FireEligibility.FireSpent(spent with
        {
            Phase = "pfph"
        }, ma));
    }

    [Fact]
    public void VehicleMaKeepsItsOwnCounterWhenAnotherWeaponChangesTheUnitCounter()
    {
        Assert.Equal(FireMarker.FirstFire, ScenarioA1FinalFireCalculator.VehicleMarker("first-fire", true, false, true, false));
        Assert.Equal(FireMarker.FinalFire, ScenarioA1FinalFireCalculator.VehicleMarker("final-fire", true, true, false, false));
        Assert.Equal(FireMarker.None, ScenarioA1FinalFireCalculator.VehicleMarker("none", true, true, true, false));
        Assert.Equal(FireMarker.None, ScenarioA1FinalFireCalculator.VehicleMarker("first-fire", true, true, false, true));
        Assert.Equal(FireMarker.None, ScenarioA1FinalFireCalculator.VehicleMarker(null, false, true, true, false));
        Assert.Equal(FireMarker.FirstFire, ScenarioA1FinalFireCalculator.VehicleMarker(null, true, true, false, false));
        Assert.Equal(FireMarker.FinalFire, ScenarioA1FinalFireCalculator.VehicleMarker(null, true, false, true, false));
    }

    private const string Contract = "a8.41-final-fire";

    private static readonly bool[] Both = [false, true];

    private static readonly bool?[] Three = [null, false, true];

    private static readonly string[] Phases = ["pfph", "afph", "dfph", "mph"];

    private static FinalFireFacts Facts(FinalFireWeapon weapon, FireMarker marker, int? range = 1, bool loneSmc = false, bool vehicular = false,
        bool mainArmament = false, bool asOrdnance = false, bool noIntensiveFire = false, bool crewFit = true) =>
        new(weapon, marker, range, loneSmc, vehicular, mainArmament, asOrdnance, noIntensiveFire, crewFit);

    [Theory]
    [InlineData(FinalFireWeapon.Inherent, FireMarker.None, FinalFireUse.Normal)]
    [InlineData(FinalFireWeapon.Inherent, FireMarker.FirstFire, FinalFireUse.OnceMore)]
    [InlineData(FinalFireWeapon.Inherent, FireMarker.FinalFire, FinalFireUse.None)]
    [InlineData(FinalFireWeapon.Inherent, FireMarker.NoFire, FinalFireUse.None)]
    [InlineData(FinalFireWeapon.Mg, FireMarker.None, FinalFireUse.Normal)]
    [InlineData(FinalFireWeapon.Mg, FireMarker.FirstFire, FinalFireUse.OnceMore)]
    [InlineData(FinalFireWeapon.Mg, FireMarker.FinalFire, FinalFireUse.None)]
    [InlineData(FinalFireWeapon.Gun, FireMarker.None, FinalFireUse.Normal)]
    [InlineData(FinalFireWeapon.Gun, FireMarker.FirstFire, FinalFireUse.OnceMore)]
    [InlineData(FinalFireWeapon.Gun, FireMarker.IntensiveFire, FinalFireUse.None)]
    [InlineData(FinalFireWeapon.Gun, FireMarker.NoFire, FinalFireUse.None)]
    [InlineData(FinalFireWeapon.OrdnanceSw, FireMarker.None, FinalFireUse.Normal)]
    [InlineData(FinalFireWeapon.OrdnanceSw, FireMarker.FirstFire, FinalFireUse.None)]
    [InlineData(FinalFireWeapon.Ife, FireMarker.None, FinalFireUse.Normal)]
    [InlineData(FinalFireWeapon.Ife, FireMarker.FirstFire, FinalFireUse.OnceMore)]
    [InlineData(FinalFireWeapon.Ife, FireMarker.FinalFire, FinalFireUse.None)]
    [InlineData(FinalFireWeapon.OtherSw, FireMarker.None, FinalFireUse.Normal)]
    [InlineData(FinalFireWeapon.OtherSw, FireMarker.FirstFire, FinalFireUse.None)]
    public void EachWeaponHasItsUseByItsOwnCounter(FinalFireWeapon weapon, FireMarker marker, FinalFireUse use) =>
        Assert.Equal(use, ScenarioA1FinalFireCalculator.Use(Facts(weapon, marker)));

    [Fact]
    public void OnlyAMgThatMaySustainHasItsSecondAttack()
    {
        // A9.3: not a lone SMC's, not a vehicular MG unless the MA (RA8.3), not as ordnance.
        Assert.True(ScenarioA1FinalFireCalculator.MaySustain(true, false, false, false, false));
        Assert.False(ScenarioA1FinalFireCalculator.MaySustain(false, false, false, false, false));
        Assert.False(ScenarioA1FinalFireCalculator.MaySustain(true, false, false, false, true));
        Assert.False(ScenarioA1FinalFireCalculator.MaySustain(true, true, false, false, false));
        Assert.True(ScenarioA1FinalFireCalculator.MaySustain(true, true, true, false, false));
        Assert.False(ScenarioA1FinalFireCalculator.MaySustain(true, false, false, true, false));

        Assert.Equal(FinalFireUse.None, ScenarioA1FinalFireCalculator.Use(Facts(FinalFireWeapon.Mg, FireMarker.FirstFire, loneSmc: true)));
        Assert.Equal(FinalFireUse.None, ScenarioA1FinalFireCalculator.Use(Facts(FinalFireWeapon.Mg, FireMarker.FirstFire, vehicular: true)));
        Assert.Equal(FinalFireUse.OnceMore, ScenarioA1FinalFireCalculator.Use(Facts(FinalFireWeapon.Mg, FireMarker.FirstFire, vehicular: true, mainArmament: true)));
        Assert.Equal(FinalFireUse.None, ScenarioA1FinalFireCalculator.Use(Facts(FinalFireWeapon.Mg, FireMarker.FirstFire, asOrdnance: true)));

        // With no counter, a lone SMC's MG fires as it would (A8.41: the counter decides).
        Assert.Equal(FinalFireUse.Normal, ScenarioA1FinalFireCalculator.Use(Facts(FinalFireWeapon.Mg, FireMarker.None, loneSmc: true)));
    }

    [Fact]
    public void InReachIsRangeZeroOrOneAndAnUnknownRangeIsNot()
    {
        Assert.True(ScenarioA1FinalFireCalculator.InReach(0));
        Assert.True(ScenarioA1FinalFireCalculator.InReach(1));
        Assert.False(ScenarioA1FinalFireCalculator.InReach(2));
        Assert.False(ScenarioA1FinalFireCalculator.InReach(null));
        Assert.False(ScenarioA1FinalFireCalculator.InReach(-1));
    }

    [Fact]
    public void TheSurveysCaseAFirstFireMarkedMgFiresAgainOnlyAdjacentWhoeverHoldsIt()
    {
        // The MG carries the First Fire counter and its squad none (ruling RA8.2): at range 4 refused, at range 1 once more, Sustained.
        Assert.Contains("ADJACENT", ScenarioA1FinalFireCalculator.FinalFireRefusal(Facts(FinalFireWeapon.Mg, FireMarker.FirstFire, 4)), StringComparison.Ordinal);
        var adjacent = ScenarioA1FinalFireCalculator.Verdict(Facts(FinalFireWeapon.Mg, FireMarker.FirstFire, 1));
        Assert.Equal(new FinalFireVerdict(FinalFireUse.OnceMore, true, true, false, 2, null), adjacent);

        // The squad's own FP, unmarked, at range 4.
        Assert.Equal(new FinalFireVerdict(FinalFireUse.Normal, false, false, false, 0, null),
            ScenarioA1FinalFireCalculator.Verdict(Facts(FinalFireWeapon.Inherent, FireMarker.None, 4)));
    }

    [Fact]
    public void TheReverseCaseAnUnmarkedMgOfAMarkedSquadFiresAtAnyRangeAtFullFp()
    {
        Assert.Equal(new FinalFireVerdict(FinalFireUse.Normal, false, false, false, 0, null),
            ScenarioA1FinalFireCalculator.Verdict(Facts(FinalFireWeapon.Mg, FireMarker.None, 4)));

        // The squad's own FP, marked First Fire: once more, Area Fire, only adjacent.
        Assert.Equal(new FinalFireVerdict(FinalFireUse.OnceMore, true, false, false, 0, null),
            ScenarioA1FinalFireCalculator.Verdict(Facts(FinalFireWeapon.Inherent, FireMarker.FirstFire, 0)));
        Assert.NotNull(ScenarioA1FinalFireCalculator.FinalFireRefusal(Facts(FinalFireWeapon.Inherent, FireMarker.FirstFire, 2)));
    }

    [Fact]
    public void AFirstFireMarkedGunFiresOnceMoreAsIntensiveFireOnlyAdjacent()
    {
        Assert.Equal(new FinalFireVerdict(FinalFireUse.OnceMore, false, false, true, 0, null),
            ScenarioA1FinalFireCalculator.Verdict(Facts(FinalFireWeapon.Gun, FireMarker.FirstFire, 1)));
        Assert.Equal("marked First Fire: it fires again only at an ADJACENT or same-hex target (A8.4, A8.41)",
            ScenarioA1FinalFireCalculator.FinalFireRefusal(Facts(FinalFireWeapon.Gun, FireMarker.FirstFire, 5)));

        // C5.63: "No IF"; C5.6: a pinned, shocked, or stunned crew; already Intensive Fired.
        var none = "marked First Fire and able to use neither Sustained nor Intensive Fire: it has no Final Fire (A8.41, A9.3, C2.241)";
        Assert.Equal(none, ScenarioA1FinalFireCalculator.FinalFireRefusal(Facts(FinalFireWeapon.Gun, FireMarker.FirstFire, 1, noIntensiveFire: true)));
        Assert.Equal(none, ScenarioA1FinalFireCalculator.FinalFireRefusal(Facts(FinalFireWeapon.Gun, FireMarker.FirstFire, 1, crewFit: false)));
        Assert.Contains("Intensive Fire", ScenarioA1FinalFireCalculator.FinalFireRefusal(Facts(FinalFireWeapon.Gun, FireMarker.IntensiveFire, 1)), StringComparison.Ordinal);

        // A Gun with no counter keeps its multiple attacks at any target (C2.241).
        Assert.Null(ScenarioA1FinalFireCalculator.FinalFireRefusal(Facts(FinalFireWeapon.Gun, FireMarker.None, 9)));
    }

    [Fact]
    public void ALoneSmcCannotSustainIfeAndTheContractRequiresThatFact()
    {
        var facts = Facts(FinalFireWeapon.Ife, FireMarker.FirstFire, loneSmc: true);
        Assert.Equal(FinalFireUse.None, ScenarioA1FinalFireCalculator.Use(facts));
        Assert.NotNull(ScenarioA1FinalFireCalculator.FinalFireRefusal(facts));
        Assert.Equal(FinalFireUse.Normal, ScenarioA1FinalFireCalculator.Use(facts with
        {
            Marker = FireMarker.None
        }));
        var contract = ScenarioA1RuleContracts.Find(Contract)!;
        var input = Ask(weapon: "ife");
        input.Remove("loneSmc");
        Assert.Equal(ContractVerdict.NotEvaluated, ScenarioA1RuleContracts.Evaluate(contract, input).Outcome);
        input["loneSmc"] = true;
        Assert.Equal("refused", ScenarioA1RuleContracts.Evaluate(contract, input).Outcome);
        input["loneSmc"] = false;
        Assert.Equal("may-fire-once-more", ScenarioA1RuleContracts.Evaluate(contract, input).Outcome);
    }

    [Fact]
    public void AnIfeGunSustainsAndAnOrdnanceSwOrAFtHasNoSecondAttack()
    {
        Assert.Equal(new FinalFireVerdict(FinalFireUse.OnceMore, true, true, false, 2, null),
            ScenarioA1FinalFireCalculator.Verdict(Facts(FinalFireWeapon.Ife, FireMarker.FirstFire, 1)));
        Assert.Equal(FinalFireUse.None, ScenarioA1FinalFireCalculator.Verdict(Facts(FinalFireWeapon.OrdnanceSw, FireMarker.FirstFire, 1)).Use);
        Assert.Equal(FinalFireUse.None, ScenarioA1FinalFireCalculator.Verdict(Facts(FinalFireWeapon.OtherSw, FireMarker.FirstFire, 0)).Use);
    }

    [Fact]
    public void TheRefusalsComeInOrder()
    {
        // The counter first, then the second attack, then the range.
        Assert.StartsWith("marked Final Fire", ScenarioA1FinalFireCalculator.FinalFireRefusal(Facts(FinalFireWeapon.Mg, FireMarker.FinalFire, 5)), StringComparison.Ordinal);
        Assert.StartsWith("marked No Fire", ScenarioA1FinalFireCalculator.FinalFireRefusal(Facts(FinalFireWeapon.Gun, FireMarker.NoFire, 5)), StringComparison.Ordinal);
        Assert.Contains("neither", ScenarioA1FinalFireCalculator.FinalFireRefusal(Facts(FinalFireWeapon.Mg, FireMarker.FirstFire, 5, loneSmc: true)), StringComparison.Ordinal);
        Assert.Contains("ADJACENT", ScenarioA1FinalFireCalculator.FinalFireRefusal(Facts(FinalFireWeapon.Mg, FireMarker.FirstFire, null)), StringComparison.Ordinal);
        Assert.Throws<ArgumentException>(() => ScenarioA1FinalFireCalculator.Use(Facts((FinalFireWeapon)99, FireMarker.None)));
        Assert.Throws<ArgumentException>(() => ScenarioA1FinalFireCalculator.Use(Facts(FinalFireWeapon.Mg, (FireMarker)99)));
    }

    [Fact]
    public void TheForwardsGiveTheOldAnswersOnEveryInput()
    {
        foreach (var subsequent in Both)
        {
            foreach (var dfph in Both)
            {
                foreach (var marked in Both)
                {
                    foreach (var isMg in Three)
                    {
                        // IsSustained, before the claim.
                        var old = (subsequent || (dfph && marked)) && isMg != false;
                        Assert.Equal(old, ScenarioA1FinalFireCalculator.Sustained(subsequent, dfph, marked, isMg));
                    }

                    // IsFinalFireAgain and FireSpent's DFPh clause, before the claim.
                    Assert.Equal(dfph && marked, ScenarioA1FinalFireCalculator.FiresAgain(dfph, marked));
                }
            }
        }

        foreach (var firstFire in Both)
        {
            foreach (var finalFire in Both)
            {
                foreach (var fired in Three)
                {
                    // FirerMayFire's DFPh arm, its rateOfFireShot aside, and WeaponMayFire's DFPh arm, before the claim.
                    Assert.Equal(!finalFire && (firstFire || fired != true), ScenarioA1FinalFireCalculator.UnitMayFinalFire(firstFire, finalFire, fired == true));
                    Assert.Equal(fired != true, ScenarioA1FinalFireCalculator.WeaponMayFinalFire(fired == true));
                }
            }
        }

        // WeaponEffects' B# lowering and counter.
        Assert.Equal(2, ScenarioA1FinalFireCalculator.SustainedBreakdownReduction);
        Assert.Equal("final-fire", ScenarioA1FinalFireCalculator.SustainedCounter);
    }

    [Fact]
    public void FireSpentForwardsItsDfphClause()
    {
        foreach (var phase in Phases)
        {
            foreach (var fired in Both)
            {
                foreach (var firstFire in Both)
                {
                    var facts = new FireSpentFacts(phase, fired, firstFire, false, () => false, () => false, () => false);
                    var old = !(phase is not ("pfph" or "afph" or "dfph") || !fired || (phase == "dfph" && firstFire));
                    Assert.Equal(old, ScenarioA1FireEligibility.FireSpent(facts));
                }
            }
        }
    }

    private static JsonObject Ask(string marker = "first-fire", int? range = 1, string phase = "DFPh", string? weapon = "mg", bool known = true)
    {
        var facts = new JsonObject
        {
            ["marker"] = marker,
            ["range"] = range,
            ["phase"] = phase,
        };
        if (known)
        {
            facts["firerId"] = "g-squad";
            facts["weapon"] = weapon;
            facts["loneSmc"] = false;
            facts["vehicular"] = false;
            facts["mainArmament"] = false;
            facts["asOrdnance"] = false;
            facts["noIntensiveFire"] = false;
            facts["crewFit"] = true;
        }

        return facts;
    }

    private static string Outcome(JsonObject facts) => ScenarioA1RuleContracts.Evaluate(ScenarioA1RuleContracts.Find(Contract)!, facts).Outcome;

    [Fact]
    public void TheContractGivesEachOutcome()
    {
        Assert.Equal("may-fire-once-more", Outcome(Ask()));
        Assert.Equal("refused", Outcome(Ask(range: 4)));
        Assert.Equal("may-fire", Outcome(Ask("none", 4)));
        Assert.Equal("refused", Outcome(Ask("final-fire", 0)));
        Assert.Equal("not-final-fire", Outcome(Ask(phase: "PFPh")));
        Assert.Equal("refused", Outcome(Ask(weapon: "ordnance-sw")));
        Assert.Equal("may-fire-once-more", Outcome(Ask(weapon: "gun", range: 0)));
        var lone = Ask();
        lone["loneSmc"] = true;
        Assert.Equal("refused", Outcome(lone));
        Assert.Equal(ContractVerdict.NotEvaluated, Outcome(Ask(range: null)));
        Assert.Equal("refused", Outcome(Ask(range: null, weapon: "other-sw")));
        var half = Ask();
        half["range"] = 1.5;
        Assert.Equal(ContractVerdict.NotEvaluated, Outcome(half));
    }

    [Fact]
    public void TheOpponentKnowsOnlyWhatThePublicFactsDecide()
    {
        // The firer and its weapon are its owner's (the design's review, finding 10): with a First Fire counter at range 1, not evaluated.
        Assert.Equal(ContractVerdict.NotEvaluated, Outcome(Ask(known: false)));

        // Beyond an ADJACENT hex, or with a Final Fire counter, refused whatever fires; with no counter, it may fire.
        Assert.Equal("refused", Outcome(Ask(range: 4, known: false)));
        Assert.Equal("refused", Outcome(Ask("final-fire", known: false)));
        Assert.Equal("may-fire", Outcome(Ask("none", 6, known: false)));

        // An Owner fact the weapon needs, unknown, leaves it open; one it does not need does not.
        var mg = Ask();
        mg.Remove("asOrdnance");
        Assert.Equal(ContractVerdict.NotEvaluated, Outcome(mg));
        var inherent = Ask(weapon: "inherent");
        inherent.Remove("asOrdnance");
        Assert.Equal("may-fire-once-more", Outcome(inherent));

        var contract = ScenarioA1RuleContracts.Find(Contract)!;
        Assert.All(contract.Facts.Where(fact => fact.Disclosure == ContractDisclosure.Owner), fact => Assert.Equal("firerId", fact.Concerns));
        Assert.All(contract.Facts.Where(fact => fact.Name is "marker" or "range" or "phase"), fact => Assert.Equal(ContractDisclosure.All, fact.Disclosure));
    }
}
