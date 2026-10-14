using System.Text.Json;
using LimboDancer.Domains.Asl.Maps.Coordinates;
using LimboDancer.Domains.Asl.Maps.Features;
using LimboDancer.Domains.Asl.Maps.Geometry;

namespace LimboDancer.Campaign.Refinement;

/// <summary>One existing-ASL authored board assembled from accepted tiles, without procedural regeneration.</summary>
public static class SituationAslBoard
{
    public const string Version = "goodwood-asl-board-1";
    // Keep one exterior guard, but remove every internal loading margin.
    public const int OriginColumn = -SituationCoverage.ColumnMargin;
    public const int OriginRow = -SituationCoverage.RowMargin;

    public static BoardPackage Assemble(PilotInputs inputs, SituationMap map, CancellationToken cancellationToken = default)
    {
        var manifest = map.Manifest;
        var addresses = new SituationAddresses(inputs, manifest.Seed);
        if (addresses.Cells.Any(c => c.Column < 0 || c.Row < 0))
            throw new InvalidDataException("This ASL board adapter requires the Goodwood nonnegative world origin.");
        var geometry = BoardGeometry.Standard(addresses.Cells.Max(c => c.Column) - OriginColumn + SituationCoverage.ColumnMargin + 1,
            addresses.Cells.Max(c => c.Row) - OriginRow + SituationCoverage.RowMargin + 1);
        var features = new Dictionary<string, (Feature Feature, byte[] Identity)>(StringComparer.Ordinal);
        foreach (var descriptor in manifest.Packages)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var tile = map.Tile(descriptor.Tile, cancellationToken);
            var model = tile.Package.Model;
            if (model.BaseCode != 0 || model.BaseElevation != 0 || model.CatalogHash != inputs.CatalogBlob ||
                model.Annotations.Stairways.Count != 0 || model.Annotations.Hexsides.Slopes.Count != 0 ||
                model.Annotations.Hexsides.RailroadEmbankments.Count != 0 || model.Annotations.Hexsides.PartialOrchards.Count != 0)
                throw new InvalidDataException("Unexpected Situation tile base terrain or annotations.");
            var dx = checked((descriptor.Crop.Column - OriginColumn) * 3600); // 56.25 pixels per column, in 1/64 pixels.
            var dy = checked((descriptor.Crop.Row - OriginRow) * 4128); // 64.5 pixels per row.
            foreach (var feature in model.Features)
            {
                var translated = Translate(feature, dx, dy);
                var identity = JsonSerializer.SerializeToUtf8Bytes(translated, translated.GetType());
                if (features.TryGetValue(feature.Id, out var existing))
                {
                    if (!existing.Identity.AsSpan().SequenceEqual(identity) && !SameQuantizedFeature(existing.Feature, translated))
                        throw new InvalidDataException("Overlapping tiles disagree about accepted feature " + feature.Id + ".");
                }
                else features.Add(feature.Id, (translated, identity));
            }
        }
        var combined = FeatureModel.New(geometry, inputs.CatalogBlob) with
        { Features = features.OrderBy(p => p.Key, StringComparer.Ordinal).Select(p => p.Value.Feature).ToArray() };
        var identityKey = BoardPackage.Hash(CanonicalJson.Serialize(new[] { Version, BoardPackage.Hash(JsonSerializer.SerializeToUtf8Bytes(manifest)) }));
        return new(BoardRef.Parse("ab-goodwood-full-" + identityKey), "Operation Goodwood", combined);
    }

    // Crop-relative floating-point conversion can round a shared metric point one 1/64-pixel unit apart.
    // Only this coordinate tolerance is allowed. All semantic fields and topology must match exactly.
    private static bool SameQuantizedFeature(Feature a, Feature b)
    {
        bool Point(FixedVector x, FixedVector y) => Math.Abs((long)x.X - y.X) <= 1 && Math.Abs((long)x.Y - y.Y) <= 1;
        bool Points(IReadOnlyList<FixedVector> x, IReadOnlyList<FixedVector> y) => x.Count == y.Count && x.Zip(y).All(p => Point(p.First, p.Second));
        bool Shape(FeatureShape x, FeatureShape y) => x.Rings.Count == y.Rings.Count && x.Rings.Zip(y.Rings).All(p => Points(p.First, p.Second));
        bool OptionalPoint(FixedVector? x, FixedVector? y) => x is { } p && y is { } q ? Point(p, q) : x == y;
        bool Path(CenterlinePath? x, CenterlinePath? y) => x is null || y is null ? x == y :
            Point(x.Start, y.Start) && x.Segments.Count == y.Segments.Count && x.Segments.Zip(y.Segments).All(p =>
                Point(p.First.End, p.Second.End) && OptionalPoint(p.First.Control1, p.Second.Control1) && OptionalPoint(p.First.Control2, p.Second.Control2));
        if (a.Id != b.Id || a.Layer != b.Layer || a.Notes != b.Notes) return false;
        return (a, b) switch
        {
            (ElevationRegion x, ElevationRegion y) => x.Level == y.Level && Shape(x.Shape, y.Shape),
            (AreaTerrainFeature x, AreaTerrainFeature y) => x.Code == y.Code && Shape(x.Shape, y.Shape),
            (BridgeFeature x, BridgeFeature y) => x.Code == y.Code && Shape(x.Shape, y.Shape),
            (BuildingFeature x, BuildingFeature y) => x.Code == y.Code && x.BuildingId == y.BuildingId &&
                x.Footprints.Count == y.Footprints.Count && x.Footprints.Zip(y.Footprints).All(p => Shape(p.First, p.Second)),
            (LinearTerrainFeature x, LinearTerrainFeature y) => x.Code == y.Code && x.Width == y.Width && Path(x.Centerline, y.Centerline) &&
                (x.Outline is null || y.Outline is null ? x.Outline == y.Outline : Shape(x.Outline, y.Outline)),
            _ => false
        };
    }

    private static Feature Translate(Feature feature, int dx, int dy)
    {
        FixedVector Point(FixedVector p) => new(checked(p.X + dx), checked(p.Y + dy));
        FeatureShape Shape(FeatureShape shape) => new(shape.Rings.Select(r => (IReadOnlyList<FixedVector>)r.Select(Point).ToArray()).ToArray());
        CenterlinePath Path(CenterlinePath path) => new(Point(path.Start), path.Segments.Select(s => new PathSegment(
            Point(s.End), s.Control1 is { } a ? Point(a) : null, s.Control2 is { } b ? Point(b) : null)).ToArray());
        return feature switch
        {
            ElevationRegion f => f with { Shape = Shape(f.Shape) },
            AreaTerrainFeature f => f with { Shape = Shape(f.Shape) },
            BuildingFeature f => f with { Footprints = f.Footprints.Select(Shape).ToArray() },
            BridgeFeature f => f with { Shape = Shape(f.Shape) },
            LinearTerrainFeature f => f with { Centerline = f.Centerline is { } p ? Path(p) : null,
                Outline = f.Outline is { } outline ? Shape(outline) : null },
            _ => throw new InvalidDataException("Unsupported accepted feature in full ASL board: " + feature.GetType().Name)
        };
    }
}
