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

    // Infantry leaving the map (A2.6, A3.3, A4.1, A4.2, A4.7, A4.11, A4.31, A4.72, A8.1, A8.11, A20.53, A26.221, A26.23, C10.2, C10.3, C10.111; rulings R8.6, R20.6, R21.5, R25.5, R26.1, R26.4).

    /// <summary>A2.6 (rulings R21.5, R25.5): units leave the map in their side's MPh or by advance in its APh, never in the RtPh.</summary>
    public static string? ExitPhaseBar(string? phase) =>
        phase != "mph" && phase != "aph" ? "play.exit-phase: units leave the map in their side's MPh or by advance in its APh, never in the RtPh (A2.6; rulings R21.5, R25.5)" : null;

    /// <summary>A2.6: every unit leaving is an active Infantry unit of the phasing side, named once; a vehicle leaves by its own move.</summary>
    public static string? ExitStackBar(int ids, int distinctIds, bool anyNotActiveInfantryOfPhasingSide) =>
        ids == 0 || distinctIds != ids || anyNotActiveInfantryOfPhasingSide
            ? "play.exit-stack: every unit leaving is an active Infantry unit of the phasing side; a vehicle leaves by its own move (A2.6)"
            : null;

    /// <summary>A2.6: the stack leaves from one Location on the map.</summary>
    public static string? ExitFromBar(int distinctLocations, bool anyOffMap) =>
        distinctLocations != 1 || anyOffMap ? "play.exit-stack: the stack leaves from one Location on the map (A2.6)" : null;

    /// <summary>C10.3 (referee, pass 26): a crew still pushing its Gun may push it on, off the map, though its last push made it TI.</summary>
    public static bool PushingOn(bool advancing, bool pushGunNamed, bool movementOpen, bool allIdsAreMembers) => !advancing && pushGunNamed && movementOpen && allIdsAreMembers;

    /// <summary>
    /// A3.3, A4.1, A4.7, C10.3: the first unit not free to move or advance off: broken, pinned, berserk, in Melee, captured, hidden, TI unless pushing on,
    /// its move ended, or Prep Fired in the MPh.
    /// </summary>
    public static string? ExitUnableBar(IReadOnlyList<(string Id, bool Broken, bool Pinned, bool Berserk, bool Melee, bool Captured, bool Hidden, bool Ti, bool MovementEnded, bool PrepFire)> movers,
        bool advancing, bool pushingOn)
    {
        ArgumentNullException.ThrowIfNull(movers);
        return movers.FirstOrDefault(unit => unit.Broken || unit.Pinned || unit.Berserk || unit.Melee || unit.Captured || unit.Hidden || (unit.Ti && !pushingOn) || unit.MovementEnded
            || (!advancing && unit.PrepFire)) is { Id: { } unable }
            ? advancing
                ? $"play.exit-unit: {unable} is not free to advance this APh (A4.7)"
                : $"play.exit-unit: {unable} is not free to move this MPh (A4.1, A3.3)"
            : null;
    }

    /// <summary>
    /// C10.3, C10.111, C10.2 (rulings R26.1, R26.4): a crew or HS manning a Gun leaves the map only pushing it: named in the request, alone, in the MPh, a
    /// crew or HS, a Gun with an M#, not from Bypass, and QSU (read last, only when the rest holds).
    /// </summary>
    public static string? ExitGunBar(string gunner, string manned, string? pushGunId, bool advancing, IReadOnlyList<string> others, bool crewOrHalfSquad, bool hasManhandlingNumber,
        bool inBypass, Func<bool> quickSetUp)
    {
        ArgumentNullException.ThrowIfNull(others);
        ArgumentNullException.ThrowIfNull(quickSetUp);
        if (pushGunId is null || pushGunId != manned)
        {
            return $"play.exit-unit: {gunner} mans {manned}; it leaves the map only pushing it (check \"push {manned}\" beside the move), since abandoning a Gun to leave is not built (C10.3; ruling R26.4)";
        }

        var pushBar = advancing ? "a Gun is pushed in the MPh, not by advance"
            : others.Count != 0 ? $"{gunner} pushes its Gun alone; leave {string.Join(", ", others)} out of the stack"
            : !crewOrHalfSquad ? $"{gunner} is not a crew or HS"
            : !hasManhandlingNumber ? $"{manned} has no M# in the catalog"
            : inBypass ? "a Gun is not pushed from Bypass"
            : null;
        if (pushBar is not null)
        {
            return $"play.move-push: {pushBar} (C10.3, C10.111; ruling R26.4)";
        }

        return !quickSetUp() ? $"play.move-push: {manned} is not QSU, and limbering is not built, so it is not pushed (C10.2; ruling R26.1)" : null;
    }

    /// <summary>A8.1, A8.11, A4.2, A2.6 (ruling R21.5; table player, pass 25): the DEFENDER's window, another stack moving, or a part of the moving stack leaving.</summary>
    public static string? ExitMovementBar(bool movementOpen, bool windowOpen, bool membersAreTheIds, bool anyIdIsMember, IReadOnlyList<string> movers)
    {
        ArgumentNullException.ThrowIfNull(movers);
        return movementOpen && (windowOpen || !membersAreTheIds)
            ? windowOpen
                ? "play.move-window: the DEFENDER may still fire at the stack's last MF expenditure (A8.1, A8.11)"
                : !anyIdIsMember
                    ? $"play.move-order: {string.Join(", ", movers)} moved last; end their move first (A4.2)"
                    : "play.exit-stack: the whole moving stack leaves together (A2.6, A4.2; ruling R21.5)"
            : null;
    }

    /// <summary>
    /// A2.6 (rulings R20.6, R21.5): the exit is from a ground-level hex of the named edge within the playable area; from a hex near the edge, the refusal names
    /// the edge hexes next to it (table player, pass 21), read only then.
    /// </summary>
    public static string? ExitEdgeBar(int fromLevel, bool onEdge, bool playable, string from, string edge, Func<IReadOnlyList<string>> nearEdgeHexes)
    {
        ArgumentNullException.ThrowIfNull(nearEdgeHexes);
        if (fromLevel == 0 && onEdge && playable)
        {
            return null;
        }

        var near = fromLevel == 0 && !onEdge ? nearEdgeHexes() : [];
        return $"play.exit-edge: {from} is not a ground-level hex of the {edge} edge within the playable area (A2.6; rulings R20.6, R21.5)"
            + (near.Count > 0 ? $"; {string.Join(" and ", near)} " + (near.Count == 1 ? "is" : "are") + " next to it" : string.Empty);
    }

    /// <summary>
    /// A2.6, A4.31 (ruling R25.5): from Bypass, through the far vertex of the last hexside, with one MF beyond the Bypass, as Open Ground; the hexside the
    /// stack entered its Bypass by must be readable, and the last hexside or its far vertex must lie on the edge. Hexsides are 0 to 5.
    /// </summary>
    public static (string? Refusal, int HalfMf, string Terrain) BypassExit(int? entered, int laneFirst, int laneLast, IReadOnlyList<int> edgeHexsides, string edge)
    {
        ArgumentNullException.ThrowIfNull(edgeHexsides);
        if (entered is not { } came)
        {
            return ("play.exit-bypass: the hexside the stack entered its Bypass by cannot be read", 0, string.Empty);
        }

        var far = (laneLast + ((laneFirst - came + 6) % 6)) % 6;
        if (!edgeHexsides.Any(side => side == laneLast || side == far))
        {
            return ($"play.exit-bypass: from Bypass the stack leaves only through the far vertex of its last hexside, which is not on the {edge} edge (A2.6, A4.31; ruling R25.5)", 0, string.Empty);
        }

        return (null, 2, "open-ground");
    }

    /// <summary>A2.6 (ruling R21.5): the cheapest edge hexside of the hex, among the decided costs that are not all of a unit's MF; a half hex may lie on the edge across two.</summary>
    public static (string? Refusal, int HalfMf, string Terrain, bool Road) ExitCost(IReadOnlyList<(int HalfMf, string Terrain, bool RoadRate, bool AllMf)> crossings, string from)
    {
        ArgumentNullException.ThrowIfNull(crossings);
        var decided = crossings.Where(item => !item.AllMf).ToArray();
        if (decided.Length == 0)
        {
            return ($"play.exit-terrain: the cost of leaving {from} is not decided (A2.6; ruling R21.5)", 0, string.Empty, false);
        }

        var cheapest = decided.MinBy(item => item.HalfMf);
        return (null, cheapest.HalfMf, cheapest.Terrain, cheapest.RoadRate);
    }

    /// <summary>C10.3 (rulings R8.6, R26.4): a Gun is pushed off only across Open Ground or grain, at double the MF.</summary>
    public static (string? Refusal, int HalfMf) PushedExit(string terrain, int halfMf, string from) =>
        terrain is not ("open-ground" or "grain")
            ? ($"play.move-push-terrain: a Gun is pushed only into Open Ground or grain in the review, and leaving {from} crosses {terrain} (C10.3; rulings R8.6, R26.4)", halfMf)
            : (null, halfMf * 2);

    /// <summary>The MF of an exit in words.</summary>
    public static string ExitHow(int halfMf, bool road, bool fromBypass) =>
        $"for {halfMf / 2m} MF" + (road ? " at the road rate" : string.Empty) + (fromBypass ? " from Bypass" : string.Empty);

    /// <summary>A20.53: the prisoners a stack escorts off, in words.</summary>
    public static string ExitWith(IReadOnlyList<string> escorted)
    {
        ArgumentNullException.ThrowIfNull(escorted);
        return escorted.Count > 0 ? $", escorting {string.Join(", ", escorted)} (A20.53)," : string.Empty;
    }

    /// <summary>
    /// A26.23: whether an exit from <paramref name="from"/> meets one of a side's Exit VP conditions: off the condition's edge, from a hex on or adjacent to
    /// one it names. <paramref name="fromOnOrAdjacent"/> reads a named hex against the map. One function serves the exit's scoring and the Victory
    /// Conditions' Qualifies (the design's D6: the two gave the same result for every input).
    /// </summary>
    public static bool ExitMeetsCondition(IEnumerable<CardConditionFacts> conditions, string side, string edge, Func<string, bool> fromOnOrAdjacent)
    {
        ArgumentNullException.ThrowIfNull(conditions);
        ArgumentNullException.ThrowIfNull(fromOnOrAdjacent);
        return conditions.Any(condition => condition.Type == "exit-vp" && condition.Side == side && condition.Edge == edge && condition.Near!.Any(fromOnOrAdjacent));
    }

    /// <summary>
    /// What an exit counts for (A26.23, A26.221; table player, pass 25): toward the side's Exit VP when it meets an exit condition, otherwise as elimination for
    /// the enemy's CVP, but an escorting Guard off its side's Friendly Board Edge (A20.53); empty for a card whose Victory Conditions name no outcome. The
    /// Friendly Board Edge is read only for an escort that meets no condition.
    /// </summary>
    public static string ExitScoring(bool hasOutcomes, bool meets, bool escorting, Func<bool> escortEdge)
    {
        ArgumentNullException.ThrowIfNull(escortEdge);
        if (!hasOutcomes)
        {
            return string.Empty;
        }

        return meets ? "; it counts toward the side's Exit VP, none for broken Personnel (A26.23)"
            : escorting && escortEdge() ? "; it meets no exit condition, but a Guard escorting prisoners off its side's Friendly Board Edge is not eliminated for CVP; its prisoners stay captured (A20.53, A26.221)"
            : escorting ? "; it meets no exit condition and is not the side's Friendly Board Edge, so the Guard counts as eliminated for the enemy's CVP; its prisoners stay captured (A20.53, A26.221)"
            : "; it meets no exit condition of the side, so the units count as eliminated for the enemy's CVP (A26.221)";
    }

    /// <summary>
    /// A4.72 (ruling R25.5; referee, pass 25): an advance off the map into Difficult Terrain makes the unit CX, and a CX unit does not make it; each unit's
    /// difficulty is read in order, with a leader's two MF and IPC as any advance. Returns the refusal, or the units that become CX.
    /// </summary>
    public static (string? Refusal, IReadOnlyList<string> Tiring) AdvanceExit(IReadOnlyList<(string Id, bool Cx)> movers, Func<int, bool?> difficult)
    {
        ArgumentNullException.ThrowIfNull(movers);
        ArgumentNullException.ThrowIfNull(difficult);
        var tiring = new List<string>();
        foreach (var (unit, index) in movers.Select((unit, index) => (unit, index)))
        {
            if (difficult(index) is not { } hard)
            {
                return ($"play.advance-mf: {unit.Id} has no MF allotment the catalog decides, or none left after portage (A4.7, A4.72)", tiring);
            }

            if (hard && unit.Cx)
            {
                return ($"play.advance-difficult-terrain: {unit.Id} is CX and may not advance into Difficult Terrain (A4.72)", tiring);
            }

            if (hard)
            {
                tiring.Add(unit.Id);
            }
        }

        return (null, tiring);
    }

    /// <summary>A4.11: Double Time's extra MF for a unit that has spent none, or one; without Double Time, the MF it already has from it.</summary>
    public static int DoubleTimeExtraMf(bool doubleTime, int mfSpent, bool halfMfSpent, int doubleTimeMf) => doubleTime ? (mfSpent == 0 && !halfMfSpent ? 2 : 1) : doubleTimeMf;

    /// <summary>A2.6, A4.11: each mover has the MF left to leave; its allowance is read in order, only until a refusal.</summary>
    public static string? ExitMfBar(IReadOnlyList<(string Id, int MfSpent, bool HalfMfSpent)> movers, Func<int, int?> allowance, int halfMf, string from)
    {
        ArgumentNullException.ThrowIfNull(movers);
        ArgumentNullException.ThrowIfNull(allowance);
        foreach (var (unit, index) in movers.Select((unit, index) => (unit, index)))
        {
            if (allowance(index) is not { } allowed)
            {
                return $"play.move-mf: {unit.Id} has no MF allowance the catalog decides";
            }

            var left = (allowed * 2) - ((unit.MfSpent * 2) + (unit.HalfMfSpent ? 1 : 0));
            if (left < halfMf)
            {
                return $"play.move-mf: {unit.Id} has {left / 2m} MF left, and leaving the map from {from} costs {halfMf / 2m} (A2.6, A4.11)";
            }
        }

        return null;
    }

    /// <summary>C10.3 (ruling R26.4): the Manhandling DR's modifier for a push off the map: the MF spent, -2 across a road hexside.</summary>
    public static int PushOffDrm(int halfMf, bool road) => (halfMf / 2) - (road ? 2 : 0);

    /// <summary>A2.6 (ruling R25.5): the advance off the map, in words.</summary>
    public static string AdvanceExitText(IReadOnlyList<string> ids, string from, string edge, string terrain, string with, IReadOnlyList<string> tiring, string scoring)
    {
        ArgumentNullException.ThrowIfNull(ids);
        ArgumentNullException.ThrowIfNull(tiring);
        return $"play.exit: {string.Join(", ", ids)} advance off the map from {from} across the {edge} edge ({terrain}){with} and may not return"
            + (tiring.Count > 0 ? $"; {string.Join(", ", tiring)} become CX advancing into Difficult Terrain (A4.72)" : string.Empty) + $" (A2.6; ruling R25.5){scoring}";
    }

    /// <summary>A2.6 (rulings R21.5, R26.4): a Gun pushed off the map, in words.</summary>
    public static string PushExitText(IReadOnlyList<string> ids, string from, string edge, string how, string scoring)
    {
        ArgumentNullException.ThrowIfNull(ids);
        return $"play.exit: {string.Join(", ", ids)} leave{(ids.Count == 1 ? "s" : string.Empty)} the map from {from} across the {edge} edge {how} (A2.6; rulings R21.5, R26.4){scoring}";
    }

    /// <summary>A2.6 (rulings R21.5, R25.5): the move off the map, in words.</summary>
    public static string MoveExitText(IReadOnlyList<string> ids, string from, string edge, string how, string with, string scoring)
    {
        ArgumentNullException.ThrowIfNull(ids);
        return $"play.exit: {string.Join(", ", ids)} leave the map from {from} across the {edge} edge {how}{with} and may not return (A2.6; rulings R21.5, R25.5){scoring}";
    }
}
