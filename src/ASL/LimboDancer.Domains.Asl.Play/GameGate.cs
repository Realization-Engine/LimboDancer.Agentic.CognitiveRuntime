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

        var plan = await planner.PlanAsync(action.Candidate.Descriptor, action.Candidate.Arguments, context.TenantId, context.Principal.PrincipalId,
            cancellationToken);
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

        var plan = await planner.PlanAsync(descriptor, action.Selected.Candidate.Arguments, action.TenantId, action.PrincipalId, cancellationToken);
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

    /// <summary>The units a fire record marks (A7.351, A9.12; ruling R12.4): its resolution's, or its firers and director in an older record.</summary>
    private static IEnumerable<string> Marked(FireResolved fire) =>
        fire.Resolution.TryGetProperty("fireCounterUnitIds", out var ids) && ids.ValueKind == JsonValueKind.Array
            ? ids.EnumerateArray().Select(item => item.GetString()).OfType<string>()
            : fire.Firers.Concat(fire.Director is { } director ? [director] : []);

    private static bool EffectHolds(GameState state, GamePlan plan) => plan.Events.FirstOrDefault(item => item.Payload is FireResolved) is { } fire
        // A fire attack: the record is kept for the phase, and every firer and the director carry the fire marker.
        ? state.FiresThisPhase.Any(record => record.EventId == fire.EventId)
            && Marked((FireResolved)fire.Payload)
                .All(id => state.Unit(id) is not { Status: InstanceStatus.Active } unit || GameState.Condition(unit, Conditions.PrepFire) == ConditionState.True
                    || GameState.Condition(unit, Conditions.FinalFire) == ConditionState.True || GameState.Condition(unit, Conditions.FirstFire) == ConditionState.True
                    || GameState.Condition(unit, Conditions.BoundingFire) == ConditionState.True)
        : plan.Events.Select(item => item.Payload).OfType<OpportunityFireDeclared>().FirstOrDefault() is { } opportunity
        // Opportunity Fire (A7.25): its units carry the Bounding Fire counter.
        ? opportunity.Units.All(id => state.Unit(id) is { } unit && GameState.Condition(unit, Conditions.BoundingFire) == ConditionState.True)
        : plan.Events.Select(item => item.Payload).OfType<RoutStepped>().ToArray() is { Length: > 0 } routed
        // A rout (A10.5; ruling R13.3): every unit that stepped is listed as routed this RtPh.
        ? routed.All(step => state.RoutedThisPhase.Contains(step.Unit))
        : plan.Events.Select(item => item.Payload).OfType<DeploymentAttempted>().FirstOrDefault() is { } deployment
        // A Deployment attempt (A1.31; ruling R13.4): the squad has spent its RPh action.
        ? state.RallyPhaseActions.Contains(deployment.Squad)
        : plan.Events.Select(item => item.Payload).OfType<RecoveryAttempted>().FirstOrDefault() is { } recovery
        // A Recovery attempt (A4.44; ruling R13.5): it is kept for the phase, and a Recovered SW is the unit's.
        ? state.RecoveryAttempts.Contains(recovery.Unit + "|" + recovery.Weapon)
            && (!recovery.Recovered || state.Find(recovery.Weapon) is EquipmentInstance { Holding: { } recoveredBy } && recoveredBy.Holder == recovery.Unit)
        : plan.Events.Select(item => item.Payload).OfType<RallyPhaseActionTaken>().FirstOrDefault() is { } action && state.Phase == "rph"
        // A Recombination or a transfer in the RPh (A1.32, A4.431; rulings R13.4, R13.5): its units have spent their RPh action.
        ? action.Units.All(state.RallyPhaseActions.Contains)
        : plan.Events.Select(item => item.Payload).OfType<RallyAttempted>().FirstOrDefault() is { } rally
        // A Rally attempt: the unit's attempt is kept for the Player Turn.
        ? state.RallyAttemptsThisPlayerTurn.Contains(rally.Unit)
        : plan.Events.Select(item => item.Payload).OfType<RepairAttempted>().FirstOrDefault() is { } repair
        // A Repair attempt: the unit's attempt is kept for the phase.
        ? state.RepairsThisPhase.Contains(repair.Unit)
        : plan.Events.Select(item => item.Payload).OfType<GunTurned>().FirstOrDefault() is { } turned
        // A CA change without fire (C3.22): the Gun faces its new hexspine.
        ? state.Find(turned.Gun) is EquipmentInstance { Position: MapPosition { Facing: { } facing } } && facing == turned.Facing
        : plan.Events.Select(item => item.Payload).OfType<ManhandlingRolled>().FirstOrDefault() is { } pushed
        // A push (C10.3): the Gun is where its result put it, with its crew.
        ? state.Find(pushed.Gun) is EquipmentInstance { Holding: { } pusher } && state.Location(pusher.Holder)?.Location is { } crewAt
            && (state.Find(pushed.Gun) as EquipmentInstance)?.Position is MapPosition gunAt && gunAt.Location == crewAt
        : plan.Events.Select(item => item.Payload).OfType<GunHooked>().FirstOrDefault() is { } hooked
        // A hook-up or unhooking (C10.11, C10.12): the Gun is towed by the vehicle, or manned by its crew.
        ? state.Find(hooked.Gun) is EquipmentInstance { Holding: { } holder } && holder.Holder == (hooked.Hooked ? hooked.Vehicle : hooked.Crew)
        : plan.Events.Select(item => item.Payload).OfType<ShockRecoveryRolled>().FirstOrDefault() is { } shock
        // A Shock or Unconfirmed Kill dr: the vehicle's dr is kept for the RPh.
        ? state.ShockRollsThisPhase.Contains(shock.Vehicle)
        : plan.Events.Select(item => item.Payload).OfType<CloseCombatResolved>().FirstOrDefault() is { } combat
        // A CC round: the Location's CC records the round and its attackers.
        ? state.CloseCombats.Any(item => item.Location == combat.Location && item.Rounds.Contains(combat.Round) && combat.Attackers.All(item.Attacking.Contains))
        : plan.Events.Select(item => item.Payload).OfType<OrdnanceFired>().FirstOrDefault() is { } ordnance
        // A Gun's shot: the phase's shots count it, with the ROF the record kept; Special Ammunition it did not have ran out instead (C8.9).
        ? (ordnance.Resolution.TryGetProperty("ammunitionUse", out var use) && use.ValueKind == JsonValueKind.String && use.GetString() == "none"
            ? state.DepletedAmmunition.Any(item => item.Gun == ordnance.Gun)
            : state.OrdnanceShots.Any(item => item.Gun == ordnance.Gun && item.RateOfFireKept == ordnance.RateOfFireKept))
        : plan.Events.Select(item => item.Payload).OfType<AmbushRolled>().FirstOrDefault() is { } ambush
        // The Ambush drs: the Location's CC records them and the ambusher.
        ? state.CloseCombats.Any(item => item.Location == ambush.Location && item.AmbushRolled && item.Ambusher == ambush.Ambusher)
        : plan.Events.Select(item => item.Payload).OfType<AdvanceMoved>().FirstOrDefault() is { } advanced
        // An advance: its units are in the Location they entered, CX where the advance made them so (A4.72).
        ? advanced.Units.All(id => state.Location(id)?.Location == advanced.To)
        : plan.Events.Select(item => item.Payload).OfType<PrisonersMassacred>().FirstOrDefault() is { } massacre
        // A Massacre (A20.4): its prisoners are eliminated.
        ? massacre.Prisoners.All(id => state.Unit(id) is { Status: InstanceStatus.Eliminated })
        : plan.Events.Select(item => item.Payload).OfType<VehicleCloseCombatResolved>().FirstOrDefault() is { } vehicleCombat
        // A CC attack with a vehicle (ruling R11.16): in the CCPh the Location's CC records its attackers.
        ? vehicleCombat.Reaction || state.CloseCombats.Any(item => item.Location == vehicleCombat.Location && vehicleCombat.Attackers.All(item.Attacking.Contains))
        : plan.Events.Select(item => item.Payload).OfType<VehicleCloseCombatPassed>().FirstOrDefault() is { } passed
        // A pass in CC with a vehicle (ruling R11.16): the Location's CC records it.
        ? state.CloseCombats.Any(item => item.Location == passed.Location && item.Passed.Contains(passed.Side))
        : plan.Events.Select(item => item.Payload).OfType<PaatcTaken>().FirstOrDefault() is { } paatc && !plan.Events.Any(item => item.Payload is AdvanceMoved or ChoiceMade)
        // A PAATC that stopped the action (A11.6): its units are still in play, pinned on a failure.
        ? paatc.Units.All(id => state.Unit(id) is { } unit && (paatc.Passed || GameState.Condition(unit, Conditions.Pinned) == ConditionState.True))
        : plan.Events.LastOrDefault(item => item.Payload is not (AcquisitionChanged or ChoicePending) && item.Type != "concealment-lost"
            && !(item.Payload is InstanceEliminated && plan.Events.Any(step => step.Payload is VehicleStepped))
            && !(item.Payload is VehicleCheckRolled or DiceRolled && plan.Events.Any(step => step.Payload is VehicleStepped))
            && !(item.Payload is ConditionsChanged && plan.Events.Any(step => step.Payload is PhaseChanged))
            // E1.921 (table player, pass 16): a hidden Starshell firer placed beneath a "?" follows the Starshell's own record.
            && !(item.Payload is ConditionsChanged && plan.Events.Any(step => step.Payload is StarshellFired))
            // A10.62 (ruling R13.1): the DM an action gives broken units follows its own effect.
            && !(item.Payload is ConditionsChanged dm && dm.Conditions.Keys.All(key => key == Conditions.DesperationMorale)))?.Payload switch
        {
            // A Starshell attempt (E1.92; ruling R16.8): its hex has made its attempt this phase.
            StarshellFired starshell => state.StarshellAttempts.Contains(starshell.From.ToString(), StringComparer.Ordinal),

            // Dismantling or assembling a MG (A9.8; ruling R13.6): the MG carries the state and its use.
            ConditionsChanged dismantled when dismantled.Conditions.ContainsKey(Conditions.Dismantled) => state.Find(dismantled.Id) is EquipmentInstance weapon
                && dismantled.Conditions.All(item => GameState.Condition(weapon, item.Key) == item.Value),

            // A vehicle's check with no MP expenditure (D2.5; ruling R11.3): an ESB DR, its effect on the vehicle.
            VehicleCheckRolled check => state.Unit(check.Vehicle) is { } checkedVehicle
                && (check.Result != VehicleCheckRolled.Immobilized || GameState.Condition(checkedVehicle, Conditions.Immobilized) == ConditionState.True),
            // Pass 21 (ruling R21.5): an exit leaves the movers Exited, with no moving stack.
            MovementStepped { Exit: not null } left => state.Movement is null && left.Movers.All(id => state.Unit(id) is { Status: InstanceStatus.Exited }),
            MovementStepped moved => state.Movement is { WindowOpen: true } movement && movement.Step == moved.Step && movement.Location == moved.To,
            // A vehicle's MP expenditure: its window is open at this step, and the vehicle is where the step put it.
            VehicleStepped { Kind: VehicleStepped.Exit } exit => state.Unit(exit.Vehicle) is { Status: InstanceStatus.Exited },
            VehicleStepped stepped => state.Movement is { WindowOpen: true, Vehicle: true } movement && movement.Step == stepped.Step
                && state.Location(stepped.Vehicle)?.Location == stepped.At,

            // The window closed; a vehicle that spent its MP left in its hex has ended its move with it (ruling R5.15).
            MovementWindowClosed => state.Movement is null or { WindowOpen: false },
            MovementEnded ended => ended.Movers.All(id => state.Unit(id) is not { Status: InstanceStatus.Active } unit || unit.MovementEnded),
            // A forced back: the mover is where it started, with its movement ended, and every defender the plan revealed is known.
            EntryForcedBack forced => state.Unit(forced.Id) is { MovementEnded: true } unit && state.Location(unit.Id)?.Location == forced.ReturnedTo
                && Revealed(state, plan),

            // A crew's BU counter placed or removed (D5.33): the vehicle carries the condition.
            ConditionsChanged exposure when plan.Events[^1].Type == "crew-exposure-changed" => state.Unit(exposure.Id) is { } vehicle
                && exposure.Conditions.All(item => GameState.Condition(vehicle, item.Key) == item.Value),

            // Hidden units placed beneath "?" (A12.32; ruling R23.5): each is concealed and no longer hidden.
            ConditionsChanged when plan.Events[^1].Type == "hidden-placed" => plan.Events.Select(item => item.Payload).OfType<ConditionsChanged>()
                .All(placed => state.Unit(placed.Id) is { } unit && GameState.Condition(unit, Conditions.Concealed) == ConditionState.True
                    && GameState.Condition(unit, Conditions.Hidden) != ConditionState.True),

            // A non-OB "?" (A12.12; ruling R23.6): each unit is concealed, and counted as a non-OB "?".
            SetupConcealed => plan.Events.Select(item => item.Payload).OfType<SetupConcealed>()
                .All(placed => state.Unit(placed.Id) is { } unit && GameState.Condition(unit, Conditions.Concealed) == ConditionState.True
                    && state.NonObConcealed.Contains(placed.Id, StringComparer.Ordinal)),

            // A pending declaration: every unit the plan revealed is known, and its attempt is still open.
            ConditionsChanged => plan.Events.Select(item => item.Payload).OfType<EntryAttempted>().Any()
                && state.OpenAttempts.Any(open => open.EventId == plan.Events[0].EventId) && Revealed(state, plan),
            InstanceMoved moved => state.Unit(moved.Id) is { } unit && state.Location(unit.Id) is { } at
                && moved.Position is MapPosition target && at.Location == target.Location,
            PhaseChanged phase => state.Phase == phase.Phase && state.PhasingSide == phase.PhasingSide && state.Turn == phase.Turn,

            // A rejected surrender (A20.3): the unit is eliminated and its side faced with No Quarter.
            SurrenderRejected rejected => state.Unit(rejected.Unit) is { Status: InstanceStatus.Eliminated } unit && state.NoQuarter.Contains(unit.Side),

            // An answered choice: nothing waits for it any more (ruling R5.8).
            ChoiceMade => state.Choice is null || plan.Events.Any(item => item.Payload is ChoicePending),
            InstanceCaptured captured => state.Unit(captured.Id) is { } prisoner && prisoner.Custodian == captured.Custodian,

            // A freed prisoner (A20.5; ruling R14.7): it is no longer guarded, and Unarmed.
            PrisonerFreed freed => state.Unit(freed.Unit) is { Custodian: null } loose && GameState.Condition(loose, Conditions.Unarmed) == ConditionState.True,
            _ => plan.Events.Select(item => item.Payload).OfType<InstanceCreated>().All(created => state.Find(created.Instance.Id) is not null),
        };

    /// <summary>Every unit the plan's events reveal is known after the commit; a placement beneath a "?" reveals nothing.</summary>
    private static bool Revealed(GameState state, GamePlan plan) => plan.Events.Select(item => item.Payload).OfType<ConditionsChanged>()
        .Where(changed => changed.Conditions.TryGetValue(Conditions.Concealed, out var concealed) && concealed == ConditionState.False)
        .All(changed => state.Unit(changed.Id) is { } revealed && GameState.Condition(revealed, Conditions.Concealed) == ConditionState.False
            && GameState.Condition(revealed, Conditions.Hidden) == ConditionState.False);

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
        planner.PlanAsync(action, arguments, tenant, cancellationToken: cancellationToken);

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
        var plan = await planner.PlanAsync(action, arguments, principal.TenantId, principal.PrincipalId, cancellationToken);

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
