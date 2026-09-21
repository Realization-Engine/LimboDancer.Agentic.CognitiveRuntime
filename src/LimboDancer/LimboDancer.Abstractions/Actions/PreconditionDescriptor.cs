using System.Text.Json;

namespace LimboDancer.Abstractions.Actions;

public enum PreconditionKind
{
    Semantic,
    Governance,
    Operational,
}

public sealed class PreconditionDescriptor
{
    public PreconditionDescriptor(
        string id,
        PreconditionKind kind,
        string evaluatorId,
        JsonElement parameters,
        bool required)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id);
        ArgumentException.ThrowIfNullOrWhiteSpace(evaluatorId);

        Id = id;
        Kind = kind;
        EvaluatorId = evaluatorId;
        Parameters = parameters.Clone();
        Required = required;
    }

    public string Id
    {
        get;
    }

    public PreconditionKind Kind
    {
        get;
    }

    public string EvaluatorId
    {
        get;
    }

    public JsonElement Parameters
    {
        get;
    }

    public bool Required
    {
        get;
    }
}
