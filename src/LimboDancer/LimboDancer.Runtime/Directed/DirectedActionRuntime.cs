using LimboDancer.Runtime.Actions;

namespace LimboDancer.Runtime.Directed;

public sealed class DirectedActionRuntime : IDirectedActionRuntime
{
    private readonly IActionBindingRegistry bindingRegistry;
    private readonly IActionRegistry actionRegistry;
    private readonly IActionExecutorResolver executorResolver;

    public DirectedActionRuntime(
        IActionBindingRegistry bindingRegistry,
        IActionRegistry actionRegistry,
        IActionExecutorResolver executorResolver)
    {
        this.bindingRegistry = bindingRegistry ?? throw new ArgumentNullException(nameof(bindingRegistry));
        this.actionRegistry = actionRegistry ?? throw new ArgumentNullException(nameof(actionRegistry));
        this.executorResolver = executorResolver ?? throw new ArgumentNullException(nameof(executorResolver));
    }

    public ValueTask<DirectedActionResult> ResolveAsync(
        DirectedActionRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        cancellationToken.ThrowIfCancellationRequested();

        if (!bindingRegistry.TryResolve(request.Protocol, request.ExternalActionName, out var binding))
        {
            return ValueTask.FromResult(
                new DirectedActionResult(DirectedActionResolution.UnknownBinding, null, null));
        }

        if (!actionRegistry.TryGet(binding.ActionId, binding.Version, out var descriptor))
        {
            return ValueTask.FromResult(
                new DirectedActionResult(DirectedActionResolution.UnknownAction, binding, null));
        }

        if (!executorResolver.TryResolve(descriptor.Executor, out var executor)
            || executor.ActionId != descriptor.Id)
        {
            return ValueTask.FromResult(
                new DirectedActionResult(DirectedActionResolution.MissingExecutor, binding, descriptor));
        }

        return ValueTask.FromResult(
            new DirectedActionResult(DirectedActionResolution.Resolved, binding, descriptor));
    }
}
