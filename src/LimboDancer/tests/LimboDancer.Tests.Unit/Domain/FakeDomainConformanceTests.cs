using LimboDancer.Abstractions.Domain;
using LimboDancer.Abstractions.Observations;
using LimboDancer.Runtime.Domain;
using LimboDancer.Tests.Unit.Observations;
using LimboDancer.Tests.Unit.Runtime;

namespace LimboDancer.Tests.Unit.Domain;

public sealed class FakeDomainConformanceTests
{
    [Fact]
    public async Task FakeDomainComposesThroughSeparateApprovedPorts()
    {
        var tenantId = Guid.NewGuid();
        var package = DomainResolutionContractsTests.CreatePackage("1.0");
        IDomainPackageResolver packageResolver = new DomainPackageRegistry(
            [CreatePackageDescriptor(package)]);
        IDomainEntityResolver entityResolver = new FakeEntityResolver();
        IObservationProvider observationProvider = new FakeObservationProvider();
        IDomainConclusionResolver conclusionResolver = new FakeConclusionResolver();

        var packageResult = await packageResolver.ResolveAsync(package);
        var entityResult = await entityResolver.ResolveAsync(
            DomainResolutionContractsTests.CreateEntityQuery(tenantId, package));
        var observationResult = await observationProvider.ObserveAsync(
            ObservationAcquisitionContractsTests.CreateQuery(tenantId, package));
        var conclusion = await conclusionResolver.ConcludeAsync(new DomainConclusionContext(
            CreateQuestion(tenantId, package),
            packageResult.Package!,
            [entityResult],
            observationResult.Observations));

        Assert.Equal(ConclusionDisposition.Definitive, conclusion.Disposition);
        Assert.True(conclusion.Value!.Value.GetBoolean());
        Assert.NotEmpty(conclusion.Evidence);
    }

    [Theory]
    [InlineData("unknown", false, false, "entity.unresolved")]
    [InlineData("ambiguous", false, false, "entity.ambiguous")]
    [InlineData("subject-1", true, false, "observation.stale")]
    [InlineData("subject-1", false, true, "evidence.conflicting")]
    public async Task MaterialResolutionProblemsProduceIndeterminateConclusion(
        string entityReference,
        bool stale,
        bool conflicting,
        string expectedReason)
    {
        var tenantId = Guid.NewGuid();
        var package = DomainResolutionContractsTests.CreatePackage("1.0");
        var entityResolver = new FakeEntityResolver();
        var entity = await entityResolver.ResolveAsync(new DomainEntityQuery(
            "entity-query-1",
            tenantId,
            package,
            new SemanticIdentifier(package.DomainId, "fake-entity-kind"),
            entityReference));
        var observation = ObservationAcquisitionContractsTests.CreateObservation(
            tenantId,
            package,
            stale ? null : "state-42",
            conflicting ? """{"conflicting":true}""" : "{}");
        var resolver = new FakeConclusionResolver();

        var conclusion = await resolver.ConcludeAsync(new DomainConclusionContext(
            CreateQuestion(tenantId, package),
            CreatePackageDescriptor(package),
            [entity],
            [observation]));

        Assert.Equal(ConclusionDisposition.Indeterminate, conclusion.Disposition);
        Assert.Contains(expectedReason, conclusion.ReasonCodes);
        Assert.NotEmpty(conclusion.Ambiguities);
    }

    [Fact]
    public void ConclusionContextRejectsCrossTenantObservationBeforeDomainEvaluation()
    {
        var tenantId = Guid.NewGuid();
        var package = DomainResolutionContractsTests.CreatePackage("1.0");

        Assert.Throws<ArgumentException>(() => new DomainConclusionContext(
            CreateQuestion(tenantId, package),
            CreatePackageDescriptor(package),
            [],
            [ObservationAcquisitionContractsTests.CreateObservation(
                Guid.NewGuid(),
                package,
                "state-42")]));
    }

    [Fact]
    public void RuntimeRemainsIndependentOfConcreteDomainAssemblies()
    {
        var references = typeof(DomainPackageRegistry).Assembly.GetReferencedAssemblies();

        Assert.DoesNotContain(
            references,
            reference => reference.Name?.Contains("ASL", StringComparison.OrdinalIgnoreCase) == true);
        Assert.DoesNotContain(
            references,
            reference => reference.Name?.Contains("FakeDomain", StringComparison.OrdinalIgnoreCase) == true);
    }

    private static DomainPackageDescriptor CreatePackageDescriptor(DomainPackageRef package) => new(
        package,
        [DomainResolutionContractsTests.CreateSource(package)]);

    private static DomainQuestion CreateQuestion(Guid tenantId, DomainPackageRef package) => new(
        "question-1",
        tenantId,
        package,
        new SemanticIdentifier(package.DomainId, "fake-eligibility"),
        GoalContractsTests.ParseJson("{}"),
        DateTimeOffset.UtcNow);

    private sealed class FakeObservationProvider : IObservationProvider
    {
        public ValueTask<ObservationAcquisitionResult> ObserveAsync(
            ObservationQuery query,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var observation = ObservationAcquisitionContractsTests.CreateObservation(
                query.TenantId,
                query.Package,
                "state-42");
            return ValueTask.FromResult(new ObservationAcquisitionResult(query, [observation]));
        }
    }

    private sealed class FakeEntityResolver : IDomainEntityResolver
    {
        public ValueTask<DomainEntityResolution> ResolveAsync(
            DomainEntityQuery query,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return query.Reference switch
            {
                "subject-1" => ValueTask.FromResult(new DomainEntityResolution(
                    query,
                    DomainEntityResolutionOutcome.Resolved,
                    [DomainResolutionContractsTests.CreateEntity(query.TenantId, query.Package, "subject-1")],
                    ["entity.resolved"])),
                "ambiguous" => ValueTask.FromResult(new DomainEntityResolution(
                    query,
                    DomainEntityResolutionOutcome.Ambiguous,
                    [
                        DomainResolutionContractsTests.CreateEntity(query.TenantId, query.Package, "subject-1"),
                        DomainResolutionContractsTests.CreateEntity(query.TenantId, query.Package, "subject-2"),
                    ],
                    ["entity.ambiguous"])),
                _ => ValueTask.FromResult(new DomainEntityResolution(
                    query,
                    DomainEntityResolutionOutcome.Unresolved,
                    [],
                    ["entity.unresolved"])),
            };
        }
    }

    private sealed class FakeConclusionResolver : IDomainConclusionResolver
    {
        public ValueTask<DomainConclusion> ConcludeAsync(
            DomainConclusionContext context,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var problems = new List<string>();
            problems.AddRange(context.EntityResolutions
                .Where(static result => result.Outcome != DomainEntityResolutionOutcome.Resolved)
                .SelectMany(static result => result.ReasonCodes));
            if (context.Observations.Any(static observation => observation.Version is null))
            {
                problems.Add("observation.stale");
            }

            if (context.Observations.Any(static observation =>
                    observation.Data.TryGetProperty("conflicting", out var value)
                    && value.ValueKind == System.Text.Json.JsonValueKind.True))
            {
                problems.Add("evidence.conflicting");
            }

            var evidence = new List<EvidenceReference>
            {
                new(
                    "package-source",
                    EvidenceKind.CanonicalSource,
                    context.Question.TenantId,
                    context.Package.Identity,
                    "fake-source",
                    context.Package.Identity.Version,
                    "fake-domain"),
            };
            evidence.AddRange(context.Observations.Select(observation => new EvidenceReference(
                $"evidence-{observation.ObservationId}",
                EvidenceKind.Observation,
                observation.TenantId,
                context.Package.Identity,
                observation.ResourceId ?? observation.ObservationId,
                observation.Version,
                observation.Provenance ?? observation.Source.SourceId)));
            var isDefinitive = problems.Count == 0;
            return ValueTask.FromResult(new DomainConclusion(
                "conclusion-1",
                context.Question,
                isDefinitive ? ConclusionDisposition.Definitive : ConclusionDisposition.Indeterminate,
                isDefinitive ? GoalContractsTests.ParseJson("true") : null,
                evidence,
                context.Package.CanonicalSources,
                [],
                [],
                problems,
                isDefinitive ? ["fake.eligible"] : problems,
                isDefinitive
                    ? "The supplied evidence satisfies the fake eligibility rule."
                    : "The fake eligibility question cannot be determined from the supplied evidence.",
                "fake-domain",
                DateTimeOffset.UtcNow));
        }
    }
}
