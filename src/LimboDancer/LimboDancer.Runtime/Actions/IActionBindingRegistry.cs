using System.Diagnostics.CodeAnalysis;
using LimboDancer.Abstractions.Actions;

namespace LimboDancer.Runtime.Actions;

public interface IActionBindingRegistry
{
    public IReadOnlyList<ActionBinding> List(string protocol);

    public bool TryResolve(
        string protocol,
        string externalName,
        [NotNullWhen(true)] out ActionBinding? binding);
}
