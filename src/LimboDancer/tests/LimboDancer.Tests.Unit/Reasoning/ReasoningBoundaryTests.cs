using LimboDancer.Abstractions.Actions;
using LimboDancer.Abstractions.Execution;
using LimboDancer.Abstractions.Reasoning;
using LimboDancer.Abstractions.Runtime;
using LimboDancer.Runtime.Actions;
using LimboDancer.Runtime.Execution;
using LimboDancer.Runtime.Reasoning;
using LimboDancer.Tests.Unit.Actions;
using LimboDancer.Tests.Unit.Domain;
using LimboDancer.Tests.Unit.Observations;
using LimboDancer.Tests.Unit.Runtime;

namespace LimboDancer.Tests.Unit.Reasoning;

public sealed class ReasoningBoundaryTests
{
    [Fact]
    public async Task PassThroughProviderProposesStructuredGoalIntentWithoutLlm()
    {
        var descriptor = ActionRegistryTests.CreateDescriptor();
        var context = CreateContext(descriptor.Id.Value);
        var provider = new PassThroughReasoningProvider();

        var result = await provider.ReasonAsync(context);

        Assert.Equal(ReasoningDisposition.ProposedAction, result.Disposition);
        Assert.Equal(descriptor.Id.Value, result.Intent!.Value);
        Assert.Equal(context.Goal.Inputs, result.Intent.Arguments);
        Assert.Equal(PassThroughReasoningProvider.Id, result.ProviderId);
    }

    [Fact]
    public async Task ReasoningIntentFeedsRegisteredResolutionWithoutInventingAction()
    {
        var descriptor = ActionRegistryTests.CreateDescriptor();
        var reasoningContext = CreateContext(descriptor.Id.Value);
        var reasoning = await new PassThroughReasoningProvider().ReasonAsync(reasoningContext);
        var resolver = new RegisteredActionResolver(new ActionRegistry([descriptor]));

        var candidates = await resolver.ResolveAsync(new ActionResolutionContext(
            reasoningContext.Goal,
            reasoningContext.StepId,
            intent: reasoning.Intent));

        Assert.Same(descriptor, Assert.Single(candidates).Descriptor);
    }

    [Fact]
    public async Task UnknownSemanticIntentIsBlockedBeforeResolution()
    {
        var registry = new ActionRegistry([ActionRegistryTests.CreateDescriptor()]);
        var engine = CreateEngine(registry);

        var evaluation = await engine.ReasonAsync(CreateContext("ldm:action/Unknown"));

        Assert.False(evaluation.Guard.CanContinue);
        Assert.Contains("reasoning.semantic_intent_unresolved", evaluation.Guard.ReasonCodes);
    }

    [Fact]
    public async Task RepeatedProposalWithoutStateChangeIsBlocked()
    {
        var descriptor = ActionRegistryTests.CreateDescriptor();
        var engine = CreateEngine(new ActionRegistry([descriptor]));
        var first = await engine.ReasonAsync(CreateContext(descriptor.Id.Value));
        var history = Assert.IsType<ReasoningStepRecord>(first.StepRecord);

        var repeated = await engine.ReasonAsync(CreateContext(descriptor.Id.Value, history: [history]));

        Assert.False(repeated.Guard.CanContinue);
        Assert.Contains("reasoning.repeated_proposal_without_state_change", repeated.Guard.ReasonCodes);
    }

    [Fact]
    public async Task RepeatedNextStepWithChangedStateIsBlockedExplicitly()
    {
        var descriptor = ActionRegistryTests.CreateDescriptor();
        var engine = CreateEngine(new ActionRegistry([descriptor]));
        var previous = new ReasoningStepRecord(
            descriptor.Id.Value,
            "different-proposal",
            "different-state");

        var repeated = await engine.ReasonAsync(CreateContext(descriptor.Id.Value, history: [previous]));

        Assert.False(repeated.Guard.CanContinue);
        Assert.Contains("reasoning.repeated_next_step", repeated.Guard.ReasonCodes);
    }

    [Fact]
    public async Task DeadlineAndStepBudgetBlockReasoning()
    {
        var descriptor = ActionRegistryTests.CreateDescriptor();
        var engine = CreateEngine(new ActionRegistry([descriptor]));
        var budget = new RuntimeBudget(
            maxSteps: 1,
            deadline: DateTimeOffset.MinValue,
            maxTokens: null,
            maxCost: null,
            maxExternalCalls: 0,
            maxRetries: 0);
        var history = new ReasoningStepRecord("prior", "prior-proposal", "prior-state");

        var evaluation = await engine.ReasonAsync(CreateContext(
            descriptor.Id.Value,
            budget,
            [history]));

        Assert.False(evaluation.Guard.CanContinue);
        Assert.Contains("reasoning.deadline_exceeded", evaluation.Guard.ReasonCodes);
        Assert.Contains("reasoning.step_budget_exhausted", evaluation.Guard.ReasonCodes);
    }

    [Fact]
    public void ResultShapesSupportObservationAndSynthesisBoundaries()
    {
        var package = DomainResolutionContractsTests.CreatePackage("1.0");
        var request = ObservationAcquisitionContractsTests.CreateQuery(Guid.NewGuid(), package);
        var observationRequired = new ReasoningResult(
            ReasoningDisposition.ObservationRequired,
            "test-provider",
            "reasoning.observation_required",
            observationRequests: [request]);
        var completed = new ReasoningResult(
            ReasoningDisposition.Completed,
            "test-provider",
            "reasoning.completed",
            output: GoalContractsTests.ParseJson("{}"));

        Assert.Single(observationRequired.ObservationRequests);
        Assert.NotNull(completed.Output);
        Assert.Throws<ArgumentException>(() => new ReasoningResult(
            ReasoningDisposition.ProposedAction,
            "test-provider",
            "reasoning.invalid"));
    }

    [Fact]
    public void ReasoningCannotCreateExecutionAuthority()
    {
        Assert.DoesNotContain(
            typeof(ReasoningResult).GetProperties(),
            property => property.PropertyType == typeof(AuthorizedAction));
        Assert.DoesNotContain(
            typeof(IReasoningProvider).GetMethods(),
            method => method.ReturnType == typeof(AuthorizedAction));
        Assert.DoesNotContain(
            typeof(IReasoningProvider).GetMethods()
                .SelectMany(static method => method.GetParameters()),
            parameter => parameter.ParameterType == typeof(IActionExecutor));
    }

    private static ReasoningEngine CreateEngine(IActionRegistry registry) => new(
        new PassThroughReasoningProvider(),
        new ReasoningGuard(registry));

    private static ReasoningContext CreateContext(
        string intent,
        RuntimeBudget? budget = null,
        IEnumerable<ReasoningStepRecord>? history = null)
    {
        var goal = new Goal(
            GoalId.New(),
            new CorrelationId("reasoning-test"),
            Guid.NewGuid(),
            null,
            GoalOrigin.System,
            intent,
            GoalContractsTests.ParseJson("{}"),
            DateTimeOffset.UtcNow);
        return new ReasoningContext(
            RuntimeInvocationId.New(),
            goal,
            StepId.New(),
            budget ?? GoalContractsTests.CreateBudget(),
            history: history);
    }
}
