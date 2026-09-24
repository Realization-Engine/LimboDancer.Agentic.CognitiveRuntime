using System.Text.Json;

namespace LimboDancer.Domains.Asl.Authoring.Tests;

public sealed class AslScenarioA1ComparisonBatchTests
{
    private const string SourceCommit = "a3254ff1d492dbdd28483d86f5b42437b48e80d4";
    private static readonly JsonSerializerOptions AttestationJsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    [Fact]
    public void ComparisonsBindToSameFullTirAndRemainIndeterminate()
    {
        var manifests = AslAuthoringManifestGenerator.Generate(RepositoryPaths.Root, SourceCommit);
        var attestation = ReadAttestation();
        using var comparison = ReadComparison();
        var batch = AslScenarioA1ComparisonBatch.Build(manifests, attestation, comparison);
        var attested = AslScenarioA1VerificationBatchBuilder.Build(manifests, attestation);

        Assert.Equal(10, batch.Records.Count);
        Assert.Equal(TirCanonicalJson.ComputePayloadSha256(attested.SourceDocument),
            TirCanonicalJson.ComputePayloadSha256(batch.SourceDocument));
        Assert.All(batch.Records, record =>
        {
            Assert.Equal(TirSourceVerificationDisposition.Indeterminate, record.Disposition);
            Assert.Contains("pending", record.ObservedDiscrepancy!, StringComparison.Ordinal);
            Assert.Equal("comparison-process:codex-not-human-verifier", record.Actor.Identity);
            Assert.NotEmpty(TirReviewCanonicalJson.Serialize(record));
        });
        Assert.Equal(2, batch.Records.Count(record => record.Dependencies.Any(dependency =>
            dependency.Kind == TirDependencyKind.Figure)));
        Assert.Equal(2, batch.Records.Count(record =>
            record.SourceEvidence.StartPage == 134
            && record.ComparisonMethod.Contains("physical PDF page 135", StringComparison.Ordinal)));
        using var json = JsonDocument.Parse(batch.Serialize());
        Assert.Equal("source-comparison-indeterminate-semantic-review-pending",
            json.RootElement.GetProperty("status").GetString());
    }

    [Fact]
    public void ChangedPdfOrMissingFigureCannotProduceComparisonRecords()
    {
        var manifests = AslAuthoringManifestGenerator.Generate(RepositoryPaths.Root, SourceCommit);
        var attestation = ReadAttestation();
        using var comparison = ReadComparison();
        var wrongPdf = comparison.RootElement.Clone();
        var missingFigure = comparison.RootElement.Clone();
        using var wrongPdfDocument = JsonDocument.Parse(wrongPdf.GetRawText().Replace(
            AslScenarioA1SourceInventory.PdfDigest, new string('0', 64), StringComparison.Ordinal));
        Assert.Throws<InvalidOperationException>(() => AslScenarioA1ComparisonBatch.Build(
            manifests, attestation, wrongPdfDocument));

        var figureId = missingFigure.GetProperty("records").EnumerateArray()
            .First(record => record.TryGetProperty("imagePath", out _))
            .GetProperty("fragmentId").GetString()!;
        var withoutFigure = missingFigure.GetRawText().Replace(
            $"\"fragmentId\": \"{figureId}\"",
            "\"fragmentId\": \"absent-figure\"",
            StringComparison.Ordinal);
        using var missingFigureDocument = JsonDocument.Parse(withoutFigure);
        Assert.Throws<InvalidOperationException>(() => AslScenarioA1ComparisonBatch.Build(
            manifests, attestation, missingFigureDocument));
    }

    private static AslScenarioA1SourceAttestation ReadAttestation()
    {
        var path = Path.Combine(RepositoryPaths.Root, "docs", "ASL", "SourceRegistry",
            "asl-scenario-a1.source-attestation.json");
        return JsonSerializer.Deserialize<AslScenarioA1SourceAttestation>(
            File.ReadAllText(path), AttestationJsonOptions)
            ?? throw new InvalidOperationException("Attestation is missing.");
    }

    private static JsonDocument ReadComparison()
    {
        var path = Path.Combine(RepositoryPaths.Root, "docs", "ASL", "SourceRegistry",
            "asl-scenario-a1.first-case-pdf-comparison.json");
        return JsonDocument.Parse(File.ReadAllText(path));
    }
}
