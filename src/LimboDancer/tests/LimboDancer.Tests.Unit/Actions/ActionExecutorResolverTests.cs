using LimboDancer.Abstractions.Actions;
using LimboDancer.Abstractions.Execution;
using LimboDancer.Runtime.Actions;
using LimboDancer.Runtime.Execution;

namespace LimboDancer.Tests.Unit.Actions;

public sealed class ActionExecutorResolverTests
{
    [Fact]
    public void RegisteredRuntimeExecutorResolves()
    {
        var executor = new TestRuntimeExecutor(
            WellKnownActions.HistoryRead,
            new ExecutorBinding("runtime:executor/HistoryRead"));
        var resolver = new ActionExecutorResolver([executor]);

        var found = resolver.TryResolve(executor.Binding, out var resolved);

        Assert.True(found);
        Assert.Same(executor, resolved);
    }

    [Fact]
    public void UnregisteredExecutorDoesNotResolve()
    {
        var resolver = new ActionExecutorResolver([]);

        var found = resolver.TryResolve(new ExecutorBinding("legacy:executor/HistoryRead"), out var resolved);

        Assert.False(found);
        Assert.Null(resolved);
    }

    internal sealed record TestRuntimeExecutor(ActionId ActionId, ExecutorBinding Binding) : IActionExecutor
    {
        public Task<ActionExecutionResult> ExecuteAsync(
            AuthorizedAction action,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(action);
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult(new ActionExecutionResult(succeeded: true, "test.success"));
        }
    }
}
