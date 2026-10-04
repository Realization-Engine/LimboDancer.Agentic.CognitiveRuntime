using System.Globalization;
using LimboDancer.Domains.Asl.Maps;
using LimboDancer.Domains.Asl.Maps.Composition;
using LimboDancer.Domains.Asl.Maps.Coordinates;
using LimboDancer.Domains.Asl.Maps.Derivation;
using LimboDancer.Domains.Asl.Maps.Geometry;
using LimboDancer.Domains.Asl.Maps.Rendering;
using LimboDancer.Domains.Asl.Units.Rendering;
using LimboDancer.Domains.Asl.Units.Documents;
using LimboDancer.Domains.Asl.Units.State;

namespace LimboDancer.Domains.Asl.MapStudio.Services;

/// <summary>
/// A game's map for one viewer as the viewport's layers (pass 28c, R11): the board it is drawn from (null when it cannot be drawn), its name, the
/// <c>layer-units</c> group of the units the viewer may see, the <c>layer-marks</c> group, and why the map cannot be drawn. Pass 29: the overlay
/// those units were drawn from and the view they were read for, which the board inspector reads; the workspace draws the picked hex and LOS itself.
/// </summary>
public sealed record GameMapLayers(StudioBoard? Board, string? Source, string? Units, string? Marks, IReadOnlyList<string> Problems)
{
    /// <summary>The units the viewer may see, as placed on the map; null when they cannot be drawn.</summary>
    public UnitOverlay? Overlay
    {
        get; init;
    }

    /// <summary>The view the units were read for.</summary>
    public GameView? View
    {
        get; init;
    }
}

/// <summary>
/// The map of a live game on the Play page (Composed Maps Design, section 8): its one board, its saved map, or a map
/// built from its placements without saving it, drawn in the Exact view with the units the viewer may see and an
/// optional highlighted location.
/// </summary>
public sealed class GameMaps(IBoardProvider boards, MapService maps, RenderCache renders, UnitLibrary units, GameLibrary games)
{
    /// <summary>The maps saved in Map Studio, whose placements a new game may copy.</summary>
    public IReadOnlyList<MapDefinition> Saved() => maps.List();

    /// <summary>The boards the library knows, by name, for a card's board inputs (ruling R28.2).</summary>
    public IReadOnlyList<string> BoardNames() => [.. boards.List().Select(board => board.Ref.Value)];

    /// <summary>A card's boards drawn as one map for the card editor (rulings R28.2, R28.3).</summary>
    public CardMaps Cards { get; } = new(maps);

    /// <summary>The board or map a game is played on, or the reasons it cannot be loaded.</summary>
    public BoardLoadResult Board(MapInPlay map)
    {
        ArgumentNullException.ThrowIfNull(map);
        if (map.IsPlaced)
        {
            var placements = map.Placements();
            return BoardRef.TryParse(map.Reference, out var saved) && saved.Kind == BoardRefKind.ComposedMap && maps.Find(saved) is { } definition
                && Same(definition.Placements, placements)
                ? maps.Load(saved)
                : maps.LoadPlacements(placements);
        }

        if (map.Boards.Count != 1)
        {
            return new BoardLoadResult(null, [new MapDiagnostic("STUDIO-MAP-003", MapDiagnosticSeverity.Error,
                "The game names several boards without placing them, so there is no map to draw.")]);
        }

        return boards.LoadVersion(map.Boards[0].Board, map.Boards[0].Version) is { } board
            ? new BoardLoadResult(board, [])
            : new BoardLoadResult(null, [new MapDiagnostic("STUDIO-MAP-003", MapDiagnosticSeverity.Error,
                $"{map.Boards[0].Board} version {map.Boards[0].Version} is not available.")]);
    }

    /// <summary>
    /// The map as both sides know it (pass 31c; the user, 2026-10-04; ruling R31c.3), for the hand-over screen, which both the side leaving and the
    /// side arriving read: each side's units as the other side's view holds them. A Known unit is drawn as its counter, a concealed one as its
    /// "?", and a hidden one not at all (A12.11, A12.3); before play starts a stack shows its top counter alone, and a side setting up now is not
    /// drawn (A2.9, A12.12; ruling R23.3). So the map shows nothing that either player could not already see on the board.
    /// </summary>
    public (StudioBoard? Board, string? Units) Public(string gameId, MapInPlay map, long revision)
    {
        ArgumentNullException.ThrowIfNull(gameId);
        ArgumentNullException.ThrowIfNull(map);
        var entry = games.Load(GameLibrary.LivePrefix + gameId);
        if (Board(map).Board is not { } board)
        {
            return (null, null);
        }

        if (entry.History is not { HasErrors: false } history || history.At(revision) is not { } state || units.Renderer(UnitLibrary.DefaultSheet) is not { } renderer)
        {
            return (board, null);
        }

        UnitPlacementSet? first = null;
        var seen = new List<UnitDocument>();
        foreach (var side in state.Sides)
        {
            var projection = games.Projection(entry, Perspective.Side(side.Id), revision);
            first ??= projection.Set;
            seen.AddRange(projection.Set.Units.Where(unit => unit.Side != side.Id));
        }

        return first is null ? (board, null) : (board, units.Overlay(board, first with
        {
            SetId = $"{first.SetId}-public",
            Units = seen,
            Hash = $"{first.Hash}-public-{revision}",
        }, renderer).Svg);
    }

    /// <summary>
    /// A live game's map at a revision for a viewer, as the layers the Play page's viewport draws (pass 28c, R11): the board to load, the units the
    /// viewer may see and the marks (the Covered Arc and the Residual FP counters in play, unit step 22).
    /// </summary>
    public GameMapLayers Layers(string gameId, MapInPlay map, Perspective viewer, long revision, Func<StudioBoard, string>? marks = null,
        IReadOnlyList<ResidualFire>? residualFire = null)
    {
        ArgumentNullException.ThrowIfNull(gameId);
        return Layers(games.Load(GameLibrary.LivePrefix + gameId), map, viewer, revision, marks, residualFire);
    }

    /// <summary>
    /// The same layers for a game already loaded (pass 31b): the Replay page draws one game at many revisions, and loads and replays it once.
    /// </summary>
    public GameMapLayers Layers(GameEntry entry, MapInPlay map, Perspective viewer, long revision, Func<StudioBoard, string>? marks = null,
        IReadOnlyList<ResidualFire>? residualFire = null)
    {
        ArgumentNullException.ThrowIfNull(entry);
        ArgumentNullException.ThrowIfNull(map);
        ArgumentNullException.ThrowIfNull(viewer);
        var loaded = Board(map);
        if (loaded.Board is not { } board)
        {
            return new GameMapLayers(null, null, null, null, [.. loaded.Diagnostics.Select(item => $"{item.Code}: {item.Message}")]);
        }

        if (renders.Get(board, BoardView.Exact, "document", trace: false) is null)
        {
            return new GameMapLayers(null, board.Ref.Value, null, null, [$"{board.Ref} has nothing to draw in the Exact view."]);
        }

        var projection = entry.History is { HasErrors: false } ? games.Projection(entry, viewer, revision) : null;
        var overlay = projection is not null && units.Renderer(UnitLibrary.DefaultSheet) is { } renderer ? units.Overlay(board, projection.Set, renderer) : null;

        // Residual FP is public (A8.2): every viewer sees each counter's value. Pass 31c (design D16; play test P-30): at the hex's upper
        // corner, small, and taking no click, so it never covers the counter in the hex or stands between a click and it.
        var marked = new List<string>();
        if (marks is not null)
        {
            marked.Add(marks(board));
        }

        foreach (var residual in residualFire ?? [])
        {
            if (UnitLibrary.TargetFor(board).Locate(residual.Location) is { } at)
            {
                var (x, y) = Corner(board, at, upper: true);
                marked.Add(string.Create(CultureInfo.InvariantCulture,
                    $"<g class=\"play-residual\" data-location=\"{residual.Location}\" data-fp=\"{residual.Fp}\" pointer-events=\"none\"><circle cx=\"{x:0.##}\" cy=\"{y:0.##}\" r=\"9\" fill=\"#fff3c4\" stroke=\"#a40\" stroke-width=\"2\"/>"
                    + $"<text x=\"{x:0.##}\" y=\"{y + 4:0.##}\" text-anchor=\"middle\" font-size=\"11\" font-weight=\"bold\" fill=\"#a40\">{residual.Fp}</text></g>"));
            }
        }

        // Pass 31c (design D16; play test P-27): a Location in Melee is marked on its hex, for a view that holds a unit of that Melee by name.
        if (projection is not null)
        {
            foreach (var melee in projection.View.Units.Where(unit => GameState.Condition(unit, Conditions.Melee) == ConditionState.True)
                .Select(unit => projection.View.Locations.TryGetValue(unit.Id, out var where) ? where.Location with { Level = 0 } : null).OfType<BoardLocation>().Distinct())
            {
                if (UnitLibrary.TargetFor(board).Locate(melee) is { } at)
                {
                    var (x, y) = Corner(board, at, upper: false);
                    marked.Add(string.Create(CultureInfo.InvariantCulture,
                        $"<g class=\"play-melee\" data-location=\"{melee}\" pointer-events=\"none\"><rect x=\"{x - 17:0.##}\" y=\"{y - 8:0.##}\" width=\"34\" height=\"13\" rx=\"3\" fill=\"#7f1d1d\" stroke=\"#ffffff\" stroke-width=\"1\"/>"
                        + $"<text x=\"{x:0.##}\" y=\"{y + 2:0.##}\" text-anchor=\"middle\" font-size=\"10\" font-weight=\"bold\" fill=\"#ffffff\">Melee</text></g>"));
                }
            }
        }

        return new GameMapLayers(board, board.Ref.Value, overlay?.Svg, $"<g id=\"layer-marks\">{string.Concat(marked)}</g>", [])
        {
            Overlay = overlay,
            View = projection?.View,
        };
    }

    /// <summary>A point inside a hex near its top or its foot, where a small mark stands clear of the counters at its centre.</summary>
    private static (double X, double Y) Corner(StudioBoard board, HexIndex hex, bool upper)
    {
        var vertices = board.Render.Grid.Geometry.Vertices(hex).ToArray();
        var (x, y) = (vertices.Average(point => point.X), vertices.Average(point => point.Y));
        var edge = upper ? vertices.Min(point => point.Y) : vertices.Max(point => point.Y);
        return (x, y + ((edge - y) * 0.72));
    }

    /// <summary>The ground-level Location of the hex under a point of the map, in board units; null off the map (pass 28c).</summary>
    public static BoardLocation? LocationAt(StudioBoard board, double x, double y)
    {
        ArgumentNullException.ThrowIfNull(board);
        return board.Render.Grid.Geometry.HexAt(x, y) is { } index ? LocationOf(board, index) : null;
    }

    /// <summary>The ground-level Location of a hex of the map, named on its own board (pass 29); null for a hex no board owns.</summary>
    public static BoardLocation? LocationOf(StudioBoard board, HexIndex index)
    {
        ArgumentNullException.ThrowIfNull(board);
        if (board.Composition is { } composition)
        {
            return composition.Map.OwnerOf(index) is { } owner ? new BoardLocation(owner.Board, owner.Hex, 0) : null;
        }

        return new BoardLocation(board.Ref, board.Facts[index].Hex, 0);
    }

    /// <summary>
    /// A hex outlined in the marks layer (pass 30: the setup's draft rows and the hexes that may take a non-OB "?"), with an optional short label at its
    /// centre; empty when the map has no such hex. The mark takes no pointer events, so a click reaches the hex and its counters.
    /// </summary>
    public static string HexMark(StudioBoard board, BoardLocation at, string css, string fill, string stroke, int width, bool dashed, string? label)
    {
        ArgumentNullException.ThrowIfNull(board);
        ArgumentNullException.ThrowIfNull(at);
        if (UnitLibrary.TargetFor(board).Locate(at with { Level = 0 }) is not { } hex)
        {
            return string.Empty;
        }

        var vertices = board.Render.Grid.Geometry.Vertices(hex).ToArray();
        var points = string.Join(' ', vertices.Select(vertex => string.Create(CultureInfo.InvariantCulture, $"{vertex.X:0.##},{vertex.Y:0.##}")));
        var x = vertices.Average(point => point.X);
        var y = vertices.Average(point => point.Y);
        var text = label is null ? string.Empty : string.Create(CultureInfo.InvariantCulture,
            $"<circle cx=\"{x:0.##}\" cy=\"{y:0.##}\" r=\"11\" fill=\"#ffffff\" stroke=\"{stroke}\" stroke-width=\"2\"/><text x=\"{x:0.##}\" y=\"{y + 5:0.##}\" text-anchor=\"middle\" font-size=\"14\" font-weight=\"bold\" fill=\"{stroke}\">{label}</text>");
        return $"<g class=\"{css}\" data-location=\"{at with { Level = 0 }}\" pointer-events=\"none\"><polygon points=\"{points}\" fill=\"{fill}\" fill-opacity=\"0.3\" stroke=\"{stroke}\" stroke-width=\"{width}\""
            + (dashed ? " stroke-dasharray=\"8 5\"" : string.Empty) + $"/>{text}</g>";
    }

    /// <summary>The map's facts of the hex holding a Location; null when the map has no such hex (pass 29).</summary>
    public static HexFacts? FactsAt(StudioBoard board, BoardLocation location)
    {
        ArgumentNullException.ThrowIfNull(board);
        ArgumentNullException.ThrowIfNull(location);
        return UnitLibrary.TargetFor(board).Locate(location with { Level = 0 }) is { } hex ? board.Facts[hex] : null;
    }

    private static bool Same(IReadOnlyList<BoardPlacement> first, IReadOnlyList<BoardPlacement> second) =>
        first.Count == second.Count && first.All(placement => second.Any(other => other.Board == placement.Board && other.Column == placement.Column
            && other.Row == placement.Row && other.Reversed == placement.Reversed && placement.Rules.Count == 0));
}
