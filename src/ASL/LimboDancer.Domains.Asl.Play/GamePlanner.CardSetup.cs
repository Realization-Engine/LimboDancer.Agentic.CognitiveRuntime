using LimboDancer.Domains.Asl.Maps.Coordinates;
using LimboDancer.Domains.Asl.Maps.Geometry;
using LimboDancer.Domains.Asl.Rules;
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
            && CachedCard(scenario.Id, catalog) is { } card && ScenarioCards.SameCard(scenario.Id, scenario.Sha256, card.Sha256)
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
                OffBoard = ScenarioA1SetupCalculator.SetupCounterOffBoard(unit.Position is OffMapPosition or ContainedPosition, state.Location(unit.Id) is not null),
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
            counters.Add(new SetupCounter(item.Id, ScenarioA1SetupCalculator.EquipmentSetupSide(holder?.Side, item.Side), holder?.Group, item.Definition?.Definition, item.Kind,
                holder is not null ? state.Location(holder.Id)?.Location : (item.Position as MapPosition)?.Location, false,
                ScenarioA1SetupCalculator.EquipmentHiddenOfItsOwn(Is(item, Conditions.Hidden), item.Holding is { Role: HoldingRole.Possessed }), false, true, placedNow.Contains(item.Id))
            {
                OffBoard = holder is not null && ScenarioA1SetupCalculator.SetupCounterOffBoard(holder.Position is OffMapPosition or ContainedPosition, state.Location(holder.Id) is not null),
                Manning = item.Holding is { Role: HoldingRole.Manned },
                Towed = item.Holding is { Role: HoldingRole.Towed },
            });
        }

        return ScenarioSetup.Check(card, counters, at => ReadLocation(state, at) is { } read ? TerrainKey(read) : null,
            key => ScenarioA1SetupCalculator.InfantryCouldEnter(key, terrain => InfantryEntryHalfMf(state, terrain) is not null), ScenarioA1FireReference.HalfSquadOf, state.ScenarioMonth,
            state.Scenario?.Balance);
    }

    /// <summary>Why a Location is refused as outside the card's playable area (A2.1; ruling R20.6); null when it is inside, or the game has no card.</summary>
    internal string? PlayableBar(GameState state, BoardLocation at)
    {
        // The card is read once a call, as before (the review of pass 32.j: the library checks a user card's file on every read).
        var card = state.Scenario is not null ? CardOf(state) : null;
        return ScenarioA1SetupCalculator.PlayableBar(card is not null && !ScenarioCards.Playable(card, at), at.ToString(), card?.PlayableArea?.Text);
    }

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
        return ScenarioA1EntryCalculator.EntryFor(GroupFacts(groupId, group), (unit.Position as OffMapPosition)?.Entry, definition, squad) is { } entry
            ? (entry.Turn, entry.Edge, group.Areas.First(area => area.Id == entry.AreaId))
            : null;
    }

    /// <summary>An OB group as Rules reads it (pass 32.j).</summary>
    private static SetupGroupFacts GroupFacts(string id, ScenarioCardGroup group) => new(id, group.Name, group.SetupOrder, group.Dummies,
        [.. group.Areas.Select(area => new SetupAreaFacts(area.Id, area.Kind, area.Hexes, area.Board, area.From, area.To, area.Turn, area.Edge, area.Counters, area.MinMmc, area.Concealed))],
        [.. group.Units.Select(line => new SetupLineFacts(line.Definition, line.Count, line.Area))]);

    /// <summary>
    /// The hexes a stack waiting off board may enter in this Game Turn (A2.5; rulings R20.5, R25.2): every hex of its edge within the playable area; or,
    /// when its entry area names its entry hexes, those hexes on its entry turn, and each Game Turn later (its entry blocked) the hexes of the edge
    /// within four more hexes of them, never past a river or canal on the edge.
    /// </summary>
    public IReadOnlyList<BoardLocation> EntryHexesFor(GameState state, (int Turn, string Edge, ScenarioCardSetup Area) entry)
    {
        ArgumentNullException.ThrowIfNull(state);
        var edge = EntryHexes(state, entry.Edge);
        var card = CardOf(state);
        var board = entry.Area.Board ?? (card?.Boards.Count == 1 ? card.Boards[0].Board : null);
        return [.. ScenarioA1EntryCalculator.EntryHexesFor(edge.Count, entry.Area.Hexes is { Count: > 0 } && card is not null,
            at => edge[at].Board.Value == board && entry.Area.Hexes!.Contains(edge[at].Hex.ToString(), StringComparer.Ordinal), state.Turn, entry.Turn,
            at => IsRiver(state, edge[at]), (one, two) => HexDistance(state, edge[one], edge[two])).Select(at => edge[at])];
    }

    /// <summary>Whether a hex is a river or canal (B21), which delayed entry never crosses along the edge (A2.5).</summary>
    private bool IsRiver(GameState state, BoardLocation at) => ScenarioA1EntryCalculator.IsRiver(ReadLocation(state, at)?.Hex.Center.Terrain?.Name);

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
                if (ScenarioA1EntryCalculator.EntryHex(EdgeSides(state, at).Any(item => item.Edge == edge), PlayableBar(state, at) is null))
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
    internal string? EntryHexBar(GameState state, string side, BoardLocation at, bool vehicle = false) =>
        ScenarioA1EntryCalculator.EntryHexBar(state.At(at).OfType<UnitInstance>().Any(unit => ScenarioA1EntryCalculator.EntryObstructor(unit.Status == InstanceStatus.Active, unit.Side != side,
                Is(unit, Conditions.Captured), KnownEnemy(unit), LiveFire.IsVehicle(unit))), at.ToString(), vehicle,
            () => ReadLocation(state, at) is { } read && TerrainKey(read) is { } terrain && (terrain == "marsh" || InfantryEntryHalfMf(state, terrain) is not null));

    /// <summary>
    /// Why the phasing side's APh may not end (A2.5; rulings R20.5, R25.1): a unit whose entry turn has come still waits off board, did not enter in the
    /// MPh, and may still enter a hex open to it by advance; null when none does. The MPh may end with units waiting, since A2.5 lets a unit capable of
    /// movement in the APh delay its entry until then. Vehicles, which cannot advance, are held in the MPh instead (ruling R26.1).
    /// </summary>
    internal string? EntryDue(GameState state) =>
        ScenarioA1EntryCalculator.EntryDue([.. state.Units.Where(unit => unit.Status == InstanceStatus.Active && unit.Side == state.PhasingSide && unit.Position is OffMapPosition
                && state.Location(unit.Id) is null).OrderBy(unit => unit.Id, StringComparer.Ordinal)
            .Select(unit =>
            {
                var vehicle = LiveFire.IsVehicle(unit);
                var entry = vehicle ? null : EntryFor(state, unit);
                return new EntryWaitFacts(unit.Id, vehicle, entry?.Turn, () => [.. AdvanceEntries(state, unit, entry!.Value).Select(at => at.ToString())]);
            })], state.Turn);

    /// <summary>
    /// The hexes a unit may enter by advance in this APh (ruling R25.1; table player and referee, pass 25): those open to its entry this Game Turn that no Known
    /// enemy unit or enemy vehicle holds, entered across the edge hexside at a decided cost that is not all of a unit's MF (marsh is not entered in the APh, B16.4),
    /// and that the unit itself can advance into: not pinned, with MF left after portage, and not CX into Difficult Terrain (A4.7, A4.72). A unit that cannot
    /// advance into any is "not capable of movement in the APh" (A2.5), so it does not hold the APh; its entry is a Game Turn later.
    /// </summary>
    private List<BoardLocation> AdvanceEntries(GameState state, UnitInstance unit, (int Turn, string Edge, ScenarioCardSetup Area) entry)
    {
        IReadOnlyList<BoardLocation> open = [];
        HexsideDirection crossing = default;
        return [.. ScenarioA1EntryCalculator.AdvanceEntries(Is(unit, Conditions.Pinned), Is(unit, Conditions.Cx), () => (open = EntryHexesFor(state, entry)).Count,
            at => EntryHexBar(state, unit.Side, open[at]) is not null,
            at =>
            {
                var found = EdgeSides(state, open[at]).FirstOrDefault(item => item.Edge == entry.Edge);
                if (found.Edge is null)
                {
                    return false;
                }

                crossing = found.Side;
                return true;
            },
            at => EntryGround(state, open[at], crossing).Entry is { } cost ? (cost.HalfMf, cost.AllMf) : null,
            halfMf => DifficultAdvance(state, unit, halfMf)).Select(at => open[at])];
    }

    /// <summary>
    /// The hexes a unit waiting off board may enter in this MPh (A2.5; rulings R20.5, R25.2; table player, pass 25): those open to its entry this Game Turn that
    /// no Known enemy unit or enemy vehicle holds; empty when its entry turn has not come or every such hex is held, which delays it a Game Turn.
    /// </summary>
    public IReadOnlyList<BoardLocation> OpenEntryHexes(GameState state, UnitInstance unit)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(unit);
        var entry = EntryFor(state, unit);
        IReadOnlyList<BoardLocation> open = [];
        return [.. ScenarioA1EntryCalculator.OpenEntryHexes(entry?.Turn, state.Turn, () => (open = EntryHexesFor(state, entry!.Value)).Count,
            at => EntryHexBar(state, unit.Side, open[at]) is not null).Select(at => open[at])];
    }

    /// <summary>
    /// The entry of a stack waiting off board (A2.5, A2.51; rulings R20.5, R25.2): every unit enters by the card in this Game Turn or a later one, along one
    /// edge, into a ground-level hex open to it within the playable area; in the MPh not one a Known enemy unit holds (A4.14), which an advance may enter.
    /// Returns the edge and the edge hexside crossed, or why the stack may not enter <paramref name="to"/>.
    /// </summary>
    private (string? Edge, HexsideDirection Side, string? Reason) EntryCheck(GameState state, UnitInstance[] movers, BoardLocation to, bool advancing, bool vehicleEntry = false)
    {
        // The entries are card and catalog reads, made for every mover before the vehicle and stacking checks now (pure; the old body read them after).
        var entries = movers.Select(unit => EntryFor(state, unit)).ToArray();
        var (edge, side, reason) = ScenarioA1EntryCalculator.EntryCheck(
            [.. movers.Select((unit, index) => new OffBoardMoverFacts(unit.Id, LiveFire.IsVehicle(unit),
                vocabulary.IsA(unit.Kind, "asl:squad") ? 1.0 : vocabulary.IsA(unit.Kind, "asl:half-squad") || vocabulary.IsA(unit.Kind, "asl:crew") ? 0.5 : 0.0,
                vocabulary.IsA(unit.Kind, "asl:smc"), entries[index] is { } entry ? (entry.Turn, entry.Edge, entry.Area.Hexes) : null))],
            to.ToString(), to.Level, state.Turn, advancing, vehicleEntry,
            onEdge => EdgeSides(state, to).FirstOrDefault(item => item.Edge == onEdge) is { Edge: not null } crossing ? (int)crossing.Side : null,
            () => PlayableBar(state, to),
            index => EntryHexesFor(state, entries[index]!.Value).Any(at => at == to),
            index => EntryHexes(state, entries[index]!.Value.Edge).Any(at => entries[index]!.Value.Area.Hexes!.Contains(at.Hex.ToString(), StringComparer.Ordinal)),
            () => EntryHexBar(state, movers[0].Side, to, vehicleEntry));
        return (edge, (HexsideDirection)side, reason);
    }

    /// <summary>Why a game from a card may not start play yet (ruling R19.2): a group that sets up on board has not finished; null when it may.</summary>
    internal string? CardSetupIncomplete(GameState state, IReadOnlyList<GameEvent> existing) =>
        ScenarioA1SetupCalculator.SetupIncompleteBar(existing.Any(item => !GameState.IsSetupEvent(item.Payload)),
            () => state.Scenario is { } scenario && (!CardLibrary.Matches(scenario.Id, scenario.Sha256) || CardOf(state) is null),
            () => $"play.scenario: the card '{state.Scenario!.Id}' {Gone(state.Scenario.Id)}, so its setup cannot be checked (ruling R19.1)",
            () => CardSetup(state, new HashSet<string>()) is { } report ? [.. report.Groups.Select(Verdict)] : null);

    /// <summary>A group's standing as Rules reads it (pass 32.j).</summary>
    private static SetupGroupVerdict Verdict(SetupGroup group) => new(group.Side, group.Id, group.Name, group.Order, group.SetsUp, group.Complete,
        [.. group.Remaining.Select(need => new SetupNeedVerdict(need.Group, need.Area, need.Definition, need.Count))], group.DummiesLeft,
        [.. group.OffBoard.Select(need => new SetupNeedVerdict(need.Group, need.Area, need.Definition, need.Count))], group.Enters);
}
