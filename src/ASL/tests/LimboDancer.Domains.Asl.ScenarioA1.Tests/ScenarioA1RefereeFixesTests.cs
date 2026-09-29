using LimboDancer.Domains.Asl.ScenarioA1;
using Xunit;

namespace LimboDancer.Domains.Asl.ScenarioA1.Tests;

/// <summary>
/// The defects and disputed readings of the pass 2 referee review (Scenario A1 Close Combat Review 2026-09-27), each with the rule it
/// rests on: a berserk unit's Original 12 (A10.31), a created leader's DRM (A10.7, A18.12), SMC grouping (A11.14), reinforcing a Melee
/// (A11.15), Withdrawal from Melee (A11.2), odds just above 10 to 1 (A11.11), berserk return (A15.46), heroic Stealth (A15.24), captors
/// the attack breaks (A15.5), and the nationalities whose Heat of Battle exceptions are not reviewed (p. 83).
/// </summary>
public sealed class ScenarioA1RefereeFixesTests
{
    private static readonly ScenarioA1CloseCombatReference CloseCombat = new ScenarioA1CloseCombatPackage().Reference;
    private static readonly ScenarioA1FireReference Fire = new ScenarioA1FirePackage().Reference;

    private static CloseCombatUnit Unit(string id, string definition, string side) =>
        new(id, definition, side, false, false, false, false, false, false, false, false, false, false, false);

    private static CloseCombatFacts Facts(CloseCombatUnit[] units, CloseCombatDeclaration[] attacks, CloseCombatRolls? rolls = null) =>
        new("CCPh", "bd01:G5:0", "open-ground", "german", CloseCombatFacts.Simultaneous, null, units, [], [], attacks, rolls ?? new CloseCombatRolls(null));

    private static CloseCombatRolls Rolls(params int[][] attacks) =>
        new(attacks.Select((dice, index) => (index, dice)).ToDictionary(item => item.index.ToString(System.Globalization.CultureInfo.InvariantCulture),
            item => (IReadOnlyList<int>)item.dice, StringComparer.Ordinal));

    private static CloseCombatDeclaration Attack(string[] attackers, string[] defenders, string? director = null) => new(attackers, defenders) { Director = director };

    private static FireAttack BerserkTarget(string definition, int[] check) => new("PFPh", "phasing", true, "bd01:F5:0", "bd01:G5:0",
        [new FireFirer("ru-1", "defender-squad", "bd01:F5:0", false, false, false, false, false), new FireFirer("ru-2", "defender-squad", "bd01:F5:0", false, false, false, false, false)],
        null, 1, true, new FireLos(false, 0, true, false), null, "open-ground",
        [new FireTarget("de-s", definition, "bd01:G5:0", false, false, false, false, false, false, false) { Berserk = true }], 3,
        new FireRolls([3, 4], null, new Dictionary<string, IReadOnlyList<int>> { ["de-s"] = check }, null)
        {
            WoundSeverity = definition.Contains("leader", StringComparison.Ordinal) ? new Dictionary<string, int> { ["de-s"] = 4 } : null,
        });

    [Fact]
    public void D1ABerserkMmcIsEliminatedAndABerserkLeaderWoundedAsIfWoundedOnAnOriginalTwelve()
    {
        // A10.31: an Original 12 on a unit not subject to breaking eliminates it; a berserk leader is wounded with +1 to the dr.
        var squadResult = ScenarioA1FireCalculator.Resolve(BerserkTarget("attacker-squad", [6, 6]), Fire);
        Assert.True(squadResult.Disposition == FireResolution.Resolved, string.Join("; ", squadResult.Reasons));
        var squad = squadResult.Effects.Single();
        Assert.Equal((true, "eliminated"), (squad.Eliminated, squad.Checks[0].Consequence));
        var leader = ScenarioA1FireCalculator.Resolve(BerserkTarget("attacker-leader-8-1", [6, 6]), Fire).Effects.Single();
        Assert.True(leader.Eliminated);

        // A15.42: an ordinary failure Casualty Reduces a berserk squad; it never breaks and a pass never pins it.
        var reduced = ScenarioA1FireCalculator.Resolve(BerserkTarget("attacker-squad", [6, 5]), Fire).Effects.Single();
        Assert.Equal(("attacker-half-squad", false, true), (reduced.FinalDefinitionId, reduced.Broken, reduced.Berserk));
        var passed = ScenarioA1FireCalculator.Resolve(BerserkTarget("attacker-squad", [4, 4]), Fire).Effects.Single();
        Assert.Equal((false, 10), (passed.Pinned, passed.Checks[0].MoraleLevel));
    }

    [Fact]
    public void D2ACreatedLeadersDrmTakesTheDirectorsPlace()
    {
        // A10.7: leadership modifiers do not combine; A18.12 makes the created leader's mandatory.
        CloseCombatUnit[] units = [Unit("g1", "attacker-squad", "german"), Unit("gl", "attacker-leader-9-2", "german") with { StackedWith = "g1" },
            Unit("r1", "defender-squad", "russian")];
        var result = ScenarioA1CloseCombatCalculator.Resolve(Facts(units, [Attack(["g1", "gl"], ["r1"], "gl")], Rolls([1, 1]) with
        {
            LeaderCreation = new Dictionary<string, int> { ["0"] = 3 },
        }), CloseCombat);
        Assert.Equal(["created-leader:attacker-leader-8-0"], result.Attacks[0].Drm.Select(item => item.Name));
    }

    [Fact]
    public void D4AnUnstackedSmcMayNotCombineWithAnMmc()
    {
        CloseCombatUnit[] units = [Unit("g1", "attacker-squad", "german"), Unit("gl", "attacker-leader-8-1", "german"), Unit("r1", "defender-squad", "russian")];
        Assert.Contains("asl.a1.cc.stacking-outside:0", ScenarioA1CloseCombatCalculator.Precheck(Facts(units, [Attack(["g1", "gl"], ["r1"])]), CloseCombat));
    }

    [Fact]
    public void D5AUnitReinforcingAMeleeMustAttack()
    {
        // A11.15: "New units may advance into a Melee Location but must engage in CC".
        CloseCombatUnit[] units = [Unit("g1", "attacker-squad", "german") with { InMelee = true }, Unit("g2", "attacker-squad", "german") with { Advanced = true },
            Unit("r1", "defender-squad", "russian") with { InMelee = true }];
        Assert.Contains("asl.a1.cc.reinforcement-must-attack", ScenarioA1CloseCombatCalculator.Precheck(Facts(units, [Attack(["g1"], ["r1"])]), CloseCombat));
        Assert.Empty(ScenarioA1CloseCombatCalculator.Precheck(Facts(units, [Attack(["g1", "g2"], ["r1"])]), CloseCombat));
    }

    [Fact]
    public void AWithdrawalIsFromMeleeByAnUnpinnedUnitThatMakesNoAttack()
    {
        // A11.2: only units held in Melee withdraw, never a pinned, berserk, or Disrupted one, and a withdrawing unit makes no attack.
        CloseCombatUnit[] units = [Unit("g1", "attacker-squad", "german") with { InMelee = true, WithdrawingTo = "bd01:G6:0" },
            Unit("r1", "defender-squad", "russian") with { InMelee = true }];
        Assert.Contains("asl.a1.cc.withdrawing-attacker:0", ScenarioA1CloseCombatCalculator.Precheck(Facts(units, [Attack(["g1"], ["r1"])]), CloseCombat));
        var pinned = units.Select(unit => unit.UnitId == "g1" ? unit with { Pinned = true } : unit).ToArray();
        Assert.Contains("asl.a1.cc.withdrawal-outside", ScenarioA1CloseCombatCalculator.Precheck(Facts(pinned, [Attack(["r1"], ["g1"])]), CloseCombat));

        // A withdrawing unit that is neither eliminated nor Reduced withdraws.
        var withdrew = ScenarioA1CloseCombatCalculator.Resolve(Facts(units, [Attack(["r1"], ["g1"])], Rolls([6, 6])), CloseCombat);
        Assert.Equal("bd01:G6:0", withdrew.Effects.Single(item => item.UnitId == "g1").WithdrewTo);
    }

    [Fact]
    public void OddsAboveTenButBelowElevenToOneAreTenToOne()
    {
        // A11.11 (ruling R14.13, replacing R29.16): odds round down to the nearest printed ratio, so 10.5 to 1 is 10-1. Two squads, a HS, and a
        // pinned leader (half his one FP) against a lone leader.
        CloseCombatUnit[] units = [Unit("g1", "attacker-squad", "german"), Unit("g2", "attacker-squad", "german"), Unit("gh", "attacker-half-squad", "german"),
            Unit("gl", "attacker-leader-8-1", "german") with { Pinned = true, StackedWith = "g1" }, Unit("r1", "defender-leader", "russian")];
        Assert.Empty(ScenarioA1CloseCombatCalculator.Precheck(Facts(units, [Attack(["g1", "g2", "gh", "gl"], ["r1"])]), CloseCombat));
        Assert.Equal(("10-1", 12), (CloseCombat.Column(10.5m, 1m).Label, CloseCombat.Column(10.5m, 1m).Kill));
    }

    [Fact]
    public void ABerserkGroupReturnsToNormalOnlyWhenItsOwnAttackClearedTheLocation()
    {
        // A15.46: "it (or the group it attacks with) eliminates all": another group's kill does not count.
        CloseCombatUnit[] units = [Unit("g1", "attacker-squad", "german") with { Berserk = true }, Unit("g2", "attacker-squad", "german"),
            Unit("r1", "defender-half-squad", "russian"), Unit("r2", "defender-half-squad", "russian")];
        var result = ScenarioA1CloseCombatCalculator.Resolve(Facts(units, [Attack(["g1"], ["r1"]), Attack(["g2"], ["r2"])], Rolls([2, 3], [2, 3]) with
        {
            LeaderCreation = null,
        }), CloseCombat);
        Assert.Null(result.Effects.Single(item => item.UnitId == "g1").BerserkEnded);
    }

    [Fact]
    public void AHeroicLeaderIsStealthy()
    {
        // A15.24: a hero is always Stealthy; A15.21: a heroic leader keeps his heroic qualities.
        CloseCombatUnit[] units = [Unit("g1", "attacker-squad", "german") with { Advanced = true }, Unit("gl", "attacker-leader-8-0", "german") with { Heroic = true },
            Unit("r1", "defender-squad", "russian")];
        var ambush = ScenarioA1CloseCombatCalculator.ResolveAmbush(new AmbushFacts("CCPh", "bd01:G5:0", "woods", "german", units,
            new Dictionary<string, int> { ["german"] = 3, ["russian"] = 3 }), CloseCombat);
        Assert.Contains(ambush.Sides.Single(item => item.Side == "german").Drm, item => item.Name == "stealthy");
    }

    [Fact]
    public void ACaptorTheAttackBreaksIsNoCaptor()
    {
        // A15.5: the FPF firer surrenders only to a Good Order captor; the target its 2KIA eliminated in the same attack is not one. The
        // Original 2 of the IFT DR is the firer's NMC (A8.31), which calls for its Heat of Battle DR: 6+6 +2 Russian is a Surrender.
        var attack = new FireAttack("MPh", "non-phasing", true, "bd01:F5:0", "bd01:G5:0",
            [new FireFirer("ru-1", "defender-squad", "bd01:F5:0", false, false, false, false, false)
            {
                FinalFireMarked = true,
                KnownEnemyInLos = true,
                Captors = ["de-s"],
            }], null, 1, true, new FireLos(false, 0, true, false), null, "open-ground",
            [new FireTarget("de-s", "attacker-squad", "bd01:G5:0", false, false, false, false, false, false, false) { KnownEnemyInLos = true, Captors = [] }], 3,
            new FireRolls([1, 1], new Dictionary<string, int> { ["de-s"] = 1 }, null, null)
            {
                HeatOfBattle = new Dictionary<string, IReadOnlyList<int>> { ["ru-1"] = [6, 6] },
            })
        {
            FireKind = ScenarioA1FireCalculator.FinalProtectiveFire,
            TargetMovement = new FireMovement(false),
            FiringSideElr = 2,
        };
        var result = ScenarioA1FireCalculator.Resolve(attack, Fire);
        var heat = result.FirerEffects!.Single().HeatOfBattle;
        Assert.Equal((HeatOfBattleOutcome.Surrender, 0), (heat!.Result, heat.Captors!.Count));
    }

    [Fact]
    public void D12TheJapaneseAreRefused()
    {
        // Backlog pass 15 (ruling R15.13) admits the Italians; the Japanese stay refused.
        var japanese = Fire.Definitions["attacker-squad"] with
        {
            Nationality = "japanese"
        };
        Assert.Equal("asl.a1.hob.nationality-unreviewed:attacker-squad",
            ScenarioA1HeatOfBattle.Resolve(japanese, false, null, false, [3, 3], Fire.Definitions, true, []).Undecided);
    }
}
