using System.Text.Json;
using LimboDancer.Abstractions.Domain;
using LimboDancer.Abstractions.Observations;
using LimboDancer.Domains.Asl.ScenarioA1;
using Xunit;

namespace LimboDancer.Domains.Asl.ScenarioA1.Tests;

public sealed class ScenarioA1SecondDefenderConsequencePackageTests
{
    private static readonly Guid Tenant = Guid.Parse("e753fdf9-d585-45cf-a7fa-d0f2cf625276");
    private static readonly DateTimeOffset Now = new(2026, 9, 24, 0, 0, 0, TimeSpan.Zero);

    [Theory]
    [InlineData("smc-revealed", "revealedEnemySmc-after-election",
        "passedNtcAndAtLeastFourMf", ConclusionDisposition.Definitive)]
    [InlineData("mmc-revealed", "revealedEnemyMmc-after-election",
        "passedNtcAndAtLeastFourMf", ConclusionDisposition.Definitive)]
    [InlineData("other-type", "revealedOtherNonDummy-after-election",
        "passedNtcAndAtLeastFourMf", ConclusionDisposition.Indeterminate)]
    [InlineData("unknown-type", "revealedUnknownType-after-election",
        "passedNtcAndAtLeastFourMf", ConclusionDisposition.Indeterminate)]
    [InlineData("unrevealed-smc", "secondSmcKnownButNotRevealed",
        "passedNtcAndAtLeastFourMf", ConclusionDisposition.Indeterminate)]
    [InlineData("capability-unresolved", "revealedEnemySmc-after-election",
        "ntcOrMfUnresolved", ConclusionDisposition.Indeterminate)]
    [InlineData("mf-insufficient", "revealedEnemySmc-after-election",
        "mfInsufficient", ConclusionDisposition.Abstained)]
    public async Task ExactReviewedConsequencesAreReadOnly(string suffix,
        string reveal, string capability, ConclusionDisposition expected)
    {
        var descriptor = await Descriptor();
        var caseId = "A1-second-defender-consequence-" + suffix;
        var observation = Observation(Facts(reveal, capability));
        var original = observation.Data.GetRawText();
        var result = await new ScenarioA1SecondDefenderConsequenceConclusionResolver()
            .ConcludeAsync(Context(descriptor, observation, caseId));
        Assert.Equal(expected, result.Disposition);
        Assert.Equal(original, observation.Data.GetRawText());
        Assert.Contains(result.Evidence, item => item.Kind == EvidenceKind.Observation
            && item.Version == "snapshot-1");
        Assert.Contains(result.Evidence, item =>
            item.ResourceId == "asl-scenario-a1.second-defender-consequence-case-matrix.json"
            && item.Version == ScenarioA1SecondDefenderConsequencePackage.MatrixSha256);
        if (expected == ConclusionDisposition.Definitive)
        {
            var value = result.Value!.Value;
            Assert.Equal("bd01:D4:0", value.GetProperty("returnToLocationId").GetString());
            Assert.Equal("bd01:D4:0", value.GetProperty("mfExpenditureLocationId").GetString());
            Assert.Equal(2, value.GetProperty("attemptedEntryMf").GetInt32());
            Assert.False(value.GetProperty("additionalOvrMfResolved").GetBoolean());
            Assert.False(value.GetProperty("defensiveAttackResolved").GetBoolean());
            Assert.False(value.GetProperty("responseOrCcResolved").GetBoolean());
            Assert.False(value.GetProperty("executed").GetBoolean());
            Assert.Equal(4, result.ApplicableRules.Count);
        }
        else
        {
            Assert.Null(result.Value);
            Assert.Empty(result.ApplicableRules);
        }
    }

    [Fact]
    public async Task PreviousLocationMissingConflictingStaleOrExtraFactsCannotResolve()
    {
        var descriptor = await Descriptor();
        const string caseId = "A1-second-defender-consequence-smc-revealed";
        var resolver = new ScenarioA1SecondDefenderConsequenceConclusionResolver();
        var facts = Facts("revealedEnemySmc-after-election", "passedNtcAndAtLeastFourMf");
        facts.Remove("previousLocationId");
        Assert.Equal(ConclusionDisposition.Indeterminate,
            (await resolver.ConcludeAsync(Context(descriptor, Observation(facts), caseId))).Disposition);
        facts["previousLocationId"] = "bd01:D5:0";
        Assert.Equal(ConclusionDisposition.Indeterminate,
            (await resolver.ConcludeAsync(Context(descriptor, Observation(facts), caseId))).Disposition);
        facts["previousLocationId"] = "bd01:D4:0";
        facts.Remove("attemptedEntryMf");
        Assert.Equal(ConclusionDisposition.Indeterminate,
            (await resolver.ConcludeAsync(Context(descriptor, Observation(facts), caseId))).Disposition);
        facts["attemptedEntryMf"] = "ordinaryBuildingTwoMf";
        facts["unreviewed"] = "yes";
        Assert.Equal(ConclusionDisposition.Abstained,
            (await resolver.ConcludeAsync(Context(descriptor, Observation(facts), caseId))).Disposition);
        facts.Remove("unreviewed");
        Assert.Equal(ConclusionDisposition.Indeterminate,
            (await resolver.ConcludeAsync(Context(descriptor, Observation(facts), caseId,
                "snapshot-2"))).Disposition);
        var context = Context(descriptor, Observation(facts), caseId);
        var wrong = new DomainPackageDescriptor(descriptor.Identity,
            descriptor.CanonicalSources.Take(3).ToArray());
        await Assert.ThrowsAsync<ArgumentException>(async () => await resolver.ConcludeAsync(
            new DomainConclusionContext(context.Question, wrong,
                context.EntityResolutions, context.Observations)));
    }

    [Fact]
    public async Task ConsequenceIdentityCannotReplaceEligibilityIdentity()
    {
        var package = new ScenarioA1SecondDefenderConsequencePackage();
        var descriptor = await Descriptor();
        Assert.Equal("sha256:" + ScenarioA1SecondDefenderConsequencePackage.ManifestSha256,
            descriptor.Identity.Version);
        Assert.Equal(DomainPackageResolutionOutcome.Unavailable,
            (await package.ResolveAsync(ScenarioA1SecondDefenderPackage.Identity)).Outcome);
        Assert.Equal(DomainPackageResolutionOutcome.Resolved,
            (await new ScenarioA1SecondDefenderPackage()
                .ResolveAsync(ScenarioA1SecondDefenderPackage.Identity)).Outcome);
    }

    private static Dictionary<string, string> Facts(string reveal, string capability) =>
        new(StringComparer.Ordinal)
        {
            ["board"] = "bd01-ground-level-ordinary-building", ["phase"] = "mph",
            ["attacker"] = "goodOrderUnconcealedNonDummyInfantryMmc",
            ["entryMode"] = "ordinaryObstacleEntryNotBypass",
            ["initialDefenderState"] = "concealed", ["firstReveal"] = "enemySmc-under-A12.15",
            ["overrunElection"] = "elected-after-first-reveal",
            ["previousLocation"] = "knownLastOccupiedLocation",
            ["previousLocationId"] = "bd01:D4:0",
            ["attemptedEntryMf"] = "ordinaryBuildingTwoMf",
            ["a414ExceptionOtherThanInfantryOvr"] = "none",
            ["specialModifier"] = "none", ["leaderExemption"] = "none",
            ["secondReveal"] = reveal, ["attackerCapability"] = capability,
        };

    private static async Task<DomainPackageDescriptor> Descriptor() =>
        (await new ScenarioA1SecondDefenderConsequencePackage()
            .ResolveAsync(ScenarioA1SecondDefenderConsequencePackage.Identity)).Package!;

    private static Observation Observation(Dictionary<string, string> facts,
        string version = "snapshot-1") =>
        new("consequence", new ObservationSource("test-supplied-state"), Tenant, Now,
            JsonSerializer.SerializeToElement(facts), "bd01:E4:0", version,
            "test-supplied-state", ScenarioA1SecondDefenderConsequencePackage.Identity);

    private static DomainConclusionContext Context(DomainPackageDescriptor descriptor,
        Observation observation, string caseId, string version = "snapshot-1")
    {
        var question = new DomainQuestion("second-defender-consequence", Tenant, descriptor.Identity,
            new SemanticIdentifier(descriptor.Identity.DomainId,
                ScenarioA1SecondDefenderConsequenceConclusionResolver.QuestionKind),
            JsonSerializer.SerializeToElement(new
            {
                unitId = "squad", locationId = "bd01:E4:0", previousLocationId = "bd01:D4:0",
                observationVersion = version, caseId,
            }), Now);
        DomainEntityResolution Entity(string id) => new(
            new DomainEntityQuery("entity-" + id, Tenant, descriptor.Identity,
                new SemanticIdentifier(descriptor.Identity.DomainId, "unit-or-location"), id),
            DomainEntityResolutionOutcome.Resolved,
            [new DomainEntityCandidate(new SemanticIdentifier(descriptor.Identity.DomainId, id),
                descriptor.CanonicalSources[0],
                new EvidenceReference("entity:" + id, EvidenceKind.CanonicalSource, Tenant,
                    descriptor.Identity, id, descriptor.Identity.Version, "test"))],
            ["test.resolution"]);
        return new DomainConclusionContext(question, descriptor,
            [Entity("squad"), Entity("bd01:E4:0"), Entity("bd01:D4:0")], [observation]);
    }
}
