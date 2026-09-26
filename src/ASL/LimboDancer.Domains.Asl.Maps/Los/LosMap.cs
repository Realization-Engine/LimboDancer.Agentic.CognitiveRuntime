using System.Collections.Concurrent;
using LimboDancer.Domains.Asl.Maps.Composition;
using LimboDancer.Domains.Asl.Maps.Coordinates;
using LimboDancer.Domains.Asl.Maps.Derivation;
using LimboDancer.Domains.Asl.Maps.Geometry;
using LimboDancer.Domains.Asl.Maps.Grid;
using LimboDancer.Domains.Asl.Maps.Read;
using LimboDancer.Domains.Asl.Maps.Terrain;

namespace LimboDancer.Domains.Asl.Maps.Los;

/// <summary>The outcome of preparing an LOS map: the map, or the diagnostics that stopped it.</summary>
public sealed record LosMapResult(LosMap? Map, IReadOnlyList<MapDiagnostic> Diagnostics)
{
    public bool Succeeded => Map is not null;
}

/// <summary>
/// What <see cref="LosCalculator"/> walks (LOS Design, section 5): a geometry, a terrain grid, the hex facts, and the
/// terrain catalog, of a single board or of a built map, with the per-hex geometry VASL's <c>Hex</c> keeps for LOS
/// (the LOS point, the extended border, the hexside points) and board-relative names for every hex.
/// </summary>
public sealed class LosMap
{
    private const int CacheCapacity = 16;

    private static readonly ConcurrentDictionary<string, LosMapResult> PlacedMaps = new(StringComparer.Ordinal);

    private readonly Func<BoardRef, HexName, HexIndex?> locate;
    private readonly Func<HexIndex, (BoardRef Board, HexName Hex)?> ownerOf;
    private readonly HexFacts[] facts;
    private readonly GridPoint[] centers;
    private readonly GridPoint[][] edges;

    private LosMap(TerrainGrid grid, HexFactSet hexFacts, TerrainCatalog catalog, bool isDefinitive,
        Func<BoardRef, HexName, HexIndex?> locate, Func<HexIndex, (BoardRef Board, HexName Hex)?> ownerOf)
    {
        ArgumentNullException.ThrowIfNull(grid);
        ArgumentNullException.ThrowIfNull(hexFacts);
        ArgumentNullException.ThrowIfNull(catalog);
        var geometry = grid.Geometry;
        if (hexFacts.Geometry.WidthInHexes != geometry.WidthInHexes || hexFacts.Geometry.HeightInHexes != geometry.HeightInHexes)
        {
            throw new ArgumentException("The hex facts are for another geometry than the grid.", nameof(hexFacts));
        }

        Geometry = geometry;
        Grid = grid;
        Facts = hexFacts;
        Catalog = catalog;
        IsDefinitive = isDefinitive;
        Locator = new VaslHexLocator(geometry, runtime: true);
        this.locate = locate;
        this.ownerOf = ownerOf;
        facts = new HexFacts[geometry.HexCount];
        centers = new GridPoint[geometry.HexCount];
        edges = new GridPoint[geometry.HexCount][];
        foreach (var hex in geometry.Hexes())
        {
            var ordinal = geometry.HexOrdinal(hex);
            facts[ordinal] = hexFacts[hex];
            centers[ordinal] = geometry.CenterPoint(hex);
            edges[ordinal] = [.. HexsideDirections.All.Select(side => geometry.EdgeSamplePoint(hex, side))];
        }
    }

    public BoardGeometry Geometry
    {
        get;
    }

    public TerrainGrid Grid
    {
        get;
    }

    public HexFactSet Facts
    {
        get;
    }

    public TerrainCatalog Catalog
    {
        get;
    }

    /// <summary>Whether every board of the map is Verified or AuthoredValid (ASL-MAP-044); otherwise answers are nondefinitive.</summary>
    public bool IsDefinitive
    {
        get;
    }

    /// <summary>VASL's runtime <c>Map.gridToHex</c> and the hexes' borders, for the map's geometry.</summary>
    public VaslHexLocator Locator
    {
        get;
    }

    /// <summary>An LOS map of one board, whose hexes are named on that board.</summary>
    public static LosMap ForGrid(BoardRef board, TerrainGrid grid, HexFactSet facts, TerrainCatalog catalog, bool isDefinitive = true)
    {
        ArgumentNullException.ThrowIfNull(board);
        ArgumentNullException.ThrowIfNull(grid);
        var geometry = grid.Geometry;
        return new LosMap(grid, facts, catalog, isDefinitive,
            (named, hex) => named == board && geometry.TryGetIndex(hex, out var index) ? index : null,
            index => geometry.Contains(index) ? (board, geometry.NameOf(index)) : null);
    }

    /// <summary>An LOS map of a built map, whose hexes are named by the board that owns them (step 12's owner rule).</summary>
    public static LosMap ForVaslMap(VaslMap map, TerrainCatalog catalog, bool isDefinitive = true)
    {
        ArgumentNullException.ThrowIfNull(map);
        return new LosMap(map.Grid, map.Facts, catalog, isDefinitive, map.Locate, map.OwnerOf);
    }

    /// <summary>
    /// The LOS map of a board handle, from its <see cref="BoardHandle.Los"/> data. A handle without LOS data is refused;
    /// a board that is not Verified or AuthoredValid gives nondefinitive answers.
    /// </summary>
    public static LosMapResult ForBoard(BoardHandle handle)
    {
        ArgumentNullException.ThrowIfNull(handle);
        if (Problem(handle) is { } problem)
        {
            return new LosMapResult(null, [problem]);
        }

        var los = handle.Los!;
        return new LosMapResult(ForGrid(handle.Ref, los.Grid, handle.Facts, los.Catalog, IsDefinitiveStatus(handle.Status)), []);
    }

    /// <summary>
    /// The LOS map of a placed map (step 12): built with <see cref="VaslMapBuilder"/> from the placed boards' LOS data,
    /// as VASL's runtime builds it, and cached by placement text and board versions. Every placed board must have LOS
    /// data and the same terrain catalog; the answers are definitive only when every board is.
    /// </summary>
    public static LosMapResult ForPlacedMap(ComposedMapRead read)
    {
        ArgumentNullException.ThrowIfNull(read);
        var placed = read.Layout.Boards.Select(board => (board.Placement, Handle: read.Board(board.Placement.Board)!)).ToArray();
        foreach (var (_, handle) in placed)
        {
            if (Problem(handle) is { } problem)
            {
                return new LosMapResult(null, [problem]);
            }
        }

        var key = string.Join(" ", placed.Select(item => $"{item.Placement}#{item.Handle.Version}#{item.Handle.Status}"));
        if (PlacedMaps.TryGetValue(key, out var cached))
        {
            return cached;
        }

        var first = placed[0].Handle.Los!;
        if (placed.Any(item => !SameCatalog(item.Handle.Los!.Catalog, first.Catalog)))
        {
            return new LosMapResult(null, [new MapDiagnostic("MAP-LOS-002", MapDiagnosticSeverity.Error,
                "The placed boards' LOS data use different terrain catalogs.")]);
        }

        var built = VaslMapBuilder.Build(
            [.. placed.Select(item => new PlacedBoard(item.Placement, item.Handle.Los!.Grid, item.Handle.Los.Annotations))], first.Catalog, first.Rules);
        var result = built.Map is { } map
            ? new LosMapResult(ForVaslMap(map, first.Catalog, placed.All(item => IsDefinitiveStatus(item.Handle.Status))), built.Diagnostics)
            : new LosMapResult(null, built.Diagnostics);
        if (PlacedMaps.Count >= CacheCapacity)
        {
            PlacedMaps.Clear();
        }

        PlacedMaps[key] = result;
        return result;
    }

    /// <summary>The map hex of a board hex, or null when the board or hex is not on the map.</summary>
    public HexIndex? Locate(BoardRef board, HexName hex) => locate(board, hex);

    /// <summary>The board and hex name that own a map hex, or null off the map.</summary>
    public (BoardRef Board, HexName Hex)? OwnerOf(HexIndex hex) => ownerOf(hex);

    /// <summary>The hex facts of a map hex.</summary>
    public HexFacts FactsOf(HexIndex hex) => facts[Geometry.HexOrdinal(hex)];

    /// <summary>The LOS point of the hex's center locations (<c>Hex.getHexCenter</c>); upper levels share it.</summary>
    public GridPoint LosPoint(HexIndex hex) => centers[Geometry.HexOrdinal(hex)];

    /// <summary>The hexside location's edge center point (<c>Location.getEdgeCenterPoint</c>).</summary>
    public GridPoint EdgePoint(HexIndex hex, HexsideDirection side) => edges[Geometry.HexOrdinal(hex)][(int)side];

    /// <summary>
    /// The hex location nearest a pixel (<c>Hex.getNearestLocation</c>): the hexside whose edge point is strictly
    /// closer than the center and every earlier hexside, among the hexsides on the map, or null for the center.
    /// </summary>
    public HexsideDirection? NearestLocation(HexIndex hex, int x, int y)
    {
        var ordinal = Geometry.HexOrdinal(hex);
        var center = centers[ordinal];
        var distance = Distance(x, y, center.X, center.Y);
        HexsideDirection? nearest = null;
        var hexsides = facts[ordinal].Hexsides;
        for (var side = 0; side < 6; side++)
        {
            if (!hexsides[side].OnMap)
            {
                continue;
            }

            var edge = edges[ordinal][side];
            var next = Distance(x, y, edge.X, edge.Y);
            if (next < distance)
            {
                distance = next;
                nearest = (HexsideDirection)side;
            }
        }

        return nearest;
    }

    /// <summary>The nearer of two hexsides to a pixel by edge point, the second on a tie (<c>Hex.getNearestHexside</c>).</summary>
    public HexsideDirection NearestHexside(HexIndex hex, int x, int y, HexsideDirection first, HexsideDirection second)
    {
        var ordinal = Geometry.HexOrdinal(hex);
        var one = edges[ordinal][(int)first];
        var two = edges[ordinal][(int)second];
        return Distance(x, y, one.X, one.Y) < Distance(x, y, two.X, two.Y) ? first : second;
    }

    /// <summary>
    /// The hexsides of a hex a line touches (<c>LOSStatus.getHexsideCrossed</c>), in ascending order: each edge of the
    /// extended border, from vertex n to vertex n + 1, tested against the line with <c>Line2D.intersectsLine</c>.
    /// </summary>
    public IReadOnlyList<HexsideDirection> HexsidesCrossed(HexIndex hex, GridPoint from, GridPoint to)
    {
        var border = Locator.ExtendedBorder(hex);
        var crossed = new List<HexsideDirection>(2);
        for (var side = 0; side < 6; side++)
        {
            var start = border[side];
            var end = border[(side + 1) % 6];
            if (LosLine.SegmentsIntersect(start.X, start.Y, end.X, end.Y, from.X, from.Y, to.X, to.Y))
            {
                crossed.Add((HexsideDirection)side);
            }
        }

        return crossed;
    }

    // Point2D.distance
    internal static double Distance(double x1, double y1, double x2, double y2)
    {
        var dx = x1 - x2;
        var dy = y1 - y2;
        return Math.Sqrt((dx * dx) + (dy * dy));
    }

    private static bool IsDefinitiveStatus(BoardReadStatus status) => status is BoardReadStatus.Verified or BoardReadStatus.AuthoredValid;

    private static MapDiagnostic? Problem(BoardHandle handle)
    {
        if (handle.Los is not { } los)
        {
            return new MapDiagnostic("MAP-LOS-001", MapDiagnosticSeverity.Error, $"{handle.Ref} has no LOS data, so LOS on it is unsupported.");
        }

        var geometry = los.Grid.Geometry;
        return geometry.WidthInHexes != handle.Geometry.WidthInHexes || geometry.HeightInHexes != handle.Geometry.HeightInHexes
            || geometry.GridWidth != handle.Geometry.GridWidth || geometry.GridHeight != handle.Geometry.GridHeight
            ? new MapDiagnostic("MAP-LOS-002", MapDiagnosticSeverity.Error, $"{handle.Ref}'s LOS grid does not match its hex geometry.")
            : null;
    }

    private static bool SameCatalog(TerrainCatalog first, TerrainCatalog second) =>
        ReferenceEquals(first, second) || first.Types.SequenceEqual(second.Types);
}
