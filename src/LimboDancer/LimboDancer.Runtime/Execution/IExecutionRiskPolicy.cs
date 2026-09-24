using LimboDancer.Abstractions.Execution;

namespace LimboDancer.Runtime.Execution;

public interface IExecutionRiskPolicy
{
    public RiskEvaluationResult Evaluate(
        SelectedAction action,
        LimboDancer.Abstractions.Execution.ExecutionContext context);
}
