using System.Security.Cryptography;
using System.Text.Json;

namespace LimboDancer.Domains.Asl.Authoring.Tests;

/// <summary>Affirmative source and consequence review, without package or execution authority.</summary>
public sealed class AslScenarioA1SecondDefenderConsequenceMatrixTests
{
    private const string SourceCommit = "a3254ff1d492dbdd28483d86f5b42437b48e80d4";
    private const string MatrixDigest = "50375415804bfb8cc0e1a16c09f80a992d4fdcedca19e41234a2a63715214ec2";
    private static readonly string[] Rules = ["A12.15", "A4.14", "A4.15", "B23.4"];
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    [Fact]
    public void AffirmativeReviewPinsPriorEligibilityAndVerifiedFragments()
    {
        Assert.Equal(MatrixDigest, Digest("asl-scenario-a1.second-defender-consequence-case-matrix.json"));
        using var matrix = Read("asl-scenario-a1.second-defender-consequence-case-matrix.json");
        var root = matrix.RootElement;
        Assert.Equal("reviewed-source-backed-consequence-candidate-no-package-publication",
            root.GetProperty("status").GetString());
        Assert.Equal(AslScenarioA1SourceInventory.PdfDigest,
            root.GetProperty("sourcePdfSha256").GetString());
        Assert.Equal(Digest("asl-scenario-a1.second-defender-reveal-case-matrix.json"),
            root.GetProperty("priorEligibilityMatrixSha256").GetString());
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
        Assert.Equal("asl-supplement:b-terrain-chart-building-entry",
            root.GetProperty("buildingCostSupplement").GetString());
        Assert.Equal(2, chartDecision.RootElement.GetProperty("woodenBuildingInfantryMf").GetInt32());
        Assert.Equal(2, chartDecision.RootElement.GetProperty("stoneBuildingInfantryMf").GetInt32());
    }

    [Fact]
    public void AffirmativeReviewAdmitsReturnAndAttemptedEntryMfOnlyForTwoExactReveals()
    {
        using var matrix = Read("asl-scenario-a1.second-defender-consequence-case-matrix.json");
        var root = matrix.RootElement;
        Assert.Equal("knownLastOccupiedLocation",
            root.GetProperty("commonFacts").GetProperty("previousLocation").GetString());
        Assert.Equal("ordinaryBuildingTwoMf",
            root.GetProperty("commonFacts").GetProperty("attemptedEntryMf").GetString());
        Assert.Equal("second-defender-reveal-before-OVR-entry-resolution",
            root.GetProperty("eventOrder").EnumerateArray().Last().GetString());
        Assert.Contains("do not prescribe a universal NTC versus reveal order",
            root.GetProperty("timingBoundary").GetString());
        var cases = root.GetProperty("cases").EnumerateArray().ToArray();
        Assert.Equal(new (string, string)[]
        {
            ("smc-revealed", "forced-back-attempted-entry-mf-in-previous-location"),
            ("mmc-revealed", "forced-back-attempted-entry-mf-in-previous-location"),
            ("other-type", "indeterminate"),
            ("unknown-type", "indeterminate"),
            ("unrevealed-smc", "indeterminate"),
            ("capability-unresolved", "indeterminate"),
            ("mf-insufficient", "abstained"),
        }, cases.Select(item => (item.GetProperty("caseId").GetString()!
            .Substring("A1-second-defender-consequence-".Length),
            item.GetProperty("expectedDisposition").GetString()!)));
        Assert.All(cases, item => Assert.Equal(Rules, item.GetProperty("sourceRules")
            .EnumerateArray().Select(rule => rule.GetString()).ToArray()));
        Assert.Equal("revealedEnemySmc-after-election",
            cases[0].GetProperty("facts").GetProperty("secondReveal").GetString());
        Assert.Equal("revealedEnemyMmc-after-election",
            cases[1].GetProperty("facts").GetProperty("secondReveal").GetString());
        Assert.All(cases.Take(2), item => Assert.Equal("passedNtcAndAtLeastFourMf",
            item.GetProperty("facts").GetProperty("attackerCapability").GetString()));
        Assert.Contains("additional doubled OVR MF charge",
            root.GetProperty("excludedConsequences").EnumerateArray().Select(item => item.GetString()));
        Assert.Contains("game-state mutation",
            root.GetProperty("excludedConsequences").EnumerateArray().Select(item => item.GetString()));
    }

    private static JsonDocument Read(string name) => JsonDocument.Parse(File.ReadAllText(
        Path.Combine(RepositoryPaths.Root, "docs", "ASL", "SourceRegistry", name)));
    private static string Digest(string name) => Convert.ToHexStringLower(SHA256.HashData(
        File.ReadAllBytes(Path.Combine(RepositoryPaths.Root, "docs", "ASL", "SourceRegistry", name))));
}
