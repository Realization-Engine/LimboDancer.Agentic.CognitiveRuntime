using LimboDancer.Abstractions.Execution;
using RuntimeExecutionContext = LimboDancer.Abstractions.Execution.ExecutionContext;

namespace LimboDancer.Runtime.Directed;

public interface IDirectedActionRuntime
{
    public ValueTask<DirectedActionResult> ResolveAsync(
        DirectedActionRequest request,
        CancellationToken cancellationToken = default);

    public ValueTask<DirectedActionExecutionResult> ExecuteAsync(
        DirectedActionRequest request,
        RuntimeExecutionContext context,
        CancellationToken cancellationToken = default);
}
