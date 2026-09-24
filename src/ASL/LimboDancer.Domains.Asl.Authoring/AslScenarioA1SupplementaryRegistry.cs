using System.Text.Encodings.Web;
using System.Text.Json;

namespace LimboDancer.Domains.Asl.Authoring;

public sealed record AslScenarioA1SupplementaryChartRow(
    string RowLabel,
    int InfantryEntryMf,
    string ExtractedLineSha256);

public sealed record AslScenarioA1SupplementaryRegistry(
    string SchemaVersion,
    string Status,
    string ParentRegistryId,
    string ParentRegistrySha256,
    string ParentSourceCommit,
    string CandidateId,
    string SourcePdfSha256,
    int PhysicalPdfPage,
    string Extractor,
    string ExtractedPageTextSha256,
    string InfantryMfColumnHeaderLineSha256,
    string CotLegendLineSha256,
    string TranscriptionPath,
    string TranscriptionSha256,
    IReadOnlyList<AslScenarioA1SupplementaryChartRow> Rows)
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
    };

    public string Serialize() => JsonSerializer.Serialize(this, JsonOptions) + "\n";
}

public static class AslScenarioA1SupplementaryRegistryBuilder
{
    public const string TranscriptionPath =
        "docs/ASL/SourceRegistry/Supplements/b-terrain-chart-building-entry.md";

    public static AslScenarioA1SupplementaryRegistry Build(
        string repositoryRoot,
        GeneratedManifests manifests,
        JsonDocument candidateEvidence)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(repositoryRoot);
        ArgumentNullException.ThrowIfNull(manifests);
        ArgumentNullException.ThrowIfNull(candidateEvidence);
        _ = AslScenarioA1SourceInventory.Extract(manifests);

        var candidate = candidateEvidence.RootElement;
        var required = AslScenarioA1CaseAssessor.CreateBuildingChartCandidate();
        if (candidate.GetProperty("schemaVersion").GetString() != "1.0.0"
            || candidate.GetProperty("status").GetString()
                != "candidate-unverified-outside-initial-registry"
            || candidate.GetProperty("candidateId").GetString() != required.CandidateId
            || candidate.GetProperty("sourcePdfSha256").GetString() != required.SourcePdfSha256
            || candidate.GetProperty("physicalPdfPage").GetInt32() != required.PhysicalPdfPage
            || candidate.GetProperty("chartTitle").GetString() != "B. Terrain Chart")
        {
            throw new InvalidOperationException("Back-matter candidate identity or status changed.");
        }

        var rows = candidate.GetProperty("rows").EnumerateArray().ToArray();
        if (rows.Length != 2
            || rows[0].GetProperty("rowLabel").GetString() != "23. Wooden Building"
            || rows[1].GetProperty("rowLabel").GetString() != "23. Stone Building"
            || rows.Any(row => row.GetProperty("entryMf").GetInt32() != 2
                || row.GetProperty("sourceStatus").GetString() != "candidate-unverified"))
        {
            throw new InvalidOperationException("The two bounded building chart rows changed.");
        }

        var transcription = Path.Combine(repositoryRoot,
            TranscriptionPath.Replace('/', Path.DirectorySeparatorChar));
        var content = File.ReadAllText(transcription);
        if (!content.Contains("| 23. Wooden Building | 2 |", StringComparison.Ordinal)
            || !content.Contains("| 23. Stone Building | 2 |", StringComparison.Ordinal)
            || !content.Contains("COT: Cost of Terrain.", StringComparison.Ordinal)
            || !content.Contains("Move assumes no road or VBM", StringComparison.Ordinal)
            || !content.Contains("before a source-verification disposition", StringComparison.Ordinal))
        {
            throw new InvalidOperationException("The bounded chart transcription changed or lost its review boundary.");
        }

        return new AslScenarioA1SupplementaryRegistry(
            "1.0.0",
            "registered-unverified-supplement",
            manifests.Registry.RegistryId,
            Hashing.Sha256Text(ManifestJson.SerializeRegistry(manifests.Registry)),
            manifests.Registry.SourceCommit,
            required.CandidateId,
            required.SourcePdfSha256,
            required.PhysicalPdfPage,
            candidate.GetProperty("extractor").GetString()!,
            candidate.GetProperty("extractedPageTextSha256").GetString()!,
            candidate.GetProperty("infantryMfColumnHeaderLineSha256").GetString()!,
            candidate.GetProperty("cotLegendLineSha256").GetString()!,
            TranscriptionPath,
            Hashing.Sha256File(transcription),
            rows.Select(row => new AslScenarioA1SupplementaryChartRow(
                row.GetProperty("rowLabel").GetString()!,
                row.GetProperty("entryMf").GetInt32(),
                row.GetProperty("extractedLineSha256").GetString()!)).ToArray());
    }
}
