using System.Globalization;
using System.Text.Json;
using LimboDancer.Domains.Asl.Maps.Coordinates;
using LimboDancer.Domains.Asl.Maps.Derivation;
using LimboDancer.Domains.Asl.Maps.Features;
using LimboDancer.Domains.Asl.Maps.Geometry;
using LimboDancer.Domains.Asl.Maps.Grid;

namespace LimboDancer.Campaign.Refinement;

public sealed record MetricFeature(string Id, string SourceKey, string Kind, string Provenance, byte Code, int WidthMm, MetricPoint[] Points);
public sealed record PilotResult(BoardPackage Package, TerrainGrid Grid, HexFactSet Facts,
    ValidationReport Validation, byte[] Manifest, IReadOnlyList<MetricFeature> MetricFeatures);

/// <summary>A bounded inspection pilot, not tactical rules admission or a complete Situation.</summary>
public sealed class PilotGenerator(PilotInputs inputs)
{
    private static readonly string[] PilotAssumptions = new[] { "250m formation / 40m ASL", "Paved roads 10m; shallow streams 8m; widths use vertical pixel scale", "Source illustration crossing retained as stone bridge; material is a pilot assumption", "Town roofs omitted; generated stone buildings are not historical footprints", "Woods preserved; connected clearing generation deferred", "Board edges not admitted for deployment" };
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };
    public PilotResult Generate(string seed, Crop? crop = null, CancellationToken cancellationToken = default)
    {
        SeedRandom.ValidateSeed(seed); crop ??= inputs.DefaultCrop;
        var geometry = crop.Geometry(); var profile = inputs.Profile;
        var spatialDigest = PilotIdentity.SpatialDigest;
        var defining = PilotIdentity.Defining(inputs, seed);
        var world = PilotIdentity.World(inputs, seed);
        var metric = new List<MetricFeature>();
        string Id(string key) => BoardPackage.Hash(CanonicalJson.Serialize(new[] { world, key }));
        SeedRandom Stream(string domain, string key) => new([.. defining, domain, key]);
        void Add(string key, string kind, string provenance, byte code, MetricPoint[] points, int width = 0)
        {
            if (MetricGeometry.Intersects(points, crop, width)) metric.Add(new(Id(key), key, kind, provenance, code, width, points));
        }
        var regions = inputs.Regions.Where(r => MetricGeometry.Intersects(r.Polygon, crop, 50000)).ToArray();
        if (regions.Length == 0) throw new InvalidDataException("Crop has no source terrain.");
        foreach (var region in regions)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if ((inputs.SupportsElevation ? region.Elevation is < 0 or > 2 : region.Elevation != 0) || region.Kind is not ("open" or "town" or "woods" or "water"))
                throw new InvalidDataException($"Unsupported source terrain/elevation in {region.Key}: {region.Kind}/{region.Elevation}.");
            if (inputs.SupportsElevation && region.Elevation > 0)
                Add(region.Key + "/elevation", "elevation", "source-level-mapped", (byte)region.Elevation, region.Polygon);
            if (region.Kind is "woods" or "water")
                Add(region.Key, "area", "source-preserved", profile.TerrainCodes[region.Kind], region.Polygon);
            if (region.Kind != "town") continue;
            // A region's complete parcel lattice is evaluated before crop filtering. Neighboring requests share keys.
            var spacing = profile.ParcelSpacingMm;
            var minX = (long)Math.Floor(region.Polygon.Min(p => p.X) / (double)spacing);
            var maxX = (long)Math.Ceiling(region.Polygon.Max(p => p.X) / (double)spacing);
            var minY = (long)Math.Floor(region.Polygon.Min(p => p.Y) / (double)spacing);
            var maxY = (long)Math.Ceiling(region.Polygon.Max(p => p.Y) / (double)spacing);
            var nearbyRoutes = inputs.Routes.Where(r => BoundsOverlap(r.Points, region.Polygon, 100000)).ToArray();
            for (var x = minX; x <= maxX; x++) for (var y = minY; y <= maxY; y++)
            {
                var key = string.Create(CultureInfo.InvariantCulture, $"{region.Key}/parcel/{x}/{y}");
                var random = Stream("building-detail", key);
                var chance = profile.BuildingPercent;
                if (random.Below(100) >= chance) continue;
                var size = profile.BuildingMinMm + (int)random.Below((ulong)(profile.BuildingMaxMm - profile.BuildingMinMm + 1));
                var center = new MetricPoint(x * spacing, y * spacing);
                var polygon = MetricGeometry.Box(center, size);
                // A conservative circle around each footprint protects polygon boundaries, all roads and channels.
                var radius = size + profile.BoundaryInsetMm;
                if (!MetricGeometry.Contains(region.Polygon, center) || MetricGeometry.Near(region.Polygon, center, radius, true)) continue;
                if (nearbyRoutes.Any(r => MetricGeometry.Near(r.Points, center,
                    radius + profile.RouteClearanceMm + Math.Max(profile.RoadWidthMm, profile.StreamWidthMm), false))) continue;
                Add(key, "building", "generated", profile.TerrainCodes["building"], polygon);
                // Woods are a locked broad region. Isolated clearings are deferred until their access is certified.
            }
        }
        foreach (var route in inputs.Routes)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (route.Kind is not ("road" or "stream")) throw new InvalidDataException("Unsupported route " + route.Key);
            // Small overlapping chunks preserve the source polyline, including joins, without rasterizing distant paths.
            for (var i = 0; i < route.Points.Length - 1; i += 32)
            {
                var points = route.Points.Skip(i).Take(33).ToArray();
                Add(string.Create(CultureInfo.InvariantCulture, $"{route.Key}/{i}"), route.Kind, "source-preserved", profile.TerrainCodes[route.Kind], points,
                    route.Kind == "road" ? profile.RoadWidthMm : profile.StreamWidthMm);
            }
        }
        foreach (var bridge in inputs.Bridges) Add(bridge.Key, "bridge", "source-guided-crossing", profile.TerrainCodes["bridge"], bridge.Polygon);
        FeatureShape Shape(MetricFeature f) => new([f.Points.Select(p => SpatialFrame.Pixel(p, crop)).ToArray()]);
        Feature Convert(MetricFeature f) => (f.Kind switch
        {
            "elevation" => (Feature)new ElevationRegion(f.Id, 0, Shape(f), f.Code),
            "area" => new AreaTerrainFeature(f.Id, 0, Shape(f), f.Code),
            "building" => new BuildingFeature(f.Id, 0, [Shape(f)], f.Code, f.Id),
            "bridge" => new BridgeFeature(f.Id, 0, Shape(f), f.Code),
            _ => new LinearTerrainFeature(f.Id, f.Kind == "stream" ? 10 : 0, f.Code,
                new CenterlinePath(SpatialFrame.Pixel(f.Points[0], crop), f.Points.Skip(1).Select(p => new PathSegment(SpatialFrame.Pixel(p, crop))).ToArray()),
                FixedPoint.FromPixels(f.WidthMm / 40000.0 * 64.5), null)
        }) with
        { Notes = f.Provenance + ": " + f.SourceKey };
        var features = metric.OrderBy(f => f.Id, StringComparer.Ordinal).ToArray();
        if (features.Select(f => f.Id).Distinct(StringComparer.Ordinal).Count() != features.Length) throw new InvalidDataException("Duplicate feature IDs.");
        var model = FeatureModel.New(geometry, inputs.CatalogBlob) with { Features = features.Select(Convert).ToArray() };
        cancellationToken.ThrowIfCancellationRequested();
        var grid = FeatureCompiler.Compile(model, inputs.Catalog).Grid;
        var facts = VaslCompatibleHexFactDerivation.Derive(grid, inputs.Catalog, HexsideAnnotations.None);
        var validation = BoardValidator.Validate(model, facts, inputs.Catalog);
        if (validation.HasErrors) throw new InvalidDataException(string.Join("\n", validation.Findings.Where(f => f.Severity == LimboDancer.Domains.Asl.Maps.MapDiagnosticSeverity.Error).Select(f => f.Code + ": " + f.Message)));
        if (crop == inputs.DefaultCrop && (!features.Any(f => f.SourceKey == profile.Anchor) || !features.Any(f => f.Kind == "building") || !features.Any(f => f.Kind == "road") || !features.Any(f => f.Kind == "stream")))
            throw new InvalidDataException("Primary pilot must contain the crossing, a settlement, road and stream.");
        var package = new BoardPackage(BoardRef.Parse(PilotIdentity.Reference(inputs, seed, crop)), "Goodwood crossing inspection pilot", model, validation.Summary);
        var parents = geometry.Hexes().Select(h =>
        {
            var center = SpatialFrame.Center(crop.Column + h.Column, crop.Row + h.Row);
            var hits = regions.Where(r => MetricGeometry.Contains(r.Polygon, center)).Select(r => r.Key).ToArray();
            var overlaps = regions.Where(r => MetricGeometry.Overlaps(r.Polygon, MetricGeometry.Hex(center))).Select(r => r.Key).ToArray();
            return new { Hex = geometry.NameOf(h).ToString(), Column = crop.Column + h.Column, Row = crop.Row + h.Row, Parent = hits.FirstOrDefault(), CenterMatches = hits, Overlaps = overlaps, DeploymentAdmitted = false };
        }).ToArray();
        if (parents.Any(p => p.Parent is null)) throw new InvalidDataException("Crop extends outside covered source polygons.");
        var manifest = JsonSerializer.SerializeToUtf8Bytes(new
        {
            FormatVersion = 1,
            Seed = seed,
            MapSpace = PilotInputs.MapSpace,
            WorldIdentity = world,
            SourceSha256 = profile.SourceSha256,
            ProfileSha256 = inputs.ProfileDigest,
            SpatialVersion = SpatialFrame.Version,
            SpatialSha256 = spatialDigest,
            GeneratorVersion = inputs.Version,
            RandomStream = SeedRandom.Version,
            CatalogSha256 = profile.CatalogSha256,
            CatalogBlob = inputs.CatalogBlob,
            CompilerVersion = FeatureCompiler.Version,
            DerivationVersion = VaslCompatibleHexFactDerivation.Version,
            SerializationVersion = "feature-model-1",
            Crop = crop,
            PackageReference = package.Board.Value,
            PackageVersion = package.Version,
            FeaturesSha256 = BoardPackage.Hash(package.FeaturesBytes()),
            GridSha256 = GridHash(grid),
            MetricFeatures = features,
            Hexes = parents,
            Findings = validation.Findings,
            Admission = "inspection-only; setup, movement, LOS and combat not admitted",
            Assumptions = inputs.SupportsElevation ? PilotAssumptions.Append("Source levels 0-2 map one-to-one to ASL base levels, a scenario assumption not measured height; B10.1 center-dot elevation; no inferred cliffs or slope SSR.").ToArray() : PilotAssumptions
        }, JsonOptions);
        cancellationToken.ThrowIfCancellationRequested();
        return new(package, grid, facts, validation, manifest, features);
    }
    public static string GridHash(TerrainGrid grid)
    {
        ArgumentNullException.ThrowIfNull(grid);
        return BoardPackage.Hash(grid.Codes.ToArray().Concat(grid.Elevations.ToArray().Select(v => unchecked((byte)v)))
            .Concat(grid.Stairways.ToArray().Select(v => v ? (byte)1 : (byte)0)).ToArray());
    }
    private static bool BoundsOverlap(MetricPoint[] a, MetricPoint[] b, long margin) =>
        a.Max(p => p.X) >= b.Min(p => p.X) - margin && a.Min(p => p.X) <= b.Max(p => p.X) + margin &&
        a.Max(p => p.Y) >= b.Min(p => p.Y) - margin && a.Min(p => p.Y) <= b.Max(p => p.Y) + margin;
}
