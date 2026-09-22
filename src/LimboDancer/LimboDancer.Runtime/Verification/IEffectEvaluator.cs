using LimboDancer.Abstractions.Actions;
using LimboDancer.Abstractions.Execution;
using LimboDancer.Abstractions.Verification;
using LimboDancer.Runtime.Execution;

namespace LimboDancer.Runtime.Verification;

public interface IEffectEvaluator
{
    public string EffectType
    {
        get;
    }

    public ValueTask<EffectVerificationFinding> EvaluateAsync(
        AuthorizedAction action,
        ActionExecutionResult execution,
        EffectDescriptor expectedEffect,
        VerificationContext context,
        CancellationToken cancellationToken = default);
}
