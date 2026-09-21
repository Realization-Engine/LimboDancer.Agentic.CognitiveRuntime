using System.Diagnostics.CodeAnalysis;
using LimboDancer.Abstractions.Actions;

namespace LimboDancer.Runtime.Actions;

public interface IActionRegistry
{
    public bool TryGet(
        ActionId id,
        ActionVersion? version,
        [NotNullWhen(true)] out ActionDescriptor? descriptor);

    public IReadOnlyList<ActionDescriptor> List();
}
