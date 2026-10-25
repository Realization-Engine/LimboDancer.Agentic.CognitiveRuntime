using System.Text.Json;
using LimboDancer.Domains.Asl.Maps.Coordinates;
using LimboDancer.Domains.Asl.Rules;
using LimboDancer.Domains.Asl.Units.State;

namespace LimboDancer.Domains.Asl.Play;

/// <summary>
/// Voluntary Break and Voluntary Rout (pass 70, task 70.1; rulings R70.11, R70.12): the facts of a unit that would break at the start of the RtPh, the
/// Known enemy units that allow it, and the leaders a broken unit may rout with. Rules decides each; the planner reads the state's index and the rout's
/// fact reader, and writes the events.
/// </summary>
public sealed partial class GamePlanner
{
    /// <summary>
    /// Why a unit may not break voluntarily now (A10.3, A10.41; ruling R70.11), or null when it may: Rules decides over the unit's facts and the Known enemy
    /// units the rout reads, read last. <paramref name="side"/> is the side breaking, the unit's own when null.
    /// </summary>
    public string? VoluntaryBreakBar(GameState state, UnitInstance? unit, string id, string? side = null)
    {
        ArgumentNullException.ThrowIfNull(state);
        var index = MemoOf(state).Objects(state);
        var at = unit is null ? null : index.Location(unit.Id)?.Location;
        var facts = new VoluntaryBreakFacts(id, unit is { Status: InstanceStatus.Active } && at is not null, unit is not null && unit.Side == (side ?? unit.Side),
            unit is not null && vocabulary.IsA(unit.Kind, "asl:personnel"), unit is not null && Is(unit, Conditions.Broken),
            unit is not null && (unit.Kind == "asl:hero" || Is(unit, Conditions.Heroic)), unit is not null && Is(unit, Conditions.Berserk),
            unit is not null && Is(unit, Conditions.Unarmed), unit is not null && Is(unit, Conditions.Captured), unit is not null && Is(unit, Conditions.Melee),
            unit is not null && index.VehicleOf(unit.Id) is not null, unit is not null && DefinitionOf(unit)?.BrokenMorale is not null);
        return ScenarioA1VoluntaryRout.BreakUnitBar(facts, () =>
        {
            var scan = new RoutScan(this, state);
            return ScenarioA1VoluntaryRout.Threat(scan, scan.Enemies(unit!.Side), scan.Index(at!));
        });
    }

    /// <summary>
    /// Voluntary Break (A10.41, p. 66; ruling R70.11): at the start of the RtPh, before any unit routs, the owner breaks units of his side that a Known
    /// enemy unit allows to. Each is broken and under DM (A10.62: "even if it breaks voluntarily"), loses a pin (A7.83, and the pinned HS of the example on
    /// p. 69), and loses its "?" (A12.14: "When a concealed unit ... breaks it loses its '?', regardless of the range ... or even if out of the LOS of all
    /// enemy units"). A leader's break calls for a LLTC of the unbroken units of lower Morale Level in his Location (A10.2), rolled through the Morale
    /// Check member (ruling R70.3).
    /// </summary>
    private GamePlan PlanVoluntaryBreak(GameScope scope, JsonElement arguments, IReadOnlyList<GameEvent> existing, string attemptId, long expected, string label, string actor)
    {
        if (Replay(existing).Current is not { } state)
        {
            return Refused(scope, label, expected, "play.no-game: the game has no state yet");
        }

        if (ScenarioA1VoluntaryRout.BreakPhaseBar(state.Phase, state.RoutedThisPhase.Count > 0) is { } phaseBar)
        {
            return Refused(scope, label, expected, phaseBar);
        }

        string[] ids = [.. Strings(arguments, "unitIds")];
        if (ids.Length == 0 || ids.Distinct(StringComparer.Ordinal).Count() != ids.Length)
        {
            return Refused(scope, label, expected, "play.invalid-arguments: a Voluntary Break names its units once each");
        }

        var side = state.Unit(ids[0])?.Side;
        foreach (var id in ids)
        {
            if (VoluntaryBreakBar(state, state.Unit(id), id, side) is { } unitBar)
            {
                return Refused(scope, label, expected, unitBar);
            }
        }

        UnitInstance[] units = [.. ids.Select(id => state.Unit(id)!)];
        string[] revealed = [.. units.Where(unit => Is(unit, Conditions.Concealed) || Is(unit, Conditions.Hidden)).Select(unit => unit.Id)];
        string[] unpinned = [.. units.Where(unit => Is(unit, Conditions.Pinned)).Select(unit => unit.Id)];
        var events = new List<GameEvent> { Event(scope, attemptId, 1, expected, "voluntary-break", new VoluntaryBreak(ids), null, null) };
        events.AddRange(RevealEvents(scope, attemptId, expected, events.Count + 1, revealed));
        foreach (var unit in units)
        {
            var conditions = new Dictionary<string, ConditionState> { [Conditions.Broken] = ConditionState.True, [Conditions.DesperationMorale] = ConditionState.True };
            if (Is(unit, Conditions.Pinned))
            {
                conditions[Conditions.Pinned] = ConditionState.False;
            }

            events.Add(Event(scope, attemptId, events.Count + 1, expected, "conditions-changed", new ConditionsChanged(unit.Id, conditions), null, null));
        }

        var reasons = ScenarioA1VoluntaryRout.BreakSummary(ids, unpinned, revealed).ToList();
        (string Id, bool Eliminated)[] leaders = [.. units.Where(unit => DefinitionOf(unit)?.IsLeader == true).Select(unit => (unit.Id, false))];
        if (leaders.Length == 0 || Replay([.. existing, .. events]) is not { HasErrors: false, Current: { } broken } || !LeaderLossDue(state, broken, leaders))
        {
            return new GamePlan(GamePlanStatus.Ready, scope, label, expected, events, reasons);
        }

        // A10.2 (ruling R70.11): "Should an unbroken leader break", the units of lower Morale Level in his Location take a LLTC, rolled now.
        reasons.Add(ScenarioA1VoluntaryRout.LeaderBreakText([.. leaders.Select(item => item.Id)]));
        var prefix = events.ToList();
        return new GamePlan(GamePlanStatus.Ready, scope, label, expected, [], reasons)
        {
            Roll = new PlannedRoll("leader-loss", draw =>
            {
                var built = new List<GameEvent>(prefix);
                AddLeaderLossChecks(scope, attemptId, expected, actor, built, draw, state, broken, leaders, ScenarioA1MoraleCheck.VoluntaryBreak, null);
                return built;
            }),
            FirstEventId = EventId(attemptId, 1),
        };
    }

    /// <summary>The facts of a leader a broken unit would rout with from a Location (A10.711; ruling R70.12), from the state's index.</summary>
    private VoluntaryRoutLeaderFacts VoluntaryRoutLeaderFactsOf(GameState state, UnitInstance unit, BoardLocation start, string leaderId)
    {
        var index = MemoOf(state).Objects(state);
        var leader = index.Unit(leaderId);
        return new VoluntaryRoutLeaderFacts(leaderId, leader is { Status: InstanceStatus.Active } && index.Location(leaderId) is not null, leader?.Side == unit.Side,
            leader is not null && DefinitionOf(leader)?.IsLeader == true, leader is not null && Is(leader, Conditions.Broken), leader is not null && Is(leader, Conditions.Pinned),
            leader is not null && Is(leader, Conditions.Berserk), leader is not null && Is(leader, Conditions.Melee), leader is not null && Is(leader, Conditions.Captured),
            index.VehicleOf(leaderId) is not null, index.Location(leaderId)?.Location == start, state.RoutedThisPhase.Contains(leaderId));
    }

    /// <summary>
    /// The leaders a broken unit may rout with by Voluntary Rout (A10.711; ruling R70.12): those of its side in its Location whom Rules allows, from the
    /// state's index, in the stack's order; none for a unit that routs beneath a vehicle or is not on the map.
    /// </summary>
    public IReadOnlyList<string> VoluntaryRoutLeaders(GameState state, UnitInstance unit)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(unit);
        var index = MemoOf(state).Objects(state);
        if (index.VehicleOf(unit.Id) is not null || index.Location(unit.Id)?.Location is not { } start)
        {
            return [];
        }

        return [.. index.At(start).OfType<UnitInstance>().Where(other => other.Id != unit.Id && other.Kind == "asl:leader" && other.Side == unit.Side
            && ScenarioA1VoluntaryRout.LeaderBar(unit.Id, VoluntaryRoutLeaderFactsOf(state, unit, start, other.Id)) is null).Select(other => other.Id)];
    }
}
