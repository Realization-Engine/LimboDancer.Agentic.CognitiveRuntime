using System.Collections.Immutable;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ASL.Turns;

public enum TurnWindowKind
{
    PrintedAsop, SupplementalUnclassified
}
public sealed record TurnGroup(string Id, string PhaseId, string Label);
public sealed record TurnWindow(string Id, string PhaseId, TurnWindowKind Kind, string? Locator);
public sealed record TurnTimingBinding(string TopicId, string GroupId, ImmutableArray<string> WindowIds);

/// <summary>Source-reference windows, not a schedule or permission to execute an action.</summary>
public sealed record TurnTimingSchema(
    int SchemaVersion, ImmutableArray<TurnGroup> Groups,
    ImmutableArray<TurnWindow> Windows, ImmutableArray<TurnTimingBinding> Bindings)
{
    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter<TurnWindowKind>(allowIntegerValues: false) },
    };

    public static TurnTimingSchema Load(TurnGuideCatalog catalog)
    {
        using var stream = typeof(TurnTimingSchema).Assembly.GetManifestResourceStream("ASL.Turns.Timing.json")!;
        return Read(stream, catalog);
    }

    /// <summary>Validates externally authored schema without altering its input or the reference catalog.</summary>
    public static TurnTimingSchema Read(Stream stream, TurnGuideCatalog catalog)
    {
        ArgumentNullException.ThrowIfNull(catalog);
        var schema = JsonSerializer.Deserialize<TurnTimingSchema>(stream, Options)
            ?? throw new InvalidDataException("Missing timing schema.");
        if (schema.SchemaVersion != 1)
        {
            throw new InvalidDataException("Unsupported timing schema version.");
        }

        if (schema.Groups.IsDefault || schema.Windows.IsDefault || schema.Bindings.IsDefault)
        {
            throw new InvalidDataException("Timing arrays are required.");
        }

        var phases = catalog.Phases.ToDictionary(phase => phase.Id, StringComparer.Ordinal);
        var groups = schema.Groups.ToDictionary(group => group.Id, StringComparer.Ordinal);
        var windows = schema.Windows.ToDictionary(window => window.Id, StringComparer.Ordinal);
        foreach (var group in schema.Groups)
        {
            if (!phases.ContainsKey(group.PhaseId) || !group.Id.StartsWith(group.PhaseId + ":group:", StringComparison.Ordinal))
            {
                throw new InvalidDataException($"Invalid group '{group.Id}'.");
            }
        }
        foreach (var window in schema.Windows)
        {
            if (!phases.TryGetValue(window.PhaseId, out var phase))
            {
                throw new InvalidDataException($"Unknown phase for '{window.Id}'.");
            }

            if (window.Kind == TurnWindowKind.PrintedAsop)
            {
                if (!phase.AsopLedger.Any(step => step.Step == window.Locator) || window.Id != window.PhaseId + ":asop:" + window.Locator)
                {
                    throw new InvalidDataException($"Unknown printed step '{window.Id}'.");
                }
            }
            else if (window.Locator is not null || !window.Id.StartsWith(window.PhaseId + ":supplemental:", StringComparison.Ordinal))
            {
                throw new InvalidDataException($"Invalid supplemental window '{window.Id}'.");
            }
        }
        var topics = catalog.Phases.SelectMany(phase => phase.Topics).ToDictionary(topic => topic.Id, StringComparer.Ordinal);
        var bindings = schema.Bindings.ToDictionary(binding => binding.TopicId, StringComparer.Ordinal);
        if (!topics.Keys.ToHashSet(StringComparer.Ordinal).SetEquals(bindings.Keys))
        {
            throw new InvalidDataException("Every topic must have exactly one timing binding.");
        }

        foreach (var binding in schema.Bindings)
        {
            var topic = topics[binding.TopicId];
            if (!groups.TryGetValue(binding.GroupId, out var group) || group.PhaseId != topic.PhaseId)
            {
                throw new InvalidDataException($"Unknown or cross-phase group for '{topic.Id}'.");
            }

            if (binding.WindowIds.IsDefaultOrEmpty)
            {
                throw new InvalidDataException($"No reference window for '{topic.Id}'.");
            }

            foreach (var id in binding.WindowIds)
            {
                if (!windows.TryGetValue(id, out var window) || window.PhaseId != topic.PhaseId)
                {
                    throw new InvalidDataException($"Unknown or cross-phase window for '{topic.Id}'.");
                }

                if (window.Kind == TurnWindowKind.PrintedAsop && !phases[topic.PhaseId].AsopLedger.Single(step => step.Step == window.Locator).TopicIds.Contains(topic.Id))
                {
                    throw new InvalidDataException($"Printed step does not reference '{topic.Id}'.");
                }
            }
        }
        foreach (var binding in schema.Bindings)
        {
            var phase = phases[topics[binding.TopicId].PhaseId];
            var expected = phase.AsopLedger.Where(step => step.TopicIds.Contains(binding.TopicId))
                .Select(step => phase.Id + ":asop:" + step.Step).ToHashSet(StringComparer.Ordinal);
            if (expected.Count == 0)
            {
                expected.Add(phase.Id + ":supplemental:" + binding.TopicId[(phase.Id.Length + 1)..]);
            }
            if (binding.WindowIds.Length != expected.Count || !expected.SetEquals(binding.WindowIds))
            {
                throw new InvalidDataException($"Incomplete or duplicate timing references for '{binding.TopicId}'.");
            }
        }
        if (!schema.Bindings.SelectMany(binding => binding.WindowIds).ToHashSet(StringComparer.Ordinal).SetEquals(windows.Keys))
        {
            throw new InvalidDataException("Unreferenced timing windows are not allowed.");
        }
        return schema;
    }
}

/// <summary>Exact contextual term identity. No flattening of DR/dr or phase-specific definitions.</summary>
public readonly record struct TurnTermId(string PhaseId, string Term);

/// <summary>Typed due specifications for future Rules adapters. No predicates execute in this schema.</summary>
public abstract record TurnDueSpec
{
    private TurnDueSpec()
    {
    }
    public sealed record AfterEvent(string EventId) : TurnDueSpec;
    public sealed record BeforeWindowCloses(string WindowId) : TurnDueSpec;
    public sealed record AtWindow(string WindowId) : TurnDueSpec;
    public sealed record NextFriendlyPhase(string OwnerSideId, string PhaseId, string OriginPlayerTurnId) : TurnDueSpec;
    public sealed record PlayerTurnEnd(string PlayerTurnId) : TurnDueSpec;
    public sealed record CompletedGameTurns(string OwnerSideId, string OriginEventId, int Count) : TurnDueSpec;
    public sealed record SourceChanges(string SourceId, string OriginEventId) : TurnDueSpec;
}
