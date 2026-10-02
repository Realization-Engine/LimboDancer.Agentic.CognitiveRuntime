using LimboDancer.Abstractions.Domain;
using LimboDancer.Domains.Asl.Rules;
using Xunit;

namespace LimboDancer.Domains.Asl.Rules.Tests;

/// <summary>
/// The Rally package (unit step 19; Scenario A1 Rally Review 2026-09-26; revised at unit steps 27 and 28): the DRM, Fate,
/// Heat of Battle and Leader Creation after an Original 2, the refusals, and a walk over every roll outcome of the accepted
/// attempts.
/// </summary>
public sealed class ScenarioA1RallyPackageTests
{
    private static readonly ScenarioA1RallyReference Reference = new ScenarioA1RallyPackage().Reference;
    private const string At = "bd01:E4:0";

    private static RallyUnit Unit(string id = "ru-1", string definition = "defender-squad", bool dm = true, bool disrupted = false,
        bool wounded = false, bool attempted = false, bool concealed = false, bool? inexperienced = null) =>
        new(id, definition, At, true, disrupted, wounded, dm, concealed, attempted, false)
        {
            Inexperienced = inexperienced ?? (definition.Contains("conscript", StringComparison.Ordinal) ? true : null),
        };

    private static RallyLeader Leader(string definition = "defender-leader", bool wounded = false, string location = At) =>
        new("ru-leader", definition, location, false, wounded, false);

    private static RallyAttempt Attempt(int[]? dice, RallyUnit? unit = null, RallyLeader? leader = null, bool selfRally = false,
        string terrain = "stone-building", string side = "phasing", bool firstMmc = false, bool goodOrderLeader = true,
        bool brokenLeader = false, int? severity = null, string phase = "RPh", int[]? heat = null, int? creation = null) =>
        new(phase, side, unit ?? Unit(), selfRally ? null : leader ?? Leader(), At, terrain, !selfRally && goodOrderLeader,
            brokenLeader, firstMmc, null, new RallyRolls(dice, severity) { HeatOfBattle = heat, LeaderCreation = creation })
        {
            // Unit step 30: the planner's reads of the unit's LOS to a Known enemy (A15.44) and of its captors (A15.5).
            KnownEnemyInLos = true,
            Captors = [],
        };

    [Fact]
    public void ALeaderRalliesABrokenSquadUnderDmInABuilding()
    {
        // 3+2 = 5, +4 DM, +0 leadership, -1 building: Final DR 8 against broken morale 7 fails; 1+2 = 3 gives 6 and rallies.
        var failed = ScenarioA1RallyCalculator.Resolve(Attempt([3, 2]), Reference);
        Assert.Equal(RallyResolution.Resolved, failed.Disposition);
        Assert.Equal([("desperation-morale", 4m), ("leadership:ru-leader", 0m), ("terrain:stone-building", -1m)],
            failed.Arithmetic!.Drm.Select(item => (item.Name, item.Value)));
        Assert.Equal((5, 8, 7, false), (failed.Arithmetic.OriginalDr, failed.Arithmetic.FinalDr, failed.Arithmetic.MoraleLevel, failed.Arithmetic.Rallied));

        var rallied = ScenarioA1RallyCalculator.Resolve(Attempt([1, 2]), Reference);
        Assert.True(rallied.Effect!.Rallied);
        Assert.Equal(["rallied"], rallied.Effect.Events);
        Assert.Equal("leader-rally", rallied.Arithmetic!.Kind);
    }

    [Fact]
    public void ExtremeWinterMakesAnOriginalElevenFate()
    {
        // E3.742 (backlog pass 16, ruling R16.14): an Original 11 is Fate for the units Extreme Winter names; otherwise it is only a failed rally.
        var winter = ScenarioA1RallyCalculator.Resolve(Attempt([5, 6]) with { ExtremeWinterFate = true }, Reference).Effect!;
        Assert.Equal(("defender-half-squad", false), (winter.FinalDefinitionId, winter.Rallied));
        var plain = ScenarioA1RallyCalculator.Resolve(Attempt([5, 6]), Reference).Effect!;
        Assert.Equal("defender-squad", plain.FinalDefinitionId);
    }

    [Fact]
    public void FateReducesTheUnitAndNeverRalliesIt()
    {
        var squad = ScenarioA1RallyCalculator.Resolve(Attempt([6, 6]), Reference).Effect!;
        Assert.Equal(("defender-half-squad", false, false), (squad.FinalDefinitionId, squad.Rallied, squad.Eliminated));

        var half = ScenarioA1RallyCalculator.Resolve(Attempt([6, 6], Unit(definition: "defender-half-squad")), Reference).Effect!;
        Assert.True(half.Eliminated);

        // A leader's Fate is a wound, and the Wound Severity dr is asked for once Fate is rolled.
        var leaderUnit = Unit("ru-leader-2", "defender-leader-7-0");
        var asked = ScenarioA1RallyCalculator.Resolve(Attempt([6, 6], leaderUnit, selfRally: true, goodOrderLeader: false), Reference);
        Assert.Equal(["asl.a1.rally.roll-missing:woundSeverity:ru-leader-2"], asked.Reasons);
        var wounded = ScenarioA1RallyCalculator.Resolve(Attempt([6, 6], leaderUnit, selfRally: true, goodOrderLeader: false, severity: 2), Reference);
        Assert.Equal((true, false), (wounded.Effect!.Wounded, wounded.Effect.Eliminated));
        var killed = ScenarioA1RallyCalculator.Resolve(Attempt([6, 6], leaderUnit, selfRally: true, goodOrderLeader: false, severity: 5), Reference);
        Assert.True(killed.Effect!.Eliminated);
    }

    [Fact]
    public void ALeaderRallysOriginalTwoCallsForHeatOfBattle()
    {
        // A15.1: the Original 2 rallies the squad, then asks for the Heat of Battle DR.
        Assert.Equal(["asl.a1.rally.roll-missing:heatOfBattle"], ScenarioA1RallyCalculator.Resolve(Attempt([1, 1]), Reference).Reasons);

        // 1+1 = 2, +2 Russian, +1 broken: 5 creates a Russian hero and Battle Hardens the 4-4-7 into the squared-E 4-5-8.
        var both = ScenarioA1RallyCalculator.Resolve(Attempt([1, 1], heat: [1, 1]), Reference);
        var heat = both.Arithmetic!.HeatOfBattle!;
        Assert.Equal((5, HeatOfBattleOutcome.HeroAndBattleHardening, "defender-hero", "defender-elite-squad"),
            (heat.FinalDr, heat.Result, heat.HeroDefinitionId, heat.HardenedDefinitionId));
        Assert.Equal([("nationality:russian", 2m), ("broken", 1m)], heat.Drm.Select(item => (item.Name, item.Value)));
        Assert.Equal(("defender-elite-squad", true, "defender-hero"), (both.Effect!.FinalDefinitionId, both.Effect.Rallied, both.Effect.HeroDefinitionId));

        // 3+3 = 6, +3: 9 is Berserk (A15.4): the squad is rallied and berserk.
        var berserk = ScenarioA1RallyCalculator.Resolve(Attempt([1, 1], heat: [3, 3]), Reference);
        Assert.Equal((HeatOfBattleOutcome.Berserk, "defender-squad", true, true), (berserk.Arithmetic!.HeatOfBattle!.Result,
            berserk.Effect!.FinalDefinitionId, berserk.Effect.Rallied, berserk.Effect.Berserk));

        // A leader rallied by another takes Heat of Battle too: 5 makes him heroic and Battle Hardens a 7-0 into an 8-0.
        var leader = ScenarioA1RallyCalculator.Resolve(Attempt([1, 1], Unit("ru-l", "defender-leader-7-0"), heat: [1, 1]), Reference);
        Assert.Equal((true, "defender-leader"), (leader.Effect!.Heroic, leader.Effect.FinalDefinitionId));

        // An elite squad already of the highest quality becomes Fanatic: 2+2 = 4, -1 elite, +3: 6.
        var elite = ScenarioA1RallyCalculator.Resolve(Attempt([1, 1], Unit(definition: "defender-elite-squad"), heat: [2, 2]), Reference);
        Assert.Equal((true, "defender-elite-squad"), (elite.Effect!.Fanatic, elite.Effect.FinalDefinitionId));

        // A15.21: a leader made heroic rallies though the Rally DR failed: a broken German 6+1 (broken morale 6) under DM and
        // a wounded 8-0 in the open: 1+1 = 2, +4, +1 = 7 fails; the Heat of Battle DR 1+1 = 2, +1 broken: 3 makes him heroic.
        var heroic = ScenarioA1RallyCalculator.Resolve(Attempt([1, 1], Unit("de-l", "attacker-leader-6-plus-1"), Leader("attacker-leader-8-0", wounded: true),
            terrain: "open-ground", heat: [1, 1]), Reference);
        Assert.True(heroic.Arithmetic!.FinalDr > heroic.Arithmetic.MoraleLevel);
        Assert.Equal((true, true), (heroic.Effect!.Rallied, heroic.Effect.Heroic));
        Assert.Contains("rallied-heat-of-battle", heroic.Effect.Events);

        // A Heat of Battle DR where none is due is an extra roll.
        Assert.Equal(["asl.a1.rally.extra-roll:heatOfBattle"], ScenarioA1RallyCalculator.Resolve(Attempt([2, 3], heat: [1, 1]), Reference).Reasons);
    }

    [Fact]
    public void FieldPromotionCreatesALeaderFromTheTable()
    {
        // A18.11: the first MMC Self-Rally rallies on an Original 2, whatever the DRM, and asks for the Leader Creation dr;
        // Self-Rally never calls for Heat of Battle (A15.1).
        var conscript = Unit(definition: "defender-conscript-squad");
        var attempt = Attempt([1, 1], conscript, selfRally: true, firstMmc: true, goodOrderLeader: false);
        Assert.Equal(["asl.a1.rally.roll-missing:leaderCreation"], ScenarioA1RallyCalculator.Resolve(attempt, Reference).Reasons);

        // A18.2: dr 1, +1 Russian, +1 for broken Morale Level 5 (6 or less), +1 broken: 4 creates a 7-0.
        var created = ScenarioA1RallyCalculator.Resolve(attempt with
        {
            Rolls = new RallyRolls([1, 1], null) { LeaderCreation = 1 }
        }, Reference);
        Assert.Equal(("field-promotion-self-rally", true, "defender-leader-7-0"), (created.Arithmetic!.Kind, created.Arithmetic.Rallied,
            created.Effect!.CreatedLeaderDefinitionId));
        Assert.Equal(4, created.Arithmetic.LeaderCreation!.FinalDr);
        Assert.Null(created.Arithmetic.HeatOfBattle);

        // dr 4, +3: 7 creates none.
        var none = ScenarioA1RallyCalculator.Resolve(attempt with
        {
            Rolls = new RallyRolls([1, 1], null) { LeaderCreation = 4 }
        }, Reference);
        Assert.Null(none.Effect!.CreatedLeaderDefinitionId);

        // A German 1st Line squad: -1 German, +1 broken, and its broken Morale Level 7 adds nothing: dr 1 creates an 8-1.
        var german = ScenarioA1RallyCalculator.Resolve(Attempt([1, 1], Unit(definition: "attacker-squad"), selfRally: true, firstMmc: true,
            goodOrderLeader: false, creation: 1), Reference);
        Assert.Equal("attacker-leader-8-1", german.Effect!.CreatedLeaderDefinitionId);
    }

    [Fact]
    public void AnNkvdFieldPromotionCreatesACommissar()
    {
        // A25.25 (backlog pass 15, ruling R15.7): an NKVD MMC's Field Promotion uses the Commissar table: a dr of 1, +1 broken, is a 9-0 Commissar.
        var attempt = Attempt([1, 1], Unit(definition: "defender-nkvd-squad"), selfRally: true, firstMmc: true, goodOrderLeader: false);
        var result = ScenarioA1RallyCalculator.Resolve(attempt with { Rolls = attempt.Rolls! with { LeaderCreation = 1 } }, Reference);
        Assert.Equal("defender-commissar-9-0", result.Effect!.CreatedLeaderDefinitionId);
    }

    [Fact]
    public void AWoundedLeaderRalliesWithAWorseModifier()
    {
        var result = ScenarioA1RallyCalculator.Resolve(Attempt([2, 3], leader: Leader(wounded: true)), Reference);
        Assert.Contains(("leadership:ru-leader", 1m), result.Arithmetic!.Drm.Select(item => (item.Name, item.Value)));
    }

    public static TheoryData<string, RallyAttempt, string> Refusals => new()
    {
        { "outside the RPh", Attempt([2, 2], phase: "PFPh"), "asl.a1.rally.phase-outside" },
        { "a second attempt", Attempt([2, 2], Unit(attempted: true)), "asl.a1.rally.already-attempted" },
        { "a leader elsewhere", Attempt([2, 2], leader: Leader(location: "bd01:D4:0")), "asl.a1.rally.leader-outside" },
        { "Self-Rally with a leader present", Attempt([2, 2], selfRally: true, firstMmc: true) with { GoodOrderLeaderInLocation = true },
            "asl.a1.rally.self-rally-with-leader-present" },
        { "a Disrupted Self-Rally", Attempt([2, 2], Unit(disrupted: true), selfRally: true, goodOrderLeader: false),
            "asl.a1.rally.disrupted-self-rally" },
        { "A18.11 with a broken leader", Attempt([2, 2], selfRally: true, firstMmc: true, goodOrderLeader: false, brokenLeader: true),
            "asl.a1.rally.self-rally-not-capable" },
        { "A18.11 in the other side's RPh", Attempt([2, 2], selfRally: true, firstMmc: true, goodOrderLeader: false, side: "non-phasing"),
            "asl.a1.rally.self-rally-not-capable" },
        { "a German or Russian MMC with no recorded capability (R13.8)", Attempt([2, 2], selfRally: true, goodOrderLeader: false),
            "asl.a1.rally.self-rally-not-capable" },
        { "terrain outside", Attempt([2, 2], terrain: "rubble"), "asl.a1.rally.terrain-outside" },
        { "a missing roll", Attempt(null), "asl.a1.rally.roll-missing:rally" },
    };

    [Theory]
    [MemberData(nameof(Refusals))]
    public void EachRefusalGivesItsReason(string name, RallyAttempt attempt, string reason)
    {
        var result = ScenarioA1RallyCalculator.Resolve(attempt, Reference);
        Assert.NotEqual(RallyResolution.Resolved, result.Disposition);
        Assert.True(result.Reasons.Any(item => item.StartsWith(reason, StringComparison.Ordinal)), $"{name}: {string.Join("; ", result.Reasons)}");
    }

    public static TheoryData<string, RallyAttempt> Accepted => new()
    {
        { "leader rally", Attempt(null) },
        { "leader self-rally", Attempt(null, Unit("ru-leader-2", "defender-leader-7-0"), selfRally: true, goodOrderLeader: false) },
        { "A18.11 self-rally", Attempt(null, Unit(definition: "defender-conscript-squad", dm: false), selfRally: true, firstMmc: true,
            goodOrderLeader: false, terrain: "open-ground") },
        { "wounded leader rallies a HS", Attempt(null, Unit(definition: "defender-half-squad"), Leader("defender-leader-6-plus-1", wounded: true)) },
        { "leader rallies an Inexperienced conscript", Attempt(null, Unit(definition: "defender-conscript-squad")) },
        { "leader rallies an elite squad", Attempt(null, Unit(definition: "defender-elite-squad")) },
        { "leader rallies a 10-3", Attempt(null, Unit("ru-l", "defender-leader-10-3"), Leader("defender-leader-9-2")) },
        { "German A18.11 self-rally", Attempt(null, Unit(definition: "attacker-squad"), selfRally: true, firstMmc: true, goodOrderLeader: false) },

        // Unit step 30: a leader rallied by another may go berserk and take his companions with him (A15.41); a squad with
        // ADJACENT captors (A15.5); a squad with no Known enemy in its LOS (A15.44).
        { "berserk leader and companions", Attempt(null, Unit("ru-l", "defender-leader-7-0"), Leader("defender-leader-9-1")) with
            {
                Companions = [new RallyCompanion("ru-leader", "defender-leader-9-1", false, false, false, false, false),
                    new RallyCompanion("ru-b", "defender-squad", true, false, false, false, false)],
            }
        },
        { "surrender to captors", Attempt(null) with { Captors = ["g1", "g2"] } },
        { "no known enemy in LOS", Attempt(null) with { KnownEnemyInLos = false } },
    };

    [Theory]
    [MemberData(nameof(Accepted))]
    public void EveryRollOutcomeOfAnAcceptedAttemptIsDecided(string name, RallyAttempt attempt)
    {
        Assert.Empty(ScenarioA1RallyCalculator.Precheck(attempt, Reference));
        var paths = Explore(name, attempt, new RallyRolls(null, null));
        Assert.True(paths >= 36, $"{name}: only {paths} paths");
    }

    // Walks every roll the package asks for, depth first, over every outcome of each: the Rally DR, the Heat of Battle DR,
    // the Leader Creation dr, a Wound Severity dr, and each companion's Berserk TC (unit step 30).
    private static int Explore(string name, RallyAttempt attempt, RallyRolls rolls)
    {
        var result = ScenarioA1RallyCalculator.Resolve(attempt with { Rolls = rolls }, Reference);
        if (result.Disposition == RallyResolution.Resolved)
        {
            return 1;
        }

        var reason = Assert.Single(result.Reasons);
        Assert.True(reason.StartsWith("asl.a1.rally.roll-missing:", StringComparison.Ordinal), $"{name}: {reason}");
        var key = reason["asl.a1.rally.roll-missing:".Length..];
        var paths = 0;
        if (key is "rally" or "heatOfBattle" || key.StartsWith("berserkCheck:", StringComparison.Ordinal))
        {
            for (var first = 1; first <= 6; first++)
            {
                for (var second = 1; second <= 6; second++)
                {
                    IReadOnlyList<int> dice = [first, second];
                    paths += Explore(name, attempt, key switch
                    {
                        "rally" => rolls with { Rally = dice },
                        "heatOfBattle" => rolls with { HeatOfBattle = dice },
                        _ => rolls with
                        {
                            BerserkChecks = new Dictionary<string, IReadOnlyList<int>>(rolls.BerserkChecks ?? new Dictionary<string, IReadOnlyList<int>>(),
                                StringComparer.Ordinal) { [key["berserkCheck:".Length..]] = dice },
                        },
                    });
                }
            }
        }
        else
        {
            for (var dr = 1; dr <= 6; dr++)
            {
                paths += Explore(name, attempt, key == "leaderCreation" ? rolls with { LeaderCreation = dr } : rolls with { WoundSeverity = dr });
            }
        }

        return paths;
    }

    [Fact]
    public async Task ThePackageResolvesOnlyItsExactIdentity()
    {
        var package = new ScenarioA1RallyPackage();
        var descriptor = (await package.ResolveAsync(ScenarioA1RallyPackage.Identity)).Package!;
        Assert.Equal("sha256:" + ScenarioA1RallyPackage.ManifestSha256, descriptor.Identity.Version);
        Assert.Contains("A10.62", descriptor.CanonicalSources.Select(item => item.ElementId));
        Assert.Equal(DomainPackageResolutionOutcome.Unavailable, (await package.ResolveAsync(ScenarioA1FirePackage.Identity)).Outcome);
    }
}
