using System.Text.Json;
using LimboDancer.Abstractions.Actions;
using LimboDancer.Abstractions.Audit;
using LimboDancer.Abstractions.Observations;
using LimboDancer.Abstractions.Reasoning;
using LimboDancer.Abstractions.Runtime;
using LimboDancer.Abstractions.Execution;
using LimboDancer.Runtime.Actions;
using LimboDancer.Runtime.Decision;
using LimboDancer.Runtime.Diagnostics;
using LimboDancer.Runtime.Execution;
using LimboDancer.Runtime.Orchestration;
using LimboDancer.Runtime.Reasoning;
using LimboDancer.Tests.Unit.Actions;
using LimboDancer.Tests.Unit.Domain;
using LimboDancer.Tests.Unit.Observations;

namespace LimboDancer.Tests.Unit.Runtime;

public sealed class GoalOrchestratorTests
{
    [Fact]
    public async Task CanonicalGoalCompletesThroughGateAndExecutor()
    {
        var descriptor = ActionRegistryTests.CreateDescriptor();
        var executor = new RecordingExecutor(descriptor, GoalContractsTests.ParseJson("""{"ok":true}"""));
        var orchestrator = CreateOrchestrator([descriptor], [executor]);

        var result = await orchestrator.RunAsync(CreateGoal(descriptor.Id.Value));

        Assert.Equal(GoalLifecycleState.Completed, result.TerminalState);
        Assert.Equal("reasoning.single_action_completed", result.Reason.Code);
        Assert.True(result.Output!.Value.GetProperty("ok").GetBoolean());
        Assert.Single(executor.Authorizations);
        Assert.Equal(SelectionOrigin.DecisionProvider, executor.Authorizations[0].Selected.Origin);
    }

    [Fact]
    public async Task NoCandidateTerminatesWithoutExecution()
    {
        var descriptor = ActionRegistryTests.CreateDescriptor();
        var executor = new RecordingExecutor(descriptor);
        var orchestrator = CreateOrchestrator(
            [descriptor],
            [executor],
            actionResolver: new EmptyActionResolver());

        var result = await orchestrator.RunAsync(CreateGoal(descriptor.Id.Value));

        Assert.Equal(GoalLifecycleState.Abstained, result.TerminalState);
        Assert.Equal("resolution.no_candidates", result.Reason.Code);
        Assert.Empty(executor.Authorizations);
    }

    [Fact]
    public async Task ReasoningAbstentionTerminatesWithoutResolution()
    {
        var descriptor = ActionRegistryTests.CreateDescriptor();
        var executor = new RecordingExecutor(descriptor);
        var provider = new DelegateReasoningProvider(static _ => new ReasoningResult(
            ReasoningDisposition.Abstained,
            DelegateReasoningProvider.Id,
            "reasoning.test_abstention"));
        var orchestrator = CreateOrchestrator([descriptor], [executor], provider);

        var result = await orchestrator.RunAsync(CreateGoal(descriptor.Id.Value));

        Assert.Equal(GoalLifecycleState.Abstained, result.TerminalState);
        Assert.Equal("reasoning.test_abstention", result.Reason.Code);
        Assert.Empty(executor.Authorizations);
    }

    [Fact]
    public async Task StaleGateReobservesAndRevalidatesBeforeExecution()
    {
        var descriptor = ActionRegistryTests.CreateDescriptor();
        var executor = new RecordingExecutor(descriptor);
        var package = DomainResolutionContractsTests.CreatePackage("1.0");
        var observationProvider = new VersionedObservationProvider(package);
        var constraintEvaluator = new SequenceConstraintEvaluator(
            ConstraintEvaluationOutcome.Stale,
            ConstraintEvaluationOutcome.Satisfied);
        var provider = new DelegateReasoningProvider(context =>
        {
            if (context.ActionOutcomes.Count != 0)
            {
                return Completed();
            }

            return context.Observations.Count == 0
                ? new ReasoningResult(
                    ReasoningDisposition.ObservationRequired,
                    DelegateReasoningProvider.Id,
                    "reasoning.observe",
                    observationRequests:
                    [
                        ObservationAcquisitionContractsTests.CreateQuery(context.Goal.TenantId, package),
                    ])
                : Proposed(descriptor.Id.Value);
        });
        var orchestrator = CreateOrchestrator(
            [descriptor],
            [executor],
            provider,
            observationProvider: observationProvider,
            gateConstraintEvaluator: constraintEvaluator,
            budget: CreateBudget(maxSteps: 1, maxExternalCalls: 2, maxRetries: 1));

        var result = await orchestrator.RunAsync(CreateGoal(descriptor.Id.Value));

        Assert.Equal(GoalLifecycleState.Completed, result.TerminalState);
        Assert.Equal(2, observationProvider.CallCount);
        Assert.Equal(2, constraintEvaluator.CallCount);
        Assert.Single(executor.Authorizations);
    }

    [Fact]
    public async Task RetryBudgetStopsRepeatedStaleGate()
    {
        var descriptor = ActionRegistryTests.CreateDescriptor();
        var executor = new RecordingExecutor(descriptor);
        var constraintEvaluator = new SequenceConstraintEvaluator(ConstraintEvaluationOutcome.Stale);
        var orchestrator = CreateOrchestrator(
            [descriptor],
            [executor],
            gateConstraintEvaluator: constraintEvaluator,
            budget: CreateBudget(maxSteps: 1, maxExternalCalls: 0, maxRetries: 1));

        var result = await orchestrator.RunAsync(CreateGoal(descriptor.Id.Value));

        Assert.Equal(GoalLifecycleState.Failed, result.TerminalState);
        Assert.Equal("budget.retry_exhausted", result.Reason.Code);
        Assert.Equal(2, constraintEvaluator.CallCount);
        Assert.Empty(executor.Authorizations);
    }

    [Fact]
    public async Task CancellationProducesCancelledTerminalResult()
    {
        var descriptor = ActionRegistryTests.CreateDescriptor();
        var executor = new RecordingExecutor(descriptor);
        var orchestrator = CreateOrchestrator([descriptor], [executor]);
        using var cancellation = new CancellationTokenSource();
        await cancellation.CancelAsync();

        var result = await orchestrator.RunAsync(CreateGoal(descriptor.Id.Value), cancellation.Token);

        Assert.Equal(GoalLifecycleState.Cancelled, result.TerminalState);
        Assert.Equal("orchestration.cancelled", result.Reason.Code);
        Assert.Empty(executor.Authorizations);
    }

    [Fact]
    public async Task MultiStepGoalRevalidatesEverySelectedAction()
    {
        var first = ActionRegistryTests.CreateDescriptor();
        var second = CreateDescriptor("ldm:action/Second", "runtime:executor/Second");
        var firstExecutor = new RecordingExecutor(first);
        var secondExecutor = new RecordingExecutor(second);
        var provider = new DelegateReasoningProvider(context => context.ActionOutcomes.Count switch
        {
            0 => Proposed(first.Id.Value),
            1 => Proposed(second.Id.Value),
            _ => Completed(),
        });
        var orchestrator = CreateOrchestrator(
            [first, second],
            [firstExecutor, secondExecutor],
            provider,
            budget: CreateBudget(maxSteps: 2));

        var result = await orchestrator.RunAsync(CreateGoal(first.Id.Value));

        Assert.Equal(GoalLifecycleState.Completed, result.TerminalState);
        Assert.Single(firstExecutor.Authorizations);
        Assert.Single(secondExecutor.Authorizations);
        Assert.NotEqual(
            firstExecutor.Authorizations[0].AuthorizationId,
            secondExecutor.Authorizations[0].AuthorizationId);
    }

    [Fact]
    public async Task GovernanceDenialCannotBeOverriddenByOrchestration()
    {
        var descriptor = ActionRegistryTests.CreateDescriptor();
        var executor = new RecordingExecutor(descriptor);
        var orchestrator = CreateOrchestrator(
            [descriptor],
            [executor],
            constraintPipeline: new GovernanceDenyingConstraintPipeline());

        var result = await orchestrator.RunAsync(CreateGoal(descriptor.Id.Value));

        Assert.Equal(GoalLifecycleState.Abstained, result.TerminalState);
        Assert.Equal("constraint.no_permitted_candidates", result.Reason.Code);
        Assert.Empty(executor.Authorizations);
    }

    private static GoalOrchestrator CreateOrchestrator(
        IReadOnlyList<ActionDescriptor> descriptors,
        IReadOnlyList<RecordingExecutor> executors,
        IReasoningProvider? reasoningProvider = null,
        IActionResolver? actionResolver = null,
        IActionConstraintPipeline? constraintPipeline = null,
        IObservationProvider? observationProvider = null,
        IActionConstraintEvaluator? gateConstraintEvaluator = null,
        RuntimeBudget? budget = null)
    {
        var registry = new ActionRegistry(descriptors);
        var auditSink = new RecordingAuditSink();
        var executorResolver = new ActionExecutorResolver(executors);
        var reasoning = reasoningProvider ?? new PassThroughReasoningProvider();
        return new GoalOrchestrator(
            new AdmittingPolicy(budget ?? CreateBudget()),
            new ReasoningEngine(reasoning, new ReasoningGuard(registry)),
            observationProvider ?? new EmptyObservationProvider(),
            actionResolver ?? new RegisteredActionResolver(registry),
            constraintPipeline ?? new SemanticActionConstraintPipeline([]),
            new DecisionPlane(new RuleDecisionProvider(), auditSink),
            new DiagnosticRunner([]),
            new ExecutionGate(
                registry,
                executorResolver,
                gateConstraintEvaluator ?? new FailClosedActionConstraintEvaluator(),
                new DiagnosticPolicy(),
                new DefaultExecutionRiskPolicy(),
                auditSink),
            executorResolver,
            auditSink);
    }

    private static Goal CreateGoal(string intent) => new(
        GoalId.New(),
        new CorrelationId("goal-orchestration-test"),
        Guid.NewGuid(),
        null,
        GoalOrigin.System,
        intent,
        GoalContractsTests.ParseJson("{}"),
        DateTimeOffset.UtcNow);

    private static RuntimeBudget CreateBudget(
        int maxSteps = 3,
        int maxExternalCalls = 3,
        int maxRetries = 1) => new(
            maxSteps,
            DateTimeOffset.UtcNow.AddMinutes(1),
            maxTokens: null,
            maxCost: null,
            maxExternalCalls,
            maxRetries);

    private static ReasoningResult Proposed(string intent) => new(
        ReasoningDisposition.ProposedAction,
        DelegateReasoningProvider.Id,
        "reasoning.test_proposal",
        new SemanticActionIntent(intent, GoalContractsTests.ParseJson("{}")));

    private static ReasoningResult Completed() => new(
        ReasoningDisposition.Completed,
        DelegateReasoningProvider.Id,
        "reasoning.test_completed",
        output: GoalContractsTests.ParseJson("{}"));

    private static ActionDescriptor CreateDescriptor(string actionId, string executorId)
    {
        var source = ActionRegistryTests.CreateDescriptor();
        return new ActionDescriptor(
            new ActionId(actionId),
            source.Version,
            actionId,
            null,
            source.InputSchema,
            source.OutputSchema,
            source.Risk,
            requiredPermissions: [],
            preconditions: [],
            expectedEffects: [],
            source.Idempotency,
            new ExecutorBinding(executorId));
    }

    private sealed class AdmittingPolicy(RuntimeBudget budget) : IGoalAdmissionPolicy
    {
        public ValueTask<GoalAdmissionResult> AdmitAsync(
            Goal goal,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return ValueTask.FromResult(new GoalAdmissionResult(
                admitted: true,
                "admission.test",
                new RuntimePrincipal("test-principal", goal.TenantId, isAuthenticated: true),
                budget));
        }
    }

    private sealed class DelegateReasoningProvider(
        Func<ReasoningContext, ReasoningResult> reason) : IReasoningProvider
    {
        public const string Id = "test:reasoning/Delegate";

        public string ProviderId => Id;

        public string? ProviderVersion => null;

        public Task<ReasoningResult> ReasonAsync(
            ReasoningContext context,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult(reason(context));
        }
    }

    private sealed class RecordingExecutor : IActionExecutor
    {
        private readonly JsonElement? output;

        public RecordingExecutor(ActionDescriptor descriptor, JsonElement? output = null)
        {
            ActionId = descriptor.Id;
            Binding = descriptor.Executor;
            this.output = output?.Clone();
        }

        public ActionId ActionId
        {
            get;
        }

        public ExecutorBinding Binding
        {
            get;
        }

        public List<AuthorizedAction> Authorizations
        {
            get;
        } = [];

        public Task<ActionExecutionResult> ExecuteAsync(
            AuthorizedAction action,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            Authorizations.Add(action);
            return Task.FromResult(new ActionExecutionResult(true, "execution.test_succeeded", output));
        }
    }

    private sealed class RecordingAuditSink : IAuditSink
    {
        public ValueTask WriteAsync(
            RuntimeAuditEvent auditEvent,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return ValueTask.CompletedTask;
        }
    }

    private sealed class EmptyActionResolver : IActionResolver
    {
        public Task<IReadOnlyList<ActionCandidate>> ResolveAsync(
            ActionResolutionContext context,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult<IReadOnlyList<ActionCandidate>>([]);
        }
    }

    private sealed class GovernanceDenyingConstraintPipeline : IActionConstraintPipeline
    {
        public Task<ConstraintPipelineResult> EvaluateAsync(
            IReadOnlyList<ActionCandidate> candidates,
            ConstraintContext context,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var rejected = candidates.Select(candidate => new RejectedCandidate(
                candidate,
                [
                    new ConstraintResult(
                        candidate.CandidateId,
                        "governance-test",
                        ConstraintAuthorityClass.Governance,
                        ConstraintOutcome.Failed,
                        "governance.denied"),
                ]));
            return Task.FromResult(new ConstraintPipelineResult([], rejected));
        }
    }

    private sealed class SequenceConstraintEvaluator(
        params ConstraintEvaluationOutcome[] outcomes) : IActionConstraintEvaluator
    {
        public int CallCount
        {
            get;
            private set;
        }

        public Task<ConstraintEvaluationResult> EvaluateAsync(
            SelectedAction action,
            LimboDancer.Abstractions.Execution.ExecutionContext context,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var index = Math.Min(CallCount, outcomes.Length - 1);
            CallCount++;
            return Task.FromResult(new ConstraintEvaluationResult(outcomes[index]));
        }
    }

    private sealed class VersionedObservationProvider(
        LimboDancer.Abstractions.Domain.DomainPackageRef package) : IObservationProvider
    {
        public int CallCount
        {
            get;
            private set;
        }

        public ValueTask<ObservationAcquisitionResult> ObserveAsync(
            ObservationQuery query,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            CallCount++;
            var observation = ObservationAcquisitionContractsTests.CreateObservation(
                query.TenantId,
                package,
                $"state-{CallCount}");
            return ValueTask.FromResult(new ObservationAcquisitionResult(query, [observation]));
        }
    }
}
