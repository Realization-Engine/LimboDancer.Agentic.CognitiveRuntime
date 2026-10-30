using System.Text.Json.Nodes;
using Xunit;

namespace LimboDancer.Domains.Asl.Rules.Tests;

/// <summary>
/// The A21.13 claim's calculator, <see cref="ScenarioA1NonQualifiedUseCalculator"/> (A21.11, A21.12, A21.13 p. 88; A.11 p. 43; C5.8 p. 173; A15.23
/// p. 83): who may fire a crew-served ordnance weapon, the penalty multiple, its arithmetic, the old members that forward to it with the same answer,
/// and the contract <c>a21.13-crew-served-use</c>.
/// </summary>
public sealed class ScenarioA1NonQualifiedUseCalculatorTests
{
    private static readonly int?[] Penalties = [null, -1, 0, 1, 2, 3];

    private static readonly string?[] Kinds = [null, "asl:crew", "asl:vehicle-crew", "asl:squad", "asl:half-squad", "asl:leader", "asl:hero", "asl:vehicle", "other"];

    private static NonQualifiedUseFacts Gun(CrewServedUser? user, bool captured = false, bool? hero = false, int? caliber = 75) =>
        new(user, captured, hero, false, caliber);

    [Fact]
    public void ACrewCounterIsQualifiedAndOnlyIt()
    {
        // C2.1 (p. 167), D5.1 (p. 203): an infantry crew or a vehicle crew.
        Assert.True(ScenarioA1NonQualifiedUseCalculator.Qualified("asl:crew"));
        Assert.True(ScenarioA1NonQualifiedUseCalculator.Qualified("asl:vehicle-crew"));
        Assert.All(new[] { "asl:squad", "asl:half-squad", "asl:leader", "asl:hero", "asl:vehicle", null }, kind => Assert.False(ScenarioA1NonQualifiedUseCalculator.Qualified(kind)));
    }

    [Fact]
    public void TheUserIsReadFromTheKindAndTheSmcFiringTogether()
    {
        Assert.Equal(CrewServedUser.Crew, ScenarioA1NonQualifiedUseCalculator.User("asl:vehicle-crew", 0));
        Assert.Equal(CrewServedUser.Mmc, ScenarioA1NonQualifiedUseCalculator.User("asl:squad", 0));
        Assert.Equal(CrewServedUser.Mmc, ScenarioA1NonQualifiedUseCalculator.User("asl:half-squad", 0));
        Assert.Equal(CrewServedUser.LoneSmc, ScenarioA1NonQualifiedUseCalculator.User("asl:leader", 1));
        Assert.Equal(CrewServedUser.SmcGroup, ScenarioA1NonQualifiedUseCalculator.User("asl:leader", 2));
        Assert.Equal(CrewServedUser.SmcGroup, ScenarioA1NonQualifiedUseCalculator.User("asl:hero", 3));
        Assert.Null(ScenarioA1NonQualifiedUseCalculator.User("asl:vehicle", 0));
        Assert.Null(ScenarioA1NonQualifiedUseCalculator.User(null, 0));
    }

    [Theory]
    // A21.13: crew 0, or 1 on a captured weapon; a squad, HS, or two SMC 1, doubled on a captured one; a lone hero 1 either way (rulings RA21.2, RA21.3).
    [InlineData(CrewServedUser.Crew, false, 0)]
    [InlineData(CrewServedUser.Crew, true, 1)]
    [InlineData(CrewServedUser.Mmc, false, 1)]
    [InlineData(CrewServedUser.Mmc, true, 2)]
    [InlineData(CrewServedUser.SmcGroup, false, 1)]
    [InlineData(CrewServedUser.SmcGroup, true, 2)]
    public void EveryCellOfTheUserAndCapturedTableGivesItsMultiple(CrewServedUser user, bool captured, int multiple)
    {
        Assert.Equal(multiple, ScenarioA1NonQualifiedUseCalculator.PenaltyMultiple(user, captured, hero: false));
        var verdict = ScenarioA1NonQualifiedUseCalculator.Use(Gun(user, captured));
        Assert.Null(verdict.Refusal);
        Assert.Equal(multiple, verdict.Multiple);
        Assert.False(verdict.FiresAsLight);
        Assert.True(verdict.HeroDrmApplies);
        Assert.False(verdict.HeroGunLimit);
    }

    [Fact]
    public void TheMultipleIsOneForCaptureAndOneForANonQualifiedUser()
    {
        // C5.8: "+4 if both apply".
        Assert.Equal(0, ScenarioA1NonQualifiedUseCalculator.Multiple(false, false));
        Assert.Equal(1, ScenarioA1NonQualifiedUseCalculator.Multiple(true, false));
        Assert.Equal(1, ScenarioA1NonQualifiedUseCalculator.Multiple(false, true));
        Assert.Equal(2, ScenarioA1NonQualifiedUseCalculator.Multiple(true, true));
    }

    [Fact]
    public void ALoneSmcThatIsNotAHeroIsRefusedAGun()
    {
        var leader = ScenarioA1NonQualifiedUseCalculator.Use(Gun(CrewServedUser.LoneSmc));
        Assert.Equal("A lone SMC fires a crew-served weapon only if heroic (A21.13, A15.23).", leader.Refusal);
        Assert.Equal("A21.13", leader.RefusalRule);
        Assert.Equal(leader.Refusal, ScenarioA1NonQualifiedUseCalculator.UseRefusal(Gun(CrewServedUser.LoneSmc)));

        // A lone leader with a 120mm mortar: the C9 claim's refusal text.
        var heavy = ScenarioA1NonQualifiedUseCalculator.Use(new NonQualifiedUseFacts(CrewServedUser.LoneSmc, false, false, true, 120));
        Assert.Equal(ScenarioA1MortarCalculator.LoneSmcRefusal(120, true), heavy.Refusal);
    }

    [Fact]
    public void ALoneSmcFiresAnEightyOneMillimetreMortarAsALightMortar()
    {
        // A21.13's exception, C9.2: no A21.13 penalty; a captured one keeps its capture's.
        var mortar = ScenarioA1NonQualifiedUseCalculator.Use(new NonQualifiedUseFacts(CrewServedUser.LoneSmc, false, false, true, 81));
        Assert.Null(mortar.Refusal);
        Assert.True(mortar.FiresAsLight);
        Assert.Equal(0, mortar.Multiple);
        Assert.Equal(1, ScenarioA1NonQualifiedUseCalculator.Use(new NonQualifiedUseFacts(CrewServedUser.LoneSmc, true, null, true, 82)).Multiple);

        // A light mortar needs no crew (C9.2): a squad fires it with no penalty.
        var light = ScenarioA1NonQualifiedUseCalculator.Use(new NonQualifiedUseFacts(CrewServedUser.Mmc, false, null, true, 50));
        Assert.True(light.FiresAsLight);
        Assert.Equal(0, light.Multiple);

        // An 81mm mortar is crew-served for a squad.
        Assert.Equal(1, ScenarioA1NonQualifiedUseCalculator.Use(new NonQualifiedUseFacts(CrewServedUser.Mmc, false, null, true, 81)).Multiple);
    }

    [Fact]
    public void ALoneHeroFiresAGunAsIfCapturedWithoutHisHeroicDrm()
    {
        // A15.23 (p. 83): 82mm or less, no limit; captured or not, the single penalty (ruling RA21.3).
        foreach (var captured in new[] { false, true })
        {
            var small = ScenarioA1NonQualifiedUseCalculator.Use(Gun(CrewServedUser.LoneSmc, captured, hero: true, caliber: 75));
            Assert.Null(small.Refusal);
            Assert.Equal(1, small.Multiple);
            Assert.False(small.HeroDrmApplies);
            Assert.False(small.HeroGunLimit);
        }

        Assert.Equal(1, ScenarioA1NonQualifiedUseCalculator.PenaltyMultiple(CrewServedUser.LoneSmc, true, hero: true));
    }

    [Fact]
    public void ALoneHeroFiresAGunOverEightyTwoMillimetresOncePerGameTurnInTheMphOrDfph()
    {
        var facts = Gun(CrewServedUser.LoneSmc, hero: true, caliber: 105) with
        {
            Phase = "DFPh",
            FiredThisGameTurn = false
        };
        var verdict = ScenarioA1NonQualifiedUseCalculator.Use(facts);
        Assert.Null(verdict.Refusal);
        Assert.True(verdict.HeroGunLimit);
        Assert.False(verdict.HeroDrmApplies);
        Assert.Equal(["MPh", "DFPh"], ScenarioA1NonQualifiedUseCalculator.HeroGunPhases);
        Assert.Null(ScenarioA1NonQualifiedUseCalculator.UseRefusal(facts with
        {
            Phase = "MPh"
        }));

        Assert.Equal("A hero fires a Gun over 82mm only in the MPh or DFPh (A15.23).", ScenarioA1NonQualifiedUseCalculator.UseRefusal(facts with
        {
            Phase = "PFPh"
        }));
        Assert.Equal("A hero fires a Gun over 82mm only once per Game Turn (A15.23).", ScenarioA1NonQualifiedUseCalculator.UseRefusal(facts with
        {
            FiredThisGameTurn = true
        }));
        Assert.Equal("The phase of the shot is not known (A15.23).", ScenarioA1NonQualifiedUseCalculator.UseRefusal(facts with
        {
            Phase = null
        }));
        Assert.Equal("Whether the Gun has fired this Game Turn is not known (A15.23).", ScenarioA1NonQualifiedUseCalculator.UseRefusal(facts with
        {
            FiredThisGameTurn = null
        }));

        // A hero with a mortar over 82mm fires it as a Gun, under the same limits (the C9 claim's HeroFiresAsGun).
        var mortar = new NonQualifiedUseFacts(CrewServedUser.LoneSmc, false, true, true, 120) { Phase = "MPh", FiredThisGameTurn = false };
        Assert.True(ScenarioA1MortarCalculator.HeroFiresAsGun(120, true, true));
        Assert.True(ScenarioA1NonQualifiedUseCalculator.Use(mortar).HeroGunLimit);
    }

    [Fact]
    public void AnUnknownFactRefusesTheUse()
    {
        Assert.Equal("Who fires the crew-served weapon is not known (A21.13).", ScenarioA1NonQualifiedUseCalculator.UseRefusal(Gun(null)));
        Assert.Equal("Whether the weapon is captured is not known (A21.11).",
            ScenarioA1NonQualifiedUseCalculator.UseRefusal(new NonQualifiedUseFacts(CrewServedUser.Crew, null, false, false, 75)));
        Assert.Equal("Whether the weapon is a mortar is not known (C9.2).",
            ScenarioA1NonQualifiedUseCalculator.UseRefusal(new NonQualifiedUseFacts(CrewServedUser.Crew, false, false, null, 75)));
        Assert.Equal("The mortar's caliber is not known (C9.2).",
            ScenarioA1NonQualifiedUseCalculator.UseRefusal(new NonQualifiedUseFacts(CrewServedUser.Crew, false, false, true, null)));
        Assert.Equal("Whether the lone SMC is a hero is not known (A15.23).", ScenarioA1NonQualifiedUseCalculator.UseRefusal(Gun(CrewServedUser.LoneSmc, hero: null)));
        Assert.Equal("The Gun's caliber is not known (A15.23).", ScenarioA1NonQualifiedUseCalculator.UseRefusal(Gun(CrewServedUser.LoneSmc, hero: true, caliber: null)));
        Assert.Throws<ArgumentNullException>(() => ScenarioA1NonQualifiedUseCalculator.Use(null!));
    }

    [Theory]
    // A21.11, A21.12, A.11, C5.8 (ruling RA21.5): at m = 0, 1, 2.
    [InlineData(0, 0, 0, 0)]
    [InlineData(1, 2, 1, 2)]
    [InlineData(2, 4, 2, 4)]
    public void TheArithmeticOfAMultiple(int multiple, int breakdown, int rof, int caseH)
    {
        Assert.Equal(breakdown, ScenarioA1NonQualifiedUseCalculator.BreakdownLowering(multiple));
        Assert.Equal(rof, ScenarioA1NonQualifiedUseCalculator.RofLowering(multiple));
        Assert.Equal(caseH, ScenarioA1NonQualifiedUseCalculator.CaseHDrm(multiple));
        Assert.Equal(multiple > 0 ? 10 : null, ScenarioA1NonQualifiedUseCalculator.XNumber(10, multiple));
        Assert.Equal(multiple > 0 ? "red" : "black", ScenarioA1NonQualifiedUseCalculator.ToHitColor("black", multiple));
        Assert.Equal("red", ScenarioA1NonQualifiedUseCalculator.ToHitColor("red", multiple));
        Assert.Equal(Math.Max(0, 2 - multiple), ScenarioA1NonQualifiedUseCalculator.MultipleRof(2, multiple));
    }

    [Fact]
    public void TheMultipleRofNeverFallsBelowZero()
    {
        Assert.Equal(0, ScenarioA1NonQualifiedUseCalculator.MultipleRof(1, 2));
        Assert.Equal(0, ScenarioA1NonQualifiedUseCalculator.MultipleRof(0, 1));
        Assert.Equal(3, ScenarioA1NonQualifiedUseCalculator.MultipleRof(3, 0));
    }

    [Fact]
    public void TheEligibilitysNonQualifiedForwardsWithTheSameAnswer()
    {
        // Ruling RA21.7: the old expression, copied, against the forward, over every kind and flag.
        foreach (var kind in Kinds)
        {
            foreach (var equipment in new[] { false, true })
            {
                foreach (var sw in new[] { false, true })
                {
                    bool? old = equipment && !(kind is "asl:crew" or "asl:vehicle-crew") && !sw ? true : null;
                    Assert.Equal(old, ScenarioA1OrdnanceEligibility.NonQualified(equipment, kind, sw));
                }
            }
        }
    }

    [Fact]
    public void TheOrdnanceArithmeticForwardsWithTheSameAnswerOverCapturedPenaltyAndNonQualified()
    {
        // Ruling RA21.7: CapturedColor, Breakdown's term, the ROF at the ordnance and armor calculators, PassEightFirerDrm's two Case H, and the fire
        // calculator's vehicle MG read CapturedPenalty alone, before and after: the forwards take the same input, so NonQualified changes none of them
        // (the fault W-A21.13 fixes). Each old expression, copied, against the forward.
        foreach (var penalty in Penalties)
        {
            var m = penalty ?? 0;
            foreach (var color in new[] { "black", "red" })
            {
                Assert.Equal(penalty is > 0 ? "red" : color, ScenarioA1NonQualifiedUseCalculator.ToHitColor(color, m));
            }

            foreach (var printed in new int?[] { null, 0, 1, 2, 3, 12 })
            {
                Assert.Equal((printed ?? 12) - (2 * (penalty ?? 0)), (printed ?? 12) - ScenarioA1NonQualifiedUseCalculator.BreakdownLowering(m));
                Assert.Equal((printed ?? 0) - (penalty ?? 0), (printed ?? 0) - ScenarioA1NonQualifiedUseCalculator.RofLowering(m));
            }

            Assert.Equal(2, ScenarioA1NonQualifiedUseCalculator.CaseHDrm(1));
            if (penalty is > 0 and var captured)
            {
                Assert.Equal(2 * captured, ScenarioA1NonQualifiedUseCalculator.CaseHDrm(captured));
            }
        }
    }

    [Fact]
    public void TheBreakdownFactsCheckForwardsWithTheSameAnswer()
    {
        // Ruling RA21.10: the D3.7 adapter checks of the ordnance and fire calculators.
        foreach (var captured in new[] { false, true })
        {
            foreach (var nonQualified in new[] { false, true })
            {
                foreach (var penalty in Penalties)
                {
                    var old = (captured ? 1 : 0) + (nonQualified ? 1 : 0) != (penalty ?? 0);
                    Assert.Equal(old, ScenarioA1NonQualifiedUseCalculator.Multiple(captured, nonQualified) != (penalty ?? 0));
                }
            }
        }
    }

    private static RuleContract Contract => ScenarioA1RuleContracts.Find("a21.13-crew-served-use")!;

    private static JsonObject Facts(string user, bool captured = false, bool hero = false, bool mortar = false, decimal caliber = 75m) => new()
    {
        ["firerId"] = "g-unit",
        ["user"] = user,
        ["captured"] = captured,
        ["hero"] = hero,
        ["mortar"] = mortar,
        ["caliber"] = caliber,
        ["phase"] = "PFPh",
        ["firedThisGameTurn"] = false,
    };

    [Theory]
    [InlineData("crew", false, false, false, 75, "no-penalty")]
    [InlineData("crew", true, false, false, 75, "penalized")]
    [InlineData("mmc", false, false, false, 75, "penalized")]
    [InlineData("mmc", true, false, false, 75, "penalized-doubled")]
    [InlineData("smc-group", false, false, false, 75, "penalized")]
    [InlineData("lone-smc", false, false, false, 75, "refused")]
    [InlineData("lone-smc", false, true, false, 75, "penalized")]
    [InlineData("lone-smc", false, false, true, 81, "fires-as-light")]
    [InlineData("lone-smc", false, true, false, 105, "refused")]
    public void TheContractGivesEachOutcome(string user, bool captured, bool hero, bool mortar, int caliber, string outcome)
    {
        var verdict = ScenarioA1RuleContracts.Evaluate(Contract, Facts(user, captured, hero, mortar, caliber));
        Assert.Equal(outcome, verdict.Outcome);
    }

    [Fact]
    public void TheContractsReasonsNameThePenaltiesAndAHerosLimits()
    {
        var doubled = ScenarioA1RuleContracts.Evaluate(Contract, Facts("mmc", captured: true));
        Assert.Contains(doubled.Reasons, reason => reason.Text.Contains("B# 4 lower", StringComparison.Ordinal) && reason.Text.Contains("Case H +4", StringComparison.Ordinal));

        var hero = Facts("lone-smc", hero: true, caliber: 105m);
        hero["phase"] = "DFPh";
        var verdict = ScenarioA1RuleContracts.Evaluate(Contract, hero);
        Assert.Equal("penalized", verdict.Outcome);
        Assert.Contains(verdict.Reasons, reason => reason.Rule == "A15.23" && reason.Text.Contains("heroic DRM", StringComparison.Ordinal));
        Assert.Contains(verdict.Reasons, reason => reason.Rule == "A15.23" && reason.Text.Contains("once per Game Turn", StringComparison.Ordinal));

        var refused = ScenarioA1RuleContracts.Evaluate(Contract, Facts("lone-smc"));
        Assert.Equal(("A21.13", 88), (refused.Reasons[0].Rule, refused.Reasons[0].Page));
        hero["phase"] = "PFPh";
        Assert.Equal(("A15.23", 83), (ScenarioA1RuleContracts.Evaluate(Contract, hero).Reasons[0].Rule, ScenarioA1RuleContracts.Evaluate(Contract, hero).Reasons[0].Page));
    }

    [Fact]
    public void TheOpponentKnowsNoneOfTheOwnersFactsSoTheQuestionStaysOpen()
    {
        // Every fact but the phase is the owner's (the Gun and its manning unit may be hidden or concealed): the opponent's view leaves them unknown.
        Assert.All(Contract.Facts.Where(fact => fact.Name != "phase"), fact => Assert.Equal(ContractDisclosure.Owner, fact.Disclosure));
        Assert.Equal(ContractVerdict.NotEvaluated, ScenarioA1RuleContracts.Evaluate(Contract, new JsonObject { ["phase"] = "PFPh" }).Outcome);

        var fraction = Facts("crew");
        fraction["caliber"] = 75.5m;
        Assert.Equal(ContractVerdict.NotEvaluated, ScenarioA1RuleContracts.Evaluate(Contract, fraction).Outcome);
    }
}
