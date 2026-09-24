using System.IO.Compression;
using System.Text;
using System.Text.Json;
using LimboDancer.Domains.Asl.Maps.Composition;
using LimboDancer.Domains.Asl.Maps.Coordinates;
using LimboDancer.Domains.Asl.Maps.Geometry;
using LimboDancer.Domains.Asl.Maps.Terrain;
using Xunit.Abstractions;

namespace LimboDancer.Domains.Asl.Maps.Vasl.Tests;

/// <summary>
/// F2 for maps (VASL Board Ingestion Design, section 11): placed, reversed, and scenario-specific-rule boards compared
/// with fixtures the oracle harness writes from VASL's own classes. The fixtures hold derived facts only (ASL-MAP-073).
/// </summary>
public sealed class VaslMapTests(ITestOutputHelper output)
{
    private static readonly string ScenarioDirectory = Path.Combine(AppContext.BaseDirectory, "Oracle", "Scenarios");

    private static IReadOnlyList<VaslScenario> Scenarios() =>
        VaslScenario.ParseFile(File.ReadAllText(Path.Combine(ScenarioDirectory, "scenarios.txt")));

    [Fact]
    public void PlacementsParseFromTheirCompactForm()
    {
        Assert.True(VaslScenario.TryParsePlacement("BFPDW1b@0,1/r[NoStairwells,Level_1ToLevel0,StoneToGuttedFactory1.5]", out var placement));
        Assert.Equal(new BoardPlacement(BoardRef.Parse("bdBFPDW1b"), 0, 1, true, ["NoStairwells", "Level_1ToLevel0", "StoneToGuttedFactory1.5"]).ToString(),
            placement!.ToString());
        Assert.True(VaslScenario.TryParsePlacement("01@2,0", out var plain));
        Assert.Equal((BoardRef.Parse("bd01"), 2, 0, false, 0), (plain!.Board, plain.Column, plain.Row, plain.Reversed, plain.Rules.Count));

        Assert.False(VaslScenario.TryParsePlacement("01", out _));
        Assert.False(VaslScenario.TryParsePlacement("01@0,0/x", out _));
        Assert.False(VaslScenario.TryParsePlacement("01@0,0[Bad Rule]", out _));
        Assert.Null(VaslScenario.ParsePlacements("01@0,0 nonsense"));
        Assert.Throws<FormatException>(() => VaslScenario.ParseFile("broken line"));
    }

    [Fact]
    public void EveryScenarioHasAFixtureProducedFromIt()
    {
        var scenarios = Scenarios();
        Assert.True(scenarios.Count >= 20);
        foreach (var scenario in scenarios)
        {
            using var fixture = Load(scenario.Name);
            var root = fixture.RootElement;
            Assert.Equal(scenario.Name, root.GetProperty("scenario").GetString());
            var boards = root.GetProperty("boards").EnumerateArray().ToArray();
            var placements = scenario.Placements.OrderBy(placement => placement.Row).ThenBy(placement => placement.Column).ToArray();
            Assert.Equal(placements.Length, boards.Length);
            for (var index = 0; index < boards.Length; index++)
            {
                Assert.Equal(placements[index].Board.Value, boards[index].GetProperty("board").GetString());
                Assert.Equal(placements[index].Reversed, boards[index].GetProperty("reversed").GetBoolean());
                Assert.Equal(placements[index].Rules, boards[index].GetProperty("rules").EnumerateArray().Select(rule => rule.GetString()!));
                Assert.Matches("^[0-9a-f]{40}$", boards[index].GetProperty("losDataBlob").GetString()!);
            }

            Assert.Equal(root.GetProperty("hexes").GetArrayLength(),
                Enumerable.Range(0, root.GetProperty("widthInHexes").GetInt32()).Sum(column => root.GetProperty("heightInHexes").GetInt32() + (column % 2)));
        }

        var names = scenarios.Select(scenario => VaslMapImporter.FixtureFileName(scenario.Name)).ToHashSet(StringComparer.Ordinal);
        Assert.All(Directory.GetFiles(ScenarioDirectory, "*.gz"), path => Assert.Contains(Path.GetFileName(path), names));
    }

    [Fact]
    public void LosRulesAreReadFromSharedMetadata()
    {
        var xml = "<?xml version=\"1.0\"?><sharedBoardMetadata><terrainTypes>"
            + "<terrainType name=\"Open Ground\" typeCode=\"0\" isLOSObstacle=\"FALSE\" isLOSHindrance=\"FALSE\" isHalfLevelHeight=\"FALSE\" "
            + "isInherentTerrain=\"FALSE\" split=\"0.0\" isLowerLOSObstacle=\"FALSE\" isLowerLOSHindrance=\"FALSE\" height=\"0\" mapColorRed=\"0\" "
            + "mapColorGreen=\"0\" mapColorBlue=\"0\" LOSCategory=\"OPEN\" /></terrainTypes><LOSSSRules>"
            + "<LOSSSRule name=\"NoRoads\" type=\"ignore\" fromValue=\"\" toValue=\"\" />"
            + "<LOSSSRule name=\"MarshToWater\" type=\"terrainMap\" fromValue=\"Marsh\" toValue=\"Water\" />"
            + "<LOSSSRule name=\"Level1ToLevel0\" type=\"elevationMap\" fromValue=\"1\" toValue=\"0\" />"
            + "<LOSSSRule name=\"MarshToWater\" type=\"terrainToElevationMap\" fromValue=\"Marsh\" toValue=\"-1\" />"
            + "<LOSSSRule name=\"Oops\" type=\"magic\" fromValue=\"\" toValue=\"\" />"
            + "</LOSSSRules></sharedBoardMetadata>";
        var result = SharedBoardMetadataParser.Parse(new MemoryStream(Encoding.UTF8.GetBytes(xml)));
        Assert.NotNull(result.Catalog);
        Assert.Equal(["NoRoads", "MarshToWater", "Level1ToLevel0"], result.Rules.Rules.Select(rule => rule.Name));
        Assert.Equal(new LosSsRule("MarshToWater", LosSsRuleKind.TerrainToElevationMap, "Marsh", "-1"), result.Rules["MarshToWater"]);
        Assert.Equal("VASL-CAT-006", Assert.Single(result.Diagnostics).Code);
    }

    [Theory]
    [InlineData(1800, 645, 1800, 645)]
    [InlineData(1800, 644, 1800, 644)]
    [InlineData(1700, 645, 1802, 645)]
    public void GridSizesComeFromLosDataWhenTheyReachEveryHexCenter(int headerWidth, int headerHeight, int width, int height)
    {
        // bd96-99 declare 56.3125-pixel hexes but 1800-pixel grids; bdLFT1's grid has 644 rows.
        var metadata = new BoardMetadata
        {
            Name = "96",
            Version = "1",
            VersionDate = string.Empty,
            Author = string.Empty,
            BoardImageFileName = "bd96.gif",
            HasHills = false,
            Width = 33,
            Height = 10,
            GeometryAttributes = new Dictionary<string, string> { ["hexWidth"] = "56.3125" },
            BuildingTypes = [],
            Slopes = [],
            RailroadEmbankments = [],
            PartialOrchards = [],
            DeferredElements = new Dictionary<string, string>(),
        };
        var geometry = VaslBoardImporter.GeometryFor(metadata, new LosDataHeader(33, 10, headerWidth, headerHeight));
        Assert.Equal((width, height, 56.3125, 32.25), (geometry.GridWidth, geometry.GridHeight, geometry.HexWidth, geometry.A1CenterY));
    }

    [VaslFact]
    public void PinnedRulesIncludeEveryKind()
    {
        var shared = Assert.IsType<VaslSource>(VaslSource.FromEnvironment()).ReadTerrainCatalog();
        Assert.True(shared.Rules.Count >= 140, $"Only {shared.Rules.Count} rules.");
        Assert.All(Enum.GetValues<LosSsRuleKind>(), kind => Assert.Contains(shared.Rules.Rules, rule => rule.Kind == kind));

        // The file defines MarshToWater twice; as in VASL, the later definition wins.
        Assert.Equal(LosSsRuleKind.TerrainToElevationMap, shared.Rules["MarshToWater"].Kind);
    }

    [VaslFact]
    public void EveryScenarioMatchesTheOracle()
    {
        var vasl = Assert.IsType<VaslSource>(VaslSource.FromEnvironment());
        var shared = vasl.ReadTerrainCatalog();
        var catalog = Assert.IsType<TerrainCatalog>(shared.Catalog);
        var failures = new List<string>();
        foreach (var scenario in Scenarios())
        {
            var import = VaslMapImporter.Build(vasl, catalog, shared.Rules, scenario.Placements);
            Assert.True(import.Succeeded, $"{scenario.Name}: {string.Join("; ", import.Diagnostics.Select(diagnostic => diagnostic.Message))}");
            var result = Assert.IsType<F2Result>(VaslMapImporter.CompareWithFixture(import, scenario, ScenarioDirectory));
            output.WriteLine($"{scenario.Name,-32} {(result.Passed ? "pass" : "FAIL")} {import.Map!.Geometry.WidthInHexes} by {import.Map.Geometry.HeightInHexes} hexes");
            if (!result.Passed)
            {
                failures.Add($"{scenario.Name}: {string.Join("; ", result.Diagnostics.Select(diagnostic => diagnostic.Message).Concat(result.Differences.Take(5)))}");
            }
        }

        Assert.Empty(failures);
    }

    [VaslFact]
    public void AComposedMapLocatesEachBoardsHexes()
    {
        var vasl = Assert.IsType<VaslSource>(VaslSource.FromEnvironment());
        var shared = vasl.ReadTerrainCatalog();
        var placements = VaslScenario.ParsePlacements("01@0,0 02@1,0/r")!;
        var map = Assert.IsType<VaslMap>(VaslMapImporter.Build(vasl, shared.Catalog!, shared.Rules, placements).Map);
        Assert.Equal((65, 10), (map.Geometry.WidthInHexes, map.Geometry.HeightInHexes));

        // bd01's GG column and bd02's reversed GG column share map column 32; bd02, placed later, owns it.
        var shared32 = Assert.IsType<HexIndex>(map.Locate(BoardRef.Parse("bd01"), HexName.Parse("GG5")));
        Assert.Equal(32, shared32.Column);
        Assert.Equal(BoardRef.Parse("bd02"), map.OwnerOf(shared32)!.Value.Board);
        var e4 = Assert.IsType<HexIndex>(map.Locate(BoardRef.Parse("bd02"), HexName.Parse("E4")));
        Assert.Equal((BoardRef.Parse("bd02"), HexName.Parse("E4")), map.OwnerOf(e4));
        Assert.Equal(HexName.Parse("E4"), map.Facts[e4].Hex);
    }

    private static JsonDocument Load(string scenario)
    {
        using var file = File.OpenRead(Path.Combine(ScenarioDirectory, VaslMapImporter.FixtureFileName(scenario)));
        using var gzip = new GZipStream(file, CompressionMode.Decompress);
        return JsonDocument.Parse(gzip);
    }
}
