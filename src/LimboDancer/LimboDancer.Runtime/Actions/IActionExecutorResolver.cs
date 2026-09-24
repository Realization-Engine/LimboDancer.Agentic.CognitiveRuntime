using System.Diagnostics.CodeAnalysis;
using LimboDancer.Abstractions.Actions;
using LimboDancer.Runtime.Execution;

namespace LimboDancer.Runtime.Actions;

public interface IActionExecutorResolver
{
    public bool TryResolve(
        ExecutorBinding binding,
        [NotNullWhen(true)] out IActionExecutor? executor);
}
