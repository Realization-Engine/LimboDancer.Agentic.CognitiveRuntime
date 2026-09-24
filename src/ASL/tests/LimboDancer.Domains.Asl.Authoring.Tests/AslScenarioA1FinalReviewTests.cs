using System.Text.Json;

namespace LimboDancer.Domains.Asl.Authoring.Tests;

public sealed class AslScenarioA1FinalReviewTests
{
    private const string SourceCommit = "a3254ff1d492dbdd28483d86f5b42437b48e80d4";
    private static readonly JsonSerializerOptions WebJsonOptions = new(JsonSerializerDefaults.Web);

    [Fact]
    public void DelegatedFidelityAndDomainReviewClosesDeclaredFirstCase()
    {
        var (manifests, attestation, comparison, chart) = Inputs();
        using (comparison)
        {
            var reviewed = AslScenarioA1FinalReviewer.Review(RepositoryPaths.Root,
                manifests, attestation, comparison, chart,
                AslScenarioA1CaseFacts.CreateDeclaredFirstCase());
            using var decision = Read("asl-scenario-a1.first-case-review-decision.json");
            var summary = JsonSerializer.SerializeToElement(new
            {
                reviewed.Status,
                reviewed.ReviewerAuthority,
                reviewed.PdfComparisonSha256,
                reviewed.SourceAttestationSha256,
                reviewed.ChartReviewSha256,
                reviewed.TirDocumentSha256,
                reviewed.NewlyVerifiedFragmentCount,
                reviewed.RequiredRuleIds,
                reviewed.ExcludedBranchRuleIds,
                reviewed.DependencyDecision,
                reviewed.CaseDecision,
            }, WebJsonOptions);
            Assert.True(JsonElement.DeepEquals(decision.RootElement, summary));
            Assert.Equal(10, reviewed.NewlyVerifiedRecords.Count);
            Assert.Equal(10, reviewed.NewlyVerifiedRecords.Select(record =>
                record.SourceFragment.FragmentId).Distinct(StringComparer.Ordinal).Count());
            Assert.All(reviewed.NewlyVerifiedRecords, record =>
            {
                Assert.Equal(TirSourceVerificationDisposition.Verified, record.Disposition);
                Assert.Equal("source-provider:delegated-xunit-review", record.Actor.Identity);
                Assert.NotEmpty(TirReviewCanonicalJson.Serialize(record));
            });
            Assert.Equal(2, reviewed.NewlyVerifiedRecords.Count(record =>
                record.Dependencies.Any(dependency => dependency.Kind == TirDependencyKind.Figure)));
            Assert.True(reviewed.Assessment.CanIssueDefinitiveRuling);
            Assert.Empty(reviewed.Assessment.Blockers);
            Assert.Empty(reviewed.Assessment.UnresolvedSupplementalSources);
        }
    }

    [Fact]
    public void UnknownOrContraryFactsDoNotInheritCaseApproval()
    {
        var (manifests, attestation, comparison, chart) = Inputs();
        using (comparison)
        {
            var reviewed = AslScenarioA1FinalReviewer.Review(RepositoryPaths.Root,
                manifests, attestation, comparison, chart,
                new AslScenarioA1CaseFacts(true, true, true, true, null,
                    true, true, true, false));
            Assert.Equal("outside-declared-case", reviewed.Status);
            Assert.False(reviewed.Assessment.CanIssueDefinitiveRuling);
            Assert.Contains(reviewed.Assessment.Blockers, blocker =>
                blocker.Kind == AslScenarioA1CaseBlockerKind.FactOutsideDeclaredScope);

            using var changed = JsonDocument.Parse(comparison.RootElement.GetRawText().Replace(
                "\"alphanumericSequenceMatch\": true",
                "\"alphanumericSequenceMatch\": false", StringComparison.Ordinal));
            Assert.Throws<InvalidOperationException>(() => AslScenarioA1FinalReviewer.Review(
                RepositoryPaths.Root, manifests, attestation, changed, chart,
                AslScenarioA1CaseFacts.CreateDeclaredFirstCase()));
        }
    }

    private static (GeneratedManifests, AslScenarioA1SourceAttestation, JsonDocument,
        AslScenarioA1ChartReviewDecision) Inputs()
    {
        var manifests = AslAuthoringManifestGenerator.Generate(RepositoryPaths.Root, SourceCommit);
        using var source = Read("asl-scenario-a1.source-attestation.json");
        var attestation = JsonSerializer.Deserialize<AslScenarioA1SourceAttestation>(
            source.RootElement.GetRawText(), WebJsonOptions)!;
        var comparison = Read("asl-scenario-a1.first-case-pdf-comparison.json");
        using var chartEvidence = Read("asl-scenario-a1.supplementary-source-registry.json");
        using var chartComparison = Read("asl-scenario-a1.backmatter-chart-pdf-comparison.json");
        var chart = AslScenarioA1ChartReview.Evaluate(RepositoryPaths.Root,
            chartEvidence, chartComparison);
        return (manifests, attestation, comparison, chart);
    }

    private static JsonDocument Read(string filename) => JsonDocument.Parse(File.ReadAllText(
        Path.Combine(RepositoryPaths.Root, "docs", "ASL", "SourceRegistry", filename)));
}
