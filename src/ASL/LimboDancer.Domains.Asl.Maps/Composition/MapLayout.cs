using LimboDancer.Domains.Asl.Maps.Coordinates;
using LimboDancer.Domains.Asl.Maps.Derivation;
using LimboDancer.Domains.Asl.Maps.Geometry;

namespace LimboDancer.Domains.Asl.Maps.Composition;

/// <summary>The outcome of laying out placed boards: the layout, or the diagnostics that stopped it.</summary>
public sealed record MapLayoutResult(MapLayout? Layout, IReadOnlyList<MapDiagnostic> Diagnostics)
{
    public bool Succeeded => Layout is not null;
}

/// <summary>
/// Where placed boards' hexes go in a map, from the boards' geometry alone (Composed Maps Design, section 3): the map
/// geometry, each board's position, the map hex of every board hex, and which board owns a hex that two boards share.
/// Boards abut on half hexes; a shared hex belongs to the board placed later, row by row and then column by column,
/// as VASL's runtime names it. <see cref="VaslMapBuilder"/> lays maps out with it, so a built map and a layout of the
/// same placements agree.
/// </summary>
public sealed class MapLayout
{
    // Map.createtheHexGrid lays out hexes only for these A1 center heights.
    private static readonly double[] LaidOutA1CenterY = [32.25, 32.235, -612.75, 97.1];

    private readonly Dictionary<(BoardRef Board, HexName Hex), HexIndex> locations = [];
    private readonly Dictionary<HexIndex, (BoardRef Board, HexName Hex)> owners = [];
    private readonly Dictionary<HexIndex, List<(BoardRef Board, HexName Hex)>> names = [];

    private MapLayout(BoardGeometry geometry, IReadOnlyList<PlacedBoardLayout> boards)
    {
        Geometry = geometry;
        Boards = boards;
        foreach (var board in boards)
        {
            foreach (var local in board.Geometry.Hexes())
            {
                var index = MapHex(board, local);
                var name = board.Geometry.NameOf(local);
                locations[(board.Placement.Board, name)] = index;

                // Later boards overwrite the names of shared edge hexes, so they own them.
                owners[index] = (board.Placement.Board, name);
                if (!names.TryGetValue(index, out var list))
                {
                    names[index] = list = [];
                }

                list.Add((board.Placement.Board, name));
            }
        }
    }

    /// <summary>The map's hex geometry.</summary>
    public BoardGeometry Geometry
    {
        get;
    }

    /// <summary>The placed boards in the order VASL adds them: row by row, left to right.</summary>
    public IReadOnlyList<PlacedBoardLayout> Boards
    {
        get;
    }

    /// <summary>
    /// Lays out placed boards as <c>ASLMap.buildVASLMap</c> does: slots, hex size, rows filled without gaps, and every
    /// board hex inside the map's hex grid (VASL-MAP-001 to 004).
    /// </summary>
    public static MapLayoutResult Create(IReadOnlyList<(BoardPlacement Placement, BoardGeometry Geometry)> boards)
    {
        ArgumentNullException.ThrowIfNull(boards);
        if (boards.Count == 0)
        {
            return Fail("VASL-MAP-001", "A map needs at least one board.");
        }

        // VASL adds boards in board-picker order: row by row, left to right.
        boards = [.. boards.OrderBy(board => board.Placement.Row).ThenBy(board => board.Placement.Column)];
        var slots = new HashSet<(int, int)>();
        foreach (var (placement, _) in boards)
        {
            if (placement.Column < 0 || placement.Row < 0 || !slots.Add((placement.Column, placement.Row)))
            {
                return Fail("VASL-MAP-001", $"{placement.Board} is placed in slot ({placement.Column}, {placement.Row}), which is negative or already taken.");
            }
        }

        var first = boards[0].Geometry;
        foreach (var (placement, geometry) in boards)
        {
            if (Math.Round(geometry.HexHeight, MidpointRounding.AwayFromZero) != Math.Round(first.HexHeight, MidpointRounding.AwayFromZero)
                || Math.Round(geometry.HexWidth, MidpointRounding.AwayFromZero) != Math.Round(first.HexWidth, MidpointRounding.AwayFromZero))
            {
                return Fail("VASL-MAP-002", $"{placement.Board} has a different hex size; VASL disables LOS for maps with multiple hex sizes.");
            }
        }

        foreach (var row in boards.GroupBy(board => board.Placement.Row))
        {
            var columns = row.Select(board => board.Placement.Column).Order().ToArray();
            if (columns.Length > VaslMapBuilder.MaxBoardsPerRow || !columns.SequenceEqual(Enumerable.Range(0, columns.Length)))
            {
                return Fail("VASL-MAP-003",
                    $"Row {row.Key} must fill slots 0 to {Math.Min(columns.Length, VaslMapBuilder.MaxBoardsPerRow) - 1} without gaps; VASL lays out at most {VaslMapBuilder.MaxBoardsPerRow} boards across.");
            }
        }

        var rowCount = boards.Max(board => board.Placement.Row) + 1;
        if (!Enumerable.Range(0, rowCount).All(row => boards.Any(board => board.Placement.Row == row)))
        {
            return Fail("VASL-MAP-003", "Map rows must be filled from row 0 without gaps.");
        }

        // A board's pixel position is the sum of the grid sizes before it in its row and above it.
        var positions = boards.Select(board =>
        {
            var x = boards.Where(other => other.Placement.Row == board.Placement.Row && other.Placement.Column < board.Placement.Column)
                .Sum(other => other.Geometry.GridWidth);
            var y = Enumerable.Range(0, board.Placement.Row)
                .Sum(row => boards.First(other => other.Placement.Row == row).Geometry.GridHeight);
            return (X: x, Y: y);
        }).ToArray();
        var gridWidth = boards.Select((board, index) => positions[index].X + board.Geometry.GridWidth).Max();
        var gridHeight = boards.Select((board, index) => positions[index].Y + board.Geometry.GridHeight).Max();

        // ASLMap.buildVASLMap: the width counts boards whose bounds move right; each after the first shares a column.
        var widthInHexes = boards.Where(board => board.Placement.Row == 0)
            .Sum(board => board.Placement.Column == 0 ? board.Geometry.WidthInHexes : board.Geometry.WidthInHexes - 1);
        var heightInHexes = (int)Math.Floor((gridHeight / first.HexHeight) + 0.5);
        if (!LaidOutA1CenterY.Contains(first.HexHeight / 2.0))
        {
            return Fail("VASL-MAP-004", $"VASL lays out no hexes for an A1 center at {first.HexHeight / 2.0}.");
        }

        var mapGeometry = BoardGeometry.Vasl(widthInHexes, heightInHexes, first.HexWidth, first.HexHeight, gridWidth, gridHeight);
        var locator = new VaslHexLocator(mapGeometry, runtime: true);
        var placed = new PlacedBoardLayout[boards.Count];
        for (var index = 0; index < boards.Count; index++)
        {
            var (x, y) = positions[index];
            if (locator.GridToHex(x, y) is not { } start)
            {
                return Fail("VASL-MAP-004", $"No map hex contains the top-left corner ({x}, {y}) of {boards[index].Placement.Board}.");
            }

            placed[index] = new PlacedBoardLayout(boards[index].Placement, boards[index].Geometry, x, y, start.Column, start.Row);
            foreach (var local in boards[index].Geometry.Hexes())
            {
                if (!mapGeometry.Contains(MapHex(placed[index], local)))
                {
                    return Fail("VASL-MAP-004", $"{boards[index].Placement.Board} hex {boards[index].Geometry.NameOf(local)} falls outside the map's hex grid.");
                }
            }
        }

        return new MapLayoutResult(new MapLayout(mapGeometry, placed), []);
    }

    /// <summary>The map hex a placed board's hex occupies; a shared edge hex answers for both boards.</summary>
    public HexIndex? Locate(BoardRef board, HexName hex) => locations.TryGetValue((board, hex), out var index) ? index : null;

    /// <summary>The board and hex name a map hex belongs to; a shared edge hex belongs to the board placed later.</summary>
    public (BoardRef Board, HexName Hex)? OwnerOf(HexIndex hex) => owners.TryGetValue(hex, out var owner) ? owner : null;

    /// <summary>Every board hex at a map hex, in the order the boards were added: one, or two for a shared half hex.</summary>
    public IReadOnlyList<(BoardRef Board, HexName Hex)> Names(HexIndex hex) => names.TryGetValue(hex, out var list) ? list : [];

    /// <summary>Whether a board hex is on the map under the name that owns it, and not a shared hex another board owns.</summary>
    public bool IsOwnerName(BoardRef board, HexName hex) => Locate(board, hex) is { } index && OwnerOf(index) == (board, hex);

    /// <summary>The map hex of a board hex: offset by the board's position, and rotated 180 degrees on a reversed board.</summary>
    public static HexIndex MapHex(PlacedBoardLayout board, HexIndex local)
    {
        ArgumentNullException.ThrowIfNull(board);
        if (!board.Placement.Reversed)
        {
            return new HexIndex(board.MapColumn + local.Column, board.MapRow + local.Row);
        }

        var column = board.Geometry.WidthInHexes - local.Column - 1;
        return new HexIndex(board.MapColumn + column, board.MapRow + board.Geometry.RowCount(local.Column) - local.Row - 1);
    }

    private static MapLayoutResult Fail(string code, string message) =>
        new(null, [new MapDiagnostic(code, MapDiagnosticSeverity.Error, message)]);
}
