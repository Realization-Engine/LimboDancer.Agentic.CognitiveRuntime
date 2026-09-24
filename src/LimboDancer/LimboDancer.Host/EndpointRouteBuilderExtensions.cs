using System.Text.Json;
using LimboDancer.Adapters.Mcp;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace LimboDancer.Host;

public static class EndpointRouteBuilderExtensions
{
    public static IEndpointRouteBuilder MapLimboDancerEndpoints(this IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);

        endpoints.MapHealthChecks("/health/live", new HealthCheckOptions
        {
            Predicate = static _ => false,
        }).AllowAnonymous();
        endpoints.MapHealthChecks("/health/ready", new HealthCheckOptions
        {
            Predicate = static registration => registration.Tags.Contains("ready"),
        }).AllowAnonymous();

        endpoints.MapGet("/.well-known/mcp", Discover).AllowAnonymous();
        endpoints.MapPost("/mcp/server/discover", Discover).AllowAnonymous();
        endpoints.MapPost("/mcp/initialize", static (
            McpLegacyInitializeRequest request,
            IMcpInteractionAdapter adapter) => Results.Json(adapter.InitializeLegacy(request))).AllowAnonymous();

        var tools = endpoints.MapGroup("/mcp/tools").RequireAuthorization();
        tools.MapGet(string.Empty, static (IMcpInteractionAdapter adapter) => Results.Json(adapter.ListTools()));
        tools.MapPost("/call", CallToolAsync);
        return endpoints;
    }

    private static IResult Discover(IMcpInteractionAdapter adapter) => Results.Json(adapter.Discover());

    private static async Task<IResult> CallToolAsync(
        McpToolCallHttpRequest request,
        HttpContext httpContext,
        IMcpInteractionAdapter adapter,
        IMcpCallerContextFactory callerFactory,
        RuntimeTelemetry telemetry)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (string.IsNullOrWhiteSpace(request.Name)
            || string.IsNullOrWhiteSpace(request.ProtocolVersion)
            || string.IsNullOrWhiteSpace(request.ClientName)
            || string.IsNullOrWhiteSpace(request.ClientVersion)
            || request.Arguments.ValueKind == JsonValueKind.Undefined)
        {
            return Results.BadRequest(new McpProtocolError(
                -32600,
                "invalid_request",
                "request.invalid",
                "The MCP tool call request is invalid."));
        }

        using var activity = telemetry.ActivitySource.StartActivity("mcp.tools.call");
        McpCallerContext caller;
        try
        {
            caller = callerFactory.Create(httpContext.User);
        }
        catch (UnauthorizedAccessException)
        {
            return Results.Unauthorized();
        }

        var result = await adapter.CallToolAsync(
                new McpToolCallRequest(
                    request.Name,
                    request.Arguments,
                    new McpRequestMetadata(
                        request.ProtocolVersion,
                        request.ClientName,
                        request.ClientVersion)),
                caller,
                httpContext.RequestAborted)
            .ConfigureAwait(false);
        telemetry.RecordToolCall(result.IsError);
        activity?.SetTag("mcp.tool.name", request.Name);
        activity?.SetTag("mcp.tool.error", result.IsError);
        return Results.Json(result);
    }
}

public sealed record McpToolCallHttpRequest(
    string Name,
    JsonElement Arguments,
    string ProtocolVersion,
    string ClientName,
    string ClientVersion);
