using System.Globalization;
using LimboDancer.Domains.Asl.Maps;
using LimboDancer.Domains.Asl.Maps.Composition;
using LimboDancer.Domains.Asl.Maps.Coordinates;
using LimboDancer.Domains.Asl.Maps.Rendering;
using LimboDancer.Domains.Asl.Units.State;

namespace LimboDancer.Domains.Asl.MapStudio.Services;

/// <summary>
/// A game's map for one viewer as the viewport's layers (pass 28c, R11): the board it is drawn from (null when it cannot be drawn), its name, the
/// <c>layer-units</c> group of the units the viewer may see, the <c>layer-los</c> group, the <c>layer-marks</c> group, the highlighted hex's points,
/// and why the map cannot be drawn.
/// </summary>
public sealed record GameMapLayers(StudioBoard? Board, string? Source, string? Units, string? Los, string? Marks, string? Highlight, IReadOnlyList<string> Problems);

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
    /// A live game's map at a revision for a viewer, as the layers the Play page's viewport draws (pass 28c, R11): the board to load, the units the
    /// viewer may see, the LOS line, the marks (the Covered Arc and the Residual FP counters in play, unit step 22), and the highlighted hex.
    /// </summary>
    public GameMapLayers Layers(string gameId, MapInPlay map, Perspective viewer, long revision, BoardLocation? highlight = null,
        Func<StudioBoard, string>? los = null, Func<StudioBoard, string>? marks = null, IReadOnlyList<ResidualFire>? residualFire = null)
    {
        ArgumentNullException.ThrowIfNull(map);
        ArgumentNullException.ThrowIfNull(viewer);
        var loaded = Board(map);
        if (loaded.Board is not { } board)
        {
            return new GameMapLayers(null, null, null, null, null, null, [.. loaded.Diagnostics.Select(item => $"{item.Code}: {item.Message}")]);
        }

        if (renders.Get(board, BoardView.Exact, "document", trace: false) is null)
        {
            return new GameMapLayers(null, board.Ref.Value, null, null, null, null, [$"{board.Ref} has nothing to draw in the Exact view."]);
        }

        var entry = games.Load(GameLibrary.LivePrefix + gameId);
        var unitLayer = entry.History is { HasErrors: false } && units.Renderer(UnitLibrary.DefaultSheet) is { } renderer
            ? units.Overlay(board, games.Projection(entry, viewer, revision).Set, renderer).Svg
            : null;

        string? points = null;
        if (highlight is not null && UnitLibrary.TargetFor(board).Locate(highlight) is { } hex)
        {
            points = string.Join(' ', board.Render.Grid.Geometry.Vertices(hex)
                .Select(point => string.Create(CultureInfo.InvariantCulture, $"{point.X:0.##},{point.Y:0.##}")));
        }

        // Residual FP is public (A8.2): every viewer sees each counter's value at the centre of its hex.
        var marked = new List<string>();
        if (marks is not null)
        {
            marked.Add(marks(board));
        }

        foreach (var residual in residualFire ?? [])
        {
            if (UnitLibrary.TargetFor(board).Locate(residual.Location) is { } at)
            {
                var vertices = board.Render.Grid.Geometry.Vertices(at).ToArray();
                var x = vertices.Average(point => point.X);
                var y = vertices.Average(point => point.Y);
                marked.Add(string.Create(CultureInfo.InvariantCulture,
                    $"<g class=\"play-residual\" data-location=\"{residual.Location}\" data-fp=\"{residual.Fp}\"><circle cx=\"{x:0.##}\" cy=\"{y:0.##}\" r=\"14\" fill=\"#fff3c4\" stroke=\"#a40\" stroke-width=\"2\"/>"
                    + $"<text x=\"{x:0.##}\" y=\"{y + 5:0.##}\" text-anchor=\"middle\" font-size=\"14\" font-weight=\"bold\" fill=\"#a40\">{residual.Fp}</text></g>"));
            }
        }

        return new GameMapLayers(board, board.Ref.Value, unitLayer, los?.Invoke(board), $"<g id=\"layer-marks\">{string.Concat(marked)}</g>", points, []);
    }

    /// <summary>The ground-level Location of the hex under a point of the map, in board units; null off the map (pass 28c).</summary>
    public static BoardLocation? LocationAt(StudioBoard board, double x, double y)
    {
        ArgumentNullException.ThrowIfNull(board);
        if (board.Render.Grid.Geometry.HexAt(x, y) is not { } index)
        {
            return null;
        }

        if (board.Composition is { } composition)
        {
            return composition.Map.OwnerOf(index) is { } owner ? new BoardLocation(owner.Board, owner.Hex, 0) : null;
        }

        return new BoardLocation(board.Ref, board.Facts[index].Hex, 0);
    }

    private static bool Same(IReadOnlyList<BoardPlacement> first, IReadOnlyList<BoardPlacement> second) =>
        first.Count == second.Count && first.All(placement => second.Any(other => other.Board == placement.Board && other.Column == placement.Column
            && other.Row == placement.Row && other.Reversed == placement.Reversed && placement.Rules.Count == 0));
}
