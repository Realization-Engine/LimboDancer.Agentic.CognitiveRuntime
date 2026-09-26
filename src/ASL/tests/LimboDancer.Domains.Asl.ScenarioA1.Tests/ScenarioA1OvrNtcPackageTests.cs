using System.Text.Json;
using LimboDancer.Abstractions.Domain;
using LimboDancer.Abstractions.Observations;
using LimboDancer.Domains.Asl.ScenarioA1;
using Xunit;

namespace LimboDancer.Domains.Asl.ScenarioA1.Tests;

/// <summary>
/// The OVR NTC package (unit step 10; Scenario A1 OVR NTC Review 2026-09-26): its five reviewed cases, and refusals for
/// missing, conflicting, stale, and extra facts.
/// </summary>
public sealed class ScenarioA1OvrNtcPackageTests
{
    private static readonly Guid Tenant = Guid.Parse("e753fdf9-d585-45cf-a7fa-d0f2cf625276");
    private static readonly DateTimeOffset Now = new(2026, 9, 26, 0, 0, 0, TimeSpan.Zero);

    public static TheoryData<string, Dictionary<string, string>, ConclusionDisposition> Cases => new()
    {
        { "election-reveals-another-defender", new() { ["election"] = "elected", ["otherConcealedNonDummy"] = "present",
            ["secondReveal"] = "randomSelection-revealed-nonDummy", ["ntc"] = "not-taken" }, ConclusionDisposition.Definitive },
        { "lone-smc-mf-insufficient", new() { ["election"] = "requested", ["otherConcealedNonDummy"] = "none", ["remainingMf"] = "belowFour" },
            ConclusionDisposition.Abstained },
        { "failed", new() { ["election"] = "elected", ["otherConcealedNonDummy"] = "none", ["remainingMf"] = "atLeastFour", ["ntc"] = "failed" },
            ConclusionDisposition.Definitive },
        { "passed-against-lone-smc", new() { ["election"] = "elected", ["otherConcealedNonDummy"] = "none", ["remainingMf"] = "atLeastFour", ["ntc"] = "passed" },
            ConclusionDisposition.Indeterminate },
        { "other-occupants-unknown", new() { ["election"] = "elected", ["otherConcealedNonDummy"] = "unknown" }, ConclusionDisposition.Indeterminate },
    };

    [Theory]
    [MemberData(nameof(Cases))]
    public async Task EachReviewedCaseConcludesReadOnly(string suffix, Dictionary<string, string> caseFacts, ConclusionDisposition expected)
    {
        var descriptor = await Descriptor();
        var observation = Observation(Facts(caseFacts));
        var original = observation.Data.GetRawText();
        var result = await new ScenarioA1OvrNtcConclusionResolver().ConcludeAsync(Context(descriptor, observation, "A1-ovr-ntc-" + suffix));
        Assert.Equal(expected, result.Disposition);
        Assert.Equal(original, observation.Data.GetRawText());
        Assert.Contains(result.Evidence, item => item.ResourceId == "asl-scenario-a1.ovr-ntc-case-matrix.json"
            && item.Version == ScenarioA1OvrNtcPackage.MatrixSha256);
        if (expected == ConclusionDisposition.Definitive)
        {
            var value = result.Value!.Value;
            Assert.Equal("bd01:D4:0", value.GetProperty("returnToLocationId").GetString());
            Assert.Equal("bd01:D4:0", value.GetProperty("mfExpenditureLocationId").GetString());
            Assert.Equal(2, value.GetProperty("attemptedEntryMf").GetInt32());
            Assert.True(value.GetProperty("movementPhaseEnds").GetBoolean());
            Assert.False(value.GetProperty("doubledOvrMfCharged").GetBoolean());
            Assert.False(value.GetProperty("executed").GetBoolean());
            Assert.Equal(6, result.ApplicableRules.Count);
        }
        else
        {
            Assert.Null(result.Value);
            Assert.Empty(result.ApplicableRules);
        }
    }

    [Fact]
    public async Task MissingConflictingStaleOrExtraFactsCannotResolve()
    {
        var descriptor = await Descriptor();
        const string caseId = "A1-ovr-ntc-failed";
        var resolver = new ScenarioA1OvrNtcConclusionResolver();
        var facts = Facts(new()
        {
            ["election"] = "elected",
            ["otherConcealedNonDummy"] = "none",
            ["remainingMf"] = "atLeastFour",
            ["ntc"] = "failed"
        });
        Assert.Equal(ConclusionDisposition.Definitive, (await resolver.ConcludeAsync(Context(descriptor, Observation(facts), caseId))).Disposition);

        facts.Remove("ntc");
        Assert.Equal(ConclusionDisposition.Indeterminate, (await resolver.ConcludeAsync(Context(descriptor, Observation(facts), caseId))).Disposition);
        facts["ntc"] = "passed";
        Assert.Equal(ConclusionDisposition.Abstained, (await resolver.ConcludeAsync(Context(descriptor, Observation(facts), caseId))).Disposition);
        facts["ntc"] = "failed";
        facts["unreviewed"] = "yes";
        Assert.Equal(ConclusionDisposition.Abstained, (await resolver.ConcludeAsync(Context(descriptor, Observation(facts), caseId))).Disposition);
        facts.Remove("unreviewed");
        facts["previousLocationId"] = "bd01:D5:0";
        Assert.Equal(ConclusionDisposition.Indeterminate, (await resolver.ConcludeAsync(Context(descriptor, Observation(facts), caseId))).Disposition);
        facts["previousLocationId"] = "bd01:D4:0";
        Assert.Equal(ConclusionDisposition.Indeterminate,
            (await resolver.ConcludeAsync(Context(descriptor, Observation(facts), caseId, "snapshot-2"))).Disposition);
        Assert.Equal(ConclusionDisposition.Abstained,
            (await resolver.ConcludeAsync(Context(descriptor, Observation(facts), "A1-ovr-ntc-unknown"))).Disposition);

        var context = Context(descriptor, Observation(facts), caseId);
        var wrong = new DomainPackageDescriptor(descriptor.Identity, descriptor.CanonicalSources.Take(5).ToArray());
        await Assert.ThrowsAsync<ArgumentException>(async () =>
            await resolver.ConcludeAsync(new DomainConclusionContext(context.Question, wrong, context.EntityResolutions, context.Observations)));
    }

    [Fact]
    public async Task ThePackageIsItsOwnIdentityAndCitesItsSixSources()
    {
        var package = new ScenarioA1OvrNtcPackage();
        var descriptor = await Descriptor();
        Assert.Equal("sha256:" + ScenarioA1OvrNtcPackage.ManifestSha256, descriptor.Identity.Version);
        Assert.Equal(["A.9", "A4.15", "A10.1", "A12.15", "B23.3", "NTC"], descriptor.CanonicalSources.Select(item => item.ElementId));
        Assert.Equal(DomainPackageResolutionOutcome.Unavailable, (await package.ResolveAsync(ScenarioA1ConcealedSmcOverrunPackage.Identity)).Outcome);
        Assert.Equal(DomainPackageResolutionOutcome.Resolved,
            (await new ScenarioA1ConcealedSmcOverrunPackage().ResolveAsync(ScenarioA1ConcealedSmcOverrunPackage.Identity)).Outcome);
    }

    private static Dictionary<string, string> Facts(Dictionary<string, string> caseFacts)
    {
        var facts = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["board"] = "bd01-ground-level-ordinary-building",
            ["phase"] = "mph",
            ["attacker"] = "goodOrderUnconcealedNonDummyInfantryMmc-moving-alone",
            ["initialDefenderState"] = "concealed",
            ["firstReveal"] = "oneEnemySmc-under-A12.15",
            ["entryMode"] = "ordinaryObstacleEntryNotBypass",
            ["a414Exception"] = "none",
            ["specialModifier"] = "none",
            ["leaderExemption"] = "none",
            ["previousLocationId"] = "bd01:D4:0",
        };
        foreach (var (name, value) in caseFacts)
        {
            facts[name] = value;
        }

        return facts;
    }

    private static async Task<DomainPackageDescriptor> Descriptor() =>
        (await new ScenarioA1OvrNtcPackage().ResolveAsync(ScenarioA1OvrNtcPackage.Identity)).Package!;

    private static Observation Observation(Dictionary<string, string> facts, string version = "snapshot-1") =>
        new("ovr-ntc", new ObservationSource("test-supplied-state"), Tenant, Now, JsonSerializer.SerializeToElement(facts), "bd01:E4:0", version,
            "test-supplied-state", ScenarioA1OvrNtcPackage.Identity);

    private static DomainConclusionContext Context(DomainPackageDescriptor descriptor, Observation observation, string caseId, string version = "snapshot-1")
    {
        var question = new DomainQuestion("ovr-ntc", Tenant, descriptor.Identity,
            new SemanticIdentifier(descriptor.Identity.DomainId, ScenarioA1OvrNtcConclusionResolver.QuestionKind),
            JsonSerializer.SerializeToElement(new
            {
                unitId = "squad",
                locationId = "bd01:E4:0",
                previousLocationId = "bd01:D4:0",
                observationVersion = version,
                caseId,
            }), Now);
        DomainEntityResolution Entity(string id) => new(
            new DomainEntityQuery("entity-" + id, Tenant, descriptor.Identity, new SemanticIdentifier(descriptor.Identity.DomainId, "unit-or-location"), id),
            DomainEntityResolutionOutcome.Resolved,
            [new DomainEntityCandidate(new SemanticIdentifier(descriptor.Identity.DomainId, id), descriptor.CanonicalSources[0],
                new EvidenceReference("entity:" + id, EvidenceKind.CanonicalSource, Tenant, descriptor.Identity, id, descriptor.Identity.Version, "test"))],
            ["test.resolution"]);
        return new DomainConclusionContext(question, descriptor, [Entity("squad"), Entity("bd01:E4:0"), Entity("bd01:D4:0")], [observation]);
    }
}
