using System.Text.Json;
using LimboDancer.Abstractions.Actions;
using LimboDancer.Abstractions.Audit;
using LimboDancer.Abstractions.Domain;
using LimboDancer.Abstractions.Execution;
using LimboDancer.Abstractions.Runtime;
using LimboDancer.Domains.Asl.Execution;
using LimboDancer.Domains.Asl.ScenarioA1;
using LimboDancer.Runtime.Actions;
using LimboDancer.Runtime.Diagnostics;
using LimboDancer.Runtime.Execution;
using Xunit;
using RuntimeExecutionContext = LimboDancer.Abstractions.Execution.ExecutionContext;

namespace LimboDancer.Domains.Asl.ScenarioA1.Tests;

public sealed class ScenarioA1ReturnGateBindingTests
{
    [Fact]
    public async Task DefaultRiskRequiresConfirmationAndMissingPermissionDenies()
    {
        var state = ScenarioA1SecondDefenderReturnTransitionTests.State();
        var conclusion = await Conclusion();
        var store = new ScenarioA1InMemoryReturnStore([state]);
        var source = new StubConclusions(conclusion);
        var executor = new ScenarioA1ReturnExecutor(store, source);
        var audit = new StubAudit();
        var gate = Gate(store, source, executor, audit, new DefaultExecutionRiskPolicy());

        var permissionDenied = await gate.AuthorizeAsync(Selected(conclusion),
            Context(state.TenantId, permitted: false));
        Assert.Equal(ExecutionGateOutcome.Denied, permissionDenied.Outcome);
        Assert.Null(permissionDenied.AuthorizedAction);
        var confirmation = await gate.AuthorizeAsync(Selected(conclusion),
            Context(state.TenantId, permitted: true));
        Assert.Equal(ExecutionGateOutcome.ConfirmationRequired, confirmation.Outcome);
        Assert.Null(confirmation.AuthorizedAction);
        Assert.Equal(10, (await store.ReadAsync(state.TenantId, state.GameId,
            state.UnitId))!.Version);
    }

    [Fact]
    public async Task GateProducedTokenCommitsOnceWithReadbackAndAudit()
    {
        var state = ScenarioA1SecondDefenderReturnTransitionTests.State();
        var conclusion = await Conclusion();
        var store = new ScenarioA1InMemoryReturnStore([state]);
        var source = new StubConclusions(conclusion);
        var executor = new ScenarioA1ReturnExecutor(store, source);
        var audit = new StubAudit();
        var gate = Gate(store, source, executor, audit, new AllowRisk());
        var selected = Selected(conclusion);
        var authorized = await gate.AuthorizeAsync(selected, Context(state.TenantId, true));
        Assert.Equal(ExecutionGateOutcome.Authorized, authorized.Outcome);
        var token = Assert.IsType<AuthorizedAction>(authorized.AuthorizedAction);
        Assert.Equal("10", token.ValidatedStateVersions[
            ScenarioA1ReturnAction.VersionKey(state.TenantId, state.GameId, state.UnitId)]);

        var first = await new AuditedActionExecutor(executor, audit).ExecuteAsync(token);
        Assert.True(first.Succeeded);
        Assert.Equal("asl.a1.return.applied", first.Code);
        Assert.Contains(audit.Events, item => item.EventType == AuditEventType.GateAuthorized);
        Assert.Contains(audit.Events, item => item.EventType == AuditEventType.ExecutorCompleted);

        var staleToken = await executor.ExecuteAsync(token);
        Assert.False(staleToken.Succeeded);
        Assert.Equal("asl.a1.return.gate-state-stale", staleToken.Code);
        var replayAuthorization = await gate.AuthorizeAsync(selected, Context(state.TenantId, true));
        var replay = await executor.ExecuteAsync(
            Assert.IsType<AuthorizedAction>(replayAuthorization.AuthorizedAction));
        Assert.True(replay.Succeeded);
        Assert.Equal("asl.a1.return.replay", replay.Code);
        var final = (await store.ReadAsync(state.TenantId, state.GameId, state.UnitId))!;
        Assert.Equal(11, final.Version);
        Assert.Equal(2, final.RemainingMf);
        Assert.Equal("bd01:D4:0", final.UnitLocationId);
    }

    [Fact]
    public async Task HazardUnknownConclusionOrCallerAuthorityCannotReachExecutor()
    {
        var state = ScenarioA1SecondDefenderReturnTransitionTests.State() with
        { ReturnHazard = ScenarioA1ReturnHazard.ResidualFirepower };
        var conclusion = await Conclusion();
        var store = new ScenarioA1InMemoryReturnStore([state]);
        var source = new StubConclusions(conclusion);
        var executor = new ScenarioA1ReturnExecutor(store, source);
        var gate = Gate(store, source, executor, new StubAudit(), new AllowRisk());
        Assert.Equal(ExecutionGateOutcome.Denied,
            (await gate.AuthorizeAsync(Selected(conclusion), Context(state.TenantId, true))).Outcome);
        Assert.Equal(ExecutionGateOutcome.Denied,
            (await Gate(store, new StubConclusions(null), executor, new StubAudit(),
                new AllowRisk()).AuthorizeAsync(Selected(conclusion),
                Context(state.TenantId, true))).Outcome);
        Assert.Equal(ExecutionGateOutcome.Denied,
            (await gate.AuthorizeAsync(Selected(conclusion, extra: true),
                Context(state.TenantId, true))).Outcome);
        Assert.Same(state, await store.ReadAsync(state.TenantId, state.GameId, state.UnitId));
    }

    [Fact]
    public async Task JournalBackedGateCommitSurvivesRestartAndReplays()
    {
        var directory = Path.Combine(Path.GetTempPath(),
            "asl-gated-return-" + Guid.NewGuid().ToString("N"));
        try
        {
            var state = ScenarioA1SecondDefenderReturnTransitionTests.State();
            var conclusion = await Conclusion();
            var source = new StubConclusions(conclusion);
            var store = new ScenarioA1JournalReturnStore(directory);
            await store.SeedAsync(state);
            var executor = new ScenarioA1ReturnExecutor(store, source);
            var gate = Gate(store, source, executor, new StubAudit(), new AllowRisk());
            var selected = Selected(conclusion);
            var authorization = await gate.AuthorizeAsync(selected, Context(state.TenantId, true));
            var committed = await executor.ExecuteAsync(
                Assert.IsType<AuthorizedAction>(authorization.AuthorizedAction));
            Assert.Equal("asl.a1.return.applied", committed.Code);

            var reopened = new ScenarioA1JournalReturnStore(directory);
            var executorAfterRestart = new ScenarioA1ReturnExecutor(reopened, source);
            var gateAfterRestart = Gate(reopened, source, executorAfterRestart,
                new StubAudit(), new AllowRisk());
            var replayAuthorization = await gateAfterRestart.AuthorizeAsync(selected,
                Context(state.TenantId, true));
            var replay = await executorAfterRestart.ExecuteAsync(
                Assert.IsType<AuthorizedAction>(replayAuthorization.AuthorizedAction));
            Assert.Equal("asl.a1.return.replay", replay.Code);
            var final = (await reopened.ReadAsync(state.TenantId, state.GameId, state.UnitId))!;
            Assert.Equal(11, final.Version);
            Assert.Equal(2, final.RemainingMf);
            Assert.Equal("bd01:D4:0", final.UnitLocationId);
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    private static async Task<DomainConclusion> Conclusion() =>
        await ScenarioA1SecondDefenderReturnTransitionTests.Conclusion(
            "smc-revealed", "revealedEnemySmc-after-election");

    private static SelectedAction Selected(DomainConclusion conclusion, bool extra = false) =>
        new(new ActionCandidate("candidate", ScenarioA1ReturnAction.Descriptor,
            JsonSerializer.SerializeToElement(extra ? new
            {
                gameId = "game-1", unitId = "squad", attemptId = "attempt-1",
                expectedVersion = 10, conclusionId = conclusion.ConclusionId,
                effects = "caller-controlled",
            } : (object)new
            {
                gameId = "game-1", unitId = "squad", attemptId = "attempt-1",
                expectedVersion = 10, conclusionId = conclusion.ConclusionId,
            })), SelectionOrigin.DirectedCaller);

    private static RuntimeExecutionContext Context(Guid tenant, bool permitted) => new(
        RuntimeInvocationId.New(), new CorrelationId(Guid.NewGuid().ToString("N")),
        tenant, new RuntimePrincipal("test-principal", tenant, isAuthenticated: true,
            permitted ? [ScenarioA1ReturnAction.Permission] : []),
        new RuntimeBudget(10, DateTimeOffset.UtcNow.AddMinutes(1), null, null, 10, 1));

    private static ExecutionGate Gate(IScenarioA1ReturnStateStore store,
        IScenarioA1ReturnConclusionSource source, ScenarioA1ReturnExecutor executor,
        IAuditSink audit, IExecutionRiskPolicy risk) => new(
        new ActionRegistry([ScenarioA1ReturnAction.Descriptor]),
        new ActionExecutorResolver([executor]),
        new ScenarioA1ReturnConstraintEvaluator(store, source),
        new DiagnosticPolicy(), risk, audit);

    private sealed class StubConclusions(DomainConclusion? conclusion)
        : IScenarioA1ReturnConclusionSource
    {
        public ValueTask<DomainConclusion?> ReadAsync(Guid tenantId, string conclusionId,
            CancellationToken cancellationToken = default) =>
            ValueTask.FromResult(conclusion is not null && conclusion.Question.TenantId == tenantId
                && conclusion.ConclusionId == conclusionId ? conclusion : null);
    }

    private sealed class StubAudit : IAuditSink
    {
        public List<RuntimeAuditEvent> Events { get; } = [];
        public ValueTask WriteAsync(RuntimeAuditEvent auditEvent,
            CancellationToken cancellationToken = default)
        {
            Events.Add(auditEvent);
            return ValueTask.CompletedTask;
        }
    }

    private sealed class AllowRisk : IExecutionRiskPolicy
    {
        public RiskEvaluationResult Evaluate(SelectedAction action,
            RuntimeExecutionContext context) => new(RiskEvaluationOutcome.Allowed);
    }
}
