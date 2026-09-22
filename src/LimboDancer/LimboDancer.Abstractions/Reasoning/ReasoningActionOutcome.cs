using System.Text.Json;
using LimboDancer.Abstractions.Runtime;

namespace LimboDancer.Abstractions.Reasoning;

public sealed class ReasoningActionOutcome
{
    public ReasoningActionOutcome(
        StepId stepId,
        string semanticIntent,
        bool succeeded,
        string code,
        JsonElement? output = null)
    {
        ArgumentOutOfRangeException.ThrowIfEqual(stepId.Value, Guid.Empty);
        ArgumentException.ThrowIfNullOrWhiteSpace(semanticIntent);
        ArgumentException.ThrowIfNullOrWhiteSpace(code);
        StepId = stepId;
        SemanticIntent = semanticIntent;
        Succeeded = succeeded;
        Code = code;
        Output = output?.Clone();
    }

    public StepId StepId
    {
        get;
    }

    public string SemanticIntent
    {
        get;
    }

    public bool Succeeded
    {
        get;
    }

    public string Code
    {
        get;
    }

    public JsonElement? Output
    {
        get;
    }
}
