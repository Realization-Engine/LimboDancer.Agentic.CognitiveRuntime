using LimboDancer.Domains.Asl.Maps.Coordinates;
using LimboDancer.Domains.Asl.Units.Catalog;
using LimboDancer.Domains.Asl.Units.State;

namespace LimboDancer.Domains.Asl.Play;

/// <summary>How closely a setup plan matches the stacks a side sees: the hexes where the two agree, of the hexes either one uses.</summary>
public sealed record SetupPlanScore(int Matching, int Hexes)
{
    /// <summary>The share of the hexes that agree, from 0 to 1; 0 when there is no hex to compare.</summary>
    public double Share => Hexes == 0 ? 0 : (double)Matching / Hexes;
}

/// <summary>
/// A setup plan of one side compared with the stacks the other side sees (pass 30b of the Card Play and Map Studio Redesign Plan). The setup plans
/// are public, like the card, and the stacks are on the viewer's own map, so the comparison says nothing the viewer could not count (rulings R23.1,
/// R23.3). It reads a <see cref="GameView"/> and never the game's state, so it cannot know which plan was used, what is under a "?", or where a
/// hidden unit is. Dummies count, since they look like units; that is what they are for (A12.11).
/// </summary>
public static class SetupPlanMatch
{
    // The kinds a view shows as a counter of a stack; SW and Guns lie beneath their units and are not seen before play (A2.9).
    private static readonly string[] UnitKinds = ["asl:squad", "asl:half-squad", "asl:crew", "asl:leader", "asl:hero", "asl:vehicle"];

    /// <summary>
    /// A plan's footprint as the other side would see it: for each hex, the number of its units and Dummies on the map that are not hidden. SW, Guns,
    /// Passengers, hidden counters, and counters off board are left out, since a view never shows them at setup.
    /// </summary>
    public static IReadOnlyDictionary<BoardLocation, int> Footprint(SetupPlan plan, UnitCatalog catalog)
    {
        ArgumentNullException.ThrowIfNull(plan);
        ArgumentNullException.ThrowIfNull(catalog);
        return plan.Placements
            .Where(item => !item.Hidden && !item.OffBoard && item.Holder is null
                && (item.Dummy || (item.Definition is { } definition && catalog.Definition(definition) is { } found && UnitKinds.Contains(found.Kind, StringComparer.Ordinal))))
            .Select(item => BoardLocation.TryParse(item.At, out var at) ? Hex(at) : null).OfType<BoardLocation>()
            .GroupBy(at => at).ToDictionary(hex => hex.Key, hex => hex.Count());
    }

    /// <summary>The counters of a side that a view holds on the map, by hex: its units seen and its sealed presences, all levels of a hex together.</summary>
    public static IReadOnlyDictionary<BoardLocation, int> Seen(GameView view, string side)
    {
        ArgumentNullException.ThrowIfNull(view);
        ArgumentNullException.ThrowIfNull(side);
        return view.Units.Where(unit => unit.Side == side && view.Locations.ContainsKey(unit.Id)).Select(unit => Hex(view.Locations[unit.Id].Location))
            .Concat(view.Sealed.Where(presence => presence.Side == side).Select(presence => Hex(presence.Location)))
            .GroupBy(at => at).ToDictionary(hex => hex.Key, hex => hex.Count());
    }

    /// <summary>A plan's footprint against what the view holds of the plan's side: the hexes where the counts are equal, of the hexes in either.</summary>
    public static SetupPlanScore Score(GameView view, SetupPlan plan, UnitCatalog catalog)
    {
        var footprint = Footprint(plan, catalog);
        var seen = Seen(view, plan.Side);
        var hexes = footprint.Keys.Union(seen.Keys).ToArray();
        return new SetupPlanScore(hexes.Count(at => footprint.GetValueOrDefault(at) == seen.GetValueOrDefault(at)), hexes.Length);
    }

    private static BoardLocation Hex(BoardLocation at) => at with { Level = 0, Side = null };
}
