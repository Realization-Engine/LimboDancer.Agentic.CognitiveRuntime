using System.Collections.ObjectModel;

namespace LimboDancer.Abstractions.Verification;

public sealed class EffectVerificationFinding
{
    public EffectVerificationFinding(
        string effectId,
        VerificationStatus status,
        string reasonCode,
        IEnumerable<string>? evidenceRefs = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(effectId);
        if (!Enum.IsDefined(status))
        {
            throw new ArgumentOutOfRangeException(nameof(status));
        }

        ArgumentException.ThrowIfNullOrWhiteSpace(reasonCode);
        var evidence = (evidenceRefs ?? []).ToArray();
        if (evidence.Any(string.IsNullOrWhiteSpace))
        {
            throw new ArgumentException("Verification evidence references cannot be empty.", nameof(evidenceRefs));
        }

        EffectId = effectId;
        Status = status;
        ReasonCode = reasonCode;
        EvidenceRefs = new ReadOnlyCollection<string>(evidence);
    }

    public string EffectId
    {
        get;
    }

    public VerificationStatus Status
    {
        get;
    }

    public string ReasonCode
    {
        get;
    }

    public IReadOnlyList<string> EvidenceRefs
    {
        get;
    }
}
