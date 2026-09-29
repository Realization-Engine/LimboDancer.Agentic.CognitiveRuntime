using LimboDancer.Domains.Asl.ScenarioA1;
using LimboDancer.Domains.Asl.Units.State;

namespace LimboDancer.Domains.Asl.Play;

/// <summary>
/// The setup of a game from a scenario card (pass 19 of the Scenario Card Games Plan; rulings R19.1 to R19.6): the planner's reading of the game for
/// <see cref="ScenarioSetup"/>, which checks each setup proposal and the start of play.
/// </summary>
public sealed partial class GamePlanner
{
    /// <summary>The card a game starts from, as it is embedded now; null for a game that names none.</summary>
    internal ScenarioCard? CardOf(GameState state)
    {
        ArgumentNullException.ThrowIfNull(state);
        return state.Scenario is { } scenario && catalogs.FirstOrDefault(catalog => catalog.Identity == state.Catalog) is { } catalog
            ? ScenarioCards.Read(scenario.Id, catalog)?.Card
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
        if (CardOf(state) is not { } card)
        {
            return null;
        }

        var counters = new List<SetupCounter>();
        foreach (var unit in state.Units.Where(unit => unit.Status == InstanceStatus.Active))
        {
            counters.Add(new SetupCounter(unit.Id, unit.Side, unit.Group, unit.Definition?.Definition, unit.Kind, state.Location(unit.Id)?.Location,
                Is(unit, Conditions.Concealed), Is(unit, Conditions.Hidden), unit.Kind == UnitKinds.Dummy, false, placedNow.Contains(unit.Id)));
        }

        foreach (var item in state.Equipment.Where(item => item.Status == InstanceStatus.Active))
        {
            // A SW belongs to its holder's group, at its holder's Location; equipment on its own belongs to no group (referee, pass 19).
            var holder = item.Holding is { } holding ? state.Unit(holding.Holder) : null;
            counters.Add(new SetupCounter(item.Id, holder?.Side ?? item.Side ?? string.Empty, holder?.Group, item.Definition?.Definition, item.Kind,
                holder is not null ? state.Location(holder.Id)?.Location : (item.Position as MapPosition)?.Location, false, false, false, true, placedNow.Contains(item.Id)));
        }

        return ScenarioSetup.Check(card, counters, at => ReadLocation(state, at) is { } read ? TerrainKey(read) : null,
            key => key is "marsh" || (key is not null && InfantryEntryHalfMf(state, key) is not null), ScenarioA1FireReference.HalfSquadOf, state.ScenarioMonth);
    }

    /// <summary>Why a game from a card may not start play yet (ruling R19.2): a group that sets up on board has not finished; null when it may.</summary>
    internal string? CardSetupIncomplete(GameState state, IReadOnlyList<GameEvent> existing)
    {
        if (existing.Any(item => item.Payload is not (GameStarted or InstanceCreated or BoreSighted)))
        {
            return null;
        }

        // Referee, pass 19: a card changed or gone since the game started cannot say whether the setup is done.
        if (state.Scenario is { } scenario && (ScenarioCards.Sha256(scenario.Id) != scenario.Sha256 || CardOf(state) is null))
        {
            return $"play.scenario: the card '{scenario.Id}' has changed since the game started, so its setup cannot be checked (ruling R19.1)";
        }

        if (CardSetup(state, new HashSet<string>()) is not { } report)
        {
            return null;
        }

        return report.Groups.FirstOrDefault(group => group.SetsUp && !group.Complete) is { } open
            ? $"play.setup-incomplete: {open.Name} ({open.Side}) has not finished setting up"
                + (open.Remaining.Count > 0 ? $": {string.Join(", ", open.Remaining.Select(need => $"{need.Count} {need.Definition}{(need.Area is { } area ? $" in {area}" : string.Empty)}"))} left" : string.Empty)
                + " (A2.9; ruling R19.2)"
            : null;
    }
}
