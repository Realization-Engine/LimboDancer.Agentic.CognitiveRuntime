using System.Globalization;
using LimboDancer.Domains.Asl.Rules;
using Xunit;

namespace LimboDancer.Domains.Asl.Rules.Tests;

/// <summary>
/// The A20.54 claim, the prisoners' escape (A20.55, p. 88; A25.63, p. 97; A25.82, p. 98; G1.621, p. 280; rulings RA20.6 to RA20.8, RA20.10 to RA20.12): the
/// calculator's members, the CC calculator's forwards equal to their inline tests, the two readings of <see cref="CloseCombatFacts.PrisonerEscapeReading"/>,
/// and the escape contract.
/// </summary>
public sealed class ScenarioA1PrisonerEscapeTests
{
    private static readonly ScenarioA1CloseCombatReference Reference = new ScenarioA1CloseCombatPackage().Reference;

    [Theory]
    [InlineData("italian", "russian", null, "A25.63")]
    [InlineData("japanese", "american", null, "G1.621")]
    [InlineData("hungarian", "russian", false, "A25.82")]
    [InlineData("hungarian", "russian", null, "A25.82")]
    [InlineData("romanian", "german", true, "A25.82")]
    [InlineData("axis-minor", "british", true, "A25.82")]
    public void ItaliansJapaneseAndAxisMinorsAwayFromHomeOrFromRussiansDoNotEscape(string nationality, string captor, bool? atHome, string rule)
    {
        var refusal = ScenarioA1PrisonerCalculator.EscapeRefusal("p", nationality, captor, true, atHome);
        Assert.NotNull(refusal);
        Assert.Contains(rule, refusal, StringComparison.Ordinal);
        Assert.Equal(rule, ScenarioA1PrisonerCalculator.Escape(new PrisonerEscapeFacts("p", nationality, captor, true, atHome, true, false)).RefusalRule);
    }

    [Theory]
    [InlineData("german", "russian", null)]
    [InlineData("russian", "german", null)]
    [InlineData("hungarian", "russian", true)]
    [InlineData("axis-minor", "russian", true)]
    public void OtherPrisonersAndAxisMinorsAtHomeAgainstRussiansMayEscape(string nationality, string captor, bool? atHome)
    {
        Assert.Null(ScenarioA1PrisonerCalculator.EscapeRefusal("p", nationality, captor, true, atHome));
    }

    [Fact]
    public void OnlyInfantryOfAKnownNationalityEscapes()
    {
        Assert.Contains("only Infantry", ScenarioA1PrisonerCalculator.EscapeRefusal("p", "russian", "german", false, null), StringComparison.Ordinal);
        Assert.Contains("not known", ScenarioA1PrisonerCalculator.EscapeRefusal("p", null, "german", true, null), StringComparison.Ordinal);
        // An Axis Minor at home with a captor of unknown nationality is refused: an unknown fact never allows.
        Assert.NotNull(ScenarioA1PrisonerCalculator.EscapeRefusal("p", "hungarian", null, true, true));
    }

    [Fact]
    public void TheGuardTheNtcTheTargetsAndSuccessFollowA2055()
    {
        foreach (var broken in new[] { false, true })
        {
            foreach (var melee in new[] { false, true })
            {
                // The CC calculator's old clause, copied: the Guard broken or in Melee.
                Assert.Equal(broken || melee, ScenarioA1PrisonerCalculator.MayAttackGuard(broken, melee));
            }
        }

        // The old NTC skip and test, copied: no NTC when the Guard is in Melee; passed at most the Morale.
        Assert.True(ScenarioA1PrisonerCalculator.EscapeNtcRequired(false));
        Assert.False(ScenarioA1PrisonerCalculator.EscapeNtcRequired(true));
        Assert.True(ScenarioA1PrisonerCalculator.EscapeNtcPassed(7, 7));
        Assert.False(ScenarioA1PrisonerCalculator.EscapeNtcPassed(8, 7));

        Assert.Null(ScenarioA1PrisonerCalculator.EscapeTargetsRefusal(true, false));
        Assert.Null(ScenarioA1PrisonerCalculator.EscapeTargetsRefusal(false, true));
        Assert.Contains("eliminate their Guard", ScenarioA1PrisonerCalculator.EscapeTargetsRefusal(false, false), StringComparison.Ordinal);

        Assert.True(ScenarioA1PrisonerCalculator.MayWithdrawFromMelee(true, true));
        Assert.False(ScenarioA1PrisonerCalculator.MayWithdrawFromMelee(true, false));
        Assert.False(ScenarioA1PrisonerCalculator.MayWithdrawFromMelee(false, true));

        Assert.True(ScenarioA1PrisonerCalculator.EscapeSucceeds(true, false, true));
        Assert.True(ScenarioA1PrisonerCalculator.EscapeSucceeds(false, true, true));
        Assert.True(ScenarioA1PrisonerCalculator.EscapeSucceeds(false, false, false));
        Assert.False(ScenarioA1PrisonerCalculator.EscapeSucceeds(false, false, true));
        foreach (var infiltrated in new[] { false, true })
        {
            foreach (var left in new[] { false, true })
            {
                // The old success test, copied: Infiltration or no enemy non-prisoner left.
                Assert.Equal(infiltrated || !left, ScenarioA1PrisonerCalculator.EscapeSucceeds(infiltrated, withdrew: false, left));
            }
        }

        var refusedGuard = ScenarioA1PrisonerCalculator.Escape(new PrisonerEscapeFacts("p", "russian", "german", true, null, false, false));
        Assert.False(refusedGuard.MayAttack);
        Assert.Equal("A20.55", refusedGuard.RefusalRule);
        Assert.True(ScenarioA1PrisonerCalculator.Escape(new PrisonerEscapeFacts("p", "russian", "german", true, null, true, false)).NtcRequired);
        Assert.False(ScenarioA1PrisonerCalculator.Escape(new PrisonerEscapeFacts("p", "russian", "german", true, null, false, true)).NtcRequired);
    }

    private static CloseCombatUnit Unit(string id, string definition, string side) =>
        new(id, definition, side, false, false, false, false, false, false, false, false, false, false, false);

    private static CloseCombatUnit Prisoner(string id, string guard) => Unit(id, "defender-half-squad", "russian") with
    {
        Captured = true,
        GuardId = guard,
        Unarmed = true
    };

    private static CloseCombatFacts Facts(CloseCombatUnit[] units, CloseCombatDeclaration[] attacks, CloseCombatRolls? rolls, string round, bool? reading) =>
        new("CCPh", "bd01:G5:0", "open-ground", "german", round, null, units, [], [], attacks, rolls ?? new CloseCombatRolls(null))
        {
            PrisonerEscapeReading = reading,
        };

    private static CloseCombatRolls Rolls(params int[][] attacks) =>
        new(attacks.Select((dice, index) => (index, dice)).ToDictionary(item => item.index.ToString(CultureInfo.InvariantCulture),
            item => (IReadOnlyList<int>)item.dice, StringComparer.Ordinal));

    private static CloseCombatUnitEffect Effect(CloseCombatResolution result, string id) => result.Effects.Single(item => item.UnitId == id);

    [Fact]
    public void UnderTheReadingAPrisonerMayAttackOthersOnceAnEarlierAttackEliminatesItsGuard()
    {
        // A broken German Guard holds two Russian HS; a German HS stands beside it. The first prisoner attacks the Guard, the second the HS alone.
        CloseCombatUnit[] units = [Unit("g1", "attacker-half-squad", "german") with { Broken = true }, Unit("g2", "attacker-half-squad", "german"), Prisoner("rp1", "g1"),
            Prisoner("rp2", "g1")];
        CloseCombatDeclaration[] attacks = [new(["rp1"], ["g1"]), new(["rp2"], ["g2"])];
        var ntc = new Dictionary<string, IReadOnlyList<int>>(StringComparer.Ordinal) { ["rp1"] = [1, 1], ["rp2"] = [1, 1] };

        // Null: every escape attack takes in the Guard, as before.
        Assert.Contains("asl.a1.cc.escape-outside:1", ScenarioA1CloseCombatCalculator.Precheck(Facts(units, attacks, null, CloseCombatFacts.PrisonersRound, null), Reference));
        Assert.DoesNotContain("asl.a1.cc.escape-outside:1", ScenarioA1CloseCombatCalculator.Precheck(Facts(units, attacks, null, CloseCombatFacts.PrisonersRound, true), Reference));
        // A second attack whose Guard no earlier attack takes in stays outside.
        Assert.Contains("asl.a1.cc.escape-outside:0", ScenarioA1CloseCombatCalculator.Precheck(Facts(units, [new(["rp2"], ["g2"]), new(["rp1"], ["g1"])], null,
            CloseCombatFacts.PrisonersRound, true), Reference));

        // The first attack eliminates the Guard: the second is made, and both prisoners attacked.
        var made = ScenarioA1CloseCombatCalculator.Resolve(Facts(units, attacks, Rolls([1, 2], [6, 6]) with
        {
            EscapeNtc = ntc
        }, CloseCombatFacts.PrisonersRound, true), Reference);
        Assert.Equal(CloseCombatResolution.Resolved, made.Disposition);
        Assert.True(Effect(made, "g1").Eliminated);
        Assert.Equal(2, made.Attacks.Count);
        Assert.True(Effect(made, "rp2").Escaped);

        // The first attack leaves the Guard: the second is held back, and its prisoner stays guarded.
        var held = ScenarioA1CloseCombatCalculator.Resolve(Facts(units, attacks, Rolls([6, 6], [1, 2]) with
        {
            EscapeNtc = ntc
        }, CloseCombatFacts.PrisonersRound, true), Reference);
        Assert.Equal(CloseCombatResolution.Resolved, held.Disposition);
        Assert.False(Effect(held, "g1").Eliminated);
        Assert.Single(held.Attacks);
        Assert.True(Effect(held, "rp1").Escaped);
        Assert.Null(Effect(held, "rp2").Escaped);
    }

    [Fact]
    public void UnderTheReadingAPrisonerInMeleeWithItsGuardMayWithdrawAndEscapes()
    {
        // A20.55's example: the Guard is in Melee since an earlier CCPh; the Russian HS withdraws, and the Guard attacks it at -2 and misses.
        CloseCombatUnit[] units = [Unit("g1", "attacker-squad", "german") with { InMelee = true }, Prisoner("rp", "g1") with { WithdrawingTo = "bd01:G6:0" }];
        CloseCombatDeclaration[] attacks = [new(["g1"], ["rp"])];

        var older = ScenarioA1CloseCombatCalculator.Precheck(Facts(units, attacks, null, CloseCombatFacts.Simultaneous, null), Reference);
        Assert.Contains("asl.a1.cc.withdrawal-outside", older);
        Assert.Contains("asl.a1.cc.prisoner-outside:0", older);

        var reading = ScenarioA1CloseCombatCalculator.Precheck(Facts(units, attacks, null, CloseCombatFacts.Simultaneous, true), Reference);
        Assert.DoesNotContain("asl.a1.cc.withdrawal-outside", reading);
        Assert.DoesNotContain("asl.a1.cc.prisoner-outside:0", reading);

        var result = ScenarioA1CloseCombatCalculator.Resolve(Facts(units, attacks, Rolls([6, 6]), CloseCombatFacts.Simultaneous, true), Reference);
        Assert.Equal(CloseCombatResolution.Resolved, result.Disposition);
        Assert.Contains(result.Attacks[0].Defending[0].Drm, item => item.Name == "vs-withdrawing");
        Assert.Equal("bd01:G6:0", Effect(result, "rp").WithdrewTo);
        Assert.True(Effect(result, "rp").Escaped);

        // A Guard not in Melee leaves its prisoner no Withdrawal.
        CloseCombatUnit[] calm = [units[0] with { InMelee = false }, units[1]];
        Assert.Contains("asl.a1.cc.withdrawal-outside", ScenarioA1CloseCombatCalculator.Precheck(Facts(calm, attacks, null, CloseCombatFacts.Simultaneous, true), Reference));
    }

    [Fact]
    public void UnderTheReadingThePrisonersRoundCarriesNoPrisonerWithdrawal()
    {
        // RA20.13 (A11.2, A20.55): the Withdrawal is resolved in a round after the prisoners' round, where the attacks against the prisoner are made.
        Assert.False(ScenarioA1PrisonerCalculator.WithdrawalInRound(true));
        Assert.True(ScenarioA1PrisonerCalculator.WithdrawalInRound(false));

        // The Guard is in Melee; one prisoner attacks it, the other withdraws.
        CloseCombatUnit[] units = [Unit("g1", "attacker-squad", "german") with { InMelee = true }, Prisoner("rp1", "g1"), Prisoner("rp2", "g1") with { WithdrawingTo = "bd01:G6:0" }];
        CloseCombatDeclaration[] escape = [new(["rp1"], ["g1"])];

        // Null: no new refusal; the old checks stand as they were.
        var older = ScenarioA1CloseCombatCalculator.Precheck(Facts(units, escape, null, CloseCombatFacts.PrisonersRound, null), Reference);
        Assert.DoesNotContain("asl.a1.cc.prisoner-withdrawal-round", older);
        Assert.Contains("asl.a1.cc.withdrawal-outside", older);

        // Under the reading: the prisoners' round refuses the Withdrawal, so no prisoner moves away from its Guard unfreed.
        var reading = ScenarioA1CloseCombatCalculator.Precheck(Facts(units, escape, null, CloseCombatFacts.PrisonersRound, true), Reference);
        Assert.Contains("asl.a1.cc.prisoner-withdrawal-round", reading);
        Assert.DoesNotContain("asl.a1.cc.withdrawal-outside", reading);
        Assert.NotEqual(CloseCombatResolution.Resolved, ScenarioA1CloseCombatCalculator.Resolve(Facts(units, escape, Rolls([1, 2]), CloseCombatFacts.PrisonersRound, true),
            Reference).Disposition);

        // The prisoners' round without the Withdrawal is made; the later round carries it, and the prisoner escapes there.
        CloseCombatUnit[] staying = [units[0], units[1], units[2] with { WithdrawingTo = null }];
        Assert.DoesNotContain("asl.a1.cc.prisoner-withdrawal-round",
            ScenarioA1CloseCombatCalculator.Precheck(Facts(staying, escape, null, CloseCombatFacts.PrisonersRound, true), Reference));
        Assert.DoesNotContain("asl.a1.cc.prisoner-withdrawal-round",
            ScenarioA1CloseCombatCalculator.Precheck(Facts([units[0], units[2]], [new(["g1"], ["rp2"])], null, CloseCombatFacts.Simultaneous, true), Reference));
    }

    [Fact]
    public void UnderTheReadingAnAttackWhoseGuardAttackIsForfeitedIsHeldBack()
    {
        // The broken Guard holds two Russian HS beside a German HS. The prisoner attacking the Guard fails its NTC, so that attack is forfeited; the
        // other prisoner's attack on the HS alone is held back, and neither prisoner escapes.
        CloseCombatUnit[] units = [Unit("g1", "attacker-half-squad", "german") with { Broken = true }, Unit("g2", "attacker-half-squad", "german"), Prisoner("rp1", "g1"),
            Prisoner("rp2", "g1")];
        CloseCombatDeclaration[] attacks = [new(["rp1"], ["g1"]), new(["rp2"], ["g2"])];
        var result = ScenarioA1CloseCombatCalculator.Resolve(Facts(units, attacks, Rolls([1, 2], [1, 2]) with
        {
            EscapeNtc = new Dictionary<string, IReadOnlyList<int>>(StringComparer.Ordinal) { ["rp1"] = [6, 6], ["rp2"] = [1, 1] },
        }, CloseCombatFacts.PrisonersRound, true), Reference);
        Assert.Equal(CloseCombatResolution.Resolved, result.Disposition);
        Assert.Empty(result.Attacks);
        Assert.False(Effect(result, "g2").Eliminated);
        Assert.Null(Effect(result, "rp1").Escaped);
        Assert.Null(Effect(result, "rp2").Escaped);
    }

    [Fact]
    public void TheReadingSwitchIsOmittedFromTheJsonWhenNull()
    {
        CloseCombatUnit[] units = [Unit("g1", "attacker-half-squad", "german"), Prisoner("rp", "g1")];
        var json = System.Text.Json.JsonSerializer.Serialize(Facts(units, [], null, CloseCombatFacts.PrisonersRound, null));
        Assert.DoesNotContain("prisonerEscapeReading", json, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("prisonerEscapeReading", System.Text.Json.JsonSerializer.Serialize(Facts(units, [], null, CloseCombatFacts.PrisonersRound, true)),
            StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void TheForwardsKeepTheOldEscapeRound()
    {
        // A Good Order Guard with no Melee: outside; broken: a NTC of 8 against Morale 7 fails, of 6 passes and the prisoner attacks.
        CloseCombatUnit[] calm = [Unit("g1", "attacker-half-squad", "german"), Prisoner("rp", "g1")];
        CloseCombatDeclaration[] escape = [new(["rp"], ["g1"])];
        foreach (var reading in new bool?[] { null, true })
        {
            Assert.Contains("asl.a1.cc.escape-outside:0", ScenarioA1CloseCombatCalculator.Precheck(Facts(calm, escape, null, CloseCombatFacts.PrisonersRound, reading), Reference));
            CloseCombatUnit[] units = [calm[0] with { Broken = true }, calm[1]];
            var failed = ScenarioA1CloseCombatCalculator.Resolve(Facts(units, escape, new CloseCombatRolls(null)
            {
                EscapeNtc = new Dictionary<string, IReadOnlyList<int>>(StringComparer.Ordinal) { ["rp"] = [4, 4] },
            }, CloseCombatFacts.PrisonersRound, reading), Reference);
            Assert.False(Assert.Single(failed.EscapeChecks!).Passed);
            Assert.Empty(failed.Attacks);

            // In Melee no NTC is asked.
            CloseCombatUnit[] melee = [calm[0] with { InMelee = true }, calm[1]];
            var noNtc = ScenarioA1CloseCombatCalculator.Resolve(Facts(melee, escape, Rolls([1, 2]), CloseCombatFacts.PrisonersRound, reading), Reference);
            Assert.Null(noNtc.EscapeChecks);
            Assert.True(Effect(noNtc, "g1").Eliminated);
            Assert.True(Effect(noNtc, "rp").Escaped);
        }
    }

    [Theory]
    [InlineData("russian", "german", false, true, false, "escape-after-ntc")]
    [InlineData("russian", "german", false, false, true, "escape-no-ntc")]
    [InlineData("russian", "german", false, false, false, "refused-guard")]
    [InlineData("italian", "russian", false, true, false, "refused-nationality")]
    [InlineData("japanese", "american", false, true, false, "refused-nationality")]
    [InlineData("hungarian", "german", true, true, false, "refused-nationality")]
    [InlineData("hungarian", "russian", true, true, false, "escape-after-ntc")]
    public void TheEscapeContractGivesTheVerdict(string nationality, string captor, bool atHome, bool guardBroken, bool melee, string outcome)
    {
        var facts = $$"""{"prisonerId":"p","nationality":"{{nationality}}","captorNationality":"{{captor}}","infantry":true,"axisMinorAtHome":{{Json(atHome)}},"guardBroken":{{Json(guardBroken)}},"meleeInLocation":{{Json(melee)}}}""";
        Assert.Contains($"\"outcome\":\"{outcome}\"", ScenarioA1RuleContracts.Evaluate("a20.55-prisoner-escape", facts), StringComparison.Ordinal);
    }

    [Fact]
    public void TheEscapeContractRefusesNonInfantryReadsAnUnknownHomeAsNotAndIsNotEvaluatedWithoutItsFacts()
    {
        Assert.Contains("\"outcome\":\"refused-not-infantry\"", ScenarioA1RuleContracts.Evaluate("a20.55-prisoner-escape",
            """{"prisonerId":"p","nationality":"russian","captorNationality":"german","infantry":false,"axisMinorAtHome":null,"guardBroken":true,"meleeInLocation":false}"""), StringComparison.Ordinal);
        Assert.Contains("\"outcome\":\"refused-nationality\"", ScenarioA1RuleContracts.Evaluate("a20.55-prisoner-escape",
            """{"prisonerId":"p","nationality":"hungarian","captorNationality":"russian","infantry":true,"axisMinorAtHome":null,"guardBroken":true,"meleeInLocation":false}"""), StringComparison.Ordinal);
        Assert.Contains("\"outcome\":\"not-evaluated\"", ScenarioA1RuleContracts.Evaluate("a20.55-prisoner-escape",
            """{"prisonerId":"p","nationality":"russian","infantry":true,"guardBroken":true,"meleeInLocation":false}"""), StringComparison.Ordinal);
    }

    private static string Json(bool value) => value ? "true" : "false";
}
