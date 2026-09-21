using System.Diagnostics.CodeAnalysis;
using LimboDancer.Abstractions.Actions;

namespace LimboDancer.Runtime.Actions;

public interface IActionExecutorResolver
{
    bool TryResolve(
        ExecutorBinding binding,
        [NotNullWhen(true)] out IRuntimeActionExecutor? executor);
}
