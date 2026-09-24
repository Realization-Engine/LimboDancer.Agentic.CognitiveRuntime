using System.Text.Json;

namespace LimboDancer.Domains.Asl.Authoring.Tests;

/// <summary>Delegated source and domain review for the declared occupied-building cases.</summary>
public sealed class AslScenarioA1OccupiedMatrixTests
{
    private const string SourceCommit = "a3254ff1d492dbdd28483d86f5b42437b48e80d4";
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    [Fact]
    public void DelegatedReviewPinsTheFullCaseMatrixAndItsOutcomes()
    {
        using var document = Read("asl-scenario-a1.occupied-case-matrix.json");
        var root = document.RootElement;
        Assert.Equal("1.0.0", root.GetProperty("schemaVersion").GetString());
        Assert.Equal("user-delegated-xunit-review-bounded", root.GetProperty("status").GetString());
        Assert.Equal("asl-scenario-a1.first-case-review-decision.json",
            root.GetProperty("sourceDecision").GetString());
        var cases = root.GetProperty("cases").EnumerateArray().ToArray();
        var approved = new (string Id, string Review, string Outcome)[]
        {
            ("A1-empty-ordinary-mph", "reviewed-bounded", "eligible-2mf"),
            ("A1-known-enemy-mmc-mph", "reviewed-bounded", "prohibited"),
            ("A1-fortified-unbreached-enemy-squad", "reviewed-bounded", "prohibited"),
            ("A1-concealed-occupancy-attempt", "reviewed-nondefinitive", "indeterminate"),
            ("A1-single-known-enemy-smc-overrun", "reviewed-bounded", "qualified-overrun-entry-attempt-4mf"),
            ("A1-fortified-breached-entry", "deferred", "abstained"),
            ("A1-stacking-equivalents-needed", "deferred", "abstained"),
            ("A1-advance-phase-entry", "deferred", "abstained"),
            ("A1-unknown-location-or-modifier", "reviewed-nondefinitive", "indeterminate"),
        };
        Assert.Equal(approved.Length, cases.Length);
        Assert.Equal(approved, cases.Select(item => (
            item.GetProperty("caseId").GetString()!,
            item.GetProperty("reviewStatus").GetString()!,
            item.GetProperty("expectedDisposition").GetString()!)));
        Assert.All(cases, item =>
        {
            Assert.False(string.IsNullOrWhiteSpace(item.GetProperty("declaredFacts").GetString()));
            Assert.False(string.IsNullOrWhiteSpace(item.GetProperty("reason").GetString()));
            Assert.NotEmpty(item.GetProperty("sourceRules").EnumerateArray());
        });
        Assert.Single(cases, item => item.GetProperty("expectedDisposition").GetString() == "eligible-2mf");
        Assert.Equal(2, cases.Count(item => item.GetProperty("expectedDisposition").GetString() == "prohibited"));
        Assert.Single(cases, item => item.GetProperty("expectedDisposition").GetString()
            == "qualified-overrun-entry-attempt-4mf");
    }

    [Fact]
    public void EveryReviewedRuleHasExactVerifiedFragmentsAndDeferredRulesRemainVisible()
    {
        var manifests = AslAuthoringManifestGenerator.Generate(RepositoryPaths.Root, SourceCommit);
        using var attestationJson = Read("asl-scenario-a1.source-attestation.json");
        var attestation = JsonSerializer.Deserialize<AslScenarioA1SourceAttestation>(
            attestationJson.RootElement.GetRawText(), JsonOptions)!;
        using var comparison = Read("asl-scenario-a1.first-case-pdf-comparison.json");
        using var chartRegistry = Read("asl-scenario-a1.supplementary-source-registry.json");
        using var chartComparison = Read("asl-scenario-a1.backmatter-chart-pdf-comparison.json");
        var chart = AslScenarioA1ChartReview.Evaluate(RepositoryPaths.Root,
            chartRegistry, chartComparison);
        var reviewed = AslScenarioA1FinalReviewer.Review(RepositoryPaths.Root,
            manifests, attestation, comparison, chart, AslScenarioA1CaseFacts.CreateDeclaredFirstCase());
        var occupied = AslScenarioA1OccupiedSourceReview.Build(RepositoryPaths.Root,
            manifests, attestation);
        Assert.Equal(7, occupied.Records.Count);
        Assert.Equal(reviewed.TirDocumentSha256,
            TirCanonicalJson.ComputePayloadSha256(occupied.SourceDocument));
        Assert.All(occupied.Records, record =>
        {
            Assert.Equal(TirSourceVerificationDisposition.Verified, record.Disposition);
            Assert.Equal("source-provider:delegated-xunit-review", record.Actor.Identity);
        });
        var baseline = AslScenarioA1VerificationBatchBuilder.Build(manifests, attestation);
        Assert.Equal(28, baseline.Records.Count + reviewed.NewlyVerifiedRecords.Count
            + occupied.Records.Count);
        Assert.Contains(baseline.Records, record => record.SourceFragment.StartLine == 2079);
        var verified = baseline.Records
            .Concat(reviewed.NewlyVerifiedRecords)
            .Concat(occupied.Records)
            .Select(record => record.SourceFragment.FragmentId).ToHashSet(StringComparer.Ordinal);
        using var matrix = Read("asl-scenario-a1.occupied-case-matrix.json");
        var cases = matrix.RootElement.GetProperty("cases").EnumerateArray().ToArray();
        foreach (var item in cases)
        {
            foreach (var rule in item.GetProperty("sourceRules").EnumerateArray())
            {
                var id = rule.GetString()!;
                var fragments = RuleFragments(manifests, id);
                Assert.Contains(fragments, fragment => fragment.Kind == SourceFragmentKind.RuleText);
                Assert.All(fragments, fragment => Assert.Contains(fragment.FragmentId, verified));
            }
            if (item.TryGetProperty("semanticDependencies", out var unresolved))
            {
                Assert.Equal("deferred", item.GetProperty("reviewStatus").GetString());
                Assert.NotEmpty(unresolved.EnumerateArray());
            }
        }
        var first = cases[0];
        Assert.Equal(reviewed.RequiredRuleIds,
            first.GetProperty("sourceRules").EnumerateArray().Select(rule => rule.GetString()!).ToArray());
        Assert.Equal(AslScenarioA1ChartReview.CandidateId,
            Assert.Single(first.GetProperty("supplements").EnumerateArray()).GetString());
        Assert.Contains("B23.711", cases[5].GetProperty("sourceRules").EnumerateArray()
            .Select(rule => rule.GetString()));
        Assert.Contains("A5.5", cases[6].GetProperty("sourceRules").EnumerateArray()
            .Select(rule => rule.GetString()));
        Assert.Contains(occupied.Records, record => record.SourceFragment.StartLine == 2087);
        Assert.Contains(occupied.Records, record => record.SourceFragment.StartLine == 1438);
        Assert.Equal(new[] { "A2.8", "A4.13", "A4.14", "A4.15", "A4.151", "A4.152", "B23.4" },
            cases[4].GetProperty("sourceRules").EnumerateArray()
                .Select(rule => rule.GetString()!).ToArray());
        Assert.Equal(AslScenarioA1ChartReview.CandidateId,
            Assert.Single(cases[4].GetProperty("supplements").EnumerateArray()).GetString());
    }

    private static SourceFragment[] RuleFragments(GeneratedManifests manifests, string id) =>
        manifests.Fragments.Where(fragment => fragment.Locator.NormalizedElementId == id
            && fragment.Kind is SourceFragmentKind.RuleText
                or SourceFragmentKind.RuleContinuation
                or SourceFragmentKind.FigureReference).ToArray();

    private static JsonDocument Read(string name) => JsonDocument.Parse(File.ReadAllText(
        Path.Combine(RepositoryPaths.Root, "docs", "ASL", "SourceRegistry", name)));
}
