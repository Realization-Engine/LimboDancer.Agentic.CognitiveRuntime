using LimboDancer.Abstractions.Actions;
using LimboDancer.Abstractions.Audit;
using LimboDancer.Abstractions.Decision;
using LimboDancer.Abstractions.Diagnostics;
using LimboDancer.Abstractions.Execution;
using LimboDancer.Abstractions.Observations;
using LimboDancer.Abstractions.Reasoning;
using LimboDancer.Abstractions.Runtime;
using LimboDancer.Abstractions.Verification;
using LimboDancer.Runtime.Actions;
using LimboDancer.Runtime.Decision;
using LimboDancer.Runtime.Diagnostics;
using LimboDancer.Runtime.Execution;
using LimboDancer.Runtime.Reasoning;
using LimboDancer.Runtime.Verification;
using RuntimeExecutionContext = LimboDancer.Abstractions.Execution.ExecutionContext;

namespace LimboDancer.Runtime.Orchestration;

public sealed class GoalOrchestrator : IGoalOrchestrator
{
    private readonly IGoalAdmissionPolicy admissionPolicy;
    private readonly IReasoningEngine reasoningEngine;
    private readonly IObservationProvider observationProvider;
    private readonly IActionResolver actionResolver;
    private readonly IActionConstraintPipeline constraintPipeline;
    private readonly IDecisionPlane decisionPlane;
    private readonly IDiagnosticRunner diagnosticRunner;
    private readonly IExecutionGate executionGate;
    private readonly IActionExecutorResolver executorResolver;
    private readonly IAuditSink auditSink;
    private readonly IEffectVerifier effectVerifier;
    private readonly IEffectVerificationPolicy effectVerificationPolicy;
    private readonly TimeProvider timeProvider;

    public GoalOrchestrator(
        IGoalAdmissionPolicy admissionPolicy,
        IReasoningEngine reasoningEngine,
        IObservationProvider observationProvider,
        IActionResolver actionResolver,
        IActionConstraintPipeline constraintPipeline,
        IDecisionPlane decisionPlane,
        IDiagnosticRunner diagnosticRunner,
        IExecutionGate executionGate,
        IActionExecutorResolver executorResolver,
        IAuditSink auditSink,
        IEffectVerifier effectVerifier,
        IEffectVerificationPolicy effectVerificationPolicy,
        TimeProvider? timeProvider = null)
    {
        this.admissionPolicy = admissionPolicy ?? throw new ArgumentNullException(nameof(admissionPolicy));
        this.reasoningEngine = reasoningEngine ?? throw new ArgumentNullException(nameof(reasoningEngine));
        this.observationProvider = observationProvider ?? throw new ArgumentNullException(nameof(observationProvider));
        this.actionResolver = actionResolver ?? throw new ArgumentNullException(nameof(actionResolver));
        this.constraintPipeline = constraintPipeline ?? throw new ArgumentNullException(nameof(constraintPipeline));
        this.decisionPlane = decisionPlane ?? throw new ArgumentNullException(nameof(decisionPlane));
        this.diagnosticRunner = diagnosticRunner ?? throw new ArgumentNullException(nameof(diagnosticRunner));
        this.executionGate = executionGate ?? throw new ArgumentNullException(nameof(executionGate));
        this.executorResolver = executorResolver ?? throw new ArgumentNullException(nameof(executorResolver));
        this.auditSink = auditSink ?? throw new ArgumentNullException(nameof(auditSink));
        this.effectVerifier = effectVerifier ?? throw new ArgumentNullException(nameof(effectVerifier));
        this.effectVerificationPolicy = effectVerificationPolicy
            ?? throw new ArgumentNullException(nameof(effectVerificationPolicy));
        this.timeProvider = timeProvider ?? TimeProvider.System;
    }

    public async Task<GoalResult> RunAsync(
        Goal goal,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(goal);
        var state = GoalLifecycleState.Admitted;
        try
        {
            var admission = await admissionPolicy.AdmitAsync(goal, cancellationToken).ConfigureAwait(false);
            ArgumentNullException.ThrowIfNull(admission);
            if (!admission.Admitted)
            {
                return Result(goal, GoalLifecycleState.Failed, admission.ReasonCode);
            }

            var principal = admission.Principal!;
            var budget = admission.Budget!;
            if (principal.TenantId != goal.TenantId)
            {
                return Result(goal, GoalLifecycleState.Failed, "admission.tenant_mismatch");
            }

            var invocationId = RuntimeInvocationId.New();
            await auditSink.WriteAsync(
                    new RuntimeAuditEvent(
                        Guid.NewGuid(),
                        AuditEventType.InvocationAdmitted,
                        invocationId,
                        goal.CorrelationId,
                        goal.TenantId,
                        timeProvider.GetUtcNow(),
                        principalId: principal.PrincipalId,
                        outcomeCode: admission.ReasonCode,
                        goalId: goal.Id),
                    cancellationToken)
                .ConfigureAwait(false);

            var observations = new Dictionary<string, Observation>(StringComparer.Ordinal);
            var observationQueries = new Dictionary<string, ObservationQuery>(StringComparer.Ordinal);
            var history = new List<ReasoningStepRecord>();
            var actionOutcomes = new List<ReasoningActionOutcome>();
            var externalCalls = 0;
            var retries = 0;

            Transition(ref state, GoalLifecycleState.Observing);
            Transition(ref state, GoalLifecycleState.Reasoning);
            while (true)
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (timeProvider.GetUtcNow() >= budget.Deadline)
                {
                    return Terminal(goal, ref state, GoalLifecycleState.Failed, "budget.deadline_exceeded");
                }

                var stepId = StepId.New();
                var reasoning = await reasoningEngine.ReasonAsync(
                        new ReasoningContext(
                            invocationId,
                            goal,
                            stepId,
                            budget,
                            observations.Values,
                            history,
                            actionOutcomes),
                        cancellationToken)
                    .ConfigureAwait(false);
                if (!reasoning.Guard.CanContinue)
                {
                    return Terminal(
                        goal,
                        ref state,
                        GoalLifecycleState.Failed,
                        reasoning.Guard.ReasonCodes[0]);
                }

                if (reasoning.Result.Disposition == ReasoningDisposition.Completed)
                {
                    return Terminal(
                        goal,
                        ref state,
                        GoalLifecycleState.Completed,
                        reasoning.Result.ReasonCode,
                        reasoning.Result.Output);
                }

                if (reasoning.Result.Disposition == ReasoningDisposition.Abstained)
                {
                    return Terminal(
                        goal,
                        ref state,
                        GoalLifecycleState.Abstained,
                        reasoning.Result.ReasonCode);
                }

                if (reasoning.Result.Disposition == ReasoningDisposition.ObservationRequired)
                {
                    Transition(ref state, GoalLifecycleState.Observing);
                    var observationFailure = await AcquireObservationsAsync(
                            reasoning.Result.ObservationRequests,
                            goal,
                            budget,
                            observations,
                            observationQueries,
                            externalCalls,
                            cancellationToken)
                        .ConfigureAwait(false);
                    externalCalls += reasoning.Result.ObservationRequests.Count;
                    if (observationFailure is not null)
                    {
                        return Terminal(goal, ref state, GoalLifecycleState.Failed, observationFailure);
                    }

                    Transition(ref state, GoalLifecycleState.Reasoning);
                    continue;
                }

                var stepRecord = reasoning.StepRecord
                    ?? throw new InvalidOperationException("An action proposal requires a Reasoning step record.");
                history.Add(stepRecord);
                Transition(ref state, GoalLifecycleState.Resolving);
                var candidates = await actionResolver.ResolveAsync(
                        new ActionResolutionContext(
                            goal,
                            stepId,
                            observations.Values,
                            reasoning.Result.Intent),
                        cancellationToken)
                    .ConfigureAwait(false);
                if (candidates.Count == 0)
                {
                    return Terminal(goal, ref state, GoalLifecycleState.Abstained, "resolution.no_candidates");
                }

                Transition(ref state, GoalLifecycleState.Constraining);
                var constrained = await constraintPipeline.EvaluateAsync(
                        candidates,
                        new ConstraintContext(goal, stepId, principal, budget, observations.Values),
                        cancellationToken)
                    .ConfigureAwait(false);
                if (constrained.Permitted.Count == 0)
                {
                    return Terminal(goal, ref state, GoalLifecycleState.Abstained, "constraint.no_permitted_candidates");
                }

                Transition(ref state, GoalLifecycleState.Deciding);
                var decision = await decisionPlane.DecideAsync(
                        new DecisionContext(invocationId, goal, stepId, budget, observations.Values),
                        constrained.Permitted,
                        cancellationToken)
                    .ConfigureAwait(false);
                if (decision.Decision.Outcome == DecisionOutcome.Abstained)
                {
                    return Terminal(
                        goal,
                        ref state,
                        GoalLifecycleState.Abstained,
                        decision.Decision.ReasonCode);
                }

                if (decision.Decision.Outcome == DecisionOutcome.Escalated)
                {
                    return Terminal(
                        goal,
                        ref state,
                        GoalLifecycleState.Escalated,
                        decision.Decision.ReasonCode);
                }

                var selected = decision.SelectedAction
                    ?? throw new InvalidOperationException("A selected Decision requires a selected action.");
                Transition(ref state, GoalLifecycleState.Gating);
                var findings = await diagnosticRunner.RunAsync(
                        new DiagnosticContext(
                            invocationId,
                            goal.CorrelationId,
                            goal.TenantId,
                            GoalLifecycleState.Gating,
                            DiagnosticPosition.PreFlight,
                            selected.Candidate.Descriptor),
                        selected.Candidate.Descriptor.Diagnostics,
                        cancellationToken)
                    .ConfigureAwait(false);
                var gate = await executionGate.AuthorizeAsync(
                        selected,
                        new RuntimeExecutionContext(
                            invocationId,
                            goal.CorrelationId,
                            goal.TenantId,
                            principal,
                            budget,
                            findings),
                        cancellationToken)
                    .ConfigureAwait(false);
                if (gate.Outcome == ExecutionGateOutcome.Stale)
                {
                    if (retries >= budget.MaxRetries)
                    {
                        return Terminal(goal, ref state, GoalLifecycleState.Failed, "budget.retry_exhausted");
                    }

                    retries++;
                    history.RemoveAt(history.Count - 1);
                    Transition(ref state, GoalLifecycleState.Observing);
                    var refreshFailure = await AcquireObservationsAsync(
                            observationQueries.Values,
                            goal,
                            budget,
                            observations,
                            observationQueries,
                            externalCalls,
                            cancellationToken)
                        .ConfigureAwait(false);
                    externalCalls += observationQueries.Count;
                    if (refreshFailure is not null)
                    {
                        return Terminal(goal, ref state, GoalLifecycleState.Failed, refreshFailure);
                    }

                    Transition(ref state, GoalLifecycleState.Reasoning);
                    continue;
                }

                if (gate.Outcome == ExecutionGateOutcome.ConfirmationRequired)
                {
                    Transition(ref state, GoalLifecycleState.AwaitingConfirmation);
                    return Terminal(goal, ref state, GoalLifecycleState.Escalated, "confirmation.required");
                }

                if (gate.Outcome == ExecutionGateOutcome.DiagnosticBlocked)
                {
                    if (gate.DiagnosticDisposition == DiagnosticDisposition.Escalate)
                    {
                        return Terminal(
                            goal,
                            ref state,
                            GoalLifecycleState.Escalated,
                            GateReason(gate));
                    }

                    if (gate.DiagnosticDisposition == DiagnosticDisposition.FailGoal)
                    {
                        return Terminal(
                            goal,
                            ref state,
                            GoalLifecycleState.Failed,
                            GateReason(gate));
                    }

                    if (gate.DiagnosticDisposition is DiagnosticDisposition.Retry
                        or DiagnosticDisposition.ReObserve)
                    {
                        if (retries >= budget.MaxRetries)
                        {
                            return Terminal(goal, ref state, GoalLifecycleState.Failed, "budget.retry_exhausted");
                        }

                        retries++;
                        history.RemoveAt(history.Count - 1);
                        Transition(ref state, GoalLifecycleState.Observing);
                        if (gate.DiagnosticDisposition == DiagnosticDisposition.ReObserve)
                        {
                            var refreshFailure = await AcquireObservationsAsync(
                                    observationQueries.Values,
                                    goal,
                                    budget,
                                    observations,
                                    observationQueries,
                                    externalCalls,
                                    cancellationToken)
                                .ConfigureAwait(false);
                            externalCalls += observationQueries.Count;
                            if (refreshFailure is not null)
                            {
                                return Terminal(goal, ref state, GoalLifecycleState.Failed, refreshFailure);
                            }
                        }

                        Transition(ref state, GoalLifecycleState.Reasoning);
                        continue;
                    }
                }

                if (gate.Outcome != ExecutionGateOutcome.Authorized || gate.AuthorizedAction is null)
                {
                    return Terminal(
                        goal,
                        ref state,
                        GoalLifecycleState.Abstained,
                        GateReason(gate));
                }

                var descriptor = selected.Candidate.Descriptor;
                if (!executorResolver.TryResolve(descriptor.Executor, out var executor)
                    || executor.ActionId != descriptor.Id)
                {
                    return Terminal(goal, ref state, GoalLifecycleState.Failed, "executor.unresolved");
                }

                var beforeExecutionObservations = observations.Values.ToArray();
                Transition(ref state, GoalLifecycleState.Executing);
                var execution = await new AuditedActionExecutor(executor, auditSink, timeProvider)
                    .ExecuteAsync(gate.AuthorizedAction, cancellationToken)
                    .ConfigureAwait(false);
                Transition(ref state, GoalLifecycleState.Verifying);
                if (!execution.Succeeded)
                {
                    actionOutcomes.Add(new ReasoningActionOutcome(
                        stepId,
                        reasoning.Result.Intent!.Value,
                        succeeded: false,
                        execution.Code,
                        execution.Output));
                    return Terminal(goal, ref state, GoalLifecycleState.Failed, execution.Code);
                }

                EffectVerificationResult? verification = null;
                if (descriptor.Verification.RequireEffectVerification)
                {
                    var refreshFailure = await AcquireObservationsAsync(
                            observationQueries.Values,
                            goal,
                            budget,
                            observations,
                            observationQueries,
                            externalCalls,
                            cancellationToken)
                        .ConfigureAwait(false);
                    externalCalls += observationQueries.Count;
                    if (refreshFailure is not null)
                    {
                        return Terminal(goal, ref state, GoalLifecycleState.Failed, refreshFailure);
                    }

                    verification = await effectVerifier.VerifyAsync(
                            gate.AuthorizedAction,
                            execution,
                            descriptor.ExpectedEffects,
                            new VerificationContext(
                                invocationId,
                                goal,
                                stepId,
                                beforeExecutionObservations,
                                observations.Values),
                            cancellationToken)
                        .ConfigureAwait(false);
                }

                actionOutcomes.Add(new ReasoningActionOutcome(
                    stepId,
                    reasoning.Result.Intent!.Value,
                    succeeded: true,
                    execution.Code,
                    execution.Output,
                    verification?.Status));
                if (verification is not null)
                {
                    EffectVerificationDisposition disposition;
                    try
                    {
                        disposition = effectVerificationPolicy.Evaluate(gate.AuthorizedAction, verification);
                        if (!Enum.IsDefined(disposition))
                        {
                            throw new InvalidOperationException("Effect verification policy returned an invalid disposition.");
                        }
                    }
                    catch (Exception)
                    {
                        return Terminal(goal, ref state, GoalLifecycleState.Failed, "verification.policy_failed");
                    }

                    if (disposition == EffectVerificationDisposition.Escalate)
                    {
                        return Terminal(
                            goal,
                            ref state,
                            GoalLifecycleState.Escalated,
                            verification.ReasonCode);
                    }

                    if (disposition == EffectVerificationDisposition.FailGoal)
                    {
                        return Terminal(
                            goal,
                            ref state,
                            GoalLifecycleState.Failed,
                            verification.ReasonCode);
                    }
                }

                Transition(ref state, GoalLifecycleState.Reasoning);
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            return Terminal(goal, ref state, GoalLifecycleState.Cancelled, "orchestration.cancelled");
        }
    }

    private async Task<string?> AcquireObservationsAsync(
        IEnumerable<ObservationQuery> requests,
        Goal goal,
        RuntimeBudget budget,
        Dictionary<string, Observation> observations,
        Dictionary<string, ObservationQuery> knownQueries,
        int externalCalls,
        CancellationToken cancellationToken)
    {
        var queryValues = requests.ToArray();
        if (externalCalls + queryValues.Length > budget.MaxExternalCalls)
        {
            return "budget.external_calls_exhausted";
        }

        foreach (var query in queryValues)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (query.TenantId != goal.TenantId)
            {
                return "observation.tenant_mismatch";
            }

            var acquisition = await observationProvider.ObserveAsync(query, cancellationToken).ConfigureAwait(false);
            ArgumentNullException.ThrowIfNull(acquisition);
            if (!ReferenceEquals(acquisition.Query, query))
            {
                return "observation.query_mismatch";
            }

            knownQueries[query.QueryId] = query;
            foreach (var observation in acquisition.Observations)
            {
                observations[observation.ObservationId] = observation;
            }
        }

        return null;
    }

    private static GoalResult Terminal(
        Goal goal,
        ref GoalLifecycleState state,
        GoalLifecycleState terminalState,
        string reasonCode,
        System.Text.Json.JsonElement? output = null)
    {
        Transition(ref state, terminalState);
        return Result(goal, terminalState, reasonCode, output);
    }

    private static string GateReason(ExecutionGateResult gate) =>
        gate.ReasonCodes.Count == 0 ? "gate.denied" : gate.ReasonCodes[0];

    private static GoalResult Result(
        Goal goal,
        GoalLifecycleState terminalState,
        string reasonCode,
        System.Text.Json.JsonElement? output = null) => new(
            goal.Id,
            terminalState,
            new TerminalReason(reasonCode),
            output);

    private static void Transition(ref GoalLifecycleState state, GoalLifecycleState next)
    {
        GoalLifecycleTransitionPolicy.EnsureTransition(state, next);
        state = next;
    }
}
