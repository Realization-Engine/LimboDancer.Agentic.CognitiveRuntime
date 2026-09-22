namespace LimboDancer.Abstractions.Actions;

public interface IActionResolver
{
    public Task<IReadOnlyList<ActionCandidate>> ResolveAsync(
        ActionResolutionContext context,
        CancellationToken cancellationToken = default);
}
