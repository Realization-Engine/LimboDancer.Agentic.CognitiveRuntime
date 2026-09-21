using LimboDancer.Abstractions.Execution;
using LimboDancer.Runtime.Actions;

namespace LimboDancer.Runtime.Execution;

public interface IActionExecutor : IRuntimeActionExecutor
{
    public Task<ActionExecutionResult> ExecuteAsync(
        AuthorizedAction action,
        CancellationToken cancellationToken = default);
}
