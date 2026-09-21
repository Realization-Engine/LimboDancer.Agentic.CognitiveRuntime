using LimboDancer.Abstractions.Actions;
using LimboDancer.Abstractions.Execution;

namespace LimboDancer.Runtime.Execution;

public sealed class DefaultExecutionRiskPolicy : IExecutionRiskPolicy
{
    public RiskEvaluationResult Evaluate(
        SelectedAction action,
        LimboDancer.Abstractions.Execution.ExecutionContext context)
    {
        ArgumentNullException.ThrowIfNull(action);
        ArgumentNullException.ThrowIfNull(context);

        var risk = action.Candidate.Descriptor.Risk;
        if (risk.Privilege == ActionPrivilege.Privileged
            || risk.Boundary == ActionBoundary.ExternalSideEffect
            || risk.Reversibility == ActionReversibility.Irreversible)
        {
            return new RiskEvaluationResult(
                RiskEvaluationOutcome.ConfirmationRequired,
                ["risk.confirmation_required"]);
        }

        return new RiskEvaluationResult(RiskEvaluationOutcome.Allowed);
    }
}
