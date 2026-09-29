using System.Text.Json;
using LimboDancer.Domains.Asl.Authoring;

namespace LimboDancer.Domains.Asl.Authoring.Tests;

/// <summary>
/// The Ordnance case matrix (unit step 24): the rulings of the plan's section 11 (R24.1 to R24.5), pinned to verified source
/// fragments, to the To Hit Table and IFT transcriptions, and to the reviewed Scenario A1 catalog.
/// </summary>
public sealed class AslScenarioA1OrdnanceMatrixTests
{
    private const string SourceCommit = "a3254ff1d492dbdd28483d86f5b42437b48e80d4";
    private const string MatrixSha256 = "edf205c001f44b26e72ebaabbb27cc4890796dfbfcdbd946434f6677c6141f29";
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    private static string Registry(string name) => Path.Combine(RepositoryPaths.Root, "docs", "ASL", "SourceRegistry", name);

    private static JsonDocument Read(string name) => JsonDocument.Parse(File.ReadAllText(Registry(name)));

    [Fact]
    public void TheMatrixPinsTheToHitTableTheCatalogAndTheRulings()
    {
        Assert.Equal(MatrixSha256, Hashing.Sha256File(Registry("asl-scenario-a1.ordnance-case-matrix.json")));
        using var matrix = Read("asl-scenario-a1.ordnance-case-matrix.json");
        var root = matrix.RootElement;
        Assert.Equal("user-directed-affirmative-xunit-review-2026-09-27", root.GetProperty("authority").GetString());
        Assert.Equal("none", root.GetProperty("executionAuthority").GetString());
        Assert.Equal(AslScenarioA1SourceInventory.PdfDigest, root.GetProperty("sourcePdfSha256").GetString());
        Assert.Equal(Hashing.Sha256File(Registry(Path.Combine("Supplements", "c3-to-hit-table.transcription.json"))),
            root.GetProperty("toHitTranscriptionSha256").GetString());
        Assert.Equal(Hashing.Sha256File(Registry(Path.Combine("Supplements", "a7-infantry-fire-table.transcription.json"))),
            root.GetProperty("iftTranscriptionSha256").GetString());
        Assert.Equal(Hashing.Sha256File(Path.Combine(RepositoryPaths.Root, "src", "ASL", "units", "catalog", "scenario-a1.catalog.json")),
            root.GetProperty("catalogSha256").GetString());

        // The To Hit Table's Infantry row, as read from p. 700: black first, red second.
        using var table = Read(Path.Combine("Supplements", "c3-to-hit-table.transcription.json"));
        Assert.Equal(700, table.RootElement.GetProperty("physicalPdfPage").GetInt32());
        Assert.Equal([8, 7, 6, 5, 4, 3, 2, 1, 0, -1], table.RootElement.GetProperty("infantry").GetProperty("black").EnumerateArray().Select(item => item.GetInt32()));
        Assert.Equal([8, 6, 5, 4, 3, 2, 1, 0, -1, -2], table.RootElement.GetProperty("infantry").GetProperty("red").EnumerateArray().Select(item => item.GetInt32()));

        // Backlog pass 7: the Vehicle row of the To Hit Table (p. 700) and the To Kill tables (p. 701).
        Assert.Equal(Hashing.Sha256File(Registry(Path.Combine("Supplements", "c3-to-hit-vehicle-row.transcription.json"))),
            root.GetProperty("toHitVehicleTranscriptionSha256").GetString());
        Assert.Equal(Hashing.Sha256File(Registry(Path.Combine("Supplements", "c7-to-kill-tables.transcription.json"))),
            root.GetProperty("toKillTranscriptionSha256").GetString());
        using var vehicleRow = Read(Path.Combine("Supplements", "c3-to-hit-vehicle-row.transcription.json"));
        Assert.Equal([10, 9, 8, 7, 6, 6, 5, 4, 3, 2], vehicleRow.RootElement.GetProperty("vehicle").GetProperty("black").EnumerateArray().Select(item => item.GetInt32()));
        Assert.Equal([10, 8, 7, 6, 5, 4, 3, 2, 1, 0], vehicleRow.RootElement.GetProperty("vehicle").GetProperty("red").EnumerateArray().Select(item => item.GetInt32()));
        // Backlog pass 9: the Area row of the To Hit Table (p. 700), red for every nationality.
        Assert.Equal(Hashing.Sha256File(Registry(Path.Combine("Supplements", "c3-to-hit-area-row.transcription.json"))),
            root.GetProperty("toHitAreaTranscriptionSha256").GetString());
        using var areaRow = Read(Path.Combine("Supplements", "c3-to-hit-area-row.transcription.json"));
        Assert.Equal([7, 7, 8, 8, 7, 7, 6, 6, 5, 5], areaRow.RootElement.GetProperty("area").EnumerateArray().Select(item => item.GetInt32()));
        using var toKill = Read(Path.Combine("Supplements", "c7-to-kill-tables.transcription.json"));
        Assert.Equal(701, toKill.RootElement.GetProperty("physicalPdfPage").GetInt32());
        int Basic(string table, string gun) => toKill.RootElement.GetProperty(table).GetProperty("basic").EnumerateArray()
            .Single(item => item.GetProperty("guns").EnumerateArray().Any(entry => entry.GetString() == gun)).GetProperty("tk").GetInt32();
        Assert.Equal(11, Basic("ap", "50"));
        Assert.Equal(13, Basic("ap", "76L:russian-japanese"));
        Assert.Equal(14, Basic("apcr", "50"));
        Assert.Equal(14, Basic("apcr", "76L:russian"));
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

        using var matrix = Read("asl-scenario-a1.ordnance-case-matrix.json");
        var fragments = matrix.RootElement.GetProperty("sourceFragments").EnumerateArray().ToArray();
        Assert.Equal(178, fragments.Length);
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
