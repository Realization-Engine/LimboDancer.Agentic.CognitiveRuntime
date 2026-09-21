using LimboDancer.Abstractions.Actions;
using LimboDancer.Abstractions.Diagnostics;
using LimboDancer.Abstractions.Runtime;
using LimboDancer.Runtime.Actions;
using LimboDancer.Runtime.Diagnostics;
using LimboDancer.Tests.Unit.Actions;

namespace LimboDancer.Tests.Unit.Diagnostics;

public sealed class DiagnosticRunnerTests
{
    private static readonly DiagnosticCheckId CheckId = new("ldm:diagnostic/Test");

    [Fact]
    public async Task RunnerProducesStructuredFinding()
    {
        var expected = CreateFinding(DiagnosticOutcome.Pass, DiagnosticSeverity.Info);
        var runner = new DiagnosticRunner([new StubCheck(expected)]);

        var findings = await runner.RunAsync(CreateContext(), CreateProfile());

        var finding = Assert.Single(findings);
        Assert.Equal(CheckId, finding.CheckId);
        Assert.Equal("1", finding.CheckVersion);
        Assert.Equal(DiagnosticOutcome.Pass, finding.Outcome);
        Assert.Equal(DiagnosticSeverity.Info, finding.Severity);
        Assert.Equal("test.code", finding.Code);
    }

    [Fact]
    public async Task MissingOptionalCheckProducesWarningFinding()
    {
        var runner = new DiagnosticRunner([]);
        var profile = new DiagnosticProfile(
        [
            new DiagnosticReference(
                CheckId,
                "1",
                required: false,
                isHardInvariant: false,
                GoalLifecycleState.Gating,
                DiagnosticPosition.PreFlight),
        ]);

        var findings = await runner.RunAsync(CreateContext(), profile);

        var finding = Assert.Single(findings);
        Assert.Equal(DiagnosticOutcome.Indeterminate, finding.Outcome);
        Assert.Equal(DiagnosticSeverity.Warning, finding.Severity);
        Assert.Equal("diagnostic.check_missing", finding.Code);
    }

    [Fact]
    public void MissingRequiredCheckPreventsDescriptorPublication()
    {
        var runner = new DiagnosticRunner([]);
        var descriptor = ActionRegistryTests.CreateDescriptor(diagnostics: CreateProfile());

        var exception = Assert.Throws<InvalidOperationException>(
            () => new ActionRegistry([descriptor], runner));

        Assert.Contains(CheckId.Value, exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void ResolvableRequiredCheckAllowsDescriptorPublication()
    {
        var check = new StubCheck(CreateFinding(DiagnosticOutcome.Pass, DiagnosticSeverity.Info));
        var runner = new DiagnosticRunner([check]);
        var descriptor = ActionRegistryTests.CreateDescriptor(diagnostics: CreateProfile());

        var registry = new ActionRegistry([descriptor], runner);

        Assert.Same(descriptor, Assert.Single(registry.List()));
    }

    [Fact]
    public async Task CheckTimeoutProducesBoundedIndeterminateFinding()
    {
        var runner = new DiagnosticRunner(
            [new NeverCompletingCheck()],
            checkTimeout: TimeSpan.FromMilliseconds(25));

        var findings = await runner.RunAsync(CreateContext(), CreateProfile());

        var finding = Assert.Single(findings);
        Assert.Equal(DiagnosticOutcome.Indeterminate, finding.Outcome);
        Assert.Equal("diagnostic.timeout", finding.Code);
    }

    [Fact]
    public async Task CallerCancellationIsPropagated()
    {
        var runner = new DiagnosticRunner([new NeverCompletingCheck()]);
        using var cancellationSource = new CancellationTokenSource();
        await cancellationSource.CancelAsync();

        await Assert.ThrowsAsync<OperationCanceledException>(
            () => runner.RunAsync(CreateContext(), CreateProfile(), cancellationSource.Token));
    }

    [Fact]
    public async Task CallerCancellationDuringEvaluationIsPropagated()
    {
        var runner = new DiagnosticRunner([new NeverCompletingCheck()]);
        using var cancellationSource = new CancellationTokenSource(TimeSpan.FromMilliseconds(25));

        await Assert.ThrowsAsync<OperationCanceledException>(
            () => runner.RunAsync(CreateContext(), CreateProfile(), cancellationSource.Token));
    }

    [Fact]
    public void DiagnosticContractsCannotProduceAuthorizedAction()
    {
        var exposedTypes = new[]
        {
            typeof(IDiagnosticCheck),
            typeof(IDiagnosticCheck<DiagnosticContext>),
            typeof(IDiagnosticRunner),
            typeof(IDiagnosticPolicy),
        }
        .SelectMany(static type => type.GetMethods())
        .SelectMany(static method =>
            method.GetParameters()
                .Select(static parameter => parameter.ParameterType)
                .Append(method.ReturnType))
        .SelectMany(Flatten)
        .Distinct();

        Assert.DoesNotContain(
            exposedTypes,
            static type => string.Equals(type.Name, "AuthorizedAction", StringComparison.Ordinal));
    }

    private static DiagnosticContext CreateContext() => new(
        RuntimeInvocationId.New(),
        new CorrelationId("diagnostic-test"),
        Guid.NewGuid(),
        GoalLifecycleState.Gating,
        DiagnosticPosition.PreFlight);

    private static DiagnosticProfile CreateProfile() => new(
    [
        new DiagnosticReference(
            CheckId,
            "1",
            required: true,
            isHardInvariant: true,
            GoalLifecycleState.Gating,
            DiagnosticPosition.PreFlight),
    ]);

    private static DiagnosticFinding CreateFinding(
        DiagnosticOutcome outcome,
        DiagnosticSeverity severity) => new(
            CheckId,
            "1",
            outcome,
            severity,
            "test.code",
            "Test finding.",
            DateTimeOffset.UtcNow);

    private static IEnumerable<Type> Flatten(Type type)
    {
        yield return type;
        foreach (var argument in type.GetGenericArguments())
        {
            foreach (var nested in Flatten(argument))
            {
                yield return nested;
            }
        }
    }

    private sealed class StubCheck(DiagnosticFinding finding) : IDiagnosticCheck<DiagnosticContext>
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

    private sealed class NeverCompletingCheck : IDiagnosticCheck<DiagnosticContext>
    {
        public DiagnosticCheckId Id => CheckId;

        public string Version => "1";

        public DiagnosticPosition Position => DiagnosticPosition.PreFlight;

        public Task<DiagnosticFinding> EvaluateAsync(
            DiagnosticContext context,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(context);
            return new TaskCompletionSource<DiagnosticFinding>(
                TaskCreationOptions.RunContinuationsAsynchronously).Task;
        }
    }
}
