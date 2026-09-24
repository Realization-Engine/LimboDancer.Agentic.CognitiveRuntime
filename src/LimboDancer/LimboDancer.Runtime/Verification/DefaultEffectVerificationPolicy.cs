using LimboDancer.Abstractions.Actions;
using LimboDancer.Abstractions.Execution;
using LimboDancer.Abstractions.Verification;

namespace LimboDancer.Runtime.Verification;

public sealed class DefaultEffectVerificationPolicy : IEffectVerificationPolicy
{
    public EffectVerificationDisposition Evaluate(
        AuthorizedAction action,
        EffectVerificationResult verification)
    {
        ArgumentNullException.ThrowIfNull(action);
        ArgumentNullException.ThrowIfNull(verification);
        if (verification.Status == VerificationStatus.Verified)
        {
            return EffectVerificationDisposition.Continue;
        }

        var risk = action.Selected.Candidate.Descriptor.Risk;
        if (verification.Status == VerificationStatus.PartiallyVerified
            && risk.Reversibility == ActionReversibility.Reversible
            && risk.Boundary == ActionBoundary.Internal
            && risk.Privilege == ActionPrivilege.Normal)
        {
            return EffectVerificationDisposition.Continue;
        }

        return EffectVerificationDisposition.Escalate;
    }
}
