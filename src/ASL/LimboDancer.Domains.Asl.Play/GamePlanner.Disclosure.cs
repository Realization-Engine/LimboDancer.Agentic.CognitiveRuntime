using System.Text.Json;
using LimboDancer.Domains.Asl.Maps.Coordinates;
using LimboDancer.Domains.Asl.Maps.Los;
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
        if (viewer.IsAdjudicator || state.SetupClosed || CardSetup(state, new HashSet<string>()) is not { CurrentOrder: { } now } report)
        {
            return new HashSet<string>(StringComparer.Ordinal);
        }

        return report.Groups.Where(group => group.SetsUp && group.Order == now && group.Side != viewer.Name).Select(group => group.Id)
            .ToHashSet(StringComparer.Ordinal);
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
        if (NonObConcealmentBar(state) is not null)
        {
            return [];
        }

        return [.. state.Units.Where(unit => unit.Status == InstanceStatus.Active && unit.Side == side && state.Location(unit.Id) is not null)
            .Select(unit => state.Location(unit.Id)!.Location).Distinct().Where(at => NonObLocationBar(state, side, at) is null)
            .OrderBy(at => at.ToString(), StringComparer.Ordinal)];
    }

    /// <summary>Why no non-OB "?" may be placed now (ruling R23.6); null when they may.</summary>
    private string? NonObConcealmentBar(GameState state)
    {
        if (state.SetupClosed)
        {
            return "play.non-ob-concealment: play has started; a non-OB \"?\" is placed only after both sides set up and before play (A12.12; ruling R23.6)";
        }

        return CardSetup(state, new HashSet<string>()) is { CurrentOrder: not null }
            ? "play.non-ob-concealment: a non-OB \"?\" is placed only after both sides have set up (A12.12; ruling R23.6)"
            : null;
    }

    /// <summary>Why a side may not place a non-OB "?" on its stack at a Location (A12.12; ruling R23.6); null when it may.</summary>
    private string? NonObLocationBar(GameState state, string side, BoardLocation at)
    {
        var mine = state.At(at).OfType<UnitInstance>().Where(unit => unit.Status == InstanceStatus.Active && unit.Side == side).ToArray();
        if (mine.Length == 0)
        {
            return $"play.non-ob-concealment: {side} has no unit at {at} (A12.12; ruling R23.6)";
        }

        if (mine.Any(unit => Is(unit, Conditions.Broken) || Is(unit, Conditions.Berserk)))
        {
            return $"play.non-ob-concealment: a unit of {side} at {at} is broken or berserk, and such a unit never gains \"?\" (A12.12; ruling R23.6)";
        }

        if (mine.Any(unit => unit.Kind == UnitKinds.Dummy || Is(unit, Conditions.Concealed) || Is(unit, Conditions.Hidden)))
        {
            return $"play.non-ob-concealment: {at} already holds a \"?\" or a hidden unit of {side}; one non-OB \"?\" per stack, never on top of another \"?\" (A12.12; ruling R23.6)";
        }

        foreach (var enemyAt in state.Units.Where(unit => unit.Status == InstanceStatus.Active && unit.Side != side && unit.Kind != UnitKinds.Dummy
                && !Is(unit, Conditions.Broken) && !Is(unit, Conditions.Hidden) && !Is(unit, Conditions.Captured))
            .Select(unit => state.Location(unit.Id)?.Location).OfType<BoardLocation>().Distinct())
        {
            // 17 hexes or more from the enemy unit needs no LOS read.
            if (HexDistance(state, enemyAt, at) is >= 17)
            {
                continue;
            }

            var los = Los(state, enemyAt, at);
            if (los is null || (los.Status != LosStatus.Blocked && los.Range <= 16))
            {
                return los is { Status: LosStatus.Clear }
                    ? $"play.non-ob-concealment: {at} is in the LOS of an unbroken enemy ground unit within 16 hexes (A12.12; ruling R23.6)"
                    : $"play.non-ob-concealment: the LOS of an enemy unit within 16 hexes to {at} is not decided (A12.12; ruling R23.6)";
            }
        }

        return null;
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

        if (existing.All(item => GameState.IsSetupEvent(item.Payload)))
        {
            return Refused(scope, label, expected, "play.place-hidden: during setup a unit is set up hidden or not; placing it beneath \"?\" comes with play (A12.32; ruling R23.5)");
        }

        var ids = Strings(arguments, "unitIds").Distinct(StringComparer.Ordinal).ToArray();
        var units = ids.Select(state.Unit).ToArray();
        if (ids.Length == 0 || units.Any(unit => unit is not { Status: InstanceStatus.Active } || !Is(unit, Conditions.Hidden) || state.Location(unit.Id) is null)
            || units.Select(unit => unit!.Side).Distinct().Count() != 1)
        {
            return Refused(scope, label, expected, "play.place-hidden: the units are hidden units of one side, on the map (A12.32; ruling R23.5)");
        }

        var side = units[0]!.Side;
        GameEvent[] events = [.. units.Select((unit, index) => Event(scope, attemptId, index + 1, expected, "hidden-placed", new ConditionsChanged(unit!.Id,
            new Dictionary<string, ConditionState> { [Conditions.Hidden] = ConditionState.False, [Conditions.Concealed] = ConditionState.True }), null, [side]))];
        return new GamePlan(GamePlanStatus.Ready, scope, label, expected, events,
            [$"play.place-hidden: {string.Join(", ", ids)} placed beneath \"?\" (A12.32; ruling R23.5)"]);
    }

    /// <summary>
    /// A setup that places non-OB "?" (A12.12; ruling R23.6): <c>conceal</c> names a side and its Locations; each of its units there gains "?", seen only
    /// by that side, in a setup event that does not start play.
    /// </summary>
    private GamePlan PlanNonObConcealment(GameScope scope, JsonElement conceal, IReadOnlyList<GameEvent> existing, string attemptId, long expected, string label)
    {
        if (existing.Count == 0 || Replay(existing).Current is not { } state)
        {
            return Refused(scope, label, expected, "play.non-ob-concealment: a non-OB \"?\" goes on a game already set up (A12.12; ruling R23.6)");
        }

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
            : new GamePlan(GamePlanStatus.Ready, scope, label, expected, events,
                [$"play.non-ob-concealment: the {side} side places {events.Count} {(events.Count == 1 ? "unit" : "units")} under \"?\" (A12.12; ruling R23.6)"]);
    }
}
