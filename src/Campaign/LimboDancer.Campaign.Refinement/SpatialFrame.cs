using LimboDancer.Domains.Asl.Maps.Features;
using LimboDancer.Domains.Asl.Maps.Geometry;

namespace LimboDancer.Campaign.Refinement;

public readonly record struct MetricPoint(long X, long Y);
public sealed record Crop(int Column, int Row, int Width = 33, int Height = 20)
{
    public BoardGeometry Geometry()
    {
        if (Column % 2 != 0 || Width is < 2 or > 65 || Height is < 1 or > 40)
            throw new ArgumentException("The pilot requires an even crop column and bounded board dimensions.");
        return BoardGeometry.Standard(Width, Height);
    }
}

/// <summary>World coordinates are integer millimetres. Crops never define a new grid origin.</summary>
public static class SpatialFrame
{
    public const string Version = "source250-asl40-v1";
    public const double ColumnMm = 34641.016151377546;
    public static MetricPoint Project(double u, double v, int rotation, double offsetX, double offsetY)
    {
        var x = u * 1130; var y = v * 3138;
        var p = rotation switch
        {
            0 => (x, y),
            90 => (3138 - y, x),
            180 => (1130 - x, 3138 - y),
            270 => (y, 1130 - x),
            _ => throw new ArgumentOutOfRangeException(nameof(rotation))
        };
        return new(Round((p.Item1 + offsetX) * 250000 / 113), Round((p.Item2 + offsetY) * 250000 / 113));
    }
    public static MetricPoint Center(int column, int row) =>
        new(Round(ColumnMm * column), checked(40000L * row - (((column % 2) + 2) % 2) * 20000L));
    public static FixedVector Pixel(MetricPoint p, Crop crop) => FixedVector.FromPixels(
        (p.X / ColumnMm - crop.Column) * 56.25,
        (p.Y / 40000.0 - crop.Row) * 64.5 + 32.25);
    public static MetricPoint Metric(FixedVector p, Crop crop) => new(
        Round((p.X / 64.0 / 56.25 + crop.Column) * ColumnMm),
        Round(((p.Y / 64.0 - 32.25) / 64.5 + crop.Row) * 40000));
    public static long Round(double value) => checked((long)Math.Round(value, MidpointRounding.AwayFromZero));
}
