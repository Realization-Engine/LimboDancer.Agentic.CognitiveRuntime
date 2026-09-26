using LimboDancer.Domains.Asl.Maps.Composition;
using LimboDancer.Domains.Asl.Maps.Coordinates;
using LimboDancer.Domains.Asl.Maps.Derivation;
using LimboDancer.Domains.Asl.Maps.Geometry;

namespace LimboDancer.Domains.Asl.Maps.Read;

/// <summary>
/// A hexside between two adjacent hexes, as each records it. Within a board the two records describe the same hexside
/// of one derivation; across a seam each board records its own edge, and <see cref="Agreed"/> is null unless they agree.
/// </summary>
public sealed record CrossedHexside(HexsideDirection Side, HexsideFacts? From, HexsideFacts? To, bool AcrossSeam)
{
    /// <summary>The hexside's facts when they are known: the mover's record within a board, or both records when they agree.</summary>
    public HexsideFacts? Agreed => !AcrossSeam ? From : From is not null && To is not null && Same(From, To) ? From : null;

    private static bool Same(HexsideFacts first, HexsideFacts second) =>
        first.Terrain?.Name == second.Terrain?.Name
        && first.HexsideTerrain?.Name == second.HexsideTerrain?.Name
        && first.DepressionTerrain?.Name == second.DepressionTerrain?.Name
        && first.Cliff == second.Cliff
        && first.Slope == second.Slope
        && first.RailroadEmbankment == second.RailroadEmbankment
        && first.PartialOrchard == second.PartialOrchard;
}

public sealed record ComposedMapReadResult(ComposedMapRead? Read, IReadOnlyList<MapDiagnostic> Diagnostics);

/// <summary>
/// The map read API over a composed map (ASL-MAP-080; Composed Maps Design, section 4): board-relative locations
/// resolved against each placed board's own handle, and neighbours, distances, and crossed hexsides measured on the
/// map, across a seam too. A hex two boards share is named by the board that owns it, and its facts count only when
/// both boards give the same base level and terrain. Nothing at a seam is guessed.
/// </summary>
public sealed class ComposedMapRead
{
    private readonly Dictionary<BoardRef, BoardHandle> boards;

    private ComposedMapRead(MapLayout layout, Dictionary<BoardRef, BoardHandle> boards)
    {
        Layout = layout;
        this.boards = boards;
    }

    public MapLayout Layout
    {
        get;
    }

    /// <summary>Lays out placed boards, each read at an exact version; a board may be placed only once.</summary>
    public static ComposedMapReadResult Create(IReadOnlyList<(BoardPlacement Placement, BoardHandle Board)> placed)
    {
        ArgumentNullException.ThrowIfNull(placed);
        var handles = new Dictionary<BoardRef, BoardHandle>();
        foreach (var (placement, board) in placed)
        {
            if (placement.Board != board.Ref)
            {
                return Fail("MAP-READ-003", $"{placement.Board} is placed, but the handle read is for {board.Ref}.");
            }

            if (!handles.TryAdd(board.Ref, board))
            {
                return Fail("MAP-READ-003", $"{board.Ref} is placed more than once; board-relative locations need each board once.");
            }
        }

        var layout = MapLayout.Create([.. placed.Select(item => (item.Placement, item.Board.Geometry))]);
        return layout.Layout is { } laid ? new ComposedMapReadResult(new ComposedMapRead(laid, handles), []) : new ComposedMapReadResult(null, layout.Diagnostics);
    }

    /// <summary>A placed board's handle, or null when the board is not placed.</summary>
    public BoardHandle? Board(BoardRef board) => boards.GetValueOrDefault(board);

    /// <summary>
    /// Resolves a location on its own board. A hex another board owns is refused, and a shared hex whose two boards
    /// disagree on its base level or its terrain at the location's level has no read.
    /// </summary>
    public LocationReadResult Resolve(BoardLocation location)
    {
        ArgumentNullException.ThrowIfNull(location);
        if (Board(location.Board) is not { } handle)
        {
            return Refuse("MAP-READ-002", $"{location.Board} is not placed on the map.");
        }

        var result = handle.Resolve(location);
        if (result.Read is not { } read || Layout.Locate(location.Board, location.Hex) is not { } index)
        {
            return result;
        }

        if (Layout.OwnerOf(index) is { } owner && owner != (location.Board, location.Hex))
        {
            return Refuse("MAP-READ-003", $"{location.Board}:{location.Hex} is a hex shared with {owner.Board}, which owns it as {owner.Board}:{owner.Hex}.");
        }

        foreach (var (board, hex) in Layout.Names(index).Where(name => name != (location.Board, location.Hex)))
        {
            var other = boards[board].HexFacts(hex);
            var level = other?.Locations.FirstOrDefault(item => item.Level == location.Level);
            if (other is null || level is null || other.BaseLevel != read.Hex.BaseLevel || level.Terrain?.Name != read.Level.Terrain?.Name)
            {
                return Refuse("MAP-READ-004",
                    $"{location} is a hex shared with {board}:{hex}, and the two boards do not give the same base level and terrain there.");
            }
        }

        return result;
    }

    /// <summary>
    /// The hex across a hexside, on the same board or across a seam, named by its owner; null off the map. The side is a
    /// direction on the map, which on a reversed board is the opposite of the same side on the board's own hexes.
    /// </summary>
    public (BoardRef Board, HexName Hex)? Neighbor(BoardRef board, HexName hex, HexsideDirection side) =>
        Layout.Locate(board, hex) is { } index && Layout.Geometry.Neighbor(index, side) is { } across ? Layout.OwnerOf(across) : null;

    /// <summary>The distance in hexes on the map between two board hexes, or null when either is not on the map.</summary>
    public int? Distance(BoardRef fromBoard, HexName from, BoardRef toBoard, HexName to) =>
        Layout.Locate(fromBoard, from) is { } source && Layout.Locate(toBoard, to) is { } target ? Layout.Geometry.Distance(source, target) : null;

    /// <summary>
    /// The hexside between two adjacent board hexes, as each hex records it; null when they are not adjacent. A hexside
    /// is across a seam when the two hexes are on different boards.
    /// </summary>
    public CrossedHexside? Crossed(BoardRef fromBoard, HexName from, BoardRef toBoard, HexName to)
    {
        if (Layout.Locate(fromBoard, from) is not { } source || Layout.Locate(toBoard, to) is not { } target)
        {
            return null;
        }

        foreach (var side in Enum.GetValues<HexsideDirection>())
        {
            if (Layout.Geometry.Neighbor(source, side) != target)
            {
                continue;
            }

            // A reversed board is rotated, so the map direction is the opposite one on that board's own hexes.
            var fromFacts = Side(fromBoard, from, side);
            var toFacts = Side(toBoard, to, side.Opposite());
            return new CrossedHexside(side, fromFacts, toFacts, fromBoard != toBoard);
        }

        return null;
    }

    private HexsideFacts? Side(BoardRef board, HexName hex, HexsideDirection mapSide)
    {
        var handle = boards[board];
        var reversed = Layout.Boards.First(item => item.Placement.Board == board).Placement.Reversed;
        var side = reversed ? mapSide.Opposite() : mapSide;
        return handle.HexFacts(hex)?.Hexsides.FirstOrDefault(item => item.Side == side);
    }

    private static ComposedMapReadResult Fail(string code, string message) =>
        new(null, [new MapDiagnostic(code, MapDiagnosticSeverity.Error, message)]);

    private static LocationReadResult Refuse(string code, string message) =>
        new(null, [new MapDiagnostic(code, MapDiagnosticSeverity.Error, message)]);
}
