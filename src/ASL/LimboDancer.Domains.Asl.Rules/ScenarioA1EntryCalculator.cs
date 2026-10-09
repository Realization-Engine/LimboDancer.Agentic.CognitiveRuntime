namespace LimboDancer.Domains.Asl.Rules;

/// <summary>A unit waiting off board as the APh's end reads it (A2.5; rulings R20.5, R25.1): its id, whether it is a vehicle, its entry turn, and the hexes it may advance into, read only when its turn has come.</summary>
public sealed record EntryWaitFacts(string Id, bool Vehicle, int? EntryTurn, Func<IReadOnlyList<string>> AdvanceEntries);

/// <summary>A mover of an entering stack as facts (A2.5, A2.51, A2.52, A5.1; rulings R20.5, R25.2, R26.1): its id, whether it is a vehicle, its squad-equivalent, whether it is a SMC, and its entry (turn, edge, and the hexes the card names).</summary>
public sealed record OffBoardMoverFacts(string Id, bool Vehicle, double SquadEquivalent, bool Smc, (int Turn, string Edge, IReadOnlyList<string>? NamedHexes)? Entry);

/// <summary>
/// Entry from off board (pass 32.j, S10; A2.5, A2.51, A2.52, A4.14, A5.1, B16.4, B21, D2.4; rulings R20.5, R20.6, R25.1 to R25.4, R26.1): which entry a unit
/// enters by, the hexes open to it this Game Turn, what bars a hex, the stack's checks in their order, and the APh's end while a unit waits. Play reads the
/// state, the card, and the map, hands the facts over as texts and as indexes into its own lists, and makes each read only when Rules asks for it.
/// </summary>
public static class ScenarioA1EntryCalculator
{
    /// <summary>
    /// The entry area a unit waiting off board enters by (rulings R20.5, R25.4): the area its setup named, else its OB group's entry for its line (a HS its
    /// squad's, a Balance counter its group's first); null when the card gives none or the area has no turn or edge.
    /// </summary>
    public static (int Turn, string Edge, string AreaId)? EntryFor(SetupGroupFacts group, string? named, string? definition, string? squad)
    {
        ArgumentNullException.ThrowIfNull(group);
        var entry = (named is not null ? group.Areas.FirstOrDefault(area => area.Kind == "entry" && area.Id == named) : null)
            ?? group.Units.Where(line => line.Definition == definition || line.Definition == squad).Select(line => ScenarioA1SetupCalculator.EntryOf(group, line)).FirstOrDefault(area => area is not null)
            ?? group.Areas.Where(area => area.Kind == "entry").OrderBy(area => area.Turn).FirstOrDefault();
        return entry is { Turn: { } turn, Edge: { } edge } ? (turn, edge, entry.Id) : null;
    }

    /// <summary>
    /// The hexes a stack waiting off board may enter in this Game Turn (A2.5; rulings R20.5, R25.2), as indexes into the edge's hexes: every hex of the edge
    /// when the card names no entry hexes; else the named hexes on the entry turn, and each Game Turn later the hexes of the edge within four more hexes of
    /// them, never past a river or canal on the edge. The distances and the river reads are made only on a late entry.
    /// </summary>
    public static IReadOnlyList<int> EntryHexesFor(int edgeHexes, bool namedGiven, Func<int, bool> named, int gameTurn, int entryTurn, Func<int, bool> river, Func<int, int, int?> distance)
    {
        ArgumentNullException.ThrowIfNull(named);
        ArgumentNullException.ThrowIfNull(river);
        ArgumentNullException.ThrowIfNull(distance);
        var edge = Enumerable.Range(0, edgeHexes).ToArray();
        if (!namedGiven)
        {
            return edge;
        }

        var points = edge.Where(named).ToArray();
        var radius = 4 * Math.Max(0, gameTurn - entryTurn);
        if (radius == 0)
        {
            return points;
        }

        var rivers = edge.Where(river).ToArray();
        return [.. edge.Where(at => points.Any(point => distance(point, at) is { } far && far <= radius
            && !rivers.Any(crossing => distance(point, crossing) is { } near && near > 0 && distance(crossing, at) is { } rest && near + rest == far)))];
    }

    /// <summary>Whether a hex is a river or canal (B21), which delayed entry never crosses along the edge (A2.5): its center terrain is Water or names a River or Canal.</summary>
    public static bool IsRiver(string? centerTerrain) =>
        centerTerrain is { } name && (name == "Water" || name.Contains("River", StringComparison.Ordinal) || name.Contains("Canal", StringComparison.Ordinal));

    /// <summary>A2.51 (ruling R20.5): a ground-level hex is an entry hex of an edge when it lies on that edge and within the playable area.</summary>
    public static bool EntryHex(bool onEdge, bool playable) => onEdge && playable;

    /// <summary>A2.5, A4.14 (rulings R25.2, R25.3): an active enemy unit obstructs entry when it is a Known enemy unit or an enemy vehicle, and not captured.</summary>
    public static bool EntryObstructor(bool active, bool enemy, bool captured, bool knownEnemy, bool vehicle) => active && enemy && !captured && (knownEnemy || vehicle);

    /// <summary>
    /// Why a hex obstructs a side's entry from off board (A2.5; rulings R20.5, R25.2): a Known enemy unit or an enemy vehicle holds it (A4.14), or its entry
    /// cost is not decided; null when it may be entered. A vehicle's entry cost is its own, read with its entry (ruling R26.1), so the cost is read only for Infantry.
    /// </summary>
    public static string? EntryHexBar(bool obstructed, string at, bool vehicle, Func<bool> entryCostDecided)
    {
        ArgumentNullException.ThrowIfNull(entryCostDecided);
        if (obstructed)
        {
            return $"play.entry-occupied: {at} holds a Known enemy unit, so it is not entered from off board in the MPh (A2.5, A4.14; ruling R25.2)";
        }

        return vehicle || entryCostDecided()
            ? null
            : $"play.entry-terrain: the entry cost of {at} is not decided (ruling R20.5)";
    }

    /// <summary>
    /// Why the phasing side's APh may not end (A2.5; rulings R20.5, R25.1): a non-vehicle unit whose entry turn has come still waits off board and may still
    /// enter a hex open to it by advance; null when none does. The refusal names every unit still waiting and hexes open to them (table player, pass 25).
    /// </summary>
    public static string? EntryDue(IReadOnlyList<EntryWaitFacts> units, int gameTurn)
    {
        ArgumentNullException.ThrowIfNull(units);
        var waiting = new List<string>();
        var open = new List<string>();
        foreach (var unit in units)
        {
            if (!unit.Vehicle && unit.EntryTurn is { } turn && turn <= gameTurn && unit.AdvanceEntries() is { Count: > 0 } hexes)
            {
                waiting.Add(unit.Id);
                open.AddRange(hexes.Where(at => !open.Contains(at, StringComparer.Ordinal)));
            }
        }

        return waiting.Count == 0 ? null
            : $"play.entry-due: {string.Join(", ", waiting)} {(waiting.Count == 1 ? "waits" : "wait")} off board and must still enter this Game Turn: advance into a hex of the entry edge, such as "
                + string.Join(", ", open.Take(6)) + (open.Count > 6 ? ", ..." : string.Empty) + ", before the APh ends (A2.5; rulings R20.5, R25.1)";
    }

    /// <summary>
    /// The hexes a unit may enter by advance in this APh (ruling R25.1; table player and referee, pass 25), as indexes into the hexes open to its entry: none
    /// when pinned; else those no Known enemy unit or enemy vehicle holds, entered across the edge hexside at a decided cost that is not all of a unit's MF
    /// (marsh is not entered in the APh, B16.4), that the unit can advance into with MF left after portage, and not CX into Difficult Terrain (A4.7, A4.72).
    /// Each read is made in the old order, only when the earlier ones pass.
    /// </summary>
    public static IReadOnlyList<int> AdvanceEntries(bool pinned, bool cx, Func<int> openHexes, Func<int, bool> barred, Func<int, bool> crossesEdge,
        Func<int, (int HalfMf, bool AllMf)?> entryCost, Func<int, bool?> difficult)
    {
        ArgumentNullException.ThrowIfNull(openHexes);
        ArgumentNullException.ThrowIfNull(barred);
        ArgumentNullException.ThrowIfNull(crossesEdge);
        ArgumentNullException.ThrowIfNull(entryCost);
        ArgumentNullException.ThrowIfNull(difficult);
        return pinned ? []
            : [.. Enumerable.Range(0, openHexes()).Where(at => !barred(at) && crossesEdge(at) && entryCost(at) is { AllMf: false } cost
                && difficult(cost.HalfMf) is { } hard && !(hard && cx))];
    }

    /// <summary>
    /// The hexes a unit waiting off board may enter in this MPh (A2.5; rulings R20.5, R25.2), as indexes into the hexes open to its entry: none before its entry
    /// turn, else those not barred.
    /// </summary>
    public static IReadOnlyList<int> OpenEntryHexes(int? entryTurn, int gameTurn, Func<int> openHexes, Func<int, bool> barred)
    {
        ArgumentNullException.ThrowIfNull(openHexes);
        ArgumentNullException.ThrowIfNull(barred);
        return entryTurn is { } turn && turn <= gameTurn
            ? [.. Enumerable.Range(0, openHexes()).Where(at => !barred(at))]
            : [];
    }

    /// <summary>
    /// The entry of a stack waiting off board (A2.5, A2.51; rulings R20.5, R25.2): no vehicle with Infantry or by advance (A2.52, D2.4; ruling R26.1); the
    /// stacking limits (A5.1); every unit enters by the card in this Game Turn or a later one, along one edge, into a ground-level hex of that edge within the
    /// playable area, among the hexes open to its entry; in the MPh not one a Known enemy unit holds (A4.14). Returns the edge and the edge hexside crossed
    /// (0 to 5), or why the stack may not enter <paramref name="to"/>. <paramref name="edgeHexside"/> gives the hexside of the target on an edge, null for none;
    /// <paramref name="openToMover"/> whether the target is among a mover's open hexes; <paramref name="anyNamedOnEdge"/> whether any hex a mover's card
    /// names lies on the edge within the playable area.
    /// </summary>
    public static (string? Edge, int Side, string? Reason) EntryCheck(IReadOnlyList<OffBoardMoverFacts> movers, string to, int toLevel, int gameTurn, bool advancing, bool vehicleEntry,
        Func<string, int?> edgeHexside, Func<string?> playableBar, Func<int, bool> openToMover, Func<int, bool> anyNamedOnEdge, Func<string?> entryHexBar)
    {
        ArgumentNullException.ThrowIfNull(movers);
        ArgumentNullException.ThrowIfNull(edgeHexside);
        ArgumentNullException.ThrowIfNull(playableBar);
        ArgumentNullException.ThrowIfNull(openToMover);
        ArgumentNullException.ThrowIfNull(anyNamedOnEdge);
        ArgumentNullException.ThrowIfNull(entryHexBar);
        if (!vehicleEntry && movers.FirstOrDefault(unit => unit.Vehicle) is { } vehicle)
        {
            return (null, default, $"play.entry-vehicle: {vehicle.Id} is a vehicle; it enters by its own MP expenditure in its MPh, in Motion, not with Infantry or by advance (A2.52, D2.4; ruling R26.1)");
        }

        // A5.1, A2.51 (referee, pass 25): an entering stack keeps to the stacking limits, which offboard setup never exceeds.
        if (ScenarioA1SetupCalculator.EntryStackOverstacked(movers.Sum(unit => unit.SquadEquivalent), movers.Count(unit => unit.Smc)))
        {
            return (null, default, "play.entry-stacking: an entering stack holds at most three squad-equivalents and four SMC, the stacking limits offboard setup never exceeds (A5.1, A2.51)");
        }

        if (movers.FirstOrDefault(item => item.Entry is null) is { } lost)
        {
            return (null, default, $"play.entry: {lost.Id} waits off board with no entry on the card (ruling R20.5)");
        }

        if (movers.FirstOrDefault(item => item.Entry!.Value.Turn > gameTurn) is { } late)
        {
            return (null, default, $"play.entry-turn: {late.Id} enters on Game Turn {late.Entry!.Value.Turn}, not {gameTurn} (A2.5; ruling R20.5)");
        }

        if (movers.Select(item => item.Entry!.Value.Edge).Distinct(StringComparer.Ordinal).ToArray() is not [var edge])
        {
            return (null, default, "play.entry-stack: a stack enters along one edge; units of different entry edges enter apart (ruling R20.5)");
        }

        if (toLevel != 0 || edgeHexside(edge) is not { } crossing)
        {
            return (null, default, $"play.entry-edge: {string.Join(", ", movers.Select(unit => unit.Id))} enter at ground level in a hex of the {edge} edge, and {to} is not one (A2.51; ruling R20.5)");
        }

        if (playableBar() is { } outside)
        {
            return (null, default, outside);
        }

        // A2.5 (ruling R25.2): named entry hexes, and the four-hex radius a Game Turn later for each turn the entry was blocked.
        foreach (var (unit, index) in movers.Select((unit, index) => (unit, index)))
        {
            var entry = unit.Entry!.Value;
            if (!openToMover(index))
            {
                var named = entry.NamedHexes is { Count: > 0 } hexes ? string.Join(", ", hexes) : null;

                // Referee, pass 25: a card naming no hex of the edge is said so, not left silent.
                if (named is not null && !anyNamedOnEdge(index))
                {
                    return (null, default, $"play.entry-hex: the card names {named} for {unit.Id}'s entry, and none is a hex of the {edge} edge within the playable area (ruling R25.2)");
                }

                return (null, default, named is null
                    ? $"play.entry-edge: {to} is not a hex of the {edge} edge within the playable area (A2.51; ruling R20.5)"
                    : gameTurn == entry.Turn
                        ? $"play.entry-hex: {unit.Id} enters by {named} on Game Turn {entry.Turn}; elsewhere only a Game Turn later, if they are blocked (A2.5; ruling R25.2)"
                        : $"play.entry-hex: {unit.Id} was to enter by {named}; on Game Turn {gameTurn} it enters within {4 * (gameTurn - entry.Turn)} hexes of them along the {edge} edge, never past a river or canal (A2.5; ruling R25.2)");
            }
        }

        return !advancing && entryHexBar() is { } barred ? (null, default, barred) : (edge, crossing, null);
    }
}
