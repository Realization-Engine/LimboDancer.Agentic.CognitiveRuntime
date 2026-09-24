using System.Text.Json;

namespace LimboDancer.Domains.Asl.Authoring.Tests;

public sealed class AslScenarioA1ChartReviewTests
{
    private const string SourceCommit = "a3254ff1d492dbdd28483d86f5b42437b48e80d4";
    private const string RegistryHash = "f73705eb8d8d3e3350bcbe81ab60e433f516bf7c691132a4f9ee866463a7bbef";
    private const string ComparisonHash = "d0a0fa8db20eabce3890f87e2fda3418fae147d1493f4190bf6658ee6ceef520";

    [Fact]
    public void UserDelegatedReviewAcceptsPinnedChartFidelityAndCaseSpecificInterpretation()
    {
        using var registry = Read("asl-scenario-a1.supplementary-source-registry.json");
        using var comparison = Read("asl-scenario-a1.backmatter-chart-pdf-comparison.json");
        using var published = Read("asl-scenario-a1.chart-review-decision.json");
        var decision = AslScenarioA1ChartReview.Evaluate(RepositoryPaths.Root, registry, comparison);

        Assert.Equal(RegistryHash, decision.RegistrySha256);
        Assert.Equal(ComparisonHash, decision.ComparisonSha256);
        Assert.Equal(AslScenarioA1ChartReview.AcceptedStatus, decision.Status);
        Assert.Equal(AslScenarioA1ChartReview.Authority, decision.ReviewerAuthority);
        Assert.Equal(2, decision.WoodenBuildingInfantryMf);
        Assert.Equal(2, decision.StoneBuildingInfantryMf);
        Assert.Equal(AslScenarioA1ChartReview.Note, decision.StoneRowNote);
        Assert.Contains("Stone building row only", decision.NoteAppliesTo);
        Assert.Contains("excludes road and VBM", decision.NoteAppliesTo);
        Assert.Contains("Controlling Infantry MF entrance-cost table", decision.ChartRole);
        Assert.Equal(new[] { "A4.13", "B23.4" }, decision.RuleBasis);
        var generated = JsonSerializer.SerializeToElement(decision,
            new JsonSerializerOptions(JsonSerializerDefaults.Web));
        Assert.True(JsonElement.DeepEquals(published.RootElement, generated));

        var manifests = AslAuthoringManifestGenerator.Generate(RepositoryPaths.Root, SourceCommit);
        var verified = manifests.Fragments.Select(fragment => fragment.FragmentId)
            .ToHashSet(StringComparer.Ordinal);
        var result = AslScenarioA1CaseAssessor.AssessWithReviewedChart(
            AslScenarioA1CaseFacts.CreateDeclaredFirstCase(), manifests.Fragments,
            verified, RepositoryPaths.Root, decision);
        Assert.Empty(result.UnresolvedSupplementalSources);
        Assert.DoesNotContain(result.Blockers, blocker =>
            blocker.Kind == AslScenarioA1CaseBlockerKind.SourceBoundaryUnresolved);
        Assert.Contains(result.Blockers, blocker =>
            blocker.Kind == AslScenarioA1CaseBlockerKind.DependencyAndSemanticReviewPending);
        Assert.False(result.CanIssueDefinitiveRuling);
    }

    [Fact]
    public void AlteredEvidenceOrDecisionCannotInheritTheDelegatedReview()
    {
        using var registry = Read("asl-scenario-a1.supplementary-source-registry.json");
        using var comparison = Read("asl-scenario-a1.backmatter-chart-pdf-comparison.json");
        using var wrongCost = JsonDocument.Parse(registry.RootElement.GetRawText().Replace(
            "\"infantryEntryMf\": 2", "\"infantryEntryMf\": 3", StringComparison.Ordinal));
        Assert.Throws<InvalidOperationException>(() => AslScenarioA1ChartReview.Evaluate(
            RepositoryPaths.Root, wrongCost, comparison));
        using var wrongComparison = JsonDocument.Parse(comparison.RootElement.GetRawText().Replace(
            "\"matchesTranscription\": true", "\"matchesTranscription\": false", StringComparison.Ordinal));
        Assert.Throws<InvalidOperationException>(() => AslScenarioA1ChartReview.Evaluate(
            RepositoryPaths.Root, registry, wrongComparison));

        var decision = AslScenarioA1ChartReview.Evaluate(RepositoryPaths.Root, registry, comparison);
        var manifests = AslAuthoringManifestGenerator.Generate(RepositoryPaths.Root, SourceCommit);
        Assert.Throws<InvalidOperationException>(() => AslScenarioA1CaseAssessor.AssessWithReviewedChart(
            AslScenarioA1CaseFacts.CreateDeclaredFirstCase(), manifests.Fragments,
            new HashSet<string>(StringComparer.Ordinal), RepositoryPaths.Root,
            decision with { ChartRole = "The chart is optional" }));
    }

    private static JsonDocument Read(string filename) => JsonDocument.Parse(File.ReadAllText(
        Path.Combine(RepositoryPaths.Root, "docs", "ASL", "SourceRegistry", filename)));
}
