using System.IO.Compression;
using System.Text.Json;

namespace LimboDancer.Domains.Asl.Maps.Vasl.Tests;

/// <summary>
/// The LOS oracle fixtures (LOS Design, section 4), written by the LOS mode of <c>src/ASL/tools/vasl-hexfact-oracle</c>
/// with VASL's own Map.LOS. These checks need no VASL checkout: each fixture records the same board sources as the
/// board's hex-fact fixture, and every location it names is in that board's hex facts. The fixtures hold VASL's
/// results only, never VASL code or data (ASL-MAP-073).
/// </summary>
public sealed class LosFixtureTests
{
    private static readonly string OracleDirectory = Path.Combine(AppContext.BaseDirectory, "Oracle");

    public static TheoryData<string, int> Fixtures => new()
    {
        { "bd01.los.json.gz", 18251 },
        { "bd11.los.json.gz", 5662 },
        { Path.Combine("Scenarios", "bd11-over-bd01.scenario.los.json.gz"), 26718 },
        { Path.Combine("Scenarios", "bd11r-over-bd01.scenario.los.json.gz"), 26682 },
        { "bd05.los.json.gz", 5811 },
        { "bd09.los.json.gz", 6124 },
        { "bd12.los.json.gz", 8059 },
        { "bd15.los.json.gz", 6051 },
        { Path.Combine("Scenarios", "bd12-over-bd15.scenario.los.json.gz"), 17588 },
        { "bd23.los.json.gz", 12866 },
        { "bd51.los.json.gz", 29151 },
        { "bd96.los.json.gz", 6079 },
        { "bdBFPB.los.json.gz", 11882 },
        { "bdBFPD.los.json.gz", 5566 },
        { "bdBFPDW2b.los.json.gz", 5584 },
        { "bdrdx.los.json.gz", 2046 },
    };

    [Theory]
    [MemberData(nameof(Fixtures))]
    public void AFixtureRecordsItsBoardsSourcesAndOnlyLocationsOnThem(string file, int pairs)
    {
        using var fixture = Load(Path.Combine(OracleDirectory, file));
        var root = fixture.RootElement;
        Assert.Equal(("1.1.0", 12, "HalfHexWidthLeftHexFullHeight"),
            (root.GetProperty("harnessVersion").GetString(), root.GetProperty("losRange").GetInt32(), root.GetProperty("gridConfiguration").GetString()));
        Assert.Matches("^[0-9a-f]{40}$", root.GetProperty("vaslCommit").GetString()!);

        // Each board's sources are the ones its hex-fact fixture was derived from, and its locations are its hex facts'.
        var locations = new HashSet<string>(StringComparer.Ordinal);
        var hexes = new HashSet<string>(StringComparer.Ordinal);
        foreach (var board in root.GetProperty("boards").EnumerateArray())
        {
            var name = board.GetProperty("board").GetString()!;
            using var facts = Load(Path.Combine(OracleDirectory, name + ".hexfacts.json.gz"));
            Assert.Equal(facts.RootElement.GetProperty("losDataBlob").GetString(), board.GetProperty("losDataBlob").GetString());
            Assert.Equal(facts.RootElement.GetProperty("metadataBlob").GetString(), board.GetProperty("metadataBlob").GetString());
            foreach (var hex in facts.RootElement.GetProperty("hexes").EnumerateArray())
            {
                var id = $"{name}:{hex.GetProperty("hex").GetString()}";
                hexes.Add(id);
                foreach (var location in hex.GetProperty("locations").EnumerateArray())
                {
                    locations.Add($"{id}:{location.GetProperty("level").GetInt32()}");
                }
            }
        }

        var pairList = root.GetProperty("pairs").EnumerateArray().ToArray();
        Assert.Equal(pairs, pairList.Length);
        foreach (var pair in pairList)
        {
            Assert.Contains(pair.GetProperty("source").GetString()!, locations);
            Assert.Contains(pair.GetProperty("target").GetString()!, locations);
            var blocked = pair.GetProperty("blocked").GetBoolean();
            Assert.Equal(blocked, pair.GetProperty("blockedAt").ValueKind == JsonValueKind.Array);
            if (pair.GetProperty("blockedHex").GetString() is { } at)
            {
                Assert.Contains(at, hexes);
            }

            Assert.InRange(pair.GetProperty("range").GetInt32(), 0, 12);
        }
    }

    [Fact]
    public void TheSeamScenariosHavePairsAcrossTheSeam()
    {
        foreach (var file in new[] { "bd11-over-bd01", "bd11r-over-bd01" })
        {
            using var fixture = Load(Path.Combine(OracleDirectory, "Scenarios", file + ".scenario.los.json.gz"));
            var across = fixture.RootElement.GetProperty("pairs").EnumerateArray()
                .Count(pair => pair.GetProperty("source").GetString()![..4] != pair.GetProperty("target").GetString()![..4]);
            Assert.True(across > 1000, $"{file} has only {across} pairs across the seam.");
        }
    }

    private static JsonDocument Load(string path)
    {
        using var file = File.OpenRead(path);
        using var gzip = new GZipStream(file, CompressionMode.Decompress);
        return JsonDocument.Parse(gzip);
    }
}
