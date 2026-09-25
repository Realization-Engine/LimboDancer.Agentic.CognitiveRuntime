using LimboDancer.Dice;
using System.Globalization;
using System.Text.Json;
using LimboDancer.Abstractions.Actions;
using LimboDancer.Abstractions.Audit;
using LimboDancer.Abstractions.Execution;
using LimboDancer.Abstractions.Runtime;
using LimboDancer.Domains.Asl.Units.State;
using LimboDancer.Runtime.Actions;
using LimboDancer.Runtime.Diagnostics;
using LimboDancer.Runtime.Execution;
using RuntimeExecutionContext = LimboDancer.Abstractions.Execution.ExecutionContext;

namespace LimboDancer.Domains.Asl.Play;

/// <summary>
/// Re-observes the live game at gate time (ASL-UNIT-042): the action is planned afresh against the current log, and
/// the gate records the revision it validated, which the executor must find unchanged.
/// </summary>
public sealed class GameConstraintEvaluator(GamePlanner planner) : IActionConstraintEvaluator
{
    public async Task<ConstraintEvaluationResult> EvaluateAsync(SelectedAction action, RuntimeExecutionContext context,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(action);
        ArgumentNullException.ThrowIfNull(context);
        if (!GameActions.All.Any(descriptor => ReferenceEquals(descriptor, action.Candidate.Descriptor)))
        {
            return new ConstraintEvaluationResult(ConstraintEvaluationOutcome.Failed, ["play.unregistered-action"]);
        }

        var plan = await planner.PlanAsync(action.Candidate.Descriptor, action.Candidate.Arguments, context.TenantId, cancellationToken);
        return plan.Status switch
        {
            GamePlanStatus.Ready or GamePlanStatus.Replay => new ConstraintEvaluationResult(ConstraintEvaluationOutcome.Satisfied, plan.Reasons,
                [new KeyValuePair<string, string>(GameActions.VersionKey(context.TenantId, plan.Scope.Game),
                    plan.ExpectedRevision.ToString(CultureInfo.InvariantCulture))]),
            GamePlanStatus.Stale => new ConstraintEvaluationResult(ConstraintEvaluationOutcome.Stale, plan.Reasons),
            _ => new ConstraintEvaluationResult(ConstraintEvaluationOutcome.Failed, plan.Reasons),
        };
    }
}

/// <summary>
/// Commits one governed game action: it accepts only a gate-produced authorization, plans again, appends at the
/// validated revision, and reads the effect back before reporting success (ASL-UNIT-042).
/// </summary>
public sealed class GameActionExecutor(ActionDescriptor descriptor, GamePlanner planner, IGameStore store, DiceRoller? roller = null) : IActionExecutor
{
    private readonly DiceRoller dice = roller ?? new DiceRoller();

    public ActionId ActionId => descriptor.Id;

    public ExecutorBinding Binding => descriptor.Executor;

    public async Task<ActionExecutionResult> ExecuteAsync(AuthorizedAction action, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(action);
        if (!ReferenceEquals(action.Selected.Candidate.Descriptor, descriptor))
        {
            return Failed("play.invalid-authorized-action");
        }

        var plan = await planner.PlanAsync(descriptor, action.Selected.Candidate.Arguments, action.TenantId, cancellationToken);
        var key = GameActions.VersionKey(action.TenantId, plan.Scope.Game);
        if (!action.ValidatedStateVersions.TryGetValue(key, out var validated)
            || validated != plan.ExpectedRevision.ToString(CultureInfo.InvariantCulture))
        {
            return Failed("play.gate-state-stale");
        }

        if (plan.Status == GamePlanStatus.Replay)
        {
            return Succeeded("play.replay", plan, store.Read(plan.Scope)?.Events.Count ?? 0);
        }

        if (plan.Status != GamePlanStatus.Ready)
        {
            return Failed(plan.Status == GamePlanStatus.Stale ? "play.gate-state-stale" : "play.refused-at-commit");
        }

        var appended = plan.Roll is { } roll
            ? store.AppendRolled(plan.Scope, plan.Label, plan.ExpectedRevision, plan.FirstEventId!, roll, dice, planner.Replay)
            : store.Append(plan.Scope, plan.Label, plan.ExpectedRevision, plan.Events, planner.Replay);
        if (appended.Status == AppendStatus.Replay)
        {
            return Succeeded("play.replay", plan, appended.Revision);
        }

        if (appended.Status != AppendStatus.Committed)
        {
            return Failed(appended.Status == AppendStatus.Stale ? "play.gate-state-stale" : "play.commit-refused");
        }

        // Verified effect: the stored log replays, ends with exactly these events, and shows their effect. A rolled batch is
        // read back from the log, never from memory, so its recorded values are what the caller sees (DICE-11).
        var stored = store.Read(plan.Scope);
        var history = stored is null ? null : planner.Replay(stored.Events);
        var committed = plan.Roll is null ? plan : plan with
        {
            Events = [.. stored?.Events.Skip((int)plan.ExpectedRevision) ?? []]
        };
        if (history?.Current is not { } state || committed.Events.Count == 0 || state.Revision != plan.ExpectedRevision + committed.Events.Count
            || !stored!.Events.TakeLast(committed.Events.Count).Select(item => item.EventId).SequenceEqual(committed.Events.Select(item => item.EventId))
            || (plan.Roll is not null && committed.Events[0].EventId != plan.FirstEventId)
            || !EffectHolds(state, committed))
        {
            return Failed("play.effect-readback-failed");
        }

        return Succeeded("play.committed", committed, state.Revision);
    }

    private static bool EffectHolds(GameState state, GamePlan plan) => plan.Events[^1].Payload switch
    {
        // A forced back: the mover is where it started, with its movement ended, and every defender the plan revealed is known.
        EntryForcedBack forced => state.Unit(forced.Id) is { MovementEnded: true } unit && state.Location(unit.Id)?.Location == forced.ReturnedTo
            && plan.Events.Select(item => item.Payload).OfType<ConditionsChanged>().All(changed => state.Unit(changed.Id) is { } revealed
                && GameState.Condition(revealed, Conditions.Concealed) == ConditionState.False
                && GameState.Condition(revealed, Conditions.Hidden) == ConditionState.False),
        InstanceMoved moved => state.Unit(moved.Id) is { } unit && state.Location(unit.Id) is { } at
            && moved.Position is MapPosition target && at.Location == target.Location,
        PhaseChanged phase => state.Phase == phase.Phase && state.PhasingSide == phase.PhasingSide && state.Turn == phase.Turn,
        _ => plan.Events.Select(item => item.Payload).OfType<InstanceCreated>().All(created => state.Find(created.Instance.Id) is not null),
    };

    private static ActionExecutionResult Succeeded(string code, GamePlan plan, long revision) => new(true, code,
        JsonSerializer.SerializeToElement(new
        {
            game = plan.Scope.Game,
            revision,
            events = plan.Events.Select(item => item.EventId)
        }),
        [plan.Scope.Game]);

    private static ActionExecutionResult Failed(string code) => new(false, code);
}

/// <summary>
/// Records the user's explicit confirmation of one proposal. The runtime has no confirmation protocol of its own
/// (an action needing confirmation stops at the gate), so a host that asks its user decides here (Governed Writes
/// Design, section 8).
/// </summary>
public sealed class ConfirmationPolicy(IExecutionRiskPolicy inner) : IExecutionRiskPolicy
{
    private readonly HashSet<string> confirmed = new(StringComparer.Ordinal);

    /// <summary>Confirms the proposal with this correlation id, once.</summary>
    public void Confirm(CorrelationId correlation)
    {
        lock (confirmed)
        {
            confirmed.Add(correlation.Value);
        }
    }

    /// <summary>Whether the proposal with this correlation id is confirmed and not yet used.</summary>
    public bool IsConfirmed(CorrelationId correlation)
    {
        lock (confirmed)
        {
            return confirmed.Contains(correlation.Value);
        }
    }

    public RiskEvaluationResult Evaluate(SelectedAction action, RuntimeExecutionContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        var result = inner.Evaluate(action, context);
        if (result.Outcome != RiskEvaluationOutcome.ConfirmationRequired)
        {
            return result;
        }

        lock (confirmed)
        {
            return confirmed.Remove(context.CorrelationId.Value) ? new RiskEvaluationResult(RiskEvaluationOutcome.Allowed) : result;
        }
    }
}

public enum PlayOutcome
{
    Committed,
    Replay,

    /// <summary>The action passed every check; it waits for the user's confirmation.</summary>
    NeedsConfirmation,
    Denied,
    Stale,
    Failed,
}

public sealed record PlayResult(PlayOutcome Outcome, CorrelationId Correlation, IReadOnlyList<string> Reasons, GamePlan? Plan);

/// <summary>
/// The governed path for live games: a registered action, re-observation, diagnostics, the Execution Gate with its
/// risk policy and audit, an expected revision, an atomic commit, and a verified effect (ASL-UNIT-042). Proposing an
/// action runs the gate; confirming runs it again with the user's confirmation and commits.
/// </summary>
public sealed class GamePlay
{
    private readonly GamePlanner planner;
    private readonly ConfirmationPolicy confirmation;
    private readonly ExecutionGate gate;
    private readonly Dictionary<ActionId, IActionExecutor> executors;

    public GamePlay(GamePlanner planner, IGameStore store, IAuditSink audit, IExecutionRiskPolicy? risk = null, DiceRoller? roller = null)
    {
        ArgumentNullException.ThrowIfNull(planner);
        ArgumentNullException.ThrowIfNull(store);
        ArgumentNullException.ThrowIfNull(audit);
        this.planner = planner;
        confirmation = new ConfirmationPolicy(risk ?? new DefaultExecutionRiskPolicy());
        IActionExecutor[] bound = [.. GameActions.All.Select(descriptor => new GameActionExecutor(descriptor, planner, store, roller))];
        executors = bound.ToDictionary(executor => executor.ActionId, executor => (IActionExecutor)new AuditedActionExecutor(executor, audit));
        gate = new ExecutionGate(new ActionRegistry(GameActions.All), new ActionExecutorResolver(bound), new GameConstraintEvaluator(planner),
            new DiagnosticPolicy(), confirmation, audit);
    }

    /// <summary>Plans an action without the gate, to show what it would do.</summary>
    public Task<GamePlan> PreviewAsync(ActionDescriptor action, JsonElement arguments, Guid tenant, CancellationToken cancellationToken = default) =>
        planner.PlanAsync(action, arguments, tenant, cancellationToken);

    /// <summary>Runs the gate; commits only if no confirmation is needed.</summary>
    public Task<PlayResult> ProposeAsync(ActionDescriptor action, JsonElement arguments, RuntimePrincipal principal, CancellationToken cancellationToken = default) =>
        RunAsync(action, arguments, principal, new CorrelationId(Guid.NewGuid().ToString("N")), cancellationToken);

    /// <summary>Confirms a proposal and runs the gate again; it commits only if every check still passes.</summary>
    public Task<PlayResult> ConfirmAsync(ActionDescriptor action, JsonElement arguments, RuntimePrincipal principal, CorrelationId correlation,
        CancellationToken cancellationToken = default)
    {
        confirmation.Confirm(correlation);
        return RunAsync(action, arguments, principal, correlation, cancellationToken);
    }

    public static RuntimePrincipal Principal(string id, Guid tenant, params string[] permissions) =>
        new(id, tenant, isAuthenticated: true, permissions);

    private async Task<PlayResult> RunAsync(ActionDescriptor action, JsonElement arguments, RuntimePrincipal principal, CorrelationId correlation,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(action);
        ArgumentNullException.ThrowIfNull(principal);
        var plan = await planner.PlanAsync(action, arguments, principal.TenantId, cancellationToken);

        // A withheld entry is only declared when proposed: running the gate would tell the mover's side whether the
        // target hides a unit. The gate runs, and decides, when the entry is confirmed (Occupied and Concealed Entry
        // Design, section 7). A refusal that depends only on the mover and the terrain reveals nothing, so it is denied now.
        if (!confirmation.IsConfirmed(correlation) && plan.Disclosure is { Withheld: true, MoverReasons.Count: 0 }
            && plan.Status is GamePlanStatus.Ready or GamePlanStatus.Refused)
        {
            return new PlayResult(PlayOutcome.NeedsConfirmation, correlation, plan.Reasons, plan);
        }

        var context = new RuntimeExecutionContext(RuntimeInvocationId.New(), correlation, principal.TenantId, principal,
            new RuntimeBudget(10, DateTimeOffset.UtcNow.AddMinutes(1), null, null, 10, 1));
        var selected = new SelectedAction(new ActionCandidate("play", action, arguments), SelectionOrigin.DirectedCaller);
        var authorization = await gate.AuthorizeAsync(selected, context, cancellationToken);
        switch (authorization.Outcome)
        {
            case ExecutionGateOutcome.ConfirmationRequired:
                return new PlayResult(PlayOutcome.NeedsConfirmation, correlation, plan.Reasons, plan);
            case ExecutionGateOutcome.Authorized:
                var result = await executors[action.Id].ExecuteAsync(authorization.AuthorizedAction!, cancellationToken);
                return new PlayResult(!result.Succeeded ? PlayOutcome.Failed : result.Code == "play.replay" ? PlayOutcome.Replay : PlayOutcome.Committed,
                    correlation, [result.Code, .. plan.Reasons], plan);
            default:
                return new PlayResult(plan.Status == GamePlanStatus.Stale ? PlayOutcome.Stale : PlayOutcome.Denied, correlation,
                    [.. authorization.ReasonCodes.Concat(plan.Reasons).Distinct(StringComparer.Ordinal)], plan);
        }
    }
}
