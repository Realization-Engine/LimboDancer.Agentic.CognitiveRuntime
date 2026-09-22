using System.Collections.ObjectModel;

namespace LimboDancer.Abstractions.Verification;

public sealed class EffectVerificationResult
{
    public EffectVerificationResult(
        VerificationStatus status,
        string reasonCode,
        IEnumerable<EffectVerificationFinding>? findings = null)
    {
        if (!Enum.IsDefined(status))
        {
            throw new ArgumentOutOfRangeException(nameof(status));
        }

        ArgumentException.ThrowIfNullOrWhiteSpace(reasonCode);
        var findingValues = (findings ?? []).ToArray();
        if (findingValues.Any(static finding => finding is null))
        {
            throw new ArgumentException("Verification findings cannot contain null values.", nameof(findings));
        }

        var effectIds = findingValues.Select(static finding => finding.EffectId).ToArray();
        if (effectIds.Distinct(StringComparer.Ordinal).Count() != effectIds.Length)
        {
            throw new ArgumentException("Verification findings must have unique effect identifiers.", nameof(findings));
        }

        Status = status;
        ReasonCode = reasonCode;
        Findings = new ReadOnlyCollection<EffectVerificationFinding>(findingValues);
    }

    public VerificationStatus Status
    {
        get;
    }

    public string ReasonCode
    {
        get;
    }

    public IReadOnlyList<EffectVerificationFinding> Findings
    {
        get;
    }
}
