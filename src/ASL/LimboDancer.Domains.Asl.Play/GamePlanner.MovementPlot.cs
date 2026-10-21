using System.Text.Json;
using LimboDancer.Domains.Asl.Maps.Coordinates;
using LimboDancer.Domains.Asl.Units.State;

namespace LimboDancer.Domains.Asl.Play;

/// <summary>A plotted step of a route: the Location entered, its half MF, and the half MF the stack has left after it (null when undecided).</summary>
public sealed record PlottedStep(BoardLocation To, int HalfMf, int? HalfMfLeft);

/// <summary>A Location the route's end may enter next, with its half MF and what is left after it, or the reason it may not.</summary>
public sealed record PlotOption(BoardLocation To, int? HalfMf, int? HalfMfLeft, string? Refusal);

/// <summary>
/// A route as the moving side plots it (the Play streamlining plan, S3): the steps the plan reads as made, where and why it stops when one may not
/// be, and the Locations its end may enter next.
/// </summary>
public sealed record MovementPlot(IReadOnlyList<PlottedStep> Route, string? Stop, IReadOnlyList<PlotOption> Next);

/// <summary>
/// The Play streamlining plan, S3: a route plotted for a stack before any of it is committed. Each step is the move's own plan on the events the
/// steps before it would add, with the DEFENDER's window on each closed as if he let the stack move on; nothing is stored. The plan reads the
/// Locations as the moving side knows them (<see cref="plotting"/>), so a hidden or concealed enemy is met when the step is made, not while plotting.
/// </summary>
public sealed partial class GamePlanner
{
    /// <summary>
    /// Plots a route for a stack: the steps of <paramref name="route"/> as the move's plan reads them, in order, until one may not be made or carries
    /// a roll (Residual FP, a Fire Lane), which the step resolves when it is made; then the Locations the last step's Location may enter. With no
    /// route, the options are from the stack's own Location.
    /// </summary>
    public MovementPlot Plot(GameScope scope, IReadOnlyList<GameEvent> existing, IReadOnlyList<string> movers, IReadOnlyList<BoardLocation> route, string mover)
    {
        ArgumentNullException.ThrowIfNull(existing);
        ArgumentNullException.ThrowIfNull(movers);
        ArgumentNullException.ThrowIfNull(route);
        plotting++;
        try
        {
            var events = existing;
            var steps = new List<PlottedStep>(route.Count);
            for (var index = 0; index < route.Count; index++)
            {
                var plan = PlanMove(scope, MoveArguments(movers, route[index]), events, $"plot-{index + 1}", events.Count, scope.Game, mover);
                if (plan.Status != GamePlanStatus.Ready)
                {
                    return new MovementPlot(steps, FirstReason(plan), []);
                }

                steps.Add(new PlottedStep(route[index], plan.HalfMf ?? 0, plan.HalfMfLeft));
                if (plan.Roll is not null)
                {
                    return new MovementPlot(steps, "play.plot-attack: an attack waits in this Location (Residual FP or a Fire Lane, A8.22, A9.22); the route goes on once the step is made", []);
                }

                events = [.. events, .. plan.Events];

                // The DEFENDER's window on the step, closed as if he let the stack move on: written here, not asked of the DEFENDER's plan, whose
                // refusals read his units.
                if (Replay(events).Current?.Movement is not { WindowOpen: true } window)
                {
                    return new MovementPlot(steps, null, []);
                }

                events = [.. events, Event(scope, $"plot-pass-{index + 1}", 1, events.Count, "movement-window-closed", new MovementWindowClosed(window.Step), null, null)];
            }

            if (Replay(events).Current is not { } state)
            {
                return new MovementPlot(steps, "play.no-game: the game has no state yet", []);
            }

            var at = route.Count > 0 ? route[^1] : movers.Count > 0 ? state.Location(movers[0])?.Location : null;
            if (at is null)
            {
                return new MovementPlot(steps, null, []);
            }

            var options = new List<PlotOption>();
            foreach (var to in PlotCandidates(state, at))
            {
                var plan = PlanMove(scope, MoveArguments(movers, to), events, "plot-next", events.Count, scope.Game, mover);
                options.Add(plan.Status == GamePlanStatus.Ready
                    ? new PlotOption(to, plan.HalfMf, plan.HalfMfLeft, null)
                    : new PlotOption(to, null, null, FirstReason(plan)));
            }

            return new MovementPlot(steps, null, options);
        }
        finally
        {
            plotting--;
        }
    }

    private static string FirstReason(GamePlan plan) => plan.Reasons.Count > 0 ? plan.Reasons[0] : "play.plot: the step may not be made";

    private static JsonElement MoveArguments(IReadOnlyList<string> movers, BoardLocation to) =>
        JsonSerializer.SerializeToElement(new Dictionary<string, object> { ["unitIds"] = movers, ["to"] = to.ToString() });

    /// <summary>
    /// The Locations a stack at <paramref name="at"/> may try next: each ADJACENT hex at ground level and, from an upper level, at the same level;
    /// and the levels above and below in its own hex. Only Locations the map reads are offered.
    /// </summary>
    private IEnumerable<BoardLocation> PlotCandidates(GameState state, BoardLocation at)
    {
        var seen = new HashSet<BoardLocation>();
        foreach (var ground in Neighbors(state, at))
        {
            if (seen.Add(ground) && ReadLocation(state, ground) is not null)
            {
                yield return ground;
            }

            var level = new BoardLocation(ground.Board, ground.Hex, at.Level);
            if (at.Level != 0 && seen.Add(level) && ReadLocation(state, level) is not null)
            {
                yield return level;
            }
        }

        foreach (var step in new[] { -1, 1 })
        {
            var other = new BoardLocation(at.Board, at.Hex, at.Level + step);
            if (other.Level >= 0 && seen.Add(other) && ReadLocation(state, other) is not null)
            {
                yield return other;
            }
        }
    }
}
