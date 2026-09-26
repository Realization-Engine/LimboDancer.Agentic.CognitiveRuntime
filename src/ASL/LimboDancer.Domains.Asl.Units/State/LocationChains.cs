using LimboDancer.Domains.Asl.Maps.Coordinates;
using LimboDancer.Domains.Asl.Maps.Derivation;
using LimboDancer.Domains.Asl.Maps.Geometry;

namespace LimboDancer.Domains.Asl.Units.State;

/// <summary>
/// The derived location chain of each hex on the boards in play (ASL-UNIT-024; Map Model and Authoring Design,
/// section 4.3), with the exact board version it was derived from, so a position is checked against that version.
/// </summary>
public interface ILocationChains
{
    /// <summary>The board version the chains belong to; null when the board is not known.</summary>
    public string? Version(BoardRef board);

    /// <summary>The levels of a hex's locations, lowest first; null when the hex is not on the board.</summary>
    public IReadOnlyList<int>? Levels(BoardRef board, HexName hex);

    public bool HasBridge(BoardRef board, HexName hex);

    /// <summary>The board's hex geometry, which lays out a composed map; null when the board is not known.</summary>
    public BoardGeometry? Geometry(BoardRef board) => null;
}

/// <summary>Location chains from boards' derived hex facts.</summary>
public sealed class HexFactLocationChains : ILocationChains
{
    private readonly Dictionary<BoardRef, (string Version, HexFactSet Facts)> boards;

    public HexFactLocationChains(IEnumerable<(BoardRef Board, string Version, HexFactSet Facts)> boards)
    {
        ArgumentNullException.ThrowIfNull(boards);
        this.boards = boards.ToDictionary(board => board.Board, board => (board.Version, board.Facts));
    }

    public string? Version(BoardRef board) => boards.TryGetValue(board, out var entry) ? entry.Version : null;

    public IReadOnlyList<int>? Levels(BoardRef board, HexName hex) =>
        Facts(board, hex) is { } facts ? [.. facts.Locations.Select(location => location.Level)] : null;

    public bool HasBridge(BoardRef board, HexName hex) => Facts(board, hex)?.Bridge is not null;

    public BoardGeometry? Geometry(BoardRef board) => boards.TryGetValue(board, out var entry) ? entry.Facts.Geometry : null;

    private HexFacts? Facts(BoardRef board, HexName hex) =>
        boards.TryGetValue(board, out var entry) && entry.Facts.Geometry.TryGetIndex(hex, out var index) ? entry.Facts[index] : null;
}
