using System.Text.Json;

namespace LimboDancer.Domains.Asl.Authoring.Tests;

public sealed class AslScenarioA1SupplementaryRegistryTests
{
    private const string SourceCommit = "a3254ff1d492dbdd28483d86f5b42437b48e80d4";

    [Fact]
    public void BoundedChartSupplementPinsDistinctSourceAndRemainsUnverified()
    {
        var manifests = AslAuthoringManifestGenerator.Generate(RepositoryPaths.Root, SourceCommit);
        using var candidate = ReadCandidate();
        var supplement = AslScenarioA1SupplementaryRegistryBuilder.Build(
            RepositoryPaths.Root, manifests, candidate);

        Assert.Equal("registered-unverified-supplement", supplement.Status);
        Assert.Equal(manifests.Registry.RegistryId, supplement.ParentRegistryId);
        Assert.Equal(AslScenarioA1SourceInventory.PdfDigest, supplement.SourcePdfSha256);
        Assert.Equal(698, supplement.PhysicalPdfPage);
        Assert.Collection(supplement.Rows,
            wooden => Assert.Equal("23. Wooden Building", wooden.RowLabel),
            stone => Assert.Equal("23. Stone Building", stone.RowLabel));
        Assert.All(supplement.Rows, row => Assert.Equal(2, row.InfantryEntryMf));
        Assert.DoesNotContain(manifests.Registry.Artifacts, artifact =>
            artifact.Path == supplement.TranscriptionPath);

        var path = Path.Combine(RepositoryPaths.Root, "docs", "ASL", "SourceRegistry",
            "asl-scenario-a1.supplementary-source-registry.json");
        using var committed = JsonDocument.Parse(File.ReadAllText(path));
        using var generated = JsonDocument.Parse(supplement.Serialize());
        Assert.True(JsonElement.DeepEquals(committed.RootElement, generated.RootElement));
    }

    [Fact]
    public void AlteredCandidateOrTranscriptCannotReuseSupplementIdentity()
    {
        var manifests = AslAuthoringManifestGenerator.Generate(RepositoryPaths.Root, SourceCommit);
        using var candidate = ReadCandidate();
        using var wrongPage = JsonDocument.Parse(candidate.RootElement.GetRawText().Replace(
            "\"physicalPdfPage\": 698", "\"physicalPdfPage\": 699", StringComparison.Ordinal));
        Assert.Throws<InvalidOperationException>(() => AslScenarioA1SupplementaryRegistryBuilder.Build(
            RepositoryPaths.Root, manifests, wrongPage));

        var tempRoot = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        var transcriptPath = Path.Combine(tempRoot, AslScenarioA1SupplementaryRegistryBuilder
            .TranscriptionPath.Replace('/', Path.DirectorySeparatorChar));
        Directory.CreateDirectory(Path.GetDirectoryName(transcriptPath)!);
        try
        {
            File.WriteAllText(transcriptPath, "| 23. Wooden Building | 3 |\n");
            Assert.Throws<InvalidOperationException>(() => AslScenarioA1SupplementaryRegistryBuilder.Build(
                tempRoot, manifests, candidate));
        }
        finally
        {
            Directory.Delete(tempRoot, recursive: true);
        }
    }

    private static JsonDocument ReadCandidate()
    {
        var path = Path.Combine(RepositoryPaths.Root, "docs", "ASL", "SourceRegistry",
            "asl-scenario-a1.backmatter-chart-candidate.json");
        return JsonDocument.Parse(File.ReadAllText(path));
    }
}
