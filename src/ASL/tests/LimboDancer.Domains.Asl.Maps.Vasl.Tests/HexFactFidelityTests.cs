using System.IO.Compression;
using System.Text.Json;
using LimboDancer.Domains.Asl.Maps.Terrain;

namespace LimboDancer.Domains.Asl.Maps.Vasl.Tests;

/// <summary>
/// F2 (ASL-MAP-041, ASL-MAP-042): derived Hex Facts compared with fixtures written by the VASL oracle harness in
/// <c>src/ASL/tools/vasl-hexfact-oracle</c>. The fixtures hold derived facts only (ASL-MAP-073).
/// </summary>
public sealed class HexFactFidelityTests
{
    private static readonly string OracleDirectory = Path.Combine(AppContext.BaseDirectory, "Oracle");

    [Fact]
    public void FixturesAreWellFormedAndRecordTheirSource()
    {
        var fixtures = Directory.GetFiles(OracleDirectory, "*.hexfacts.json.gz");
        Assert.True(fixtures.Length >= 1, "No oracle fixtures were copied to the test output.");
        foreach (var path in fixtures)
        {
            using var fixture = Load(path);
            var source = HexFactFidelity.ReadSource(fixture);
            Assert.Equal(Path.GetFileName(path).Split('.')[0], source.Board);
            Assert.Matches("^[0-9a-f]{40}$", source.VaslCommit);
            Assert.Matches("^[0-9a-f]{40}$", source.LosDataBlob);
            // VASL's geomorphic layout: every column has the first column's hexes, odd-indexed columns one more.
            var hexes = fixture.RootElement.GetProperty("hexes").EnumerateArray()
                .Select(hex => (Column: hex.GetProperty("col").GetInt32(), Row: hex.GetProperty("row").GetInt32())).ToArray();
            var height = hexes.Count(hex => hex.Column == 0);
            var width = hexes.Max(hex => hex.Column) + 1;
            Assert.Equal(Enumerable.Range(0, width).Sum(column => height + (column % 2)), hexes.Length);
        }
    }

    [Fact]
    public void Board01FixtureWasProducedFromThePinnedEvidence()
    {
        using var fixture = Load(Path.Combine(OracleDirectory, "bd01.hexfacts.json.gz"));
        var source = HexFactFidelity.ReadSource(fixture);
        Assert.Equal("8d77d26222b7bb21d8c1fdda6ba05b447f63c317", source.LosDataBlob);
        Assert.Equal("e91b0d99a7a788812444753cae245841875a8de6", source.MetadataBlob);
        Assert.Equal("33324f9adb3b9b97dd93700685103b6940dc9c3c", source.VaslCommit);
    }

    [VaslFact]
    public void Board01PassesF2()
    {
        var result = CheckF2("bd01.hexfacts.json.gz");
        Assert.True(result.Passed, string.Join(Environment.NewLine, result.Diagnostics.Select(diagnostic => diagnostic.Message).Concat(result.Differences.Take(20))));
    }

    [VaslFact]
    public void EveryFixtureBoardPassesF2()
    {
        var failures = new List<string>();
        foreach (var path in Directory.GetFiles(OracleDirectory, "*.hexfacts.json.gz").Order(StringComparer.Ordinal))
        {
            var result = CheckF2(Path.GetFileName(path));
            if (!result.Passed)
            {
                failures.Add($"{Path.GetFileName(path)}: {string.Join("; ", result.Diagnostics.Select(diagnostic => diagnostic.Code).Concat(result.Differences.Take(5)))}");
            }
        }

        Assert.True(failures.Count == 0, string.Join(Environment.NewLine, failures));
    }

    [VaslFact]
    public void AFixtureFromOtherSourceBytesIsRefusedNotCompared()
    {
        var vasl = Assert.IsType<VaslSource>(VaslSource.FromEnvironment());
        var catalog = Catalog(vasl);
        var board02 = Assert.IsType<IngestedBoard>(VaslBoardImporter.Import(vasl, VaslBoardSource.SourceDirectory(vasl, "02"), catalog).Board);
        using var board01Fixture = Load(Path.Combine(OracleDirectory, "bd01.hexfacts.json.gz"));
        var result = HexFactFidelity.Compare(board02, HexFactFidelity.Derive(board02, catalog), board01Fixture);
        Assert.False(result.Passed);
        Assert.Equal("F2-SOURCE-MISMATCH", Assert.Single(result.Diagnostics).Code);
        Assert.Empty(result.Differences);
    }

    private static F2Result CheckF2(string fixtureFile)
    {
        var vasl = Assert.IsType<VaslSource>(VaslSource.FromEnvironment());
        var catalog = Catalog(vasl);
        using var fixture = Load(Path.Combine(OracleDirectory, fixtureFile));
        var boardName = HexFactFidelity.ReadSource(fixture).Board[2..];
        var board = Assert.IsType<IngestedBoard>(VaslBoardImporter.Import(vasl, VaslBoardSource.SourceDirectory(vasl, boardName), catalog).Board);
        return HexFactFidelity.Compare(board, HexFactFidelity.Derive(board, catalog), fixture);
    }

    private static TerrainCatalog Catalog(VaslSource vasl) => Assert.IsType<TerrainCatalog>(vasl.ReadTerrainCatalog().Catalog);

    private static JsonDocument Load(string path)
    {
        using var file = File.OpenRead(path);
        using var gzip = new GZipStream(file, CompressionMode.Decompress);
        return JsonDocument.Parse(gzip);
    }
}
