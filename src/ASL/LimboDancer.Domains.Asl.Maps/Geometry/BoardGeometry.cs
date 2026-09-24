using LimboDancer.Domains.Asl.Maps.Coordinates;

namespace LimboDancer.Domains.Asl.Maps.Geometry;

/// <summary>
/// Hex grid geometry for one board, reproducing VASL's standard geomorphic layout under the runtime
/// configuration <c>HalfHexWidthLeftHexFullHeight</c> (VASL Board Ingestion Design, section 5).
/// </summary>
/// <remarks>
/// Methods that return <see cref="PixelPoint"/> or <see cref="GridPoint"/> values reproduce VASL's double
/// arithmetic, operation order, truncation, and edge clamping exactly, because hex-fact derivation samples
/// single pixels at those positions.
/// </remarks>
public sealed class BoardGeometry
{
    private const double StandardHexWidth = 1800.0 / 32.0;
    private const double StandardHexHeight = 645.0 / 10.0;

    // Java: Math.cos(Math.toRadians(30.0)); toRadians multiplies by PI / 180.
    private static readonly double Cos30 = Math.Cos(30.0 * (Math.PI / 180.0));

    private BoardGeometry(int widthInHexes, int heightInHexes, double hexWidth, double hexHeight,
        double a1CenterX, double a1CenterY, int gridWidth, int gridHeight)
    {
        WidthInHexes = widthInHexes;
        HeightInHexes = heightInHexes;
        HexWidth = hexWidth;
        HexHeight = hexHeight;
        A1CenterX = a1CenterX;
        A1CenterY = a1CenterY;
        GridWidth = gridWidth;
        GridHeight = gridHeight;
    }

    public int WidthInHexes
    {
        get;
    }

    /// <summary>Hexes in an even-indexed column. Odd-indexed columns hold one more (half hexes at both ends).</summary>
    public int HeightInHexes
    {
        get;
    }

    public double HexWidth
    {
        get;
    }

    public double HexHeight
    {
        get;
    }

    public double A1CenterX
    {
        get;
    }

    public double A1CenterY
    {
        get;
    }

    public int GridWidth
    {
        get;
    }

    public int GridHeight
    {
        get;
    }

    public int HexCount => Enumerable.Range(0, WidthInHexes).Sum(RowCount);

    /// <summary>
    /// The hex's position in VASL order (<see cref="Hexes"/>), which is also its position in the LOSData
    /// stairway section.
    /// </summary>
    public int HexOrdinal(HexIndex hex)
    {
        EnsureContains(hex);
        return (hex.Column * HeightInHexes) + (hex.Column / 2) + hex.Row;
    }

    /// <summary>The 33 by 10 geomorphic board: 1800 by 645 grid, hex 56.25 by 64.5, A1 center at (0, 32.25).</summary>
    public static BoardGeometry StandardGeomorphic { get; } = Standard(33, 10);

    /// <summary>Standard hex geometry for a board of the given size in hexes.</summary>
    public static BoardGeometry Standard(int widthInHexes, int heightInHexes)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(widthInHexes, 2);
        ArgumentOutOfRangeException.ThrowIfLessThan(heightInHexes, 1);
        var gridWidth = (int)Math.Ceiling((widthInHexes - 1) * StandardHexWidth);
        var gridHeight = (int)Math.Ceiling(heightInHexes * StandardHexHeight);
        return new BoardGeometry(widthInHexes, heightInHexes, StandardHexWidth, StandardHexHeight,
            0.0, StandardHexHeight / 2.0, gridWidth, gridHeight);
    }

    public int RowCount(int column)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(column);
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(column, WidthInHexes);
        return HeightInHexes + (column % 2);
    }

    public bool Contains(HexIndex hex) =>
        hex.Column >= 0 && hex.Column < WidthInHexes && hex.Row >= 0 && hex.Row < RowCount(hex.Column);

    public bool ContainsCell(int x, int y) => x >= 0 && y >= 0 && x < GridWidth && y < GridHeight;

    /// <summary>All hexes in VASL order: every row of column 0, then column 1, and so on.</summary>
    public IEnumerable<HexIndex> Hexes()
    {
        for (var column = 0; column < WidthInHexes; column++)
        {
            for (var row = 0; row < RowCount(column); row++)
            {
                yield return new HexIndex(column, row);
            }
        }
    }

    public HexName NameOf(HexIndex hex)
    {
        EnsureContains(hex);
        return new HexName(hex.Column, hex.Row + (hex.Column % 2 == 0 ? 1 : 0));
    }

    public bool TryGetIndex(HexName name, out HexIndex hex)
    {
        hex = new HexIndex(name.Column, name.RowNumber - (name.Column % 2 == 0 ? 1 : 0));
        return Contains(hex);
    }

    public HexIndex IndexOf(HexName name) =>
        TryGetIndex(name, out var hex) ? hex : throw new ArgumentOutOfRangeException(nameof(name), name, "Hex is not on this board.");

    /// <summary>VASL's center dot (<c>Map.getHexCenterPoint</c>), with its x clamp at the right edge.</summary>
    public PixelPoint CenterDot(HexIndex hex)
    {
        EnsureContains(hex);
        var x = A1CenterX + (HexWidth * hex.Column);
        var y = A1CenterY + (HexHeight * hex.Row) - (HexHeight / 2.0 * (hex.Column % 2));
        if (x >= GridWidth)
        {
            x = GridWidth - 1;
        }

        return new PixelPoint(x, y);
    }

    /// <summary>
    /// The hex whose center dot is nearest the board point, or null outside the grid. This is a UI hit test; hex-fact
    /// derivation never uses it.
    /// </summary>
    public HexIndex? HexAt(double x, double y)
    {
        if (x < 0 || y < 0 || x >= GridWidth || y >= GridHeight)
        {
            return null;
        }

        var approximateColumn = (int)Math.Round((x - A1CenterX) / HexWidth);
        HexIndex? nearest = null;
        var nearestDistance = double.MaxValue;
        for (var column = Math.Max(0, approximateColumn - 1); column <= Math.Min(WidthInHexes - 1, approximateColumn + 1); column++)
        {
            for (var row = 0; row < RowCount(column); row++)
            {
                var candidate = new HexIndex(column, row);
                var center = CenterDot(candidate);
                var distance = ((center.X - x) * (center.X - x)) + ((center.Y - y) * (center.Y - y));
                if (distance < nearestDistance)
                {
                    nearestDistance = distance;
                    nearest = candidate;
                }
            }
        }

        return nearest;
    }

    /// <summary>The integer center point VASL samples from (<c>Hex.getHexCenter</c>).</summary>
    public GridPoint CenterPoint(HexIndex hex)
    {
        var dot = CenterDot(hex);
        var fixedDot = FixMapEdgePoint(dot.X, dot.Y);
        return new GridPoint((int)fixedDot.X, (int)fixedDot.Y);
    }

    /// <summary>Vertices clockwise from the top-left one (<c>Hex.initHexNew</c>).</summary>
    public IReadOnlyList<PixelPoint> Vertices(HexIndex hex)
    {
        var dot = CenterDot(hex);
        var x = dot.X;
        var y = dot.Y;
        var hexside = HexWidth * 2.0 / 3.0;
        var verticalOffset = HexHeight / 2.0;
        return
        [
            FixMapEdgePoint((-hexside / 2.0) + x, -verticalOffset + y),
            FixMapEdgePoint((hexside / 2.0) + x, -verticalOffset + y),
            FixMapEdgePoint(hexside + x, y),
            FixMapEdgePoint((hexside / 2.0) + x, verticalOffset + y),
            FixMapEdgePoint((-hexside / 2.0) + x, verticalOffset + y),
            FixMapEdgePoint(-hexside + x, y),
        ];
    }

    /// <summary>The integer border polygon VASL uses for containment: vertices rounded half up (Java <c>Math.round</c>).</summary>
    public IReadOnlyList<GridPoint> Border(HexIndex hex) =>
        Vertices(hex).Select(vertex => new GridPoint(JavaRound(vertex.X), JavaRound(vertex.Y))).ToArray();

    /// <summary>
    /// The pixel VASL samples for a hexside: the edge midpoint moved one pixel toward the center, truncated to
    /// integers and edge-clamped. It can fall outside the grid on board-edge half hexes.
    /// </summary>
    public GridPoint EdgeSamplePoint(HexIndex hex, HexsideDirection side)
    {
        var dot = CenterDot(hex);
        var x = dot.X;
        var y = dot.Y;
        var verticalOffset = HexHeight / 2.0;
        var horizontalOffset = Cos30 * verticalOffset;
        var point = side switch
        {
            HexsideDirection.North => FixMapEdgePoint((int)x, (int)(-verticalOffset + y + 1.0)),
            HexsideDirection.NorthEast => FixMapEdgePoint((int)(horizontalOffset + x - 1), (int)((-verticalOffset / 2.0) + y + 1.0)),
            HexsideDirection.SouthEast => FixMapEdgePoint((int)(horizontalOffset + x - 1), (int)((verticalOffset / 2.0) + y - 1.0)),
            HexsideDirection.South => FixMapEdgePoint((int)x, (int)(verticalOffset + y - 1.0)),
            HexsideDirection.SouthWest => FixMapEdgePoint((int)(-horizontalOffset + x + 1), (int)((verticalOffset / 2.0) + y - 1.0)),
            HexsideDirection.NorthWest => FixMapEdgePoint((int)(-horizontalOffset + x + 1), (int)((-verticalOffset / 2.0) + y + 1.0)),
            _ => throw new ArgumentOutOfRangeException(nameof(side), side, "Unknown hexside."),
        };
        return new GridPoint((int)point.X, (int)point.Y);
    }

    /// <summary>The adjacent hex across a hexside, or null at the board edge (<c>Map.getAdjacentHex</c>).</summary>
    public HexIndex? Neighbor(HexIndex hex, HexsideDirection side)
    {
        EnsureContains(hex);
        var column = hex.Column;
        var row = hex.Row;
        var columnIsEven = column % 2 == 0;
        switch (side)
        {
            case HexsideDirection.North:
                row -= 1;
                break;
            case HexsideDirection.NorthEast:
                column += 1;
                row += columnIsEven ? 0 : -1;
                break;
            case HexsideDirection.SouthEast:
                column += 1;
                row += columnIsEven ? 1 : 0;
                break;
            case HexsideDirection.South:
                row += 1;
                break;
            case HexsideDirection.SouthWest:
                column -= 1;
                row += columnIsEven ? 1 : 0;
                break;
            case HexsideDirection.NorthWest:
                column -= 1;
                row += columnIsEven ? 0 : -1;
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(side), side, "Unknown hexside.");
        }

        var neighbor = new HexIndex(column, row);
        return Contains(neighbor) ? neighbor : null;
    }

    /// <summary>
    /// The canonical ref for a hexside: sides 3 to 5 are expressed from the neighbor across them with the
    /// opposite side (0 to 2). A board-edge hexside with no neighbor keeps its original ref.
    /// </summary>
    public HexsideRef Canonicalize(HexsideRef hexside)
    {
        if ((int)hexside.Side < 3)
        {
            EnsureContains(hexside.Hex);
            return hexside;
        }

        var neighbor = Neighbor(hexside.Hex, hexside.Side);
        return neighbor is { } across ? new HexsideRef(across, hexside.Side.Opposite()) : hexside;
    }

    /// <summary>Hex distance in hexes, reproducing VASL's <c>Map.range</c> for this configuration.</summary>
    public int Distance(HexIndex source, HexIndex target)
    {
        EnsureContains(source);
        EnsureContains(target);
        var directionX = target.Column > source.Column ? 1 : -1;
        var directionY = target.Row > source.Row ? 1 : -1;
        var range = 0;
        var currentRow = source.Row;
        var currentColumn = source.Column;
        while (currentColumn != target.Column)
        {
            if (currentRow != target.Row
                && ((currentColumn % 2 == 0 && directionY == 1) || (currentColumn % 2 == 1 && directionY == -1)))
            {
                currentRow += directionY;
            }

            currentColumn += directionX;
            range += 1;
        }

        if (currentRow != target.Row)
        {
            range += Math.Abs(target.Row - currentRow);
        }

        return range;
    }

    // Hex.fixMapEdgePoints: pulls points that are one or two pixels off the grid back onto its edge.
    private PixelPoint FixMapEdgePoint(double x, double y)
    {
        var newX = x == -1.0 ? 0.0 : x;
        var newY = y == -1.0 ? 0.0 : y;
        newX = (int)newX == GridWidth || (int)newX == (GridWidth + 1.0) ? GridWidth - 1.0 : newX;
        newY = (int)newY == GridHeight || (int)newY == (GridHeight + 1.0) ? GridHeight - 1.0 : newY;
        return new PixelPoint(newX, newY);
    }

    // Java Math.round(double): floor(value + 0.5), unlike .NET's default banker's rounding.
    private static int JavaRound(double value) => (int)Math.Floor(value + 0.5);

    private void EnsureContains(HexIndex hex)
    {
        if (!Contains(hex))
        {
            throw new ArgumentOutOfRangeException(nameof(hex), hex, "Hex is not on this board.");
        }
    }
}
