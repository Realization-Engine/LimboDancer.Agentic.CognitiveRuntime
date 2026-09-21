using System.Diagnostics.CodeAnalysis;
using LimboDancer.Abstractions.Actions;

namespace LimboDancer.Runtime.Actions;

public sealed class ActionExecutorResolver : IActionExecutorResolver
{
    private readonly IReadOnlyDictionary<ExecutorBinding, IRuntimeActionExecutor> executors;

    public ActionExecutorResolver(IEnumerable<IRuntimeActionExecutor> executors)
    {
        ArgumentNullException.ThrowIfNull(executors);

        var registered = new Dictionary<ExecutorBinding, IRuntimeActionExecutor>();
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
        [NotNullWhen(true)] out IRuntimeActionExecutor? executor) =>
        executors.TryGetValue(binding, out executor);
}
