using System.Text.Json;

namespace LimboDancer.Abstractions.Domain;

public sealed class DomainQuestion
{
    public DomainQuestion(
        string questionId,
        Guid tenantId,
        DomainPackageRef package,
        SemanticIdentifier kind,
        JsonElement parameters,
        DateTimeOffset askedAt)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(questionId);
        ArgumentOutOfRangeException.ThrowIfEqual(tenantId, Guid.Empty);
        ArgumentNullException.ThrowIfNull(package);
        ArgumentException.ThrowIfNullOrWhiteSpace(kind.Value);
        if (kind.DomainId != package.DomainId)
        {
            throw new ArgumentException("Question kind and package must belong to the same domain.", nameof(kind));
        }

        if (parameters.ValueKind != JsonValueKind.Object)
        {
            throw new ArgumentException("Domain question parameters must be a JSON object.", nameof(parameters));
        }

        QuestionId = questionId;
        TenantId = tenantId;
        Package = package;
        Kind = kind;
        Parameters = parameters.Clone();
        AskedAt = askedAt;
    }

    public string QuestionId
    {
        get;
    }

    public Guid TenantId
    {
        get;
    }

    public DomainPackageRef Package
    {
        get;
    }

    public SemanticIdentifier Kind
    {
        get;
    }

    public JsonElement Parameters
    {
        get;
    }

    public DateTimeOffset AskedAt
    {
        get;
    }
}
