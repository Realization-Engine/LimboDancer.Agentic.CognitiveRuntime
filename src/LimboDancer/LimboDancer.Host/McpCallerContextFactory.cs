using System.Security.Claims;
using LimboDancer.Abstractions.Execution;
using LimboDancer.Adapters.Mcp;

namespace LimboDancer.Host;

public interface IMcpCallerContextFactory
{
    public McpCallerContext Create(ClaimsPrincipal principal);
}

public sealed class McpCallerContextFactory : IMcpCallerContextFactory
{
    public McpCallerContext Create(ClaimsPrincipal principal)
    {
        ArgumentNullException.ThrowIfNull(principal);

        var principalId = principal.FindFirstValue(ClaimTypes.NameIdentifier);
        var tenantValue = principal.FindFirstValue(LimboDancerClaimTypes.TenantId);
        if (principal.Identity?.IsAuthenticated != true
            || string.IsNullOrWhiteSpace(principalId)
            || !Guid.TryParse(tenantValue, out var tenantId)
            || tenantId == Guid.Empty)
        {
            throw new UnauthorizedAccessException("An authenticated principal with a valid tenant is required.");
        }

        var permissions = principal.FindAll(LimboDancerClaimTypes.Permission)
            .Select(static claim => claim.Value);
        return new McpCallerContext(
            tenantId,
            new RuntimePrincipal(principalId, tenantId, isAuthenticated: true, permissions));
    }
}
