using System.Globalization;
using LimboDancer.Domains.Asl.Maps;
using LimboDancer.Domains.Asl.Maps.Composition;
using LimboDancer.Domains.Asl.Maps.Coordinates;
using LimboDancer.Domains.Asl.Maps.Rendering;
using LimboDancer.Domains.Asl.Units.State;

namespace LimboDancer.Domains.Asl.MapStudio.Services;

/// <summary>A game's map drawn for one viewer: the SVG, the board or map it was drawn from, and why it could not be drawn.</summary>
public sealed record GameMapDrawing(string? Svg, string? Source, IReadOnlyList<string> Problems);

/// <summary>
/// The map of a live game on the Play page (Composed Maps Design, section 8): its one board, its saved map, or a map
/// built from its placements without saving it, drawn in the Exact view with the units the viewer may see and an
/// optional highlighted location.
/// </summary>
public sealed class GameMaps(IBoardProvider boards, MapService maps, RenderCache renders, UnitLibrary units, GameLibrary games)
{
    /// <summary>The maps saved in Map Studio, whose placements a new game may copy.</summary>
    public IReadOnlyList<MapDefinition> Saved() => maps.List();

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

    /// <summary>Draws a live game at a revision for a viewer, with a location highlighted when given.</summary>
    public GameMapDrawing Draw(string gameId, MapInPlay map, Perspective viewer, long revision, BoardLocation? highlight = null)
    {
        ArgumentNullException.ThrowIfNull(map);
        ArgumentNullException.ThrowIfNull(viewer);
        var loaded = Board(map);
        if (loaded.Board is not { } board)
        {
            return new GameMapDrawing(null, null, [.. loaded.Diagnostics.Select(item => $"{item.Code}: {item.Message}")]);
        }

        if (renders.Get(board, BoardView.Exact, "document", trace: false) is not { } rendered)
        {
            return new GameMapDrawing(null, board.Ref.Value, [$"{board.Ref} has nothing to draw in the Exact view."]);
        }

        var entry = games.Load(GameLibrary.LivePrefix + gameId);
        var layers = new List<string>();
        if (entry.History is { HasErrors: false } && units.Renderer(UnitLibrary.DefaultSheet) is { } renderer)
        {
            layers.Add(units.Overlay(board, games.Projection(entry, viewer, revision).Set, renderer).Svg);
        }

        if (highlight is not null && UnitLibrary.TargetFor(board).Locate(highlight) is { } hex)
        {
            var points = string.Join(' ', board.Render.Grid.Geometry.Vertices(hex)
                .Select(point => string.Create(CultureInfo.InvariantCulture, $"{point.X:0.##},{point.Y:0.##}")));
            layers.Add($"<polygon id=\"play-highlight\" class=\"play-highlight\" points=\"{points}\" fill=\"none\" stroke=\"#d00\" stroke-width=\"4\"/>");
        }

        var svg = rendered.Svg;
        var end = svg.LastIndexOf("</svg>", StringComparison.Ordinal);
        return new GameMapDrawing(end < 0 ? svg : svg[..end] + string.Concat(layers) + svg[end..], board.Ref.Value, []);
    }

    private static bool Same(IReadOnlyList<BoardPlacement> first, IReadOnlyList<BoardPlacement> second) =>
        first.Count == second.Count && first.All(placement => second.Any(other => other.Board == placement.Board && other.Column == placement.Column
            && other.Row == placement.Row && other.Reversed == placement.Reversed && placement.Rules.Count == 0));
}
