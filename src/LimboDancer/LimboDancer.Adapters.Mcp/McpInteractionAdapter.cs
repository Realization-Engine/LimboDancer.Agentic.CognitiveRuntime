using LimboDancer.Abstractions.Execution;
using LimboDancer.Abstractions.Runtime;
using LimboDancer.Runtime.Actions;
using LimboDancer.Runtime.Directed;
using RuntimeExecutionContext = LimboDancer.Abstractions.Execution.ExecutionContext;

namespace LimboDancer.Adapters.Mcp;

public sealed class McpInteractionAdapter : IMcpInteractionAdapter
{
    private const string Protocol = WellKnownActions.McpProtocol;
    private readonly IDirectedActionRuntime runtime;
    private readonly IActionBindingRegistry bindingRegistry;
    private readonly IActionRegistry actionRegistry;
    private readonly McpServerIdentity serverIdentity;
    private readonly TimeProvider timeProvider;
    private readonly TimeSpan invocationTimeout;

    public McpInteractionAdapter(
        IDirectedActionRuntime runtime,
        IActionBindingRegistry bindingRegistry,
        IActionRegistry actionRegistry,
        McpServerIdentity serverIdentity,
        TimeProvider? timeProvider = null,
        TimeSpan? invocationTimeout = null)
    {
        this.runtime = runtime ?? throw new ArgumentNullException(nameof(runtime));
        this.bindingRegistry = bindingRegistry ?? throw new ArgumentNullException(nameof(bindingRegistry));
        this.actionRegistry = actionRegistry ?? throw new ArgumentNullException(nameof(actionRegistry));
        this.serverIdentity = serverIdentity ?? throw new ArgumentNullException(nameof(serverIdentity));
        this.timeProvider = timeProvider ?? TimeProvider.System;
        this.invocationTimeout = invocationTimeout ?? TimeSpan.FromSeconds(30);
        if (this.invocationTimeout <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(invocationTimeout), "Invocation timeout must be positive.");
        }
    }

    public McpDiscoveryResult Discover() => new(
        serverIdentity,
        [McpProtocolVersions.Modern, McpProtocolVersions.LatestLegacy]);

    public McpLegacyInitializeResult InitializeLegacy(McpLegacyInitializeRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.ProtocolVersion);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.ClientName);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.ClientVersion);
        return new McpLegacyInitializeResult(
            McpProtocolVersions.LatestLegacy,
            serverIdentity,
            SupportsTools: true);
    }

    public IReadOnlyList<McpToolDefinition> ListTools()
    {
        var tools = bindingRegistry.List(Protocol)
            .Select(binding =>
            {
                if (!actionRegistry.TryGet(binding.ActionId, binding.Version, out var descriptor))
                {
                    throw new InvalidOperationException(
                        $"MCP binding '{binding.ExternalName}' references an unpublished action.");
                }

                return new McpToolDefinition(
                    binding.ExternalName,
                    descriptor.Description,
                    descriptor.InputSchema,
                    descriptor.OutputSchema);
            })
            .OrderBy(static tool => tool.Name, StringComparer.Ordinal)
            .ToArray();
        return Array.AsReadOnly(tools);
    }

    public async ValueTask<McpToolCallResult> CallToolAsync(
        McpToolCallRequest request,
        McpCallerContext caller,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(caller);
        cancellationToken.ThrowIfCancellationRequested();

        if (request.Metadata.ProtocolVersion is not McpProtocolVersions.Modern
            and not McpProtocolVersions.LatestLegacy)
        {
            return McpToolCallResult.Failure(
                new McpProtocolError(-32600, "unsupported_protocol", "protocol.unsupported", "Unsupported MCP protocol version."));
        }

        if (!bindingRegistry.TryResolve(Protocol, request.Name, out var binding))
        {
            return McpToolCallResult.Failure(
                new McpProtocolError(-32601, "method_not_found", "binding.unknown", "Unknown tool."));
        }

        if (!actionRegistry.TryGet(binding.ActionId, binding.Version, out var descriptor))
        {
            return McpToolCallResult.Failure(
                new McpProtocolError(-32603, "internal_error", "action.unknown", "The tool is unavailable."));
        }

        if (!JsonSchemaSubsetValidator.IsValid(request.Arguments, descriptor.InputSchema))
        {
            return McpToolCallResult.Failure(
                new McpProtocolError(-32602, "invalid_params", "schema.invalid", "Tool arguments were rejected."));
        }

        var invocationId = RuntimeInvocationId.New();
        var correlationId = new CorrelationId(Guid.NewGuid().ToString("N"));
        var directedRequest = new DirectedActionRequest(
            invocationId,
            correlationId,
            caller.TenantId,
            Protocol,
            request.Name,
            request.Arguments);
        var context = new RuntimeExecutionContext(
            invocationId,
            correlationId,
            caller.TenantId,
            caller.Principal,
            new RuntimeBudget(
                maxSteps: 1,
                deadline: timeProvider.GetUtcNow().Add(invocationTimeout),
                maxTokens: null,
                maxCost: null,
                maxExternalCalls: 1,
                maxRetries: 0),
            caller.DiagnosticFindings);

        try
        {
            var result = await runtime
                .ExecuteAsync(directedRequest, context, cancellationToken)
                .ConfigureAwait(false);
            return result.Succeeded
                ? McpToolCallResult.Success(result.Execution?.Output)
                : McpToolCallResult.Failure(MapError(result));
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception)
        {
            return McpToolCallResult.Failure(
                new McpProtocolError(-32603, "internal_error", "runtime.exception", "The tool call failed."));
        }
    }

    private static McpProtocolError MapError(DirectedActionExecutionResult result)
    {
        var runtimeCode = result.Execution?.Code
            ?? result.ReasonCodes.FirstOrDefault()
            ?? "runtime.failed";
        if (result.Resolution is DirectedActionResolution.UnknownBinding or DirectedActionResolution.UnknownAction)
        {
            return new McpProtocolError(-32601, "method_not_found", runtimeCode, "Unknown tool.");
        }

        if (runtimeCode.Contains("invalid_arguments", StringComparison.Ordinal)
            || string.Equals(runtimeCode, "history.append.caller_authority_rejected", StringComparison.Ordinal)
            || string.Equals(runtimeCode, "graph.query.semantic_mapping_unresolved", StringComparison.Ordinal))
        {
            return new McpProtocolError(-32602, "invalid_params", runtimeCode, "Tool arguments were rejected.");
        }

        return result.GateOutcome switch
        {
            ExecutionGateOutcome.Denied or ExecutionGateOutcome.DiagnosticBlocked =>
                new McpProtocolError(-32001, "not_authorized", runtimeCode, "The tool call was not authorized."),
            ExecutionGateOutcome.Stale =>
                new McpProtocolError(-32009, "state_conflict", runtimeCode, "Authoritative state changed."),
            ExecutionGateOutcome.ConfirmationRequired =>
                new McpProtocolError(-32010, "confirmation_required", runtimeCode, "Confirmation is required."),
            _ => new McpProtocolError(-32603, "internal_error", runtimeCode, "The tool call failed."),
        };
    }
}
