namespace LimboDancer.Domains.Asl.Maps.Geometry;

/// <summary>An integer grid cell position. It may lie outside the grid when VASL's geometry puts it there.</summary>
public readonly record struct GridPoint(int X, int Y);

/// <summary>A double-precision board position, used only where VASL's own arithmetic is reproduced.</summary>
public readonly record struct PixelPoint(double X, double Y);
