using LimboDancer.Domains.Asl.Maps.Geometry;
using LimboDancer.Domains.Asl.Maps.Grid;

namespace LimboDancer.Domains.Asl.Maps.Composition;

/// <summary>
/// The terrain and elevation grids of a VASL map while it is being built (<c>Map.terrainGrid</c> and
/// <c>elevationGrid</c>): mutable, column-major like <see cref="TerrainGrid"/>, and zero until boards are copied in.
/// </summary>
public sealed class VaslMapGrid
{
    private readonly byte[] codes;
    private readonly sbyte[] elevations;

    public VaslMapGrid(BoardGeometry geometry)
    {
        ArgumentNullException.ThrowIfNull(geometry);
        Geometry = geometry;
        codes = new byte[checked(geometry.GridWidth * geometry.GridHeight)];
        elevations = new sbyte[codes.Length];
    }

    public BoardGeometry Geometry
    {
        get;
    }

    public int Width => Geometry.GridWidth;

    public int Height => Geometry.GridHeight;

    /// <summary><c>Map.onMap</c>.</summary>
    public bool Contains(int x, int y) => Geometry.ContainsCell(x, y);

    public byte CodeAt(int x, int y) => codes[Index(x, y)];

    public void SetCode(int x, int y, byte code) => codes[Index(x, y)] = code;

    public sbyte ElevationAt(int x, int y) => elevations[Index(x, y)];

    /// <summary><c>Map.setGridElevation</c>: the value is stored as a Java byte.</summary>
    public void SetElevation(int x, int y, int elevation) => elevations[Index(x, y)] = unchecked((sbyte)elevation);

    /// <summary>An immutable copy of the grids, with the given stairway flags.</summary>
    public TerrainGrid Snapshot(Func<HexIndex, bool>? stairway = null)
    {
        var flags = new bool[Geometry.HexCount];
        if (stairway is not null)
        {
            foreach (var hex in Geometry.Hexes())
            {
                flags[Geometry.HexOrdinal(hex)] = stairway(hex);
            }
        }

        return new TerrainGrid(Geometry, codes, elevations, flags);
    }

    private int Index(int x, int y)
    {
        if (!Geometry.ContainsCell(x, y))
        {
            throw new ArgumentOutOfRangeException(nameof(x), $"Cell ({x}, {y}) is outside the {Width} by {Height} grid.");
        }

        return (x * Height) + y;
    }
}

/// <summary><c>java.awt.geom.Ellipse2D.Double</c>: containment and integer bounds as Java computes them.</summary>
internal readonly record struct JavaEllipse(double X, double Y, double Width, double Height)
{
    public bool Contains(double x, double y)
    {
        if (Width <= 0.0 || Height <= 0.0)
        {
            return false;
        }

        var normalX = ((x - X) / Width) - 0.5;
        var normalY = ((y - Y) / Height) - 0.5;
        return (normalX * normalX) + (normalY * normalY) < 0.25;
    }

    // RectangularShape.getBounds
    public (int X, int Y, int Width, int Height) Bounds
    {
        get
        {
            var x1 = Math.Floor(X);
            var y1 = Math.Floor(Y);
            var x2 = Math.Ceiling(X + Width);
            var y2 = Math.Ceiling(Y + Height);
            return ((int)x1, (int)y1, (int)(x2 - x1), (int)(y2 - y1));
        }
    }
}
