using LimboDancer.Domains.Asl.ScenarioA1;
using Xunit;

namespace LimboDancer.Domains.Asl.ScenarioA1.Tests;

/// <summary>
/// The pre-check of Fire in Live Play (unit step 18): for an attack it accepts, every sequence of rolls the package asks
/// for, over every distinct outcome of each roll, ends Resolved. The walk gives each two-dice roll one representative per
/// total, and each IFT roll one per unordered pair, since only the total and doubles matter, or one per ordered pair when a
/// MG fires, since its first (colored) die decides Multiple ROF (unit steps 19 to 23).
/// </summary>
public sealed class ScenarioA1FireReachabilityTests
{
    private static readonly ScenarioA1FireReference Reference = new ScenarioA1FirePackage().Reference;

    private static FireFirer Firer(string id, string definition, string location) => new(id, definition, location, false, false, false, false, false)
    {
        KnownEnemyInLos = true,
        Captors = [],
    };

    // Unit step 30: every target has the planner's reads of its LOS to a Known enemy (A15.44) and of its captors (A15.5).
    private static FireTarget Target(string id, string definition, string location, bool broken = false) =>
        new(id, definition, location, broken, false, false, false, false, false, false)
        {
            KnownEnemyInLos = true,
            Captors = [],
        };

    private static FireAttack Attack(string firer, string firerDefinition, FireDirector? director, FireTarget[] targets, int? elr, string terrain = "open-ground") =>
        new("PFPh", "phasing", true, "bd01:F5:0", "bd01:G5:0", [Firer(firer + "-1", firerDefinition, "bd01:F5:0"), Firer(firer + "-2", firerDefinition, "bd01:F5:0")],
            director, 1, true, new FireLos(false, 0, true, false), null, terrain, targets, elr, null);

    public static TheoryData<string> Accepted =>
    [
        "german-mmc", "russian-squad-and-leader", "broken-russian-squad", "advancing-fire", "two-locations", "first-fire-in-the-open",
        "subsequent-first-fire", "final-protective-fire", "machine-guns", "hidden-and-dummy", "residual-fp", "final-fire-again",
        "heroes-and-fanatic", "conscript-and-elite", "berserk-leader-and-companions", "berserk-target", "surrender-to-captors", "no-known-enemy-in-los",
    ];

    private static readonly string[] TwoDiceRolls = ["checks", "leaderLoss", "heatOfBattle", "berserkCheck"];

    private static readonly FireDirector RussianLeader = new("ru-l", "defender-leader", "bd01:F5:0", false, false, false, false, false);

    private static FireAttack Movement(FireAttack attack, string kind) => attack with
    {
        Phase = "MPh",
        FiringSide = "non-phasing",
        FireKind = kind,
        TargetMovement = new FireMovement(false),
    };

    private static FireAttack Scenario(string name) => name switch
    {
        "german-mmc" => Attack("ru", "defender-squad", new FireDirector("ru-l", "defender-leader", "bd01:F5:0", false, false, false, false, false),
            [Target("de-s", "attacker-squad", "bd01:G5:0"), Target("de-h", "attacker-half-squad", "bd01:G5:0")], 3, "wooden-building"),
        "russian-squad-and-leader" => Attack("de", "attacker-squad", null,
            [Target("ru-s", "defender-squad", "bd01:G5:0"), Target("ru-l", "defender-leader", "bd01:G5:0")], 2),
        "broken-russian-squad" => Attack("de", "attacker-squad", null,
            [Target("ru-s", "defender-squad", "bd01:G5:0", broken: true), Target("ru-l", "defender-leader", "bd01:G5:0")], 0),
        "advancing-fire" => Scenario("german-mmc") with { Phase = "AFPh" },
        "two-locations" => Scenario("german-mmc") with
        {
            Director = null,
            Firers = [Firer("ru-1", "defender-squad", "bd01:F5:0") with { Range = 1, SameLevel = true, Los = new FireLos(false, 0, true, false) },
                Firer("ru-2", "defender-squad", "bd01:F6:0") with { Range = 2, SameLevel = true, Los = new FireLos(false, 1, true, false) }],
            FirerLocationsAdjacent = true,
        },
        "first-fire-in-the-open" => Movement(Attack("ru", "defender-squad", RussianLeader,
            [Target("de-s", "attacker-squad", "bd01:G5:0"), Target("de-h", "attacker-half-squad", "bd01:G5:0")], 3), ScenarioA1FireCalculator.FirstFire),
        "subsequent-first-fire" => Movement(Scenario("german-mmc") with
        {
            Firers = [.. Scenario("german-mmc").Firers!.Select(firer => firer with { FirstFireMarked = true })],
            WithinSubsequentFirstFireRange = true,
        }, ScenarioA1FireCalculator.SubsequentFirstFire),
        "final-protective-fire" => Movement(Scenario("german-mmc") with
        {
            Director = null,
            Firers = [.. Scenario("german-mmc").Firers!.Select(firer => firer with { FinalFireMarked = true })],
            FiringSideElr = 2,
        }, ScenarioA1FireCalculator.FinalProtectiveFire),
        "machine-guns" => Scenario("german-mmc") with
        {
            Firers = [Firer("ru-1", "defender-squad", "bd01:F5:0") with { Weapons = [new FireWeapon("ru-mmg", "defender-mmg", false, false, false)] },
                Firer("ru-2", "defender-squad", "bd01:F5:0") with { Weapons = [new FireWeapon("ru-lmg", "defender-lmg", false, false, false)] }],
        },
        "hidden-and-dummy" => Scenario("german-mmc") with
        {
            Targets = [Target("de-s", "attacker-squad", "bd01:G5:0") with { Hidden = true }, Target("de-h", "attacker-half-squad", "bd01:G5:0"),
                new FireTarget("de-dummy", null, "bd01:G5:0", false, false, true, false, true, false, false)],
        },
        "residual-fp" => Movement(Scenario("german-mmc") with
        {
            Firers = null,
            Director = null,
            FireGroupComplete = null,
            FirerLocationId = null,
            Range = null,
            SameLevel = null,
            Los = null,
            ResidualFp = 4,
        }, ScenarioA1FireCalculator.ResidualFire),
        "final-fire-again" => Scenario("german-mmc") with
        {
            Phase = "DFPh",
            FiringSide = "non-phasing",
            Firers = [.. Scenario("german-mmc").Firers!.Select(firer => firer with { FirstFireMarked = true })],
        },

        // Unit steps 27 and 28: a hero in the fire group, and a hero, a leader, and a Fanatic elite squad among the targets.
        "heroes-and-fanatic" => Scenario("german-mmc") with
        {
            Director = null,
            Firers = [Firer("ru-1", "defender-squad", "bd01:F5:0"), Firer("ru-h", "defender-hero", "bd01:F5:0")],
            Targets = [Target("de-e", "attacker-elite-squad", "bd01:G5:0") with { Fanatic = true }, Target("de-h", "attacker-hero", "bd01:G5:0"),
                Target("de-l", "attacker-leader-8-0", "bd01:G5:0")],
        },
        "conscript-and-elite" => Attack("de", "attacker-squad", null,
            [Target("ru-c", "defender-conscript-squad", "bd01:G5:0"), Target("ru-e", "defender-elite-half-squad", "bd01:G5:0")], 2),

        // Unit step 30: Defensive First Fire at a moving leader and squad, with a broken squad left behind in the Location, whom a
        // leader who goes berserk may take with him (A15.41); a berserk target (A15.42); captors (A15.5); no Known enemy (A15.44).
        "berserk-leader-and-companions" => Scenario("first-fire-in-the-open") with
        {
            Targets = [Target("de-l", "attacker-leader-8-0", "bd01:G5:0"), Target("de-s", "attacker-squad", "bd01:G5:0")],
            Companions = [Target("de-b", "attacker-squad", "bd01:G5:0", broken: true)],
        },
        "berserk-target" => Scenario("german-mmc") with
        {
            Targets = [Target("de-s", "attacker-squad", "bd01:G5:0") with { Berserk = true }, Target("de-h", "attacker-half-squad", "bd01:G5:0")],
        },
        "surrender-to-captors" => Scenario("russian-squad-and-leader") with
        {
            Targets = [.. Scenario("russian-squad-and-leader").Targets!.Select(target => target with { Captors = ["de-1", "de-2"] })],
        },
        "no-known-enemy-in-los" => Scenario("russian-squad-and-leader") with
        {
            Targets = [.. Scenario("russian-squad-and-leader").Targets!.Select(target => target with { KnownEnemyInLos = false })],
        },
        _ => throw new ArgumentOutOfRangeException(nameof(name)),
    };

    [Theory]
    [MemberData(nameof(Accepted))]
    public void EveryReachableOutcomeOfAnAcceptedAttackIsDecided(string name)
    {
        var attack = Scenario(name);
        Assert.Empty(ScenarioA1FireCalculator.Precheck(attack, Reference));
        var paths = Explore(attack, new FireRolls(null, null, null, null, null));
        Assert.True(paths > 100, $"{name}: only {paths} paths");
    }

    [Fact]
    public void ThePrecheckRefusesWhatTheDiceCouldLeaveUndecided()
    {
        var attack = Scenario("german-mmc");
        Assert.Contains("asl.a1.fire.elr-undecided:elr-undeclared", ScenarioA1FireCalculator.Precheck(attack with
        {
            TargetSideElr = null
        }, Reference));
        Assert.Contains("asl.a1.fire.phase-outside", ScenarioA1FireCalculator.Precheck(attack with
        {
            Phase = "MPh"
        }, Reference));
        Assert.Contains("asl.a1.fire.levels-differ", ScenarioA1FireCalculator.Precheck(attack with
        {
            SameLevel = false
        }, Reference));
        var concealed = attack with
        {
            Firers = [attack.Firers![0] with { Concealed = true }, attack.Firers[1]],
            Targets = [.. attack.Targets!.Select(target => target with { Broken = true })],
        };
        Assert.Contains("asl.a1.fire.concealment-unreviewed:firer-concealment", ScenarioA1FireCalculator.Precheck(concealed, Reference));

        // Unit step 30: without the planner's reads of LOS and captors, Heat of Battle could reach an undecided result.
        var unread = attack with
        {
            Targets = [attack.Targets![0] with { KnownEnemyInLos = null }, attack.Targets[1] with { Captors = null }],
        };
        Assert.Equal(["asl.a1.fire.fact-missing:targets[0].knownEnemyInLos", "asl.a1.fire.fact-missing:targets[1].captors"],
            ScenarioA1FireCalculator.Precheck(unread, Reference));
    }

    // Walks the rolls the package asks for, depth first; returns the number of resolved paths.
    private static int Explore(FireAttack attack, FireRolls rolls)
    {
        var result = ScenarioA1FireCalculator.Resolve(attack with
        {
            Rolls = rolls
        }, Reference);
        if (result.Disposition == FireResolution.Resolved)
        {
            return 1;
        }

        var reason = Assert.Single(result.Reasons);
        Assert.StartsWith("asl.a1.fire.roll-missing:", reason, StringComparison.Ordinal);
        var key = reason["asl.a1.fire.roll-missing:".Length..];
        var paths = 0;
        if (key == "attack")
        {
            var ordered = attack.Firers?.Any(firer => firer.Weapons is { Count: > 0 }) == true;
            for (var low = 1; low <= 6; low++)
            {
                for (var high = ordered ? 1 : low; high <= 6; high++)
                {
                    paths += Explore(attack, rolls with
                    {
                        Attack = [low, high]
                    });
                }
            }
        }
        else if (key.StartsWith("randomSelection:", StringComparison.Ordinal))
        {
            var ids = key["randomSelection:".Length..].Split(',');
            foreach (var values in Assignments(ids.Length))
            {
                paths += Explore(attack, rolls with
                {
                    RandomSelection = Merge(rolls.RandomSelection, ids.Zip(values))
                });
            }
        }
        else if (key.StartsWith("weaponSelection:", StringComparison.Ordinal) || key.StartsWith("firerSelection:", StringComparison.Ordinal))
        {
            var weapon = key.StartsWith("weaponSelection:", StringComparison.Ordinal);
            var ids = key[(key.IndexOf(':', StringComparison.Ordinal) + 1)..].Split(',');
            foreach (var values in Assignments(ids.Length))
            {
                var drs = ids.Zip(values).ToDictionary(pair => pair.First, pair => pair.Second, StringComparer.Ordinal);
                paths += Explore(attack, weapon ? rolls with
                {
                    WeaponSelection = drs
                } : rolls with
                {
                    FirerSelection = drs
                });
            }
        }
        else if (key.StartsWith("woundSeverity:", StringComparison.Ordinal))
        {
            var id = key["woundSeverity:".Length..];
            for (var dr = 1; dr <= 6; dr++)
            {
                paths += Explore(attack, rolls with
                {
                    WoundSeverity = With(rolls.WoundSeverity, id, dr)
                });
            }
        }
        else
        {
            var split = key.IndexOf(':', StringComparison.Ordinal);
            var (kind, id) = (key[..split], key[(split + 1)..]);
            Assert.Contains(kind, TwoDiceRolls);
            for (var total = 2; total <= 12; total++)
            {
                IReadOnlyList<int> dice = total <= 7 ? [1, total - 1] : [total - 6, 6];
                paths += Explore(attack, kind switch
                {
                    "checks" => rolls with { Checks = With(rolls.Checks, id, dice) },
                    "leaderLoss" => rolls with { LeaderLoss = With(rolls.LeaderLoss, id, dice) },
                    "berserkCheck" => rolls with { BerserkChecks = With(rolls.BerserkChecks, id, dice) },
                    _ => rolls with { HeatOfBattle = With(rolls.HeatOfBattle, id, dice) },
                });
            }
        }

        return paths;
    }

    private static Dictionary<string, int> Merge(IReadOnlyDictionary<string, int>? existing, IEnumerable<(string Id, int Value)> added)
    {
        var next = existing?.ToDictionary(item => item.Key, item => item.Value, StringComparer.Ordinal) ?? new Dictionary<string, int>(StringComparer.Ordinal);
        foreach (var (id, value) in added)
        {
            next[id] = value;
        }

        return next;
    }

    private static Dictionary<string, T> With<T>(IReadOnlyDictionary<string, T>? existing, string id, T value)
    {
        var next = existing?.ToDictionary(item => item.Key, item => item.Value, StringComparer.Ordinal) ?? new Dictionary<string, T>(StringComparer.Ordinal);
        next[id] = value;
        return next;
    }

    // Every assignment of the values 1 to n to n units covers every order and tie among their drs.
    private static IEnumerable<int[]> Assignments(int count)
    {
        var values = new int[count];
        var total = (int)Math.Pow(count, count);
        for (var index = 0; index < total; index++)
        {
            var rest = index;
            for (var position = 0; position < count; position++)
            {
                values[position] = rest % count + 1;
                rest /= count;
            }

            yield return [.. values];
        }
    }
}
