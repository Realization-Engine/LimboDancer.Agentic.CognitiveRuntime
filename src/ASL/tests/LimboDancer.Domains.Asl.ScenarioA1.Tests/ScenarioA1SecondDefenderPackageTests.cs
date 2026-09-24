using System.Text.Json;
using LimboDancer.Abstractions.Domain;
using LimboDancer.Abstractions.Observations;
using LimboDancer.Domains.Asl.ScenarioA1;
using Xunit;

namespace LimboDancer.Domains.Asl.ScenarioA1.Tests;

public sealed class ScenarioA1SecondDefenderPackageTests
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
    public async Task ExactReviewedCasesConcludeOnlySingleSmcEligibility(string suffix,
        string secondReveal, string capability, ConclusionDisposition expected)
    {
        var descriptor = await Descriptor();
        var caseId = "A1-second-defender-" + suffix;
        var facts = Facts(secondReveal, capability);
        var observation = Observation(facts);
        var before = observation.Data.GetRawText();
        var conclusion = await new ScenarioA1SecondDefenderConclusionResolver()
            .ConcludeAsync(Context(descriptor, observation, caseId));
        Assert.Equal(expected, conclusion.Disposition);
        Assert.Equal(before, observation.Data.GetRawText());
        Assert.Contains(conclusion.Evidence, item => item.Kind == EvidenceKind.Observation
            && item.Version == "snapshot-1");
        if (expected == ConclusionDisposition.Definitive)
        {
            Assert.False(conclusion.Value!.Value.GetProperty("singleSmcOverrunEligible").GetBoolean());
            Assert.False(conclusion.Value.Value.GetProperty("forcedBackResolved").GetBoolean());
            Assert.False(conclusion.Value.Value.GetProperty("mfSpendResolved").GetBoolean());
            Assert.False(conclusion.Value.Value.GetProperty("responseOrCcResolved").GetBoolean());
            Assert.Equal(3, conclusion.ApplicableRules.Count);
            Assert.Contains(conclusion.ApplicableRules, item => item.ElementId == "A12.15");
            Assert.Contains(conclusion.ApplicableRules, item => item.ElementId == "A4.15");
        }
        else
        {
            Assert.Null(conclusion.Value);
            Assert.Empty(conclusion.ApplicableRules);
        }
    }

    [Fact]
    public async Task ExactVersionSourceAndFactsCannotFloat()
    {
        var package = new ScenarioA1SecondDefenderPackage();
        var descriptor = (await package.ResolveAsync(ScenarioA1SecondDefenderPackage.Identity)).Package!;
        Assert.Equal("sha256:" + ScenarioA1SecondDefenderPackage.ManifestSha256,
            descriptor.Identity.Version);
        Assert.Equal(DomainPackageResolutionOutcome.Unavailable,
            (await package.ResolveAsync(ScenarioA1ConcealedSmcOverrunPackage.Identity)).Outcome);
        Assert.Equal(DomainPackageResolutionOutcome.Resolved,
            (await new ScenarioA1ConcealedSmcOverrunPackage()
                .ResolveAsync(ScenarioA1ConcealedSmcOverrunPackage.Identity)).Outcome);
        var resolver = new ScenarioA1SecondDefenderConclusionResolver();
        const string caseId = "A1-second-defender-smc-revealed";
        var facts = Facts("revealedEnemySmc-after-election", "passedNtcAndAtLeastFourMf");
        facts["unreviewedModifier"] = "yes";
        Assert.Equal(ConclusionDisposition.Abstained,
            (await resolver.ConcludeAsync(Context(descriptor, Observation(facts), caseId))).Disposition);
        facts.Remove("unreviewedModifier");
        facts.Remove("firstReveal");
        Assert.Equal(ConclusionDisposition.Indeterminate,
            (await resolver.ConcludeAsync(Context(descriptor, Observation(facts), caseId))).Disposition);
        var stale = Observation(Facts("revealedEnemySmc-after-election",
            "passedNtcAndAtLeastFourMf"), "snapshot-2");
        Assert.Equal(ConclusionDisposition.Indeterminate,
            (await resolver.ConcludeAsync(Context(descriptor, stale, caseId))).Disposition);
        var valid = Observation(Facts("revealedEnemySmc-after-election",
            "passedNtcAndAtLeastFourMf"));
        var context = Context(descriptor, valid, caseId);
        var wrong = new DomainPackageDescriptor(descriptor.Identity,
            descriptor.CanonicalSources.Take(2).ToArray());
        await Assert.ThrowsAsync<ArgumentException>(async () => await resolver.ConcludeAsync(
            new DomainConclusionContext(context.Question, wrong,
                context.EntityResolutions, context.Observations)));
    }

    private static Dictionary<string, string> Facts(string secondReveal, string capability) =>
        new(StringComparer.Ordinal)
        {
            ["board"] = "bd01-ground-level-ordinary-building",
            ["phase"] = "mph",
            ["attacker"] = "goodOrderUnconcealedNonDummyInfantryMmc",
            ["initialDefenderState"] = "concealed",
            ["firstReveal"] = "enemySmc-under-A12.15",
            ["overrunElection"] = "elected-after-first-reveal",
            ["entryMode"] = "ordinaryObstacleEntryNotBypass",
            ["a414Exception"] = "none", ["smcOutsideAfv"] = "true",
            ["specialModifier"] = "none", ["leaderExemption"] = "none",
            ["secondReveal"] = secondReveal, ["attackerCapability"] = capability,
        };

    private static async Task<DomainPackageDescriptor> Descriptor() =>
        (await new ScenarioA1SecondDefenderPackage()
            .ResolveAsync(ScenarioA1SecondDefenderPackage.Identity)).Package!;

    private static Observation Observation(Dictionary<string, string> facts,
        string version = "snapshot-1") =>
        new("second-reveal", new ObservationSource("test-supplied-state"), Tenant, Now,
            JsonSerializer.SerializeToElement(facts), "bd01:E4:0", version,
            "test-supplied-state", ScenarioA1SecondDefenderPackage.Identity);

    private static DomainConclusionContext Context(DomainPackageDescriptor descriptor,
        Observation observation, string caseId)
    {
        var question = new DomainQuestion("second-defender-eligibility", Tenant, descriptor.Identity,
            new SemanticIdentifier(descriptor.Identity.DomainId,
                ScenarioA1SecondDefenderConclusionResolver.QuestionKind),
            JsonSerializer.SerializeToElement(new
            {
                unitId = "squad", locationId = "bd01:E4:0",
                observationVersion = "snapshot-1", caseId,
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
            [Entity("squad"), Entity("bd01:E4:0")], [observation]);
    }
}
