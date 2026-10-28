using System.Collections.Immutable;

namespace ASL.Turns;

/// <summary>Internal adapter scope. EvaluationStamp must include all facts/capability versions; never display it.</summary>
public sealed record TurnEvaluationContext(string GameId, string ViewerId, string ViewerSideId,
    string Profile, string ActualPhaseId, string WindowId, string EvaluationStamp);
public sealed record TurnGuideRequest(TurnEvaluationContext Context, string BrowsedPhaseId, bool Historical);

/// <summary>Trusted host input containing only an audience-authorized subject, explanation and opaque route.</summary>
public sealed record TurnGuideVerdict(string Id, string ViewerId, string CapabilityId, string? SubjectId,
    string WindowId, string DecisionOwnerSideId, TurnApplicability Applicability, string Reason, string? ActionRoute);
public sealed record TurnFacts(TurnEvaluationContext Context, ImmutableArray<TurnGuideVerdict> Verdicts);
public enum TurnAttentionKind
{
    EvaluationGap, RequiredDecision, OptionalChoice, Informational
}
public sealed record TurnDecision(string? Id, string? SubjectId, string? CapabilityId,
    string? DecisionOwnerSideId, TurnApplicability Applicability, string Reason, string? ActionRoute, TurnAttentionKind Attention);
public sealed record TurnOptionGuidance(TurnTopic Topic, TurnTimingBinding Timing,
    ImmutableArray<TurnCapability> Capabilities, ImmutableArray<TurnDecision> Decisions);
public sealed record TurnGuideSnapshot(string GameId, string ViewerId, string ActualPhaseId,
    string BrowsedPhaseId, string WindowId, bool Historical, ImmutableArray<TurnOptionGuidance> Options,
    ImmutableArray<TurnDependency> Dependencies, TurnSource Source, ImmutableDictionary<string, string> Glossary,
    ImmutableArray<TurnGroup> Groups, ImmutableArray<TurnWindow> Windows, string CoverageNote);

/// <summary>Pure reference and authorized-verdict composition. No game access, writes, dice or rule predicates.</summary>
public sealed class TurnGuideService
{
    public TurnCapabilityInventory Inventory
    {
        get;
    }

    private readonly TurnGuideCatalog catalog;
    private readonly TurnTimingSchema timing;
    private readonly TurnCapabilityCatalog capabilities;

    public TurnGuideService(TurnGuideCatalog catalog, IEnumerable<TurnCapability> registrations)
    {
        ArgumentNullException.ThrowIfNull(catalog);
        this.catalog = catalog;
        Inventory = TurnCapabilityInventory.Load(catalog);
        timing = TurnTimingSchema.Load(catalog);
        capabilities = TurnCapabilityCatalog.Create(catalog, registrations);
    }

    public TurnGuideSnapshot Describe(TurnGuideRequest request, TurnFacts facts)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(facts);
        if (request.Context != facts.Context)
        {
            throw new InvalidOperationException("Guidance facts do not match the requested evaluation scope.");
        }
        var context = request.Context;
        ArgumentNullException.ThrowIfNull(context);
        foreach (var value in new[] { context.GameId, context.ViewerId, context.ViewerSideId, context.Profile, context.ActualPhaseId, context.WindowId, context.EvaluationStamp })
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(value);
        }
        if (!catalog.Phases.Any(phase => phase.Id == context.ActualPhaseId))
        {
            throw new ArgumentException("Unknown actual phase.", nameof(request));
        }
        var phaseOnly = context.WindowId == context.ActualPhaseId + ":phase";
        if (!phaseOnly && !timing.Windows.Any(window => window.Id == context.WindowId && window.PhaseId == context.ActualPhaseId))
        {
            throw new ArgumentException("Unknown or cross-phase current window.", nameof(request));
        }
        var phase = catalog.Phases.SingleOrDefault(item => item.Id == request.BrowsedPhaseId)
            ?? throw new ArgumentException("Unknown browsed phase.", nameof(request));
        if (facts.Verdicts.IsDefault)
        {
            throw new ArgumentException("Verdicts must be supplied, even when empty.", nameof(facts));
        }
        // Remove other audiences before examining their contents or producing any counts/reasons.
        var visible = facts.Verdicts.Where(item => item.ViewerId == context.ViewerId).ToImmutableArray();
        if (visible.Select(item => item.Id).Distinct(StringComparer.Ordinal).Count() != visible.Length)
        {
            throw new InvalidDataException("Duplicate visible verdict identity.");
        }
        if (visible.Any(item => !Enum.IsDefined(item.Applicability)))
        {
            throw new InvalidDataException("Invalid applicability verdict.");
        }
        var options = phase.Topics.Select(topic =>
        {
            var registrations = capabilities.Topics.Single(item => item.Topic.Id == topic.Id).Registrations
                .Where(item => item.Profile == context.Profile).ToImmutableArray();
            var evaluators = registrations.Where(item => Supported(item.Evaluator)).ToDictionary(item => item.Id, StringComparer.Ordinal);
            var decisions = visible.Where(item => !phaseOnly && evaluators.ContainsKey(item.CapabilityId) && item.WindowId == context.WindowId)
                .OrderBy(item => item.Id, StringComparer.Ordinal)
                .Select(item => Compose(item, evaluators[item.CapabilityId], request)).ToImmutableArray();
            if (decisions.IsEmpty)
            {
                decisions = [new TurnDecision(null, null, null, null, TurnApplicability.NotEvaluated,
                    "No verified assessment for this topic in the requested context.", null, TurnAttentionKind.EvaluationGap)];
            }
            return new TurnOptionGuidance(topic, timing.Bindings.Single(item => item.TopicId == topic.Id), registrations, decisions);
        }).ToImmutableArray();
        var topicIds = phase.Topics.Select(topic => topic.Id).ToHashSet(StringComparer.Ordinal);
        return new TurnGuideSnapshot(context.GameId, context.ViewerId, context.ActualPhaseId, phase.Id, context.WindowId,
            request.Historical, options, [.. catalog.Dependencies.Where(item => item.TopicIds.Any(topicIds.Contains))],
            phase.Source, phase.Glossary, [.. timing.Groups.Where(item => item.PhaseId == phase.Id)],
            [.. timing.Windows.Where(item => item.PhaseId == phase.Id)],
            "Reference inventory is complete for the reviewed catalog. Supplied assessments do not establish complete ASL legality or phase closure.");
    }

    private static TurnDecision Compose(TurnGuideVerdict verdict, TurnCapability capability, TurnGuideRequest request)
    {
        if (string.IsNullOrWhiteSpace(verdict.SubjectId))
        {
            return new TurnDecision(verdict.Id, null, capability.Id, null, TurnApplicability.NotEvaluated,
                "Subject facts are missing; applicability has not been established.", null, TurnAttentionKind.EvaluationGap);
        }
        var attention = verdict.Applicability switch
        {
            TurnApplicability.NotEvaluated => TurnAttentionKind.EvaluationGap,
            TurnApplicability.Required => TurnAttentionKind.RequiredDecision,
            TurnApplicability.Available => TurnAttentionKind.OptionalChoice,
            _ => TurnAttentionKind.Informational,
        };
        var canRoute = !request.Historical
            && request.BrowsedPhaseId == request.Context.ActualPhaseId
            && verdict.DecisionOwnerSideId == request.Context.ViewerSideId
            && Supported(capability.Command)
            && verdict.Applicability is TurnApplicability.Available or TurnApplicability.Required;
        return new TurnDecision(verdict.Id, verdict.SubjectId, capability.Id, verdict.DecisionOwnerSideId, verdict.Applicability, verdict.Reason,
            canRoute ? verdict.ActionRoute : null, attention);
    }

    private static bool Supported(TurnSupport support) => support is TurnSupport.Partial or TurnSupport.IntegratedSubset;
}
