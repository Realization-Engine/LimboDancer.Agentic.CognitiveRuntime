using System.Diagnostics.CodeAnalysis;
using LimboDancer.Abstractions.Actions;

namespace LimboDancer.Runtime.Actions;

public sealed class ActionBindingRegistry : IActionBindingRegistry
{
    private readonly Dictionary<(string Protocol, string ExternalName), ActionBinding> bindings;

    public ActionBindingRegistry(IEnumerable<ActionBinding> bindings)
    {
        ArgumentNullException.ThrowIfNull(bindings);

        var registered = new Dictionary<(string Protocol, string ExternalName), ActionBinding>();
        foreach (var binding in bindings)
        {
            ArgumentNullException.ThrowIfNull(binding);

            var key = (binding.Protocol, binding.ExternalName);
            if (!registered.TryAdd(key, binding))
            {
                throw new InvalidOperationException(
                    $"Protocol binding '{binding.Protocol}:{binding.ExternalName}' is already registered.");
            }
        }

        this.bindings = registered;
    }

    public bool TryResolve(
        string protocol,
        string externalName,
        [NotNullWhen(true)] out ActionBinding? binding)
    {
        if (string.IsNullOrWhiteSpace(protocol) || string.IsNullOrWhiteSpace(externalName))
        {
            binding = null;
            return false;
        }

        return bindings.TryGetValue((protocol, externalName), out binding);
    }
}
