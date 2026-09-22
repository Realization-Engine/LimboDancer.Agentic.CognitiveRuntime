using LimboDancer.Abstractions.Actions;

namespace LimboDancer.Runtime.Actions;

public sealed class RegisteredActionResolver : IActionResolver
{
    private readonly IActionRegistry actionRegistry;

    public RegisteredActionResolver(IActionRegistry actionRegistry)
    {
        ArgumentNullException.ThrowIfNull(actionRegistry);
        this.actionRegistry = actionRegistry;
    }

    public Task<IReadOnlyList<ActionCandidate>> ResolveAsync(
        ActionResolutionContext context,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);
        cancellationToken.ThrowIfCancellationRequested();
        var actionId = new ActionId(context.Goal.Intent);
        if (!actionRegistry.TryGet(actionId, null, out var descriptor))
        {
            return Task.FromResult<IReadOnlyList<ActionCandidate>>([]);
        }

        var evidenceRefs = context.Observations
            .Select(static observation => observation.ObservationId)
            .Distinct(StringComparer.Ordinal)
            .ToArray();
        var stateVersions = context.Observations
            .Where(static observation => observation.Version is not null)
            .Select(static observation => new KeyValuePair<string, string>(
                observation.ObservationId,
                observation.Version!))
            .ToArray();
        var candidate = new ActionCandidate(
            $"{context.Goal.Id}:{context.StepId}:{descriptor.Id}:{descriptor.Version}",
            descriptor,
            context.Goal.Inputs,
            evidenceRefs,
            stateVersions);
        return Task.FromResult<IReadOnlyList<ActionCandidate>>([candidate]);
    }
}
