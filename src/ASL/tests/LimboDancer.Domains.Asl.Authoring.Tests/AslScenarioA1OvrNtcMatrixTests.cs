using System.Text.Json;
using LimboDancer.Domains.Asl.Authoring;

namespace LimboDancer.Domains.Asl.Authoring.Tests;

/// <summary>
/// The OVR NTC case matrix (unit step 10): the user's rulings on the order of the second reveal and the NTC, the choice
/// of the second defender, and a failed NTC, pinned to verified source fragments and to the packages it follows.
/// </summary>
public sealed class AslScenarioA1OvrNtcMatrixTests
{
    private const string SourceCommit = "a3254ff1d492dbdd28483d86f5b42437b48e80d4";
    private const string MatrixSha256 = "198f1af760eb1bdaed9f40ed867300ecc826b2633fec711c8096325246c536ce";
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    private static readonly (string Rule, int Page)[] Fragments =
        [("A.9", 43), ("A4.15", 49), ("A10.1", 65), ("A12.15", 78), ("B23.3", 136), ("NTC-glossary", 30)];

    private static string Registry(string name) => Path.Combine(RepositoryPaths.Root, "docs", "ASL", "SourceRegistry", name);

    private static JsonDocument Read(string name) => JsonDocument.Parse(File.ReadAllText(Registry(name)));

    [Fact]
    public void TheMatrixPinsItsPredecessorsAndRecordsTheRulings()
    {
        Assert.Equal(MatrixSha256, Hashing.Sha256File(Registry("asl-scenario-a1.ovr-ntc-case-matrix.json")));
        using var matrix = Read("asl-scenario-a1.ovr-ntc-case-matrix.json");
        var root = matrix.RootElement;
        Assert.Equal("reviewed-source-backed-ovr-ntc-candidate-no-package-publication", root.GetProperty("status").GetString());
        Assert.Equal("user-directed-affirmative-xunit-review-2026-09-26", root.GetProperty("authority").GetString());
        Assert.Equal("none", root.GetProperty("executionAuthority").GetString());
        Assert.Equal(AslScenarioA1SourceInventory.PdfDigest, root.GetProperty("sourcePdfSha256").GetString());
        Assert.Equal(Hashing.Sha256File(Registry("asl-scenario-a1.concealed-smc-overrun-package.json")),
            root.GetProperty("priorConcealedSmcOverrunPackageManifestSha256").GetString());
        Assert.Equal(Hashing.Sha256File(Registry("asl-scenario-a1.post-reveal-package.json")),
            root.GetProperty("priorPostRevealPackageManifestSha256").GetString());
        Assert.Equal(Hashing.Sha256File(Registry("asl-scenario-a1.second-defender-consequence-package.json")),
            root.GetProperty("priorSecondDefenderConsequencePackageManifestSha256").GetString());

        var rulings = root.GetProperty("rulings");
        Assert.Equal("ntc-before-second-reveal", rulings.GetProperty("order").GetString());
        Assert.Equal("random-selection-A.9", rulings.GetProperty("secondDefenderSelection").GetString());
        Assert.Equal("forced-back-ordinary-entry-mf-in-previous-location-mph-ended", rulings.GetProperty("failedNtc").GetString());
        var tem = root.GetProperty("ntcResolution").GetProperty("drm").GetProperty("buildingTem");
        Assert.Equal(("B23.3", 3, 2), (tem.GetProperty("source").GetString(), tem.GetProperty("stone").GetInt32(), tem.GetProperty("wooden").GetInt32()));
    }

    [Fact]
    public void EverySourceFragmentIsRegisteredAndVerified()
    {
        var manifests = AslAuthoringManifestGenerator.Generate(RepositoryPaths.Root, SourceCommit);
        using var attestationJson = Read("asl-scenario-a1.source-attestation.json");
        var attestation = JsonSerializer.Deserialize<AslScenarioA1SourceAttestation>(attestationJson.RootElement.GetRawText(), JsonOptions)!;
        using var comparison = Read("asl-scenario-a1.first-case-pdf-comparison.json");
        using var chartRegistry = Read("asl-scenario-a1.supplementary-source-registry.json");
        using var chartComparison = Read("asl-scenario-a1.backmatter-chart-pdf-comparison.json");
        var chart = AslScenarioA1ChartReview.Evaluate(RepositoryPaths.Root, chartRegistry, chartComparison);
        var firstReview = AslScenarioA1FinalReviewer.Review(RepositoryPaths.Root, manifests, attestation, comparison, chart,
            AslScenarioA1CaseFacts.CreateDeclaredFirstCase());
        var verified = AslScenarioA1VerificationBatchBuilder.Build(manifests, attestation).Records
            .Concat(firstReview.NewlyVerifiedRecords)
            .Concat(AslScenarioA1OccupiedSourceReview.Build(RepositoryPaths.Root, manifests, attestation).Records)
            .Concat(AslScenarioA1OvrNtcSourceReview.Build(RepositoryPaths.Root, manifests, attestation).Records)
            .Where(item => item.Disposition == TirSourceVerificationDisposition.Verified)
            .Select(item => item.SourceFragment.FragmentId).ToHashSet(StringComparer.Ordinal);

        using var matrix = Read("asl-scenario-a1.ovr-ntc-case-matrix.json");
        var fragments = matrix.RootElement.GetProperty("sourceFragments").EnumerateArray().ToArray();
        Assert.Equal(Fragments.Length, fragments.Length);
        for (var index = 0; index < Fragments.Length; index++)
        {
            var (rule, page) = Fragments[index];
            var fragmentId = fragments[index].GetProperty("fragmentId").GetString();
            var fragment = Assert.Single(manifests.Fragments, item => item.FragmentId == fragmentId);
            Assert.Contains(fragment.FragmentId, verified);
            Assert.Equal(rule, fragments[index].GetProperty("ruleId").GetString());
            Assert.Equal(page, fragments[index].GetProperty("physicalPdfPage").GetInt32());
            Assert.Equal(fragment.ContentSha256, fragments[index].GetProperty("contentSha256").GetString());
            Assert.Equal(rule == "NTC-glossary" ? null : rule, fragment.Locator.NormalizedElementId);
        }
    }

    [Fact]
    public void TheCasesCoverEveryOutcomeOfAnElection()
    {
        using var matrix = Read("asl-scenario-a1.ovr-ntc-case-matrix.json");
        var cases = matrix.RootElement.GetProperty("cases").EnumerateArray()
            .Select(item => (Id: item.GetProperty("caseId").GetString(), Status: item.GetProperty("reviewStatus").GetString(),
                Disposition: item.GetProperty("expectedDisposition").GetString()))
            .ToArray();
        Assert.Equal(
            [
                ("A1-ovr-ntc-passed-second-defender-revealed", "reviewed-bounded", "forced-back-attempted-entry-mf-in-previous-location"),
                ("A1-ovr-ntc-mf-insufficient", "reviewed-bounded", "election-unavailable"),
                ("A1-ovr-ntc-failed", "reviewed-bounded", "forced-back-attempted-entry-mf-in-previous-location"),
                ("A1-ovr-ntc-passed-against-lone-smc", "reviewed-nondefinitive", "indeterminate"),
                ("A1-ovr-ntc-passed-other-occupants-unknown", "reviewed-nondefinitive", "indeterminate"),
            ],
            cases);
        var ruleIds = matrix.RootElement.GetProperty("sourceFragments").EnumerateArray().Select(item => item.GetProperty("ruleId").GetString()).ToHashSet();
        Assert.All(matrix.RootElement.GetProperty("cases").EnumerateArray(),
            item => Assert.All(item.GetProperty("sourceRules").EnumerateArray(), rule => Assert.Contains(rule.GetString(), ruleIds)));
    }
}
