using System.Text.Json;
using LimboDancer.Abstractions.Actions;
using LimboDancer.Abstractions.Audit;
using LimboDancer.Abstractions.Diagnostics;
using LimboDancer.Abstractions.Execution;
using LimboDancer.Abstractions.Runtime;
using LimboDancer.Runtime.Actions;
using LimboDancer.Runtime.Diagnostics;
using LimboDancer.Runtime.Execution;
using LimboDancer.Tests.Unit.Actions;
using RuntimeExecutionContext = LimboDancer.Abstractions.Execution.ExecutionContext;

namespace LimboDancer.Tests.Unit.Execution;

public sealed class ExecutionGateTests
{
    private static readonly DiagnosticCheckId CheckId = new("ldm:diagnostic/GateTest");

    [Fact]
    public async Task ValidDirectedSelectionAuthorizes()
    {
        var descriptor = ActionRegistryTests.CreateDescriptor();
        var constraints = new ConstraintEvaluationResult(
            ConstraintEvaluationOutcome.Satisfied,
            validatedStateVersions:
            [
                new KeyValuePair<string, string>("history", "42"),
            ]);
        var gate = CreateGate(descriptor, constraints);

        var result = await gate.AuthorizeAsync(CreateSelection(descriptor), CreateContext());

        Assert.Equal(ExecutionGateOutcome.Authorized, result.Outcome);
        var authorized = Assert.IsType<AuthorizedAction>(result.AuthorizedAction);
        Assert.Equal(descriptor, authorized.Selected.Candidate.Descriptor);
        Assert.Equal("42", authorized.ValidatedStateVersions["history"]);
        Assert.False(string.IsNullOrWhiteSpace(authorized.AuthorizationId));
    }

    [Fact]
    public async Task AuthorizationAndExecutionAreRecordedWithoutPayloads()
    {
        var descriptor = ActionRegistryTests.CreateDescriptor();
        var auditSink = new RecordingAuditSink();
        var gate = CreateGate(descriptor, Satisfied(), auditSink: auditSink);

        var gateResult = await gate.AuthorizeAsync(CreateSelection(descriptor), CreateContext());
        var authorized = Assert.IsType<AuthorizedAction>(gateResult.AuthorizedAction);
        var executor = new AuditedActionExecutor(
            new TestRuntimeExecutor(descriptor.Id, descriptor.Executor),
            auditSink);
        var executionResult = await executor.ExecuteAsync(authorized);

        Assert.True(executionResult.Succeeded);
        var gateEvent = Assert.Single(
            auditSink.Events,
            static auditEvent => auditEvent.EventType == AuditEventType.GateAuthorized);
        Assert.Equal(descriptor.Id, gateEvent.ActionId);
        Assert.Equal(descriptor.Version, gateEvent.ActionVersion);
        Assert.Equal(authorized.InvocationId, gateEvent.InvocationId);
        Assert.Equal(authorized.CorrelationId, gateEvent.CorrelationId);
        Assert.Equal(authorized.TenantId, gateEvent.TenantId);
        Assert.Equal(authorized.PrincipalId, gateEvent.PrincipalId);
        Assert.Equal(authorized.AuthorizationId, gateEvent.AuthorizationId);
        var constraintEvent = Assert.Single(
            auditSink.Events,
            static auditEvent => auditEvent.EventType == AuditEventType.ConstraintEvaluated);
        Assert.Equal(ConstraintEvaluationOutcome.Satisfied.ToString(), constraintEvent.OutcomeCode);
        var completed = Assert.Single(
            auditSink.Events,
            static auditEvent => auditEvent.EventType == AuditEventType.ExecutorCompleted);
        Assert.Equal("test.success", completed.ExecutionCode);
        Assert.DoesNotContain(
            typeof(RuntimeAuditEvent).GetProperties(),
            static property => property.Name.Contains("Argument", StringComparison.OrdinalIgnoreCase)
                || property.Name.Contains("Prompt", StringComparison.OrdinalIgnoreCase)
                || property.Name.Contains("Reasoning", StringComparison.OrdinalIgnoreCase)
                || property.Name.Contains("ChainOfThought", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task ExecutorFailureIsRecordedAndRethrown()
    {
        var descriptor = ActionRegistryTests.CreateDescriptor();
        var auditSink = new RecordingAuditSink();
        var gate = CreateGate(descriptor, Satisfied(), auditSink: auditSink);
        var gateResult = await gate.AuthorizeAsync(CreateSelection(descriptor), CreateContext());
        var authorized = Assert.IsType<AuthorizedAction>(gateResult.AuthorizedAction);
        var executor = new AuditedActionExecutor(
            new ThrowingExecutor(descriptor.Id, descriptor.Executor),
            auditSink);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => executor.ExecuteAsync(authorized));

        var failed = Assert.Single(
            auditSink.Events,
            static auditEvent => auditEvent.EventType == AuditEventType.ExecutorFailed);
        Assert.Equal("Exception", failed.OutcomeCode);
        Assert.Equal(authorized.AuthorizationId, failed.AuthorizationId);
        Assert.Null(failed.ExecutionCode);
    }

    [Fact]
    public async Task MissingTenantDenies()
    {
        var descriptor = ActionRegistryTests.CreateDescriptor();
        var gate = CreateGate(descriptor, Satisfied());
        var principal = new RuntimePrincipal("principal", Guid.Empty, isAuthenticated: true);

        var result = await gate.AuthorizeAsync(
            CreateSelection(descriptor),
            CreateContext(Guid.Empty, principal));

        Assert.Equal(ExecutionGateOutcome.Denied, result.Outcome);
        Assert.Contains("tenant.invalid", result.ReasonCodes);
        Assert.Null(result.AuthorizedAction);
    }

    [Fact]
    public async Task PermissionDenialDenies()
    {
        var descriptor = ActionRegistryTests.CreateDescriptor(permissions: ["history:read"]);
        var auditSink = new RecordingAuditSink();
        var gate = CreateGate(descriptor, Satisfied(), auditSink: auditSink);

        var result = await gate.AuthorizeAsync(CreateSelection(descriptor), CreateContext());

        Assert.Equal(ExecutionGateOutcome.Denied, result.Outcome);
        Assert.Contains("permission.missing:history:read", result.ReasonCodes);
        Assert.Null(result.AuthorizedAction);
        var denied = Assert.Single(
            auditSink.Events,
            static auditEvent => auditEvent.EventType == AuditEventType.GateDenied);
        Assert.Equal(ExecutionGateOutcome.Denied, denied.ExecutionGateOutcome);
        Assert.Equal(descriptor.Version, denied.ActionVersion);
    }

    [Fact]
    public async Task StaleDescriptorInstanceReturnsStale()
    {
        var registered = ActionRegistryTests.CreateDescriptor();
        var staleCopy = ActionRegistryTests.CreateDescriptor();
        var gate = CreateGate(registered, Satisfied());

        var result = await gate.AuthorizeAsync(CreateSelection(staleCopy), CreateContext());

        Assert.Equal(ExecutionGateOutcome.Stale, result.Outcome);
        Assert.Contains("descriptor.stale", result.ReasonCodes);
        Assert.Null(result.AuthorizedAction);
    }

    [Fact]
    public async Task BlockingDiagnosticReturnsDiagnosticBlocked()
    {
        var reference = CreateDiagnosticReference();
        var descriptor = ActionRegistryTests.CreateDescriptor(
            diagnostics: new DiagnosticProfile([reference]));
        var finding = CreateFinding(DiagnosticOutcome.Fail, DiagnosticSeverity.Critical);
        var auditSink = new RecordingAuditSink();
        var gate = CreateGate(
            descriptor,
            Satisfied(),
            new StubDiagnosticCheck(finding),
            auditSink);

        var result = await gate.AuthorizeAsync(
            CreateSelection(descriptor),
            CreateContext(diagnosticFindings: [finding]));

        Assert.Equal(ExecutionGateOutcome.DiagnosticBlocked, result.Outcome);
        Assert.Equal(DiagnosticDisposition.Block, result.DiagnosticDisposition);
        Assert.Contains($"diagnostic.blocked:{CheckId}", result.ReasonCodes);
        Assert.Null(result.AuthorizedAction);
        var diagnosticEvent = Assert.Single(
            auditSink.Events,
            static auditEvent => auditEvent.EventType == AuditEventType.DiagnosticEvaluated);
        Assert.Equal(finding.CheckId, diagnosticEvent.DiagnosticFinding!.CheckId);
        Assert.Equal(finding.Outcome, diagnosticEvent.DiagnosticFinding.Outcome);
        Assert.Null(typeof(AuditDiagnosticFinding).GetProperty("Evidence"));
        Assert.Equal(DiagnosticDisposition.Block, diagnosticEvent.DiagnosticDisposition);
    }

    [Fact]
    public async Task MissingRequiredDiagnosticReturnsDiagnosticBlocked()
    {
        var reference = CreateDiagnosticReference();
        var pass = CreateFinding(DiagnosticOutcome.Pass, DiagnosticSeverity.Info);
        var descriptor = ActionRegistryTests.CreateDescriptor(
            diagnostics: new DiagnosticProfile([reference]));
        var gate = CreateGate(descriptor, Satisfied(), new StubDiagnosticCheck(pass));

        var result = await gate.AuthorizeAsync(CreateSelection(descriptor), CreateContext());

        Assert.Equal(ExecutionGateOutcome.DiagnosticBlocked, result.Outcome);
        Assert.Contains($"diagnostic.missing:{CheckId}", result.ReasonCodes);
    }

    [Fact]
    public async Task FailedRequiredPreconditionDenies()
    {
        var precondition = new PreconditionDescriptor(
            "history.exists",
            PreconditionKind.Operational,
            "runtime:evaluator/HistoryExists",
            ParseJson("{}"),
            required: true);
        var descriptor = ActionRegistryTests.CreateDescriptor(preconditions: [precondition]);
        var gate = CreateGate(
            descriptor,
            new ConstraintEvaluationResult(
                ConstraintEvaluationOutcome.Failed,
                ["precondition.failed:history.exists"]));

        var result = await gate.AuthorizeAsync(CreateSelection(descriptor), CreateContext());

        Assert.Equal(ExecutionGateOutcome.Denied, result.Outcome);
        Assert.Contains("precondition.failed:history.exists", result.ReasonCodes);
        Assert.Null(result.AuthorizedAction);
    }

    [Fact]
    public async Task StaleConstraintStateReturnsStale()
    {
        var descriptor = ActionRegistryTests.CreateDescriptor();
        var gate = CreateGate(
            descriptor,
            new ConstraintEvaluationResult(ConstraintEvaluationOutcome.Stale, ["state.version_changed"]));

        var result = await gate.AuthorizeAsync(CreateSelection(descriptor), CreateContext());

        Assert.Equal(ExecutionGateOutcome.Stale, result.Outcome);
        Assert.Contains("state.version_changed", result.ReasonCodes);
        Assert.Null(result.AuthorizedAction);
    }

    [Fact]
    public async Task ElevatedRiskRequiresConfirmation()
    {
        var risk = new ActionRiskProfile(
            ActionMutability.Write,
            ActionIdempotency.NonIdempotent,
            ActionReversibility.Irreversible,
            ActionBoundary.ExternalSideEffect,
            ActionPrivilege.Privileged);
        var descriptor = ActionRegistryTests.CreateDescriptor(risk: risk);
        var gate = CreateGate(descriptor, Satisfied());

        var result = await gate.AuthorizeAsync(CreateSelection(descriptor), CreateContext());

        Assert.Equal(ExecutionGateOutcome.ConfirmationRequired, result.Outcome);
        Assert.Contains("risk.confirmation_required", result.ReasonCodes);
        Assert.Null(result.AuthorizedAction);
    }

    [Fact]
    public async Task ConfidenceArgumentCannotBypassPermissionGate()
    {
        var descriptor = ActionRegistryTests.CreateDescriptor(permissions: ["history:read"]);
        var gate = CreateGate(descriptor, Satisfied());
        var selection = CreateSelection(descriptor, """{"confidence":1.0}""");

        var result = await gate.AuthorizeAsync(selection, CreateContext());

        Assert.Equal(ExecutionGateOutcome.Denied, result.Outcome);
        Assert.Null(result.AuthorizedAction);
    }

    [Fact]
    public void AuthorizedActionCannotBeCreatedByJsonDeserialization()
    {
        Assert.Throws<NotSupportedException>(
            () => JsonSerializer.Deserialize<AuthorizedAction>("{}"));
    }

    [Fact]
    public void ExecutorContractRequiresAuthorizedAction()
    {
        var parameterType = typeof(IActionExecutor)
            .GetMethod(nameof(IActionExecutor.ExecuteAsync))!
            .GetParameters()
            .Single(static parameter => parameter.ParameterType != typeof(CancellationToken))
            .ParameterType;

        Assert.Equal(typeof(AuthorizedAction), parameterType);
    }

    internal static ExecutionGate CreateGate(
        ActionDescriptor descriptor,
        ConstraintEvaluationResult constraintResult,
        IDiagnosticCheck<DiagnosticContext>? diagnosticCheck = null,
        IAuditSink? auditSink = null)
    {
        IDiagnosticCheck<DiagnosticContext>[] diagnosticChecks =
            diagnosticCheck is null ? [] : [diagnosticCheck];
        var diagnosticRunner = new DiagnosticRunner(
            diagnosticChecks);
        var registry = new ActionRegistry([descriptor], diagnosticRunner);
        var executor = new TestRuntimeExecutor(descriptor.Id, descriptor.Executor);
        return new ExecutionGate(
            registry,
            new ActionExecutorResolver([executor]),
            new StubConstraintEvaluator(constraintResult),
            new DiagnosticPolicy(),
            new DefaultExecutionRiskPolicy(),
            auditSink ?? new RecordingAuditSink());
    }

    private static SelectedAction CreateSelection(
        ActionDescriptor descriptor,
        string arguments = "{}") => new(
            new ActionCandidate("candidate-1", descriptor, ParseJson(arguments)),
            SelectionOrigin.DirectedCaller);

    internal static RuntimeExecutionContext CreateContext(
        Guid? tenantId = null,
        RuntimePrincipal? principal = null,
        IEnumerable<DiagnosticFinding>? diagnosticFindings = null)
    {
        var resolvedTenantId = tenantId ?? Guid.NewGuid();
        return new RuntimeExecutionContext(
            RuntimeInvocationId.New(),
            new CorrelationId("execution-gate-test"),
            resolvedTenantId,
            principal ?? new RuntimePrincipal("principal", resolvedTenantId, isAuthenticated: true),
            new RuntimeBudget(
                maxSteps: 10,
                deadline: DateTimeOffset.UtcNow.AddMinutes(1),
                maxTokens: null,
                maxCost: null,
                maxExternalCalls: 10,
                maxRetries: 1),
            diagnosticFindings);
    }

    private static ConstraintEvaluationResult Satisfied() => new(
        ConstraintEvaluationOutcome.Satisfied);

    private static DiagnosticReference CreateDiagnosticReference() => new(
        CheckId,
        "1",
        required: true,
        isHardInvariant: true,
        GoalLifecycleState.Gating,
        DiagnosticPosition.PreFlight);

    private static DiagnosticFinding CreateFinding(
        DiagnosticOutcome outcome,
        DiagnosticSeverity severity) => new(
            CheckId,
            "1",
            outcome,
            severity,
            "gate.test",
            "Execution gate test finding.",
            DateTimeOffset.UtcNow);

    private static JsonElement ParseJson(string json)
    {
        using var document = JsonDocument.Parse(json);
        return document.RootElement.Clone();
    }

    private sealed class StubConstraintEvaluator(ConstraintEvaluationResult result)
        : IActionConstraintEvaluator
    {
        public Task<ConstraintEvaluationResult> EvaluateAsync(
            SelectedAction action,
            RuntimeExecutionContext context,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(action);
            ArgumentNullException.ThrowIfNull(context);
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult(result);
        }
    }

    private sealed class StubDiagnosticCheck(DiagnosticFinding finding)
        : IDiagnosticCheck<DiagnosticContext>
    {
        public DiagnosticCheckId Id => CheckId;

        public string Version => "1";

        public DiagnosticPosition Position => DiagnosticPosition.PreFlight;

        public Task<DiagnosticFinding> EvaluateAsync(
            DiagnosticContext context,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(context);
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult(finding);
        }
    }

    private sealed record TestRuntimeExecutor(
        ActionId ActionId,
        ExecutorBinding Binding) : IActionExecutor
    {
        public Task<ActionExecutionResult> ExecuteAsync(
            AuthorizedAction action,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(action);
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult(new ActionExecutionResult(succeeded: true, "test.success"));
        }
    }

    private sealed record ThrowingExecutor(
        ActionId ActionId,
        ExecutorBinding Binding) : IActionExecutor
    {
        public Task<ActionExecutionResult> ExecuteAsync(
            AuthorizedAction action,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(action);
            cancellationToken.ThrowIfCancellationRequested();
            throw new InvalidOperationException("Sensitive internal failure detail.");
        }
    }

    private sealed class RecordingAuditSink : IAuditSink
    {
        private readonly List<RuntimeAuditEvent> events = [];

        public IReadOnlyList<RuntimeAuditEvent> Events => events;

        public ValueTask WriteAsync(
            RuntimeAuditEvent auditEvent,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(auditEvent);
            cancellationToken.ThrowIfCancellationRequested();
            events.Add(auditEvent);
            return ValueTask.CompletedTask;
        }
    }
}
