using System.Text.Json;

namespace LimboDancer.Abstractions.Runtime;

public sealed class Goal
{
    public Goal(
        GoalId id,
        CorrelationId correlationId,
        Guid tenantId,
        string? sessionId,
        GoalOrigin origin,
        string intent,
        JsonElement inputs,
        DateTimeOffset createdAt)
    {
        ArgumentOutOfRangeException.ThrowIfEqual(id.Value, Guid.Empty);
        ArgumentException.ThrowIfNullOrWhiteSpace(correlationId.Value);
        ArgumentOutOfRangeException.ThrowIfEqual(tenantId, Guid.Empty);
        if (sessionId is not null)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(sessionId);
        }

        if (!Enum.IsDefined(origin))
        {
            throw new ArgumentOutOfRangeException(nameof(origin));
        }

        ArgumentException.ThrowIfNullOrWhiteSpace(intent);
        if (inputs.ValueKind != JsonValueKind.Object)
        {
            throw new ArgumentException("Goal inputs must be a JSON object.", nameof(inputs));
        }

        Id = id;
        CorrelationId = correlationId;
        TenantId = tenantId;
        SessionId = sessionId;
        Origin = origin;
        Intent = intent;
        Inputs = inputs.Clone();
        CreatedAt = createdAt;
    }

    public GoalId Id
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

    public string? SessionId
    {
        get;
    }

    public GoalOrigin Origin
    {
        get;
    }

    public string Intent
    {
        get;
    }

    public JsonElement Inputs
    {
        get;
    }

    public DateTimeOffset CreatedAt
    {
        get;
    }
}
