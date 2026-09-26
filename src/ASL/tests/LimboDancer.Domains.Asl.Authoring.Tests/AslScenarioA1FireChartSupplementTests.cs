using System.Text.Json;
using System.Text.Json.Nodes;
using LimboDancer.Domains.Asl.Authoring;

namespace LimboDancer.Domains.Asl.Authoring.Tests;

/// <summary>
/// The delegated transcription review of the Fire chart supplement (unit step 17): the IFT (page 692) and the Terrain
/// Chart TEM rows (page 698) reproduce the pinned <c>pdftotext</c> extraction item by item, and a changed cell is
/// rejected even when its transcription digest is re-pinned.
/// </summary>
public sealed class AslScenarioA1FireChartSupplementTests
{
    private static readonly JsonSerializerOptions WebJsonOptions = new(JsonSerializerDefaults.Web);

    private static string RegistryPath => Path.Combine(RepositoryPaths.Root, "docs", "ASL", "SourceRegistry",
        AslScenarioA1FireChartSupplement.RegistryFile);

    private static string IftPath => Path.Combine(RepositoryPaths.Root, "docs", "ASL", "SourceRegistry", "Supplements",
        "a7-infantry-fire-table.transcription.json");

    private static string TemPath => Path.Combine(RepositoryPaths.Root, "docs", "ASL", "SourceRegistry", "Supplements",
        "b-terrain-chart-tem.transcription.json");

    [Fact]
    public void TheTranscriptionsReproduceThePinnedExtraction()
    {
        var charts = AslScenarioA1FireChartSupplement.Load(RepositoryPaths.Root);
        Assert.Equal(AslScenarioA1FireChartSupplement.RegistrySha256, charts.RegistrySha256);
        Assert.Equal([1, 2, 4, 6, 8, 12, 16, 20, 24, 30, 36], charts.InfantryFireTable.Columns.Select(column => column.Fp));
        Assert.Equal(16, charts.InfantryFireTable.Rows.Count);
        Assert.Equal(7, charts.TerrainChart.Rows.Count);
        Assert.Equal(5, charts.TerrainChart.Legend.Count);
    }

    [Theory]
    [InlineData(0, 1, "1KIA")]
    [InlineData(1, 1, "K/1")]
    [InlineData(4, 8, "2MC")]
    [InlineData(6, 8, "1MC")]
    [InlineData(8, 8, "NMC")]
    [InlineData(9, 8, "PTC")]
    [InlineData(10, 8, "none")]
    [InlineData(3, 12, "K/3")]
    [InlineData(15, 36, "PTC")]
    public void SampleCellsReadAsPrinted(int dr, int fp, string expected)
    {
        var ift = AslScenarioA1FireChartSupplement.Load(RepositoryPaths.Root).InfantryFireTable;
        var column = ift.Columns.Select((item, index) => (item, index)).Single(pair => pair.item.Fp == fp).index;
        Assert.Equal(expected, ift.Rows.Single(row => row.Dr == dr).Results[column]);
    }

    [Fact]
    public void TheTerrainRowsReadAsPrinted()
    {
        var rows = AslScenarioA1FireChartSupplement.Load(RepositoryPaths.Root).TerrainChart.Rows
            .ToDictionary(row => row.Terrain, StringComparer.Ordinal);
        Assert.Equal("+2(+1*)", rows["23. Wooden Building"].TemIndirect);
        Assert.Equal("+3(+1*)", rows["23. Stone Building"].TemIndirect);
        Assert.Equal("+1/-1", rows["13. Woods"].TemIndirect);
        Assert.Equal("Hindrance", rows["12. Brush"].LosObstacleHindrance);
        Assert.Equal("Hindrance*", rows["15. Grain"].LosObstacleHindrance);
        Assert.Equal("FFMO: -1*", rows["1. Open Ground"].TemIndirect);
    }

    [Fact]
    public void AChangedCellIsRejectedEvenWithARepinnedDigest()
    {
        var registry = File.ReadAllText(RegistryPath);
        var ift = File.ReadAllText(IftPath);
        var tem = File.ReadAllText(TemPath);

        // Row 8, FP 8 is NMC; make it 1MC and re-pin the transcription digest.
        var node = JsonNode.Parse(ift)!;
        node["rows"]![8]!["results"]![4] = "1MC";
        var changed = node.ToJsonString();
        Assert.NotEqual(ift, changed);
        var repinned = registry.Replace(Hashing.Sha256Text(ift), Hashing.Sha256Text(changed), StringComparison.Ordinal);
        var error = Assert.Throws<InvalidOperationException>(() =>
            AslScenarioA1FireChartSupplement.Evaluate(repinned, changed, tem));
        Assert.Contains("'8'", error.Message);

        var building = tem.Replace("\"+3(+1*)\"", "\"+2(+1*)\"", StringComparison.Ordinal);
        var repinnedTem = registry.Replace(Hashing.Sha256Text(tem), Hashing.Sha256Text(building), StringComparison.Ordinal);
        error = Assert.Throws<InvalidOperationException>(() =>
            AslScenarioA1FireChartSupplement.Evaluate(repinnedTem, ift, building));
        Assert.Contains("23. Stone Building", error.Message);
    }

    [Fact]
    public void AChangedDigestOrIdentityIsRejected()
    {
        var registry = File.ReadAllText(RegistryPath);
        var ift = File.ReadAllText(IftPath);
        var tem = File.ReadAllText(TemPath);
        Assert.Throws<InvalidOperationException>(() =>
            AslScenarioA1FireChartSupplement.Evaluate(registry, ift.Replace("K/1", "K/2", StringComparison.Ordinal), tem));
        Assert.Throws<InvalidOperationException>(() => AslScenarioA1FireChartSupplement.Evaluate(
            registry.Replace("\"physicalPdfPage\": 692", "\"physicalPdfPage\": 693", StringComparison.Ordinal), ift, tem));
        Assert.Throws<InvalidOperationException>(() => AslScenarioA1FireChartSupplement.Evaluate(
            registry.Replace("pdftotext 4.00", "pdftotext 24.02.0", StringComparison.Ordinal), ift, tem));
    }

    [Fact]
    public void TheDelegatedReviewIsRecordedAsCommitted()
    {
        var decision = AslScenarioA1FireChartSupplement.Review(RepositoryPaths.Root);
        Assert.Equal(AslScenarioA1FireChartSupplement.AcceptedStatus, decision.Status);
        Assert.Equal(["K/3", "none", "5KIA", "NMC", "4MC"], decision.SpotCheck.Take(5).Select(cell => cell.Transcribed));
        var generated = JsonSerializer.SerializeToElement(decision, WebJsonOptions);
        using var committed = JsonDocument.Parse(File.ReadAllText(Path.Combine(RepositoryPaths.Root, "docs", "ASL",
            "SourceRegistry", AslScenarioA1FireChartSupplement.DecisionFile)));
        Assert.True(JsonElement.DeepEquals(committed.RootElement, generated));
    }

    [Fact]
    public void NormalizationMatchesTheRecordedRule()
    {
        Assert.Equal("2 1MC K/1 1KIA", AslScenarioA1FireChartSupplement.Normalize("2   1MC • K/1  •‡•1KIA"));
        Assert.Equal("≥15 PTC", AslScenarioA1FireChartSupplement.Normalize("≥ 15   PTC‡"));
        Assert.Equal("1-31/2 Levels", AslScenarioA1FireChartSupplement.Normalize("1-3½ Levels"));
    }
}
