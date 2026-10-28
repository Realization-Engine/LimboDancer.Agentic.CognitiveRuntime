using System.Text.Json;
using LimboDancer.Domains.Asl.Units.State;

namespace LimboDancer.Domains.Asl.Play;

public sealed partial class GamePlanner
{
    private GamePlan PlanSpotterDesignation(GameScope scope, JsonElement arguments, IReadOnlyList<GameEvent> existing,
        string attemptId, long expected, string label)
    {
        if (!Text(arguments, "gunId", out var gun) || !Text(arguments, "spotter", out var spotter)
            || Replay(existing).Current is not { } state || state.Unit(spotter)?.Side is not { } owner)
        {
            return Refused(scope, label, expected, "play.spotter: identify the mortar and Spotter in an existing game");
        }
        var privateRecord = state.Find(gun) is { } weapon && (Is(weapon, Conditions.Hidden) || Is(weapon, Conditions.Concealed))
            || state.Unit(spotter) is { } unit && (Is(unit, Conditions.Hidden) || Is(unit, Conditions.Concealed))
            || state.Find(gun) is EquipmentInstance { Holding: { } held } && state.Unit(held.Holder) is { } holder
                && (Is(holder, Conditions.Hidden) || Is(holder, Conditions.Concealed));
        var proposed = Event(scope, attemptId, 1, expected, "mortar-spotter-designated", new MortarSpotterDesignated(gun, spotter),
            "asl.spotter.v1", privateRecord ? [owner] : null);
        var replay = Replay([.. existing, proposed]);
        if (replay.HasErrors)
        {
            return Refused(scope, label, expected, "play.spotter: " + string.Join(" ", replay.Diagnostics.Where(item => item.Severity == Units.UnitDiagnosticSeverity.Error).Select(item => item.Message)));
        }
        return new(GamePlanStatus.Ready, scope, label, expected, [proposed],
            [$"Designate {spotter} for {gun}; record phase SW use and its fire marker. This does not authorize a shot."])
        {
            FirstEventId = EventId(attemptId, 1)
        };
    }
}
