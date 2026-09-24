using System.Text.Json;
using LimboDancer.Abstractions.Domain;
using LimboDancer.Abstractions.Observations;
using LimboDancer.Domains.Asl.ScenarioA1;
using Xunit;

namespace LimboDancer.Domains.Asl.ScenarioA1.Tests;

public sealed class ScenarioA1Tests
{
    private static readonly Guid Tenant = Guid.Parse("e753fdf9-d585-45cf-a7fa-d0f2cf625276");
    private static readonly DateTimeOffset Now = new(2026, 9, 24, 0, 0, 0, TimeSpan.Zero);
    private const string AllTrue = """{"isKnownGoodOrderInfantrySquad":true,"isAttackerMovementPhase":true,"canMoveThisPhase":true,"isAdjacentGroundLevelOrdinaryBuilding":true,"isDestinationKnownEmpty":true,"hasNoRoadBypassElevationOrAdditionalTerrain":true,"hasEnoughMovementFactors":true,"isBelowStackingLimit":true,"hasNoSpecialRuleOrOtherModifier":true}""";

    [Fact]
    public async Task ExactReviewedPackageProducesEvidenceBackedReadOnlyConclusion()
    {
        var package = new ScenarioA1Package();
        var resolved = await package.ResolveAsync(ScenarioA1Package.Identity);
        Assert.Equal(DomainPackageResolutionOutcome.Resolved, resolved.Outcome);
        Assert.Equal(12, resolved.Package!.CanonicalSources.Count);
        var result = await new ScenarioA1ConclusionResolver().ConcludeAsync(Context(resolved.Package, AllTrue));
        Assert.Equal(ConclusionDisposition.Definitive, result.Disposition);
        Assert.True(result.Value!.Value.GetProperty("eligible").GetBoolean());
        Assert.Equal(2, result.Value.Value.GetProperty("entryMf").GetInt32());
        Assert.Equal(12, result.ApplicableRules.Count);
        Assert.Equal(2, result.Evidence.Count);
        Assert.Contains(ScenarioA1Package.DecisionSha256, result.ExplanationProvenance);
    }

    [Fact]
    public async Task PackageVersionCannotFloat()
    {
        var other = new DomainPackageRef(new DomainId("asl"),
            ScenarioA1Package.Identity.PackageId, "latest");
        var result = await new ScenarioA1Package().ResolveAsync(other);
        Assert.Equal(DomainPackageResolutionOutcome.Unavailable, result.Outcome);
        Assert.Null(result.Package);
    }

    [Theory]
    [InlineData("isDestinationKnownEmpty")]
    [InlineData("canMoveThisPhase")]
    [InlineData("hasNoSpecialRuleOrOtherModifier")]
    public async Task FalseFactsCannotInheritEligibility(string name)
    {
        var data = AllTrue.Replace("\"" + name + "\":true", "\"" + name + "\":false", StringComparison.Ordinal);
        var result = await new ScenarioA1ConclusionResolver().ConcludeAsync(Context(Descriptor(), data));
        Assert.Equal(ConclusionDisposition.Abstained, result.Disposition);
        Assert.Null(result.Value);
        Assert.Empty(result.ApplicableRules);
    }

    [Fact]
    public async Task MissingOrAmbiguousEvidenceRemainsIndeterminate()
    {
        var resolver = new ScenarioA1ConclusionResolver();
        var missing = await resolver.ConcludeAsync(Context(Descriptor(), "{}"));
        Assert.Equal(ConclusionDisposition.Indeterminate, missing.Disposition);
        Assert.Contains(missing.Ambiguities, reason => reason.StartsWith("fact.unknown:", StringComparison.Ordinal));
        var ambiguous = await resolver.ConcludeAsync(Context(Descriptor(), AllTrue, ambiguous: true));
        Assert.Equal(ConclusionDisposition.Indeterminate, ambiguous.Disposition);
        Assert.Contains(ambiguous.Ambiguities, reason => reason.StartsWith("entity.unresolved", StringComparison.Ordinal));
        var stale = await resolver.ConcludeAsync(Context(Descriptor(), AllTrue, version: null));
        Assert.Equal(ConclusionDisposition.Indeterminate, stale.Disposition);
    }

    [Fact]
    public void ContextRejectsCrossTenantOrCrossPackageEvidence()
    {
        var context = Context(Descriptor(), AllTrue);
        var other = new Observation("other", new ObservationSource("test"), Guid.NewGuid(), Now,
            JsonSerializer.Deserialize<JsonElement>(AllTrue), "building", "state-1",
            domainPackage: ScenarioA1Package.Identity);
        Assert.Throws<ArgumentException>(() => new DomainConclusionContext(context.Question,
            context.Package, context.EntityResolutions, [other]));
    }

    private static DomainPackageDescriptor Descriptor() =>
        new ScenarioA1Package().ResolveAsync(ScenarioA1Package.Identity).AsTask().GetAwaiter().GetResult().Package!;

    private static DomainConclusionContext Context(DomainPackageDescriptor package, string data,
        bool ambiguous = false, string? version = "state-1")
    {
        var question = new DomainQuestion("entry-1", Tenant, package.Identity,
            new SemanticIdentifier(package.Identity.DomainId, ScenarioA1ConclusionResolver.QuestionKind),
            JsonSerializer.Deserialize<JsonElement>("""{"unitId":"squad","locationId":"building"}"""), Now);
        DomainEntityResolution Entity(string id, bool isAmbiguous) => new(
            new DomainEntityQuery("query-" + id, Tenant, package.Identity,
                new SemanticIdentifier(package.Identity.DomainId, "unit-or-location"), id),
            isAmbiguous ? DomainEntityResolutionOutcome.Ambiguous : DomainEntityResolutionOutcome.Resolved,
            Enumerable.Range(0, isAmbiguous ? 2 : 1).Select(i => new DomainEntityCandidate(
                new SemanticIdentifier(package.Identity.DomainId, id + i),
                package.CanonicalSources[0],
                new EvidenceReference("entity:" + id + i, EvidenceKind.CanonicalSource,
                    Tenant, package.Identity, id, package.Identity.Version, "test"))),
            ["test.resolution"]);
        var observation = new Observation("state", new ObservationSource("test"), Tenant, Now,
            JsonSerializer.Deserialize<JsonElement>(data), "building", version,
            "test-state-provider", package.Identity);
        return new DomainConclusionContext(question, package,
            [Entity("squad", ambiguous), Entity("building", false)], [observation]);
    }
}
