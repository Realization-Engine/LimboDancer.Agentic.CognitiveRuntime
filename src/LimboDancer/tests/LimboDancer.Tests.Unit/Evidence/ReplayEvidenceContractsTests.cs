using LimboDancer.Abstractions.Evidence;
using LimboDancer.Abstractions.Execution;

namespace LimboDancer.Tests.Unit.Evidence;

public sealed class ReplayEvidenceContractsTests
{
    [Fact]
    public void EvidenceReferencesAuthorizationWithoutRetainingExecutableAuthority()
    {
        var gate = new GateEvidence(
            ExecutionGateOutcome.Authorized,
            authorizationId: "authorization-1");

        Assert.Equal("authorization-1", gate.AuthorizationId);
        Assert.Null(typeof(GateEvidence).GetProperty(nameof(AuthorizedAction)));
        Assert.DoesNotContain(
            typeof(RuntimeStepEvidence).GetProperties(),
            static property => typeof(AuthorizedAction).IsAssignableFrom(property.PropertyType));
    }

    [Fact]
    public void AuthorizedGateEvidenceRequiresAuthorizationReference()
    {
        Assert.Throws<ArgumentException>(() => new GateEvidence(ExecutionGateOutcome.Authorized));
        Assert.Throws<ArgumentException>(() => new GateEvidence(
            ExecutionGateOutcome.Denied,
            authorizationId: "authorization-1"));
    }
}
