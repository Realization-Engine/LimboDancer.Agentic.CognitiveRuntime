using LimboDancer.Abstractions.Actions;

namespace LimboDancer.Runtime.Actions;

/// <summary>
/// Identifies an executor owned by the new runtime. Executable implementations
/// use <see cref="Execution.IActionExecutor"/> so only authorized actions cross
/// the operational boundary.
/// </summary>
public interface IRuntimeActionExecutor
{
    public ActionId ActionId
    {
        get;
    }

    public ExecutorBinding Binding
    {
        get;
    }
}
