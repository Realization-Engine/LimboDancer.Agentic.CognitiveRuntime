using System.Text.Json;
using LimboDancer.Domains.Asl.Authoring;

namespace LimboDancer.Domains.Asl.Authoring.Tests;

/// <summary>
/// The Close Combat case matrix (unit step 29): the rulings of the plan's section 11 (R29.1 to R29.15), pinned to verified source
/// fragments, to the CCT transcription, and to the reviewed Scenario A1 catalog.
/// </summary>
public sealed class AslScenarioA1CloseCombatMatrixTests
{
    private const string SourceCommit = "a3254ff1d492dbdd28483d86f5b42437b48e80d4";
    private const string MatrixSha256 = "065300051e452cc9b9d8cf834cb47ff7d63496be6db12ea453603c0ba3fdfbf5";
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    private static string Registry(string name) => Path.Combine(RepositoryPaths.Root, "docs", "ASL", "SourceRegistry", name);

    private static JsonDocument Read(string name) => JsonDocument.Parse(File.ReadAllText(Registry(name)));

    [Fact]
    public void TheMatrixPinsTheCctTheCatalogAndTheRulings()
    {
        Assert.Equal(MatrixSha256, Hashing.Sha256File(Registry("asl-scenario-a1.close-combat-case-matrix.json")));
        using var matrix = Read("asl-scenario-a1.close-combat-case-matrix.json");
        var root = matrix.RootElement;
        Assert.Equal("user-directed-affirmative-xunit-review-2026-09-27", root.GetProperty("authority").GetString());
        Assert.Equal("none", root.GetProperty("executionAuthority").GetString());
        Assert.Equal(AslScenarioA1SourceInventory.PdfDigest, root.GetProperty("sourcePdfSha256").GetString());
        Assert.Equal(Hashing.Sha256File(Registry(Path.Combine("Supplements", "a11-close-combat-table.transcription.json"))),
            root.GetProperty("cctTranscriptionSha256").GetString());
        Assert.Equal(Hashing.Sha256File(Path.Combine(RepositoryPaths.Root, "src", "ASL", "units", "catalog", "scenario-a1.catalog.json")),
            root.GetProperty("catalogSha256").GetString());

        // The CCT transcription: fourteen columns, black Kill Numbers 0 to 13, as read from p. 692.
        using var cct = Read(Path.Combine("Supplements", "a11-close-combat-table.transcription.json"));
        var columns = cct.RootElement.GetProperty("columns").EnumerateArray().ToArray();
        Assert.Equal(692, cct.RootElement.GetProperty("physicalPdfPage").GetInt32());
        Assert.Equal(Enumerable.Range(0, 14), columns.Select(item => item.GetProperty("blackKill").GetInt32()));
        Assert.Equal(Enumerable.Range(2, 14), columns.Select(item => item.GetProperty("redKill").GetInt32()));
    }

    [Fact]
    public void EverySourceFragmentIsRegisteredAndVerified()
    {
        var manifests = AslAuthoringManifestGenerator.Generate(RepositoryPaths.Root, SourceCommit);
        using var attestationJson = Read("asl-scenario-a1.source-attestation.json");
        var attestation = JsonSerializer.Deserialize<AslScenarioA1SourceAttestation>(attestationJson.RootElement.GetRawText(), JsonOptions)!;
        var verified = AslScenarioA1FireSourceReview.Build(RepositoryPaths.Root, manifests, attestation).Records
            .Concat(AslScenarioA1FireSourceReview.BuildBranches(RepositoryPaths.Root, manifests, attestation).Records)
            .Concat(AslScenarioA1FireSourceReview.BuildHeatOfBattle(RepositoryPaths.Root, manifests, attestation).Records)
            .Concat(AslScenarioA1FireSourceReview.BuildCloseCombat(RepositoryPaths.Root, manifests, attestation).Records)
            .Concat(AslScenarioA1FireSourceReview.BuildBerserkSurrender(RepositoryPaths.Root, manifests, attestation).Records)
            .Concat(AslScenarioA1FireSourceReview.BuildOrdnance(RepositoryPaths.Root, manifests, attestation).Records)
            .Concat(AslScenarioA1FireSourceReview.BuildVehicles(RepositoryPaths.Root, manifests, attestation).Records)
            .Concat(AslScenarioA1FireSourceReview.BuildPass5(RepositoryPaths.Root, manifests, attestation).Records)
            .Where(item => item.Disposition == TirSourceVerificationDisposition.Verified)
            .Select(item => item.SourceFragment.FragmentId).ToHashSet(StringComparer.Ordinal);

        using var matrix = Read("asl-scenario-a1.close-combat-case-matrix.json");
        var fragments = matrix.RootElement.GetProperty("sourceFragments").EnumerateArray().ToArray();
        Assert.Equal(37, fragments.Length);
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
