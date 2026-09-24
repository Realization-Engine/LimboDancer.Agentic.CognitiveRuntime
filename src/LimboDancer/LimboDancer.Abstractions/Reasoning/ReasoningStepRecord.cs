namespace LimboDancer.Abstractions.Reasoning;

public sealed class ReasoningStepRecord
{
    public ReasoningStepRecord(
        string semanticIntent,
        string proposalFingerprint,
        string stateFingerprint)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(semanticIntent);
        ArgumentException.ThrowIfNullOrWhiteSpace(proposalFingerprint);
        ArgumentException.ThrowIfNullOrWhiteSpace(stateFingerprint);
        SemanticIntent = semanticIntent;
        ProposalFingerprint = proposalFingerprint;
        StateFingerprint = stateFingerprint;
    }

    public string SemanticIntent
    {
        get;
    }

    public string ProposalFingerprint
    {
        get;
    }

    public string StateFingerprint
    {
        get;
    }
}
