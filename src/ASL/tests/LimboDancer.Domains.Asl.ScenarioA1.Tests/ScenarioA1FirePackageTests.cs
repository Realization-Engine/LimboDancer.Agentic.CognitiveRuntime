using System.Text.Json;
using LimboDancer.Abstractions.Domain;
using LimboDancer.Abstractions.Observations;
using LimboDancer.Domains.Asl.ScenarioA1;
using Xunit;

namespace LimboDancer.Domains.Asl.ScenarioA1.Tests;

/// <summary>
/// The Fire package (unit step 17; Scenario A1 Fire Review 2026-09-26): U18, the reviewed arithmetic and effects, and
/// refusals for unreviewed elements and for missing, extra, and stale facts.
/// </summary>
public sealed class ScenarioA1FirePackageTests
{
    private static readonly Guid Tenant = Guid.Parse("5b0f3f7e-6b52-4f0e-9d7c-0f5e2a1c9d44");
    private static readonly DateTimeOffset Now = new(2026, 9, 26, 0, 0, 0, TimeSpan.Zero);
    private static readonly JsonSerializerOptions WebJson = new(JsonSerializerDefaults.Web);
    private static readonly ScenarioA1FireReference Reference = new ScenarioA1FirePackage().Reference;

    private static FireFirer Firer(string id, string definition = "defender-squad", bool pinned = false, bool concealed = false) =>
        new(id, definition, "bd01:F5:0", false, pinned, concealed, false, false);

    private static FireTarget Target(string id, string definition, bool broken = false, bool concealed = false) =>
        new(id, definition, "bd01:G5:0", broken, false, concealed, false, false);

    private static FireDirector Leader() => new("ru-leader", "defender-leader", "bd01:F5:0", false, false, false, false);

    // U18: two Russian squads, directed by the 8-0, Prep Fire at an adjacent wooden building holding a German squad and HS.
    private static FireAttack U18(FireRolls rolls, string terrain = "wooden-building", FireDirector? director = null, bool useDirector = true,
        int? elr = 3) =>
        new("PFPh", "phasing", true, "bd01:F5:0", "bd01:G5:0", [Firer("ru-1"), Firer("ru-2")], useDirector ? director ?? Leader() : null,
            1, true, new FireLos(false, 0, true, false), null, terrain,
            [Target("de-squad", "attacker-squad"), Target("de-hs", "attacker-half-squad")], elr, rolls);

    private static FireRolls Rolls(int[] attack, Dictionary<string, int>? selection = null, Dictionary<string, IReadOnlyList<int>>? checks = null,
        Dictionary<string, IReadOnlyList<int>>? leaderLoss = null) =>
        new(attack, selection, checks, leaderLoss);

    [Fact]
    public void U18ResolvesTheColumnDrmResultAndEachUnitsMc()
    {
        var result = ScenarioA1FireCalculator.Resolve(U18(Rolls([3, 4], checks: new()
        {
            ["de-squad"] = [2, 3],
            ["de-hs"] = [4, 3],
        })), Reference);

        Assert.Equal(FireResolution.Resolved, result.Disposition);
        var arithmetic = result.Arithmetic!;
        Assert.All(arithmetic.Firers, firer => Assert.Equal(8m, firer.Firepower));
        Assert.Equal(16m, arithmetic.TotalFirepower);
        Assert.Equal(16, arithmetic.ColumnFp);
        Assert.False(arithmetic.Cowered);
        Assert.Equal([("tem:wooden-building", 2m), ("leadership:ru-leader", 0m)], arithmetic.Drm.Select(item => (item.Name, item.Value)));
        Assert.Equal(7, arithmetic.OriginalDr);
        Assert.Equal(9, arithmetic.FinalDr);
        Assert.Equal("1MC", arithmetic.Result);

        var squad = result.Effects.Single(item => item.UnitId == "de-squad");
        Assert.Equal((6, 7, true), (squad.Checks[0].FinalDr, squad.Checks[0].MoraleLevel, squad.Checks[0].Passed));
        Assert.False(squad.Broken);
        var hs = result.Effects.Single(item => item.UnitId == "de-hs");
        Assert.Equal((8, false, "broken"), (hs.Checks[0].FinalDr, hs.Checks[0].Passed, hs.Checks[0].Consequence));
        Assert.True(hs.Broken);
        Assert.Equal(["ru-1", "ru-2", "ru-leader"], result.FireCounterUnitIds);
        Assert.Equal("prep-fire", result.FireCounter);
    }

    [Fact]
    public void DoublesCowerWithoutADirectorAndNotWithOne()
    {
        var checks = new Dictionary<string, IReadOnlyList<int>> { ["de-squad"] = [1, 2], ["de-hs"] = [1, 2] };
        var cowered = ScenarioA1FireCalculator.Resolve(U18(Rolls([3, 3], checks: checks), useDirector: false), Reference).Arithmetic!;
        Assert.True(cowered.Cowered);
        Assert.Equal((16, 12), (cowered.UnshiftedColumnFp!.Value, cowered.ColumnFp!.Value));
        Assert.Equal((8, "1MC"), (cowered.FinalDr, cowered.Result));

        var directed = ScenarioA1FireCalculator.Resolve(U18(Rolls([3, 3], checks: checks)), Reference).Arithmetic!;
        Assert.False(directed.Cowered);
        Assert.Equal((16, "1MC"), (directed.ColumnFp!.Value, directed.Result));
    }

    [Fact]
    public void KResultReducesTheHighestDrAndEverySurvivorTakesTheMc()
    {
        var result = ScenarioA1FireCalculator.Resolve(U18(Rolls([1, 1], new() { ["de-squad"] = 5, ["de-hs"] = 2 }, new()
        {
            ["de-squad"] = [1, 2],
            ["de-hs"] = [2, 2],
        })), Reference);
        Assert.Equal(FireResolution.Resolved, result.Disposition);
        Assert.Equal((4, "K/3"), (result.Arithmetic!.FinalDr, result.Arithmetic.Result));
        var squad = result.Effects.Single(item => item.UnitId == "de-squad");
        Assert.Equal("attacker-half-squad", squad.FinalDefinitionId);
        Assert.Equal(5, squad.RandomSelectionDr);
        Assert.Contains("casualty-reduced-k", squad.Events);
        Assert.Equal((6, true), (squad.Checks[0].FinalDr, squad.Checks[0].Passed));

        // Passing with the highest passing DR pins (A7.8).
        var hs = result.Effects.Single(item => item.UnitId == "de-hs");
        Assert.Equal((7, "pinned"), (hs.Checks[0].FinalDr, hs.Checks[0].Consequence));
        Assert.True(hs.Pinned);
    }

    [Fact]
    public void KiaEliminatesByRandomSelectionAndBreaksTheRest()
    {
        var result = ScenarioA1FireCalculator.Resolve(U18(Rolls([1, 2], new() { ["de-squad"] = 4, ["de-hs"] = 2 }), "open-ground"), Reference);
        Assert.Equal((3, "1KIA"), (result.Arithmetic!.FinalDr, result.Arithmetic.Result));
        Assert.True(result.Effects.Single(item => item.UnitId == "de-squad").Eliminated);
        Assert.True(result.Effects.Single(item => item.UnitId == "de-hs").Broken);

        // A tie at the cut eliminates both.
        var tied = ScenarioA1FireCalculator.Resolve(U18(Rolls([1, 2], new() { ["de-squad"] = 4, ["de-hs"] = 4 }), "open-ground"), Reference);
        Assert.All(tied.Effects, effect => Assert.True(effect.Eliminated));
    }

    [Fact]
    public void AnEliminatedLeaderCausesALlmc()
    {
        // German squads Prep Fire at a Russian squad and the 8-0 in Open Ground: 1KIA, the leader is selected.
        var attack = new FireAttack("PFPh", "phasing", true, "bd01:G5:0", "bd01:F5:0",
            [new FireFirer("de-1", "attacker-squad", "bd01:G5:0", false, false, false, false, false),
             new FireFirer("de-2", "attacker-squad", "bd01:G5:0", false, false, false, false, false)], null,
            1, true, new FireLos(false, 0, true, false), null, "open-ground",
            [new FireTarget("ru-squad", "defender-squad", "bd01:F5:0", false, false, false, false, false),
             new FireTarget("ru-leader", "defender-leader", "bd01:F5:0", false, false, false, false, false)], 2,
            Rolls([1, 2], new() { ["ru-squad"] = 1, ["ru-leader"] = 6 }, leaderLoss: new() { ["ru-squad"] = [2, 2] }));
        var result = ScenarioA1FireCalculator.Resolve(attack, Reference);
        Assert.Equal(FireResolution.Resolved, result.Disposition);
        Assert.True(result.Effects.Single(item => item.UnitId == "ru-leader").Eliminated);
        var squad = result.Effects.Single(item => item.UnitId == "ru-squad");
        Assert.True(squad.Broken);
        Assert.Equal(("LLMC", true), (squad.Checks[0].Kind, squad.Checks[0].Passed));
    }

    [Fact]
    public void ConcealmentHalvesFirepowerAndIsLost()
    {
        var attack = U18(Rolls([3, 4], checks: new() { ["de-squad"] = [2, 3], ["de-hs"] = [2, 3] })) with
        {
            Firers = [Firer("ru-1", concealed: true), Firer("ru-2")],
            Targets = [Target("de-squad", "attacker-squad", concealed: true), Target("de-hs", "attacker-half-squad")],
        };
        var result = ScenarioA1FireCalculator.Resolve(attack, Reference);
        Assert.Equal((8m, 8, "PTC"), (result.Arithmetic!.TotalFirepower, result.Arithmetic.ColumnFp!.Value, result.Arithmetic.Result));
        Assert.True(result.Effects.Single(item => item.UnitId == "de-squad").ConcealmentLost);
        Assert.Equal(["ru-1"], result.FirerConcealmentLost);
    }

    [Fact]
    public void LongRangeHalvesAndBeyondTwiceNormalRangeAbstains()
    {
        var rolls = Rolls([3, 4]);
        var longRange = ScenarioA1FireCalculator.Resolve(U18(rolls) with { Range = 5 }, Reference);
        Assert.Equal(4m, longRange.Arithmetic!.TotalFirepower);
        Assert.Equal(FireResolution.Abstained, ScenarioA1FireCalculator.Resolve(U18(rolls) with { Range = 9 }, Reference).Disposition);
    }

    public static TheoryData<string, string> Refusals => new()
    {
        { "afph", "asl.a1.fire.phase-outside" },
        { "support-weapon", "asl.a1.fire.firer-outside" },
        { "los-blocked", "asl.a1.fire.los-blocked" },
        { "levels", "asl.a1.fire.levels-differ" },
        { "grain", "asl.a1.fire.hindrance-unattributed" },
        { "hidden", "asl.a1.fire.concealment-unreviewed" },
        { "elr", "asl.a1.fire.elr-undecided:elr-undeclared" },
        { "roll", "asl.a1.fire.roll-missing:checks:de-hs" },
        { "extra-roll", "asl.a1.fire.extra-roll:leaderLoss:de-hs" },
        { "russian-hs", "asl.a1.fire.reduction-counter-missing:ru-squad" },
    };

    [Theory]
    [MemberData(nameof(Refusals))]
    public void UnreviewedElementsAreRefusedWithTheirReason(string variant, string reason)
    {
        var checks = new Dictionary<string, IReadOnlyList<int>> { ["de-squad"] = [2, 3], ["de-hs"] = [4, 3] };
        var attack = U18(Rolls([3, 4], checks: checks));
        attack = variant switch
        {
            "afph" => attack with { Phase = "AFPh" },
            "support-weapon" => attack with { Firers = [Firer("ru-1"), Firer("ru-2") with { UsesSupportWeapon = true }] },
            "los-blocked" => attack with { Los = new FireLos(true, 0, true, false) },
            "levels" => attack with { SameLevel = false },
            "grain" => attack with { Los = new FireLos(false, 1, true, true) },
            "hidden" => attack with { Targets = [Target("de-squad", "attacker-squad") with { Hidden = true }] },
            "elr" => attack with { TargetSideElr = null },
            "roll" => attack with { Rolls = Rolls([3, 4], checks: new() { ["de-squad"] = [2, 3] }) },
            "extra-roll" => attack with { Rolls = Rolls([3, 4], checks: checks, leaderLoss: new() { ["de-hs"] = [1, 1] }) },
            "russian-hs" => new FireAttack("PFPh", "phasing", true, "bd01:G5:0", "bd01:F5:0",
                [new FireFirer("de-1", "attacker-squad", "bd01:G5:0", false, false, false, false, false)], null,
                1, true, new FireLos(false, 0, true, false), null, "open-ground",
                [new FireTarget("ru-squad", "defender-squad", "bd01:F5:0", false, false, false, false, false)], 2,
                Rolls([1, 2], new() { ["ru-squad"] = 3 }, new() { ["ru-squad"] = [1, 2] })),
            _ => throw new ArgumentOutOfRangeException(nameof(variant)),
        };
        var result = ScenarioA1FireCalculator.Resolve(attack, Reference);
        Assert.NotEqual(FireResolution.Resolved, result.Disposition);
        Assert.Contains(reason, result.Reasons);
        Assert.Null(result.Arithmetic);
    }

    [Fact]
    public void GrainCountsWithADeclaredMonthInSeason()
    {
        var rolls = Rolls([3, 4], checks: new() { ["de-squad"] = [2, 3], ["de-hs"] = [2, 3] });
        var result = ScenarioA1FireCalculator.Resolve(U18(rolls) with { Los = new FireLos(false, 1, true, true), ScenarioMonth = 7 }, Reference);
        Assert.Equal(FireResolution.Resolved, result.Disposition);
        Assert.Contains(result.Arithmetic!.Drm, item => item.Name == "los-hindrance" && item.Value == 1);
        Assert.Equal(10, result.Arithmetic.FinalDr);
    }

    [Fact]
    public async Task TheResolverConcludesDefinitiveAndRefusesExtraOrStaleFacts()
    {
        var descriptor = (await new ScenarioA1FirePackage().ResolveAsync(ScenarioA1FirePackage.Identity)).Package!;
        var attack = U18(Rolls([3, 4], checks: new() { ["de-squad"] = [2, 3], ["de-hs"] = [4, 3] }));
        var data = JsonSerializer.SerializeToElement(attack, WebJson);
        var resolver = new ScenarioA1FireConclusionResolver();

        var result = await resolver.ConcludeAsync(Context(descriptor, data));
        Assert.Equal(ConclusionDisposition.Definitive, result.Disposition);
        Assert.Equal("1MC", result.Value!.Value.GetProperty("resolution").GetProperty("arithmetic").GetProperty("result").GetString());
        Assert.False(result.Value.Value.GetProperty("executed").GetBoolean());
        Assert.Equal(descriptor.CanonicalSources.Count, result.ApplicableRules.Count);

        using var extra = JsonDocument.Parse(data.GetRawText().Insert(1, "\"unreviewed\":true,"));
        Assert.Equal(ConclusionDisposition.Abstained, (await resolver.ConcludeAsync(Context(descriptor, extra.RootElement))).Disposition);
        Assert.Equal(ConclusionDisposition.Indeterminate, (await resolver.ConcludeAsync(Context(descriptor, data, "snapshot-2"))).Disposition);

        var context = Context(descriptor, data);
        var wrong = new DomainPackageDescriptor(descriptor.Identity, descriptor.CanonicalSources.Skip(1).ToArray());
        await Assert.ThrowsAsync<ArgumentException>(async () =>
            await resolver.ConcludeAsync(new DomainConclusionContext(context.Question, wrong, context.EntityResolutions, context.Observations)));
    }

    [Fact]
    public async Task ThePackageIsItsOwnIdentityAndCitesItsSources()
    {
        var descriptor = (await new ScenarioA1FirePackage().ResolveAsync(ScenarioA1FirePackage.Identity)).Package!;
        Assert.Equal("sha256:" + ScenarioA1FirePackage.ManifestSha256, descriptor.Identity.Version);
        var elements = descriptor.CanonicalSources.Select(item => item.ElementId).ToArray();
        Assert.Contains("A7.8", elements);
        Assert.Contains("A7-IFT", elements);
        Assert.Contains("B-Terrain-Chart-TEM", elements);
        Assert.Equal(DomainPackageResolutionOutcome.Unavailable,
            (await new ScenarioA1FirePackage().ResolveAsync(ScenarioA1OvrNtcPackage.Identity)).Outcome);
    }

    private static DomainConclusionContext Context(DomainPackageDescriptor descriptor, JsonElement data, string version = "snapshot-1")
    {
        var observation = new Observation("fire", new ObservationSource("test-supplied-state"), Tenant, Now, data, "bd01:G5:0", "snapshot-1",
            "test-supplied-state", ScenarioA1FirePackage.Identity);
        var question = new DomainQuestion("fire", Tenant, descriptor.Identity,
            new SemanticIdentifier(descriptor.Identity.DomainId, ScenarioA1FireConclusionResolver.QuestionKind),
            JsonSerializer.SerializeToElement(new { firerLocationId = "bd01:F5:0", targetLocationId = "bd01:G5:0", observationVersion = version }), Now);
        DomainEntityResolution Entity(string id) => new(
            new DomainEntityQuery("entity-" + id, Tenant, descriptor.Identity, new SemanticIdentifier(descriptor.Identity.DomainId, "location"), id),
            DomainEntityResolutionOutcome.Resolved,
            [new DomainEntityCandidate(new SemanticIdentifier(descriptor.Identity.DomainId, id), descriptor.CanonicalSources[0],
                new EvidenceReference("entity:" + id, EvidenceKind.CanonicalSource, Tenant, descriptor.Identity, id, descriptor.Identity.Version, "test"))],
            ["test.resolution"]);
        return new DomainConclusionContext(question, descriptor, [Entity("bd01:F5:0"), Entity("bd01:G5:0")], [observation]);
    }
}
