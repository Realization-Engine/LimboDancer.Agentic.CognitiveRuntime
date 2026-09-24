using System.Security.Cryptography;
using System.Text.Json;

namespace LimboDancer.Domains.Asl.Authoring.Tests;

/// <summary>Affirmative review of A12.15 effects and the execution boundary; grants no authority.</summary>
public sealed class AslScenarioA1SecondDefenderExecutionReviewTests
{
    private const string SourceCommit = "a3254ff1d492dbdd28483d86f5b42437b48e80d4";
    private const string ReviewDigest = "ccec677b126067aa5cca45656b945cd8f34e870ed868b36225defe265e9d2693";
    private static readonly string[] Rules = ["A12.15", "A4.14", "A4.15", "B23.4"];
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    [Fact]
    public void ReviewPinsExactConsequenceAndVerifiedSourceSubjects()
    {
        Assert.Equal(ReviewDigest, Digest("asl-scenario-a1.second-defender-execution-review.json"));
        using var review = Read("asl-scenario-a1.second-defender-execution-review.json");
        var root = review.RootElement;
        Assert.Equal("reviewed-execution-boundary-no-executor-or-state-mutation",
            root.GetProperty("status").GetString());
        Assert.Equal(AslScenarioA1SourceInventory.PdfDigest,
            root.GetProperty("sourcePdfSha256").GetString());
        Assert.Equal(Digest("asl-scenario-a1.second-defender-consequence-case-matrix.json"),
            root.GetProperty("priorConsequenceMatrixSha256").GetString());
        Assert.Equal(Digest("asl-scenario-a1.second-defender-consequence-package.json"),
            root.GetProperty("priorConsequencePackageSha256").GetString());
        Assert.Equal("none", root.GetProperty("executionAuthority").GetString());

        var manifests = AslAuthoringManifestGenerator.Generate(RepositoryPaths.Root, SourceCommit);
        using var attestationJson = Read("asl-scenario-a1.source-attestation.json");
        var attestation = JsonSerializer.Deserialize<AslScenarioA1SourceAttestation>(
            attestationJson.RootElement.GetRawText(), JsonOptions)!;
        using var comparison = Read("asl-scenario-a1.first-case-pdf-comparison.json");
        using var chartRegistry = Read("asl-scenario-a1.supplementary-source-registry.json");
        using var chartComparison = Read("asl-scenario-a1.backmatter-chart-pdf-comparison.json");
        var chart = AslScenarioA1ChartReview.Evaluate(RepositoryPaths.Root, chartRegistry, chartComparison);
        var firstReview = AslScenarioA1FinalReviewer.Review(RepositoryPaths.Root,
            manifests, attestation, comparison, chart, AslScenarioA1CaseFacts.CreateDeclaredFirstCase());
        var occupied = AslScenarioA1OccupiedSourceReview.Build(RepositoryPaths.Root, manifests, attestation);
        var verified = AslScenarioA1VerificationBatchBuilder.Build(manifests, attestation).Records
            .Concat(firstReview.NewlyVerifiedRecords).Concat(occupied.Records)
            .Where(item => item.Disposition == TirSourceVerificationDisposition.Verified)
            .Select(item => item.SourceFragment.FragmentId).ToHashSet(StringComparer.Ordinal);
        var fragments = root.GetProperty("sourceFragments").EnumerateArray().ToArray();
        Assert.Equal(Rules.Length, fragments.Length);
        for (var index = 0; index < Rules.Length; index++)
        {
            var rule = Rules[index];
            var fragment = Assert.Single(manifests.Fragments, item =>
                item.Locator.NormalizedElementId == rule && item.Kind == SourceFragmentKind.RuleText);
            Assert.Contains(fragment.FragmentId, verified);
            Assert.Equal(rule, fragments[index].GetProperty("ruleId").GetString());
            Assert.Equal(fragment.FragmentId, fragments[index].GetProperty("fragmentId").GetString());
            Assert.Equal(fragment.ContentSha256,
                fragments[index].GetProperty("contentSha256").GetString());
            Assert.Equal(rule == "A12.15" ? 78 : rule == "B23.4" ? 136 : 49,
                fragments[index].GetProperty("physicalPdfPage").GetInt32());
        }
        using var chartDecision = Read("asl-scenario-a1.chart-review-decision.json");
        Assert.Equal(2, chartDecision.RootElement.GetProperty("woodenBuildingInfantryMf").GetInt32());
        Assert.Equal(2, chartDecision.RootElement.GetProperty("stoneBuildingInfantryMf").GetInt32());
    }

    [Fact]
    public void ReviewSeparatesCoreEffectsConditionalAttacksAndRuntimeAuthority()
    {
        using var review = Read("asl-scenario-a1.second-defender-execution-review.json");
        var root = review.RootElement;
        Assert.Equal(new[]
        {
            "A1-second-defender-consequence-smc-revealed",
            "A1-second-defender-consequence-mmc-revealed",
        }, Strings(root.GetProperty("admittedCaseIds")));
        Assert.Equal(new[]
        {
            "return-to-last-location", "attempted-entry-mf-in-return-location",
            "attacker-concealment-lost", "moving-phase-ended",
        }, root.GetProperty("coreEffectsFromA1215").EnumerateArray()
            .Select(item => item.GetProperty("effect").GetString()).ToArray());
        Assert.Equal(new[]
        {
            "existing-residual-fp-in-return-location",
            "return-location-ffe-or-minefield",
            "return-location-wire-depression-entrenchment-or-shellhole",
            "defender-first-fire-or-snap-shot-on-return",
        }, root.GetProperty("conditionalFollowOn").EnumerateArray()
            .Select(item => item.GetProperty("trigger").GetString()).ToArray());
        Assert.Contains("compare-and-swap", root.GetProperty("runtimeBoundary")
            .GetProperty("version").GetString());
        Assert.Contains("must not debit MF twice", root.GetProperty("runtimeBoundary")
            .GetProperty("idempotency").GetString());
        Assert.Contains("unknown-or-active-return-location-hazard", Strings(root.GetProperty("failClosed")));
        Assert.Contains("stable-idempotency-key-for-this-attempt", Strings(root.GetProperty("requiredInput")));
        Assert.Contains("no Residual FP, FFE, minefield", root.GetProperty("initialExecutionSubset").GetString());
        Assert.Equal("none", root.GetProperty("executionAuthority").GetString());
    }

    private static string[] Strings(JsonElement array) => array.EnumerateArray()
        .Select(item => item.GetString()!).ToArray();
    private static JsonDocument Read(string name) => JsonDocument.Parse(File.ReadAllText(
        Path.Combine(RepositoryPaths.Root, "docs", "ASL", "SourceRegistry", name)));
    private static string Digest(string name) => Convert.ToHexStringLower(SHA256.HashData(
        File.ReadAllBytes(Path.Combine(RepositoryPaths.Root, "docs", "ASL", "SourceRegistry", name))));
}
