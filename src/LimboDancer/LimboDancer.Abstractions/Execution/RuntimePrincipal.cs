using System.Collections.Frozen;

namespace LimboDancer.Abstractions.Execution;

public sealed class RuntimePrincipal
{
    public RuntimePrincipal(
        string principalId,
        Guid tenantId,
        bool isAuthenticated,
        IEnumerable<string>? permissions = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(principalId);

        var permissionSet = (permissions ?? []).ToFrozenSet(StringComparer.Ordinal);
        if (permissionSet.Any(string.IsNullOrWhiteSpace))
        {
            throw new ArgumentException("Permission identifiers cannot be empty.", nameof(permissions));
        }

        PrincipalId = principalId;
        TenantId = tenantId;
        IsAuthenticated = isAuthenticated;
        Permissions = permissionSet;
    }

    public string PrincipalId
    {
        get;
    }

    public Guid TenantId
    {
        get;
    }

    public bool IsAuthenticated
    {
        get;
    }

    public IReadOnlySet<string> Permissions
    {
        get;
    }
}
