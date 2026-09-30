using LimboDancer.Domains.Asl.Maps.Coordinates;
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
            counters.Add(new SetupCounter(unit.Id, unit.Side, unit.Group, unit.Definition?.Definition, unit.Kind, state.Location(unit.Id)?.Location,
                Is(unit, Conditions.Concealed), Is(unit, Conditions.Hidden), unit.Kind == UnitKinds.Dummy, false, placedNow.Contains(unit.Id))
            {
                OffBoard = unit.Position is OffMapPosition && state.Location(unit.Id) is null,
                Broken = Is(unit, Conditions.Broken),
            });
        }

        foreach (var item in state.Equipment.Where(item => item.Status == InstanceStatus.Active))
        {
            // A SW belongs to its holder's group, at its holder's Location; equipment on its own belongs to no group (referee, pass 19).
            var holder = item.Holding is { } holding ? state.Unit(holding.Holder) : null;
            counters.Add(new SetupCounter(item.Id, holder?.Side ?? item.Side ?? string.Empty, holder?.Group, item.Definition?.Definition, item.Kind,
                holder is not null ? state.Location(holder.Id)?.Location : (item.Position as MapPosition)?.Location, false, false, false, true, placedNow.Contains(item.Id))
            {
                OffBoard = holder is not null && holder.Position is OffMapPosition && state.Location(holder.Id) is null,
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
    /// The Game Turn and edge a unit waiting off board enters by (ruling R20.5): its OB group's entry for its line (a HS its squad's, a Balance counter
    /// its group's first); null when the card gives none.
    /// </summary>
    public (int Turn, string Edge)? EntryFor(GameState state, UnitInstance unit)
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
        var entry = group.Units.Where(line => line.Definition == definition || line.Definition == squad).Select(line => ScenarioSetup.EntryOf(group, line)).FirstOrDefault(area => area is not null)
            ?? group.Areas.Where(area => area.Kind == "entry").OrderBy(area => area.Turn).FirstOrDefault();
        return entry is { Turn: { } turn, Edge: { } edge } ? (turn, edge) : null;
    }

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
    /// Why a stack of a side may not enter a hex from off board (ruling R20.5): it holds enemy units (A4.14; concealed ones are not built at entry),
    /// Residual FP or a Fire Lane (not built at entry), or terrain whose entry cost is not decided; null when it may.
    /// </summary>
    internal string? EntryHexBar(GameState state, string side, BoardLocation at)
    {
        if (state.At(at).OfType<UnitInstance>().Any(unit => unit.Status == InstanceStatus.Active && unit.Side != side && !Is(unit, Conditions.Captured)))
        {
            return $"play.entry-occupied: {at} holds enemy units; Infantry may not enter a Known enemy's Location in the MPh, and entering concealed ones from off board is not built (A4.14, A12.15; ruling R20.5)";
        }

        if (state.ResidualFire.Any(item => item.Location == at) || state.FireLanes.Any(lane => lane.Entries.Any(item => item.Location == at)))
        {
            return $"play.entry-residual: {at} holds Residual FP; entering it from off board is not built (A8.22; ruling R20.5)";
        }

        return ReadLocation(state, at) is { } read && TerrainKey(read) is { } terrain && InfantryEntryHalfMf(state, terrain) is not null
            ? null
            : $"play.entry-terrain: the entry cost of {at} is not decided (ruling R20.5)";
    }

    /// <summary>
    /// Why the phasing side's MPh may not end (A2.5; ruling R20.5): a unit whose entry turn has come still waits off board, and a hex of its edge within the
    /// playable area may be entered; null when none does. Vehicles, which cannot enter yet, are not held (referee, pass 20).
    /// </summary>
    internal string? EntryDue(GameState state)
    {
        foreach (var unit in state.Units.Where(unit => unit.Status == InstanceStatus.Active && unit.Side == state.PhasingSide && unit.Position is OffMapPosition
            && state.Location(unit.Id) is null).OrderBy(unit => unit.Id, StringComparer.Ordinal))
        {
            if (!LiveFire.IsVehicle(unit) && EntryFor(state, unit) is { } entry && entry.Turn <= state.Turn
                && EntryHexes(state, entry.Edge).Any(at => EntryHexBar(state, unit.Side, at) is null))
            {
                return $"play.entry-due: {unit.Id} was to enter on Game Turn {entry.Turn} along the {entry.Edge} edge and enters in this MPh (A2.5; ruling R20.5)";
            }
        }

        return null;
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
