using System.Collections.Frozen;
using LimboDancer.Abstractions.Runtime;

namespace LimboDancer.Abstractions.Execution;

public sealed class AuthorizedAction
{
    internal AuthorizedAction(
        string authorizationId,
        SelectedAction selected,
        RuntimeInvocationId invocationId,
        CorrelationId correlationId,
        Guid tenantId,
        string principalId,
        DateTimeOffset authorizedAt,
        DateTimeOffset? expiresAt,
        IEnumerable<KeyValuePair<string, string>>? validatedStateVersions)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(authorizationId);
        ArgumentNullException.ThrowIfNull(selected);
        ArgumentOutOfRangeException.ThrowIfEqual(invocationId.Value, Guid.Empty);
        ArgumentException.ThrowIfNullOrWhiteSpace(correlationId.Value);
        ArgumentOutOfRangeException.ThrowIfEqual(tenantId, Guid.Empty);
        ArgumentException.ThrowIfNullOrWhiteSpace(principalId);

        AuthorizationId = authorizationId;
        Selected = selected;
        InvocationId = invocationId;
        CorrelationId = correlationId;
        TenantId = tenantId;
        PrincipalId = principalId;
        AuthorizedAt = authorizedAt;
        ExpiresAt = expiresAt;
        ValidatedStateVersions = (validatedStateVersions ?? [])
            .ToFrozenDictionary(
                static item => item.Key,
                static item => item.Value,
                StringComparer.Ordinal);
    }

    public string AuthorizationId
    {
        get;
    }

    public SelectedAction Selected
    {
        get;
    }

    public RuntimeInvocationId InvocationId
    {
        get;
    }

    public CorrelationId CorrelationId
    {
        get;
    }

    public Guid TenantId
    {
        get;
    }

    public string PrincipalId
    {
        get;
    }

    public DateTimeOffset AuthorizedAt
    {
        get;
    }

    public DateTimeOffset? ExpiresAt
    {
        get;
    }

    public IReadOnlyDictionary<string, string> ValidatedStateVersions
    {
        get;
    }
}
