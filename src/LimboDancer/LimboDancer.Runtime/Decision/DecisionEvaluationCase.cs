using System.Collections.ObjectModel;
using LimboDancer.Abstractions.Actions;
using LimboDancer.Abstractions.Decision;
using LimboDancer.Abstractions.Evidence;

namespace LimboDancer.Runtime.Decision;

public sealed class DecisionEvaluationCase
{
    public DecisionEvaluationCase(
        string caseId,
        RuntimeStepEvidence evidence,
        DecisionOutcome expectedOutcome,
        ActionRiskProfile actionRisk,
        DecisionWrongChoiceSeverity wrongChoiceSeverity,
        bool isAmbiguous = false,
        IEnumerable<string>? acceptableCandidateIds = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(caseId);
        ArgumentNullException.ThrowIfNull(evidence);
        if (!Enum.IsDefined(expectedOutcome))
        {
            throw new ArgumentOutOfRangeException(nameof(expectedOutcome));
        }

        ArgumentNullException.ThrowIfNull(actionRisk);
        if (!Enum.IsDefined(wrongChoiceSeverity))
        {
            throw new ArgumentOutOfRangeException(nameof(wrongChoiceSeverity));
        }

        var acceptable = (acceptableCandidateIds ?? []).ToArray();
        if (acceptable.Any(string.IsNullOrWhiteSpace)
            || acceptable.Distinct(StringComparer.Ordinal).Count() != acceptable.Length)
        {
            throw new ArgumentException("Acceptable candidate identifiers must be unique and non-empty.", nameof(acceptableCandidateIds));
        }

        if ((expectedOutcome == DecisionOutcome.Selected) != (acceptable.Length != 0))
        {
            throw new ArgumentException(
                "Selected evaluation cases require acceptable candidates; other outcomes cannot have them.",
                nameof(acceptableCandidateIds));
        }

        var permitted = evidence.PermittedCandidates
            .Select(static item => item.Candidate.CandidateId)
            .ToHashSet(StringComparer.Ordinal);
        if (acceptable.Any(candidateId => !permitted.Contains(candidateId)))
        {
            throw new ArgumentException("Acceptable candidates must belong to the preserved permitted set.", nameof(acceptableCandidateIds));
        }

        CaseId = caseId;
        Evidence = evidence;
        ExpectedOutcome = expectedOutcome;
        ActionRisk = actionRisk;
        WrongChoiceSeverity = wrongChoiceSeverity;
        IsAmbiguous = isAmbiguous;
        AcceptableCandidateIds = new ReadOnlyCollection<string>(acceptable);
    }

    public string CaseId
    {
        get;
    }

    public RuntimeStepEvidence Evidence
    {
        get;
    }

    public DecisionOutcome ExpectedOutcome
    {
        get;
    }

    public ActionRiskProfile ActionRisk
    {
        get;
    }

    public DecisionWrongChoiceSeverity WrongChoiceSeverity
    {
        get;
    }

    public bool IsAmbiguous
    {
        get;
    }

    public IReadOnlyList<string> AcceptableCandidateIds
    {
        get;
    }
}
