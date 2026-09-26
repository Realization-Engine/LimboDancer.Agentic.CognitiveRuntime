using LimboDancer.Domains.Asl.ScenarioA1;
using Xunit;

namespace LimboDancer.Domains.Asl.ScenarioA1.Tests;

/// <summary>
/// The pre-check of Fire in Live Play (unit step 18): for an attack it accepts, every sequence of rolls the package asks
/// for, over every distinct outcome of each roll, ends Resolved. The walk gives each two-dice roll one representative per
/// total, and each IFT roll one per unordered pair, since only the total and doubles matter.
/// </summary>
public sealed class ScenarioA1FireReachabilityTests
{
    private static readonly ScenarioA1FireReference Reference = new ScenarioA1FirePackage().Reference;

    private static FireFirer Firer(string id, string definition, string location) => new(id, definition, location, false, false, false, false, false);

    private static FireTarget Target(string id, string definition, string location, bool broken = false) =>
        new(id, definition, location, broken, false, false, false, false, false, false);

    private static FireAttack Attack(string firer, string firerDefinition, FireDirector? director, FireTarget[] targets, int? elr, string terrain = "open-ground") =>
        new("PFPh", "phasing", true, "bd01:F5:0", "bd01:G5:0", [Firer(firer + "-1", firerDefinition, "bd01:F5:0"), Firer(firer + "-2", firerDefinition, "bd01:F5:0")],
            director, 1, true, new FireLos(false, 0, true, false), null, terrain, targets, elr, null);

    public static TheoryData<string> Accepted => ["german-mmc", "russian-squad-and-leader", "broken-russian-squad"];

    private static FireAttack Scenario(string name) => name switch
    {
        "german-mmc" => Attack("ru", "defender-squad", new FireDirector("ru-l", "defender-leader", "bd01:F5:0", false, false, false, false, false),
            [Target("de-s", "attacker-squad", "bd01:G5:0"), Target("de-h", "attacker-half-squad", "bd01:G5:0")], 3, "wooden-building"),
        "russian-squad-and-leader" => Attack("de", "attacker-squad", null,
            [Target("ru-s", "defender-squad", "bd01:G5:0"), Target("ru-l", "defender-leader", "bd01:G5:0")], 2),
        "broken-russian-squad" => Attack("de", "attacker-squad", null,
            [Target("ru-s", "defender-squad", "bd01:G5:0", broken: true), Target("ru-l", "defender-leader", "bd01:G5:0")], 0),
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
            for (var low = 1; low <= 6; low++)
            {
                for (var high = low; high <= 6; high++)
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
            var ids = attack.Targets!.Select(target => target.UnitId!).ToArray();
            foreach (var values in Assignments(ids.Length))
            {
                paths += Explore(attack, rolls with
                {
                    RandomSelection = ids.Zip(values).ToDictionary(pair => pair.First, pair => pair.Second)
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
            for (var total = 2; total <= 12; total++)
            {
                IReadOnlyList<int> dice = total <= 7 ? [1, total - 1] : [total - 6, 6];
                paths += Explore(attack, kind == "checks"
                    ? rolls with
                    {
                        Checks = With(rolls.Checks, id, dice)
                    }
                    : rolls with
                    {
                        LeaderLoss = With(rolls.LeaderLoss, id, dice)
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
