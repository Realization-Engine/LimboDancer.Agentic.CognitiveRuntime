using LimboDancer.Abstractions.Actions;
using LimboDancer.Abstractions.Decision;
using LimboDancer.Abstractions.Evidence;
using LimboDancer.Abstractions.Execution;
using LimboDancer.Abstractions.Observations;
using LimboDancer.Abstractions.Runtime;
using LimboDancer.Abstractions.Verification;
using LimboDancer.Runtime.Execution;

namespace LimboDancer.Runtime.Orchestration;

internal sealed class RuntimeStepEvidenceCapture
{
    private readonly IReplayEvidenceSink sink;
    private readonly TimeProvider timeProvider;
    private readonly RuntimeInvocationId invocationId;
    private readonly Goal goal;
    private readonly StepId stepId;
    private RuntimeBudget budget;
    private readonly IReadOnlyList<Observation> observations;
    private readonly IReadOnlyList<ActionCandidate> candidates;
    private ConstraintPipelineResult? constrained;
    private DecisionResult? decision;
    private ExecutionGateResult? gate;
    private ActionExecutionResult? execution;
    private EffectVerificationResult? verification;
    private bool written;

    public RuntimeStepEvidenceCapture(
        IReplayEvidenceSink sink,
        TimeProvider timeProvider,
        RuntimeInvocationId invocationId,
        Goal goal,
        StepId stepId,
        RuntimeBudget budget,
        IReadOnlyList<Observation> observations,
        IReadOnlyList<ActionCandidate> candidates)
    {
        this.sink = sink ?? throw new ArgumentNullException(nameof(sink));
        this.timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
        this.invocationId = invocationId;
        this.goal = goal ?? throw new ArgumentNullException(nameof(goal));
        this.stepId = stepId;
        this.budget = budget ?? throw new ArgumentNullException(nameof(budget));
        this.observations = observations ?? throw new ArgumentNullException(nameof(observations));
        this.candidates = candidates ?? throw new ArgumentNullException(nameof(candidates));
    }

    public void SetConstraints(ConstraintPipelineResult value) =>
        constrained = value ?? throw new ArgumentNullException(nameof(value));

    public void SetDecision(DecisionResult value) =>
        decision = value ?? throw new ArgumentNullException(nameof(value));

    public void SetDecisionBudget(RuntimeBudget value) =>
        budget = value ?? throw new ArgumentNullException(nameof(value));

    public void SetGate(ExecutionGateResult value) =>
        gate = value ?? throw new ArgumentNullException(nameof(value));

    public void SetExecution(ActionExecutionResult value) =>
        execution = value ?? throw new ArgumentNullException(nameof(value));

    public void SetVerification(EffectVerificationResult value) =>
        verification = value ?? throw new ArgumentNullException(nameof(value));

    public ValueTask WriteAsync(CancellationToken cancellationToken)
    {
        if (written)
        {
            throw new InvalidOperationException("Replay evidence for an attempt may be written only once.");
        }

        written = true;
        var gateEvidence = gate is null
            ? null
            : new GateEvidence(
                gate.Outcome,
                gate.ReasonCodes,
                gate.DiagnosticDisposition,
                gate.AuthorizedAction?.AuthorizationId);
        var executionEvidence = execution is null
            ? null
            : new ExecutionEvidence(
                execution.Succeeded,
                execution.Code,
                execution.Output,
                execution.ProducedResourceIds);
        return sink.WriteAsync(
            new RuntimeStepEvidence(
                Guid.NewGuid(),
                invocationId,
                goal,
                stepId,
                budget,
                timeProvider.GetUtcNow(),
                observations,
                candidates,
                constrained?.Permitted,
                constrained?.Rejected,
                decision,
                gateEvidence,
                executionEvidence,
                verification),
            cancellationToken);
    }
}
