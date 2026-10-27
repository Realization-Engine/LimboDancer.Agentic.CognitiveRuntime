using System.Collections.Immutable;

namespace ASL.Turns;

public enum TurnSourceReview
{
    NotReviewed, Reconciled, InterpretationOpen, SourceMissing
}
public enum TurnSupport
{
    NotEvaluated, Pending, Deferred, Partial, IntegratedSubset
}
public enum TurnApplicability
{
    NotEvaluated, Available, Required, Unavailable, NotApplicable
}

/// <summary>Evidence for one explicitly named sub-action and profile, never inferred from prose.</summary>
public sealed record TurnCapability(
    string Id, string TopicId, string Profile, TurnSourceReview SourceReview,
    TurnSupport Evaluator, TurnSupport Command, ImmutableArray<string> Evidence);

/// <summary>Unknown support never removes reference content or asserts applicability.</summary>
public sealed record TurnTopicCapabilities(TurnTopic Topic, ImmutableArray<TurnCapability> Registrations)
{
    public TurnSupport UnregisteredSupport { get; } = TurnSupport.NotEvaluated;

    public TurnApplicability Applicability { get; } = TurnApplicability.NotEvaluated;
}

public sealed class TurnCapabilityCatalog
{
    private TurnCapabilityCatalog(ImmutableArray<TurnTopicCapabilities> topics) => Topics = topics;

    public ImmutableArray<TurnTopicCapabilities> Topics
    {
        get;
    }

    /// <summary>An empty manifest is honest about unverified adapter coverage, including existing Rules features.</summary>
    public static TurnCapabilityCatalog Create(TurnGuideCatalog catalog, IEnumerable<TurnCapability> registrations)
    {
        ArgumentNullException.ThrowIfNull(catalog);
        ArgumentNullException.ThrowIfNull(registrations);
        var entries = registrations.ToImmutableArray();
        var ids = new HashSet<string>(StringComparer.Ordinal);
        foreach (var item in entries)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(item.Id);
            ArgumentException.ThrowIfNullOrWhiteSpace(item.Profile);
            if (!ids.Add(item.Id))
            {
                throw new InvalidDataException($"Duplicate capability '{item.Id}'.");
            }

            if (catalog.FindTopic(item.TopicId) is null)
            {
                throw new InvalidDataException($"Unknown capability topic '{item.TopicId}'.");
            }

            if (!Enum.IsDefined(item.SourceReview) || !Enum.IsDefined(item.Evaluator) || !Enum.IsDefined(item.Command))
            {
                throw new InvalidDataException($"Invalid capability status '{item.Id}'.");
            }

            if (item.Evidence.IsDefault || item.Evidence.Any(string.IsNullOrWhiteSpace))
            {
                throw new InvalidDataException($"Invalid evidence for '{item.Id}'.");
            }

            if ((NeedsEvidence(item.Evaluator) || NeedsEvidence(item.Command)) && item.Evidence.IsEmpty)
            {
                throw new InvalidDataException($"Supported capability '{item.Id}' requires evidence.");
            }
        }

        return new TurnCapabilityCatalog([.. catalog.Phases.SelectMany(phase => phase.Topics)
            .Select(topic => new TurnTopicCapabilities(topic, [.. entries.Where(item => item.TopicId == topic.Id)]))]);
    }

    private static bool NeedsEvidence(TurnSupport support) => support is TurnSupport.Partial or TurnSupport.IntegratedSubset;
}
