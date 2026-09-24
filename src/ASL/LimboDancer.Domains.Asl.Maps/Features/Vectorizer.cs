using LimboDancer.Domains.Asl.Maps.Coordinates;
using LimboDancer.Domains.Asl.Maps.Derivation;
using LimboDancer.Domains.Asl.Maps.Geometry;
using LimboDancer.Domains.Asl.Maps.Grid;
using LimboDancer.Domains.Asl.Maps.Terrain;

namespace LimboDancer.Domains.Asl.Maps.Features;

/// <summary>An ingested board as the vectorizer needs it: the grid, its derived facts, and its metadata annotations.</summary>
public sealed record VectorizerSource(
    BoardRef Board,
    string? SourceVersion,
    TerrainGrid Grid,
    HexFactSet Facts,
    HexsideAnnotations Annotations,
    string CatalogHash);

public sealed class VectorizerOptions
{
    /// <summary>Douglas-Peucker tolerance in pixels outside the areas the fidelity loop tightens.</summary>
    public double Tolerance { get; init; } = 1.5;

    /// <summary>
    /// Tolerance for linear terrain, whose narrow shapes lose proportionally more area to simplification. Calibrated on
    /// board 01, where 1.5 pixels left dirt roads at 0.946 overlap.
    /// </summary>
    public double LinearTolerance { get; init; } = 1.0;

    public int MaxIterations { get; init; } = 8;

    /// <summary>Pixels around a failing hex whose boundaries drop to tolerance 0 (section 7.4).</summary>
    public int TightMargin { get; init; } = 6;
}

/// <summary>The vectorized model, its compiled grid and derived facts, and any facts the loop could not reproduce.</summary>
public sealed record VectorizeResult(
    FeatureModel Model,
    TerrainGrid Compiled,
    HexFactSet CompiledFacts,
    IReadOnlyList<string> Differences,
    int Iterations,
    IReadOnlyList<MapDiagnostic> Diagnostics)
{
    public bool FactsMatch => Differences.Count == 0;
}

/// <summary>
/// Produces an editable Feature Model from an ingested board (Model Design section 7). Compiling the output must
/// reproduce the source Hex Facts exactly (F3); the fidelity loop tightens simplification and adds fidelity pins until
/// it does, or reports <c>MAP-VEC-001</c>.
/// </summary>
public static class Vectorizer
{
    public const string Version = "1.0.0";

    private const int Background = 0;

    public static VectorizeResult Vectorize(VectorizerSource source, TerrainCatalog catalog, VectorizerOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(catalog);
        options ??= new VectorizerOptions();
        var grid = source.Grid;
        var geometry = grid.Geometry;
        var baseCode = catalog.TryGet("Open Ground", out var open) ? open.Code : (byte)0;
        var kinds = TerrainKinds.Classify(catalog, baseCode);

        // Stage 1: reverse the post-passes.
        var codes = grid.Codes.ToArray();
        var elevations = grid.Elevations.ToArray();
        ReversePostPasses(geometry, catalog, codes, elevations);
        var baseElevation = elevations.Min();

        // Stages 2 to 6: traced passes per kind, and elevation.
        var passes = new List<KindPass>
        {
            KindPass.Build(geometry, codes, kinds, TerrainKind.Area, DitheredCodes(geometry, grid.Codes, kinds), 0x03),
            KindPass.Build(geometry, codes, kinds, TerrainKind.Linear, [], 0x04),
            KindPass.Build(geometry, codes, kinds, TerrainKind.Bridge, [], 0x05),
            KindPass.Build(geometry, codes, kinds, TerrainKind.Building, [], 0x06),
        };
        var buildingIds = BuildingIds(geometry, passes[3]);
        var elevationPasses = new List<(int Level, BoundarySimplifier Simplifier)>();
        for (var level = baseElevation + 1; level <= elevations.Max(); level++)
        {
            var labels = new int[elevations.Length];
            for (var cell = 0; cell < labels.Length; cell++)
            {
                labels[cell] = elevations[cell] >= level ? 1 : Background;
            }

            elevationPasses.Add((level, new BoundarySimplifier(geometry, labels)));
        }

        var hexsides = HexsideFeatures(geometry, grid, source.Facts, source.Annotations);
        var stairways = geometry.Hexes().Where(grid.HasStairway).ToHashSet();
        var annotations = new HexAnnotations(stairways, source.Annotations);

        // Stage 8: the fidelity loop.
        var tight = new List<PixelBox>();
        var tightHexes = new HashSet<HexIndex>();
        var pinRounds = new Dictionary<HexIndex, int>();
        var pinMask = new bool[codes.Length];
        FeatureModel model;
        CompileResult compiled;
        HexFactSet facts;
        IReadOnlyList<HexIndex> failing;
        var iteration = 0;
        while (true)
        {
            iteration++;
            double Tolerance(PixelBox box) => tight.Exists(area => area.Intersects(box)) ? 0 : options.Tolerance;
            double LinearTolerance(PixelBox box) => Math.Min(Tolerance(box), options.LinearTolerance);
            var features = new List<Feature>();
            features.AddRange(elevationPasses.SelectMany((pass, index) => ElevationFeatures(pass.Level, pass.Simplifier, index, Tolerance)));
            features.AddRange(passes[0].Features(Tolerance, (id, shape, code) => new AreaTerrainFeature(id, 0, shape, code)));
            features.AddRange(passes[1].Features(LinearTolerance, (id, shape, code) => new LinearTerrainFeature(id, 0, code, null, FixedPoint.Zero, shape)));
            features.AddRange(passes[2].Features(Tolerance, (id, shape, code) => new BridgeFeature(id, 0, shape, code)));
            features.AddRange(BuildingFeatures(passes[3], buildingIds, Tolerance));
            features.AddRange(hexsides);
            features.AddRange(PinFeatures(geometry, grid, pinMask));

            model = new FeatureModel(geometry, source.CatalogHash, baseCode, baseElevation, features, annotations,
                FeatureProvenance.Vectorized(source.Board.Value, source.SourceVersion, Version));
            compiled = FeatureCompiler.Compile(model, catalog);
            facts = VaslCompatibleHexFactDerivation.Derive(compiled.Grid, catalog, source.Annotations);
            failing = HexFactComparer.DifferingHexes(source.Facts, facts);
            if (failing.Count == 0 || iteration >= options.MaxIterations)
            {
                break;
            }

            foreach (var hex in failing)
            {
                var bounds = HexBounds(geometry, hex);
                if (tightHexes.Add(hex))
                {
                    tight.Add(Expand(bounds, options.TightMargin));
                    continue;
                }

                // Tolerance 0 was not enough. Pin first only the pixels the derivation samples for this hex, then the
                // pixels that still differ over a widening area around it.
                var round = pinRounds.GetValueOrDefault(hex);
                pinRounds[hex] = round + 1;
                if (round == 0)
                {
                    MarkSampleDifferences(geometry, grid, compiled.Grid, hex, pinMask);
                }
                else
                {
                    MarkDifferences(geometry, grid, compiled.Grid, Expand(bounds, 2 + (4 * (round - 1))), pinMask);
                }
            }
        }

        var differences = HexFactComparer.Differences(source.Facts, facts);
        var diagnostics = new List<MapDiagnostic>();
        if (differences.Count > 0)
        {
            diagnostics.Add(new MapDiagnostic("MAP-VEC-001", MapDiagnosticSeverity.Error,
                $"{source.Board}: {failing.Count} hexes still differ after {iteration} iterations: "
                + string.Join(", ", failing.Take(20).Select(geometry.NameOf))));
        }

        return new VectorizeResult(model, compiled.Grid, facts, differences, iteration, diagnostics);
    }

    // Stage 1: depression pixels one level up; exterior factory walls back to their factory where a neighbor shows which.
    private static void ReversePostPasses(BoardGeometry geometry, TerrainCatalog catalog, byte[] codes, sbyte[] elevations)
    {
        var height = geometry.GridHeight;
        var width = geometry.GridWidth;
        var source = codes.ToArray();
        foreach (var type in catalog.Types)
        {
            if (!type.IsDepression)
            {
                continue;
            }

            for (var cell = 0; cell < codes.Length; cell++)
            {
                if (source[cell] == type.Code)
                {
                    elevations[cell] = checked((sbyte)(elevations[cell] + 1));
                }
            }
        }

        var factoriesFor = new IReadOnlyList<byte>?[256];
        foreach (var type in catalog.Types)
        {
            var factories = FeatureCompiler.FactoriesWithWall(catalog, type.Code);
            if (factories.Count > 0)
            {
                factoriesFor[type.Code] = factories;
            }
        }

        for (var x = 0; x < width; x++)
        {
            for (var y = 0; y < height; y++)
            {
                if (factoriesFor[source[(x * height) + y]] is not { } candidates)
                {
                    continue;
                }

                // The nearest factory interior of a matching type, searched in rings up to three pixels out.
                for (var radius = 1; radius <= 3; radius++)
                {
                    var found = FindNear(source, width, height, x, y, radius, candidates);
                    if (found is { } factory)
                    {
                        codes[(x * height) + y] = factory;
                        break;
                    }
                }
            }
        }
    }

    private static byte? FindNear(byte[] source, int width, int height, int x, int y, int radius, IReadOnlyList<byte> candidates)
    {
        for (var dx = -radius; dx <= radius; dx++)
        {
            for (var dy = -radius; dy <= radius; dy++)
            {
                if (Math.Max(Math.Abs(dx), Math.Abs(dy)) != radius)
                {
                    continue;
                }

                var nx = x + dx;
                var ny = y + dy;
                if (nx >= 0 && ny >= 0 && nx < width && ny < height && candidates.Contains(source[(nx * height) + ny]))
                {
                    return source[(nx * height) + ny];
                }
            }
        }

        return null;
    }

    // A code is dithered when fewer than half of its pixels have all four neighbors in the same code (section 7.5).
    private static HashSet<byte> DitheredCodes(BoardGeometry geometry, ReadOnlySpan<byte> codes, TerrainKind[] kinds)
    {
        var width = geometry.GridWidth;
        var height = geometry.GridHeight;
        var total = new int[256];
        var interior = new int[256];
        for (var x = 0; x < width; x++)
        {
            for (var y = 0; y < height; y++)
            {
                var code = codes[(x * height) + y];
                total[code]++;
                if (Same(codes, width, height, x - 1, y, code) && Same(codes, width, height, x + 1, y, code)
                    && Same(codes, width, height, x, y - 1, code) && Same(codes, width, height, x, y + 1, code))
                {
                    interior[code]++;
                }
            }
        }

        var dithered = new HashSet<byte>();
        for (var code = 0; code < 256; code++)
        {
            if (kinds[code] == TerrainKind.Area && total[code] >= 100 && interior[code] * 2 < total[code])
            {
                dithered.Add((byte)code);
            }
        }

        return dithered;
    }

    private static bool Same(ReadOnlySpan<byte> codes, int width, int height, int x, int y, byte code) =>
        x < 0 || y < 0 || x >= width || y >= height || codes[(x * height) + y] == code;

    /// <summary>A code agreement statistic used by F3: whether a code is dithered in a grid.</summary>
    public static IReadOnlySet<byte> Dithered(TerrainGrid grid, TerrainCatalog catalog)
    {
        ArgumentNullException.ThrowIfNull(grid);
        ArgumentNullException.ThrowIfNull(catalog);
        var baseCode = catalog.TryGet("Open Ground", out var open) ? open.Code : (byte)0;
        return DitheredCodes(grid.Geometry, grid.Codes, TerrainKinds.Classify(catalog, baseCode));
    }

    private static IEnumerable<Feature> ElevationFeatures(int level, BoundarySimplifier simplifier, int passIndex, Func<PixelBox, double> tolerance)
    {
        var index = 0;
        foreach (var region in simplifier.Regions)
        {
            if (region.Label != Background && simplifier.Simplify(region, tolerance) is { } shape)
            {
                yield return new ElevationRegion(FeatureIds.Sequential((byte)(0x10 + passIndex), index++), 0, shape, level);
            }
        }
    }

    // Stage 3: components that cover the edge samples on both sides of a shared hexside form one building.
    private static Dictionary<int, string> BuildingIds(BoardGeometry geometry, KindPass pass)
    {
        var parent = Enumerable.Range(0, pass.Simplifier.Regions.Count).ToArray();
        int Find(int item)
        {
            while (parent[item] != item)
            {
                parent[item] = parent[parent[item]];
                item = parent[item];
            }

            return item;
        }

        foreach (var hex in geometry.Hexes())
        {
            for (var side = 0; side < 3; side++)
            {
                if (geometry.Neighbor(hex, (HexsideDirection)side) is not { } neighbor)
                {
                    continue;
                }

                var a = pass.RegionAt(geometry.EdgeSamplePoint(hex, (HexsideDirection)side));
                var b = pass.RegionAt(geometry.EdgeSamplePoint(neighbor, ((HexsideDirection)side).Opposite()));
                if (a is { } first && b is { } second)
                {
                    parent[Find(first)] = Find(second);
                }
            }
        }

        var ids = new Dictionary<int, string>();
        var names = new Dictionary<int, string>();
        for (var region = 0; region < parent.Length; region++)
        {
            if (pass.Simplifier.Regions[region].Label == Background)
            {
                continue;
            }

            var root = Find(region);
            if (!names.TryGetValue(root, out var name))
            {
                name = "B" + (names.Count + 1).ToString(System.Globalization.CultureInfo.InvariantCulture);
                names[root] = name;
            }

            ids[region] = name;
        }

        return ids;
    }

    private static IEnumerable<Feature> BuildingFeatures(KindPass pass, Dictionary<int, string> buildingIds, Func<PixelBox, double> tolerance)
    {
        var groups = new SortedDictionary<(string BuildingId, byte Code), List<FeatureShape>>();
        for (var index = 0; index < pass.Simplifier.Regions.Count; index++)
        {
            var region = pass.Simplifier.Regions[index];
            if (region.Label == Background || pass.Simplifier.Simplify(region, tolerance) is not { } shape)
            {
                continue;
            }

            var key = (buildingIds[index], pass.CodeOf(region.Label));
            if (!groups.TryGetValue(key, out var shapes))
            {
                shapes = [];
                groups[key] = shapes;
            }

            shapes.Add(shape);
        }

        var sequence = 0;
        foreach (var ((buildingId, code), shapes) in groups)
        {
            yield return new BuildingFeature(FeatureIds.Sequential(0x06, sequence++), 0, shapes, code, buildingId);
        }
    }

    // Stage 2: hexside terrain from the derived facts, one feature per code, with widths and extents from the pixels.
    private static List<Feature> HexsideFeatures(BoardGeometry geometry, TerrainGrid grid, HexFactSet facts, HexsideAnnotations annotations)
    {
        var spans = new SortedDictionary<byte, SortedDictionary<(int Column, int Row, int Side), HexsideSpan>>();
        foreach (var hex in facts.Hexes)
        {
            for (var side = 0; side < hex.Hexsides.Count; side++)
            {
                var terrain = hex.Hexsides[side].HexsideTerrain;
                if (terrain is null || terrain.Name is "Rrembankment" or "PartialOrchard"
                    || Annotated(annotations, hex.Hex, (HexsideDirection)side))
                {
                    continue;
                }

                var canonical = geometry.Canonicalize(new HexsideRef(hex.Index, (HexsideDirection)side));
                if (!spans.TryGetValue(terrain.Code, out var byCode))
                {
                    byCode = [];
                    spans[terrain.Code] = byCode;
                }

                var key = (canonical.Hex.Column, canonical.Hex.Row, (int)canonical.Side);
                if (!byCode.ContainsKey(key))
                {
                    byCode[key] = Measure(geometry, grid, canonical, terrain.Code);
                }
            }
        }

        var features = new List<Feature>();
        var sequence = 0;
        foreach (var (code, byCode) in spans)
        {
            features.Add(new HexsideTerrainFeature(FeatureIds.Sequential(0x07, sequence++), 0, code, [.. byCode.Values]));
        }

        return features;
    }

    private static bool Annotated(HexsideAnnotations annotations, HexName hex, HexsideDirection side) =>
        (annotations.RailroadEmbankments.TryGetValue(hex, out var embankments) && embankments.Contains(side))
        || (annotations.PartialOrchards.TryGetValue(hex, out var orchards) && orchards.Contains(side));

    // The extent from 33 samples along the side, and the width as the median run across the side at five points.
    private static HexsideSpan Measure(BoardGeometry geometry, TerrainGrid grid, HexsideRef side, byte code)
    {
        var (from, to) = FeatureCompiler.HexsideEndpoints(geometry, side);
        var first = -1;
        var last = -1;
        for (var step = 0; step <= 32; step++)
        {
            var x = (from.X + ((to.X - from.X) * step / 32)) / 64;
            var y = (from.Y + ((to.Y - from.Y) * step / 32)) / 64;
            if (NearCode(grid, x, y, code))
            {
                first = first < 0 ? step : first;
                last = step;
            }
        }

        var start = first < 0 ? 0 : first * 2;
        var end = first < 0 ? 64 : last * 2;
        if (start > 32 || end < 32 || end <= start)
        {
            (start, end) = (0, 64);
        }

        double dx = to.X - from.X;
        double dy = to.Y - from.Y;
        var length = Math.Sqrt((dx * dx) + (dy * dy));
        var nx = -dy / length;
        var ny = dx / length;
        var runs = new List<int>();
        foreach (var t in new[] { 0.3, 0.4, 0.5, 0.6, 0.7 })
        {
            var px = (from.X + (dx * t)) / 64;
            var py = (from.Y + (dy * t)) / 64;
            var run = 0;
            for (var offset = -12; offset <= 12; offset++)
            {
                if (grid.TryGetCode((int)Math.Floor(px + (nx * offset)), (int)Math.Floor(py + (ny * offset)), out var found) && found == code)
                {
                    run++;
                }
            }

            runs.Add(run);
        }

        runs.Sort();
        var width = Math.Clamp(runs[2], FeatureCompiler.MinimumHexsideWidth, 17);
        return new HexsideSpan(side, start, end, width == FeatureCompiler.StandardHexsideWidth ? null : width);
    }

    private static bool NearCode(TerrainGrid grid, int x, int y, byte code)
    {
        for (var dx = -1; dx <= 1; dx++)
        {
            for (var dy = -1; dy <= 1; dy++)
            {
                if (grid.TryGetCode(x + dx, y + dy, out var found) && found == code)
                {
                    return true;
                }
            }
        }

        return false;
    }

    // Stage 8: pinned pixels traced exactly per (code, elevation) of the source.
    private static List<Feature> PinFeatures(BoardGeometry geometry, TerrainGrid source, bool[] pinMask)
    {
        if (Array.IndexOf(pinMask, true) < 0)
        {
            return [];
        }

        var keys = new Dictionary<(byte Code, sbyte Elevation), int>();
        var byLabel = new List<(byte Code, sbyte Elevation)> { default };
        var labels = new int[pinMask.Length];
        for (var cell = 0; cell < pinMask.Length; cell++)
        {
            if (!pinMask[cell])
            {
                continue;
            }

            var key = (source.Codes[cell], source.Elevations[cell]);
            if (!keys.TryGetValue(key, out var label))
            {
                label = byLabel.Count;
                keys[key] = label;
                byLabel.Add(key);
            }

            labels[cell] = label;
        }

        var simplifier = new BoundarySimplifier(geometry, labels);
        var pins = new List<Feature>();
        foreach (var region in simplifier.Regions)
        {
            if (region.Label != Background && simplifier.Simplify(region, _ => 0) is { } shape)
            {
                var (code, elevation) = byLabel[region.Label];
                pins.Add(new FidelityPin(FeatureIds.Sequential(0x08, pins.Count), 0, shape, code, elevation));
            }
        }

        return pins;
    }

    // The pixels Hex.resetTerrain reads for a hex: the center and its four diagonal neighbors, the four 5-pixel probes,
    // its six edge samples, and each neighbor's opposite edge sample.
    private static void MarkSampleDifferences(BoardGeometry geometry, TerrainGrid source, TerrainGrid compiled, HexIndex hex, bool[] pinMask)
    {
        var center = geometry.CenterPoint(hex);
        var points = new List<GridPoint> { center };
        foreach (var (dx, dy) in new[] { (1, -1), (1, 1), (-1, 1), (-1, -1), (5, 0), (-5, 0), (0, 5), (0, -5) })
        {
            points.Add(new GridPoint(center.X + dx, center.Y + dy));
        }

        foreach (var side in HexsideDirections.All)
        {
            points.Add(geometry.EdgeSamplePoint(hex, side));
            if (geometry.Neighbor(hex, side) is { } neighbor)
            {
                points.Add(geometry.EdgeSamplePoint(neighbor, side.Opposite()));
            }
        }

        var height = geometry.GridHeight;
        foreach (var point in points)
        {
            if (!geometry.ContainsCell(point.X, point.Y))
            {
                continue;
            }

            var cell = (point.X * height) + point.Y;
            if (source.Codes[cell] != compiled.Codes[cell] || source.Elevations[cell] != compiled.Elevations[cell])
            {
                pinMask[cell] = true;
            }
        }
    }

    private static void MarkDifferences(BoardGeometry geometry, TerrainGrid source, TerrainGrid compiled, PixelBox box, bool[] pinMask)
    {
        var height = geometry.GridHeight;
        for (var x = Math.Max(0, box.MinX); x <= Math.Min(geometry.GridWidth - 1, box.MaxX); x++)
        {
            for (var y = Math.Max(0, box.MinY); y <= Math.Min(height - 1, box.MaxY); y++)
            {
                var cell = (x * height) + y;
                if (source.Codes[cell] != compiled.Codes[cell] || source.Elevations[cell] != compiled.Elevations[cell])
                {
                    pinMask[cell] = true;
                }
            }
        }
    }

    private static PixelBox HexBounds(BoardGeometry geometry, HexIndex hex)
    {
        var border = geometry.Border(hex);
        return new PixelBox(border.Min(point => point.X), border.Min(point => point.Y), border.Max(point => point.X), border.Max(point => point.Y));
    }

    private static PixelBox Expand(PixelBox box, int margin) => new(box.MinX - margin, box.MinY - margin, box.MaxX + margin, box.MaxY + margin);

    /// <summary>One kind's label image, traced once: its own codes, lower kinds as background, higher kinds under-filled.</summary>
    private sealed class KindPass
    {
        private readonly int[] labels;
        private readonly byte[] codeOfLabel;
        private readonly int height;
        private readonly int width;
        private readonly byte idPrefix;
        private int[]? components;

        private KindPass(BoardGeometry geometry, int[] labels, byte[] codeOfLabel, byte idPrefix)
        {
            this.labels = labels;
            this.codeOfLabel = codeOfLabel;
            this.idPrefix = idPrefix;
            height = geometry.GridHeight;
            width = geometry.GridWidth;
            Simplifier = new BoundarySimplifier(geometry, labels);
        }

        public BoundarySimplifier Simplifier
        {
            get;
        }

        public byte CodeOf(byte label) => codeOfLabel[label];

        /// <summary>Region index for each cell, matched to the simplifier's regions by anchor cell.</summary>
        public int[] Components => components ??= LabelComponents();

        public int? RegionAt(GridPoint point)
        {
            if (point.X < 0 || point.Y < 0 || point.X >= width || point.Y >= height)
            {
                return null;
            }

            var cell = (point.X * height) + point.Y;
            return labels[cell] == Background ? null : Components[cell];
        }

        public static KindPass Build(BoardGeometry geometry, byte[] codes, TerrainKind[] kinds, TerrainKind kind, HashSet<byte> dithered, byte idPrefix)
        {
            var width = geometry.GridWidth;
            var height = geometry.GridHeight;
            var labelOfCode = new int[256];
            var codeOfLabel = new List<byte> { 0 };
            var labels = new int[codes.Length];
            const int Unknown = -1;
            for (var cell = 0; cell < codes.Length; cell++)
            {
                var code = codes[cell];
                var codeKind = kinds[code];
                if (codeKind == kind)
                {
                    if (labelOfCode[code] == 0)
                    {
                        labelOfCode[code] = codeOfLabel.Count;
                        codeOfLabel.Add(code);
                    }

                    labels[cell] = labelOfCode[code];
                }
                else
                {
                    labels[cell] = codeKind < kind ? Background : Unknown;
                }
            }

            foreach (var code in dithered.Order())
            {
                if (labelOfCode[code] != 0)
                {
                    Close(labels, width, height, labelOfCode[code]);
                }
            }

            UnderFill(labels, width, height, Unknown);
            return new KindPass(geometry, labels, [.. codeOfLabel], idPrefix);
        }

        // Morphological closing with a 3 by 3 square, adding only background pixels to the label.
        private static void Close(int[] labels, int width, int height, int label)
        {
            var dilated = new bool[labels.Length];
            for (var x = 0; x < width; x++)
            {
                for (var y = 0; y < height; y++)
                {
                    if (labels[(x * height) + y] != label)
                    {
                        continue;
                    }

                    for (var dx = -1; dx <= 1; dx++)
                    {
                        for (var dy = -1; dy <= 1; dy++)
                        {
                            var nx = x + dx;
                            var ny = y + dy;
                            if (nx >= 0 && ny >= 0 && nx < width && ny < height)
                            {
                                dilated[(nx * height) + ny] = true;
                            }
                        }
                    }
                }
            }

            for (var x = 0; x < width; x++)
            {
                for (var y = 0; y < height; y++)
                {
                    var cell = (x * height) + y;
                    if (labels[cell] != Background || !dilated[cell])
                    {
                        continue;
                    }

                    var eroded = true;
                    for (var dx = -1; dx <= 1 && eroded; dx++)
                    {
                        for (var dy = -1; dy <= 1 && eroded; dy++)
                        {
                            var nx = x + dx;
                            var ny = y + dy;
                            if (nx >= 0 && ny >= 0 && nx < width && ny < height && !dilated[(nx * height) + ny])
                            {
                                eroded = false;
                            }
                        }
                    }

                    if (eroded)
                    {
                        labels[cell] = label;
                    }
                }
            }
        }

        // Pixels of higher kinds take the label of the nearest known pixel (breadth first, column-major seeds).
        private static void UnderFill(int[] labels, int width, int height, int unknown)
        {
            var queue = new Queue<int>();
            for (var cell = 0; cell < labels.Length; cell++)
            {
                if (labels[cell] == unknown)
                {
                    continue;
                }

                var x = cell / height;
                var y = cell % height;
                if ((x > 0 && labels[cell - height] == unknown) || (x < width - 1 && labels[cell + height] == unknown)
                    || (y > 0 && labels[cell - 1] == unknown) || (y < height - 1 && labels[cell + 1] == unknown))
                {
                    queue.Enqueue(cell);
                }
            }

            while (queue.Count > 0)
            {
                var cell = queue.Dequeue();
                var x = cell / height;
                var y = cell % height;
                Spread(cell - height, x > 0);
                Spread(cell + height, x < width - 1);
                Spread(cell - 1, y > 0);
                Spread(cell + 1, y < height - 1);

                void Spread(int neighbor, bool inside)
                {
                    if (inside && labels[neighbor] == unknown)
                    {
                        labels[neighbor] = labels[cell];
                        queue.Enqueue(neighbor);
                    }
                }
            }

            // A board with no known pixels at all: everything is background.
            for (var cell = 0; cell < labels.Length; cell++)
            {
                if (labels[cell] == unknown)
                {
                    labels[cell] = Background;
                }
            }
        }

        // 4-connected components of equal labels, numbered in column-major order of their first cell, which is the
        // tracer's region order within each label; mapped back to the simplifier's region indices by anchor cell.
        private int[] LabelComponents()
        {
            var regionByAnchor = new Dictionary<GridPoint, int>();
            for (var index = 0; index < Simplifier.Regions.Count; index++)
            {
                regionByAnchor[Simplifier.Regions[index].AnchorCell] = index;
            }

            var result = new int[labels.Length];
            Array.Fill(result, -1);
            var stack = new Stack<int>();
            for (var start = 0; start < labels.Length; start++)
            {
                if (result[start] >= 0)
                {
                    continue;
                }

                var region = regionByAnchor[new GridPoint(start / height, start % height)];
                result[start] = region;
                stack.Push(start);
                while (stack.Count > 0)
                {
                    var cell = stack.Pop();
                    var x = cell / height;
                    var y = cell % height;
                    Visit(cell - height, x > 0);
                    Visit(cell + height, x < width - 1);
                    Visit(cell - 1, y > 0);
                    Visit(cell + 1, y < height - 1);

                    void Visit(int neighbor, bool inside)
                    {
                        if (inside && result[neighbor] < 0 && labels[neighbor] == labels[cell])
                        {
                            result[neighbor] = region;
                            stack.Push(neighbor);
                        }
                    }
                }
            }

            return result;
        }

        public IEnumerable<Feature> Features(Func<PixelBox, double> tolerance, Func<string, FeatureShape, byte, Feature> create)
        {
            var sequence = 0;
            foreach (var region in Simplifier.Regions)
            {
                if (region.Label != Background && Simplifier.Simplify(region, tolerance) is { } shape)
                {
                    yield return create(FeatureIds.Sequential(idPrefix, sequence++), shape, codeOfLabel[region.Label]);
                }
            }
        }
    }
}
