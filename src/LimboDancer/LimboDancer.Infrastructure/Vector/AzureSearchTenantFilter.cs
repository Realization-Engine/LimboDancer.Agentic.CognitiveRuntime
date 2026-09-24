using LimboDancer.Abstractions.State;

namespace LimboDancer.Infrastructure.Vector;

public static class AzureSearchTenantFilter
{
    public static string Build(TenantScope tenant)
    {
        tenant.ThrowIfInvalid();
        return $"tenantId eq '{tenant.TenantId:D}'";
    }
}
