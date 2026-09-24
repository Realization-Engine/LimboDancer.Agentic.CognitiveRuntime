using System.Security.Cryptography;
using System.Text.Json;

namespace LimboDancer.Domains.Asl.Authoring.Tests;

/// <summary>Affirmative xUnit review of the source-backed transition; publishes no domain package.</summary>
public sealed class AslScenarioA1ConcealedSmcTransitionTests
{
    private const string SourceCommit = "a3254ff1d492dbdd28483d86f5b42437b48e80d4";
    private const string ReviewDigest = "d1db66dbc66b00272cf458b341f5789512d4900c405024292c676324dcaf32f0";
    private static readonly string[] Rules = ["A12.15", "A4.14", "A4.15", "A4.151", "A4.152", "B23.4"];
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    [Fact]
    public void AffirmativeReviewPinsTransitionAndEverySourceFragment()
    {
        var path = Path.Combine(RepositoryPaths.Root, "docs", "ASL", "SourceRegistry",
            "asl-scenario-a1.concealed-smc-overrun-transition.json");
        Assert.Equal(ReviewDigest, Digest(path));
        using var document = JsonDocument.Parse(File.ReadAllText(path));
        var review = document.RootElement;
        Assert.Equal("reviewed-source-backed-transition-no-package-publication", review.GetProperty("status").GetString());
        Assert.Equal(AslScenarioA1SourceInventory.PdfDigest, review.GetProperty("sourcePdfSha256").GetString());
        Assert.Equal(Digest(Registry("asl-scenario-a1.occupied-package.json")),
            review.GetProperty("priorOccupiedPackageManifestSha256").GetString());
        Assert.Equal(Digest(Registry("asl-scenario-a1.post-reveal-package.json")),
            review.GetProperty("priorPostRevealPackageManifestSha256").GetString());
        Assert.Equal(Rules, Strings(review.GetProperty("sourceRules")));
        Assert.Equal("asl-supplement:b-terrain-chart-building-entry",
            review.GetProperty("buildingCostSupplement").GetString());

        var manifests = AslAuthoringManifestGenerator.Generate(RepositoryPaths.Root, SourceCommit);
        using var attestationJson = JsonDocument.Parse(File.ReadAllText(Registry("asl-scenario-a1.source-attestation.json")));
        var attestation = JsonSerializer.Deserialize<AslScenarioA1SourceAttestation>(
            attestationJson.RootElement.GetRawText(), JsonOptions)!;
        var baseline = AslScenarioA1VerificationBatchBuilder.Build(manifests, attestation);
        using var comparison = JsonDocument.Parse(File.ReadAllText(Registry("asl-scenario-a1.first-case-pdf-comparison.json")));
        using var chartRegistry = JsonDocument.Parse(File.ReadAllText(Registry("asl-scenario-a1.supplementary-source-registry.json")));
        using var chartComparison = JsonDocument.Parse(File.ReadAllText(Registry("asl-scenario-a1.backmatter-chart-pdf-comparison.json")));
        var chart = AslScenarioA1ChartReview.Evaluate(RepositoryPaths.Root, chartRegistry, chartComparison);
        var firstReview = AslScenarioA1FinalReviewer.Review(RepositoryPaths.Root,
            manifests, attestation, comparison, chart, AslScenarioA1CaseFacts.CreateDeclaredFirstCase());
        var occupied = AslScenarioA1OccupiedSourceReview.Build(RepositoryPaths.Root, manifests, attestation);
        var verified = baseline.Records.Concat(firstReview.NewlyVerifiedRecords)
            .Concat(occupied.Records)
            .Where(record => record.Disposition == TirSourceVerificationDisposition.Verified)
            .Select(record => record.SourceFragment.FragmentId).ToHashSet(StringComparer.Ordinal);
        Assert.Equal(28, verified.Count);
        var evidence = review.GetProperty("sourceFragments").EnumerateArray().ToArray();
        Assert.Equal(Rules.Length, evidence.Length);
        for (var index = 0; index < Rules.Length; index++)
        {
            var rule = Rules[index];
            var fragments = manifests.Fragments.Where(fragment =>
                fragment.Locator.NormalizedElementId == rule
                && fragment.Kind is SourceFragmentKind.RuleText or SourceFragmentKind.RuleContinuation).ToArray();
            Assert.Single(fragments);
            Assert.Contains(fragments[0].FragmentId, verified);
            Assert.Equal(rule, evidence[index].GetProperty("ruleId").GetString());
            Assert.Equal(fragments[0].FragmentId, evidence[index].GetProperty("fragmentId").GetString());
            Assert.Equal(fragments[0].ContentSha256, evidence[index].GetProperty("contentSha256").GetString());
            Assert.Equal(rule == "A12.15" ? 78 : rule == "B23.4" ? 136 : 49,
                evidence[index].GetProperty("physicalPdfPage").GetInt32());
        }
        Assert.Contains(occupied.Records, item => item.SourceFragment.StartLine == 259);
        Assert.Contains(occupied.Records, item => item.SourceFragment.StartLine == 263);
        using var chartDecision = JsonDocument.Parse(File.ReadAllText(Registry("asl-scenario-a1.chart-review-decision.json")));
        Assert.Equal(2, chartDecision.RootElement.GetProperty("woodenBuildingInfantryMf").GetInt32());
        Assert.Equal(2, chartDecision.RootElement.GetProperty("stoneBuildingInfantryMf").GetInt32());
    }

    [Fact]
    public void AffirmativeReviewKeepsControllingFactsAndUnresolvedConsequencesExplicit()
    {
        using var document = JsonDocument.Parse(File.ReadAllText(Registry("asl-scenario-a1.concealed-smc-overrun-transition.json")));
        var review = document.RootElement;
        var facts = review.GetProperty("reviewedFacts");
        Assert.Equal("A12.15-immediate-defender-reveal", facts.GetProperty("revealProvenance").GetString());
        Assert.Equal(["exactlyOneEnemySmc", "otherNonDummy", "dummiesOnly", "unresolved"], Strings(facts.GetProperty("revealedOccupant")));
        Assert.Equal(["elected", "declined", "unknown"], Strings(facts.GetProperty("overrunElection")));
        Assert.Equal(["passed", "failed", "unresolved"], Strings(facts.GetProperty("ntc")));
        Assert.Equal(["atLeastFour", "insufficient", "unknown"], Strings(facts.GetProperty("remainingMf")));
        Assert.Equal(["none", "anotherNonDummyRevealed", "unresolved"], Strings(facts.GetProperty("additionalDefenderReveal")));
        Assert.Equal(["unresolved", "resolvedWithOutcome", "unknown"], Strings(facts.GetProperty("defenderResponseOrImmediateCc")));
        Assert.Equal(7, review.GetProperty("semanticDependencies").GetArrayLength());
        Assert.Contains("No new case, package, state mutation", review.GetProperty("reviewBoundary").GetString());
    }

    private static string[] Strings(JsonElement array) => array.EnumerateArray()
        .Select(item => item.GetString()!).ToArray();
    private static string Registry(string name) => Path.Combine(RepositoryPaths.Root, "docs", "ASL", "SourceRegistry", name);
    private static string Digest(string path) => Convert.ToHexStringLower(SHA256.HashData(File.ReadAllBytes(path)));
}
