namespace LimboDancer.Adapters.Mcp;

public interface IMcpInteractionAdapter
{
    public McpDiscoveryResult Discover();

    public McpLegacyInitializeResult InitializeLegacy(McpLegacyInitializeRequest request);

    public IReadOnlyList<McpToolDefinition> ListTools();

    public ValueTask<McpToolCallResult> CallToolAsync(
        McpToolCallRequest request,
        McpCallerContext caller,
        CancellationToken cancellationToken = default);
}
