using System.Text.Json;
using LimboDancer.Abstractions.Runtime;
using LimboDancer.Abstractions.Verification;

namespace LimboDancer.Abstractions.Reasoning;

public sealed class ReasoningActionOutcome
{
    public ReasoningActionOutcome(
        StepId stepId,
        string semanticIntent,
        bool succeeded,
        string code,
        JsonElement? output = null,
        VerificationStatus? verificationStatus = null)
    {
        ArgumentOutOfRangeException.ThrowIfEqual(stepId.Value, Guid.Empty);
        ArgumentException.ThrowIfNullOrWhiteSpace(semanticIntent);
        ArgumentException.ThrowIfNullOrWhiteSpace(code);
        if (verificationStatus is { } status && !Enum.IsDefined(status))
        {
            throw new ArgumentOutOfRangeException(nameof(verificationStatus));
        }

        StepId = stepId;
        SemanticIntent = semanticIntent;
        Succeeded = succeeded;
        Code = code;
        Output = output?.Clone();
        VerificationStatus = verificationStatus;
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

    public VerificationStatus? VerificationStatus
    {
        get;
    }
}
