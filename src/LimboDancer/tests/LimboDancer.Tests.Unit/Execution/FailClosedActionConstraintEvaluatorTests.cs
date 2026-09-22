using System.Text.Json;
using LimboDancer.Abstractions.Actions;
using LimboDancer.Abstractions.Execution;
using LimboDancer.Abstractions.Runtime;
using LimboDancer.Runtime.Execution;
using RuntimeExecutionContext = LimboDancer.Abstractions.Execution.ExecutionContext;

namespace LimboDancer.Tests.Unit.Execution;

public sealed class FailClosedActionConstraintEvaluatorTests
{
    [Fact]
    public async Task DescriptorWithoutPreconditionsIsSatisfied()
    {
        var result = await new FailClosedActionConstraintEvaluator().EvaluateAsync(
            CreateSelectedAction(preconditions: []),
            CreateContext());

        Assert.Equal(ConstraintEvaluationOutcome.Satisfied, result.Outcome);
    }

    [Fact]
    public async Task DescriptorWithPreconditionsFailsClosed()
    {
        var result = await new FailClosedActionConstraintEvaluator().EvaluateAsync(
            CreateSelectedAction(
            [
                new PreconditionDescriptor(
                    "state.current",
                    PreconditionKind.Operational,
                    "test.evaluator",
                    ParseJson("{}"),
                    required: true),
            ]),
            CreateContext());

        Assert.Equal(ConstraintEvaluationOutcome.Indeterminate, result.Outcome);
        Assert.Contains("precondition.evaluator_unavailable", result.ReasonCodes);
    }

    private static SelectedAction CreateSelectedAction(IEnumerable<PreconditionDescriptor> preconditions)
    {
        var descriptor = new ActionDescriptor(
            new ActionId("ldm:test/action"),
            new ActionVersion("1.0.0"),
            "Test action",
            "Test action.",
            ParseJson("""{"type":"object"}"""),
            outputSchema: null,
            new ActionRiskProfile(
                ActionMutability.ReadOnly,
                ActionIdempotency.Idempotent,
                ActionReversibility.Reversible,
                ActionBoundary.Internal,
                ActionPrivilege.Normal),
            requiredPermissions: [],
            preconditions,
            expectedEffects: [],
            IdempotencyMode.Intrinsic,
            new ExecutorBinding("test"));
        return new SelectedAction(
            new ActionCandidate("candidate", descriptor, ParseJson("{}")),
            SelectionOrigin.DirectedCaller);
    }

    private static RuntimeExecutionContext CreateContext()
    {
        var tenantId = Guid.NewGuid();
        return new RuntimeExecutionContext(
            RuntimeInvocationId.New(),
            new CorrelationId("correlation"),
            tenantId,
            new RuntimePrincipal("principal", tenantId, isAuthenticated: true),
            new RuntimeBudget(1, DateTimeOffset.UtcNow.AddMinutes(1), null, null, 1, 0));
    }

    private static JsonElement ParseJson(string json)
    {
        using var document = JsonDocument.Parse(json);
        return document.RootElement.Clone();
    }
}
