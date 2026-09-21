using System.Diagnostics.CodeAnalysis;
using LimboDancer.Abstractions.Actions;

namespace LimboDancer.Runtime.Actions;

public interface IActionExecutorResolver
{
    public bool TryResolve(
        ExecutorBinding binding,
        [NotNullWhen(true)] out IRuntimeActionExecutor? executor);
}
