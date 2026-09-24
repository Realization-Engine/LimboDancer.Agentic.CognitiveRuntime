using LimboDancer.Domains.Asl.Maps.Geometry;
using LimboDancer.Domains.Asl.Maps.Grid;

namespace LimboDancer.Domains.Asl.Maps.Outlines;

/// <summary>
/// A closed ring along pixel edges. Vertices are pixel corners, only where the boundary turns. Outer rings run
/// clockwise on screen (y down) and holes counterclockwise; each ring starts at its top-left vertex.
/// </summary>
public sealed record OutlineRing(IReadOnlyList<GridPoint> Vertices, bool IsHole);

/// <summary>One 4-connected component of cells sharing a terrain code and elevation.</summary>
public sealed record OutlineRegion(byte Code, sbyte Elevation, GridPoint AnchorCell, int CellCount, IReadOnlyList<OutlineRing> Rings)
{
    /// <summary>A stable id derived from content: code, elevation, and the component's first cell in column-major order.</summary>
    public string Id => $"r{Code}e{Elevation}x{AnchorCell.X}y{AnchorCell.Y}";
}

/// <summary>The lossless vector form of a <see cref="TerrainGrid"/> (Model Design section 4.4).</summary>
public sealed class GridOutlines
{
    public GridOutlines(BoardGeometry geometry, IReadOnlyList<OutlineRegion> regions)
    {
        ArgumentNullException.ThrowIfNull(geometry);
        ArgumentNullException.ThrowIfNull(regions);
        Geometry = geometry;
        Regions = regions;
    }

    public BoardGeometry Geometry
    {
        get;
    }

    /// <summary>Regions ordered by code, elevation, then anchor cell in column-major order.</summary>
    public IReadOnlyList<OutlineRegion> Regions
    {
        get;
    }

    public int VertexCount => Regions.Sum(region => region.Rings.Sum(ring => ring.Vertices.Count));
}
