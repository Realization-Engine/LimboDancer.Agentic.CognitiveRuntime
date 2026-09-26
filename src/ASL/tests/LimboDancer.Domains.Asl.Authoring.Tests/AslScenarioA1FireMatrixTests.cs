using System.Text.Json;
using LimboDancer.Domains.Asl.Authoring;

namespace LimboDancer.Domains.Asl.Authoring.Tests;

/// <summary>
/// The Fire case matrix (unit step 17, revised at step 18): the user's rulings, pinned to verified source fragments, to the reviewed chart
/// supplement and its transcriptions, and to the reviewed Scenario A1 catalog.
/// </summary>
public sealed class AslScenarioA1FireMatrixTests
{
    private const string SourceCommit = "a3254ff1d492dbdd28483d86f5b42437b48e80d4";
    private const string MatrixSha256 = "81128b0fe8237131970b2970ab026abe086035983bce8b7d6a14aaec605e7c32";
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    private static string Registry(string name) => Path.Combine(RepositoryPaths.Root, "docs", "ASL", "SourceRegistry", name);

    private static JsonDocument Read(string name) => JsonDocument.Parse(File.ReadAllText(Registry(name)));

    [Fact]
    public void TheMatrixPinsTheChartsTheCatalogAndTheRulings()
    {
        Assert.Equal(MatrixSha256, Hashing.Sha256File(Registry("asl-scenario-a1.fire-case-matrix.json")));
        using var matrix = Read("asl-scenario-a1.fire-case-matrix.json");
        var root = matrix.RootElement;
        Assert.Equal("user-directed-affirmative-xunit-review-2026-09-26", root.GetProperty("authority").GetString());
        Assert.Equal("none", root.GetProperty("executionAuthority").GetString());
        Assert.Equal(AslScenarioA1SourceInventory.PdfDigest, root.GetProperty("sourcePdfSha256").GetString());

        var charts = AslScenarioA1FireChartSupplement.Load(RepositoryPaths.Root);
        Assert.Equal(charts.RegistrySha256, root.GetProperty("chartSupplementSha256").GetString());
        Assert.Equal(Hashing.Sha256File(Registry(AslScenarioA1FireChartSupplement.DecisionFile)), root.GetProperty("chartReviewDecisionSha256").GetString());
        Assert.Equal(Hashing.Sha256File(Registry(Path.Combine("Supplements", "a7-infantry-fire-table.transcription.json"))),
            root.GetProperty("iftTranscriptionSha256").GetString());
        Assert.Equal(Hashing.Sha256File(Registry(Path.Combine("Supplements", "b-terrain-chart-tem.transcription.json"))),
            root.GetProperty("terrainChartTranscriptionSha256").GetString());
        Assert.Equal(Hashing.Sha256File(Path.Combine(RepositoryPaths.Root, "src", "ASL", "units", "catalog", "scenario-a1.catalog.json")),
            root.GetProperty("catalogSha256").GetString());

        var rulings = root.GetProperty("rulings");
        Assert.Equal("any-directing-leader-prevents-cowering", rulings.GetProperty("cowering").GetString());
        Assert.Equal("llmc-and-lltc-admitted", rulings.GetProperty("leaderLoss").GetString());
        Assert.Equal("wound-severity-dr-A17.11; wounded-morale-and-leadership-one-worse-A17.3", rulings.GetProperty("leaderWound").GetString());
        Assert.Equal("own-conscript-hs-broken-and-disrupted", rulings.GetProperty("conscriptCasualtyBeyondElr").GetString());
        Assert.Equal("in-catalog-1.1.0", rulings.GetProperty("russianHalfSquad").GetString());
        var tem = root.GetProperty("resolution").GetProperty("drm").GetProperty("tem");
        Assert.Equal((1, 2, 3), (tem.GetProperty("woods").GetInt32(), tem.GetProperty("wooden-building").GetInt32(),
            tem.GetProperty("stone-building").GetInt32()));
        var reading = Assert.Single(root.GetProperty("visualReadings").EnumerateArray().ToArray());
        Assert.Equal(("A7.8", 58), (reading.GetProperty("ruleId").GetString(), reading.GetProperty("physicalPdfPage").GetInt32()));
    }

    [Fact]
    public void EverySourceFragmentIsRegisteredAndVerified()
    {
        var manifests = AslAuthoringManifestGenerator.Generate(RepositoryPaths.Root, SourceCommit);
        using var attestationJson = Read("asl-scenario-a1.source-attestation.json");
        var attestation = JsonSerializer.Deserialize<AslScenarioA1SourceAttestation>(attestationJson.RootElement.GetRawText(), JsonOptions)!;
        var verified = AslScenarioA1OvrNtcSourceReview.Build(RepositoryPaths.Root, manifests, attestation).Records
            .Concat(AslScenarioA1FireSourceReview.Build(RepositoryPaths.Root, manifests, attestation).Records)
            .Concat(AslScenarioA1FireSourceReview.BuildBranches(RepositoryPaths.Root, manifests, attestation).Records)
            .Where(item => item.Disposition == TirSourceVerificationDisposition.Verified)
            .Select(item => item.SourceFragment.FragmentId).ToHashSet(StringComparer.Ordinal);

        using var matrix = Read("asl-scenario-a1.fire-case-matrix.json");
        var fragments = matrix.RootElement.GetProperty("sourceFragments").EnumerateArray().ToArray();
        Assert.Equal(61, fragments.Length);
        foreach (var item in fragments)
        {
            var fragment = Assert.Single(manifests.Fragments, candidate => candidate.FragmentId == item.GetProperty("fragmentId").GetString());
            Assert.Contains(fragment.FragmentId, verified);
            Assert.Equal(fragment.ContentSha256, item.GetProperty("contentSha256").GetString());
            Assert.Equal(fragment.Locator.StartLine, item.GetProperty("startLine").GetInt32());
        }

        var ruleIds = fragments.Select(item => item.GetProperty("ruleId").GetString()).ToHashSet();
        Assert.All(matrix.RootElement.GetProperty("cases").EnumerateArray(),
            item => Assert.All(item.GetProperty("sourceRules").EnumerateArray(), rule => Assert.Contains(rule.GetString(), ruleIds)));
    }
}
