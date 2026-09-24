using LimboDancer.Abstractions.Actions;

namespace LimboDancer.Runtime.Directed;

public enum DirectedActionResolution
{
    Resolved,
    UnknownBinding,
    UnknownAction,
    MissingExecutor,
}

public sealed record DirectedActionResult(
    DirectedActionResolution Resolution,
    ActionBinding? Binding,
    ActionDescriptor? Descriptor)
{
    public bool IsExecutable => Resolution == DirectedActionResolution.Resolved;
}
