using System.Text.Json;
using LimboDancer.Abstractions.Domain;
using LimboDancer.Abstractions.Observations;
using LimboDancer.Domains.Asl.ScenarioA1;
using Xunit;

namespace LimboDancer.Domains.Asl.ScenarioA1.Tests;

public sealed class ScenarioA1OccupiedPackageTests
{
    private static readonly Guid Tenant = Guid.Parse("e753fdf9-d585-45cf-a7fa-d0f2cf625276");
    private static readonly DateTimeOffset Now = new(2026, 9, 24, 0, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task AffirmativeConformanceAdmitsScopedPackageAndCitesControllingExceptions()
    {
        ScenarioA1ConformanceAdmission.Validate();
        Assert.Equal("7a8cc062e3d60b7289e30a50981120e56544c1fc3555eb82cc6731473b9b14c8",
            ScenarioA1ConformanceAdmission.Sha256);
        var candidate = new ScenarioA1SemanticCandidate();
        var resolver = new ScenarioA1OccupiedConclusionResolver();
        var descriptor = Descriptor();
        foreach (var (id, exception) in new[]
        {
            ("A1-fortified-breached-entry", "B23.9221"),
            ("A1-single-known-enemy-smc-overrun", "A4.15"),
            ("A1-advance-phase-entry", "A4.14"),
        })
        {
            var semanticCase = candidate.Cases.Single(item => item.Id == id);
            var facts = semanticCase.Predicates.ToDictionary(item => item.Key, item => item.ExpectedValue);
            var conclusion = await resolver.ConcludeAsync(Context(descriptor, id, facts));
            Assert.Equal(ConclusionDisposition.Qualified, conclusion.Disposition);
            Assert.True(conclusion.Value!.Value.GetProperty("attemptOnly").GetBoolean());
            Assert.Contains(conclusion.ControllingExceptions, rule => rule.ElementId == exception);
            facts.Remove(semanticCase.Predicates.Single(item => item.Key == "phase").Key);
            Assert.Equal(ConclusionDisposition.Indeterminate,
                (await resolver.ConcludeAsync(Context(descriptor, id, facts))).Disposition);
        }
    }

    [Fact]
    public async Task PackageIsImmutableAndDoesNotSupersedeFirstCaseIdentity()
    {
        var package = new ScenarioA1OccupiedPackage();
        var resolved = await package.ResolveAsync(ScenarioA1OccupiedPackage.Identity);
        Assert.Equal(DomainPackageResolutionOutcome.Resolved, resolved.Outcome);
        Assert.NotEmpty(resolved.Package!.CanonicalSources);
        Assert.Equal(resolved.Package.CanonicalSources.Count,
            resolved.Package.CanonicalSources.Select(rule => rule.ElementId).Distinct().Count());
        Assert.Equal(DomainPackageResolutionOutcome.Unavailable,
            (await package.ResolveAsync(ScenarioA1Package.Identity)).Outcome);
        Assert.Equal(DomainPackageResolutionOutcome.Unavailable,
            (await package.ResolveAsync(new DomainPackageRef(new DomainId("asl"),
                ScenarioA1OccupiedPackage.Identity.PackageId, "latest"))).Outcome);
    }

    [Fact]
    public async Task SevenExactCasesProduceCitedReadOnlyConclusionsAndMissingFactsFailClosed()
    {
        var resolver = new ScenarioA1OccupiedConclusionResolver();
        var candidate = new ScenarioA1SemanticCandidate();
        var descriptor = Descriptor();
        foreach (var semanticCase in candidate.Cases.Where(item => item.ReviewStatus == "reviewed-bounded"))
        {
            var facts = semanticCase.Predicates.ToDictionary(item => item.Key, item => item.ExpectedValue);
            var snapshot = JsonSerializer.Serialize(facts);
            var conclusion = await resolver.ConcludeAsync(Context(descriptor, semanticCase.Id, facts));
            Assert.Equal(semanticCase.ExpectedDisposition.StartsWith("qualified-", StringComparison.Ordinal)
                ? ConclusionDisposition.Qualified : ConclusionDisposition.Definitive, conclusion.Disposition);
            Assert.NotNull(conclusion.Value);
            Assert.Equal(semanticCase.Id, conclusion.Value.Value.GetProperty("caseId").GetString());
            Assert.Equal(semanticCase.ExpectedDisposition != "prohibited",
                conclusion.Value.Value.GetProperty("eligible").GetBoolean());
            Assert.Equal(semanticCase.SourceRules, conclusion.ApplicableRules.Select(item => item.ElementId));
            Assert.Equal(snapshot, JsonSerializer.Serialize(facts));
            facts.Remove(semanticCase.Predicates[0].Key);
            var missing = await resolver.ConcludeAsync(Context(descriptor, semanticCase.Id, facts));
            Assert.Equal(ConclusionDisposition.Indeterminate, missing.Disposition);
            Assert.Null(missing.Value);
            facts[semanticCase.Predicates[0].Key] = "contrary";
            var contrary = await resolver.ConcludeAsync(Context(descriptor, semanticCase.Id, facts));
            Assert.Equal(ConclusionDisposition.Abstained, contrary.Disposition);
            Assert.Null(contrary.Value);
        }
    }

    [Fact]
    public async Task UnknownAndNonDefinitiveCasesAndStaleObservationsCannotBecomeEligible()
    {
        var resolver = new ScenarioA1OccupiedConclusionResolver();
        var descriptor = Descriptor();
        foreach (var id in new[] { "A1-concealed-occupancy-attempt", "A1-unknown-location-or-modifier" })
            Assert.Equal(ConclusionDisposition.Indeterminate,
                (await resolver.ConcludeAsync(Context(descriptor, id, new Dictionary<string, string>())))
                    .Disposition);
        Assert.Equal(ConclusionDisposition.Abstained,
            (await resolver.ConcludeAsync(Context(descriptor, "invented-case", new Dictionary<string, string>())))
                .Disposition);
        var exact = new ScenarioA1SemanticCandidate().Cases.Single(item => item.Id == "A1-empty-ordinary-mph");
        var facts = exact.Predicates.ToDictionary(item => item.Key, item => item.ExpectedValue);
        Assert.Equal(ConclusionDisposition.Indeterminate,
            (await resolver.ConcludeAsync(Context(descriptor, exact.Id, facts, version: null))).Disposition);
        Assert.Contains((await resolver.ConcludeAsync(Context(descriptor, exact.Id, facts,
            version: "state-2"))).Ambiguities,
            reason => reason == "observation.version-changed-during-adjudication");
        Assert.Equal(ConclusionDisposition.Indeterminate,
            (await resolver.ConcludeAsync(Context(descriptor, exact.Id, facts, ambiguous: true))).Disposition);
        facts["unreviewedModifier"] = "present";
        Assert.Equal(ConclusionDisposition.Abstained,
            (await resolver.ConcludeAsync(Context(descriptor, exact.Id, facts))).Disposition);
    }

    [Fact]
    public void ScopeRejectsCrossTenantAndCrossPackageEvidence()
    {
        var descriptor = Descriptor();
        var context = Context(descriptor, "A1-empty-ordinary-mph", new Dictionary<string, string>());
        var otherTenant = new Observation("other", new ObservationSource("test"), Guid.NewGuid(), Now,
            JsonSerializer.SerializeToElement(new Dictionary<string, string>()), "building", "state-1",
            domainPackage: descriptor.Identity);
        Assert.Throws<ArgumentException>(() => new DomainConclusionContext(context.Question,
            context.Package, context.EntityResolutions, [otherTenant]));
        var otherPackage = new Observation("other", new ObservationSource("test"), Tenant, Now,
            JsonSerializer.SerializeToElement(new Dictionary<string, string>()), "building", "state-1",
            domainPackage: ScenarioA1Package.Identity);
        Assert.Throws<ArgumentException>(() => new DomainConclusionContext(context.Question,
            context.Package, context.EntityResolutions, [otherPackage]));
    }

    private static DomainPackageDescriptor Descriptor() => new ScenarioA1OccupiedPackage()
        .ResolveAsync(ScenarioA1OccupiedPackage.Identity).AsTask().GetAwaiter().GetResult().Package!;

    private static DomainConclusionContext Context(DomainPackageDescriptor package, string caseId,
        Dictionary<string, string> facts, string? version = "state-1", bool ambiguous = false)
    {
        var question = new DomainQuestion("entry-" + caseId, Tenant, package.Identity,
            new SemanticIdentifier(package.Identity.DomainId, ScenarioA1OccupiedConclusionResolver.QuestionKind),
            JsonSerializer.SerializeToElement(new
            {
                unitId = "squad", locationId = "building", caseId, observationVersion = "state-1",
            }), Now);
        DomainEntityResolution Entity(string id) => new(
            new DomainEntityQuery("query-" + id, Tenant, package.Identity,
                new SemanticIdentifier(package.Identity.DomainId, "unit-or-location"), id),
            ambiguous && id == "squad" ? DomainEntityResolutionOutcome.Ambiguous
                : DomainEntityResolutionOutcome.Resolved,
            Enumerable.Range(0, ambiguous && id == "squad" ? 2 : 1).Select(index =>
                new DomainEntityCandidate(new SemanticIdentifier(package.Identity.DomainId, id + index),
                package.CanonicalSources[0],
                new EvidenceReference("entity:" + id + index, EvidenceKind.CanonicalSource, Tenant,
                    package.Identity, id, package.Identity.Version, "test"))), ["test.resolution"]);
        var observation = new Observation("state", new ObservationSource("test"), Tenant, Now,
            JsonSerializer.SerializeToElement(facts), "building", version, "test-state-provider", package.Identity);
        return new DomainConclusionContext(question, package, [Entity("squad"), Entity("building")],
            [observation]);
    }
}
