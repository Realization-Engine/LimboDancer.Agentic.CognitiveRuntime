using LimboDancer.Abstractions.Domain;
using LimboDancer.Domains.Asl.ScenarioA1;
using Xunit;

namespace LimboDancer.Domains.Asl.ScenarioA1.Tests;

/// <summary>
/// The Rally package (unit step 19; Scenario A1 Rally Review 2026-09-26): the DRM, Fate, the recorded deviations of
/// ruling R0.2, the refusals, and a walk over every roll outcome of the accepted attempts.
/// </summary>
public sealed class ScenarioA1RallyPackageTests
{
    private static readonly ScenarioA1RallyReference Reference = new ScenarioA1RallyPackage().Reference;
    private const string At = "bd01:E4:0";

    private static RallyUnit Unit(string id = "ru-1", string definition = "defender-squad", bool dm = true, bool disrupted = false,
        bool wounded = false, bool attempted = false, bool concealed = false) =>
        new(id, definition, At, true, disrupted, wounded, dm, concealed, attempted, false);

    private static RallyLeader Leader(string definition = "defender-leader", bool wounded = false, string location = At) =>
        new("ru-leader", definition, location, false, wounded, false);

    private static RallyAttempt Attempt(int[]? dice, RallyUnit? unit = null, RallyLeader? leader = null, bool selfRally = false,
        string terrain = "stone-building", string side = "phasing", bool firstMmc = false, bool goodOrderLeader = true,
        bool brokenLeader = false, int? severity = null, string phase = "RPh") =>
        new(phase, side, unit ?? Unit(), selfRally ? null : leader ?? Leader(), At, terrain, !selfRally && goodOrderLeader,
            brokenLeader, firstMmc, null, new RallyRolls(dice, severity));

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
    public void AnOriginalTwoIsRecordedAsADeviationNotRefused()
    {
        // R0.2: a leader rally's Original 2 rallies as its DR gives and records that Heat of Battle was not taken.
        var leaderRally = ScenarioA1RallyCalculator.Resolve(Attempt([1, 1]), Reference);
        Assert.True(leaderRally.Arithmetic!.HeatOfBattleNotTaken);
        Assert.Contains("heat-of-battle-not-taken", leaderRally.Effect!.Events);

        // A18.11: the first MMC Self-Rally of the side's own RPh rallies on an Original 2, whatever the DRM, and records that
        // Leader Creation was not taken; Self-Rally never calls for Heat of Battle (A15.1).
        var fieldPromotion = ScenarioA1RallyCalculator.Resolve(
            Attempt([1, 1], Unit(definition: "defender-conscript-squad"), selfRally: true, firstMmc: true, goodOrderLeader: false), Reference);
        Assert.Equal(("field-promotion-self-rally", true, true, false), (fieldPromotion.Arithmetic!.Kind, fieldPromotion.Arithmetic.Rallied,
            fieldPromotion.Arithmetic.LeaderCreationNotTaken, fieldPromotion.Arithmetic.HeatOfBattleNotTaken));
        Assert.Contains(("self-rally", 1m), fieldPromotion.Arithmetic.Drm.Select(item => (item.Name, item.Value)));
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
            "asl.a1.rally.self-rally-capability-unrecorded" },
        { "capability unrecorded", Attempt([2, 2], selfRally: true, goodOrderLeader: false), "asl.a1.rally.self-rally-capability-unrecorded" },
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
    };

    [Theory]
    [MemberData(nameof(Accepted))]
    public void EveryRollOutcomeOfAnAcceptedAttemptIsDecided(string name, RallyAttempt attempt)
    {
        Assert.Empty(ScenarioA1RallyCalculator.Precheck(attempt, Reference));
        var paths = 0;
        for (var first = 1; first <= 6; first++)
        {
            for (var second = 1; second <= 6; second++)
            {
                var result = ScenarioA1RallyCalculator.Resolve(attempt with { Rolls = new RallyRolls([first, second], null) }, Reference);
                if (result.Reasons is [{ } missing] && missing.StartsWith("asl.a1.rally.roll-missing:woundSeverity:", StringComparison.Ordinal))
                {
                    for (var severity = 1; severity <= 6; severity++)
                    {
                        var wound = ScenarioA1RallyCalculator.Resolve(attempt with { Rolls = new RallyRolls([first, second], severity) }, Reference);
                        Assert.True(wound.Disposition == RallyResolution.Resolved, $"{name} {first},{second},{severity}: {string.Join("; ", wound.Reasons)}");
                        paths++;
                    }

                    continue;
                }

                Assert.True(result.Disposition == RallyResolution.Resolved, $"{name} {first},{second}: {string.Join("; ", result.Reasons)}");
                paths++;
            }
        }

        Assert.True(paths >= 36);
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
