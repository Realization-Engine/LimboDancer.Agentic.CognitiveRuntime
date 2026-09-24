using LimboDancer.Abstractions.Actions;
using LimboDancer.Abstractions.Domain;
using LimboDancer.Abstractions.Execution;
using LimboDancer.Abstractions.Runtime;
using LimboDancer.Tests.Unit.Runtime;

namespace LimboDancer.Tests.Unit.Domain;

public sealed class DomainConclusionTests
{
    [Fact]
    public void EvidenceBackedConclusionPreservesExactPackageAndProvenance()
    {
        var tenantId = Guid.NewGuid();
        var package = CreatePackage("2026.1");
        var conclusion = CreateConclusion(tenantId, package);

        Assert.Equal("2026.1", conclusion.Question.Package.Version);
        Assert.Equal(package, Assert.Single(conclusion.Evidence).Package);
        Assert.Equal("test-fixture", conclusion.ExplanationProvenance);
        Assert.Equal("rule-entry", Assert.Single(conclusion.ApplicableRules).ElementId);
    }

    [Fact]
    public void ConclusionRejectsCrossTenantOrCrossPackageEvidence()
    {
        var tenantId = Guid.NewGuid();
        var package = CreatePackage("2026.1");
        var question = CreateQuestion(tenantId, package);

        Assert.Throws<ArgumentException>(() => CreateConclusion(
            tenantId,
            package,
            question,
            evidenceTenantId: Guid.NewGuid()));
        Assert.Throws<ArgumentException>(() => CreateConclusion(
            tenantId,
            package,
            question,
            evidencePackage: CreatePackage("2026.2")));
    }

    [Fact]
    public void DispositionControlsValueAndAmbiguityInvariants()
    {
        var tenantId = Guid.NewGuid();
        var package = CreatePackage("2026.1");
        var question = CreateQuestion(tenantId, package);
        var evidence = CreateEvidence(tenantId, package);

        Assert.Throws<ArgumentException>(() => new DomainConclusion(
            "conclusion-1",
            question,
            ConclusionDisposition.Definitive,
            GoalContractsTests.ParseJson("true"),
            [evidence],
            [],
            [],
            [],
            ["occupancy-unresolved"],
            ["eligible"],
            "Entry is eligible.",
            "test-fixture",
            DateTimeOffset.UtcNow));
        Assert.Throws<ArgumentException>(() => new DomainConclusion(
            "conclusion-2",
            question,
            ConclusionDisposition.Indeterminate,
            null,
            [evidence],
            [],
            [],
            [],
            [],
            ["insufficient-evidence"],
            "Entry eligibility cannot be determined.",
            "test-fixture",
            DateTimeOffset.UtcNow));
    }

    [Fact]
    public void ConclusionIsTerminalOutputAndNeverActionAuthority()
    {
        var conclusion = CreateConclusion(Guid.NewGuid(), CreatePackage("2026.1"));
        var result = new GoalResult(
            GoalId.New(),
            GoalLifecycleState.Completed,
            new TerminalReason("domain.concluded"),
            conclusion: conclusion);

        Assert.Same(conclusion, result.Conclusion);
        Assert.False(typeof(ActionCandidate).IsAssignableFrom(typeof(DomainConclusion)));
        Assert.False(typeof(SelectedAction).IsAssignableFrom(typeof(DomainConclusion)));
        Assert.False(typeof(AuthorizedAction).IsAssignableFrom(typeof(DomainConclusion)));
        Assert.DoesNotContain(
            typeof(DomainConclusion).GetProperties(),
            property => property.PropertyType == typeof(AuthorizedAction));
    }

    [Fact]
    public void AbstainedConclusionRequiresAbstainedGoalResult()
    {
        var tenantId = Guid.NewGuid();
        var package = CreatePackage("2026.1");
        var conclusion = new DomainConclusion(
            "conclusion-1",
            CreateQuestion(tenantId, package),
            ConclusionDisposition.Abstained,
            null,
            [CreateEvidence(tenantId, package)],
            [],
            [],
            [],
            ["source-unavailable"],
            ["abstained"],
            "The required evidence source is unavailable.",
            "test-fixture",
            DateTimeOffset.UtcNow);

        Assert.Throws<ArgumentException>(() => new GoalResult(
            GoalId.New(),
            GoalLifecycleState.Completed,
            new TerminalReason("domain.abstained"),
            conclusion: conclusion));
        var result = new GoalResult(
            GoalId.New(),
            GoalLifecycleState.Abstained,
            new TerminalReason("domain.abstained"),
            conclusion: conclusion);
        Assert.Equal(GoalLifecycleState.Abstained, result.TerminalState);
    }

    private static DomainConclusion CreateConclusion(
        Guid tenantId,
        DomainPackageRef package,
        DomainQuestion? question = null,
        Guid? evidenceTenantId = null,
        DomainPackageRef? evidencePackage = null) => new(
            "conclusion-1",
            question ?? CreateQuestion(tenantId, package),
            ConclusionDisposition.Definitive,
            GoalContractsTests.ParseJson("true"),
            [CreateEvidence(evidenceTenantId ?? tenantId, evidencePackage ?? package)],
            [new CanonicalReference(package, "rules", "rule-entry", package.Version)],
            [],
            [],
            [],
            ["eligible"],
            "The evidence satisfies the applicable entry rule.",
            "test-fixture",
            DateTimeOffset.UtcNow);

    private static DomainQuestion CreateQuestion(Guid tenantId, DomainPackageRef package) => new(
        "question-1",
        tenantId,
        package,
        new SemanticIdentifier(package.DomainId, "entry-eligibility"),
        GoalContractsTests.ParseJson("{}"),
        DateTimeOffset.UtcNow);

    private static EvidenceReference CreateEvidence(Guid tenantId, DomainPackageRef package) => new(
        "evidence-1",
        EvidenceKind.Observation,
        tenantId,
        package,
        "building-1",
        "state-42",
        "test-fixture");

    private static DomainPackageRef CreatePackage(string version) => new(
        new DomainId("test-domain"),
        "test-package",
        version);
}
