using System.Text.Json;
using LimboDancer.Abstractions.Execution;
using LimboDancer.Abstractions.Observations;
using LimboDancer.Abstractions.Runtime;

namespace LimboDancer.Tests.Unit.Runtime;

public sealed class GoalContractsTests
{
    [Fact]
    public void GoalPreservesTypedIdentityAndDefensivelyCopiesInputs()
    {
        var goalId = GoalId.New();
        var document = JsonDocument.Parse("""{"subject":"building-1"}""");

        var goal = new Goal(
            goalId,
            new CorrelationId("correlation-1"),
            Guid.NewGuid(),
            "session-1",
            GoalOrigin.Mcp,
            "Determine entry eligibility.",
            document.RootElement,
            DateTimeOffset.UtcNow);

        document.Dispose();

        Assert.Equal(goalId, goal.Id);
        Assert.Equal("building-1", goal.Inputs.GetProperty("subject").GetString());
        Assert.Null(typeof(Goal).GetProperty(nameof(Goal.Id))!.SetMethod);
    }

    [Fact]
    public void GoalRejectsNonObjectInputs()
    {
        Assert.Throws<ArgumentException>(() => new Goal(
            GoalId.New(),
            new CorrelationId("correlation-1"),
            Guid.NewGuid(),
            null,
            GoalOrigin.Http,
            "Test intent",
            ParseJson("[]"),
            DateTimeOffset.UtcNow));
    }

    [Fact]
    public void GoalResultRequiresTerminalState()
    {
        Assert.Throws<ArgumentException>(() => new GoalResult(
            GoalId.New(),
            GoalLifecycleState.Reasoning,
            new TerminalReason("still-running")));
    }

    [Fact]
    public void OrchestrationContextRejectsCrossTenantInputs()
    {
        var tenantId = Guid.NewGuid();
        var goal = CreateGoal(tenantId);
        var principal = new RuntimePrincipal("principal-1", tenantId, isAuthenticated: true);
        var observation = new Observation(
            "observation-1",
            new ObservationSource("test"),
            Guid.NewGuid(),
            DateTimeOffset.UtcNow,
            ParseJson("{}"));

        Assert.Throws<ArgumentException>(() => new OrchestrationContext(
            goal,
            principal,
            CreateBudget(),
            GoalLifecycleState.Observing,
            observations: [observation]));
        Assert.Throws<ArgumentException>(() => new OrchestrationContext(
            goal,
            new RuntimePrincipal("principal-2", Guid.NewGuid(), isAuthenticated: true),
            CreateBudget(),
            GoalLifecycleState.Admitted));
    }

    internal static Goal CreateGoal(Guid? tenantId = null) => new(
        GoalId.New(),
        new CorrelationId("correlation-1"),
        tenantId ?? Guid.NewGuid(),
        null,
        GoalOrigin.Mcp,
        "Determine a bounded result.",
        ParseJson("{}"),
        DateTimeOffset.UtcNow);

    internal static RuntimeBudget CreateBudget() => new(
        maxSteps: 10,
        deadline: DateTimeOffset.UtcNow.AddMinutes(1),
        maxTokens: 1_000,
        maxCost: 1,
        maxExternalCalls: 5,
        maxRetries: 1);

    internal static JsonElement ParseJson(string json)
    {
        using var document = JsonDocument.Parse(json);
        return document.RootElement.Clone();
    }
}
