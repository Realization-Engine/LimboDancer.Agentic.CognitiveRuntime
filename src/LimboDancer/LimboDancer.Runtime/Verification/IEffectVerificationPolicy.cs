using LimboDancer.Abstractions.Execution;
using LimboDancer.Abstractions.Verification;

namespace LimboDancer.Runtime.Verification;

public interface IEffectVerificationPolicy
{
    public EffectVerificationDisposition Evaluate(
        AuthorizedAction action,
        EffectVerificationResult verification);
}
