using System.Globalization;
using System.Text.Json;
using LimboDancer.Campaign.Refinement;
using LimboDancer.Domains.Asl.Maps.Features;

[assembly: CollectionBehavior(DisableTestParallelization = true)]
namespace LimboDancer.Campaign.Refinement.Tests;

public sealed class PilotTests
{
    private const string Seed = "00112233445566778899aabbccddeeff";
    private static readonly string Source = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "goodwood.json"));
    private static readonly PilotInputs Inputs = new(Source);
    private static readonly Lazy<PilotResult> Primary = new(() => new PilotGenerator(Inputs).Generate(Seed));
    private static readonly string[] GoldenFields = ["sha256-counter-v1", Seed, "golden-world", "source", "spatial", "profile", "generator", "building-detail", "A/U-7/parcel/10/20"];
    private static readonly uint[] GoldenValues = [2042751958, 2933409446, 261155148, 59040833, 2074831444, 4116365430, 2769344829, 402625825];

    [Fact]
    public void RandomProtocolMatchesIndependentPythonVectors()
    {
        var random = new SeedRandom(GoldenFields);
        Assert.Equal(GoldenValues, Enumerable.Range(0, 8).Select(_ => random.Next()));
        random = new SeedRandom(GoldenFields);
        Assert.Equal(GoldenValues.Select(v => v % 100), Enumerable.Range(0, 8).Select(_ => random.Below(100)));
        Assert.Throws<ArgumentOutOfRangeException>(() => random.Below(0));
        Assert.Throws<ArgumentOutOfRangeException>(() => random.Below((1UL << 32) + 1));
    }

    [Theory]
    [InlineData(0, 0, 0)]
    [InlineData(90, 6942478, 0)]
    [InlineData(180, 2500000, 6942478)]
    [InlineData(270, 0, 2500000)]
    public void SourceRotationMatchesJoinedFrame(int rotation, long x, long y) =>
        Assert.Equal(new MetricPoint(x, y), SpatialFrame.Project(0, 0, rotation, 0, 0));

    [Fact]
    public void NegativeParityAndQuantizationAreExplicit()
    {
        Assert.Equal(-20000, SpatialFrame.Center(-1, 0).Y);
        Assert.Equal(0, SpatialFrame.Center(-2, 0).Y);
        Assert.Throws<ArgumentException>(() => new Crop(1, 0).Geometry());
        var crop = new Crop(-20, -10);
        foreach (var point in new[] { new MetricPoint(-600003, -234567), new MetricPoint(0, 0), new MetricPoint(123456, 987654) })
        {
            var back = SpatialFrame.Metric(SpatialFrame.Pixel(point, crop), crop);
            Assert.InRange(Math.Abs(point.X - back.X), 0, 6);
            Assert.InRange(Math.Abs(point.Y - back.Y), 0, 6);
        }
        Assert.Equal(new FixedVector(1, -1), FixedVector.FromPixels(1.0 / 128, -1.0 / 128));
    }

    [Fact]
    public void JoinedBoardsShareMetricSeamAndUnsupportedHillsFailClosed()
    {
        Assert.Equal(SpatialFrame.Project(1, 0.25, 90, 0, 1130), SpatialFrame.Project(0, 0.25, 90, 0, 2260));
        var error = Assert.Throws<InvalidDataException>(() => new PilotGenerator(Inputs.ForVersion(PilotInputs.LegacyGeneratorVersion)).Generate(Seed, new Crop(52, 116)));
        Assert.Contains("Unsupported source terrain/elevation", error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void PrimaryBridgeHasConnectedRoadApproachesOnBothSides()
    {
        var result = Primary.Value; var crop = Inputs.DefaultCrop;
        var bridge = Inputs.Bridges.Single(b => b.Key == Inputs.Profile.Anchor);
        var center = new MetricPoint((long)bridge.Polygon.Average(p => p.X), (long)bridge.Polygon.Average(p => p.Y));
        var pixel = SpatialFrame.Pixel(center, crop); var x = pixel.X / 64; var y = pixel.Y / 64;
        Assert.Equal(84, result.Grid.CodeAt(x, y));
        var reached = new HashSet<(int X, int Y)> { (x, y) }; var queue = new Queue<(int X, int Y)>(); queue.Enqueue((x, y));
        var left = false; var right = false;
        while (queue.TryDequeue(out var p))
        {
            if (result.Grid.CodeAt(p.X, p.Y) == 66) { left |= p.X < x - 100; right |= p.X > x + 100; }
            for (var dx = -1; dx <= 1; dx++) for (var dy = -1; dy <= 1; dy++)
            {
                var next = (X: p.X + dx, Y: p.Y + dy);
                if (result.Grid.TryGetCode(next.X, next.Y, out var code) && code is 66 or 84 && reached.Add(next)) queue.Enqueue(next);
            }
        }
        Assert.True(left && right, "The compiled crossing must connect road approaches extending at least 60m to each side.");
    }

    [Fact]
    public void PinnedInputsRejectChangesAndMalformedSeeds()
    {
        Assert.Throws<InvalidDataException>(() => new PilotInputs(Source + " "));
        Assert.Throws<ArgumentException>(() => new PilotGenerator(Inputs).Generate("not-a-seed"));
        Assert.Throws<ArgumentException>(() => new PilotGenerator(Inputs).Generate(Seed.ToUpperInvariant()));
        Assert.Equal(Inputs.ProfileDigest, new PilotInputs(Source.Replace("\r\n", "\n", StringComparison.Ordinal)).ProfileDigest);
    }

    [Fact]
    public void RepeatedGenerationMatchesFeaturesGridAndPackage()
    {
        var first = Primary.Value; var second = new PilotGenerator(Inputs).Generate(Seed);
        Assert.Equal(first.Package.FeaturesBytes(), second.Package.FeaturesBytes());
        Assert.Equal(first.Package.Version, second.Package.Version);
        Assert.Equal(PilotGenerator.GridHash(first.Grid), PilotGenerator.GridHash(second.Grid));
        Assert.Equal(first.Manifest, second.Manifest);
        Assert.False(first.Validation.HasErrors);
        Assert.Equal(676, first.Facts.Hexes.Count);
        using var manifest = JsonDocument.Parse(first.Manifest);
        Assert.Contains(manifest.RootElement.GetProperty("Hexes").EnumerateArray(), h => h.GetProperty("Overlaps").GetArrayLength() > 1);
        Assert.Contains(first.MetricFeatures, f => f.SourceKey == Inputs.Profile.Anchor);
        Assert.All(first.MetricFeatures.Where(f => f.Kind == "building"), f => Assert.Equal("generated", f.Provenance));
        // The physical bridge remains painted and appears in derived facts.
        Assert.Contains(first.Grid.DistinctCodes(), c => c == 84);
        Assert.Contains(first.Facts.Hexes, h => h.Bridge is not null);
    }

    [Fact]
    public void ThirtyTwoSeedsRetainFixedGeometryAndVaryBuildings()
    {
        var hashes = new HashSet<string>(StringComparer.Ordinal);
        string? locked = null;
        for (var i = 0; i < 32; i++)
        {
            var result = new PilotGenerator(Inputs).Generate(i.ToString("x32", CultureInfo.InvariantCulture));
            Assert.False(result.Validation.HasErrors);
            Assert.Contains(result.MetricFeatures, f => f.Kind == "building");
            var fixedFeatures = JsonSerializer.Serialize(result.MetricFeatures.Where(f => f.Provenance != "generated")
                .OrderBy(f => f.SourceKey, StringComparer.Ordinal).Select(f => new { f.SourceKey, f.Kind, f.Code, f.WidthMm, f.Points }));
            locked ??= fixedFeatures; Assert.Equal(locked, fixedFeatures);
            hashes.Add(PilotGenerator.GridHash(result.Grid));
            var pilot = new PilotDeployment(Inputs, result, i.ToString("x32", CultureInfo.InvariantCulture));
            var context = DeploymentTests.SourceContext();
            var setup = pilot.AutoDeploy(context, pilot.Create(context));
            Assert.Equal(8, setup.Placements.Length);
            pilot.Validate(context, setup);
        }
        Assert.Equal(32, hashes.Count);
    }

    [Fact]
    public void OverlappingCropsPreserveWorldFeaturesInEitherOrder()
    {
        var generator = new PilotGenerator(Inputs);
        var shifted = new Crop(60, 92);
        var second = generator.Generate(Seed, shifted);
        var first = generator.Generate(Seed);
        var common = first.MetricFeatures.Join(second.MetricFeatures, a => a.Id, b => b.Id, (a, b) => (a, b)).ToArray();
        Assert.NotEmpty(common);
        Assert.All(common, pair => Assert.Equal(JsonSerializer.Serialize(pair.a), JsonSerializer.Serialize(pair.b)));
        Assert.Equal(second.Manifest, generator.Generate(Seed, shifted).Manifest);
        // This fixture's translation is exactly 450 x 129 raster pixels. Only compare
        // the common two-hex interior; edge half-hex sampling is intentionally excluded.
        var compared = 0;
        foreach (var a in first.Facts.Hexes.Where(f => f.Index.Column >= 10 && f.Index.Column < 31 && f.Index.Row >= 4 && f.Index.Row < 18))
        {
            var b = second.Facts[new LimboDancer.Domains.Asl.Maps.Geometry.HexIndex(a.Index.Column - 8, a.Index.Row - 2)];
            Assert.Equal(JsonSerializer.Serialize(new { a.BaseLevel, a.Stairway, a.Center, a.Locations, a.Hexsides, a.Bridge }),
                JsonSerializer.Serialize(new { b.BaseLevel, b.Stairway, b.Center, b.Locations, b.Hexsides, b.Bridge }));
            compared++;
        }
        Assert.True(compared > 250);
    }

    [Fact]
    public void PublicationIsRepeatableReloadableAndRejectsCorruption()
    {
        var root = Path.Combine(Path.GetTempPath(), "asl-pilot-test-" + Guid.NewGuid().ToString("N"));
        try
        {
            var result = Primary.Value;
            var directory = PackagePublisher.Publish(result, Inputs, root);
            Assert.Equal(directory, PackagePublisher.Publish(result, Inputs, root));
            var store = new BoardPackageStore(Path.Combine(directory, "boards"));
            Assert.Equal(result.Package.Version, store.Load(result.Package.Board).Package!.Version);
            File.AppendAllText(Path.Combine(directory, "refinement.json"), " ");
            Assert.Throws<InvalidDataException>(() => PackagePublisher.Publish(result, Inputs, root));
        }
        finally { if (Directory.Exists(root)) Directory.Delete(root, true); }
    }

    [Fact]
    public void CancelledGenerationAndPublicationNeverPublish()
    {
        using var cancel = new CancellationTokenSource(); cancel.Cancel();
        Assert.Throws<OperationCanceledException>(() => new PilotGenerator(Inputs).Generate(Seed, cancellationToken: cancel.Token));
        var root = Path.Combine(Path.GetTempPath(), "asl-pilot-test-" + Guid.NewGuid().ToString("N"));
        try
        {
            Assert.Throws<OperationCanceledException>(() => PackagePublisher.Publish(Primary.Value, Inputs, root, cancel.Token));
            Assert.Empty(Directory.GetDirectories(root));
        }
        finally { if (Directory.Exists(root)) Directory.Delete(root, true); }
    }
}
