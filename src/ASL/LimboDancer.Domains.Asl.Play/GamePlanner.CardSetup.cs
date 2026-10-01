using LimboDancer.Domains.Asl.Maps.Coordinates;
using LimboDancer.Domains.Asl.Maps.Geometry;
using LimboDancer.Domains.Asl.ScenarioA1;
using LimboDancer.Domains.Asl.Units.Catalog;
using LimboDancer.Domains.Asl.Units.State;

namespace LimboDancer.Domains.Asl.Play;

/// <summary>
/// The setup of a game from a scenario card (pass 19 of the Scenario Card Games Plan; rulings R19.1 to R19.6): the planner's reading of the game for
/// <see cref="ScenarioSetup"/>, which checks each setup proposal and the start of play.
/// </summary>
public sealed partial class GamePlanner
{
    // The embedded cards, read once per card and catalog: the route searches ask for the playable area at every step (referee, pass 20).
    private static readonly System.Collections.Concurrent.ConcurrentDictionary<(string Card, string Sha256, CatalogIdentity Catalog), ScenarioCard?> Cards = new();

    /// <summary>
    /// The card a game starts from, as the library holds it now; null for a game that names none, and for one whose card has changed or gone since it
    /// started, so no rule follows a card the game did not start from (referee, pass 22).
    /// </summary>
    internal ScenarioCard? CardOf(GameState state)
    {
        ArgumentNullException.ThrowIfNull(state);
        return state.Scenario is { } scenario && catalogs.FirstOrDefault(catalog => catalog.Identity == state.Catalog) is { } catalog
            && CachedCard(scenario.Id, catalog) is { } card && card.Sha256 == scenario.Sha256
            ? card.Card
            : null;
    }

    /// <summary>Why a game's card no longer serves it: deleted, or changed since the game started (table player, pass 22).</summary>
    internal string Gone(string id) => CardLibrary.Sha256(id) is null ? "is no longer among the scenario cards" : "has changed since the game started";

    /// <summary>A card and the SHA-256 of the text it was parsed from, read once per text (referee, pass 22).</summary>
    private (ScenarioCard Card, string Sha256)? CachedCard(string id, UnitCatalog catalog)
    {
        if (CardLibrary.Current(id) is not { } current)
        {
            return null;
        }

        return Cards.GetOrAdd((id, current.Sha256, catalog.Identity), _ => ScenarioCards.Parse(id, current.Text, catalog).Card) is { } card
            ? (card, current.Sha256)
            : null;
    }

    /// <summary>
    /// The setup of a game from a card (rulings R19.1 to R19.6): every counter on the map with its group and Location (a SW its holder's), the
    /// ones this proposal places marked new. Null for a game that names no card.
    /// </summary>
    public SetupReport? CardSetup(GameState state, IReadOnlySet<string> placedNow)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(placedNow);
        // Ruling R22.3: a minimal card has no OB, so its units set up by hand as in a game with no card.
        if (CardOf(state) is not { Minimal: false } card)
        {
            return null;
        }

        var counters = new List<SetupCounter>();
        foreach (var unit in state.Units.Where(unit => unit.Status == InstanceStatus.Active))
        {
            // Ruling R26.2: a Passenger waits off board, and enters, with its vehicle.
            var aboard = state.Aboard(unit.Id);
            var carrier = aboard is null ? null : state.Unit(aboard);
            counters.Add(new SetupCounter(unit.Id, unit.Side, unit.Group, unit.Definition?.Definition, unit.Kind, state.Location(unit.Id)?.Location,
                Is(unit, Conditions.Concealed), Is(unit, Conditions.Hidden), unit.Kind == UnitKinds.Dummy, false, placedNow.Contains(unit.Id))
            {
                OffBoard = unit.Position is OffMapPosition or ContainedPosition && state.Location(unit.Id) is null,
                Entry = (unit.Position as OffMapPosition)?.Entry ?? (carrier?.Position as OffMapPosition)?.Entry,
                Broken = Is(unit, Conditions.Broken),
                NonOb = state.NonObConcealed.Contains(unit.Id, StringComparer.Ordinal),
                Aboard = aboard,
                Manning = state.Equipment.Any(item => item.Status == InstanceStatus.Active && item.Kind == "asl:gun" && item.Holding is { Role: HoldingRole.Manned } manned
                    && manned.Holder == unit.Id),
            });
        }

        foreach (var item in state.Equipment.Where(item => item.Status == InstanceStatus.Active))
        {
            // A SW belongs to its holder's group, at its holder's Location; equipment on its own belongs to no group (referee, pass 19).
            var holder = item.Holding is { } holding ? state.Unit(holding.Holder) : null;
            // A12.34 (ruling R26.5): a manned Gun's hidden status is its own, no longer hidden behind its crew's; a SW is hidden only with its holder.
            counters.Add(new SetupCounter(item.Id, holder?.Side ?? item.Side ?? string.Empty, holder?.Group, item.Definition?.Definition, item.Kind,
                holder is not null ? state.Location(holder.Id)?.Location : (item.Position as MapPosition)?.Location, false,
                Is(item, Conditions.Hidden) && item.Holding is not { Role: HoldingRole.Possessed }, false, true, placedNow.Contains(item.Id))
            {
                OffBoard = holder is not null && holder.Position is OffMapPosition or ContainedPosition && state.Location(holder.Id) is null,
                Manning = item.Holding is { Role: HoldingRole.Manned },
                Towed = item.Holding is { Role: HoldingRole.Towed },
            });
        }

        return ScenarioSetup.Check(card, counters, at => ReadLocation(state, at) is { } read ? TerrainKey(read) : null,
            key => key is "marsh" || (key is not null && InfantryEntryHalfMf(state, key) is not null), ScenarioA1FireReference.HalfSquadOf, state.ScenarioMonth,
            state.Scenario?.Balance);
    }

    /// <summary>Why a Location is refused as outside the card's playable area (A2.1; ruling R20.6); null when it is inside, or the game has no card.</summary>
    internal string? PlayableBar(GameState state, BoardLocation at) =>
        state.Scenario is not null && CardOf(state) is { } card && !ScenarioCards.Playable(card, at)
            ? $"play.playable: {at} is outside the playable area: {card.PlayableArea!.Text} (A2.1; ruling R20.6)"
            : null;

    /// <summary>
    /// The Game Turn, edge, and entry area a unit waiting off board enters by (rulings R20.5, R25.4): the entry area its setup named, else its OB group's
    /// entry for its line (a HS its squad's, a Balance counter its group's first); null when the card gives none.
    /// </summary>
    public (int Turn, string Edge, ScenarioCardSetup Area)? EntryFor(GameState state, UnitInstance unit)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(unit);
        if (CardOf(state) is not { } card || unit.Group is not { } groupId || card.Sides.FirstOrDefault(side => side.Side == unit.Side) is not { } side)
        {
            return null;
        }

        var index = side.Groups.Select((group, at) => (group, at)).FirstOrDefault(item => ScenarioCards.GroupId(side.Side, item.at) == groupId).group;
        if (index is not { } group)
        {
            return null;
        }

        var definition = unit.Definition?.Definition;
        var squad = definition is null ? null : ScenarioA1FireReference.SquadOf(definition);
        var entry = (unit.Position is OffMapPosition { Entry: { } named } ? group.Areas.FirstOrDefault(area => area.Kind == "entry" && area.Id == named) : null)
            ?? group.Units.Where(line => line.Definition == definition || line.Definition == squad).Select(line => ScenarioSetup.EntryOf(group, line)).FirstOrDefault(area => area is not null)
            ?? group.Areas.Where(area => area.Kind == "entry").OrderBy(area => area.Turn).FirstOrDefault();
        return entry is { Turn: { } turn, Edge: { } edge } ? (turn, edge, entry) : null;
    }

    /// <summary>
    /// The hexes a stack waiting off board may enter in this Game Turn (A2.5; rulings R20.5, R25.2): every hex of its edge within the playable area; or,
    /// when its entry area names its entry hexes, those hexes on its entry turn, and each Game Turn later (its entry blocked) the hexes of the edge
    /// within four more hexes of them, never past a river or canal on the edge.
    /// </summary>
    public IReadOnlyList<BoardLocation> EntryHexesFor(GameState state, (int Turn, string Edge, ScenarioCardSetup Area) entry)
    {
        ArgumentNullException.ThrowIfNull(state);
        var edge = EntryHexes(state, entry.Edge);
        if (entry.Area.Hexes is not { Count: > 0 } named || CardOf(state) is not { } card)
        {
            return edge;
        }

        var board = entry.Area.Board ?? (card.Boards.Count == 1 ? card.Boards[0].Board : null);
        var points = edge.Where(at => at.Board.Value == board && named.Contains(at.Hex.ToString(), StringComparer.Ordinal)).ToArray();
        var radius = 4 * Math.Max(0, state.Turn - entry.Turn);
        if (radius == 0)
        {
            return points;
        }

        var rivers = edge.Where(at => IsRiver(state, at)).ToArray();
        return [.. edge.Where(at => points.Any(point => HexDistance(state, point, at) is { } distance && distance <= radius
            && !rivers.Any(river => HexDistance(state, point, river) is { } near && near > 0 && HexDistance(state, river, at) is { } far && near + far == distance)))];
    }

    /// <summary>Whether a hex is a river or canal (B21), which delayed entry never crosses along the edge (A2.5).</summary>
    private bool IsRiver(GameState state, BoardLocation at) =>
        ReadLocation(state, at)?.Hex.Center.Terrain?.Name is { } name && (name == "Water" || name.Contains("River", StringComparison.Ordinal) || name.Contains("Canal", StringComparison.Ordinal));

    /// <summary>The ground-level hexes of an edge within the playable area (A2.51; ruling R20.5), from each board of the map.</summary>
    public IReadOnlyList<BoardLocation> EntryHexes(GameState state, string edge)
    {
        ArgumentNullException.ThrowIfNull(state);
        var hexes = new List<BoardLocation>();
        foreach (var placed in state.Map.Boards)
        {
            if (boards.TryGetBoard(placed.Board, placed.Version).Board is not { } handle)
            {
                continue;
            }

            foreach (var index in handle.Geometry.Hexes())
            {
                var at = BoardLocation.Parse($"{placed.Board.Value}:{handle.Geometry.NameOf(index)}:0");
                if (EdgeSides(state, at).Any(item => item.Edge == edge) && PlayableBar(state, at) is null)
                {
                    hexes.Add(at);
                }
            }
        }

        return hexes;
    }

    /// <summary>
    /// Why a hex obstructs a side's entry from off board (A2.5; rulings R20.5, R25.2): a Known enemy unit or an enemy vehicle holds it (A4.14), or its
    /// entry cost is not decided; null when it may be entered. Concealed and hidden enemy units do not obstruct it: an entering stack meets them as at any
    /// step (A12.15; ruling R25.3), as it meets Residual FP and Fire Lanes.
    /// </summary>
    internal string? EntryHexBar(GameState state, string side, BoardLocation at, bool vehicle = false)
    {
        if (state.At(at).OfType<UnitInstance>().Any(unit => unit.Status == InstanceStatus.Active && unit.Side != side && !Is(unit, Conditions.Captured)
            && (KnownEnemy(unit) || LiveFire.IsVehicle(unit))))
        {
            return $"play.entry-occupied: {at} holds a Known enemy unit, so it is not entered from off board in the MPh (A2.5, A4.14; ruling R25.2)";
        }

        // Ruling R26.1: a vehicle's entry cost is its own, read with its entry.
        return vehicle || (ReadLocation(state, at) is { } read && TerrainKey(read) is { } terrain && (terrain == "marsh" || InfantryEntryHalfMf(state, terrain) is not null))
            ? null
            : $"play.entry-terrain: the entry cost of {at} is not decided (ruling R20.5)";
    }

    /// <summary>
    /// Why the phasing side's APh may not end (A2.5; rulings R20.5, R25.1): a unit whose entry turn has come still waits off board, did not enter in the
    /// MPh, and may still enter a hex open to it by advance; null when none does. The MPh may end with units waiting, since A2.5 lets a unit capable of
    /// movement in the APh delay its entry until then. Vehicles, which cannot advance, are held in the MPh instead (ruling R26.1).
    /// </summary>
    internal string? EntryDue(GameState state)
    {
        var waiting = new List<string>();
        var open = new List<BoardLocation>();
        foreach (var unit in state.Units.Where(unit => unit.Status == InstanceStatus.Active && unit.Side == state.PhasingSide && unit.Position is OffMapPosition
            && state.Location(unit.Id) is null).OrderBy(unit => unit.Id, StringComparer.Ordinal))
        {
            if (!LiveFire.IsVehicle(unit) && EntryFor(state, unit) is { } entry && entry.Turn <= state.Turn && AdvanceEntries(state, unit, entry) is { Count: > 0 } hexes)
            {
                waiting.Add(unit.Id);
                open.AddRange(hexes.Where(at => !open.Contains(at)));
            }
        }

        // Table player, pass 25: the refusal names every unit still waiting and hexes open to them.
        return waiting.Count == 0 ? null
            : $"play.entry-due: {string.Join(", ", waiting)} {(waiting.Count == 1 ? "waits" : "wait")} off board and must still enter this Game Turn: advance into a hex of the entry edge, such as "
                + string.Join(", ", open.Take(6)) + (open.Count > 6 ? ", ..." : string.Empty) + ", before the APh ends (A2.5; rulings R20.5, R25.1)";
    }

    /// <summary>
    /// The hexes a unit may enter by advance in this APh (ruling R25.1; table player and referee, pass 25): those open to its entry this Game Turn that no Known
    /// enemy unit or enemy vehicle holds, entered across the edge hexside at a decided cost that is not all of a unit's MF (marsh is not entered in the APh, B16.4),
    /// and that the unit itself can advance into: not pinned, with MF left after portage, and not CX into Difficult Terrain (A4.7, A4.72). A unit that cannot
    /// advance into any is "not capable of movement in the APh" (A2.5), so it does not hold the APh; its entry is a Game Turn later.
    /// </summary>
    private List<BoardLocation> AdvanceEntries(GameState state, UnitInstance unit, (int Turn, string Edge, ScenarioCardSetup Area) entry) =>
        Is(unit, Conditions.Pinned) ? []
            : [.. EntryHexesFor(state, entry).Where(at => EntryHexBar(state, unit.Side, at) is null
                && EdgeSides(state, at).FirstOrDefault(item => item.Edge == entry.Edge) is { Edge: not null } crossing
                && EntryGround(state, at, crossing.Side).Entry is { AllMf: false } cost
                && DifficultAdvance(state, unit, cost.HalfMf) is { } difficult && !(difficult && Is(unit, Conditions.Cx)))];

    /// <summary>
    /// The hexes a unit waiting off board may enter in this MPh (A2.5; rulings R20.5, R25.2; table player, pass 25): those open to its entry this Game Turn that
    /// no Known enemy unit or enemy vehicle holds; empty when its entry turn has not come or every such hex is held, which delays it a Game Turn.
    /// </summary>
    public IReadOnlyList<BoardLocation> OpenEntryHexes(GameState state, UnitInstance unit)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(unit);
        return EntryFor(state, unit) is { } entry && entry.Turn <= state.Turn
            ? [.. EntryHexesFor(state, entry).Where(at => EntryHexBar(state, unit.Side, at) is null)]
            : [];
    }

    /// <summary>
    /// The entry of a stack waiting off board (A2.5, A2.51; rulings R20.5, R25.2): every unit enters by the card in this Game Turn or a later one, along one
    /// edge, into a ground-level hex open to it within the playable area; in the MPh not one a Known enemy unit holds (A4.14), which an advance may enter.
    /// Returns the edge and the edge hexside crossed, or why the stack may not enter <paramref name="to"/>.
    /// </summary>
    private (string? Edge, HexsideDirection Side, string? Reason) EntryCheck(GameState state, UnitInstance[] movers, BoardLocation to, bool advancing, bool vehicleEntry = false)
    {
        if (!vehicleEntry && movers.FirstOrDefault(LiveFire.IsVehicle) is { } vehicle)
        {
            return (null, default, $"play.entry-vehicle: {vehicle.Id} is a vehicle; it enters by its own MP expenditure in its MPh, in Motion, not with Infantry or by advance (A2.52, D2.4; ruling R26.1)");
        }

        // A5.1, A2.51 (referee, pass 25): an entering stack keeps to the stacking limits, which offboard setup never exceeds.
        var squads = movers.Sum(unit => vocabulary.IsA(unit.Kind, "asl:squad") ? 1.0 : vocabulary.IsA(unit.Kind, "asl:half-squad") || vocabulary.IsA(unit.Kind, "asl:crew") ? 0.5 : 0.0);
        if (squads > 3 || movers.Count(unit => vocabulary.IsA(unit.Kind, "asl:smc")) > 4)
        {
            return (null, default, "play.entry-stacking: an entering stack holds at most three squad-equivalents and four SMC, the stacking limits offboard setup never exceeds (A5.1, A2.51)");
        }

        var entries = movers.Select(unit => (Unit: unit, Entry: EntryFor(state, unit))).ToArray();
        if (entries.FirstOrDefault(item => item.Entry is null) is { Unit: { } lost })
        {
            return (null, default, $"play.entry: {lost.Id} waits off board with no entry on the card (ruling R20.5)");
        }

        if (entries.FirstOrDefault(item => item.Entry!.Value.Turn > state.Turn) is { Unit: { } early } late)
        {
            return (null, default, $"play.entry-turn: {early.Id} enters on Game Turn {late.Entry!.Value.Turn}, not {state.Turn} (A2.5; ruling R20.5)");
        }

        if (entries.Select(item => item.Entry!.Value.Edge).Distinct(StringComparer.Ordinal).ToArray() is not [var edge])
        {
            return (null, default, "play.entry-stack: a stack enters along one edge; units of different entry edges enter apart (ruling R20.5)");
        }

        if (to.Level != 0 || EdgeSides(state, to).FirstOrDefault(item => item.Edge == edge) is not { Edge: not null } crossing)
        {
            return (null, default, $"play.entry-edge: {string.Join(", ", movers.Select(unit => unit.Id))} enter at ground level in a hex of the {edge} edge, and {to} is not one (A2.51; ruling R20.5)");
        }

        if (PlayableBar(state, to) is { } outside)
        {
            return (null, default, outside);
        }

        // A2.5 (ruling R25.2): named entry hexes, and the four-hex radius a Game Turn later for each turn the entry was blocked.
        foreach (var (unit, entry) in entries)
        {
            if (!EntryHexesFor(state, entry!.Value).Any(at => at == to))
            {
                var named = entry.Value.Area.Hexes is { Count: > 0 } hexes ? string.Join(", ", hexes) : null;

                // Referee, pass 25: a card naming no hex of the edge is said so, not left silent.
                if (named is not null && !EntryHexes(state, edge).Any(at => entry.Value.Area.Hexes!.Contains(at.Hex.ToString(), StringComparer.Ordinal)))
                {
                    return (null, default, $"play.entry-hex: the card names {named} for {unit.Id}'s entry, and none is a hex of the {edge} edge within the playable area (ruling R25.2)");
                }

                return (null, default, named is null
                    ? $"play.entry-edge: {to} is not a hex of the {edge} edge within the playable area (A2.51; ruling R20.5)"
                    : state.Turn == entry.Value.Turn
                        ? $"play.entry-hex: {unit.Id} enters by {named} on Game Turn {entry.Value.Turn}; elsewhere only a Game Turn later, if they are blocked (A2.5; ruling R25.2)"
                        : $"play.entry-hex: {unit.Id} was to enter by {named}; on Game Turn {state.Turn} it enters within {4 * (state.Turn - entry.Value.Turn)} hexes of them along the {edge} edge, never past a river or canal (A2.5; ruling R25.2)");
            }
        }

        return !advancing && EntryHexBar(state, movers[0].Side, to, vehicleEntry) is { } barred ? (null, default, barred) : (edge, crossing.Side, null);
    }

    /// <summary>Why a game from a card may not start play yet (ruling R19.2): a group that sets up on board has not finished; null when it may.</summary>
    internal string? CardSetupIncomplete(GameState state, IReadOnlyList<GameEvent> existing)
    {
        if (existing.Any(item => !GameState.IsSetupEvent(item.Payload)))
        {
            return null;
        }

        // Referee, pass 19: a card changed or gone since the game started cannot say whether the setup is done.
        if (state.Scenario is { } scenario && (CardLibrary.Sha256(scenario.Id) != scenario.Sha256 || CardOf(state) is null))
        {
            return $"play.scenario: the card '{scenario.Id}' {Gone(scenario.Id)}, so its setup cannot be checked (ruling R19.1)";
        }

        if (CardSetup(state, new HashSet<string>()) is not { } report)
        {
            return null;
        }

        return report.Groups.FirstOrDefault(group => group.SetsUp && !group.Complete) is { } open
            ? $"play.setup-incomplete: {open.Name} ({open.Side}) has not finished setting up"
                + (open.Remaining.Count > 0 ? $": {string.Join(", ", open.Remaining.Select(need => $"{need.Count} {need.Definition}{(need.Area is { } area ? $" in {area}" : string.Empty)}"))} left" : string.Empty)
                + " (A2.9; ruling R19.2)"
            : report.Groups.FirstOrDefault(group => group.OffBoard.Count > 0) is { } waiting
            ? $"play.setup-incomplete: {waiting.Name} ({waiting.Side}) still sets up off board to enter: "
                + $"{string.Join(", ", waiting.OffBoard.Select(need => $"{need.Count} {need.Definition}"))} (A2.51; ruling R20.5)"
            : null;
    }
}
