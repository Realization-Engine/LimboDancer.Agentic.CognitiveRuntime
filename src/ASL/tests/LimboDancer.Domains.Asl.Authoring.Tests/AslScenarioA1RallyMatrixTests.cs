using System.Text.Json;
using LimboDancer.Domains.Asl.Authoring;

namespace LimboDancer.Domains.Asl.Authoring.Tests;

/// <summary>
/// The Rally case matrix (unit step 19): the user's rulings of 2026-09-26, pinned to verified source fragments and to the
/// reviewed Scenario A1 catalog.
/// </summary>
public sealed class AslScenarioA1RallyMatrixTests
{
    private const string SourceCommit = "a3254ff1d492dbdd28483d86f5b42437b48e80d4";
    private const string MatrixSha256 = "74618ac1a29222303cc3316b7ca4fefbf8cd5b829494ed67ee996745189706cb";
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    private static string Registry(string name) => Path.Combine(RepositoryPaths.Root, "docs", "ASL", "SourceRegistry", name);

    private static JsonDocument Read(string name) => JsonDocument.Parse(File.ReadAllText(Registry(name)));

    [Fact]
    public void TheMatrixPinsTheCatalogAndTheRulings()
    {
        Assert.Equal(MatrixSha256, Hashing.Sha256File(Registry("asl-scenario-a1.rally-case-matrix.json")));
        using var matrix = Read("asl-scenario-a1.rally-case-matrix.json");
        var root = matrix.RootElement;
        Assert.Equal("user-directed-affirmative-xunit-review-2026-09-26", root.GetProperty("authority").GetString());
        Assert.Equal("none", root.GetProperty("executionAuthority").GetString());
        Assert.Equal(AslScenarioA1SourceInventory.PdfDigest, root.GetProperty("sourcePdfSha256").GetString());
        Assert.Equal(Hashing.Sha256File(Path.Combine(RepositoryPaths.Root, "src", "ASL", "units", "catalog", "scenario-a1.catalog.json")),
            root.GetProperty("catalogSha256").GetString());
        var rulings = root.GetProperty("rulings");
        Assert.StartsWith("original-2-on-a-leader-rally", rulings.GetProperty("heatOfBattle").GetString(), StringComparison.Ordinal);
        Assert.StartsWith("first-mmc-self-rally", rulings.GetProperty("fieldPromotion").GetString(), StringComparison.Ordinal);
    }

    [Fact]
    public void EverySourceFragmentIsRegisteredAndVerified()
    {
        var manifests = AslAuthoringManifestGenerator.Generate(RepositoryPaths.Root, SourceCommit);
        using var attestationJson = Read("asl-scenario-a1.source-attestation.json");
        var attestation = JsonSerializer.Deserialize<AslScenarioA1SourceAttestation>(attestationJson.RootElement.GetRawText(), JsonOptions)!;
        var verified = AslScenarioA1FireSourceReview.Build(RepositoryPaths.Root, manifests, attestation).Records
            .Concat(AslScenarioA1FireSourceReview.BuildBranches(RepositoryPaths.Root, manifests, attestation).Records)
            .Concat(AslScenarioA1FireSourceReview.BuildRally(RepositoryPaths.Root, manifests, attestation).Records)
            .Concat(AslScenarioA1FireSourceReview.BuildHeatOfBattle(RepositoryPaths.Root, manifests, attestation).Records)
            .Concat(AslScenarioA1FireSourceReview.BuildBerserkSurrender(RepositoryPaths.Root, manifests, attestation).Records)
            .Concat(AslScenarioA1FireSourceReview.BuildOrdnance(RepositoryPaths.Root, manifests, attestation).Records)
            .Concat(AslScenarioA1FireSourceReview.BuildVehicles(RepositoryPaths.Root, manifests, attestation).Records)
            .Concat(AslScenarioA1FireSourceReview.BuildPass5(RepositoryPaths.Root, manifests, attestation).Records)
            .Concat(AslScenarioA1FireSourceReview.BuildPass6(RepositoryPaths.Root, manifests, attestation).Records)
            .Concat(AslScenarioA1FireSourceReview.BuildPass7(RepositoryPaths.Root, manifests, attestation).Records)
            .Concat(AslScenarioA1FireSourceReview.BuildPass8(RepositoryPaths.Root, manifests, attestation).Records)
            .Concat(AslScenarioA1FireSourceReview.BuildPass9(RepositoryPaths.Root, manifests, attestation).Records)
            .Concat(AslScenarioA1FireSourceReview.BuildPass9b(RepositoryPaths.Root, manifests, attestation).Records)
            .Concat(AslScenarioA1FireSourceReview.BuildPass10(RepositoryPaths.Root, manifests, attestation).Records)
            .Concat(AslScenarioA1FireSourceReview.BuildPass11(RepositoryPaths.Root, manifests, attestation).Records)
            .Concat(AslScenarioA1FireSourceReview.BuildPass12(RepositoryPaths.Root, manifests, attestation).Records)
            .Concat(AslScenarioA1FireSourceReview.BuildPass13(RepositoryPaths.Root, manifests, attestation).Records)
            .Concat(AslScenarioA1FireSourceReview.BuildPass15(RepositoryPaths.Root, manifests, attestation).Records)
            .Concat(AslScenarioA1FireSourceReview.BuildPass16(RepositoryPaths.Root, manifests, attestation).Records)
            .Where(item => item.Disposition == TirSourceVerificationDisposition.Verified)
            .Select(item => item.SourceFragment.FragmentId).ToHashSet(StringComparer.Ordinal);

        using var matrix = Read("asl-scenario-a1.rally-case-matrix.json");
        var fragments = matrix.RootElement.GetProperty("sourceFragments").EnumerateArray().ToArray();
        Assert.Equal(55, fragments.Length);
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
