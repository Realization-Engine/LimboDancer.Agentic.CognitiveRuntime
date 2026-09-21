using LimboDancer.Abstractions.Diagnostics;
using LimboDancer.Abstractions.Runtime;
using LimboDancer.Runtime.Diagnostics;

namespace LimboDancer.Tests.Unit.Diagnostics;

public sealed class DiagnosticPolicyTests
{
    private static readonly DiagnosticCheckId CheckId = new("ldm:diagnostic/Test");

    [Fact]
    public void HardCheckPassContinues()
    {
        var policy = new DiagnosticPolicy();

        var disposition = policy.Evaluate(
            CreateFinding(DiagnosticOutcome.Pass, DiagnosticSeverity.Info),
            CreateContext(isHardInvariant: true));

        Assert.Equal(DiagnosticDisposition.Continue, disposition);
    }

    [Fact]
    public void HardCheckFailBlocks()
    {
        var policy = new DiagnosticPolicy();

        var disposition = policy.Evaluate(
            CreateFinding(DiagnosticOutcome.Fail, DiagnosticSeverity.Critical),
            CreateContext(isHardInvariant: true));

        Assert.Equal(DiagnosticDisposition.Block, disposition);
    }

    [Fact]
    public void HardCheckIndeterminateBlocks()
    {
        var policy = new DiagnosticPolicy();

        var disposition = policy.Evaluate(
            CreateFinding(DiagnosticOutcome.Indeterminate, DiagnosticSeverity.Warning),
            CreateContext(isHardInvariant: true));

        Assert.Equal(DiagnosticDisposition.Block, disposition);
    }

    [Fact]
    public void AdvisoryWarningCanContinueDegraded()
    {
        var policy = new DiagnosticPolicy();

        var disposition = policy.Evaluate(
            CreateFinding(DiagnosticOutcome.Fail, DiagnosticSeverity.Warning),
            CreateContext(isHardInvariant: false));

        Assert.Equal(DiagnosticDisposition.ContinueDegraded, disposition);
    }

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

    private static DiagnosticPolicyContext CreateContext(bool isHardInvariant) => new(
        new DiagnosticReference(
            CheckId,
            "1",
            required: true,
            isHardInvariant,
            GoalLifecycleState.Gating,
            DiagnosticPosition.PreFlight),
        ActionRisk: null);
}
