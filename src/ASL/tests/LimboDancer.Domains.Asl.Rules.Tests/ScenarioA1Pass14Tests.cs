using System.Globalization;
using LimboDancer.Domains.Asl.Rules;
using Xunit;

namespace LimboDancer.Domains.Asl.Rules.Tests;

/// <summary>
/// The backlog pass 14 in the Close Combat package (rulings R14.1 to R14.13): Hand-to-Hand, concealment and TI in CC, capture attempts, prisoners'
/// escape and rearming, Infiltration, overstacking, the BPV of Field Promotion, and a walk proving each new kind of round decided before any roll.
/// </summary>
public sealed class ScenarioA1Pass14Tests
{
    private static readonly ScenarioA1CloseCombatReference Reference = new ScenarioA1CloseCombatPackage().Reference;

    private static CloseCombatUnit Unit(string id, string definition, string side) =>
        new(id, definition, side, false, false, false, false, false, false, false, false, false, false, false);

    private static CloseCombatFacts Facts(CloseCombatUnit[] units, CloseCombatDeclaration[] attacks, CloseCombatRolls? rolls = null,
        string round = CloseCombatFacts.Simultaneous, string? ambusher = null) =>
        new("CCPh", "bd01:G5:0", "open-ground", "german", round, ambusher, units, [], [], attacks, rolls ?? new CloseCombatRolls(null));

    private static CloseCombatRolls Rolls(params int[][] attacks) =>
        new(attacks.Select((dice, index) => (index, dice)).ToDictionary(item => item.index.ToString(CultureInfo.InvariantCulture),
            item => (IReadOnlyList<int>)item.dice, StringComparer.Ordinal));

    private static CloseCombatDeclaration Attack(string[] attackers, string[] defenders) => new(attackers, defenders);

    private static CloseCombatUnitEffect Effect(CloseCombatResolution result, string id) => result.Effects.Single(item => item.UnitId == id);

    [Fact]
    public void HandToHandUsesTheRedKillNumber()
    {
        // A11.11, J2.31 (ruling R14.1): 4-4 is 1-1, black 5 and red 7; a DR of 6 kills in Hand-to-Hand and does nothing otherwise.
        CloseCombatUnit[] units = [Unit("g1", "attacker-squad", "german"), Unit("r1", "defender-squad", "russian")];
        var normal = ScenarioA1CloseCombatCalculator.Resolve(Facts(units, [Attack(["g1"], ["r1"])], Rolls([3, 3])), Reference);
        var hand = ScenarioA1CloseCombatCalculator.Resolve(Facts(units, [Attack(["g1"], ["r1"])], Rolls([3, 3])) with
        {
            HandToHand = true
        }, Reference);
        Assert.Equal(5, normal.Attacks[0].KillNumber);
        Assert.Equal(7, hand.Attacks[0].KillNumber);
        Assert.False(Effect(normal, "r1").Eliminated);
        Assert.True(Effect(hand, "r1").Eliminated);
    }

    [Fact]
    public void AConcealedDefenderHalvesTheAttackAndAConcealedAttackerLosesItsConcealment()
    {
        // A11.19 (ruling R14.2): 4 FP against a concealed 4-4-7 is 2-4, 1-2; the concealed Russian attacking loses its "?".
        CloseCombatUnit[] units = [Unit("g1", "attacker-squad", "german"), Unit("r1", "defender-squad", "russian") with { Concealed = true }];
        var result = ScenarioA1CloseCombatCalculator.Resolve(Facts(units, [Attack(["g1"], ["r1"]), Attack(["r1"], ["g1"])], Rolls([6, 6], [6, 6])), Reference);
        Assert.Equal("1-2", result.Attacks[0].Odds);
        Assert.Contains(result.Attacks[0].FirepowerModifiers, item => item.Name == "vs-concealed");
        Assert.True(Effect(result, "r1").ConcealmentLost);

        // Declining to attack keeps the "?".
        var declined = ScenarioA1CloseCombatCalculator.Resolve(Facts(units, [Attack(["g1"], ["r1"])], Rolls([6, 6])), Reference);
        Assert.Null(Effect(declined, "r1").ConcealmentLost);
    }

    [Fact]
    public void AnAmbusherKeepsItsConcealmentWhenItsAttackClearsItsTarget()
    {
        // A11.4, A12.14 (ruling R14.2): the ambusher's attack that eliminates its target keeps the "?"; one that fails loses it.
        CloseCombatUnit[] units = [Unit("g1", "attacker-squad", "german") with { Concealed = true }, Unit("r1", "defender-half-squad", "russian")];
        var clean = ScenarioA1CloseCombatCalculator.Resolve(Facts(units, [Attack(["g1"], ["r1"])], Rolls([1, 2]), CloseCombatFacts.AmbusherRound, "german"), Reference);
        Assert.True(Effect(clean, "r1").Eliminated);
        Assert.Null(Effect(clean, "g1").ConcealmentLost);
        var missed = ScenarioA1CloseCombatCalculator.Resolve(Facts(units, [Attack(["g1"], ["r1"])], Rolls([6, 6]), CloseCombatFacts.AmbusherRound, "german"), Reference);
        Assert.True(Effect(missed, "g1").ConcealmentLost);
    }

    [Fact]
    public void AnAmbushCanOccurWithAConcealedUnitInOpenGroundAndConcealmentGivesMinusTwo()
    {
        // A11.4 (ruling R14.2): an advance into CC against a concealed unit in any terrain; -2 to the concealed force's dr.
        CloseCombatUnit[] units = [Unit("g1", "attacker-squad", "german") with { Advanced = true }, Unit("r1", "defender-squad", "russian") with { Concealed = true }];
        Assert.True(ScenarioA1CloseCombatCalculator.AmbushPossible("open-ground", units));
        Assert.False(ScenarioA1CloseCombatCalculator.AmbushPossible("open-ground", [units[0], units[1] with { Concealed = false }]));
        Assert.True(ScenarioA1CloseCombatCalculator.AmbushPossible("open-ground", [units[0] with { Advanced = false }, units[1]], hiddenPlaced: true));
        var ambush = ScenarioA1CloseCombatCalculator.ResolveAmbush(new AmbushFacts("CCPh", "bd01:G5:0", "open-ground", "german", units,
            new Dictionary<string, int>(StringComparer.Ordinal) { ["german"] = 4, ["russian"] = 3 }), Reference);
        Assert.Equal(1, ambush.Sides.Single(item => item.Side == "russian").FinalDr);
        Assert.Equal("russian", ambush.Ambusher);
    }

    [Fact]
    public void TiUnitsTakePlusOneAttackingAndMinusOneAttacked()
    {
        // A4.8 (ruling R14.3).
        CloseCombatUnit[] units = [Unit("g1", "attacker-squad", "german") with { Ti = true }, Unit("r1", "defender-squad", "russian") with { Ti = true }];
        var result = ScenarioA1CloseCombatCalculator.Resolve(Facts(units, [Attack(["g1"], ["r1"])], Rolls([2, 3])), Reference);
        Assert.Contains(result.Attacks[0].Drm, item => item.Name == "ti:g1" && item.Value == 1);
        Assert.Contains(result.Attacks[0].Defending[0].Drm, item => item.Name == "vs-ti" && item.Value == -1);
    }

    [Fact]
    public void ACaptureAttemptCapturesInsteadOfEliminatingAndPlacesTheGuard()
    {
        // A20.22, A20.5 (ruling R14.4): +1 DRM; below the Kill Number every defender is captured and placed with the attacker.
        CloseCombatUnit[] units = [Unit("g1", "attacker-squad", "german"), Unit("r1", "defender-half-squad", "russian")];
        var attack = Attack(["g1"], ["r1"]) with
        {
            Capture = true
        };
        var result = ScenarioA1CloseCombatCalculator.Resolve(Facts(units, [attack], Rolls([2, 3])), Reference);
        Assert.Contains(result.Attacks[0].Defending[0].Drm, item => item.Name == "capture" && item.Value == 1);
        var effect = Effect(result, "r1");
        Assert.True(effect.Captured);
        Assert.False(effect.Eliminated);
        Assert.Equal("g1", effect.GuardId);

        // A19.35: -1 instead against an Inexperienced (Conscript) unit.
        CloseCombatUnit[] green = [units[0], Unit("r1", "defender-conscript-half-squad", "russian")];
        var inexperienced = ScenarioA1CloseCombatCalculator.Resolve(Facts(green, [attack], Rolls([2, 3])), Reference);
        Assert.Contains(inexperienced.Attacks[0].Defending[0].Drm, item => item.Name == "capture-vs-inexperienced" && item.Value == -1);
    }

    [Fact]
    public void AtTheKillNumberTheDefenderChoosesAndASquadGivesUpOneHalfSquad()
    {
        // A20.22 EX (ruling R14.4): one unit of the defender's choice at the Kill Number; a squad is exchanged for two HS, one captured.
        CloseCombatUnit[] units = [Unit("g1", "attacker-squad", "german"), Unit("g2", "attacker-squad", "german"), Unit("r1", "defender-squad", "russian"),
            Unit("r2", "defender-half-squad", "russian")];
        var undecided = ScenarioA1CloseCombatCalculator.Precheck(Facts(units, [Attack(["g1", "g2"], ["r1", "r2"]) with { Capture = true }]), Reference);
        Assert.Contains("asl.a1.cc.capture-choice-undecided:0", undecided);

        // 8-6 is 1-1, Kill 5; +1 capture: an Original 4 is a Final 5.
        var attack = Attack(["g1", "g2"], ["r1", "r2"]) with
        {
            Capture = true,
            Yield = ["r1", "r2"]
        };
        var result = ScenarioA1CloseCombatCalculator.Resolve(Facts(units, [attack], Rolls([2, 2])), Reference);
        Assert.True(Effect(result, "r1").Captured);
        Assert.True(Effect(result, "r1").CapturedHalf);
        Assert.Null(Effect(result, "r2").Captured);
    }

    [Fact]
    public void BerserkUnitsNeitherCaptureNorAreCaptured()
    {
        // A20.2 (ruling R14.4).
        CloseCombatUnit[] units = [Unit("g1", "attacker-squad", "german") with { Berserk = true }, Unit("r1", "defender-squad", "russian")];
        Assert.Contains("asl.a1.cc.capture-outside:0",
            ScenarioA1CloseCombatCalculator.Precheck(Facts(units, [Attack(["g1"], ["r1"]) with { Capture = true }]), Reference));
    }

    [Fact]
    public void ASideWipedOutInASimultaneousRoundCapturesNoOne()
    {
        // A20.221 (ruling R14.4): the German HS captures the Russian HS but is itself eliminated, so the capture is void.
        CloseCombatUnit[] units = [Unit("g1", "attacker-half-squad", "german"), Unit("r1", "defender-half-squad", "russian")];
        var result = ScenarioA1CloseCombatCalculator.Resolve(Facts(units, [Attack(["g1"], ["r1"]) with { Capture = true }, Attack(["r1"], ["g1"])],
            Rolls([1, 2], [1, 2])), Reference);
        Assert.True(Effect(result, "g1").Eliminated);
        Assert.Null(Effect(result, "r1").Captured);
        Assert.False(Effect(result, "r1").Eliminated);
    }

    [Fact]
    public void AGuardWithoutCapacityFreesTheCapturedUnitAsUnarmed()
    {
        // A20.51 (ruling R14.4): a leader (US# 1) guards up to five US#; a squad and a HS already guarded fill it, and a HS is freed as Unarmed.
        CloseCombatUnit[] units = [Unit("gl", "attacker-leader-8-1", "german"), Unit("rp", "defender-squad", "russian") with { Captured = true, GuardId = "gl" },
            Unit("rq", "defender-half-squad", "russian") with { Captured = true, GuardId = "gl" }, Unit("r1", "defender-half-squad", "russian")];
        var result = ScenarioA1CloseCombatCalculator.Resolve(Facts(units, [Attack(["gl"], ["r1"]) with { Capture = true }], Rolls([1, 1])), Reference);
        var effect = Effect(result, "r1");
        Assert.True(effect.Captured);
        Assert.Null(effect.GuardId);
        Assert.Contains("freed-unarmed", effect.Events);
    }

    [Fact]
    public void AGuardAttackingNonPrisonersIsHalved()
    {
        // A20.52 (ruling R14.5).
        CloseCombatUnit[] units = [Unit("g1", "attacker-squad", "german"), Unit("rp", "defender-half-squad", "russian") with { Captured = true, GuardId = "g1", Unarmed = true },
            Unit("r1", "defender-squad", "russian")];
        var result = ScenarioA1CloseCombatCalculator.Resolve(Facts(units, [Attack(["g1"], ["r1"])], Rolls([6, 6])), Reference);
        Assert.Equal(2m, result.Attacks[0].AttackFirepower);
        Assert.Contains(result.Attacks[0].FirepowerModifiers, item => item.Name == "guarding:g1");
        Assert.Contains("asl.a1.cc.prisoner-outside:0",
            ScenarioA1CloseCombatCalculator.Precheck(Facts(units, [Attack(["g1"], ["rp"])]), Reference));
    }

    [Fact]
    public void PrisonersEscapeAfterANtcOnlyAgainstABrokenGuard()
    {
        // A20.55 (ruling R14.6): the Guard is Good Order, so no escape; broken, each prisoner takes a NTC and attacks at (1) FP.
        var guard = Unit("g1", "attacker-half-squad", "german");
        var prisoner = Unit("rp", "defender-squad", "russian") with
        {
            Captured = true,
            GuardId = "g1",
            Unarmed = true
        };
        CloseCombatDeclaration[] escape = [Attack(["rp"], ["g1"])];
        Assert.Contains("asl.a1.cc.escape-outside:0",
            ScenarioA1CloseCombatCalculator.Precheck(Facts([guard, prisoner], escape, round: CloseCombatFacts.PrisonersRound), Reference));

        CloseCombatUnit[] units = [guard with { Broken = true }, prisoner];
        var asked = ScenarioA1CloseCombatCalculator.Resolve(Facts(units, escape, round: CloseCombatFacts.PrisonersRound), Reference);
        Assert.Equal(["asl.a1.cc.roll-missing:escapeNtc:rp"], asked.Reasons);

        // A NTC of 8 against Morale 7 fails: the prisoner does not attack and stays guarded.
        var failed = ScenarioA1CloseCombatCalculator.Resolve(Facts(units, escape, new CloseCombatRolls(null)
        {
            EscapeNtc = new Dictionary<string, IReadOnlyList<int>>(StringComparer.Ordinal) { ["rp"] = [4, 4] },
        }, CloseCombatFacts.PrisonersRound), Reference);
        Assert.Equal(CloseCombatResolution.Resolved, failed.Disposition);
        Assert.False(Assert.Single(failed.EscapeChecks!).Passed);
        Assert.Empty(failed.Attacks);
        Assert.Null(Effect(failed, "rp").Escaped);

        // Passed: 1 FP against the broken HS's 2 is 1-2, Kill 4, -2 against a broken unit: an Original 3 eliminates the Guard; the Unarmed squad is
        // larger than the HS it eliminated, so it is not rearmed.
        var passed = ScenarioA1CloseCombatCalculator.Resolve(Facts(units, escape, Rolls([1, 2]) with
        {
            EscapeNtc = new Dictionary<string, IReadOnlyList<int>>(StringComparer.Ordinal) { ["rp"] = [3, 3] },
        }, CloseCombatFacts.PrisonersRound), Reference);
        Assert.True(Effect(passed, "g1").Eliminated);
        Assert.True(Effect(passed, "rp").Escaped);
        Assert.Null(Effect(passed, "rp").RearmedAs);
    }

    [Fact]
    public void AnUnarmedHalfSquadThatEliminatesAnArmedOneIsRearmedAsAConscript()
    {
        // A20.551 (ruling R14.6).
        CloseCombatUnit[] units = [Unit("g1", "attacker-half-squad", "german") with { Broken = true },
            Unit("rp", "defender-half-squad", "russian") with { Captured = true, GuardId = "g1", Unarmed = true }];
        var result = ScenarioA1CloseCombatCalculator.Resolve(Facts(units, [Attack(["rp"], ["g1"])], Rolls([1, 2]) with
        {
            EscapeNtc = new Dictionary<string, IReadOnlyList<int>>(StringComparer.Ordinal) { ["rp"] = [1, 1] },
        }, CloseCombatFacts.PrisonersRound), Reference);
        Assert.True(Effect(result, "g1").Eliminated);
        Assert.Equal("defender-conscript-half-squad", Effect(result, "rp").RearmedAs);
    }

    [Fact]
    public void AnOriginalTwoLetsTheAttackerInfiltrateBeforeTheDefenderStrikes()
    {
        // A11.22 (ruling R14.8): the ATTACKER's Original 2 lets its squad withdraw, so the DEFENDER's attack on it has no effect.
        CloseCombatUnit[] units = [Unit("g1", "attacker-squad", "german") with { InfiltrateTo = "bd01:G6:0" }, Unit("r1", "defender-squad", "russian")];
        CloseCombatDeclaration[] attacks = [Attack(["r1"], ["g1"]), Attack(["g1"], ["r1"])];
        var result = ScenarioA1CloseCombatCalculator.Resolve(Facts(units, attacks, Rolls([2, 2], [1, 1]) with
        {
            LeaderCreation = new Dictionary<string, int>(StringComparer.Ordinal) { ["1"] = 6 },
        }), Reference);
        Assert.Equal("bd01:G6:0", Effect(result, "g1").InfiltratedTo);
        Assert.False(Effect(result, "g1").Eliminated);

        // An Original 12 against the Russian, who declared a destination, lets it leave before its own attack, which is forfeited.
        CloseCombatUnit[] other = [Unit("g1", "attacker-squad", "german"), Unit("r1", "defender-squad", "russian") with { InfiltrateTo = "bd01:G4:0" }];
        var slipped = ScenarioA1CloseCombatCalculator.Resolve(Facts(other, [Attack(["g1"], ["r1"]), Attack(["r1"], ["g1"])], Rolls([6, 6])), Reference);
        Assert.Equal(CloseCombatResolution.Resolved, slipped.Disposition);
        Assert.Single(slipped.Attacks);
        Assert.Equal("bd01:G4:0", Effect(slipped, "r1").InfiltratedTo);
    }

    [Fact]
    public void OverstackingAddsToTheSidesAttacksAndSubtractsFromAttacksOnIt()
    {
        // A5.12, A5.131 (ruling R14.10): four Russian squads exceed three by one.
        CloseCombatUnit[] units = [Unit("g1", "attacker-squad", "german"), Unit("r1", "defender-squad", "russian"), Unit("r2", "defender-squad", "russian"),
            Unit("r3", "defender-squad", "russian"), Unit("r4", "defender-squad", "russian")];
        var result = ScenarioA1CloseCombatCalculator.Resolve(Facts(units, [Attack(["g1"], ["r1"]), Attack(["r2"], ["g1"])], Rolls([6, 6], [6, 6])), Reference);
        Assert.Contains(result.Attacks[0].Defending[0].Drm, item => item.Name == "vs-overstacked" && item.Value == -1);
        Assert.Contains(result.Attacks[1].Drm, item => item.Name == "overstacked" && item.Value == 1);
        Assert.Equal(1, ScenarioA1CloseCombatCalculator.Overstack(units, "russian", Reference));
        Assert.Equal(0, ScenarioA1CloseCombatCalculator.Overstack(units, "german", Reference));
    }

    [Fact]
    public void TheMmcOfTheHighestBpvFoundsACreatedLeader()
    {
        // A18.2 (ruling R14.12): a 4-6-7 (BPV 10) with a 4-6-8 (13): the elite squad's Morale 8 gives -1.
        Assert.Equal(13, Reference.Bpv["attacker-elite-squad"]);
        CloseCombatUnit[] units = [Unit("g1", "attacker-squad", "german"), Unit("g2", "attacker-elite-squad", "german"), Unit("r1", "defender-squad", "russian")];
        var result = ScenarioA1CloseCombatCalculator.Resolve(Facts(units, [Attack(["g1", "g2"], ["r1"])], Rolls([1, 1]) with
        {
            LeaderCreation = new Dictionary<string, int>(StringComparer.Ordinal) { ["0"] = 6 },
        }), Reference);
        Assert.Contains(result.Attacks[0].LeaderCreation!.Drm, item => item.Name == "morale-8-or-more");
    }

    [Fact]
    public void RefereeFindingsOnTheRoundAreFixed()
    {
        // Referee, pass 14, item 2 (A18.12): the Russian Original 2 creates a leader, and the Russian squad infiltrates; the German attack on it,
        // resolved first, is not re-figured for him.
        CloseCombatUnit[] units = [Unit("g1", "attacker-squad", "german"), Unit("r1", "defender-squad", "russian") with { InfiltrateTo = "bd01:G4:0" }];
        var infiltrated = ScenarioA1CloseCombatCalculator.Resolve(Facts(units, [Attack(["g1"], ["r1"]), Attack(["r1"], ["g1"])], Rolls([5, 6], [1, 1]) with
        {
            LeaderCreation = new Dictionary<string, int>(StringComparer.Ordinal) { ["1"] = 1 },
        }), Reference);
        Assert.Equal(4m, infiltrated.Attacks[0].DefenseFirepower);
        Assert.Equal("bd01:G4:0", Effect(infiltrated, "r1").InfiltratedTo);

        // Item 4 (A11.14): a concealed squad declared stacked with a Known leader forfeits its "?": the attack on them is not halved.
        CloseCombatUnit[] stacked = [Unit("g1", "attacker-squad", "german"), Unit("r1", "defender-squad", "russian") with { Concealed = true },
            Unit("rl", "defender-leader", "russian") with { StackedWith = "r1" }];
        var forfeit = ScenarioA1CloseCombatCalculator.Resolve(Facts(stacked, [Attack(["g1"], ["r1", "rl"])], Rolls([6, 6])), Reference);
        Assert.DoesNotContain(forfeit.Attacks[0].FirepowerModifiers, item => item.Name == "vs-concealed");
        Assert.True(Effect(forfeit, "r1").ConcealmentLost);

        // Item 5 (A19.3): an Unarmed unit is Inexperienced, so a capture attempt on it takes -1.
        CloseCombatUnit[] unarmed = [Unit("g1", "attacker-squad", "german"), Unit("r1", "defender-half-squad", "russian") with { Unarmed = true }];
        var capture = ScenarioA1CloseCombatCalculator.Resolve(Facts(unarmed, [Attack(["g1"], ["r1"]) with { Capture = true }], Rolls([6, 6])), Reference);
        Assert.Contains(capture.Attacks[0].Defending[0].Drm, item => item.Name == "capture-vs-inexperienced");

        // Item 13 (A11.22): a pinned unit does not infiltrate after an Original 12 against it.
        CloseCombatUnit[] pinned = [Unit("g1", "attacker-squad", "german"), Unit("r1", "defender-squad", "russian") with { InfiltrateTo = "bd01:G4:0", Pinned = true }];
        var stayed = ScenarioA1CloseCombatCalculator.Resolve(Facts(pinned, [Attack(["g1"], ["r1"]), Attack(["r1"], ["g1"])], Rolls([6, 6], [6, 6])), Reference);
        Assert.Null(Effect(stayed, "r1").InfiltratedTo);

        // Item 11 (A5.5): six SMC are one HS, so two squads, a HS, and six leaders are three squad-equivalents, not overstacked.
        CloseCombatUnit[] crowd = [Unit("g1", "attacker-squad", "german"), Unit("g2", "attacker-squad", "german"), Unit("g3", "attacker-half-squad", "german"),
            .. Enumerable.Range(1, 6).Select(index => Unit("gl" + index, "attacker-leader-8-1", "german"))];
        Assert.Equal(0, ScenarioA1CloseCombatCalculator.Overstack(crowd, "german", Reference));
    }

    [Fact]
    public void AnEscapedSmcIsArmedOnlyWhenTheEscapeSucceeds()
    {
        // Referee, pass 14, item 6 (A20.55, A20.551): the prisoner leader attacks his broken Guard but another German squad remains: no escape yet.
        CloseCombatUnit[] units = [Unit("g1", "attacker-half-squad", "german") with { Broken = true }, Unit("g2", "attacker-squad", "german"),
            Unit("rl", "defender-leader", "russian") with { Captured = true, GuardId = "g1", Unarmed = true }];
        var result = ScenarioA1CloseCombatCalculator.Resolve(Facts(units, [Attack(["rl"], ["g1"])], Rolls([1, 2]) with
        {
            EscapeNtc = new Dictionary<string, IReadOnlyList<int>>(StringComparer.Ordinal) { ["rl"] = [1, 1] },
        }, CloseCombatFacts.PrisonersRound), Reference);
        Assert.True(Effect(result, "rl").Escaped);
        Assert.Null(Effect(result, "rl").Armed);
    }

    public static TheoryData<string> Walked => ["capture", "escape", "infiltration", "concealed", "leader-stack"];

    [Theory]
    [MemberData(nameof(Walked))]
    public void EveryOutcomeOfThePass14RoundsIsDecided(string name)
    {
        var facts = name switch
        {
            "capture" => Facts([Unit("g1", "attacker-squad", "german"), Unit("g2", "attacker-half-squad", "german"), Unit("r1", "defender-squad", "russian"),
                Unit("r2", "defender-conscript-half-squad", "russian")], [Attack(["g1", "g2"], ["r1", "r2"]) with { Capture = true, Yield = ["r2", "r1"] },
                    Attack(["r1", "r2"], ["g1"])]),
            "escape" => Facts([Unit("g1", "attacker-half-squad", "german") with { Broken = true }, Unit("g2", "attacker-leader-8-1", "german"),
                Unit("rp", "defender-squad", "russian") with { Captured = true, GuardId = "g1", Unarmed = true }],
                [Attack(["rp"], ["g1", "g2"])], round: CloseCombatFacts.PrisonersRound),
            "infiltration" => Facts([Unit("g1", "attacker-squad", "german") with { InfiltrateTo = "bd01:G6:0" },
                Unit("r1", "defender-squad", "russian") with { InfiltrateTo = "bd01:G4:0" }], [Attack(["g1"], ["r1"]), Attack(["r1"], ["g1"])]),
            "concealed" => Facts([Unit("g1", "attacker-squad", "german"), Unit("r1", "defender-squad", "russian") with { Concealed = true }],
                [Attack(["g1"], ["r1"]), Attack(["r1"], ["g1"])]) with
            {
                HandToHand = true
            },
            _ => Facts([Unit("g1", "attacker-squad", "german"), Unit("g2", "attacker-squad", "german"), Unit("r1", "defender-squad", "russian")],
                [Attack(["g1", "g2"], ["r1"]), Attack(["r1"], ["g1"])]),
        };
        Assert.Empty(ScenarioA1CloseCombatCalculator.Precheck(facts, Reference));
        Assert.True(Explore(facts, new CloseCombatRolls(null)) >= 36);
    }

    // Walks every roll the package asks for: two dice over their 36 pairs, a Random Selection over every assignment, a dr over 1 to 6.
    private static int Explore(CloseCombatFacts facts, CloseCombatRolls rolls)
    {
        var result = ScenarioA1CloseCombatCalculator.Resolve(facts with
        {
            Rolls = rolls
        }, Reference);
        if (result.Disposition == CloseCombatResolution.Resolved)
        {
            return 1;
        }

        var reason = Assert.Single(result.Reasons);
        Assert.StartsWith("asl.a1.cc.roll-missing:", reason, StringComparison.Ordinal);
        var key = reason["asl.a1.cc.roll-missing:".Length..];
        var split = key.IndexOf(':', StringComparison.Ordinal);
        var (kind, rest) = (key[..split], key[(split + 1)..]);
        var paths = 0;
        if (kind is "attack" or "escapeNtc")
        {
            for (var first = 1; first <= 6; first++)
            {
                for (var second = 1; second <= 6; second++)
                {
                    IReadOnlyList<int> pair = [first, second];
                    paths += Explore(facts, kind == "attack" ? rolls with
                    {
                        Attacks = With(rolls.Attacks, rest, pair)
                    } : rolls with
                    {
                        EscapeNtc = With(rolls.EscapeNtc, rest, pair)
                    });
                }
            }
        }
        else if (kind is "randomSelection" or "leaderStack")
        {
            var attack = rest[..rest.IndexOf(':', StringComparison.Ordinal)];
            var ids = rest[(rest.IndexOf(':', StringComparison.Ordinal) + 1)..].Split(',');
            for (var index = 0; index < (int)Math.Pow(6, ids.Length); index++)
            {
                var map = (kind == "randomSelection" ? rolls.RandomSelection : rolls.LeaderStack)?.ToDictionary(item => item.Key, item => item.Value, StringComparer.Ordinal)
                    ?? new Dictionary<string, int>(StringComparer.Ordinal);
                var value = index;
                foreach (var id in ids)
                {
                    map[attack + ":" + id] = (value % 6) + 1;
                    value /= 6;
                }

                paths += Explore(facts, kind == "randomSelection" ? rolls with
                {
                    RandomSelection = map
                } : rolls with
                {
                    LeaderStack = map
                });
            }
        }
        else
        {
            for (var dr = 1; dr <= 6; dr++)
            {
                paths += Explore(facts, kind switch
                {
                    "woundSeverity" => rolls with { WoundSeverity = With(rolls.WoundSeverity, rest, dr) },
                    "leaderCreation" => rolls with { LeaderCreation = With(rolls.LeaderCreation, rest, dr) },
                    _ => rolls with { WeaponLoss = With(rolls.WeaponLoss, rest, dr) },
                });
            }
        }

        return paths;
    }

    private static Dictionary<string, T> With<T>(IReadOnlyDictionary<string, T>? existing, string id, T value)
    {
        var next = existing?.ToDictionary(item => item.Key, item => item.Value, StringComparer.Ordinal) ?? new Dictionary<string, T>(StringComparer.Ordinal);
        next[id] = value;
        return next;
    }
}
