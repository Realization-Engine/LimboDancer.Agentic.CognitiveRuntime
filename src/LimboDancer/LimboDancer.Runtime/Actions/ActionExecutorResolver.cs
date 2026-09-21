using System.Diagnostics.CodeAnalysis;
using LimboDancer.Abstractions.Actions;
using LimboDancer.Runtime.Execution;

namespace LimboDancer.Runtime.Actions;

public sealed class ActionExecutorResolver : IActionExecutorResolver
{
    private readonly Dictionary<ExecutorBinding, IActionExecutor> executors;

    public ActionExecutorResolver(IEnumerable<IActionExecutor> executors)
    {
        ArgumentNullException.ThrowIfNull(executors);

        var registered = new Dictionary<ExecutorBinding, IActionExecutor>();
        foreach (var executor in executors)
        {
            ArgumentNullException.ThrowIfNull(executor);
            ArgumentException.ThrowIfNullOrWhiteSpace(executor.ActionId.Value);
            ArgumentException.ThrowIfNullOrWhiteSpace(executor.Binding.Value);

            if (!registered.TryAdd(executor.Binding, executor))
            {
                throw new InvalidOperationException(
                    $"Executor binding '{executor.Binding}' is already registered.");
            }
        }

        this.executors = registered;
    }

    public bool TryResolve(
        ExecutorBinding binding,
        [NotNullWhen(true)] out IActionExecutor? executor) =>
        executors.TryGetValue(binding, out executor);
}
