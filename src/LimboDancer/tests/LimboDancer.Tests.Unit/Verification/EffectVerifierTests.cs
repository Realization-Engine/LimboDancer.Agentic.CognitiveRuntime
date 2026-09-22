using LimboDancer.Abstractions.Actions;
using LimboDancer.Abstractions.Audit;
using LimboDancer.Abstractions.Execution;
using LimboDancer.Abstractions.Runtime;
using LimboDancer.Abstractions.Verification;
using LimboDancer.Runtime.Execution;
using LimboDancer.Runtime.Verification;
using LimboDancer.Tests.Unit.Actions;
using LimboDancer.Tests.Unit.Runtime;

namespace LimboDancer.Tests.Unit.Verification;

public sealed class EffectVerifierTests
{
    [Fact]
    public async Task VerifiedEffectsProduceVerifiedResultAndAudit()
    {
        var effect = CreateEffect("effect-1");
        var audit = new RecordingAuditSink();
        var verifier = new EffectVerifier(
            [new StubEffectEvaluator(static _ => VerificationStatus.Verified)],
            audit);
        var fixture = CreateFixture([effect]);

        var result = await verifier.VerifyAsync(
            fixture.Action,
            fixture.Execution,
            [effect],
            fixture.Context);

        Assert.Equal(VerificationStatus.Verified, result.Status);
        Assert.Equal("verification.verified", result.ReasonCode);
        Assert.Equal(VerificationStatus.Verified, Assert.Single(result.Findings).Status);
        var auditEvent = Assert.Single(audit.Events);
        Assert.Equal(AuditEventType.EffectVerificationEvaluated, auditEvent.EventType);
        Assert.Equal(VerificationStatus.Verified.ToString(), auditEvent.OutcomeCode);
        Assert.Equal(fixture.Action.AuthorizationId, auditEvent.AuthorizationId);
    }

    [Fact]
    public async Task ContradictedEffectIsExplicitAndAudited()
    {
        var effect = CreateEffect("effect-1");
        var audit = new RecordingAuditSink();
        var verifier = new EffectVerifier(
            [new StubEffectEvaluator(static _ => VerificationStatus.Contradicted)],
            audit);
        var fixture = CreateFixture([effect]);

        var result = await verifier.VerifyAsync(
            fixture.Action,
            fixture.Execution,
            [effect],
            fixture.Context);

        Assert.Equal(VerificationStatus.Contradicted, result.Status);
        Assert.Contains("verification.test", Assert.Single(audit.Events).ReasonCodes);
    }

    [Fact]
    public async Task MissingEvaluatorNeverTreatsExecutionSuccessAsVerification()
    {
        var effect = CreateEffect("effect-1");
        var verifier = new EffectVerifier([], new RecordingAuditSink());
        var fixture = CreateFixture([effect]);

        var result = await verifier.VerifyAsync(
            fixture.Action,
            fixture.Execution,
            [effect],
            fixture.Context);

        Assert.True(fixture.Execution.Succeeded);
        Assert.Equal(VerificationStatus.Unverifiable, result.Status);
        Assert.Equal("verification.evaluator_unavailable", Assert.Single(result.Findings).ReasonCode);
    }

    [Fact]
    public async Task MixedVerifiedAndUnverifiableEffectsArePartiallyVerified()
    {
        var verified = CreateEffect("verified-effect");
        var unavailable = new EffectDescriptor(
            "unavailable-effect",
            "unregistered-effect-type",
            GoalContractsTests.ParseJson("{}"),
            requiredVerification: true);
        var verifier = new EffectVerifier(
            [new StubEffectEvaluator(static _ => VerificationStatus.Verified)],
            new RecordingAuditSink());
        var fixture = CreateFixture([verified, unavailable]);

        var result = await verifier.VerifyAsync(
            fixture.Action,
            fixture.Execution,
            [verified, unavailable],
            fixture.Context);

        Assert.Equal(VerificationStatus.PartiallyVerified, result.Status);
        Assert.Equal(2, result.Findings.Count);
    }

    [Fact]
    public void DefaultPolicyEscalatesContradiction()
    {
        var fixture = CreateFixture([CreateEffect("effect-1")]);
        var result = new EffectVerificationResult(
            VerificationStatus.Contradicted,
            "verification.contradicted");

        var disposition = new DefaultEffectVerificationPolicy().Evaluate(fixture.Action, result);

        Assert.Equal(EffectVerificationDisposition.Escalate, disposition);
    }

    private static VerificationFixture CreateFixture(IReadOnlyList<EffectDescriptor> effects)
    {
        var descriptor = ActionRegistryTests.CreateDescriptor(effects: effects);
        var tenantId = Guid.NewGuid();
        var invocationId = RuntimeInvocationId.New();
        var goal = new Goal(
            GoalId.New(),
            new CorrelationId("verification-test"),
            tenantId,
            null,
            GoalOrigin.System,
            descriptor.Id.Value,
            GoalContractsTests.ParseJson("{}"),
            DateTimeOffset.UtcNow);
        var selected = new SelectedAction(
            new ActionCandidate("verification-candidate", descriptor, GoalContractsTests.ParseJson("{}")),
            SelectionOrigin.SystemRule);
        var action = new AuthorizedAction(
            "verification-authorization",
            selected,
            invocationId,
            goal.CorrelationId,
            tenantId,
            "verification-principal",
            DateTimeOffset.UtcNow,
            expiresAt: null,
            validatedStateVersions: null);
        return new VerificationFixture(
            action,
            new ActionExecutionResult(
                succeeded: true,
                "execution.succeeded",
                GoalContractsTests.ParseJson("{}")),
            new VerificationContext(invocationId, goal, StepId.New()));
    }

    private static EffectDescriptor CreateEffect(string id) => new(
        id,
        StubEffectEvaluator.Type,
        GoalContractsTests.ParseJson("{}"),
        requiredVerification: true);

    private sealed record VerificationFixture(
        AuthorizedAction Action,
        ActionExecutionResult Execution,
        VerificationContext Context);

    private sealed class StubEffectEvaluator(
        Func<EffectDescriptor, VerificationStatus> resolveStatus) : IEffectEvaluator
    {
        public const string Type = "test-effect";

        public string EffectType => Type;

        public ValueTask<EffectVerificationFinding> EvaluateAsync(
            AuthorizedAction action,
            ActionExecutionResult execution,
            EffectDescriptor expectedEffect,
            VerificationContext context,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return ValueTask.FromResult(new EffectVerificationFinding(
                expectedEffect.Id,
                resolveStatus(expectedEffect),
                "verification.test",
                ["observation:test"]));
        }
    }

    private sealed class RecordingAuditSink : IAuditSink
    {
        public List<RuntimeAuditEvent> Events
        {
            get;
        } = [];

        public ValueTask WriteAsync(
            RuntimeAuditEvent auditEvent,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            Events.Add(auditEvent);
            return ValueTask.CompletedTask;
        }
    }
}
