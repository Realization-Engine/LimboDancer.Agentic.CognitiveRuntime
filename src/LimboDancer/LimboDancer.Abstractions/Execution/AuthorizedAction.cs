using System.Collections.Frozen;

namespace LimboDancer.Abstractions.Execution;

public sealed class AuthorizedAction
{
    internal AuthorizedAction(
        string authorizationId,
        SelectedAction selected,
        Guid tenantId,
        DateTimeOffset authorizedAt,
        DateTimeOffset? expiresAt,
        IEnumerable<KeyValuePair<string, string>>? validatedStateVersions)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(authorizationId);
        ArgumentNullException.ThrowIfNull(selected);
        ArgumentOutOfRangeException.ThrowIfEqual(tenantId, Guid.Empty);

        AuthorizationId = authorizationId;
        Selected = selected;
        TenantId = tenantId;
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

    public Guid TenantId
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
