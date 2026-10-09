using System.Text.Json;
using LimboDancer.Domains.Asl.Maps.Coordinates;
using LimboDancer.Domains.Asl.Maps.Los;
using LimboDancer.Domains.Asl.Rules;
using LimboDancer.Domains.Asl.Units.State;

namespace LimboDancer.Domains.Asl.Play;

/// <summary>
/// What a side may see at setup (pass 23 of the Card Play and Map Studio Redesign Plan; rulings R23.3 and R23.6): the sides setting up out of its sight,
/// and the non-OB "?" each side places once both have set up (A12.12).
/// </summary>
public sealed partial class GamePlanner
{
    /// <summary>
    /// The OB groups a perspective may not see at all now (A12.12: "The player setting up first in a scenario does so out of vision of his opponent"; ruling
    /// R23.3): during the setup of a game from a card, the enemy groups of the order setting up now; an enemy group of an earlier order, already seen, stays
    /// seen (referee, pass 23). Empty for the adjudicator, once play has started, and for a game with no OB, whose players set up on the honor system.
    /// </summary>
    public IReadOnlySet<string> OutOfSight(GameState state, Perspective viewer)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(viewer);
        return ScenarioA1SetupCalculator.OutOfSight(viewer.IsAdjudicator, state.SetupClosed, viewer.Name,
            () => CardSetup(state, new HashSet<string>()) is { } report ? (report.CurrentOrder, [.. report.Groups.Select(Verdict)]) : null).ToHashSet(StringComparer.Ordinal);
    }

    /// <summary>
    /// The Locations where a side may place a non-OB "?" now (A12.12; ruling R23.6): each holding its units on the map, none of them under "?" or hidden, out
    /// of the LOS of every unbroken enemy ground unit within 16 hexes of it or at 17 hexes or more from all of them, once every group of a card that sets up
    /// on board has set up and before play starts. Hidden enemy units are not on the board, so they are not counted (A12.3).
    /// </summary>
    public IReadOnlyList<BoardLocation> NonObConcealment(GameState state, string side)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(side);
        IReadOnlyList<BoardLocation> own = [];
        var texts = ScenarioA1Concealment.NonObConcealment(() => NonObConcealmentBar(state) is not null,
            () => [.. (own = [.. state.Units.Where(unit => unit.Status == InstanceStatus.Active && unit.Side == side && state.Location(unit.Id) is not null)
                .Select(unit => state.Location(unit.Id)!.Location).Distinct()]).Select(at => at.ToString())],
            at => NonObLocationBar(state, side, own[at]) is not null);
        var byText = own.ToDictionary(at => at.ToString(), at => at, StringComparer.Ordinal);
        return [.. texts.Select(text => byText[text])];
    }

    /// <summary>Why no non-OB "?" may be placed now (ruling R23.6); null when they may.</summary>
    private string? NonObConcealmentBar(GameState state) =>
        ScenarioA1Concealment.NonObConcealmentBar(state.SetupClosed, () => CardSetup(state, new HashSet<string>()) is { CurrentOrder: not null });

    /// <summary>Why a side may not place a non-OB "?" on its stack at a Location (A12.12; ruling R23.6); null when it may.</summary>
    private string? NonObLocationBar(GameState state, string side, BoardLocation at)
    {
        var mine = state.At(at).OfType<UnitInstance>().Where(unit => unit.Status == InstanceStatus.Active && unit.Side == side).ToArray();
        return ScenarioA1Concealment.NonObLocationBar([.. mine.Select(unit => (Is(unit, Conditions.Broken), Is(unit, Conditions.Berserk), unit.Kind == UnitKinds.Dummy,
            Is(unit, Conditions.Concealed), Is(unit, Conditions.Hidden)))], side, at.ToString(), new NonObReader(this, state, side, at));
    }

    /// <summary>The non-OB "?" scan's reads (pass 32.j): the units with their Locations as indexes into a table, the distance and the LOS read as Rules asks.</summary>
    private sealed class NonObReader : INonObConcealmentReader
    {
        private readonly GamePlanner planner;
        private readonly GameState state;
        private readonly BoardLocation at;
        private readonly List<BoardLocation> locations = [];

        public NonObReader(GamePlanner planner, GameState state, string side, BoardLocation at)
        {
            this.planner = planner;
            this.state = state;
            this.at = at;
            var indexes = new Dictionary<BoardLocation, int>();
            Units = [.. state.Units.Select(unit =>
            {
                int? location = null;
                if (state.Location(unit.Id)?.Location is { } here)
                {
                    if (!indexes.TryGetValue(here, out var index))
                    {
                        index = locations.Count;
                        indexes[here] = index;
                        locations.Add(here);
                    }

                    location = index;
                }

                return new NonObUnitFacts(unit.Status == InstanceStatus.Active, unit.Side != side, unit.Kind == UnitKinds.Dummy, Is(unit, Conditions.Broken), Is(unit, Conditions.Hidden),
                    Is(unit, Conditions.Captured), location);
            })];
        }

        public IReadOnlyList<NonObUnitFacts> Units
        {
            get;
        }

        public int? Distance(int location) => planner.HexDistance(state, locations[location], at);

        public NonObLosFacts? Los(int location) => planner.Los(state, locations[location], at) is { } los ? new NonObLosFacts(los.Status == LosStatus.Blocked, los.Status == LosStatus.Clear, los.Range) : null;
    }

    /// <summary>
    /// Hidden units placed on the map beneath a "?" (A12.32; ruling R23.5): all of one side, hidden, on the map; each becomes concealed, seen only by its
    /// side, so the enemy learns only that a "?" is there. A hidden unit must be placed so before it moves or advances; it may be at any time.
    /// </summary>
    private GamePlan PlanPlaceHidden(GameScope scope, JsonElement arguments, IReadOnlyList<GameEvent> existing, string attemptId, long expected, string label)
    {
        if (Replay(existing).Current is not { } state)
        {
            return Refused(scope, label, expected, "play.no-game");
        }

        if (ScenarioA1Concealment.PlaceHiddenSetupBar(existing.All(item => GameState.IsSetupEvent(item.Payload))) is { } setupBar)
        {
            return Refused(scope, label, expected, setupBar);
        }

        var ids = Strings(arguments, "unitIds").Distinct(StringComparer.Ordinal).ToArray();
        var units = ids.Select(state.Unit).ToArray();
        if (ScenarioA1Concealment.PlaceHiddenUnitsBar(ids.Length, units.Any(unit => unit is not { Status: InstanceStatus.Active } || !Is(unit, Conditions.Hidden) || state.Location(unit.Id) is null),
            ids.Length == 0 ? 0 : units.Select(unit => unit?.Side).Distinct().Count()) is { } unitsBar)
        {
            return Refused(scope, label, expected, unitsBar);
        }

        var side = units[0]!.Side;
        GameEvent[] events = [.. units.Select((unit, index) => Event(scope, attemptId, index + 1, expected, "hidden-placed", new ConditionsChanged(unit!.Id,
            ConditionChanges(ScenarioA1Concealment.PlaceHiddenConditions())), null, [side]))];
        return new GamePlan(GamePlanStatus.Ready, scope, label, expected, events, [ScenarioA1Concealment.PlaceHiddenText(ids)]);
    }

    /// <summary>
    /// A setup that places non-OB "?" (A12.12; ruling R23.6): <c>conceal</c> names a side and its Locations; each of its units there gains "?", seen only
    /// by that side, in a setup event that does not start play.
    /// </summary>
    private GamePlan PlanNonObConcealment(GameScope scope, JsonElement conceal, IReadOnlyList<GameEvent> existing, string attemptId, long expected, string label)
    {
        var replayed = existing.Count == 0 ? null : Replay(existing).Current;
        if (ScenarioA1Concealment.NonObGameBar(existing.Count > 0, replayed is not null) is { } gameBar)
        {
            return Refused(scope, label, expected, gameBar);
        }

        var state = replayed!;

        if (conceal.ValueKind != JsonValueKind.Object || !conceal.TryGetProperty("side", out var sideElement) || sideElement.ValueKind != JsonValueKind.String
            || state.Side(sideElement.GetString()!) is null || !conceal.TryGetProperty("locations", out var list) || list.ValueKind != JsonValueKind.Array
            || list.GetArrayLength() == 0)
        {
            return Refused(scope, label, expected, "play.invalid-arguments: conceal names a side of the game and a list of Locations");
        }

        if (NonObConcealmentBar(state) is { } bar)
        {
            return Refused(scope, label, expected, bar);
        }

        var side = sideElement.GetString()!;
        var events = new List<GameEvent>();
        var reasons = new List<string>();
        foreach (var item in list.EnumerateArray())
        {
            if (item.ValueKind != JsonValueKind.String || !BoardLocation.TryParse(item.GetString()!, out var at))
            {
                return Refused(scope, label, expected, "play.invalid-arguments: each Location to conceal is a Location of the map");
            }

            if (NonObLocationBar(state, side, at) is { } refused)
            {
                reasons.Add(refused);
                continue;
            }

            foreach (var unit in state.At(at).OfType<UnitInstance>().Where(unit => unit.Status == InstanceStatus.Active && unit.Side == side)
                .OrderBy(unit => unit.Id, StringComparer.Ordinal))
            {
                events.Add(Event(scope, attemptId, events.Count + 1, expected, "setup-concealed", new SetupConcealed(unit.Id), null, [side]));
            }
        }

        return reasons.Count > 0
            ? Refused(scope, label, expected, [.. reasons])
            : new GamePlan(GamePlanStatus.Ready, scope, label, expected, events, [ScenarioA1Concealment.NonObConcealmentText(side, [.. events.Select(item => item.EventId)])]);
    }
}
