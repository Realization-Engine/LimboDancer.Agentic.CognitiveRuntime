namespace LimboDancer.Abstractions.State;

public readonly record struct TenantScope
{
    public TenantScope(Guid tenantId)
    {
        ArgumentOutOfRangeException.ThrowIfEqual(tenantId, Guid.Empty);
        TenantId = tenantId;
    }

    public Guid TenantId
    {
        get;
    }

    public void ThrowIfInvalid()
    {
        if (TenantId == Guid.Empty)
        {
            throw new InvalidOperationException("A non-empty tenant identifier is required for State access.");
        }
    }

    public override string ToString() => TenantId.ToString("D");
}
