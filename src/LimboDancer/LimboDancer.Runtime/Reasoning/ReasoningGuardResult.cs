using System.Collections.ObjectModel;

namespace LimboDancer.Runtime.Reasoning;

public sealed class ReasoningGuardResult
{
    public ReasoningGuardResult(bool canContinue, IEnumerable<string>? reasonCodes = null)
    {
        var reasons = (reasonCodes ?? []).ToArray();
        if (reasons.Any(string.IsNullOrWhiteSpace))
        {
            throw new ArgumentException("Reason codes cannot be empty.", nameof(reasonCodes));
        }

        if (canContinue == (reasons.Length != 0))
        {
            throw new ArgumentException("Blocked Reasoning requires reasons and successful Reasoning cannot contain them.");
        }

        CanContinue = canContinue;
        ReasonCodes = new ReadOnlyCollection<string>(reasons);
    }

    public bool CanContinue
    {
        get;
    }

    public IReadOnlyList<string> ReasonCodes
    {
        get;
    }
}
