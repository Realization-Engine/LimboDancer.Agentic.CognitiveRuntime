using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using LimboDancer.Domains.Asl.Maps.Coordinates;
using LimboDancer.Domains.Asl.Rules;
using LimboDancer.Domains.Asl.Units.Catalog;
using LimboDancer.Domains.Asl.Units.State;

namespace LimboDancer.Domains.Asl.Play;

/// <summary>Where a card came from, and how it was brought to the registered rulebook (ruling R17.2).</summary>
public sealed record ScenarioCardSource(string Basis, string Legacy, IReadOnlyList<string> Adaptation);

/// <summary>The card's date; the month and year feed Extreme Winter (E3.741) and the H1.28 ELR Chart.</summary>
public sealed record ScenarioCardDate(int Day, int Month, int Year);

/// <summary>A board in the card's configuration (A2.1; ruling R17.3): its slot, and whether it is turned 180 degrees.</summary>
public sealed record ScenarioCardBoard(string Board, int Column, int Row, bool Reversed);

/// <summary>A playable area's hexrows (ruling R20.6): a board's lettered hexrows from and to, both included.</summary>
public sealed record ScenarioCardHexrows(string From, string To, string? Board = null);

/// <summary>
/// A playable area the card names (rulings R17.3, R20.6): its text, and, when it is enforced, its hexrows; setup, movement, entry, rout, advance, and
/// withdrawal then refuse a Location outside it.
/// </summary>
public sealed record ScenarioCardArea(string Text, bool Enforced, ScenarioCardHexrows? Hexrows = null);

/// <summary>
/// The Turn Record Chart (A3.9; ruling R17.4). The side that moves first may be left to a die roll, with a note saying so (The Tractor Works;
/// ruling R17.12).
/// </summary>
public sealed record ScenarioCardTurns(int Count, bool HalfTurn, string SetsUpFirst, string? MovesFirst, string? MovesFirstNote = null);

/// <summary>A side's Friendly Board Edge and what it rests on (A20.53; ruling R17.7): an SSR, entry, setup, or R0.3.</summary>
public sealed record ScenarioCardEdge(string Edge, string Basis, string? Note);

/// <summary>
/// Where an OB group sets up or enters (ruling R17.8): named <c>building</c> hexes, the <c>hex-numbers</c> from and to of a
/// board, or an <c>entry</c> on a Game Turn along an edge. An SSR may fix how many counters set up in an area, how many of them are
/// MMC at least, and whether they may set up under "?" (pass 19, ruling R19.4).
/// </summary>
public sealed record ScenarioCardSetup(string Id, string Kind, IReadOnlyList<string>? Hexes, string? Board, int? From, int? To, int? Turn, string? Edge, string? Limit,
    int? Counters = null, int? MinMmc = null, bool? Concealed = null);

/// <summary>A counter line of an OB group: a catalog definition, a count, and the setup area it belongs to.</summary>
public sealed record ScenarioCardUnit(string Definition, int Count, string? Area);

/// <summary>
/// An OB group with its ELR (A19.1; ruling R17.5), its place in a sequential setup (1 sets up first), and the "?" it may set up with
/// (A12.11, A12.12; ruling R17.12).
/// </summary>
public sealed record ScenarioCardGroup(string Name, int Elr, IReadOnlyList<ScenarioCardSetup> Areas, IReadOnlyList<ScenarioCardUnit> Units,
    int? SetupOrder = null, int? Dummies = null);

/// <summary>
/// A side: its SAN (A14.1), Battlefield Integrity total (A16.1), Friendly Board Edge, Balance (A26.4), and OB groups; the counters its Balance adds
/// to its OB, set up with any of its groups (ruling R20.4). An Axis Minor side names its nation (A25.8; ruling R27.1).
/// </summary>
public sealed record ScenarioCardSide(string Side, int San, int? IntegrityBpv, ScenarioCardEdge FriendlyEdge, string Balance, IReadOnlyList<ScenarioCardGroup> Groups,
    IReadOnlyList<ScenarioCardUnit>? BalanceUnits = null, int? Elr = null, string? Nation = null);

/// <summary>
/// An SSR (ruling R17.10): its text; <c>token</c> when the game reads it from its tokens, <c>game-default</c> when the game
/// already plays it with no token, or <c>not-enforced</c> when it is shown only (with a note saying why).
/// </summary>
public sealed record ScenarioCardRule(int Number, string Text, string Status, IReadOnlyList<string> Tokens, IReadOnlyList<string> Rules, string? Note);

/// <summary>
/// A Victory Condition the game evaluates (A26; ruling R21.3). <c>control-margin</c>: the side Controls <c>Margin</c> more of <c>Buildings</c> than the other
/// side Controls of <c>Versus</c>. <c>control-count</c>: the side Controls at least <c>AtLeast</c> hexes of <c>Building</c>, a hex in Melee Controlled by neither
/// when <c>MeleeUncontrolled</c>. <c>squad-ratio</c>: the side has at least <c>Ratio</c> times the other side's unbroken squad-equivalents. <c>sole-unbroken</c>:
/// the side alone has an unbroken unit in <c>Building</c>. <c>exit-vp</c>: the side has exited at least <c>AtLeast</c> Exit VP off <c>Edge</c> from a hex on
/// or adjacent to one of <c>Near</c>. <c>cvp</c>: the side has at least <c>AtLeast</c> CVP. Buildings are named by the id of a building setup area of the card.
/// </summary>
public sealed record ScenarioCardCondition(string Type, string Side, IReadOnlyList<string>? Buildings = null, IReadOnlyList<string>? Versus = null, int? Margin = null,
    string? Building = null, int? AtLeast = null, bool? MeleeUncontrolled = null, double? Ratio = null, string? Edge = null, IReadOnlyList<string>? Near = null);

/// <summary>
/// An outcome of the Victory Conditions (ruling R21.3): its winner (a side, or <c>draw</c>) when any of its conditions holds, checked at once after every
/// action when <c>Immediate</c>, and always at game end, in the card's order.
/// </summary>
public sealed record ScenarioCardOutcome(string Winner, bool Immediate, IReadOnlyList<ScenarioCardCondition> Any);

/// <summary>
/// The Victory Conditions (A26; rulings R17.11, R21.3): their text in A26 terms, their kind, and their structured form: the outcomes in order, and the
/// result when none holds at game end (Avoidance, A26.3, or a draw).
/// </summary>
public sealed record ScenarioCardVictory(string Kind, string Text, IReadOnlyList<string> Rules, IReadOnlyList<ScenarioCardOutcome>? Outcomes = null,
    string? Otherwise = null);

/// <summary>
/// A scenario card (backlog pass 17, rulings R17.1 to R17.11): read, validated, and shown; since pass 18 a game starts from one (rulings
/// R18.1 to R18.3). Placing the OB in its setup areas, reinforcements entering, and evaluating the Victory Conditions are passes 19 to 21.
/// </summary>
public sealed record ScenarioCard(
    string Format,
    string Id,
    string Title,
    string Catalog,
    ScenarioCardSource Source,
    string Place,
    ScenarioCardDate Date,
    string Introduction,
    IReadOnlyList<ScenarioCardBoard> Boards,
    string North,
    ScenarioCardArea? PlayableArea,
    ScenarioCardTurns Turns,
    string? ScenarioDefender,
    IReadOnlyList<ScenarioCardSide> Sides,
    IReadOnlyList<ScenarioCardRule> SpecialRules,
    ScenarioCardVictory VictoryConditions,
    string? Aftermath)
{
    /// <summary>The SSR tokens the game reads (rulings R16.1, R16.9).</summary>
    public IReadOnlyList<string> Tokens => [.. SpecialRules.SelectMany(rule => rule.Tokens)];

    /// <summary>
    /// Whether the card is a minimal card (ruling R22.3): boards and two sides with no OB, for a game with no scenario. Its sides may carry an ELR, and its
    /// date and edges may be left unrecorded; units set up by hand, and the players judge the result.
    /// </summary>
    [System.Text.Json.Serialization.JsonIgnore]
    public bool Minimal => Rules.ScenarioA1ResultTables.CardIsMinimal(Sides.Count, Sides.Select(side => side.Groups.Count));
}

/// <summary>A card and why it is refused, if it is: each diagnostic is a code and a message.</summary>
public sealed record ScenarioCardRead(string Name, ScenarioCard? Card, IReadOnlyList<string> Diagnostics)
{
    public bool IsValid => Card is not null && Diagnostics.Count == 0;
}

/// <summary>
/// The scenario cards embedded from <c>src/ASL/units/scenarios</c> (backlog pass 17), their reader, and their validation
/// against a catalog (rulings R17.2 to R17.11).
/// </summary>
public static class ScenarioCards
{
    public const string Format = "asl-scenario-card/1";

    private const string Prefix = "Scenarios.";
    private const string Suffix = ".scenario-card.json";

    /// <summary>The rules a card may cite: the registered fragments of the pass 17 PDF comparison, and the rulings (R0.3, R16.9, ...).</summary>
    public static IReadOnlyList<string> CitableRules
    {
        get;
    } =
        ["A2.1", "A3.9", "A7.7", "A14.1", "A16.1", "A19.1", "A20.53", "A24.1", "A26.1", "A26.11", "A26.14", "A26.211", "A26.23", "A26.3", "A26.4", "B8.4", "C8.2",
        "A25.22", "A25.44", "B8.1", "B25.5", "B25.63", "A10.8", "A12.11", "A26.13", "B23.74"];

    /// <summary>The kinds of setup area (ruling R17.8).</summary>
    public static IReadOnlyList<string> AreaKinds { get; } = ["building", "hex-numbers", "entry"];

    /// <summary>The bases of a Friendly Board Edge (A20.53; ruling R17.7); <c>none</c> leaves a minimal card's edge unnamed (ruling R22.3).</summary>
    public static IReadOnlyList<string> EdgeBases { get; } = ["ssr", "entry", "setup", "manufactured", "none"];

    /// <summary>The SSR statuses (ruling R17.10).</summary>
    public static IReadOnlyList<string> RuleStatuses { get; } = ["token", "game-default", "not-enforced"];

    /// <summary>The kinds of Victory Conditions (A26; ruling R17.11).</summary>
    public static IReadOnlyList<string> VictoryKinds { get; } = ["control", "exit", "casualty", "other"];

    /// <summary>The Victory Condition types the game evaluates (ruling R21.3).</summary>
    public static IReadOnlyList<string> ConditionTypes { get; } = ["control-margin", "control-count", "squad-ratio", "sole-unbroken", "exit-vp", "cvp"];

    /// <summary>
    /// The SHA-256 of a built-in card's earlier texts that differ only in a note or status from a later text, each paired with that later text's SHA-256
    /// (ruling R28.4; referee, pass 28): a game started from the earlier text plays on from the later one, and from no other. The Guards Counterattack's
    /// SSR 3 was revised in pass 28.
    /// </summary>
    public static IReadOnlyDictionary<string, IReadOnlyList<(string Earlier, string Current)>> EarlierRevisions
    {
        get;
    } = new Dictionary<string, IReadOnlyList<(string Earlier, string Current)>>(StringComparer.Ordinal)
    {
        ["guards-counterattack"] = [("132c2241db7d192460b1c7146f58b9b63027c8d5af17e698111772f871dddcab", "ad43d29ddd9a989bd30953abbf620a93e866ec9bbc566906e696b115fd348dae")],
    };

    /// <summary>Whether a game that recorded <paramref name="recorded"/> still plays the card whose text now hashes to <paramref name="current"/> (rulings R22.2, R28.4).</summary>
    public static bool SameCard(string id, string recorded, string? current) =>
        current is not null && (current == recorded || (EarlierRevisions.TryGetValue(id, out var earlier) && earlier.Contains((recorded, current))));

    /// <summary>A building setup area of the card by its id (ruling R21.3): its hexes, on the card's board or its own; null when none has that id.</summary>
    public static IReadOnlyList<BoardLocation>? BuildingHexes(ScenarioCard card, string id)
    {
        ArgumentNullException.ThrowIfNull(card);
        var area = card.Sides.SelectMany(side => side.Groups).SelectMany(group => group.Areas).FirstOrDefault(item => item.Kind == "building" && item.Id == id);
        var board = area?.Board ?? (card.Boards.Count == 1 ? card.Boards[0].Board : null);
        return area?.Hexes is { } hexes && board is not null ? [.. hexes.Select(hex => BoardLocation.Parse($"{board}:{hex}:0"))] : null;
    }

    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow,
    };

    /// <summary>The embedded card names, in ordinal order.</summary>
    public static IReadOnlyList<string> Names => [.. typeof(ScenarioCards).Assembly.GetManifestResourceNames()
        .Where(name => name.StartsWith(Prefix, StringComparison.Ordinal) && name.EndsWith(Suffix, StringComparison.Ordinal))
        .Select(name => name[Prefix.Length..^Suffix.Length])
        .Order(StringComparer.Ordinal)];

    private static readonly System.Collections.Concurrent.ConcurrentDictionary<string, string?> EmbeddedSha256 = new(StringComparer.Ordinal);

    /// <summary>The SHA-256 of an embedded card's text, with line endings as LF (ruling R18.2); null when none has that name. Read once (referee, pass 22).</summary>
    public static string? Sha256(string name) => EmbeddedSha256.GetOrAdd(name, key =>
        EmbeddedText(key) is { } text ? Convert.ToHexStringLower(System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(text))) : null);

    /// <summary>An embedded card's text with LF line endings; null when none has that name.</summary>
    public static string? EmbeddedText(string name)
    {
        ArgumentNullException.ThrowIfNull(name);
        using var stream = typeof(ScenarioCards).Assembly.GetManifestResourceStream(Prefix + name + Suffix);
        if (stream is null)
        {
            return null;
        }

        using var reader = new StreamReader(stream);
        return reader.ReadToEnd().ReplaceLineEndings("\n");
    }

    /// <summary>The id a game gives an OB group of a side (ruling R18.3): the side and the group's place on the card, from 1.</summary>
    public static string GroupId(string side, int index) => $"{side}-{index + 1}";

    /// <summary>
    /// The <c>start</c> of <c>asl.game.setup</c> a card gives (backlog pass 18, rulings R18.1 to R18.3): its boards, sides with their SAN,
    /// Friendly Board Edges, and OB groups with their ELR, the side's ELR when all its groups share one, month and year, Scenario Defender,
    /// the SSR tokens, and the card's id and SHA-256. The label is the card's title unless one is given; the side that moves first is the
    /// card's, or the one given when the card leaves it to a die roll.
    /// </summary>
    public static JsonObject Start(ScenarioCard card, string sha256, string catalog, string? label, string? firstSide)
    {
        ArgumentNullException.ThrowIfNull(card);
        var boards = new JsonArray();
        foreach (var board in card.Boards)
        {
            boards.Add(card.Boards.Count == 1 && !board.Reversed
                ? JsonValue.Create(board.Board)
                : new JsonObject { ["board"] = board.Board, ["column"] = board.Column, ["row"] = board.Row, ["reversed"] = board.Reversed });
        }

        var sides = new JsonArray();
        foreach (var side in card.Sides)
        {
            var node = new JsonObject { ["id"] = side.Side, ["nationality"] = side.Side };

            // Ruling R22.3: a minimal card names only what it records: a SAN above 0, an edge, the side's own ELR.
            if (side.San > 0 || !card.Minimal)
            {
                node["san"] = side.San;
            }

            if (side.FriendlyEdge.Edge.Length > 0)
            {
                node["friendlyEdge"] = side.FriendlyEdge.Edge;
            }

            if (side.Nation is { } nation)
            {
                node["nation"] = nation;
            }

            var elrs = side.Groups.Select(group => group.Elr).Distinct().ToArray();
            if (elrs.Length == 1)
            {
                node["elr"] = elrs[0];
            }
            else if (side.Groups.Count == 0 && side.Elr is { } own)
            {
                node["elr"] = own;
            }

            node["groups"] = new JsonArray([.. side.Groups.Select((group, index) =>
                (JsonNode)new JsonObject { ["id"] = GroupId(side.Side, index), ["name"] = group.Name, ["elr"] = group.Elr })]);
            sides.Add(node);
        }

        var start = new JsonObject
        {
            ["label"] = string.IsNullOrWhiteSpace(label) ? card.Title : label,
            ["catalog"] = catalog,
            ["boards"] = boards,
            ["firstSide"] = card.Turns.MovesFirst ?? firstSide,
            ["sides"] = sides,
            ["specialRules"] = new JsonArray([.. card.Tokens.Select(token => (JsonNode)JsonValue.Create(token))]),
            ["scenario"] = new JsonObject { ["id"] = card.Id, ["sha256"] = sha256, ["title"] = card.Title },
        };

        // Ruling R22.3: a minimal card may leave its month and year unrecorded (0).
        if (card.Date.Month > 0)
        {
            start["scenarioMonth"] = card.Date.Month;
        }

        if (card.Date.Year > 0)
        {
            start["scenarioYear"] = card.Date.Year;
        }
        if (card.ScenarioDefender is { } defender)
        {
            start["scenarioDefender"] = defender;
        }

        return start;
    }

    /// <summary>Reads and validates an embedded card; null when none has that name.</summary>
    public static ScenarioCardRead? Read(string name, UnitCatalog catalog)
    {
        ArgumentNullException.ThrowIfNull(name);
        using var stream = typeof(ScenarioCards).Assembly.GetManifestResourceStream(Prefix + name + Suffix);
        if (stream is null)
        {
            return null;
        }

        using var reader = new StreamReader(stream);
        return Parse(name, reader.ReadToEnd(), catalog);
    }

    /// <summary>Reads and validates a card's JSON.</summary>
    public static ScenarioCardRead Parse(string name, string json, UnitCatalog catalog)
    {
        ArgumentNullException.ThrowIfNull(json);
        ArgumentNullException.ThrowIfNull(catalog);
        ScenarioCard? card;
        try
        {
            card = JsonSerializer.Deserialize<ScenarioCard>(json, Options);
        }
        catch (JsonException error)
        {
            return new ScenarioCardRead(name, null, [$"card.json: {error.Message}"]);
        }

        return card is null ? new ScenarioCardRead(name, null, ["card.json: the card is empty"]) : new ScenarioCardRead(name, card, Validate(card, catalog));
    }

    /// <summary>Why a card is refused (rulings R17.2 to R17.11); empty when it is valid.</summary>
    public static IReadOnlyList<string> Validate(ScenarioCard card, UnitCatalog catalog)
    {
        ArgumentNullException.ThrowIfNull(card);
        ArgumentNullException.ThrowIfNull(catalog);
        var found = new List<string>();
        void Check(bool valid, string diagnostic)
        {
            if (!valid)
            {
                found.Add(diagnostic);
            }
        }

        Check(card.Format == Format, $"card.format: a card is '{Format}', not '{card.Format}'");
        Check(!string.IsNullOrWhiteSpace(card.Id) && !string.IsNullOrWhiteSpace(card.Title), "card.id: a card has an id and a title");
        // A card names its catalog; the version it names is where it was written, not a lock (UnitCatalogs.For).
        Check(card.Catalog is { } cardCatalog && (cardCatalog == catalog.Identity.Catalog || cardCatalog.StartsWith(catalog.Identity.Catalog + "@", StringComparison.Ordinal)),
            $"card.catalog: the card names '{card.Catalog}', and the catalog is {catalog.Identity.Catalog}@{catalog.Identity.Version}");
        Check(card.Source is { Basis.Length: > 0, Legacy.Length: > 0 }, "card.source: a card says what it rests on and which legacy card it adapts (ruling R17.2)");
        Check((card.Minimal && card.Date.Day == 0 && card.Date.Month is >= 0 and <= 12 && card.Date.Year is 0 or (>= 1936 and <= 1945))
            || (card.Date.Year is >= 1936 and <= 1945 && card.Date.Month is >= 1 and <= 12 && card.Date.Day >= 1 && card.Date.Day <= DateTime.DaysInMonth(card.Date.Year, card.Date.Month)),
            "card.date: the date is a real day of 1936 to 1945; a minimal card may give only a month and a year, or neither, as 0 (ruling R22.3)");

        // A2.1 (ruling R17.3): the boards, each in its own slot.
        found.AddRange(ScenarioA1SetupCalculator.BoardDiagnostics([.. card.Boards.Select(board => new CardBoardFacts(board.Board, BoardRef.TryParse(board.Board, out _), board.Column, board.Row))],
            card.North, SideState.Edges));

        // A2.1 (ruling R20.6): an enforced playable area names its hexrows on a board of the card.
        if (card.PlayableArea is { } playable && ScenarioA1SetupCalculator.PlayableAreaDiagnostic(playable.Enforced, playable.Hexrows is not null, playable.Hexrows?.From, playable.Hexrows?.To,
            playable.Hexrows?.Board is not null, card.Boards.Count, card.Boards.Any(board => board.Board == playable.Hexrows?.Board)) is { } playableRefused)
        {
            found.Add(playableRefused);
        }

        // A3.9 (ruling R17.4): the Turn Record Chart; ruling R17.12: the sequential setup's order.
        var sides = card.Sides.Select(side => side.Side).ToArray();
        found.AddRange(ScenarioA1SetupCalculator.TurnDiagnostics(sides, card.Turns.Count, card.Turns.SetsUpFirst, card.Turns.MovesFirst, card.Turns.MovesFirstNote,
            [.. card.Sides.SelectMany(side => side.Groups.Select(group => (side.Side, group.SetupOrder, group.Dummies)))]));

        foreach (var side in card.Sides)
        {
            found.AddRange(ScenarioA1SetupCalculator.SideDiagnostics(SideFacts(card, side, catalog), card.Minimal, SideState.Edges, EdgeBases, card.Boards.Count, card.Turns.Count));
        }

        // A16 and A16.1 (ruling R17.4): the bracketed Battlefield Integrity totals.
        var equivalents = card.Sides.Select(side => SquadEquivalents(card, side, catalog)).ToArray();
        foreach (var side in card.Sides.Where(side => side.IntegrityBpv is not null))
        {
            found.AddRange(ScenarioA1SetupCalculator.IntegrityDiagnostics(side.Side, side.IntegrityBpv!.Value,
                Starting(side, catalog).Where(item => ScenarioA1SetupCalculator.Mmc(item.Definition.Kind)).All(item => item.Definition.Printed("broken", "asl:bpv")?.Value?.Number is not null),
                equivalents, IntegrityBpv(card, side, catalog)));
        }

        // Index, Scenario Attacker/Defender (ruling R17.6): a Defender faces a side that enters wholly from offboard.
        if (card.ScenarioDefender is { } defender)
        {
            var attacker = card.Sides.FirstOrDefault(side => side.Side != defender);
            found.AddRange(ScenarioA1SetupCalculator.DefenderDiagnostics(defender, sides, card.Minimal,
                card.Sides.FirstOrDefault(side => side.Side == defender) is { } own && own.Groups.SelectMany(group => group.Areas).Any(area => area.Kind != "entry"),
                attacker is not null && attacker.Groups.SelectMany(group => group.Areas).All(area => area.Kind == "entry")));
        }

        // Ruling R17.10: the SSRs, numbered in order, and their tokens checked as a new game checks them.
        found.AddRange(ScenarioA1SetupCalculator.RuleDiagnostics([.. card.SpecialRules.Select(rule => new CardRuleFacts(rule.Number, rule.Status, rule.Tokens.Count, rule.Note, rule.Text, rule.Rules,
            [.. rule.Tokens.Where(token => token.StartsWith("hip:", StringComparison.Ordinal))
                .Select(token => (token, token.Split(':') is [_, var named, _] && card.Sides.Any(side => side.Side == named) ? named : null))]))], RuleStatuses, CitableRules));

        var start = new JsonObject
        {
            ["specialRules"] = new JsonArray([.. card.Tokens.Select(token => (JsonNode)JsonValue.Create(token))]),
        };
        if (card.Date.Month > 0)
        {
            start["scenarioMonth"] = card.Date.Month;
        }

        if (card.Date.Year > 0)
        {
            start["scenarioYear"] = card.Date.Year;
        }

        using (var document = JsonDocument.Parse(start.ToJsonString()))
        {
            var refused = GamePlanner.NightAndWeatherRulesBar(document.RootElement);
            Check(refused is null, $"card.ssr: {refused}");
        }

        // A26 (ruling R17.11): the Victory Conditions as text, with their kind and the rules they rest on; ruling R21.3: the structured ones name the card's sides and buildings.
        found.AddRange(ScenarioA1SetupCalculator.VictoryDiagnostics(card.VictoryConditions.Kind, card.VictoryConditions.Text, card.VictoryConditions.Rules, card.Minimal, VictoryKinds, CitableRules,
            card.VictoryConditions.Otherwise,
            card.VictoryConditions.Outcomes is { } outcomes
                ? [.. outcomes.Select(outcome => (outcome.Winner, (IReadOnlyList<CardConditionFacts>?)(outcome.Any is null ? null : [.. outcome.Any.Select(ConditionFacts)])))]
                : null,
            sides, SideState.Edges, id => BuildingHexes(card, id)?.Count,
            hex => BoardLocation.TryParse(hex + ":0", out var at) && card.Boards.Any(item => item.Board == at.Board.Value)));

        return found;
    }

    // The card's records as the facts Rules asks for (the design's D11).
    private static CardSideFacts SideFacts(ScenarioCard card, ScenarioCardSide side, UnitCatalog catalog) => new(side.Side, side.San, side.Nation, side.FriendlyEdge.Edge, side.FriendlyEdge.Basis,
        side.Groups.SelectMany(group => group.Areas).Any(area => area.Kind == "entry" && area.Edge == side.FriendlyEdge.Edge), side.Elr, side.Balance,
        [.. (side.BalanceUnits ?? []).Select(unit => LineFacts(side, unit, null, catalog))],
        [.. side.Groups.Select(group => new CardGroupFacts(group.Name, group.Elr, [.. group.Areas.Select(area => AreaFacts(card, area))], [.. group.Units.Select(unit => LineFacts(side, unit, group, catalog))]))]);

    private static CardLineFacts LineFacts(ScenarioCardSide side, ScenarioCardUnit unit, ScenarioCardGroup? group, UnitCatalog catalog)
    {
        var definition = catalog.Definition(unit.Definition);
        return new CardLineFacts(unit.Definition, definition is not null, definition?.Nationality == side.Side, unit.Count, unit.Area,
            unit.Area is null || group?.Areas.Any(area => area.Id == unit.Area) == true);
    }

    private static CardAreaFacts AreaFacts(ScenarioCard card, ScenarioCardSetup area) => new(area.Id, area.Kind, area.Hexes, area.Board is not null,
        card.Boards.Any(board => board.Board == area.Board), area.From, area.To, area.Turn, area.Edge, area.Counters, area.MinMmc);

    private static CardConditionFacts ConditionFacts(ScenarioCardCondition condition) =>
        new(condition.Type, condition.Side, condition.Buildings, condition.Versus, condition.Margin, condition.Building, condition.AtLeast, condition.Ratio, condition.Edge, condition.Near);

    /// <summary>The units a side starts with: everything but what enters after Turn 1 (A16.1).</summary>
    private static IEnumerable<(UnitDefinition Definition, int Count)> Starting(ScenarioCardSide side, UnitCatalog catalog) =>
        side.Groups.SelectMany(group => group.Units
            .Where(unit => group.Areas.FirstOrDefault(area => area.Id == unit.Area) is var area && ScenarioA1SetupCalculator.StartsOnTurnOne(area?.Kind, area?.Turn))
            .Select(unit => (Definition: catalog.Definition(unit.Definition), unit.Count)))
            .Where(item => item.Definition is not null)
            .Select(item => (item.Definition!, item.Count));

    /// <summary>A side's starting squad-equivalents: a squad is one, a HS or crew half (A16).</summary>
    public static double SquadEquivalents(ScenarioCard card, ScenarioCardSide side, UnitCatalog catalog)
    {
        ArgumentNullException.ThrowIfNull(side);
        return Starting(side, catalog).Sum(item => ScenarioA1SetupCalculator.SquadEquivalent(item.Definition.Kind) * item.Count);
    }

    /// <summary>A side's Battlefield Integrity total: the BPV of its starting MMC (A16.1), from the catalog.</summary>
    public static int IntegrityBpv(ScenarioCard card, ScenarioCardSide side, UnitCatalog catalog)
    {
        ArgumentNullException.ThrowIfNull(side);
        return ScenarioA1SetupCalculator.IntegrityBpv(Starting(side, catalog).Select(item => (item.Definition.Kind, item.Definition.Printed("broken", "asl:bpv")?.Value?.Number, item.Count)));
    }

    /// <summary>Whether a card's counter is manufactured under R0.3 (sheet MFG).</summary>
    public static bool Manufactured(UnitDefinition definition)
    {
        ArgumentNullException.ThrowIfNull(definition);
        return definition.Counter.Sheet == "MFG";
    }

    private static readonly string[] Months =
        ["January", "February", "March", "April", "May", "June", "July", "August", "September", "October", "November", "December"];

    /// <summary>The card's place and date, as far as it gives them (table player, pass 22).</summary>
    public static string PlaceAndDate(ScenarioCard card)
    {
        ArgumentNullException.ThrowIfNull(card);
        return string.IsNullOrWhiteSpace(card.Place) ? DateText(card.Date) : $"{card.Place}, {DateText(card.Date)}";
    }

    /// <summary>The card's date as a card prints it: "6 October 1942".</summary>
    public static string DateText(ScenarioCardDate date)
    {
        ArgumentNullException.ThrowIfNull(date);

        // Ruling R22.3: a minimal card may record only a month and a year, or neither.
        return date switch
        {
            { Day: > 0, Month: > 0 } => $"{date.Day} {Months[date.Month - 1]} {date.Year}",
            { Month: > 0, Year: > 0 } => $"{Months[date.Month - 1]} {date.Year}",
            { Month: > 0 } => Months[date.Month - 1],
            { Year: > 0 } => date.Year.ToString(System.Globalization.CultureInfo.InvariantCulture),
            _ => "date not recorded",
        };
    }

    /// <summary>The compass direction of a map edge, from the card's North (A2.1).</summary>
    public static string Compass(ScenarioCard card, string edge)
    {
        ArgumentNullException.ThrowIfNull(card);
        string[] clockwise = ["top", "right", "bottom", "left"];
        string[] compass = ["north", "east", "south", "west"];
        var turn = Array.IndexOf(clockwise, card.North);
        var index = Array.IndexOf(clockwise, edge);
        return turn < 0 || index < 0 ? edge : compass[(index - turn + 4) % 4];
    }

    /// <summary>A counter as a card names it: "4-6-7 1st-line squad", "9-1 leader", "heavy MG 7-16", "51mm light mortar", "FT 24-1", "DC 30".</summary>
    public static string Describe(UnitDefinition definition)
    {
        ArgumentNullException.ThrowIfNull(definition);
        int? Value(string face, string name) => definition.Printed(face, name)?.Value?.Number;
        string? Text(string face, string name) => definition.Printed(face, name)?.Value?.Text;
        var strength = $"{Value("front", "asl:firepower")}-{Value("front", "asl:range")}-{Value("front", "asl:morale")}";
        return definition.Kind switch
        {
            "asl:squad" => $"{strength} {definition.Class} squad",
            "asl:half-squad" => $"{strength} {definition.Class} half-squad",
            "asl:crew" => $"{strength} crew",
            "asl:leader" when Value("front", "asl:leadership") is { } modifier =>
                $"{Value("front", "asl:morale")}{(modifier > 0 ? "+" : "-")}{Math.Abs(modifier)} {(definition.Id.Contains("commissar", StringComparison.Ordinal) ? "Commissar" : "leader")}",
            "asl:hero" => "hero",
            "asl:mg" => $"{Text("front", "asl:size")} MG {Value("front", "asl:firepower")}-{Value("front", "asl:range")}",
            "asl:light-mortar" => $"{Value("front", "asl:caliber")}mm light mortar",
            "asl:latw" => $"{Text("front", "asl:latw-type")?.ToUpperInvariant()} {Value("front", "asl:firepower")}-{Value("front", "asl:range")}",
            "asl:ft" => $"FT {Value("front", "asl:firepower")}-{Value("front", "asl:range")}",
            "asl:dc" => $"DC {Value("front", "asl:firepower")}",
            _ => definition.Id,
        };
    }

    /// <summary>
    /// Whether the Player Turn ending now is the game's last (A3.9; ruling R20.1): the second of the card's last Game Turn, or its first when the card
    /// gives that Game Turn only one Player Turn.
    /// </summary>
    public static bool EndsAfter(ScenarioCardTurns turns, int turn, bool firstSidePhasing)
    {
        ArgumentNullException.ThrowIfNull(turns);
        return Rules.ScenarioA1ResultTables.EndsAfter(turns.Count, turns.HalfTurn, turn, firstSidePhasing);
    }

    /// <summary>A lettered hexrow's place from west to east (A2.2): A is 0, Z 25, AA 26, and GG 32; null for anything else.</summary>
    public static int? HexrowIndex(string? row) => ScenarioA1SetupCalculator.HexrowIndex(row);

    /// <summary>
    /// Whether a Location lies in the card's playable area (A2.1; ruling R20.6): always, when the card enforces none; otherwise on its board, in its
    /// hexrows.
    /// </summary>
    public static bool Playable(ScenarioCard card, BoardLocation at)
    {
        ArgumentNullException.ThrowIfNull(card);
        ArgumentNullException.ThrowIfNull(at);
        return card.PlayableArea is not { Enforced: true, Hexrows: { } rows }
            || ScenarioA1SetupCalculator.Playable(true, rows.From, rows.To, rows.Board, card.Boards.Count, card.Boards.Count == 1 ? card.Boards[0].Board : null, at.Board.Value, at.Hex.ToString());
    }
}
