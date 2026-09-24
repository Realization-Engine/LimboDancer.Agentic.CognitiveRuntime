using LimboDancer.Abstractions.Actions;
using LimboDancer.Abstractions.Execution;
using LimboDancer.Abstractions.Verification;
using LimboDancer.Runtime.Execution;

namespace LimboDancer.Runtime.Verification;

public interface IEffectVerifier
{
    public Task<EffectVerificationResult> VerifyAsync(
        AuthorizedAction action,
        ActionExecutionResult execution,
        IReadOnlyList<EffectDescriptor> expectedEffects,
        VerificationContext context,
        CancellationToken cancellationToken = default);
}
