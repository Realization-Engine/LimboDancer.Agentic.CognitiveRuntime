using LimboDancer.Abstractions.Execution;

namespace LimboDancer.Runtime.Execution;

public interface IExecutionGate
{
    public Task<ExecutionGateResult> AuthorizeAsync(
        SelectedAction action,
        LimboDancer.Abstractions.Execution.ExecutionContext context,
        CancellationToken cancellationToken = default);
}
