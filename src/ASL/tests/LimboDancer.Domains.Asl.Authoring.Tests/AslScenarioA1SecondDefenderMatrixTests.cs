using System.Security.Cryptography;
using System.Text.Json;

namespace LimboDancer.Domains.Asl.Authoring.Tests;

/// <summary>Affirmative source and semantic review; no package or world-state authority.</summary>
public sealed class AslScenarioA1SecondDefenderMatrixTests
{
    private const string SourceCommit = "a3254ff1d492dbdd28483d86f5b42437b48e80d4";
    private const string MatrixDigest = "7db79dc8a634712983b5a14c5b4353b6a57038c89c822012c614e99105e34e6c";
    private static readonly string[] Rules = ["A12.15", "A4.14", "A4.15"];
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    [Fact]
    public void AffirmativeReviewPinsPriorPackageAndVerifiedSourceSubjects()
    {
        Assert.Equal(MatrixDigest, Digest("asl-scenario-a1.second-defender-reveal-case-matrix.json"));
        using var matrix = Read("asl-scenario-a1.second-defender-reveal-case-matrix.json");
        var root = matrix.RootElement;
        Assert.Equal("reviewed-source-backed-second-defender-candidate-no-package-publication",
            root.GetProperty("status").GetString());
        Assert.Equal(AslScenarioA1SourceInventory.PdfDigest,
            root.GetProperty("sourcePdfSha256").GetString());
        Assert.Equal(Digest("asl-scenario-a1.concealed-smc-overrun-transition.json"),
            root.GetProperty("priorTransitionReviewSha256").GetString());
        Assert.Equal(Digest("asl-scenario-a1.concealed-smc-overrun-package.json"),
            root.GetProperty("priorPackageManifestSha256").GetString());
        Assert.Equal(Rules, Strings(root.GetProperty("sourceRules")));
        Assert.Equal("none", root.GetProperty("executionAuthority").GetString());

        var manifests = AslAuthoringManifestGenerator.Generate(RepositoryPaths.Root, SourceCommit);
        using var attestationJson = Read("asl-scenario-a1.source-attestation.json");
        var attestation = JsonSerializer.Deserialize<AslScenarioA1SourceAttestation>(
            attestationJson.RootElement.GetRawText(), JsonOptions)!;
        var verified = AslScenarioA1VerificationBatchBuilder.Build(manifests, attestation).Records
            .Where(item => item.Disposition == TirSourceVerificationDisposition.Verified)
            .Select(item => item.SourceFragment.FragmentId).ToHashSet(StringComparer.Ordinal);
        var fragments = root.GetProperty("sourceFragments").EnumerateArray().ToArray();
        Assert.Equal(Rules.Length, fragments.Length);
        for (var index = 0; index < Rules.Length; index++)
        {
            var rule = Rules[index];
            var fragment = Assert.Single(manifests.Fragments, item =>
                item.Locator.NormalizedElementId == rule && item.Kind == SourceFragmentKind.RuleText);
            Assert.Equal(rule, fragments[index].GetProperty("ruleId").GetString());
            Assert.Equal(fragment.FragmentId, fragments[index].GetProperty("fragmentId").GetString());
            Assert.Equal(fragment.ContentSha256,
                fragments[index].GetProperty("contentSha256").GetString());
            Assert.Equal(rule == "A12.15" ? 78 : 49,
                fragments[index].GetProperty("physicalPdfPage").GetInt32());
            Assert.Contains(fragment.FragmentId, verified);
        }
    }

    [Fact]
    public void AffirmativeReviewDistinguishesRevealedSmcMmcAndUnresolvedFacts()
    {
        using var matrix = Read("asl-scenario-a1.second-defender-reveal-case-matrix.json");
        var root = matrix.RootElement;
        Assert.Equal(["A12.15-first-SMC-reveal", "attacker-OVR-election",
            "second-defender-reveal-if-available"], Strings(root.GetProperty("eventOrder")));
        Assert.Contains("do not impose NTC-before-second-reveal",
            root.GetProperty("eventOrderBoundary").GetString());
        Assert.Equal("enemySmc-under-A12.15",
            root.GetProperty("commonFacts").GetProperty("firstReveal").GetString());
        var cases = root.GetProperty("cases").EnumerateArray().ToArray();
        Assert.Equal(new (string Id, string Disposition)[]
        {
            ("smc-revealed", "single-smc-ovr-denied-by-two-revealed-smc"),
            ("mmc-revealed", "single-smc-ovr-inapplicable-revealed-mmc"),
            ("other-type", "indeterminate"),
            ("unknown-type", "indeterminate"),
            ("unrevealed-smc", "indeterminate"),
            ("capability-unresolved", "indeterminate"),
            ("mf-insufficient", "abstained"),
        }, cases.Select(item => (item.GetProperty("caseId").GetString()!
            .Substring("A1-second-defender-".Length),
            item.GetProperty("expectedDisposition").GetString()!)));
        Assert.All(cases, item =>
        {
            Assert.Equal(Rules, Strings(item.GetProperty("sourceRules")));
            Assert.Equal(item.GetProperty("expectedDisposition").GetString() is
                "single-smc-ovr-denied-by-two-revealed-smc" or
                "single-smc-ovr-inapplicable-revealed-mmc"
                    ? "reviewed-bounded" : "reviewed-nondefinitive",
                item.GetProperty("reviewStatus").GetString());
        });
        Assert.Equal("revealedEnemySmc-after-election",
            cases[0].GetProperty("facts").GetProperty("secondReveal").GetString());
        Assert.Equal("revealedEnemyMmc-after-election",
            cases[1].GetProperty("facts").GetProperty("secondReveal").GetString());
        Assert.All(cases.Take(2), item => Assert.Equal("passedNtcAndAtLeastFourMf",
            item.GetProperty("facts").GetProperty("attackerCapability").GetString()));
        Assert.Contains("No forced-back, MF-spend or execution outcome",
            root.GetProperty("scope").GetString());
    }

    private static string[] Strings(JsonElement array) => array.EnumerateArray()
        .Select(item => item.GetString()!).ToArray();
    private static JsonDocument Read(string name) => JsonDocument.Parse(File.ReadAllText(
        Path.Combine(RepositoryPaths.Root, "docs", "ASL", "SourceRegistry", name)));
    private static string Digest(string name) => Convert.ToHexStringLower(SHA256.HashData(
        File.ReadAllBytes(Path.Combine(RepositoryPaths.Root, "docs", "ASL", "SourceRegistry", name))));
}
