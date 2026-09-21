using LimboDancer.Abstractions.Actions;

namespace LimboDancer.Runtime.Actions;

/// <summary>
/// Identifies an executor owned by the new runtime. The executable contract is
/// added with the execution-gate increment; PR-02 resolves registrations only.
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
