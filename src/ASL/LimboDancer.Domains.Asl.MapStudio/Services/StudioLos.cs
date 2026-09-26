using System.Collections.Concurrent;
using System.Globalization;
using LimboDancer.Domains.Asl.Maps.Coordinates;
using LimboDancer.Domains.Asl.Maps.Geometry;
using LimboDancer.Domains.Asl.Maps.Los;
using LimboDancer.Domains.Asl.Maps.Vasl;

namespace LimboDancer.Domains.Asl.MapStudio.Services;

/// <summary>An LOS check in the Studio: the result, or why the locations could not be read, and the line's end points.</summary>
public sealed record LosCheck(string Source, string Target, LosResult? Result, string? Problem, GridPoint? From, GridPoint? To)
{
    /// <summary>The result in words: the status, range, hindrance total, and the reason or blocking hex, then the breakdown.</summary>
    public string Summary => Result is not { } result ? Problem ?? string.Empty
        : Breakdown.Length == 0 ? Outcome(result) : $"{Outcome(result)}. {Breakdown}";

    /// <summary>
    /// The hindrance breakdown as VASL keeps it (LOS Result Design, section 2): the largest hindrance at each range and
    /// the hex where the first was met; empty when there is none.
    /// </summary>
    public string Breakdown => Result is { Hindrances.Count: > 0 } result
        ? $"Hindrances {string.Join(", ", result.Hindrances.Select(item => $"{item.Value.ToString("0.#", CultureInfo.InvariantCulture)} at range {item.Range}"))}"
            + (result.FirstHindranceAt is { } first ? $"; first at {(first.Board is null || first.Hex is null ? first.ToString() : $"{first.Board}:{first.Hex}")}" : string.Empty)
        : string.Empty;

    private static string Outcome(LosResult result) =>
        result.Status switch
        {
            LosStatus.Clear => $"Clear, range {result.Range}, hindrance {result.Hindrance}",
            LosStatus.Blocked => $"Blocked at {result.BlockedAt}, range {result.Range}: {result.Reason}",
            LosStatus.Unsupported => $"Not reproduced yet: {result.Reason}",
            _ => $"Not definitive on an unverified board: {(result.IsBlocked == true ? $"blocked at {result.BlockedAt}: {result.Reason}" : "clear")}, range {result.Range}",
        };
}

/// <summary>
/// LOS in the Studio (LOS Design, sections 6 and 8): the read over a loaded board or composed map, the line drawn on it,
/// and the comparison with the LOS oracle fixtures. LOS reads terrain only, so every viewer may use it.
/// </summary>
public sealed class StudioLos(IBoardProvider boards, MapService maps, StudioOptions options)
{
    private readonly ConcurrentDictionary<(string Board, string Version), LosMap> cache = new();

    /// <summary>The LOS map of a board or composed map; definitive only when the board is Verified or AuthoredValid.</summary>
    public LosMap MapFor(StudioBoard board)
    {
        ArgumentNullException.ThrowIfNull(board);
        return cache.GetOrAdd((board.Ref.Value, board.Version), _ =>
        {
            // A composed map is as definitive as its boards, as LosMap.ForPlacedMap judges it: a map built from placements
            // that match no oracle scenario is only Ingested itself, but its terrain is its verified boards'.
            var definitive = board.Composition is { } placed
                ? placed.Placements.All(placement => boards.Load(placement.Board).Board?.Status is BoardStatus.Verified or BoardStatus.AuthoredValid)
                : board.Status is BoardStatus.Verified or BoardStatus.AuthoredValid;
            return board.Composition is { } composition
                ? LosMap.ForVaslMap(composition.Map, board.Catalog, definitive)
                : LosMap.ForGrid(board.Ref, board.Render.Grid, board.Facts, board.Catalog, definitive);
        });
    }

    /// <summary>
    /// Checks LOS between two locations such as <c>bd01:E4:0</c>, or a hexside location such as <c>bd01:E4:0/3</c> aimed
    /// at its LOS point or its auxiliary point; a board's own hex may omit the board.
    /// </summary>
    public LosCheck Check(StudioBoard board, string source, string target, LosAim sourceAim = LosAim.LosPoint)
    {
        ArgumentNullException.ThrowIfNull(board);
        if (!TryLocation(board, source, out var from) || !TryLocation(board, target, out var to))
        {
            return new LosCheck(source, target, null, "Give both locations as board:hex:level, such as bd01:E4:0.", null, null);
        }

        var map = MapFor(board);
        try
        {
            var result = LosCalculator.Check(map, from, sourceAim, to, LosAim.LosPoint);
            return new LosCheck(source, target, result, null, Point(board, map, from, sourceAim), Point(board, map, to, LosAim.LosPoint));
        }
        catch (ArgumentException exception)
        {
            return new LosCheck(source, target, null, exception.Message, null, null);
        }
    }

    /// <summary>
    /// The LOS layer drawn over the board: the line between the two LOS points, dashed when LOS is blocked from the
    /// blocking point on, and the blocking hex outlined.
    /// </summary>
    public string Layer(StudioBoard board, LosCheck check)
    {
        ArgumentNullException.ThrowIfNull(board);
        ArgumentNullException.ThrowIfNull(check);
        if (check is not { From: { } from, To: { } to, Result: { } result })
        {
            return "<g id=\"layer-los\"></g>";
        }

        static string Number(double value) => value.ToString("0.##", CultureInfo.InvariantCulture);
        var color = result.IsBlocked == true ? "#c00" : result.IsAnswered ? "#060" : "#666";
        var parts = new List<string>();
        if (result.IsBlocked == true && result.BlockedAt is { } at)
        {
            parts.Add($"<line class=\"los-line\" x1=\"{from.X}\" y1=\"{from.Y}\" x2=\"{at.Point.X}\" y2=\"{at.Point.Y}\" stroke=\"{color}\" stroke-width=\"3\"/>");
            parts.Add($"<line class=\"los-line-blocked\" x1=\"{at.Point.X}\" y1=\"{at.Point.Y}\" x2=\"{to.X}\" y2=\"{to.Y}\" stroke=\"{color}\" stroke-width=\"2\" stroke-dasharray=\"6 4\"/>");
            if (UnitLibrary.TargetFor(board) is { } target && at.Board is { } blockedBoard && at.Hex is { } blockedHex
                && target.Locate(new BoardLocation(blockedBoard, blockedHex, 0)) is { } hex)
            {
                var points = string.Join(' ', board.Render.Grid.Geometry.Vertices(hex).Select(point => $"{Number(point.X)},{Number(point.Y)}"));
                parts.Add($"<polygon id=\"los-blocked-hex\" points=\"{points}\" fill=\"none\" stroke=\"{color}\" stroke-width=\"3\"/>");
            }
        }
        else
        {
            parts.Add($"<line class=\"los-line\" x1=\"{from.X}\" y1=\"{from.Y}\" x2=\"{to.X}\" y2=\"{to.Y}\" stroke=\"{color}\" stroke-width=\"3\"/>");
        }

        parts.Add($"<circle cx=\"{from.X}\" cy=\"{from.Y}\" r=\"5\" fill=\"{color}\"/><circle cx=\"{to.X}\" cy=\"{to.Y}\" r=\"5\" fill=\"{color}\"/>");
        return $"<g id=\"layer-los\" data-status=\"{result.Status}\">{string.Concat(parts)}</g>";
    }

    // A center location's LOS point is its hex center; a hexside location's is vertex `side`, or the next vertex when
    // aimed at its auxiliary point, truncated as VASL's hexside locations are (Hex.createLocations).
    private static GridPoint Point(StudioBoard board, LosMap map, BoardLocation location, LosAim aim)
    {
        var hex = map.Locate(location.Board, location.Hex)!.Value;
        if (location.Side is not { } side)
        {
            return map.LosPoint(hex);
        }

        var vertices = board.Render.Grid.Geometry.Vertices(hex);
        var vertex = vertices[((int)side + (aim == LosAim.AuxiliaryPoint ? 1 : 0)) % 6];
        return new GridPoint((int)vertex.X, (int)vertex.Y);
    }

    /// <summary>The LOS oracle fixtures the Studio can find.</summary>
    public IReadOnlyList<string> Fixtures() => options.ResolveOracleFixtures() is { } oracle ? LosFidelity.Find(oracle) : [];

    /// <summary>Compares the read with one LOS fixture, on the fixture's board or its placed boards as the Studio loads them.</summary>
    public LosFidelityResult? Compare(string fixturePath)
    {
        var fixture = LosFidelity.Read(fixturePath);
        var loaded = fixture.Scenario is null ? boards.Load(fixture.Placements[0].Board) : maps.LoadPlacements(fixture.Placements);
        return loaded.Board is { } board ? LosFidelity.Compare(MapFor(board), fixture) : null;
    }

    private static bool TryLocation(StudioBoard board, string text, [System.Diagnostics.CodeAnalysis.NotNullWhen(true)] out BoardLocation? location)
    {
        var trimmed = text?.Trim() ?? string.Empty;
        if (BoardLocation.TryParse(trimmed, out location))
        {
            return true;
        }

        // On a single board, E4:0 or E4 names the board's own hex.
        var parts = trimmed.Split(':');
        return board.Composition is null && parts.Length is 1 or 2
            && BoardLocation.TryParse($"{board.Ref.Value}:{parts[0]}:{(parts.Length == 2 ? parts[1] : "0")}", out location);
    }
}
