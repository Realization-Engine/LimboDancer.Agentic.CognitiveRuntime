using System.IO.Compression;
using System.Text.Json;
using System.Xml.Linq;
using LimboDancer.Domains.Asl.Maps.Coordinates;
using LimboDancer.Domains.Asl.Maps.Derivation;
using LimboDancer.Domains.Asl.Maps.Geometry;
using LimboDancer.Domains.Asl.Maps.Grid;
using LimboDancer.Domains.Asl.Maps.Los;
using LimboDancer.Domains.Asl.Maps.Terrain;
using Xunit.Abstractions;

namespace LimboDancer.Domains.Asl.Maps.Vasl.Tests;

public sealed class LosLeftoverTests(ITestOutputHelper output)
{
    private static readonly string DirectoryPath = Path.Combine(AppContext.BaseDirectory, "Oracle", "Leftovers");
    private static readonly string Specification = Path.Combine(DirectoryPath, "maps.xml");
    private static readonly BoardGeometry Geometry = BoardGeometry.StandardGeomorphic;

    public static TheoryData<string, int> Maps => new()
    {
        { "entrenchment-hillock", 1026 },
        { "entrenchment-railroad", 1026 },
        { "entrenchment-wall-0", 1026 },
        { "entrenchment-wall-1", 1026 },
        { "entrenchment-wall-2", 1026 },
        { "gutted-blind-hex", 2346 },
        { "gutted-building-0", 2346 },
        { "gutted-building-1", 2346 },
        { "gutted-building-2", 2346 },
        { "gutted-factory", 1026 },
        { "gutted-tall", 1026 },
        { "gutted-wall", 1856 },
        { "partial-orchard-0", 1026 },
        { "partial-orchard-1", 1026 },
        { "partial-orchard-2", 1026 },
        { "partial-orchard-water", 1026 },
        { "railroad-bridge-control", 624 },
        { "railroad-bridge", 624 },
        { "railroad-cellar", 2886 },
        { "railroad-hexside-0", 1026 },
        { "railroad-hexside-1", 1026 },
        { "railroad-hexside-2", 1026 },
        { "railroad-terrain-0", 1026 },
        { "railroad-terrain-1", 1026 },
        { "railroad-terrain-2", 1026 },
        { "roofless-factory-0", 1026 },
        { "roofless-factory-1", 1026 },
        { "roofless-factory-2", 1026 },
        { "roofless-hexside", 1026 },
        { "roofless-rooftop", 1334 },
        { "roofless-rubble", 2664 },
        { "roofless-tall", 1026 },
        { "tunnel", 1100 },
    };

    [Fact]
    public void EveryControlledMapHasAPinnedFixture()
    {
        var names = Maps.Select(row => (string)row[0]).Order(StringComparer.Ordinal).ToArray();
        Assert.Equal(names, XDocument.Load(Specification).Root!.Elements("map").Select(map => (string)map.Attribute("name")!).Order(StringComparer.Ordinal));
        Assert.Equal(names, Directory.GetFiles(DirectoryPath, "*.los.json.gz").Select(path => Path.GetFileName(path).Split('.')[0]).Order(StringComparer.Ordinal));
    }

    [Theory]
    [MemberData(nameof(Maps))]
    public void ControlledFixturePinsItsInputsAndPairCount(string name, int pairs)
    {
        using var stream = new GZipStream(File.OpenRead(Path.Combine(DirectoryPath, name + ".los.json.gz")), CompressionMode.Decompress);
        using var document = JsonDocument.Parse(stream);
        var root = document.RootElement;
        Assert.Equal("leftovers-1", root.GetProperty("harnessVersion").GetString());
        Assert.Equal(GitBlob.Sha(System.Text.Encoding.UTF8.GetBytes(File.ReadAllText(Specification).ReplaceLineEndings("\n"))), root.GetProperty("specBlob").GetString());
        Assert.Equal("33324f9adb3b9b97dd93700685103b6940dc9c3c", root.GetProperty("vaslCommit").GetString());
        Assert.Equal(pairs, root.GetProperty("pairs").GetArrayLength());
        Assert.All(root.GetProperty("pairs").EnumerateArray(), pair => Assert.False(pair.TryGetProperty("vaslError", out _)));
    }

    [VaslTheory]
    [InlineData("Dinant")]
    [InlineData("RBv2")]
    [InlineData("RBv3")]
    [InlineData("RO")]
    [InlineData("VotG")]
    public void SurveyCandidatesRequireHistoricalBoardSupport(string board)
    {
        var vasl = Assert.IsType<VaslSource>(VaslSource.FromEnvironment());
        var scope = VaslBoardImporter.CheckScope(VaslBoardSource.SourceDirectory(vasl, board));
        Assert.Equal(BoardScope.OutOfScope, scope.Scope);
        Assert.False(string.IsNullOrWhiteSpace(scope.Reason));
        output.WriteLine(scope.Reason);
    }

    [Theory]
    [InlineData("railroad-bridge", 0)]
    [InlineData("railroad-bridge-control", 1)]
    public void TheRailroadBridgeFixtureExercisesTheBridgeException(string name, int hindrance)
    {
        var path = Path.Combine(DirectoryPath, name + ".los.json.gz");
        using var stream = new GZipStream(File.OpenRead(path), CompressionMode.Decompress);
        using var document = JsonDocument.Parse(stream);
        var hex = document.RootElement.GetProperty("hexes").EnumerateArray().Single(hex => hex.GetProperty("hex").GetString() == "E4");
        Assert.Equal("Stone Bridge", hex.GetProperty("bridge").GetProperty("terrain").GetString());
        Assert.Equal(0, hex.GetProperty("bridge").GetProperty("roadLevel").GetInt32());
        var pair = LosFidelity.Read(path).Pairs.Single(pair => pair.Source.ToString() == "bd01:C3:0" && pair.Target.ToString() == "bd01:G5:0");
        Assert.False(pair.Blocked);
        Assert.Equal(hindrance, pair.Hindrance);
    }

    [VaslTheory]
    [MemberData(nameof(Maps))]
    public void ControlledMapAgreesWithVasl(string name, int pairs)
    {
        var vasl = Assert.IsType<VaslSource>(VaslSource.FromEnvironment());
        var catalog = Assert.IsType<TerrainCatalog>(vasl.ReadTerrainCatalog().Catalog);
        var path = Path.Combine(DirectoryPath, name + ".los.json.gz");
        using var stream = new GZipStream(File.OpenRead(path), CompressionMode.Decompress);
        using var document = JsonDocument.Parse(stream);
        var root = document.RootElement;
        Assert.Equal("leftovers-1", root.GetProperty("harnessVersion").GetString());
        Assert.Equal(pairs, root.GetProperty("pairs").GetArrayLength());
        Assert.Equal(vasl.SharedBoardMetadataProvenance().IndexBlob, root.GetProperty("sharedBoardMetadataBlob").GetString());
        Assert.Equal(vasl.Git?.HeadCommit, root.GetProperty("vaslCommit").GetString());
        var map = Build(name, catalog);

        // Check the independently built map's endpoints and annotations before comparing LOS.
        foreach (var expected in root.GetProperty("hexes").EnumerateArray())
        {
            var facts = map.Facts[HexName.Parse(expected.GetProperty("hex").GetString()!)];
            Assert.Equal(expected.GetProperty("baseLevel").GetInt32(), facts.BaseLevel);
            Assert.Equal(expected.GetProperty("center").GetProperty("terrain").GetString(), facts.Center.Terrain?.Name);
            Assert.Equal(expected.GetProperty("locations").EnumerateArray().Select(loc => (loc.GetProperty("level").GetInt32(), loc.GetProperty("terrain").GetString())),
                facts.Locations.Select(loc => (loc.Level, loc.Terrain?.Name)));
            foreach (var expectedSide in expected.GetProperty("hexsides").EnumerateArray())
            {
                var side = facts.Hexsides[expectedSide.GetProperty("side").GetInt32()];
                Assert.Equal(expectedSide.GetProperty("partialOrchard").GetBoolean(), side.PartialOrchard);
                Assert.Equal(expectedSide.GetProperty("railroadEmbankment").GetBoolean(), side.RailroadEmbankment);
                Assert.Equal(expectedSide.GetProperty("hexsideTerrain").GetString(), side.HexsideTerrain?.Name);
                Assert.Equal(expectedSide.GetProperty("terrain").GetString(), side.Terrain?.Name);
            }
        }

        var fixture = LosFidelity.Read(path);
        var comparison = LosFidelity.Compare(map, fixture);
        output.WriteLine($"{name}: {comparison.Answered}/{comparison.Pairs}, unsupported: {string.Join(", ", comparison.Unsupported.Select(p => $"{p.Key}={p.Value}"))}");
        Assert.True(comparison.Disagreements.Count == 0, string.Join(Environment.NewLine, comparison.Disagreements.Take(15)));
        Assert.Equal(fixture.Pairs.Count(pair => pair.VaslError is null), comparison.Answered);
    }

    private static LosMap Build(string name, TerrainCatalog catalog)
    {
        var spec = XDocument.Load(Specification).Root!.Elements("map").Single(map => (string?)map.Attribute("name") == name);
        var codes = new byte[Geometry.GridWidth * Geometry.GridHeight];
        var levels = new sbyte[codes.Length];
        foreach (var paint in spec.Elements("paint"))
        {
            var box = ((string)paint.Attribute("box")!).Split(',').Select(int.Parse).ToArray();
            var terrain = catalog[(string)paint.Attribute("terrain")!];
            for (var x = box[0]; x < box[2]; x++)
            {
                for (var y = box[1]; y < box[3]; y++)
                {
                    codes[(x * Geometry.GridHeight) + y] = terrain.Code;
                    levels[(x * Geometry.GridHeight) + y] = (sbyte)(int)paint.Attribute("elevation")!;
                }
            }
        }

        var orchards = new Dictionary<HexName, IReadOnlySet<HexsideDirection>>();
        var railroads = new Dictionary<HexName, IReadOnlySet<HexsideDirection>>();
        foreach (var annotation in spec.Elements("annotation"))
        {
            var dictionary = (string?)annotation.Attribute("kind") == "partialOrchard" ? orchards : railroads;
            dictionary.Add(HexName.Parse((string)annotation.Attribute("hex")!),
                ((string)annotation.Attribute("sides")!).Select(side => (HexsideDirection)(side - '0')).ToHashSet());
        }

        var grid = new TerrainGrid(Geometry, codes, levels, new bool[Geometry.HexCount]);
        var annotations = new HexsideAnnotations(new Dictionary<HexName, IReadOnlySet<HexsideDirection>>(), railroads, orchards);
        var facts = VaslCompatibleHexFactDerivation.Derive(grid, catalog, annotations);
        var overrides = spec.Elements("location").ToDictionary(e => HexName.Parse((string)e.Attribute("hex")!), e => catalog[(string)e.Attribute("terrain")!]);
        var hexes = facts.Hexes.Select(hex => overrides.TryGetValue(hex.Hex, out var terrain)
            ? hex with
            {
                Center = hex.Center with
                {
                    Terrain = terrain
                },
                Locations = hex.Locations.Select(loc => loc == hex.Center ? loc with
                {
                    Terrain = terrain
                } : loc).ToArray()
            }
            : hex).ToArray();
        return LosMap.ForGrid(BoardRef.Parse("bd01"), grid, new HexFactSet(Geometry, facts.DerivationVersion, hexes), catalog);
    }
}
