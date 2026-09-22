using LimboDancer.Abstractions.Actions;

namespace LimboDancer.Runtime.Actions;

public interface ISemanticPreconditionEvaluator
{
    public string EvaluatorId
    {
        get;
    }

    public ValueTask<SemanticPreconditionEvaluation> EvaluateAsync(
        ActionCandidate candidate,
        PreconditionDescriptor precondition,
        ConstraintContext context,
        CancellationToken cancellationToken = default);
}
