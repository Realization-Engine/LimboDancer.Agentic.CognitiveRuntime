namespace LimboDancer.Runtime.Directed;

public interface IDirectedActionRuntime
{
    ValueTask<DirectedActionResult> ResolveAsync(
        DirectedActionRequest request,
        CancellationToken cancellationToken = default);
}
