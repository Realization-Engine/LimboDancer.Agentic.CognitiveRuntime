using System.Globalization;
using System.Text;
using LimboDancer.Domains.Asl.Maps.Composition;
using LimboDancer.Domains.Asl.Maps.Coordinates;
using LimboDancer.Domains.Asl.Maps.Geometry;
using LimboDancer.Domains.Asl.Play;

namespace LimboDancer.Domains.Asl.MapStudio.Services;

/// <summary>A hex of a card's map: its board and its name on that board.</summary>
public sealed record CardHex(string Board, string Hex)
{
    /// <summary>The hex as an exit condition names it (<c>bd02:I1</c>).</summary>
    public override string ToString() => $"{Board}:{Hex}";
}

/// <summary>
/// A card's boards drawn as one map for the card editor (pass 28, rulings R28.2 and R28.3): composed from the card's placements as a game's map is,
/// the hex a click lands on, the whole building a hex belongs to, and the checks of a card's areas and hexes against the boards.
/// </summary>
public sealed class CardMaps(MapService maps)
{
    /// <summary>The card's boards as a map, or why they cannot be drawn.</summary>
    public (StudioBoard? Board, IReadOnlyList<string> Problems) Load(IReadOnlyList<ScenarioCardBoard> boards)
    {
        ArgumentNullException.ThrowIfNull(boards);
        var placements = new List<BoardPlacement>();
        foreach (var board in boards)
        {
            if (!BoardRef.TryParse(board.Board, out var reference) || board.Column < 0 || board.Row < 0)
            {
                return (null, [$"'{board.Board}' is not a board in a slot, so the map cannot be drawn"]);
            }

            placements.Add(new BoardPlacement(reference, board.Column, board.Row, board.Reversed, []));
        }

        if (placements.Count == 0)
        {
            return (null, ["the card names no boards"]);
        }

        var loaded = maps.LoadPlacements(placements);
        return loaded.Board is { Composition: not null } map ? (map, []) : (null, [.. loaded.Diagnostics.Select(diagnostic => diagnostic.Message)]);
    }

    /// <summary>The hex under a point of the drawn map, with its board.</summary>
    public static CardHex? HexAt(StudioBoard map, double x, double y)
    {
        ArgumentNullException.ThrowIfNull(map);
        return map.Render.Grid.Geometry.HexAt(x, y) is { } index && map.Composition?.Map.OwnerOf(index) is { } owner
            ? new CardHex(owner.Board.Value, owner.Hex.ToString())
            : null;
    }

    /// <summary>The hexes of the building a hex is in, the hex first (ruling R17.8): joined across hexsides of building terrain; empty when the hex is no building.</summary>
    public static IReadOnlyList<string> Building(StudioBoard map, CardHex start)
    {
        ArgumentNullException.ThrowIfNull(map);
        ArgumentNullException.ThrowIfNull(start);
        if (Index(map, start) is not { } first || map.Facts[first].Center.Terrain is not { IsBuilding: true })
        {
            return [];
        }

        var geometry = map.Facts.Geometry;
        var found = new HashSet<HexIndex> { first };
        var open = new Stack<HexIndex>([first]);
        while (open.TryPop(out var hex))
        {
            foreach (var side in map.Facts[hex].Hexsides.Where(side => side.OnMap && side.Terrain is { IsBuilding: true }))
            {
                if (geometry.Neighbor(hex, side.Side) is { } next && map.Facts[next].Center.Terrain is { IsBuilding: true }
                    && map.Composition!.Map.OwnerOf(next)?.Board.Value == start.Board && found.Add(next))
                {
                    open.Push(next);
                }
            }
        }

        return [start.Hex, .. found.Where(hex => hex != first).Select(hex => map.Composition!.Map.OwnerOf(hex)!.Value.Hex.ToString()).Order(StringComparer.Ordinal)];
    }

    /// <summary>Whether a hex lies on an edge of the map (top, bottom, left, or right): no hex beyond it on that side.</summary>
    public static bool OnEdge(StudioBoard map, CardHex hex, string edge)
    {
        ArgumentNullException.ThrowIfNull(map);
        if (Index(map, hex) is not { } index)
        {
            return false;
        }

        HexsideDirection[] sides = edge switch
        {
            "top" => [HexsideDirection.North],
            "bottom" => [HexsideDirection.South],
            "left" => [HexsideDirection.NorthWest, HexsideDirection.SouthWest],
            "right" => [HexsideDirection.NorthEast, HexsideDirection.SouthEast],
            _ => [],
        };
        return sides.Any(side => map.Facts.Geometry.Neighbor(index, side) is null);
    }

    /// <summary>
    /// The card's areas and hexes checked against its drawn map (ruling R28.3): every hex named is on its board, a building area is one whole building,
    /// an entry's hexes lie on its edge, and an exit condition's hexes are on the map. Each problem is a code and a plain message.
    /// </summary>
    public static IReadOnlyList<string> Check(ScenarioCard card, StudioBoard map)
    {
        ArgumentNullException.ThrowIfNull(card);
        ArgumentNullException.ThrowIfNull(map);
        var problems = new List<string>();
        string BoardOf(string? board) => board ?? card.Boards[0].Board;
        foreach (var group in card.Sides.SelectMany(side => side.Groups))
        {
            foreach (var area in group.Areas.Where(area => area.Hexes is { Count: > 0 }))
            {
                var board = BoardOf(area.Board);
                var missing = area.Hexes!.Where(hex => Index(map, new CardHex(board, hex)) is null).ToArray();
                if (missing.Length > 0)
                {
                    problems.Add($"card.setup: '{group.Name}' area '{area.Id}' names {string.Join(", ", missing)}, not hexes of {board}");
                    continue;
                }

                if (area.Kind == "building")
                {
                    var building = Building(map, new CardHex(board, area.Id));
                    if (building.Count == 0)
                    {
                        problems.Add($"card.setup: '{group.Name}' area '{area.Id}': {board} {area.Id} is not a building hex (ruling R17.8)");
                    }
                    else if (!building.Order(StringComparer.Ordinal).SequenceEqual(area.Hexes!.Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal), StringComparer.Ordinal))
                    {
                        problems.Add($"card.setup: '{group.Name}' area '{area.Id}' is not one whole building: the building at {area.Id} is {string.Join(" ", building)} (ruling R17.8)");
                    }
                }
                else if (area.Kind == "entry" && area.Hexes!.Where(hex => !OnEdge(map, new CardHex(board, hex), area.Edge ?? string.Empty)).ToArray() is { Length: > 0 } inland)
                {
                    problems.Add($"card.setup: '{group.Name}' entry '{area.Id}' names {string.Join(", ", inland)}, which are not on the {area.Edge} edge (A2.5)");
                }
            }
        }

        foreach (var condition in (card.VictoryConditions.Outcomes ?? []).SelectMany(outcome => outcome.Any).Where(condition => condition.Near is { Count: > 0 }))
        {
            var off = condition.Near!.Where(near => Parse(near) is not { } hex || Index(map, hex) is null).ToArray();
            if (off.Length > 0)
            {
                problems.Add($"card.victory: the exit condition of {condition.Side} names {string.Join(", ", off)}, which are not hexes of the card's boards");
            }
        }

        return problems;
    }

    /// <summary>A hex given as <c>bd02:I1</c>.</summary>
    public static CardHex? Parse(string text) => text.Split(':') is [var board, var hex] && board.Length > 0 && hex.Length > 0 ? new CardHex(board, hex) : null;

    /// <summary>The outlines of hexes as an overlay layer for the drawn map; null when there are none.</summary>
    public static string? Marks(StudioBoard map, IEnumerable<CardHex> hexes)
    {
        ArgumentNullException.ThrowIfNull(map);
        var markup = new StringBuilder();
        foreach (var hex in hexes)
        {
            if (Index(map, hex) is not { } index)
            {
                continue;
            }

            var points = string.Join(' ', map.Render.Grid.Geometry.Vertices(index).Select(vertex => string.Create(CultureInfo.InvariantCulture, $"{vertex.X:0.##},{vertex.Y:0.##}")));
            markup.Append(CultureInfo.InvariantCulture, $"<polygon class=\"card-picked\" data-hex=\"{hex}\" points=\"{points}\" fill=\"#d0a000\" fill-opacity=\"0.35\" stroke=\"#a06000\" stroke-width=\"3\"/>");
        }

        return markup.Length == 0 ? null : $"<g id=\"layer-los\">{markup}</g>";
    }

    private static HexIndex? Index(StudioBoard map, CardHex hex) =>
        BoardRef.TryParse(hex.Board, out var board) && HexName.TryParse(hex.Hex, out var name) ? map.Composition?.Map.Locate(board, name) : null;
}
