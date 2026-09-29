using LimboDancer.Abstractions.Domain;
using LimboDancer.Domains.Asl.ScenarioA1;
using Xunit;

namespace LimboDancer.Domains.Asl.ScenarioA1.Tests;

/// <summary>
/// The Close Combat package (unit step 29): the CCT columns and Kill Numbers (A11.11, p. 692), the DRM, Casualty Reduction,
/// SMC and leaders (A11.14, A11.141), Ambush (A11.4), Field Promotion (A18.12), SW loss (A11.13), berserk units (A15.43,
/// A15.46), the refusals, and a walk proving every reachable outcome of an accepted round decided before any roll.
/// </summary>
public sealed class ScenarioA1CloseCombatTests
{
    private static readonly ScenarioA1CloseCombatReference Reference = new ScenarioA1CloseCombatPackage().Reference;

    private const string At = "bd01:G5:0";

    private static CloseCombatUnit Unit(string id, string definition, string side, bool broken = false, bool advanced = false) =>
        new(id, definition, side, broken, false, false, false, false, false, false, false, false, advanced, false);

    private static CloseCombatFacts Facts(CloseCombatUnit[] units, CloseCombatDeclaration[] attacks, CloseCombatRolls? rolls = null,
        string round = CloseCombatFacts.Simultaneous, string? ambusher = null, string terrain = "open-ground") =>
        new("CCPh", At, terrain, "german", round, ambusher, units, [], [], attacks, rolls ?? new CloseCombatRolls(null));

    private static CloseCombatRolls Rolls(params int[][] attacks) =>
        new(attacks.Select((dice, index) => (index, dice)).ToDictionary(item => item.index.ToString(System.Globalization.CultureInfo.InvariantCulture),
            item => (IReadOnlyList<int>)item.dice, StringComparer.Ordinal));

    private static CloseCombatDeclaration Attack(string[] attackers, string[] defenders, string? director = null) => new(attackers, defenders)
    {
        Director = director,
    };

    // A11.141 EX: a German 4-6-7 and an 8-1 leader against two Russian 4-4-7 squads.
    private static readonly CloseCombatUnit[] Example =
    [
        Unit("g1", "attacker-squad", "german") with { StackedWith = null },
        Unit("gl", "attacker-leader-8-1", "german") with { StackedWith = "g1" },
        Unit("r1", "defender-squad", "russian"),
        Unit("r2", "defender-squad", "russian"),
    ];

    private static readonly CloseCombatDeclaration[] ExampleAttacks =
    [
        Attack(["g1", "gl"], ["r1", "r2"], "gl"),
        Attack(["r1", "r2"], ["g1", "gl"]),
    ];

    [Theory]
    [InlineData(12, 4, "3-1", 8)]
    [InlineData(7, 4, "3-2", 6)]
    [InlineData(11, 2, "4-1", 9)]
    [InlineData(4, 15, "1-4", 3)]
    [InlineData(1, 9, "<1-8", 0)]
    [InlineData(1, 8, "1-8", 1)]
    [InlineData(10, 1, "10-1", 12)]
    [InlineData(21, 2, "10-1", 12)]
    [InlineData(22, 2, ">10-1", 13)]
    [InlineData(3, 4, "1-2", 4)]
    [InlineData(5, 5, "1-1", 5)]
    public void TheOddsRoundDownToThePrintedColumn(int attack, int defense, string odds, int kill)
    {
        // A11.11: "7 to 4 would be 3-2; 11 to 2 would be 4-1; 4 to 15 would be 1-4"; the black Kill Numbers of p. 692.
        var (label, killNumber, _) = Reference.Column(attack, defense);
        Assert.Equal((odds, kill), (label, killNumber));
    }

    [Fact]
    public void TheA11141ExampleResolvesAsTheRulebookSays()
    {
        // The Germans attack at 1-2 (5-8) with the 8-1's -1; the Russians at 3-2 (8-5) (A11.141 EX).
        var result = ScenarioA1CloseCombatCalculator.Resolve(Facts(Example, ExampleAttacks, Rolls([2, 2], [3, 4])), Reference);
        Assert.Equal(CloseCombatResolution.Resolved, result.Disposition);
        var german = result.Attacks[0];
        Assert.Equal(("1-2", 4, 5m, 8m), (german.Odds, german.KillNumber, german.AttackFirepower, german.DefenseFirepower));
        Assert.Equal([("leadership:gl", -1m)], german.Drm.Select(item => (item.Name, item.Value)));

        // An Original 4, -1: Final DR 3 eliminates both Russian squads; the Russians' 7 against Kill Number 6 has no effect.
        Assert.All(german.Defending, item => Assert.Equal(CloseCombatDefenderResult.Eliminated, item.Result));
        var russian = result.Attacks[1];
        Assert.Equal(("3-2", 6), (russian.Odds, russian.KillNumber));
        Assert.All(russian.Defending, item => Assert.Equal(CloseCombatDefenderResult.NoEffect, item.Result));
        Assert.True(result.Effects.Single(item => item.UnitId == "r1").Eliminated);

        // An Original 5: a Partial Kill; with two Russian squads at the Kill Number, a Random Selection dr each (A11.11).
        var partial = ScenarioA1CloseCombatCalculator.Resolve(Facts(Example, ExampleAttacks, Rolls([2, 3], [3, 4])), Reference);
        Assert.Equal(["asl.a1.cc.roll-missing:randomSelection:0:r1,r2"], partial.Reasons);
        var selected = ScenarioA1CloseCombatCalculator.Resolve(Facts(Example, ExampleAttacks, Rolls([2, 3], [3, 4]) with
        {
            RandomSelection = new Dictionary<string, int> { ["0:r1"] = 5, ["0:r2"] = 2 },
        }), Reference);
        Assert.Equal(("defender-half-squad", "defender-squad"),
            (selected.Effects.Single(item => item.UnitId == "r1").FinalDefinitionId, selected.Effects.Single(item => item.UnitId == "r2").FinalDefinitionId));

        // A Russian Original 6: a Partial Kill on the German squad and its leader; the leader's Casualty Reduction is a wound
        // with a Wound Severity dr (A17.11).
        var wound = ScenarioA1CloseCombatCalculator.Resolve(Facts(Example, ExampleAttacks, Rolls([6, 6], [3, 3]) with
        {
            RandomSelection = new Dictionary<string, int> { ["1:g1"] = 4, ["1:gl"] = 4 },
        }), Reference);
        Assert.Equal(["asl.a1.cc.roll-missing:woundSeverity:gl"], wound.Reasons);
        var wounded = ScenarioA1CloseCombatCalculator.Resolve(Facts(Example, ExampleAttacks, Rolls([6, 6], [3, 3]) with
        {
            RandomSelection = new Dictionary<string, int> { ["1:g1"] = 4, ["1:gl"] = 4 },
            WoundSeverity = new Dictionary<string, int> { ["gl"] = 2 },
        }), Reference);
        Assert.Equal(("attacker-half-squad", true), (wounded.Effects.Single(item => item.UnitId == "g1").FinalDefinitionId,
            wounded.Effects.Single(item => item.UnitId == "gl").Wounded));
    }

    [Fact]
    public void TheBrokenDrmAppliesToTheBrokenDefenderAlone()
    {
        // A11.16: -2 against a broken unit; attacked with an unbroken one, each has its own Final DR (A4.8 EX).
        CloseCombatUnit[] units = [Unit("g1", "attacker-squad", "german"), Unit("r1", "defender-squad", "russian", broken: true),
            Unit("r2", "defender-half-squad", "russian")];
        var result = ScenarioA1CloseCombatCalculator.Resolve(Facts(units, [Attack(["g1"], ["r1", "r2"])], Rolls([3, 4])), Reference);
        var defending = result.Attacks[0].Defending;
        Assert.Equal(("1-2", 4), (result.Attacks[0].Odds, result.Attacks[0].KillNumber));
        Assert.Equal((5, 7), (defending.Single(item => item.UnitId == "r1").FinalDr, defending.Single(item => item.UnitId == "r2").FinalDr));
    }

    [Fact]
    public void AnAmbushOccursAtThreeBelowAndGivesItsDrmAndOrder()
    {
        // A11.4: the German squad advanced into the woods; the broken Russian squad gives +1; 1 against 4+1: the Germans ambush.
        CloseCombatUnit[] units = [Unit("g1", "attacker-squad", "german", advanced: true), Unit("r1", "defender-squad", "russian", broken: true)];
        var facts = new AmbushFacts("CCPh", At, "woods", "german", units, new Dictionary<string, int> { ["german"] = 1, ["russian"] = 4 });
        var ambush = ScenarioA1CloseCombatCalculator.ResolveAmbush(facts, Reference);
        Assert.Equal("german", ambush.Ambusher);
        Assert.Equal([("broken", 1m)], ambush.Sides.Single(item => item.Side == "russian").Drm.Select(item => (item.Name, item.Value)));
        var none = ScenarioA1CloseCombatCalculator.ResolveAmbush(facts with { Rolls = new Dictionary<string, int> { ["german"] = 2, ["russian"] = 3 } }, Reference);
        Assert.Null(none.Ambusher);

        // No Ambush in the open, or where nobody advanced.
        Assert.Contains("asl.a1.cc.ambush-not-possible", ScenarioA1CloseCombatCalculator.ResolveAmbush(facts with { Terrain = "open-ground" }, Reference).Reasons);

        // A hero is Stealthy (-1, A11.17, A15.24); an Inexperienced unit is Lax (+1, A19.36); the best leader's modifier unless alone.
        CloseCombatUnit[] mixed = [Unit("g1", "attacker-conscript-squad", "german", advanced: true), Unit("gh", "attacker-hero", "german"),
            Unit("gl", "attacker-leader-9-1", "german"), Unit("r1", "defender-squad", "russian")];
        var drm = ScenarioA1CloseCombatCalculator.ResolveAmbush(facts with { Units = mixed }, Reference).Sides.Single(item => item.Side == "german").Drm;
        Assert.Equal([("lax", 1m), ("stealthy", -1m), ("leadership:gl", -1m)], drm.Select(item => (item.Name, item.Value)));

        // The ambusher's round comes first, with -1, and attacks against it take +1 in the other side's round (A11.4, A11.32).
        var first = ScenarioA1CloseCombatCalculator.Resolve(Facts(units, [Attack(["g1"], ["r1"])], Rolls([4, 4]), CloseCombatFacts.AmbusherRound, "german", "woods"), Reference);
        Assert.Equal([("ambush", -1m)], first.Attacks[0].Drm.Select(item => (item.Name, item.Value)));
        var wrong = ScenarioA1CloseCombatCalculator.Resolve(Facts([Unit("g1", "attacker-squad", "german"), Unit("r1", "defender-squad", "russian")],
            [Attack(["r1"], ["g1"])], Rolls([4, 4]), CloseCombatFacts.AmbusherRound, "german", "woods"), Reference);
        Assert.Contains("asl.a1.cc.round-outside:0", wrong.Reasons);
    }

    [Fact]
    public void AnOriginalTwoByAnMmcCreatesALeaderWhoJoinsTheAttack()
    {
        // A18.12: the German 4-6-7 attacks a Russian 4-4-7 at 1-1 and rolls an Original 2; Leader Creation dr 3, -1 German:
        // 2 creates an 8-0 (A18.2); he adds his FP (5-4, still 1-1) and his leadership 0, and defends with the squad.
        CloseCombatUnit[] units = [Unit("g1", "attacker-squad", "german"), Unit("r1", "defender-squad", "russian")];
        CloseCombatDeclaration[] attacks = [Attack(["g1"], ["r1"]), Attack(["r1"], ["g1"])];
        var asked = ScenarioA1CloseCombatCalculator.Resolve(Facts(units, attacks, Rolls([1, 1], [6, 6])), Reference);
        Assert.Equal(["asl.a1.cc.roll-missing:leaderCreation:0"], asked.Reasons);
        var created = ScenarioA1CloseCombatCalculator.Resolve(Facts(units, attacks, Rolls([1, 1], [6, 6]) with
        {
            LeaderCreation = new Dictionary<string, int> { ["0"] = 3 },
        }), Reference);
        var leader = Assert.Single(created.CreatedLeaders);
        Assert.Equal(("attacker-leader-8-0", "g1"), (leader.DefinitionId, leader.StackedWith));
        Assert.Equal(5m, created.Attacks[0].AttackFirepower);
        Assert.Equal(5m, created.Attacks[1].DefenseFirepower);
        Assert.Contains(created.Attacks[1].Defenders, id => id == CloseCombatCreatedLeader.PlaceholderPrefix + "0");
        Assert.Equal(-1m, created.Attacks[0].LeaderCreation!.Drm.Sum(item => item.Value));
    }

    [Fact]
    public void ASupportWeaponOfAnEliminatedUnitMayBeLostOnAColoredOne()
    {
        // A11.13: the defender's MMG goes with it on an Original colored dr of 1 and a dr at most the black Kill Number.
        CloseCombatUnit[] units = [Unit("g1", "attacker-squad", "german"), Unit("g2", "attacker-squad", "german"),
            Unit("r1", "defender-half-squad", "russian") with { Weapons = ["r-mmg"] }];
        var facts = Facts(units, [Attack(["g1", "g2"], ["r1"])], Rolls([1, 3]));
        Assert.Equal(["asl.a1.cc.roll-missing:weaponLoss:r-mmg"], ScenarioA1CloseCombatCalculator.Resolve(facts, Reference).Reasons);
        var lost = ScenarioA1CloseCombatCalculator.Resolve(facts with { Rolls = Rolls([1, 3]) with { WeaponLoss = new Dictionary<string, int> { ["r-mmg"] = 6 } } }, Reference);
        Assert.Equal(("4-1", true), (lost.Attacks[0].Odds, Assert.Single(lost.WeaponEffects).Eliminated));
        var kept = ScenarioA1CloseCombatCalculator.Resolve(facts with { Rolls = Rolls([3, 1]) }, Reference);
        Assert.Empty(kept.WeaponEffects);
    }

    [Fact]
    public void ABerserkUnitMustAttackAndReturnsToNormalWhenItClearsTheLocation()
    {
        CloseCombatUnit[] units = [Unit("g1", "attacker-squad", "german") with { Berserk = true }, Unit("r1", "defender-half-squad", "russian")];
        Assert.Contains("asl.a1.cc.berserk-must-attack", ScenarioA1CloseCombatCalculator.Resolve(Facts(units, [Attack(["r1"], ["g1"])], Rolls([6, 6])), Reference).Reasons);
        var cleared = ScenarioA1CloseCombatCalculator.Resolve(Facts(units, [Attack(["g1"], ["r1"])], Rolls([2, 3])), Reference);
        Assert.True(cleared.Effects.Single(item => item.UnitId == "g1").BerserkEnded);

        // A11.141: no leadership DRM when a berserk unit attacks with the leader.
        CloseCombatUnit[] led = [.. units, Unit("gl", "attacker-leader-9-1", "german") with { StackedWith = "g1" }];
        var noLeadership = ScenarioA1CloseCombatCalculator.Resolve(Facts(led, [Attack(["g1", "gl"], ["r1"], "gl")], Rolls([3, 5])), Reference);
        Assert.DoesNotContain(noLeadership.Attacks[0].Drm, item => item.Name.StartsWith("leadership", StringComparison.Ordinal));
    }

    public static TheoryData<string, CloseCombatFacts, string> Refusals => new()
    {
        { "phase", Facts(Example, ExampleAttacks) with { Phase = "APh" }, "asl.a1.cc.phase-outside" },
        { "attacked twice", Facts(Example, [Attack(["g1", "gl"], ["r1"]), Attack(["g1"], ["r2"])]), "asl.a1.cc.attacked-twice:1" },
        { "broken attacker", Facts([Unit("g1", "attacker-squad", "german", broken: true), Unit("r1", "defender-squad", "russian")], [Attack(["g1"], ["r1"])]),
            "asl.a1.cc.broken-attacker:0" },
        { "SMC singled out", Facts(Example, [Attack(["r1", "r2"], ["gl"])]), "asl.a1.cc.stacking-outside:0" },
        { "SMC apart from its MMC", Facts(Example, [Attack(["gl"], ["r1"])]), "asl.a1.cc.stacking-outside:0" },
        { "prisoner without a Guard", Facts([.. Example, Unit("rp", "defender-half-squad", "russian") with { Captured = true }], ExampleAttacks),
            "asl.a1.cc.prisoner-guard-outside" },
        { "pinned director", Facts([Example[0], Example[1] with { Pinned = true }, Example[2], Example[3]], ExampleAttacks), "asl.a1.cc.director-outside:0" },
    };

    [Theory]
    [MemberData(nameof(Refusals))]
    public void RefusalsComeBeforeAnyRoll(string name, CloseCombatFacts facts, string reason)
    {
        Assert.True(ScenarioA1CloseCombatCalculator.Precheck(facts, Reference).Contains(reason), name);
    }

    [Fact]
    public void AFieldPromotionOfMixedMmcIsDecidedByBpvAndRandomSelection()
    {
        // A18.2 (ruling R14.12): two attacking MMC of different Morale Levels found the leader on the higher BPV; the refusal of R29.12 is gone.
        CloseCombatUnit[] units = [Unit("g1", "attacker-squad", "german"), Unit("g2", "attacker-elite-squad", "german"), Unit("r1", "defender-squad", "russian")];
        Assert.Empty(ScenarioA1CloseCombatCalculator.Precheck(Facts(units, [Attack(["g1", "g2"], ["r1"])]), Reference));

        // Two identical squads attacking, when the enemy attacks only one of them: the MMC the leader defends with is chosen by Random Selection.
        CloseCombatUnit[] twins = [Unit("g1", "attacker-squad", "german"), Unit("g2", "attacker-squad", "german"), Unit("r1", "defender-squad", "russian")];
        Assert.Empty(ScenarioA1CloseCombatCalculator.Precheck(Facts(twins, [Attack(["g1", "g2"], ["r1"]), Attack(["r1"], ["g1"])]), Reference));
    }

    public static TheoryData<string> Accepted => ["example", "broken-and-hero", "ambusher-round", "berserk-and-weapon", "field-promotion"];

    private static CloseCombatFacts Scenario(string name) => name switch
    {
        "example" => Facts(Example, ExampleAttacks),
        "broken-and-hero" => Facts([Unit("g1", "attacker-half-squad", "german"), Unit("gh", "attacker-hero", "german") with { StackedWith = "g1" },
            Unit("r1", "defender-squad", "russian", broken: true), Unit("rl", "defender-leader", "russian") with { StackedWith = "r1", Wounded = true }],
            [Attack(["g1", "gh"], ["r1", "rl"])]),
        "ambusher-round" => Facts([Unit("g1", "attacker-squad", "german", advanced: true), Unit("r1", "defender-squad", "russian"),
            Unit("r2", "defender-half-squad", "russian")], [Attack(["g1"], ["r1", "r2"])], round: CloseCombatFacts.AmbusherRound, ambusher: "german", terrain: "woods"),
        "berserk-and-weapon" => Facts([Unit("g1", "attacker-squad", "german") with { Berserk = true }, Unit("r1", "defender-half-squad", "russian") with { Weapons = ["r-lmg"] },
            Unit("r2", "defender-half-squad", "russian")], [Attack(["g1"], ["r1", "r2"]), Attack(["r1", "r2"], ["g1"])]),
        "field-promotion" => Facts([Unit("g1", "attacker-squad", "german"), Unit("g2", "attacker-squad", "german"), Unit("r1", "defender-squad", "russian"),
            Unit("rl", "defender-leader", "russian") with { StackedWith = "r1" }], [Attack(["g1", "g2"], ["r1", "rl"]), Attack(["r1", "rl"], ["g1", "g2"], "rl")]),
        _ => throw new ArgumentOutOfRangeException(nameof(name)),
    };

    [Theory]
    [MemberData(nameof(Accepted))]
    public void EveryReachableOutcomeOfAnAcceptedRoundIsDecided(string name)
    {
        var facts = Scenario(name);
        Assert.Empty(ScenarioA1CloseCombatCalculator.Precheck(facts, Reference));
        var paths = Explore(facts, new CloseCombatRolls(null));
        Assert.True(paths >= 36, $"{name}: only {paths} paths");
    }

    // Walks every roll the package asks for, depth first: each attack's DR over all 36 ordered pairs (the colored die and
    // an Original 2 matter), and every dr over 1 to 6.
    private static int Explore(CloseCombatFacts facts, CloseCombatRolls rolls)
    {
        var result = ScenarioA1CloseCombatCalculator.Resolve(facts with { Rolls = rolls }, Reference);
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
        if (kind == "attack")
        {
            for (var first = 1; first <= 6; first++)
            {
                for (var second = 1; second <= 6; second++)
                {
                    paths += Explore(facts, rolls with { Attacks = With(rolls.Attacks, rest, (IReadOnlyList<int>)[first, second]) });
                }
            }
        }
        else if (kind == "randomSelection")
        {
            var attack = rest[..rest.IndexOf(':', StringComparison.Ordinal)];
            var ids = rest[(rest.IndexOf(':', StringComparison.Ordinal) + 1)..].Split(',');
            foreach (var values in Assignments(ids.Length))
            {
                var next = rolls.RandomSelection?.ToDictionary(item => item.Key, item => item.Value, StringComparer.Ordinal) ?? new(StringComparer.Ordinal);
                foreach (var (id, value) in ids.Zip(values))
                {
                    next[attack + ":" + id] = value;
                }

                paths += Explore(facts, rolls with { RandomSelection = next });
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

    private static IEnumerable<int[]> Assignments(int count)
    {
        var values = new int[count];
        var total = (int)Math.Pow(count, count);
        for (var index = 0; index < total; index++)
        {
            var rest = index;
            for (var position = 0; position < count; position++)
            {
                values[position] = (rest % count) + 1;
                rest /= count;
            }

            yield return [.. values];
        }
    }

    [Fact]
    public async Task ThePackageResolvesOnlyItsExactIdentity()
    {
        var package = new ScenarioA1CloseCombatPackage();
        var descriptor = (await package.ResolveAsync(ScenarioA1CloseCombatPackage.Identity)).Package!;
        Assert.Equal("sha256:" + ScenarioA1CloseCombatPackage.ManifestSha256, descriptor.Identity.Version);
        Assert.Contains("A11.11", descriptor.CanonicalSources.Select(item => item.ElementId));
        Assert.Equal(DomainPackageResolutionOutcome.Unavailable, (await package.ResolveAsync(ScenarioA1FirePackage.Identity)).Outcome);
    }
}
