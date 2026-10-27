using System.Collections.Immutable;
using System.Text.Json;

namespace ASL.Turns;

/// <summary>Authored reference content, independent of a renderer or a live game.</summary>
public sealed record TurnTopic(
    string Id, string PhaseId, int GroupIndex, string Title, string Who, string Kind,
    string Prompt, ImmutableArray<string> Checks, string Effect, string Example,
    string Timing, string References, ImmutableArray<int> SourcePages,
    ImmutableArray<string> Terms, string IntegrationNote);

public sealed record TurnSource(string Edition, string Sha256, string Scope);

public sealed record TurnAsopStep(string Step, string Clauses, ImmutableArray<string> TopicIds);

public sealed record TurnPhase(
    string Id, string Name, TurnSource Source, ImmutableArray<string> Groups,
    ImmutableArray<TurnTopic> Topics, ImmutableDictionary<string, string> Glossary,
    ImmutableArray<TurnAsopStep> AsopLedger);

/// <summary>A descriptive dependency, not an instantiated or executable game obligation.</summary>
public sealed record TurnDependency(
    string Id, string Title, string Origin, string Due, ImmutableArray<string> Checks,
    string Example, string References, ImmutableArray<int> SourcePages,
    ImmutableArray<string> TopicIds, string IntegrationNote);

/// <summary>
/// Loads the reviewed reference inventory without UI services, file paths, or game-state access.
/// Integration notes remain prose; this catalog makes no availability or execution verdicts.
/// </summary>
public sealed class TurnGuideCatalog
{
    private readonly ImmutableDictionary<string, TurnTopic> topics;

    private TurnGuideCatalog(ImmutableArray<TurnPhase> phases, ImmutableArray<TurnDependency> dependencies)
    {
        Phases = phases;
        Dependencies = dependencies;
        topics = phases.SelectMany(phase => phase.Topics).ToImmutableDictionary(topic => topic.Id, StringComparer.Ordinal);
        if (dependencies.Select(item => item.Id).Distinct(StringComparer.Ordinal).Count() != dependencies.Length)
        {
            throw new InvalidDataException("Turn dependency IDs must be unique.");
        }

        foreach (var dependency in dependencies)
        {
            foreach (var id in dependency.TopicIds)
            {
                if (!topics.ContainsKey(id))
                {
                    throw new InvalidDataException($"Dependency '{dependency.Id}' references missing topic '{id}'.");
                }
            }
        }
    }

    public ImmutableArray<TurnPhase> Phases
    {
        get;
    }

    public ImmutableArray<TurnDependency> Dependencies
    {
        get;
    }

    public TurnTopic? FindTopic(string id) => topics.GetValueOrDefault(id);

    /// <summary>Returns every reference relationship for the topic, without filtering pending rules.</summary>
    public ImmutableArray<TurnDependency> DependenciesFor(string topicId) =>
        [.. Dependencies.Where(item => item.TopicIds.Contains(topicId, StringComparer.Ordinal))];

    /// <summary>Loads a fresh immutable catalog; callers may retain it for their service lifetime.</summary>
    public static TurnGuideCatalog Load() => Load(name => typeof(TurnGuideCatalog).Assembly.GetManifestResourceStream(name));

    /// <summary>Loads versioned source streams; each supplied stream is disposed after reading.</summary>
    public static TurnGuideCatalog Load(Func<string, Stream?> resourceProvider)
    {
        (string Id, string Name)[] identities =
        [
            ("rph", "Rally"), ("pfph", "Prep Fire"), ("mph", "Movement"),
            ("dfph", "Defensive Fire"), ("afph", "Advancing Fire"),
            ("rtph", "Rout"), ("aph", "Advance"), ("ccph", "Close Combat"),
        ];
        var phases = identities.Select(identity => ReadPhase(identity.Id, identity.Name, resourceProvider)).ToImmutableArray();
        using var dependencyDocument = ReadResource("ASL.Turns.Dependencies.json", resourceProvider);
        var dependencies = dependencyDocument.RootElement.GetProperty("chains").EnumerateArray().Select(item =>
            new TurnDependency(Text(item, "id"), Text(item, "title"), Text(item, "origin"), Text(item, "due"),
                Strings(item, "checks"), Text(item, "example"), Text(item, "refs"), Pages(item),
                [.. item.GetProperty("links").EnumerateArray().Select(link => Text(link, "phase") + ":" + Text(link, "topic"))],
                Text(item, "status"))).ToImmutableArray();
        return new TurnGuideCatalog(phases, dependencies);
    }

    private static TurnPhase ReadPhase(string id, string name, Func<string, Stream?> resourceProvider)
    {
        using var document = ReadResource($"ASL.Turns.ASL {name} Phase Checklist Catalog.json", resourceProvider);
        var root = document.RootElement;
        var groups = Strings(root, "groups");
        var glossary = root.GetProperty("glossary").EnumerateObject().ToImmutableDictionary(
            property => property.Name, property => property.Value.GetString() ?? throw new InvalidDataException("Missing term definition."), StringComparer.Ordinal);
        var options = root.GetProperty("options").EnumerateArray().Select(item =>
            new TurnTopic(id + ":" + Text(item, "id"), id, item.GetProperty("g").GetInt32(),
                Text(item, "title"), Text(item, "who"), Text(item, "kind"), Text(item, "prompt"),
                Strings(item, "checks"), Text(item, "effect"), Text(item, "example"), Text(item, "timing"),
                Text(item, "refs"), Pages(item), Strings(item, "terms"), Text(item, "status"))).ToImmutableArray();
        foreach (var topic in options)
        {
            if (topic.GroupIndex < 0 || topic.GroupIndex >= groups.Length || topic.Terms.Any(term => !glossary.ContainsKey(term)))
            {
                throw new InvalidDataException($"Topic '{topic.Id}' has an invalid group or undefined term.");
            }
        }

        ImmutableArray<TurnAsopStep> ledger = root.TryGetProperty("asopLedger", out var steps)
            ? [.. steps.EnumerateArray().Select(step => new TurnAsopStep(Text(step, "step"), Text(step, "clauses"),
                [.. Strings(step, "optionIds").Select(topic => id + ":" + topic)]))]
            : [];
        var known = options.Select(topic => topic.Id).ToHashSet(StringComparer.Ordinal);
        if (ledger.Any(step => step.TopicIds.Any(topic => !known.Contains(topic))))
        {
            throw new InvalidDataException($"Phase '{id}' has an invalid ASOP topic link.");
        }

        var source = root.GetProperty("source");
        return new TurnPhase(id, name, new TurnSource(Text(source, "edition"), Text(source, "sha256"), Text(source, "scope")),
            groups, options, glossary, ledger);
    }

    private static JsonDocument ReadResource(string name, Func<string, Stream?> resourceProvider)
    {
        ArgumentNullException.ThrowIfNull(resourceProvider);
        using var stream = resourceProvider(name)
            ?? throw new InvalidDataException($"Missing turn-guide resource '{name}'.");
        var document = JsonDocument.Parse(stream);
        if (!document.RootElement.TryGetProperty("schemaVersion", out var version) || (version.GetInt32() != 1 && !(version.GetInt32() == 2 && name == "ASL.Turns.ASL Rally Phase Checklist Catalog.json")))
        {
            document.Dispose();
            throw new InvalidDataException($"Unsupported turn-guide schema in '{name}'.");
        }

        return document;
    }

    private static string Text(JsonElement item, string key) =>
        item.GetProperty(key).GetString() ?? throw new InvalidDataException($"Missing '{key}' text.");

    private static ImmutableArray<string> Strings(JsonElement item, string key) =>
        [.. item.GetProperty(key).EnumerateArray().Select(value => value.GetString() ?? throw new InvalidDataException($"Missing '{key}' text."))];

    private static ImmutableArray<int> Pages(JsonElement item)
    {
        var pages = item.GetProperty("sourcePages").EnumerateArray().Select(value => value.GetInt32()).ToImmutableArray();
        if (pages.IsEmpty || pages.Any(page => page < 1 || page > 716))
        {
            throw new InvalidDataException("Rulebook page must be between 1 and 716.");
        }

        return pages;
    }
}
