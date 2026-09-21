using System.Collections.ObjectModel;

namespace LimboDancer.Abstractions.Execution;

public sealed class ExecutionGateResult
{
    public ExecutionGateResult(
        ExecutionGateOutcome outcome,
        AuthorizedAction? authorizedAction,
        IEnumerable<string>? reasonCodes = null)
    {
        if ((outcome == ExecutionGateOutcome.Authorized) != (authorizedAction is not null))
        {
            throw new ArgumentException(
                "Only an authorized outcome may contain an authorized action.",
                nameof(authorizedAction));
        }

        var reasons = (reasonCodes ?? []).ToArray();
        if (reasons.Any(string.IsNullOrWhiteSpace))
        {
            throw new ArgumentException("Reason codes cannot be empty.", nameof(reasonCodes));
        }

        Outcome = outcome;
        AuthorizedAction = authorizedAction;
        ReasonCodes = new ReadOnlyCollection<string>(reasons);
    }

    public ExecutionGateOutcome Outcome
    {
        get;
    }

    public AuthorizedAction? AuthorizedAction
    {
        get;
    }

    public IReadOnlyList<string> ReasonCodes
    {
        get;
    }
}
