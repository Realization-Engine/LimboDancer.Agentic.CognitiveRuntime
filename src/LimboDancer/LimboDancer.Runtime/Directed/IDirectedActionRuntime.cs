namespace LimboDancer.Runtime.Directed;

public interface IDirectedActionRuntime
{
    public ValueTask<DirectedActionResult> ResolveAsync(
        DirectedActionRequest request,
        CancellationToken cancellationToken = default);
}
