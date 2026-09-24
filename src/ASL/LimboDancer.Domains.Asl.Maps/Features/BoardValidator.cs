using System.Globalization;
using LimboDancer.Domains.Asl.Maps.Derivation;
using LimboDancer.Domains.Asl.Maps.Geometry;
using LimboDancer.Domains.Asl.Maps.Terrain;

namespace LimboDancer.Domains.Asl.Maps.Features;

/// <summary>One validation finding, located at a hex, a feature, or both.</summary>
public sealed record ValidationFinding(string Code, MapDiagnosticSeverity Severity, string Message, HexIndex? Hex = null, string? FeatureId = null);

public sealed record ValidationReport(IReadOnlyList<ValidationFinding> Findings)
{
    public ValidationSummary Summary => new(
        Findings.Count(finding => finding.Severity == MapDiagnosticSeverity.Error),
        Findings.Count(finding => finding.Severity == MapDiagnosticSeverity.Warning),
        Findings.Count(finding => finding.Severity == MapDiagnosticSeverity.Info));

    public bool HasErrors => Findings.Any(finding => finding.Severity == MapDiagnosticSeverity.Error);
}

/// <summary>
/// Validates a Feature Model with its derived Hex Facts (Model Design section 6). It reports findings and never
/// changes the model. Rules 003, 004, 005, 007, 010, 011, and 012 are implemented; 001, 002, 006, 008, and 009 are
/// deferred (Model Design section 17).
/// </summary>
public static class BoardValidator
{
    public static ValidationReport Validate(FeatureModel model, HexFactSet facts, TerrainCatalog catalog)
    {
        ArgumentNullException.ThrowIfNull(model);
        ArgumentNullException.ThrowIfNull(facts);
        ArgumentNullException.ThrowIfNull(catalog);
        var findings = new List<ValidationFinding>();
        foreach (var feature in model.Features.OrderBy(feature => feature.Id, StringComparer.Ordinal))
        {
            if (AuthoringCommands.Check(model, feature, catalog) is { } error)
            {
                findings.Add(new ValidationFinding("MAP-VAL-010", MapDiagnosticSeverity.Error, error, FeatureId: feature.Id));
            }

            if (feature is FidelityPin)
            {
                findings.Add(new ValidationFinding("MAP-VAL-012", MapDiagnosticSeverity.Info,
                    "Fidelity pin: it reproduces a VASL artifact and can be removed for an original map.", FeatureId: feature.Id));
            }
        }

        CheckElevationNesting(model, findings);
        CheckBuildings(model, facts, findings);
        CheckStairways(model, facts, findings);
        CheckFragileSamples(model, findings);
        return new ValidationReport(findings);
    }

    // MAP-VAL-011: a level L region should lie within the level L - 1 regions, above the base level.
    private static void CheckElevationNesting(FeatureModel model, List<ValidationFinding> findings)
    {
        var geometry = model.Geometry;
        var regions = model.Features.OfType<ElevationRegion>().ToArray();
        foreach (var region in regions.Where(region => region.Level > model.BaseElevation + 1).OrderBy(region => region.Id, StringComparer.Ordinal))
        {
            var below = regions.Where(other => other.Level == region.Level - 1).SelectMany(other => other.Shape.Rings).ToArray();
            var support = ShapeRasterizer.Coverage(below, geometry.GridWidth, geometry.GridHeight);
            var outside = 0;
            ShapeRasterizer.Fill(region.Shape.Rings, geometry.GridWidth, geometry.GridHeight, (x, y) => outside += support[(x * geometry.GridHeight) + y] ? 0 : 1);
            if (outside > 0)
            {
                findings.Add(new ValidationFinding("MAP-VAL-011", MapDiagnosticSeverity.Warning,
                    string.Create(CultureInfo.InvariantCulture, $"Level {region.Level} extends {outside} pixels beyond the level {region.Level - 1} regions."),
                    FeatureId: region.Id));
            }
        }
    }

    // MAP-VAL-003 and 004: hexes of one building id must connect through covered shared hexsides and derive one code.
    private static void CheckBuildings(FeatureModel model, HexFactSet facts, List<ValidationFinding> findings)
    {
        var geometry = model.Geometry;
        foreach (var group in model.Features.OfType<BuildingFeature>().GroupBy(building => building.BuildingId).OrderBy(group => group.Key, StringComparer.Ordinal))
        {
            var covered = ShapeRasterizer.Coverage(group.SelectMany(building => building.Footprints).SelectMany(footprint => footprint.Rings), geometry.GridWidth, geometry.GridHeight);
            bool Covered(GridPoint point) => geometry.ContainsCell(point.X, point.Y) && covered[(point.X * geometry.GridHeight) + point.Y];
            var hexes = geometry.Hexes().Where(hex => Covered(geometry.CenterPoint(hex))).ToArray();
            if (hexes.Length == 0)
            {
                continue;
            }

            var codes = hexes.Select(hex => facts[hex].Center.Terrain?.Name ?? "none").Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal).ToArray();
            if (codes.Length > 1)
            {
                findings.Add(new ValidationFinding("MAP-VAL-004", MapDiagnosticSeverity.Error,
                    $"Building {group.Key} derives different terrain in its hexes: {string.Join(", ", codes)}.", hexes[0], group.First().Id));
            }

            var members = hexes.ToHashSet();
            var reached = new HashSet<HexIndex> { hexes[0] };
            var queue = new Queue<HexIndex>(reached);
            while (queue.Count > 0)
            {
                var hex = queue.Dequeue();
                foreach (var side in HexsideDirections.All)
                {
                    if (geometry.Neighbor(hex, side) is { } neighbor && members.Contains(neighbor) && !reached.Contains(neighbor)
                        && Covered(geometry.EdgeSamplePoint(hex, side)) && Covered(geometry.EdgeSamplePoint(neighbor, side.Opposite())))
                    {
                        reached.Add(neighbor);
                        queue.Enqueue(neighbor);
                    }
                }
            }

            if (reached.Count < members.Count)
            {
                var apart = members.Except(reached).Select(geometry.NameOf).Select(name => name.ToString()).Order(StringComparer.Ordinal);
                findings.Add(new ValidationFinding("MAP-VAL-003", MapDiagnosticSeverity.Error,
                    $"Building {group.Key} is not connected through covered shared hexsides: {string.Join(", ", apart)} stand apart.", hexes[0], group.First().Id));
            }
        }
    }

    // MAP-VAL-005: a stairway belongs in a hex with upper building levels.
    private static void CheckStairways(FeatureModel model, HexFactSet facts, List<ValidationFinding> findings)
    {
        foreach (var hex in model.Annotations.Stairways.OrderBy(hex => hex.Column).ThenBy(hex => hex.Row))
        {
            if (model.Geometry.Contains(hex) && !facts[hex].Locations.Any(location => location.Level > 0))
            {
                findings.Add(new ValidationFinding("MAP-VAL-005", MapDiagnosticSeverity.Warning,
                    $"Stairway in {model.Geometry.NameOf(hex)}, whose center terrain has no upper levels.", hex));
            }
        }
    }

    // MAP-VAL-007: a feature boundary within one pixel of a derivation sample point makes the derived fact fragile.
    private static void CheckFragileSamples(FeatureModel model, List<ValidationFinding> findings)
    {
        var geometry = model.Geometry;
        const int Bucket = 16;
        var samples = new Dictionary<(int, int), List<(HexIndex Hex, double X, double Y)>>();
        foreach (var hex in geometry.Hexes())
        {
            foreach (var point in SamplePoints(geometry, hex))
            {
                var key = (point.X / Bucket, point.Y / Bucket);
                if (!samples.TryGetValue(key, out var list))
                {
                    list = [];
                    samples[key] = list;
                }

                list.Add((hex, point.X + 0.5, point.Y + 0.5));
            }
        }

        var reported = new HashSet<HexIndex>();
        foreach (var feature in model.Features.OrderBy(feature => feature.Id, StringComparer.Ordinal))
        {
            foreach (var rings in AuthoringGeometry.Rings(geometry, feature))
            {
                foreach (var ring in rings)
                {
                    for (var index = 0; index < ring.Count; index++)
                    {
                        var a = ring[index];
                        var b = ring[(index + 1) % ring.Count];
                        var minX = (Math.Min(a.X, b.X) / 64) - 2;
                        var maxX = (Math.Max(a.X, b.X) / 64) + 2;
                        var minY = (Math.Min(a.Y, b.Y) / 64) - 2;
                        var maxY = (Math.Max(a.Y, b.Y) / 64) + 2;
                        for (var bx = Math.Max(0, minX) / Bucket; bx <= Math.Max(0, maxX) / Bucket; bx++)
                        {
                            for (var by = Math.Max(0, minY) / Bucket; by <= Math.Max(0, maxY) / Bucket; by++)
                            {
                                if (!samples.TryGetValue((bx, by), out var list))
                                {
                                    continue;
                                }

                                foreach (var (hex, x, y) in list)
                                {
                                    if (!reported.Contains(hex) && Distance(x, y, a, b) <= 1.0)
                                    {
                                        reported.Add(hex);
                                        findings.Add(new ValidationFinding("MAP-VAL-007", MapDiagnosticSeverity.Warning,
                                            $"{Article(FeatureDescriptions.Kind(feature))} {FeatureDescriptions.Kind(feature)} boundary passes within a pixel of a sample point of {geometry.NameOf(hex)}; a small edit could change its facts.",
                                            hex, feature.Id));
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }
    }

    /// <summary>The pixels the derivation samples for a hex: center and diagonal neighbors, 5-pixel probes, and edge samples.</summary>
    public static IEnumerable<GridPoint> SamplePoints(BoardGeometry geometry, HexIndex hex)
    {
        ArgumentNullException.ThrowIfNull(geometry);
        var center = geometry.CenterPoint(hex);
        yield return center;
        foreach (var (dx, dy) in new[] { (1, -1), (1, 1), (-1, 1), (-1, -1), (5, 0), (-5, 0), (0, 5), (0, -5) })
        {
            yield return new GridPoint(center.X + dx, center.Y + dy);
        }

        foreach (var side in HexsideDirections.All)
        {
            yield return geometry.EdgeSamplePoint(hex, side);
        }
    }

    private static string Article(string word) => "aeiou".Contains(word[0], StringComparison.Ordinal) ? "An" : "A";

    private static double Distance(double x, double y, FixedVector a, FixedVector b)
    {
        double ax = a.X / 64.0;
        double ay = a.Y / 64.0;
        double dx = (b.X / 64.0) - ax;
        double dy = (b.Y / 64.0) - ay;
        var lengthSquared = (dx * dx) + (dy * dy);
        var t = lengthSquared == 0 ? 0 : Math.Clamp((((x - ax) * dx) + ((y - ay) * dy)) / lengthSquared, 0, 1);
        var px = ax + (t * dx) - x;
        var py = ay + (t * dy) - y;
        return Math.Sqrt((px * px) + (py * py));
    }
}
