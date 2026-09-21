using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using LimboDancer.Abstractions.Actions;

namespace LimboDancer.Runtime.Actions;

public sealed class ActionRegistry : IActionRegistry
{
    private readonly ReadOnlyDictionary<(ActionId Id, ActionVersion Version), ActionDescriptor> descriptors;
    private readonly ReadOnlyDictionary<ActionId, ActionDescriptor> unambiguousDescriptors;
    private readonly ReadOnlyCollection<ActionDescriptor> publishedDescriptors;

    public ActionRegistry(IEnumerable<ActionDescriptor> descriptors)
    {
        ArgumentNullException.ThrowIfNull(descriptors);

        var byIdentity = new Dictionary<(ActionId Id, ActionVersion Version), ActionDescriptor>();
        foreach (var descriptor in descriptors)
        {
            ArgumentNullException.ThrowIfNull(descriptor);

            if (!byIdentity.TryAdd((descriptor.Id, descriptor.Version), descriptor))
            {
                throw new InvalidOperationException(
                    $"Action '{descriptor.Id}' version '{descriptor.Version}' is already registered.");
            }
        }

        this.descriptors = new ReadOnlyDictionary<(ActionId Id, ActionVersion Version), ActionDescriptor>(byIdentity);
        publishedDescriptors = new ReadOnlyCollection<ActionDescriptor>(byIdentity.Values.ToArray());
        unambiguousDescriptors = new ReadOnlyDictionary<ActionId, ActionDescriptor>(
            byIdentity.Values
                .GroupBy(static descriptor => descriptor.Id)
                .Where(static group => group.Count() == 1)
                .ToDictionary(static group => group.Key, static group => group.Single()));
    }

    public bool TryGet(
        ActionId id,
        ActionVersion? version,
        [NotNullWhen(true)] out ActionDescriptor? descriptor)
    {
        if (version is { } requestedVersion)
        {
            return descriptors.TryGetValue((id, requestedVersion), out descriptor);
        }

        // Versionless lookup intentionally fails when more than one version is published.
        // This avoids silently changing authority when a new version is registered.
        return unambiguousDescriptors.TryGetValue(id, out descriptor);
    }

    public IReadOnlyList<ActionDescriptor> List() => publishedDescriptors;
}
