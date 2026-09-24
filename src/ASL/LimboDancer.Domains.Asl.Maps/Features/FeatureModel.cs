using LimboDancer.Domains.Asl.Maps.Coordinates;
using LimboDancer.Domains.Asl.Maps.Derivation;
using LimboDancer.Domains.Asl.Maps.Geometry;

namespace LimboDancer.Domains.Asl.Maps.Features;

/// <summary>A board-space point in 1/64 pixel units (Model Design section 3.5).</summary>
public readonly record struct FixedVector(int X, int Y)
{
    public static FixedVector FromPixels(int x, int y) => new(checked(x * FixedPoint.UnitsPerPixel), checked(y * FixedPoint.UnitsPerPixel));

    public static FixedVector FromExactPixels(double x, double y) => new(FixedPoint.FromExactPixels(x).Raw, FixedPoint.FromExactPixels(y).Raw);

    public override string ToString() => $"({FixedPoint.FromRaw(X)}, {FixedPoint.FromRaw(Y)})";
}

/// <summary>
/// A polygon with holes, filled under the nonzero winding rule (Model Design section 5.3). Holes run opposite to
/// their outer ring; the vectorizer's rings do so by construction.
/// </summary>
public sealed record FeatureShape(IReadOnlyList<IReadOnlyList<FixedVector>> Rings)
{
    public int VertexCount => Rings.Sum(ring => ring.Count);

    public static FeatureShape Rectangle(FixedVector topLeft, FixedVector bottomRight) =>
        new([[topLeft, new FixedVector(bottomRight.X, topLeft.Y), bottomRight, new FixedVector(topLeft.X, bottomRight.Y)]]);
}

/// <summary>One segment of a centerline: a line when both controls are null, otherwise a cubic Bézier.</summary>
public sealed record PathSegment(FixedVector End, FixedVector? Control1 = null, FixedVector? Control2 = null)
{
    public bool IsCurve => Control1 is not null && Control2 is not null;
}

/// <summary>A centerline path for linear terrain.</summary>
public sealed record CenterlinePath(FixedVector Start, IReadOnlyList<PathSegment> Segments);

/// <summary>
/// One hexside of a <see cref="HexsideTerrainFeature"/>: the canonical ref, the extent along the side in 1/64ths of
/// its length (0 to 64, the whole side by default), and an optional stroke width in pixels.
/// </summary>
public sealed record HexsideSpan(HexsideRef Side, int From = 0, int To = 64, int? Width = null);

/// <summary>Where a Feature Model came from.</summary>
public sealed record FeatureProvenance(string Kind, string? SourceBoard = null, string? SourceVersion = null, string? VectorizerVersion = null)
{
    public const string AuthoredKind = "authored";
    public const string VectorizedKind = "vectorized";

    public static FeatureProvenance Authored { get; } = new(AuthoredKind);

    public static FeatureProvenance Vectorized(string sourceBoard, string? sourceVersion, string vectorizerVersion) =>
        new(VectorizedKind, sourceBoard, sourceVersion, vectorizerVersion);
}

/// <summary>Facts VASL keeps outside the pixel grid: stairway flags per hex, and hexside flags (section 5.2).</summary>
public sealed record HexAnnotations(IReadOnlySet<HexIndex> Stairways, HexsideAnnotations Hexsides)
{
    public static HexAnnotations None { get; } = new(new HashSet<HexIndex>(), HexsideAnnotations.None);
}

/// <summary>
/// Common feature fields. Every feature has a stable id, a layer (paint order within its kind), and optional author
/// notes. Features are processed by (stage, layer, id), never by list order (section 5.4).
/// </summary>
public abstract record Feature(string Id, int Layer)
{
    public string? Notes
    {
        get; init;
    }

    /// <summary>The compiler stage that paints this kind (section 5.3).</summary>
    public abstract int Stage
    {
        get;
    }
}

/// <summary>Sets the elevation of covered pixels to <see cref="Level"/>.</summary>
public sealed record ElevationRegion(string Id, int Layer, FeatureShape Shape, int Level) : Feature(Id, Layer)
{
    public override int Stage => 2;
}

/// <summary>Area terrain such as woods, brush, grain, orchard, marsh, or water.</summary>
public sealed record AreaTerrainFeature(string Id, int Layer, FeatureShape Shape, byte Code) : Feature(Id, Layer)
{
    public override int Stage => 3;
}

/// <summary>
/// Linear terrain: a centerline stroked at <see cref="Width"/> with round joins and caps, or an explicit outline.
/// The vectorizer emits outlines (Model Design section 7.2, stage 4).
/// </summary>
public sealed record LinearTerrainFeature(string Id, int Layer, byte Code, CenterlinePath? Centerline, FixedPoint Width, FeatureShape? Outline)
    : Feature(Id, Layer)
{
    public override int Stage => 4;
}

public sealed record BridgeFeature(string Id, int Layer, FeatureShape Shape, byte Code) : Feature(Id, Layer)
{
    public override int Stage => 5;
}

/// <summary>A building's footprints. Hexes sharing a <see cref="BuildingId"/> form one building.</summary>
public sealed record BuildingFeature(string Id, int Layer, IReadOnlyList<FeatureShape> Footprints, byte Code, string BuildingId) : Feature(Id, Layer)
{
    public override int Stage => 6;
}

/// <summary>Hexside terrain such as walls, hedges, bocage, cliffs, and rowhouse walls, as hexside refs.</summary>
public sealed record HexsideTerrainFeature(string Id, int Layer, byte Code, IReadOnlyList<HexsideSpan> Spans) : Feature(Id, Layer)
{
    public override int Stage => 7;
}

/// <summary>
/// A few pixels painted last, with a code and optionally an elevation, to reproduce a VASL artifact that the
/// vectorized shapes cannot (section 7.4).
/// </summary>
public sealed record FidelityPin(string Id, int Layer, FeatureShape Shape, byte Code, int? Elevation) : Feature(Id, Layer)
{
    public override int Stage => 8;
}

/// <summary>
/// The editable authoring layer (Model Design section 5.1). It is immutable; edits produce a new model. Geometry is in
/// fixed point, so compiling it is deterministic.
/// </summary>
public sealed record FeatureModel(
    BoardGeometry Geometry,
    string CatalogHash,
    byte BaseCode,
    int BaseElevation,
    IReadOnlyList<Feature> Features,
    HexAnnotations Annotations,
    FeatureProvenance Provenance)
{
    /// <summary>A new board: base Open Ground (code 0) at level 0 and no features (section 12).</summary>
    public static FeatureModel New(BoardGeometry geometry, string catalogHash) =>
        new(geometry, catalogHash, 0, 0, [], HexAnnotations.None, FeatureProvenance.Authored);
}

/// <summary>Feature ids. Authored features get ULIDs; generated ones get deterministic ids so output is reproducible.</summary>
public static class FeatureIds
{
    private const string Alphabet = "0123456789ABCDEFGHJKMNPQRSTVWXYZ";

    /// <summary>A ULID: 48-bit millisecond timestamp and 80 random bits, in Crockford base 32.</summary>
    public static string NewUlid()
    {
        Span<byte> bytes = stackalloc byte[16];
        var time = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        for (var index = 5; index >= 0; index--)
        {
            bytes[index] = (byte)(time & 0xFF);
            time >>= 8;
        }

        System.Security.Cryptography.RandomNumberGenerator.Fill(bytes[6..]);
        return Encode(bytes);
    }

    /// <summary>A deterministic ULID-shaped id: zero timestamp, then a prefix byte and a sequence number.</summary>
    public static string Sequential(byte prefix, int sequence)
    {
        Span<byte> bytes = stackalloc byte[16];
        bytes[6] = prefix;
        for (var index = 0; index < 4; index++)
        {
            bytes[15 - index] = (byte)((sequence >> (8 * index)) & 0xFF);
        }

        return Encode(bytes);
    }

    private static string Encode(ReadOnlySpan<byte> bytes)
    {
        // 128 bits as 26 base-32 digits, most significant first; the first digit carries the top 3 bits.
        var high = System.Buffers.Binary.BinaryPrimitives.ReadUInt64BigEndian(bytes[..8]);
        var low = System.Buffers.Binary.BinaryPrimitives.ReadUInt64BigEndian(bytes[8..]);
        var value = new UInt128(high, low);
        var chars = new char[26];
        for (var index = 25; index >= 0; index--)
        {
            chars[index] = Alphabet[(int)(value & 31)];
            value >>= 5;
        }

        return new string(chars);
    }
}
