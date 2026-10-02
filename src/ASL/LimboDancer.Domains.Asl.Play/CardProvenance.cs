using System.Text.Json;
using LimboDancer.Domains.Asl.Units.Catalog;

namespace LimboDancer.Domains.Asl.Play;

/// <summary>Whether a game's card still matches what the game started from (ruling R18.1).</summary>
public enum CardMatch
{
    Same,
    Changed,
    Gone,
}

/// <summary>What a citation rests on: a registered rulebook fragment, a project ruling, or neither.</summary>
public enum CitationKind
{
    Fragment,
    Ruling,
    Unregistered,
}

/// <summary>A card's identity: its id, whether it is built in or the user's (with its file), its SHA-256, format, and pinned catalog.</summary>
public sealed record CardIdentityFacts(string Id, bool IsUser, string? File, string Sha256, string Format, string Catalog);

/// <summary>What a game recorded at its start (ruling R18.2), and whether the card still matches it.</summary>
public sealed record GameStartFacts(string CardId, string Sha256, string Catalog, CardMatch Match);

/// <summary>One printed value of a counter and where it was read: the sheet, counter, face, and row.</summary>
public sealed record ValueProvenance(string Face, string Name, string Value, ValueSource Source);

/// <summary>
/// A counter a side fields: how many, its counter reference, whether it is manufactured, its catalog source, and its values' sources. Balance
/// counters (A26.4) are a row of their own, since they join the OB only when the side takes its Balance.
/// </summary>
public sealed record CounterProvenance(string Definition, string Label, int Count, CounterReference Counter, bool Manufactured, CatalogSource? Source, IReadOnlyList<ValueProvenance> Values,
    string Side = "", bool Balance = false);

/// <summary>A rule a card cites, and what it rests on: a fragment's physical PDF pages and comparison, or a ruling's link.</summary>
public sealed record CitationProvenance(string Rule, CitationKind Kind, IReadOnlyList<int> Pages, string? FragmentId, string? Comparison, string? Link);

/// <summary>
/// A scenario card's full provenance (plan task 22b.5, section 13.6): its identity, what a game started from, its source, its counters'
/// sources, and its citations. Built here from the card library, the catalog, and the pass 17 comparison registry, and passed to the Studio's
/// panel as a prepared model.
/// </summary>
public sealed record CardProvenance(CardIdentityFacts Identity, GameStartFacts? Game, ScenarioCardSource Source, IReadOnlyList<CounterProvenance> Counters, IReadOnlyList<CitationProvenance> Citations)
{
    /// <summary>Where the rulings are kept: section 5 of the Backlog Passes Plan. Rulings are shown by id with this link, not parsed.</summary>
    public const string RulingsLink = "https://github.com/Realization-Engine/LimboDancer.Agentic.CognitiveRuntime/blob/main/src/ASL/docs/ASL%20Unit%20Backlog%20Passes%20Plan.md#5-rulings";

    private sealed record Subject(string RuleId, string FragmentId, int PhysicalPdfPage, string Comparison);

    private static readonly Lazy<Dictionary<string, IReadOnlyList<Subject>>> Registry = new(ReadRegistry);

    /// <summary>The rule ids the embedded pass 17 comparison registers.</summary>
    public static IReadOnlyCollection<string> RegisteredRules => [.. Registry.Value.Keys];

    /// <summary>The provenance of a card, and of the game started from it when there is one.</summary>
    public static CardProvenance Of(ScenarioCard card, bool isUser, string? file, string sha256, UnitCatalog catalog, ScenarioCardReferenceFacts? game = null)
    {
        ArgumentNullException.ThrowIfNull(card);
        ArgumentNullException.ThrowIfNull(catalog);
        var identity = new CardIdentityFacts(card.Id, isUser, file, sha256, card.Format, card.Catalog);
        var started = game is null ? null : new GameStartFacts(game.CardId, game.Sha256, game.Catalog,
            game.CurrentSha256 is null ? CardMatch.Gone : ScenarioCards.SameCard(game.CardId, game.Sha256, game.CurrentSha256) ? CardMatch.Same : CardMatch.Changed);

        // Table player, pass 22b: each side's counters apart, and its Balance counters apart from its OB.
        var counts = card.Sides.SelectMany(side => side.Groups.SelectMany(group => group.Units).Select(unit => (side.Side, Balance: false, unit))
                .Concat((side.BalanceUnits ?? []).Select(unit => (side.Side, Balance: true, unit))))
            .GroupBy(line => (line.Side, line.Balance, line.unit.Definition))
            .Select(line => (line.Key.Side, line.Key.Balance, line.Key.Definition, Count: line.Sum(item => item.unit.Count)));
        var counters = counts.Select(line => catalog.Definition(line.Definition) is { } definition
                ? new CounterProvenance(definition.Id, ScenarioCards.Describe(definition), line.Count, definition.Counter, ScenarioCards.Manufactured(definition),
                    catalog.Source(definition.Counter.Source),
                    [.. definition.Values.Where(value => !value.NotInSource).Select(value => new ValueProvenance(value.Face, value.Name, Shown(value), value.Source))],
                    line.Side, line.Balance)
                : new CounterProvenance(line.Definition, line.Definition, line.Count, new CounterReference("unknown", "unknown", line.Definition), false, null, [],
                    line.Side, line.Balance))
            .OrderBy(counter => counter.Side, StringComparer.Ordinal)
            .ThenBy(counter => counter.Balance)
            .ThenBy(counter => counter.Label, StringComparer.Ordinal)
            .ToList();

        var cited = card.SpecialRules.SelectMany(rule => rule.Rules).Concat(card.VictoryConditions.Rules).Distinct(StringComparer.Ordinal);
        var citations = cited.Select(Citation).ToList();
        return new CardProvenance(identity, started, card.Source, counters, citations);
    }

    /// <summary>What a rule id rests on: the registry's fragment (its pages and comparison), a ruling, or nothing registered.</summary>
    public static CitationProvenance Citation(string rule)
    {
        ArgumentNullException.ThrowIfNull(rule);
        if (Registry.Value.TryGetValue(rule, out var subjects))
        {
            return new CitationProvenance(rule, CitationKind.Fragment, [.. subjects.Select(subject => subject.PhysicalPdfPage).Distinct().Order()],
                subjects[0].FragmentId, string.Join("; ", subjects.Select(subject => subject.Comparison).Distinct(StringComparer.Ordinal)), null);
        }

        return rule.Length > 1 && rule[0] == 'R' && char.IsDigit(rule[1])
            ? new CitationProvenance(rule, CitationKind.Ruling, [], null, null, RulingsLink)
            : new CitationProvenance(rule, CitationKind.Unregistered, [], null, null, null);
    }

    private static string Shown(PrintedValue value) => value.State switch
    {
        PrintedState.NotPrinted => "not printed",
        _ when value.IsTrait => value.TraitPresent == true ? "present" : "absent",
        _ => value.Value?.Display ?? string.Empty,
    };

    private static Dictionary<string, IReadOnlyList<Subject>> ReadRegistry()
    {
        using var stream = typeof(CardProvenance).Assembly.GetManifestResourceStream("SourceRegistry.asl-scenario-a1.pass17-pdf-comparison.json")
            ?? throw new InvalidOperationException("The pass 17 comparison registry is not embedded in the Play library.");
        using var document = JsonDocument.Parse(stream);
        return document.RootElement.GetProperty("subjects").EnumerateArray()
            .Select(subject => new Subject(
                subject.GetProperty("ruleId").GetString()!,
                subject.GetProperty("fragmentId").GetString()!,
                subject.GetProperty("physicalPdfPage").GetInt32(),
                subject.GetProperty("comparison").GetString()!))
            .GroupBy(subject => subject.RuleId, StringComparer.Ordinal)
            .ToDictionary(group => group.Key, group => (IReadOnlyList<Subject>)[.. group], StringComparer.Ordinal);
    }
}

/// <summary>What a game recorded about its card (the id, SHA-256, and catalog) and the card's current SHA-256, if it still exists.</summary>
public sealed record ScenarioCardReferenceFacts(string CardId, string Sha256, string Catalog, string? CurrentSha256);
