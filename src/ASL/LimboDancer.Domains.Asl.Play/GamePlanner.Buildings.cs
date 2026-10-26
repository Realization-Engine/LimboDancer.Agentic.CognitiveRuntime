using System.Runtime.CompilerServices;
using LimboDancer.Domains.Asl.Maps.Coordinates;
using LimboDancer.Domains.Asl.Maps.Derivation;
using LimboDancer.Domains.Asl.Maps.Geometry;
using LimboDancer.Domains.Asl.Maps.Read;
using LimboDancer.Domains.Asl.Rules;
using LimboDancer.Domains.Asl.Units.State;

namespace LimboDancer.Domains.Asl.Play;

/// <summary>
/// The buildings of a map, found from its hex facts as the card editor finds one (ruling R17.8; pass 70, ruling R70.25): from a hex of building terrain,
/// across each hexside into a hex of building terrain where Rules says the building goes on (<see cref="ScenarioA1RoutCalculator.BuildingGoesOn"/>: no
/// Rowhouse wall, Breach, or Factory outside wall divides them). Rubble has no building terrain, so a rubbled hex is in none.
/// </summary>
public static class MapBuildings
{
    /// <summary>
    /// The hexes of the building a hex is in, the hex first: joined across hexsides within the hexes <paramref name="within"/> admits (one board's, or a
    /// footprint's); empty when the hex is no building. The Studio's card editor and the game's map read this one fill.
    /// </summary>
    public static IReadOnlyList<HexIndex> Building(HexFactSet facts, HexIndex start, Func<HexIndex, bool> within)
    {
        ArgumentNullException.ThrowIfNull(facts);
        ArgumentNullException.ThrowIfNull(within);
        if (facts[start].Center.Terrain is not { IsBuilding: true })
        {
            return [];
        }

        var geometry = facts.Geometry;
        List<HexIndex> found = [start];
        var seen = new HashSet<HexIndex> { start };
        var open = new Stack<HexIndex>([start]);
        while (open.TryPop(out var hex))
        {
            foreach (var side in facts[hex].Hexsides.Where(side => side.OnMap))
            {
                if (geometry.Neighbor(hex, side.Side) is { } next && !seen.Contains(next) && within(next)
                    && ScenarioA1RoutCalculator.BuildingGoesOn(side.Terrain?.IsBuilding == true, facts[next].Center.Terrain?.IsBuilding == true, side.Terrain?.Name,
                        side.Terrain?.IsOutsideFactoryWall == true, side.HexsideTerrain?.Name, side.HexsideTerrain?.IsOutsideFactoryWall == true))
                {
                    seen.Add(next);
                    found.Add(next);
                    open.Push(next);
                }
            }
        }

        return found;
    }
}

/// <summary>
/// A building of the game's map as the rout, Mopping Up, and Searching read it (pass 70, ruling R70.25): the map's buildings are found once per board handle
/// and kept with it; a building a card names keeps the card's id and hexes, so older records keep theirs, and any other is named by its board and lowest hex.
/// </summary>
public sealed partial class GamePlanner
{
    /// <summary>
    /// A board's buildings (ruling R70.25): each building hex's building, and each building's ground-level Locations with the lowest hex first and its id,
    /// "board:lowest hex". Read only; it depends on the board's facts alone.
    /// </summary>
    private sealed class BoardBuildings(Dictionary<HexName, int> of, BoardLocation[][] hexes, string[] ids)
    {
        public Dictionary<HexName, int> Of { get; } = of;

        public BoardLocation[][] Hexes { get; } = hexes;

        public string[] Ids { get; } = ids;

        public static BoardBuildings Read(BoardHandle handle)
        {
            var geometry = handle.Geometry;
            var of = new Dictionary<HexName, int>();
            var hexes = new List<BoardLocation[]>();
            var ids = new List<string>();
            foreach (var hex in handle.Facts.Hexes.Where(item => item.Center.Terrain is { IsBuilding: true } && handle.ContainsLocation(item.Hex))
                .OrderBy(item => item.Hex.Column).ThenBy(item => item.Hex.RowNumber))
            {
                if (of.ContainsKey(hex.Hex))
                {
                    continue;
                }

                HexName[] members = [.. MapBuildings.Building(handle.Facts, hex.Index, next => handle.ContainsLocation(geometry.NameOf(next))).Select(geometry.NameOf)
                    .Where(name => !of.ContainsKey(name)).OrderBy(name => name.Column).ThenBy(name => name.RowNumber)];
                foreach (var member in members)
                {
                    of[member] = hexes.Count;
                }

                hexes.Add([.. members.Select(name => new BoardLocation(handle.Ref, name, 0))]);
                ids.Add($"{handle.Ref.Value}:{members[0]}");
            }

            return new BoardBuildings(of, [.. hexes], [.. ids]);
        }
    }

    // Ruling R70.25: a board's buildings, kept with its handle, which the catalog keeps while the board is loaded; the fill reads the board's facts only.
    private static readonly ConditionalWeakTable<BoardHandle, BoardBuildings> BuildingMaps = new();

    // A card's building areas with their hexes (ruling R21.3), read once per card.
    private static readonly ConditionalWeakTable<ScenarioCard, (string Id, IReadOnlyList<BoardLocation> Hexes)[]> CardBuildingAreas = new();

    private BoardBuildings? BuildingsOf(GameState state, BoardRef board) => Handle(state, board) is { } handle ? BuildingMaps.GetValue(handle, BoardBuildings.Read) : null;

    private static (string Id, IReadOnlyList<BoardLocation> Hexes)[] CardBuildings(ScenarioCard card) => CardBuildingAreas.GetValue(card, static read =>
        [.. read.Sides.SelectMany(side => side.Groups).SelectMany(group => group.Areas).Where(area => area.Kind == "building").Select(area => area.Id)
            .Distinct(StringComparer.Ordinal).Select(id => (id, ScenarioCards.BuildingHexes(read, id))).Where(item => item.Item2 is { Count: > 0 })
            .Select(item => (item.id, (IReadOnlyList<BoardLocation>)item.Item2!))]);

    /// <summary>
    /// Whether two Locations are in one building of the map (A10.51's EXC; ruling R70.13), read from the board's buildings (ruling R70.25); a building is of one
    /// board, as the card editor joins it.
    /// </summary>
    private bool SameMapBuilding(GameState state, BoardLocation one, BoardLocation two) =>
        one.Board == two.Board && BuildingsOf(state, one.Board) is { } map && map.Of.TryGetValue(one.Hex, out var first) && map.Of.TryGetValue(two.Hex, out var second)
        && first == second;

    /// <summary>The building a Location is in (ruling R70.25): the card's where one of its building areas holds the hex, else the map's; null for none.</summary>
    private (string Id, IReadOnlyList<BoardLocation> Hexes)? BuildingOf(GameState state, ScenarioCard? card, BoardLocation at)
    {
        if (card is not null && CardBuildings(card).FirstOrDefault(item => item.Hexes.Any(hex => hex.Board == at.Board && hex.Hex == at.Hex)) is { Id: not null } named)
        {
            return named;
        }

        return BuildingsOf(state, at.Board) is { } map && map.Of.TryGetValue(at.Hex, out var index) ? (map.Ids[index], map.Hexes[index]) : null;
    }

    /// <summary>
    /// The hexes of the building a Mopping Up names (A12.153; rulings R24.2, R70.25): the card's building of that id, or the map's whose id it is ("board:lowest
    /// hex"), without its hexes that hold a Blaze (A12.153: a Blazing building Location is no part of it); null when it names none.
    /// </summary>
    private IReadOnlyList<BoardLocation>? MopUpHexes(GameState state, ScenarioCard? card, string building)
    {
        var hexes = card is not null && CardBuildings(card).FirstOrDefault(item => item.Id == building) is { Id: not null } named ? named.Hexes
            : BoardLocation.TryParse($"{building}:0", out var at) && BuildingOf(state, null, at!) is { } found && found.Id == building ? found.Hexes
            : null;
        if (hexes is null)
        {
            return null;
        }

        var index = MemoOf(state).Objects(state);
        return [.. hexes.Where(hex => ScenarioA1MoppingUp.PartOfBuilding(index.Wrecks(hex).Any(wreck => index.ActiveEntity(BlazeId(wreck.Id)))))];
    }

    /// <summary>
    /// The buildings that may be Mopped Up holding any of the Locations given (A12.153; rulings R24.2, R70.25), by id, with their hexes: those of more than
    /// one hex or level, each once. The Play page hands over its side's units' Locations, read in one pass over them.
    /// </summary>
    public IReadOnlyList<(string Id, IReadOnlyList<BoardLocation> Hexes)> MopUpBuildings(GameState state, IEnumerable<BoardLocation> locations)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(locations);
        var card = CardOf(state);
        var found = new List<(string Id, IReadOnlyList<BoardLocation> Hexes)>();
        var seen = new HashSet<string>(StringComparer.Ordinal);
        foreach (var at in locations.Where(at => at.Level >= 0))
        {
            if (BuildingOf(state, card, at) is { } building && seen.Add(building.Id)
                && ScenarioA1MoppingUp.MopUpBuilding(building.Hexes.Count, building.Hexes.Any(hex => HexLevels(state, hex).Any(level => level > 0))))
            {
                found.Add(building);
            }
        }

        return found;
    }
}
