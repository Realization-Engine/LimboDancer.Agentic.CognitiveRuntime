using System.Collections.Immutable;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ASL.Turns;

public enum TurnInventoryKind
{
    Topic, Dependency
}
public enum TurnEvidenceKind
{
    RulePredicate, GovernedSubset, ObservedHistory, AdjudicatedEffect
}
public sealed record TurnInventoryGap(string Id, string WorkItem, string Reason);
public sealed record TurnInventoryEvidence(string Id, TurnEvidenceKind Kind, string Scope,
    ImmutableArray<string> Profiles, ImmutableArray<string> References);
public sealed record TurnInventoryBinding(string Id, TurnInventoryKind Kind, TurnSourceReview SourceReview,
    ImmutableArray<string> EvidenceIds, ImmutableArray<string> GapIds)
{
    // Inventory evidence is not an executable capability registration or a scenario verdict.
    public TurnSupport UnregisteredSupport { get; } = TurnSupport.NotEvaluated;
    public TurnSupport PendingRuleSupport { get; } = TurnSupport.Pending;
    public TurnApplicability Applicability { get; } = TurnApplicability.NotEvaluated;
}

/// <summary>Public reference metadata only. No game facts, inferred capabilities or hidden-state counts.</summary>
public sealed record TurnCapabilityInventory(int SchemaVersion, ImmutableArray<TurnInventoryGap> Gaps,
    ImmutableArray<TurnInventoryEvidence> Evidence, ImmutableArray<TurnInventoryBinding> Bindings)
{
    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter(allowIntegerValues: false) },
    };

    public static TurnCapabilityInventory Load(TurnGuideCatalog catalog)
    {
        using var stream = typeof(TurnCapabilityInventory).Assembly.GetManifestResourceStream("ASL.Turns.Inventory.json")!;
        return Read(stream, catalog);
    }

    public static TurnCapabilityInventory Read(Stream stream, TurnGuideCatalog catalog)
    {
        ArgumentNullException.ThrowIfNull(catalog);
        var inventory = JsonSerializer.Deserialize<TurnCapabilityInventory>(stream, Options)
            ?? throw new InvalidDataException("Missing capability inventory.");
        if (inventory.SchemaVersion != 1 || inventory.Gaps.IsDefaultOrEmpty || inventory.Evidence.IsDefaultOrEmpty || inventory.Bindings.IsDefaultOrEmpty)
        {
            throw new InvalidDataException("Unsupported or incomplete capability inventory.");
        }
        var gaps = inventory.Gaps.ToDictionary(item => item.Id, StringComparer.Ordinal);
        var evidence = inventory.Evidence.ToDictionary(item => item.Id, StringComparer.Ordinal);
        foreach (var gap in inventory.Gaps)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(gap.Id);
            ArgumentException.ThrowIfNullOrWhiteSpace(gap.Reason);
            if (!Enumerable.Range(1, 9).Select(number => "B5-D" + number).Contains(gap.WorkItem, StringComparer.Ordinal))
            {
                throw new InvalidDataException("Unknown deferred work owner.");
            }
        }
        foreach (var item in inventory.Evidence)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(item.Id);
            ArgumentException.ThrowIfNullOrWhiteSpace(item.Scope);
            if (item.Profiles.IsDefaultOrEmpty || item.References.IsDefaultOrEmpty
                || item.References.Any(string.IsNullOrWhiteSpace))
            {
                throw new InvalidDataException("Incomplete subset evidence.");
            }
            foreach (var profile in item.Profiles)
            {
                _ = TurnGuideProfiles.Get(profile);
            }
        }
        var expected = catalog.Phases.SelectMany(phase => phase.Topics).Select(topic => (TurnInventoryKind.Topic, topic.Id))
            .Concat(catalog.Dependencies.Select(dependency => (TurnInventoryKind.Dependency, dependency.Id))).ToHashSet();
        var actual = inventory.Bindings.Select(item => (item.Kind, item.Id)).ToArray();
        if (actual.Length != expected.Count || !expected.SetEquals(actual))
        {
            throw new InvalidDataException("Exactly one inventory binding is required for every topic and dependency.");
        }
        foreach (var binding in inventory.Bindings)
        {
            if (binding.EvidenceIds.IsDefault || binding.GapIds.IsDefaultOrEmpty
                || binding.EvidenceIds.Distinct(StringComparer.Ordinal).Count() != binding.EvidenceIds.Length
                || binding.GapIds.Distinct(StringComparer.Ordinal).Count() != binding.GapIds.Length
                || binding.EvidenceIds.Any(id => !evidence.ContainsKey(id)) || binding.GapIds.Any(id => !gaps.ContainsKey(id)))
            {
                throw new InvalidDataException("Invalid source review, evidence or pending-work mapping.");
            }
        }
        if (!gaps.Keys.ToHashSet().SetEquals(inventory.Bindings.SelectMany(item => item.GapIds))
            || !evidence.Keys.ToHashSet().SetEquals(inventory.Bindings.SelectMany(item => item.EvidenceIds)))
        {
            throw new InvalidDataException("Unreferenced gap or evidence records are not allowed.");
        }
        return inventory;
    }
}
