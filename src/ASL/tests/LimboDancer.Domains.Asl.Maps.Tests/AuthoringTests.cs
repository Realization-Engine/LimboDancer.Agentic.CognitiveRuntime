using System.Text;
using LimboDancer.Domains.Asl.Maps.Coordinates;
using LimboDancer.Domains.Asl.Maps.Derivation;
using LimboDancer.Domains.Asl.Maps.Features;
using LimboDancer.Domains.Asl.Maps.Geometry;
using static LimboDancer.Domains.Asl.Maps.Tests.FeatureTestCatalog;

namespace LimboDancer.Domains.Asl.Maps.Tests;

public sealed class FeatureModelJsonTests
{
    [Fact]
    public void ModelsRoundTripByteForByte()
    {
        var model = FeatureCompilerTests.SampleModel() with
        {
            Annotations = new HexAnnotations(new HashSet<HexIndex> { new(4, 3) }, new HexsideAnnotations(
                new Dictionary<HexName, IReadOnlySet<HexsideDirection>> { [HexName.Parse("E4")] = new HashSet<HexsideDirection> { HexsideDirection.South, HexsideDirection.North } },
                new Dictionary<HexName, IReadOnlySet<HexsideDirection>>(),
                new Dictionary<HexName, IReadOnlySet<HexsideDirection>>())),
        };
        var bytes = FeatureModelJson.Serialize(model);
        var back = FeatureModelJson.Deserialize(bytes);
        Assert.Equal(bytes, FeatureModelJson.Serialize(back));
        Assert.Equal(model.Features.Count, back.Features.Count);
        Assert.Contains(new HexIndex(4, 3), back.Annotations.Stairways);

        // The compiled grids agree, so nothing the compiler reads was lost.
        Assert.Equal(FeatureCompilerTests.Hash(FeatureCompiler.Compile(model, Catalog).Grid), FeatureCompilerTests.Hash(FeatureCompiler.Compile(back, Catalog).Grid));
    }

    [Fact]
    public void CanonicalFormIgnoresListOrderAndSortsKeys()
    {
        var model = FeatureCompilerTests.SampleModel();
        var reversed = model with
        {
            Features = [.. model.Features.Reverse()]
        };
        Assert.Equal(FeatureModelJson.Serialize(model), FeatureModelJson.Serialize(reversed));

        var json = Encoding.UTF8.GetString(FeatureModelJson.Serialize(model));
        Assert.StartsWith("{\"annotations\":", json, StringComparison.Ordinal);
        Assert.DoesNotContain(" ", json.Replace("\"Stone Building", string.Empty, StringComparison.Ordinal), StringComparison.Ordinal);
    }

    [Fact]
    public void CanonicalStringsUseMinimalEscapes()
    {
        var json = Encoding.UTF8.GetString(CanonicalJson.Serialize(new Dictionary<string, object?> { ["b"] = "q\"\\\n\u0001é", ["a"] = 1L }));
        Assert.Equal("{\"a\":1,\"b\":\"q\\\"\\\\\\n\\u0001é\"}", json);
    }
}

public sealed class BoardPackageTests : IDisposable
{
    private readonly string root = Path.Combine(Path.GetTempPath(), "asl-packages-" + Guid.NewGuid().ToString("N"));

    [Fact]
    public void VersionDependsOnDefiningEntriesOnly()
    {
        var model = FeatureCompilerTests.SampleModel();
        var package = new BoardPackage(BoardRef.Parse("ab-sample"), "Sample", model);
        Assert.Matches("^[0-9a-f]{64}$", package.Version);
        Assert.Equal(package.Version, (package with
        {
            Validation = new ValidationSummary(3, 2, 1)
        }).Version);
        Assert.NotEqual(package.Version, (package with
        {
            Name = "Renamed"
        }).Version);
        Assert.NotEqual(package.Version, (package with
        {
            Model = model with
            {
                Features = model.Features.Skip(1).ToArray()
            }
        }).Version);
        Assert.NotEqual(package.Version, (package with
        {
            Model = model with
            {
                CatalogHash = "other"
            }
        }).Version);
    }

    [Fact]
    public void PackagesSaveAndLoadAsDirectories()
    {
        var store = new BoardPackageStore(root);
        var package = new BoardPackage(BoardRef.Parse("ab-sample"), "Sample", FeatureCompilerTests.SampleModel(), new ValidationSummary(0, 1, 0));
        store.Save(package);

        Assert.Equal([BoardRef.Parse("ab-sample")], store.List());
        var loaded = Assert.IsType<BoardPackage>(store.Load(package.Board).Package);
        Assert.Equal(package.Version, loaded.Version);
        Assert.Equal("Sample", loaded.Name);
        Assert.True(File.Exists(Path.Combine(root, "sample", BoardPackage.ManifestEntry)));
    }

    [Fact]
    public void AChangedFeaturesEntryIsRefused()
    {
        var store = new BoardPackageStore(root);
        var package = new BoardPackage(BoardRef.Parse("ab-sample"), "Sample", FeatureCompilerTests.SampleModel());
        store.Save(package);
        var features = Path.Combine(root, "sample", BoardPackage.FeaturesEntry);
        File.WriteAllText(features, File.ReadAllText(features).Replace("\"layer\":0", "\"layer\":1", StringComparison.Ordinal));

        var load = store.Load(package.Board);
        Assert.Null(load.Package);
        Assert.Equal("MAP-PKG-002", Assert.Single(load.Diagnostics).Code);
        Assert.Equal("MAP-PKG-001", Assert.Single(store.Load(BoardRef.Parse("ab-missing")).Diagnostics).Code);
    }

    public void Dispose()
    {
        if (Directory.Exists(root))
        {
            Directory.Delete(root, recursive: true);
        }
    }
}

public sealed class AuthoringCommandTests
{
    private static readonly BoardGeometry Geometry = BoardGeometry.StandardGeomorphic;

    [Fact]
    public void EveryCommandHasAnInverseThatRestoresTheModel()
    {
        var model = FeatureCompilerTests.SampleModel();
        var hex = Geometry.IndexOf(HexName.Parse("C3"));
        AuthoringCommand[] commands =
        [
            new AddFeature(new AreaTerrainFeature("new-woods", 2, Box(700, 50, 800, 150), Code("Woods"))),
            new ReplaceFeature(new AreaTerrainFeature("woods", 3, Box(10, 10, 60, 60), Code("Grain"))),
            new RemoveFeature("house"),
            new SetStairway(hex, true),
            new SetHexsideMark(HexsideMark.Slope, new HexsideRef(hex, HexsideDirection.North), true),
            new CompositeCommand([new RemoveFeature("road"), new SetStairway(hex, true)], "two edits"),
        ];

        foreach (var command in commands)
        {
            var applied = AuthoringCommands.Apply(model, command, Catalog);
            Assert.True(applied.Succeeded, applied.Error);
            Assert.NotEqual(FeatureModelJson.Serialize(model), FeatureModelJson.Serialize(applied.Model!));
            var undone = AuthoringCommands.Apply(applied.Model!, applied.Inverse!, Catalog);
            Assert.True(undone.Succeeded, undone.Error);
            Assert.Equal(FeatureModelJson.Serialize(model), FeatureModelJson.Serialize(undone.Model!));
        }
    }

    [Theory]
    [InlineData("duplicate")]
    [InlineData("missing")]
    [InlineData("wrong-kind")]
    [InlineData("unknown-code")]
    [InlineData("short-ring")]
    [InlineData("bad-span")]
    [InlineData("no-centerline-width")]
    [InlineData("off-board-stairway")]
    public void InvalidCommandsAreRefusedWithAReason(string kind)
    {
        var model = FeatureCompilerTests.SampleModel();
        AuthoringCommand command = kind switch
        {
            "duplicate" => new AddFeature(new AreaTerrainFeature("woods", 0, Box(0, 0, 10, 10), Code("Woods"))),
            "missing" => new RemoveFeature("nothing"),
            "wrong-kind" => new AddFeature(new AreaTerrainFeature("x", 0, Box(0, 0, 10, 10), Code("Stone Building"))),
            "unknown-code" => new AddFeature(new AreaTerrainFeature("x", 0, Box(0, 0, 10, 10), 250)),
            "short-ring" => new AddFeature(new AreaTerrainFeature("x", 0, new FeatureShape([[FixedVector.FromPixels(0, 0), FixedVector.FromPixels(5, 5)]]), Code("Woods"))),
            "bad-span" => new AddFeature(new HexsideTerrainFeature("x", 0, Code("Wall"), [new HexsideSpan(new HexsideRef(new HexIndex(0, 0), HexsideDirection.North), 40, 20)])),
            "no-centerline-width" => new AddFeature(new LinearTerrainFeature("x", 0, Code("Paved Road"),
                new CenterlinePath(FixedVector.FromPixels(0, 0), [new PathSegment(FixedVector.FromPixels(10, 0))]), FixedPoint.Zero, null)),
            _ => new SetStairway(new HexIndex(99, 99), true),
        };

        var result = AuthoringCommands.Apply(model, command, Catalog);
        Assert.False(result.Succeeded);
        Assert.False(string.IsNullOrWhiteSpace(result.Error));
    }

    [Fact]
    public void AreaTerrainMayUseTheBaseCodeToClearTerrain()
    {
        var model = FeatureCompilerTests.SampleModel();
        var result = AuthoringCommands.Apply(model, new AddFeature(new AreaTerrainFeature("clearing", 5, Box(100, 100, 120, 120), Code("Open Ground"))), Catalog);
        Assert.True(result.Succeeded, result.Error);
    }
}

public sealed class AuthoringGeometryTests
{
    private static readonly BoardGeometry Geometry = BoardGeometry.StandardGeomorphic;

    [Fact]
    public void HexsideHitTestsFindTheNearestCanonicalSide()
    {
        var hex = Geometry.IndexOf(HexName.Parse("E4"));
        var south = BuildingKit.HexsideMidpoint(Geometry, new HexsideRef(hex, HexsideDirection.South));
        var hit = AuthoringGeometry.NearestHexside(Geometry, new FixedVector(south.X, south.Y + 64));
        Assert.Equal(Geometry.Canonicalize(new HexsideRef(hex, HexsideDirection.South)), hit);
        Assert.Null(AuthoringGeometry.NearestHexside(Geometry, BuildingKit.Center(Geometry, hex)));
    }

    [Fact]
    public void SnappingPrefersHexCentersThenVerticesThenMidpoints()
    {
        var hex = Geometry.IndexOf(HexName.Parse("E4"));
        var center = BuildingKit.Center(Geometry, hex);
        Assert.Equal((center, AuthoringGeometry.SnapKind.HexCenter), AuthoringGeometry.Snap(Geometry, null, new FixedVector(center.X + 100, center.Y - 80)));

        var vertex = Geometry.Vertices(hex)[0];
        var vertexPoint = FixedVector.FromExactPixels(vertex.X, vertex.Y);
        Assert.Equal(AuthoringGeometry.SnapKind.Vertex, AuthoringGeometry.Snap(Geometry, null, new FixedVector(vertexPoint.X + 64, vertexPoint.Y)).Kind);

        var free = AuthoringGeometry.Snap(Geometry, null, new FixedVector(center.X + (15 * 64) + 10, center.Y + (3 * 64) + 40));
        Assert.Equal(AuthoringGeometry.SnapKind.Pixel, free.Kind);
        Assert.Equal(0, free.Point.X % 64);
        Assert.Equal(0, free.Point.Y % 64);
    }

    [Fact]
    public void TheTopmostFeatureIsHit()
    {
        var model = FeatureCompilerTests.SampleModel();
        Assert.Equal("house", AuthoringGeometry.FeatureAt(model, FixedVector.FromPixels(440, 200))?.Id);
        Assert.Equal("hill", AuthoringGeometry.FeatureAt(model, FixedVector.FromPixels(600, 150))?.Id);
        Assert.Null(AuthoringGeometry.FeatureAt(model, FixedVector.FromPixels(1700, 600)));
    }

    [Fact]
    public void SpanFootprintsInNeighborHexesJoinIntoOneBuilding()
    {
        var first = Geometry.IndexOf(HexName.Parse("E4"));
        var second = Geometry.Neighbor(first, HexsideDirection.South)!.Value;
        var model = Model(Geometry,
            new BuildingFeature("a", 0, [BuildingKit.Span(Geometry, first, 0)], Code("Stone Building"), "B1"),
            new BuildingFeature("b", 0, [BuildingKit.Span(Geometry, second, 0)], Code("Stone Building"), "B1"));
        var facts = Derive(FeatureCompiler.Compile(model, Catalog).Grid);
        Assert.Equal("Stone Building", facts[first].Center.Terrain?.Name);
        Assert.Equal("Stone Building", facts[first].Hexsides[3].Terrain?.Name);
        Assert.DoesNotContain(BoardValidator.Validate(model, facts, Catalog).Findings, finding => finding.Code == "MAP-VAL-003");
    }
}

public sealed class BoardValidatorTests
{
    private static readonly BoardGeometry Geometry = BoardGeometry.StandardGeomorphic;

    [Fact]
    public void AnEmptyBoardIsValid()
    {
        var model = FeatureModel.New(Geometry, "test-catalog");
        Assert.Empty(Validate(model).Findings);
    }

    [Fact]
    public void BuildingsMustConnectAndAgree()
    {
        var e4 = Geometry.IndexOf(HexName.Parse("E4"));
        var g4 = Geometry.IndexOf(HexName.Parse("G4"));
        var apart = Validate(Model(Geometry,
            new BuildingFeature("a", 0, [BuildingKit.Centered(Geometry, e4)], Code("Stone Building"), "B1"),
            new BuildingFeature("b", 0, [BuildingKit.Centered(Geometry, g4)], Code("Stone Building"), "B1")));
        Assert.Contains(apart.Findings, finding => finding.Code == "MAP-VAL-003" && finding.Severity == MapDiagnosticSeverity.Error);

        var mixed = Validate(Model(Geometry,
            new BuildingFeature("a", 0, [BuildingKit.Centered(Geometry, e4)], Code("Stone Building"), "B1"),
            new BuildingFeature("b", 0, [BuildingKit.Centered(Geometry, g4)], Code("Stone Building, 2 Level"), "B1")));
        Assert.Contains(mixed.Findings, finding => finding.Code == "MAP-VAL-004");
    }

    [Fact]
    public void StairwaysNeedUpperLevels()
    {
        var hex = Geometry.IndexOf(HexName.Parse("E4"));
        var model = FeatureModel.New(Geometry, "test-catalog") with
        {
            Annotations = new HexAnnotations(new HashSet<HexIndex> { hex }, HexsideAnnotations.None)
        };
        var finding = Assert.Single(Validate(model).Findings);
        Assert.Equal("MAP-VAL-005", finding.Code);
        Assert.Equal(hex, finding.Hex);

        var withBuilding = model with
        {
            Features = [new BuildingFeature("b", 0, [BuildingKit.Centered(Geometry, hex)], Code("Stone Building, 2 Level"), "B1")]
        };
        Assert.DoesNotContain(Validate(withBuilding).Findings, item => item.Code == "MAP-VAL-005");
    }

    [Fact]
    public void BoundariesNearSamplePointsAreFragile()
    {
        var center = Geometry.CenterPoint(Geometry.IndexOf(HexName.Parse("E4")));
        var model = Model(Geometry, new AreaTerrainFeature("w", 0, Box(center.X, center.Y - 40, center.X + 40, center.Y + 40), Code("Woods")));
        Assert.Contains(Validate(model).Findings, finding => finding.Code == "MAP-VAL-007" && finding.FeatureId == "w");
    }

    [Fact]
    public void InvalidCodesPinsAndUnnestedLevelsAreReported()
    {
        var findings = Validate(Model(Geometry,
            new AreaTerrainFeature("bad", 0, Box(0, 0, 10, 10), Code("Wall")),
            new FidelityPin("pin", 0, Box(20, 20, 21, 21), Code("Woods"), 0),
            new ElevationRegion("high", 0, Box(300, 300, 350, 350), 2))).Findings;
        Assert.Contains(findings, finding => finding.Code == "MAP-VAL-010" && finding.FeatureId == "bad");
        Assert.Contains(findings, finding => finding.Code == "MAP-VAL-012" && finding.Severity == MapDiagnosticSeverity.Info);
        Assert.Contains(findings, finding => finding.Code == "MAP-VAL-011" && finding.FeatureId == "high");
    }

    private static ValidationReport Validate(FeatureModel model) =>
        BoardValidator.Validate(model, Derive(FeatureCompiler.Compile(model, Catalog).Grid), Catalog);
}
