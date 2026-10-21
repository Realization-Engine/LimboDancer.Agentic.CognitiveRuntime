using System.Collections.Concurrent;
using System.Text.Json;
using LimboDancer.Abstractions.Actions;
using LimboDancer.Domains.Asl.Maps.Coordinates;
using LimboDancer.Domains.Asl.Play;
using LimboDancer.Domains.Asl.Units.State;

namespace LimboDancer.Domains.Asl.MapStudio.Services;

/// <summary>A stack's plotted route, being made step by step (the Play streamlining plan, S6).</summary>
/// <param name="Game">The game's id.</param>
/// <param name="Movers">The moving stack.</param>
/// <param name="Route">Every Location of the route, in order.</param>
/// <param name="Assault">Assault Movement, declared for the route (A4.61).</param>
/// <param name="DoubleTime">Double Time, declared for the route (A4.5).</param>
/// <param name="Mover">The moving side, for whom each step is proposed.</param>
/// <param name="Defender">The DEFENDER, for whom each pass is proposed.</param>
/// <param name="Key">The plan's own id, which each step's attempt id carries, so a step retried is a replay.</param>
public sealed record MovementPlan(string Game, IReadOnlyList<string> Movers, IReadOnlyList<BoardLocation> Route, bool Assault, bool DoubleTime, string Mover, string Defender,
    string Key)
{
    /// <summary>The index in <see cref="Route"/> of the next step to make; the steps before it are made.</summary>
    public int Next
    {
        get; init;
    }

    /// <summary>The Location the stack left for its latest step, for a Snap Shot's hexside (A8.15).</summary>
    public BoardLocation? From
    {
        get; init;
    }

    /// <summary>The game's revision after the plan's latest action; another action since (the DEFENDER's fire) shows as a different revision.</summary>
    public long Revision
    {
        get; init;
    }

    /// <summary>Why the plan stopped and waits for the moving side; null while it runs.</summary>
    public string? Paused
    {
        get; init;
    }

    /// <summary>The Location the stack is in now: the latest step's, or its start before any.</summary>
    public BoardLocation? At => Next > 0 ? Route[Next - 1] : null;
}

/// <summary>What a plan's run left: whose turn it is, and what to say.</summary>
public enum MovementPlanTurn
{
    /// <summary>The DEFENDER decides at the stack's latest step: fire, or let it move on.</summary>
    Defender,

    /// <summary>The moving side decides: continue the route, change it, or end the move.</summary>
    Mover,

    /// <summary>The route is made and the move ended; the plan is gone.</summary>
    Done,
}

/// <param name="Turn">Whose turn the plan leaves it.</param>
/// <param name="Note">What happened, in words for the view that sees it next.</param>
/// <param name="Refusal">The gate's refusal of a step or a pass, when one was refused.</param>
public sealed record MovementPlanRun(MovementPlanTurn Turn, string Note, IReadOnlyList<string> Refusal);

/// <summary>
/// The Play streamlining plan, S6: the movement plans being made, one per game. Each step is the ordinary move, proposed for the moving side and
/// confirmed at once; after it, the DEFENDER's window is open. A step where none of his units could fire is passed for him (ruling Q8); at any
/// other he decides, and "to the end" passes every later one (Q9). Fire, a step the gate refuses, or a stack that a step's own attack changed hands
/// the plan back to the moving side. The plans are not game events and do not outlive the Studio: a plan lost leaves the stack as the game records it.
/// </summary>
public sealed class MovementPlans(LivePlay live)
{
    private readonly ConcurrentDictionary<string, MovementPlan> plans = new(StringComparer.Ordinal);
    private readonly ConcurrentDictionary<string, SemaphoreSlim> locks = new(StringComparer.Ordinal);

    /// <summary>The plan being made in a game, or null.</summary>
    public MovementPlan? Of(string game) => plans.TryGetValue(game, out var plan) ? plan : null;

    /// <summary>Forgets a game's plan; the stack stays as the game records it.</summary>
    public void Drop(string game) => plans.TryRemove(game, out _);

    /// <summary>
    /// Starts a plan: its first step is made, and the DEFENDER decides there, or, when it is public that none of his units could fire at the
    /// step (he has no concealed or hidden unit), the plan runs on in the moving side's view until he could.
    /// </summary>
    public async Task<MovementPlanRun> Start(MovementPlan plan)
    {
        ArgumentNullException.ThrowIfNull(plan);
        var gate = locks.GetOrAdd(plan.Game, _ => new SemaphoreSlim(1, 1));
        await gate.WaitAsync();
        try
        {
            var events = Events(plan.Game);
            plans[plan.Game] = plan with
            {
                Revision = events.Count
            };
            return await Run(plan.Game, stepFirst: true, toEnd: false, defenderView: false);
        }
        finally
        {
            gate.Release();
        }
    }

    /// <summary>The moving side continues a paused plan from its next step.</summary>
    public Task<MovementPlanRun> Continue(string game) => Locked(game, () =>
    {
        if (Of(game) is { } plan)
        {
            // The plan's section 2.7: a game that has moved on since the pause (another view or Studio acted) is not stepped on blind.
            if (Events(game).Count != plan.Revision)
            {
                Drop(game);
                return Task.FromResult(new MovementPlanRun(MovementPlanTurn.Mover, "The game has moved on since the route was paused: plot the rest again.", []));
            }

            plans[game] = plan with
            {
                Paused = null
            };
        }

        return Run(game, stepFirst: true, toEnd: false, defenderView: false);
    });

    /// <summary>The DEFENDER lets the stack move on: his pass, then the plan's next steps, until he could fire again (or, to the end, never).</summary>
    public Task<MovementPlanRun> MoveOn(string game, bool toEnd) => Locked(game, () => Run(game, stepFirst: false, toEnd, defenderView: true));

    /// <summary>
    /// The DEFENDER's view opened on the plan: when none of his units could fire at the latest step, it is passed for him and the plan runs on;
    /// otherwise he decides.
    /// </summary>
    public Task<MovementPlanRun> Review(string game) => Locked(game, async () =>
    {
        var plan = Of(game);
        var events = Events(game);
        if (plan is null || live.Planner.Replay(events).Current is not { } state)
        {
            return new MovementPlanRun(MovementPlanTurn.Done, "No route is being moved.", []);
        }

        return plan.Paused is not null || live.Planner.DefenderMayFire(state, events, plan.From)
            ? new MovementPlanRun(MovementPlanTurn.Defender, StepNote(plan), [])
            : await Run(game, stepFirst: false, toEnd: false, defenderView: true);
    });

    private async Task<MovementPlanRun> Locked(string game, Func<Task<MovementPlanRun>> work)
    {
        var gate = locks.GetOrAdd(game, _ => new SemaphoreSlim(1, 1));
        await gate.WaitAsync();
        try
        {
            return await work();
        }
        finally
        {
            gate.Release();
        }
    }

    /// <summary>
    /// Runs a plan: unless <paramref name="stepFirst"/>, first the DEFENDER's pass at the latest step; then, step by step, the next step, and the
    /// DEFENDER's pass where none of his units could fire (or every pass, <paramref name="toEnd"/>), until he could, the route ends, or the plan stops.
    /// In the moving side's view (<paramref name="defenderView"/> false) a step is passed for the DEFENDER only when that tells the moving side
    /// nothing: the DEFENDER has no concealed, hidden, or Dummy unit; otherwise the screen goes to him after the step.
    /// </summary>
    private async Task<MovementPlanRun> Run(string game, bool stepFirst, bool toEnd, bool defenderView)
    {
        if (Of(game) is not { } plan)
        {
            return new MovementPlanRun(MovementPlanTurn.Done, "No route is being moved.", []);
        }

        var events = Events(game);
        var interrupted = !stepFirst && events.Count != plan.Revision;
        var pass = !stepFirst;
        while (true)
        {
            if (pass)
            {
                var passed = await Commit(game, GameActions.PassFire, plan.Defender, $"{plan.Key}-pass-{plan.Next}", null);
                if (passed.Outcome is not (PlayOutcome.Committed or PlayOutcome.Replay))
                {
                    return new MovementPlanRun(MovementPlanTurn.Defender, "The DEFENDER may not pass here yet.", passed.Reasons);
                }

                events = Events(game);
                if (live.Planner.Replay(events).Current?.Movement is null)
                {
                    // The pass ended the move: every member of the stack was broken, pinned, or eliminated (A8.1).
                    Drop(game);
                    return new MovementPlanRun(MovementPlanTurn.Done, "The stack's move is over: no member of it may move on.", []);
                }

                // A8.11: after the DEFENDER's fire, or a step that changed the stack, the moving side sees the result and decides again.
                if (interrupted || plan.Paused is not null)
                {
                    plans[game] = plan = plan with
                    {
                        Revision = events.Count,
                        Paused = interrupted ? "The DEFENDER fired at the stack." : plan.Paused
                    };
                    return new MovementPlanRun(MovementPlanTurn.Mover, plan.Paused!, []);
                }
            }

            if (plan.Next >= plan.Route.Count)
            {
                var ended = await Commit(game, GameActions.EndMove, plan.Mover, $"{plan.Key}-end", null);
                Drop(game);
                return ended.Outcome is PlayOutcome.Committed or PlayOutcome.Replay
                    ? new MovementPlanRun(MovementPlanTurn.Done, $"The stack ends its move in {Say(plan.At)}.", [])
                    : new MovementPlanRun(MovementPlanTurn.Mover, "The route is made; the move did not end.", ended.Reasons);
            }

            var to = plan.Route[plan.Next];
            var before = live.Planner.Replay(events).Current;
            var stepped = await Commit(game, GameActions.Move, plan.Mover, $"{plan.Key}-step-{plan.Next}", node =>
            {
                node["unitIds"] = JsonSerializer.SerializeToNode(plan.Movers);
                node["to"] = to.ToString();
                node["assault"] = plan.Assault;
                node["doubleTime"] = plan.DoubleTime && plan.Next == 0;
            });
            if (stepped.Outcome is not (PlayOutcome.Committed or PlayOutcome.Replay))
            {
                plans[game] = plan = plan with
                {
                    Paused = $"The step into {Say(to)} was refused."
                };
                return new MovementPlanRun(MovementPlanTurn.Mover, plan.Paused, stepped.Reasons);
            }

            events = Events(game);
            var after = live.Planner.Replay(events).Current;
            plan = plan with
            {
                Next = plan.Next + 1,
                From = before is null ? null : StackAt(before, plan.Movers),
                Revision = events.Count
            };
            plans[game] = plan;

            // A step's own attack (Residual FP, a Fire Lane) or a reveal may have changed the stack: the DEFENDER decides at this step, and then the
            // moving side decides again.
            if (after?.Movement is not { } moving || moving.Members.Count != plan.Movers.Count || Revealed(before, after))
            {
                plans[game] = plan = plan with
                {
                    Paused = "The step changed the stack or revealed a unit."
                };
                return new MovementPlanRun(MovementPlanTurn.Defender, StepNote(plan), []);
            }

            if (!defenderView && !Public(after, plan.Defender))
            {
                return new MovementPlanRun(MovementPlanTurn.Defender, StepNote(plan), []);
            }

            if (!toEnd && live.Planner.DefenderMayFire(after, events, plan.From))
            {
                return new MovementPlanRun(MovementPlanTurn.Defender, StepNote(plan), []);
            }

            pass = true;
        }
    }

    /// <summary>Whether the DEFENDER's units are all Known, so whether one could fire is a fact the moving side may see for itself.</summary>
    private static bool Public(GameState state, string defender) => !state.Units.Any(unit => unit.Status == InstanceStatus.Active && unit.Side == defender
        && (unit.Kind == UnitKinds.Dummy || GameState.Condition(unit, Conditions.Concealed) == ConditionState.True || GameState.Condition(unit, Conditions.Hidden) == ConditionState.True));

    private static bool Revealed(GameState? before, GameState after) =>
        before is not null && before.Units.Count(unit => GameState.Condition(unit, Conditions.Concealed) == ConditionState.True || GameState.Condition(unit, Conditions.Hidden) == ConditionState.True)
            != after.Units.Count(unit => GameState.Condition(unit, Conditions.Concealed) == ConditionState.True || GameState.Condition(unit, Conditions.Hidden) == ConditionState.True);

    private static BoardLocation? StackAt(GameState state, IReadOnlyList<string> movers) => movers.Count > 0 ? state.Location(movers[0])?.Location : null;

    private static string StepNote(MovementPlan plan) => $"The stack has entered {Say(plan.At)}, step {plan.Next} of {plan.Route.Count}.";

    private static string Say(BoardLocation? at) => at is null ? "its start" : $"[{at.Hex}]";

    private IReadOnlyList<GameEvent> Events(string game) => live.Store.Read(new GameScope(LivePlay.Tenant, game))?.Events ?? [];

    /// <summary>Proposes an action for a side and confirms it at once: the gate still asks for and records the confirmation.</summary>
    private async Task<PlayResult> Commit(string game, ActionDescriptor action, string side, string attemptId, Action<System.Text.Json.Nodes.JsonObject>? fill)
    {
        var node = new System.Text.Json.Nodes.JsonObject
        {
            ["gameId"] = game,
            ["attemptId"] = attemptId,
            ["expectedRevision"] = Events(game).Count,
            [GamePlanner.ProposedByArgument] = side,
        };
        fill?.Invoke(node);
        var arguments = JsonSerializer.SerializeToElement(node);
        var proposed = await live.Play.ProposeAsync(action, arguments, live.Principal);
        return proposed.Outcome == PlayOutcome.NeedsConfirmation ? await live.Play.ConfirmAsync(action, arguments, live.Principal, proposed.Correlation) : proposed;
    }
}
