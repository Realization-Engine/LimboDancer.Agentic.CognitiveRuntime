using System.Net;
using System.Text;
using System.Text.Json;
using LimboDancer.Abstractions.Actions;
using LimboDancer.Abstractions.Decision;
using LimboDancer.Abstractions.Execution;
using LimboDancer.Abstractions.Runtime;
using LimboDancer.Infrastructure.Decision;
using LimboDancer.Runtime.Actions;

namespace LimboDancer.Tests.Integration.Decision;

public sealed class OpenAiDecisionProviderTests
{
    [Fact]
    public async Task StrictStructuredSelectionReturnsMeasuredDecisionEvidence()
    {
        var handler = new RecordingHandler(CreateResponse(
            DecisionOutcome.Selected,
            "candidate-1",
            "decision.best_candidate",
            inputTokens: 120,
            outputTokens: 30));
        using var provider = CreateProvider(handler);
        var (context, candidates) = await CreateDecisionBoundaryAsync();

        var result = await provider.DecideAsync(context, candidates);

        Assert.Equal(DecisionOutcome.Selected, result.Outcome);
        Assert.Equal("candidate-1", result.SelectedCandidateId);
        Assert.Equal(OpenAiDecisionProvider.Id, result.ProviderId);
        Assert.Equal("test-model-2026-09-01", result.ProviderVersion);
        Assert.Equal(150, result.TokenUsage!.TotalTokens);
        Assert.Equal(0.00048m, result.Cost);
        Assert.NotNull(result.Latency);
        Assert.Contains("\"store\":false", handler.RequestBody, StringComparison.Ordinal);
        Assert.Contains("\"type\":\"json_schema\"", handler.RequestBody, StringComparison.Ordinal);
        Assert.Contains("\"strict\":true", handler.RequestBody, StringComparison.Ordinal);
        Assert.DoesNotContain(context.Goal.TenantId.ToString("D"), handler.RequestBody, StringComparison.Ordinal);
        Assert.DoesNotContain("runtime:executor/TestRead", handler.RequestBody, StringComparison.Ordinal);
    }

    [Fact]
    public async Task UnknownCandidateIsProviderFailure()
    {
        var handler = new RecordingHandler(CreateResponse(
            DecisionOutcome.Selected,
            "candidate-unknown",
            "decision.invalid"));
        using var provider = CreateProvider(handler);
        var (context, candidates) = await CreateDecisionBoundaryAsync();

        await Assert.ThrowsAsync<InvalidOperationException>(() => provider.DecideAsync(context, candidates));
    }

    [Fact]
    public async Task InsufficientTokenBudgetRejectsBeforeTransport()
    {
        var handler = new RecordingHandler(CreateResponse(
            DecisionOutcome.Abstained,
            selectedCandidateId: null,
            reasonCode: "decision.insufficient_evidence"));
        using var provider = CreateProvider(handler);
        var (context, candidates) = await CreateDecisionBoundaryAsync(maxTokens: 1);

        await Assert.ThrowsAsync<InvalidOperationException>(() => provider.DecideAsync(context, candidates));

        Assert.Equal(0, handler.CallCount);
    }

    [Fact]
    public async Task InsufficientCostBudgetRejectsBeforeTransport()
    {
        var handler = new RecordingHandler(CreateResponse(
            DecisionOutcome.Abstained,
            selectedCandidateId: null,
            reasonCode: "decision.insufficient_evidence"));
        using var provider = CreateProvider(handler);
        var (context, candidates) = await CreateDecisionBoundaryAsync(maxCost: 0.000001m);

        await Assert.ThrowsAsync<InvalidOperationException>(() => provider.DecideAsync(context, candidates));

        Assert.Equal(0, handler.CallCount);
    }

    [Theory]
    [InlineData(DecisionOutcome.Abstained, "decision.insufficient_evidence")]
    [InlineData(DecisionOutcome.Escalated, "decision.human_review_required")]
    public async Task StructuredNonSelectionPreservesProviderOutcome(
        DecisionOutcome outcome,
        string reasonCode)
    {
        var handler = new RecordingHandler(CreateResponse(outcome, null, reasonCode));
        using var provider = CreateProvider(handler);
        var (context, candidates) = await CreateDecisionBoundaryAsync();

        var result = await provider.DecideAsync(context, candidates);

        Assert.Equal(outcome, result.Outcome);
        Assert.Null(result.SelectedCandidateId);
        Assert.Equal(reasonCode, result.ReasonCode);
    }

    [Fact]
    public async Task RefusalIsProviderFailureRatherThanAbstention()
    {
        var handler = new RecordingHandler(JsonSerializer.Serialize(new
        {
            status = "completed",
            output = new[]
            {
                new
                {
                    content = new[]
                    {
                        new { type = "refusal", refusal = "cannot decide" },
                    },
                },
            },
            usage = new
            {
                input_tokens = 10,
                output_tokens = 1,
            },
        }));
        using var provider = CreateProvider(handler);
        var (context, candidates) = await CreateDecisionBoundaryAsync();

        await Assert.ThrowsAsync<InvalidOperationException>(() => provider.DecideAsync(context, candidates));
    }

    private static OpenAiDecisionProvider CreateProvider(HttpMessageHandler handler) => new(
        new HttpClient(handler),
        new OpenAiDecisionProviderOptions(
            "test-api-key",
            "test-model-2026-09-01",
            new Uri("https://api.openai.test/v1/responses"),
            TimeSpan.FromSeconds(5),
            maxOutputTokens: 128,
            inputCostPerMillionTokens: 2m,
            outputCostPerMillionTokens: 8m));

    private static async Task<(DecisionContext Context, IReadOnlyList<PermittedAction> Candidates)>
        CreateDecisionBoundaryAsync(long? maxTokens = 10_000, decimal? maxCost = 1m)
    {
        var tenantId = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;
        var goal = new Goal(
            GoalId.New(),
            new CorrelationId("openai-decision-provider-test"),
            tenantId,
            null,
            GoalOrigin.System,
            "choose the safe read action",
            ParseJson("{}"),
            now);
        var budget = new RuntimeBudget(1, now.AddMinutes(1), maxTokens, maxCost, 0, 0);
        var descriptor = new ActionDescriptor(
            new ActionId("ldm:action/TestRead"),
            new ActionVersion("1"),
            "Test read",
            "Reads test state.",
            ParseJson("{\"type\":\"object\"}"),
            ParseJson("{\"type\":\"object\"}"),
            new ActionRiskProfile(
                ActionMutability.ReadOnly,
                ActionIdempotency.Idempotent,
                ActionReversibility.Reversible,
                ActionBoundary.Internal,
                ActionPrivilege.Normal),
            requiredPermissions: [],
            preconditions: [],
            expectedEffects: [],
            IdempotencyMode.Intrinsic,
            new ExecutorBinding("runtime:executor/TestRead"));
        var candidate = new ActionCandidate(
            "candidate-1",
            descriptor,
            ParseJson("{}"),
            evidenceRefs: ["evidence-1"]);
        var principal = new RuntimePrincipal("test-principal", tenantId, isAuthenticated: true);
        var stepId = StepId.New();
        var constrained = await new SemanticActionConstraintPipeline([]).EvaluateAsync(
            [candidate],
            new ConstraintContext(goal, stepId, principal, budget));
        return (
            new DecisionContext(RuntimeInvocationId.New(), goal, stepId, budget),
            constrained.Permitted);
    }

    private static string CreateResponse(
        DecisionOutcome outcome,
        string? selectedCandidateId,
        string reasonCode,
        long inputTokens = 20,
        long outputTokens = 10)
    {
        object[] distribution = selectedCandidateId is null
            ? []
            : [new { candidateId = selectedCandidateId, probability = 0.9 }];
        var decision = JsonSerializer.Serialize(new
        {
            outcome = outcome.ToString(),
            selectedCandidateId,
            reasonCode,
            confidence = 0.9,
            distribution,
        });
        return JsonSerializer.Serialize(new
        {
            status = "completed",
            output = new[]
            {
                new
                {
                    content = new[]
                    {
                        new { type = "output_text", text = decision },
                    },
                },
            },
            usage = new
            {
                input_tokens = inputTokens,
                output_tokens = outputTokens,
            },
        });
    }

    private static JsonElement ParseJson(string json)
    {
        using var document = JsonDocument.Parse(json);
        return document.RootElement.Clone();
    }

    private sealed class RecordingHandler(string responseBody) : HttpMessageHandler
    {
        public int CallCount
        {
            get;
            private set;
        }

        public string RequestBody
        {
            get;
            private set;
        } = string.Empty;

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            CallCount++;
            RequestBody = await request.Content!.ReadAsStringAsync(cancellationToken);
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(responseBody, Encoding.UTF8, "application/json"),
            };
        }
    }
}
