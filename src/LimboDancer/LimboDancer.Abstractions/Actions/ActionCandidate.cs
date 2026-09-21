using System.Collections.Frozen;
using System.Collections.ObjectModel;
using System.Text.Json;

namespace LimboDancer.Abstractions.Actions;

public sealed class ActionCandidate
{
    public ActionCandidate(
        string candidateId,
        ActionDescriptor descriptor,
        JsonElement arguments,
        IEnumerable<string>? evidenceRefs = null,
        IEnumerable<KeyValuePair<string, string>>? stateVersions = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(candidateId);
        ArgumentNullException.ThrowIfNull(descriptor);

        var evidence = (evidenceRefs ?? []).ToArray();
        if (evidence.Any(string.IsNullOrWhiteSpace))
        {
            throw new ArgumentException("Evidence references cannot be empty.", nameof(evidenceRefs));
        }

        CandidateId = candidateId;
        Descriptor = descriptor;
        Arguments = arguments.Clone();
        EvidenceRefs = new ReadOnlyCollection<string>(evidence);
        StateVersions = (stateVersions ?? [])
            .ToFrozenDictionary(
                static item => item.Key,
                static item => item.Value,
                StringComparer.Ordinal);
    }

    public string CandidateId
    {
        get;
    }

    public ActionDescriptor Descriptor
    {
        get;
    }

    public JsonElement Arguments
    {
        get;
    }

    public IReadOnlyList<string> EvidenceRefs
    {
        get;
    }

    public IReadOnlyDictionary<string, string> StateVersions
    {
        get;
    }
}
