using System.Globalization;
using System.Text.Json;
using LimboDancer.Abstractions.Actions;
using LimboDancer.Abstractions.Execution;
using LimboDancer.Domains.Asl.ScenarioA1;
using LimboDancer.Runtime.Execution;
using RuntimeExecutionContext = LimboDancer.Abstractions.Execution.ExecutionContext;

namespace LimboDancer.Domains.Asl.Execution;

/// <summary>Re-evaluates the exact return against current server-owned state at gate time.</summary>
public sealed class ScenarioA1ReturnConstraintEvaluator(
    IScenarioA1ReturnStateStore store, IScenarioA1ReturnConclusionSource conclusions)
    : IActionConstraintEvaluator
{
    public async Task<ConstraintEvaluationResult> EvaluateAsync(SelectedAction action,
        RuntimeExecutionContext context, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(action);
        ArgumentNullException.ThrowIfNull(context);
        cancellationToken.ThrowIfCancellationRequested();
        if (!ReferenceEquals(action.Candidate.Descriptor, ScenarioA1ReturnAction.Descriptor)
            || !ScenarioA1ReturnAction.TryParse(action.Candidate.Arguments, out var arguments))
            return Failed("asl.a1.return.invalid-action-arguments");
        var state = await store.ReadAsync(context.TenantId, arguments!.GameId,
            arguments.UnitId, cancellationToken);
        var conclusion = await conclusions.ReadAsync(context.TenantId,
            arguments.ConclusionId, cancellationToken);
        if (state is null || conclusion is null
            || conclusion.ConclusionId != arguments.ConclusionId)
            return Failed("asl.a1.return.state-or-conclusion-unavailable");
        var result = ScenarioA1SecondDefenderReturnTransition.Evaluate(state,
            arguments.WithConclusion(conclusion));
        if (result.Status is not (ScenarioA1ReturnTransitionStatus.Applied
            or ScenarioA1ReturnTransitionStatus.Replay))
            return new ConstraintEvaluationResult(result.Status == ScenarioA1ReturnTransitionStatus.Stale
                ? ConstraintEvaluationOutcome.Stale : ConstraintEvaluationOutcome.Failed,
                [result.ReasonCode]);
        return new ConstraintEvaluationResult(ConstraintEvaluationOutcome.Satisfied,
            ["asl.a1.return.exact-current-state"],
            [new KeyValuePair<string, string>(ScenarioA1ReturnAction.VersionKey(
                context.TenantId, arguments.GameId, arguments.UnitId),
                state.Version.ToString(CultureInfo.InvariantCulture))]);
    }

    private static ConstraintEvaluationResult Failed(string reason) =>
        new(ConstraintEvaluationOutcome.Failed, [reason]);
}

/// <summary>Accepts only gate-produced authorization and commits through the CAS store.</summary>
public sealed class ScenarioA1ReturnExecutor(
    IScenarioA1ReturnStateStore store, IScenarioA1ReturnConclusionSource conclusions)
    : IActionExecutor
{
    public ActionId ActionId => ScenarioA1ReturnAction.Id;
    public ExecutorBinding Binding => ScenarioA1ReturnAction.Binding;

    public async Task<ActionExecutionResult> ExecuteAsync(AuthorizedAction action,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(action);
        cancellationToken.ThrowIfCancellationRequested();
        if (!ReferenceEquals(action.Selected.Candidate.Descriptor, ScenarioA1ReturnAction.Descriptor)
            || !ScenarioA1ReturnAction.TryParse(action.Selected.Candidate.Arguments,
                out var arguments))
            return Failed("asl.a1.return.invalid-authorized-action");
        var versionKey = ScenarioA1ReturnAction.VersionKey(action.TenantId,
            arguments!.GameId, arguments.UnitId);
        var state = await store.ReadAsync(action.TenantId, arguments.GameId,
            arguments.UnitId, cancellationToken);
        if (state is null || !action.ValidatedStateVersions.TryGetValue(versionKey,
                out var validatedVersion)
            || validatedVersion != state.Version.ToString(CultureInfo.InvariantCulture))
            return Failed("asl.a1.return.gate-state-stale");
        var conclusion = await conclusions.ReadAsync(action.TenantId,
            arguments.ConclusionId, cancellationToken);
        if (conclusion is null || conclusion.ConclusionId != arguments.ConclusionId)
            return Failed("asl.a1.return.conclusion-unavailable");
        var attempt = arguments.WithConclusion(conclusion);
        var preflight = ScenarioA1SecondDefenderReturnTransition.Evaluate(state, attempt);
        if (preflight.Status is not (ScenarioA1ReturnTransitionStatus.Applied
            or ScenarioA1ReturnTransitionStatus.Replay))
            return Failed(preflight.ReasonCode);
        var result = await new ScenarioA1ReturnSimulation(store).ApplyAsync(action.TenantId,
            arguments.GameId, arguments.UnitId, attempt, cancellationToken);
        if (result.Status is not (ScenarioA1ReturnTransitionStatus.Applied
            or ScenarioA1ReturnTransitionStatus.Replay) || result.State is null)
            return Failed(result.ReasonCode);
        var committed = await store.ReadAsync(action.TenantId, arguments.GameId,
            arguments.UnitId, cancellationToken);
        if (committed is null || committed != result.State
            || committed.UnitLocationId != committed.PreviousLocationId
            || committed.MfExpenditureLocationId != committed.PreviousLocationId
            || !committed.MovementEnded
            || committed.CompletedAttemptId != arguments.AttemptId)
            return Failed("asl.a1.return.effect-readback-failed");
        return new ActionExecutionResult(true,
            result.Status == ScenarioA1ReturnTransitionStatus.Applied
                ? "asl.a1.return.applied" : "asl.a1.return.replay",
            JsonSerializer.SerializeToElement(new
            {
                status = result.Status.ToString(), committed.Version,
                committed.UnitLocationId, committed.RemainingMf,
                committed.MfExpenditureLocationId,
            }), [arguments.GameId + ":" + arguments.UnitId]);
    }

    private static ActionExecutionResult Failed(string code) => new(false, code);
}
