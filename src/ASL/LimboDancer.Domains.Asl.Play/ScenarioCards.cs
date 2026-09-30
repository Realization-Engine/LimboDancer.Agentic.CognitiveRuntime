using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using LimboDancer.Domains.Asl.Maps.Coordinates;
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
/// to its OB, set up with any of its groups (ruling R20.4).
/// </summary>
public sealed record ScenarioCardSide(string Side, int San, int? IntegrityBpv, ScenarioCardEdge FriendlyEdge, string Balance, IReadOnlyList<ScenarioCardGroup> Groups,
    IReadOnlyList<ScenarioCardUnit>? BalanceUnits = null);

/// <summary>
/// An SSR (ruling R17.10): its text; <c>token</c> when the game reads it from its tokens, <c>game-default</c> when the game
/// already plays it with no token, or <c>not-enforced</c> when it is shown only (with a note saying why).
/// </summary>
public sealed record ScenarioCardRule(int Number, string Text, string Status, IReadOnlyList<string> Tokens, IReadOnlyList<string> Rules, string? Note);

/// <summary>The Victory Conditions as text in A26 terms (ruling R17.11); never evaluated in pass 17.</summary>
public sealed record ScenarioCardVictory(string Kind, string Text, IReadOnlyList<string> Rules);

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
public static partial class ScenarioCards
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

    /// <summary>The bases of a Friendly Board Edge (A20.53; ruling R17.7).</summary>
    public static IReadOnlyList<string> EdgeBases { get; } = ["ssr", "entry", "setup", "manufactured"];

    /// <summary>The SSR statuses (ruling R17.10).</summary>
    public static IReadOnlyList<string> RuleStatuses { get; } = ["token", "game-default", "not-enforced"];

    /// <summary>The kinds of Victory Conditions (A26; ruling R17.11).</summary>
    public static IReadOnlyList<string> VictoryKinds { get; } = ["control", "exit", "casualty", "other"];

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

    /// <summary>The SHA-256 of an embedded card's text, with line endings as LF (ruling R18.2); null when none has that name.</summary>
    public static string? Sha256(string name)
    {
        ArgumentNullException.ThrowIfNull(name);
        using var stream = typeof(ScenarioCards).Assembly.GetManifestResourceStream(Prefix + name + Suffix);
        if (stream is null)
        {
            return null;
        }

        using var reader = new StreamReader(stream);
        var text = reader.ReadToEnd().ReplaceLineEndings("\n");
        return Convert.ToHexStringLower(System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(text)));
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
            var node = new JsonObject { ["id"] = side.Side, ["nationality"] = side.Side, ["san"] = side.San, ["friendlyEdge"] = side.FriendlyEdge.Edge };
            var elrs = side.Groups.Select(group => group.Elr).Distinct().ToArray();
            if (elrs.Length == 1)
            {
                node["elr"] = elrs[0];
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
            ["scenarioMonth"] = card.Date.Month,
            ["scenarioYear"] = card.Date.Year,
            ["specialRules"] = new JsonArray([.. card.Tokens.Select(token => (JsonNode)JsonValue.Create(token))]),
            ["scenario"] = new JsonObject { ["id"] = card.Id, ["sha256"] = sha256, ["title"] = card.Title },
        };
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
        Check(card.Catalog == $"{catalog.Identity.Catalog}@{catalog.Identity.Version}",
            $"card.catalog: the card names '{card.Catalog}', and the catalog is {catalog.Identity.Catalog}@{catalog.Identity.Version}");
        Check(card.Source is { Basis.Length: > 0, Legacy.Length: > 0 }, "card.source: a card says what it rests on and which legacy card it adapts (ruling R17.2)");
        Check(card.Date.Year is >= 1936 and <= 1945 && card.Date.Month is >= 1 and <= 12 && card.Date.Day >= 1 && card.Date.Day <= DateTime.DaysInMonth(card.Date.Year, card.Date.Month),
            "card.date: the date is a real day of 1936 to 1945");

        // A2.1 (ruling R17.3): the boards, each in its own slot.
        Check(card.Boards.Count > 0, "card.boards: a card names its boards (A2.1)");
        foreach (var board in card.Boards)
        {
            Check(BoardRef.TryParse(board.Board, out _) && board.Column >= 0 && board.Row >= 0, $"card.boards: '{board.Board}' is not a board in a slot");
        }

        Check(card.Boards.Select(board => (board.Column, board.Row)).Distinct().Count() == card.Boards.Count, "card.boards: two boards share a slot");
        Check(card.Boards.Select(board => board.Board).Distinct(StringComparer.Ordinal).Count() == card.Boards.Count, "card.boards: a board is named twice");
        Check(SideState.Edges.Contains(card.North, StringComparer.Ordinal), "card.north: North is the map's top, bottom, left, or right");

        // A2.1 (ruling R20.6): an enforced playable area names its hexrows on a board of the card.
        if (card.PlayableArea is { } playable)
        {
            Check(!playable.Enforced || (playable.Hexrows is { } rows && HexrowIndex(rows.From) is { } from && HexrowIndex(rows.To) is { } to && from <= to
                && (rows.Board is null ? card.Boards.Count == 1 : card.Boards.Any(board => board.Board == rows.Board))),
                "card.playable: an enforced playable area names its hexrows from and to, A to GG, on a board of the card (A2.1; ruling R20.6)");
        }

        // A3.9 (ruling R17.4): the Turn Record Chart.
        var sides = card.Sides.Select(side => side.Side).ToArray();
        Check(card.Sides.Count == 2 && sides.Distinct(StringComparer.Ordinal).Count() == 2, "card.sides: a card has two sides");
        Check(card.Turns.Count is >= 1 and <= 30, "card.turns: a card has 1 to 30 Game Turns (A3.9)");
        Check(sides.Contains(card.Turns.SetsUpFirst, StringComparer.Ordinal)
            && (card.Turns.MovesFirst is null ? !string.IsNullOrWhiteSpace(card.Turns.MovesFirstNote) : sides.Contains(card.Turns.MovesFirst, StringComparer.Ordinal)),
            "card.turns: the side that sets up first and the side that moves first are the card's sides, or a note says how the first move is decided (A3.9)");

        // Ruling R17.12: a sequential setup numbers every group from 1, the first of them the side that sets up first.
        var groups = card.Sides.SelectMany(side => side.Groups.Select(group => (side.Side, Group: group))).ToArray();
        if (groups.Any(item => item.Group.SetupOrder is not null))
        {
            var orders = groups.Select(item => item.Group.SetupOrder ?? 0).ToArray();
            Check(orders.All(order => order >= 1) && orders.Max() == orders.Distinct().Count() && orders.Distinct().Count() <= orders.Length
                && groups.Where(item => item.Group.SetupOrder == 1).All(item => item.Side == card.Turns.SetsUpFirst),
                "card.setup: a sequential setup numbers the groups from 1, without gaps, beginning with the side that sets up first (A12.12)");
        }

        Check(groups.All(item => item.Group.Dummies is null or >= 0), "card.ob: a group's \"?\" are a count of 0 or more (A12.11)");

        foreach (var side in card.Sides)
        {
            Side(card, side, catalog, Check);
        }

        // A16 and A16.1 (ruling R17.4): the bracketed Battlefield Integrity totals.
        var equivalents = card.Sides.Select(side => SquadEquivalents(card, side, catalog)).ToArray();
        foreach (var side in card.Sides.Where(side => side.IntegrityBpv is not null))
        {
            Check(Starting(side, catalog).Where(item => Mmc(item.Definition)).All(item => item.Definition.Printed("broken", "asl:bpv")?.Value?.Number is not null),
                $"card.integrity: {side.Side} prints a Battlefield Integrity total, and one of its starting MMC has no BPV in the catalog (A16.1)");
            Check(equivalents.All(count => count >= 10),
                $"card.integrity: {side.Side} prints a Battlefield Integrity total, and a side starts with fewer than ten squad-equivalents (A16)");
            Check(side.IntegrityBpv == IntegrityBpv(card, side, catalog),
                $"card.integrity: {side.Side} prints [{side.IntegrityBpv}], and the BPV of its starting MMC is {IntegrityBpv(card, side, catalog)} (A16.1)");
        }

        // Index, Scenario Attacker/Defender (ruling R17.6): a Defender faces a side that enters wholly from offboard.
        if (card.ScenarioDefender is { } defender)
        {
            var attacker = card.Sides.FirstOrDefault(side => side.Side != defender);
            Check(sides.Contains(defender, StringComparer.Ordinal), $"card.defender: '{defender}' is not a side of the card");
            Check(card.Sides.FirstOrDefault(side => side.Side == defender) is { } own && own.Groups.SelectMany(group => group.Areas).Any(area => area.Kind != "entry"),
                "card.defender: a Scenario Defender sets up wholly or partly on board (Index, Scenario Attacker/Defender)");
            Check(attacker is not null && attacker.Groups.SelectMany(group => group.Areas).All(area => area.Kind == "entry"),
                "card.defender: a Scenario Defender faces a side that enters wholly from offboard (Index, Scenario Attacker/Defender)");
        }

        // Ruling R17.10: the SSRs, numbered in order, and their tokens checked as a new game checks them.
        Check(card.SpecialRules.Select(rule => rule.Number).SequenceEqual(Enumerable.Range(1, card.SpecialRules.Count)), "card.ssr: the SSRs are numbered 1 onward");
        foreach (var rule in card.SpecialRules)
        {
            Check(RuleStatuses.Contains(rule.Status, StringComparer.Ordinal), $"card.ssr: SSR {rule.Number} has no status {string.Join(", ", RuleStatuses)}");
            Check(rule.Status == "token" ? rule.Tokens.Count > 0 : rule.Tokens.Count == 0, $"card.ssr: SSR {rule.Number} has tokens exactly when the game reads it");
            Check(rule.Status != "not-enforced" || !string.IsNullOrWhiteSpace(rule.Note), $"card.ssr: SSR {rule.Number} is not enforced and says why");
            Check(!string.IsNullOrWhiteSpace(rule.Text), $"card.ssr: SSR {rule.Number} has its text");
            Cited(rule.Rules, $"SSR {rule.Number}", Check);
        }

        var start = new JsonObject
        {
            ["specialRules"] = new JsonArray([.. card.Tokens.Select(token => (JsonNode)JsonValue.Create(token))]),
            ["scenarioMonth"] = card.Date.Month,
            ["scenarioYear"] = card.Date.Year,
        };
        using (var document = JsonDocument.Parse(start.ToJsonString()))
        {
            var refused = GamePlanner.NightAndWeatherRulesBar(document.RootElement);
            Check(refused is null, $"card.ssr: {refused}");
        }

        // A26 (ruling R17.11): the Victory Conditions as text, with their kind and the rules they rest on.
        Check(VictoryKinds.Contains(card.VictoryConditions.Kind, StringComparer.Ordinal) && !string.IsNullOrWhiteSpace(card.VictoryConditions.Text)
            && card.VictoryConditions.Rules.Count > 0, "card.victory: the Victory Conditions have a kind, their text, and the A26 rules they rest on");
        Cited(card.VictoryConditions.Rules, "the Victory Conditions", Check);
        return found;
    }

    private static void Side(ScenarioCard card, ScenarioCardSide side, UnitCatalog catalog, Action<bool, string> check)
    {
        check(side.San is >= 0 and <= 7, $"card.san: {side.Side}'s SAN is 0 to 7 (A14.1)");
        check(SideState.Edges.Contains(side.FriendlyEdge.Edge, StringComparer.Ordinal) && EdgeBases.Contains(side.FriendlyEdge.Basis, StringComparer.Ordinal),
            $"card.edge: {side.Side}'s Friendly Board Edge is the map's top, bottom, left, or right, from an SSR, entry, setup, or R0.3 (A20.53)");
        check(side.FriendlyEdge.Basis != "entry" || side.Groups.SelectMany(group => group.Areas).Any(area => area.Kind == "entry" && area.Edge == side.FriendlyEdge.Edge),
            $"card.edge: {side.Side}'s Friendly Board Edge rests on its entry, and it enters along no such edge (A20.53)");
        check(!string.IsNullOrWhiteSpace(side.Balance), $"card.balance: {side.Side} has its Balance provision (A26.4)");
        foreach (var unit in side.BalanceUnits ?? [])
        {
            var definition = catalog.Definition(unit.Definition);
            check(definition is not null && definition.Nationality == side.Side && unit.Count >= 1 && unit.Area is null,
                $"card.balance: the Balance counter '{unit.Definition}' is a {side.Side} definition of the catalog, at least one, with no area of its own (ruling R20.4)");
        }
        check(side.Groups.Count > 0, $"card.ob: {side.Side} has an OB");
        foreach (var group in side.Groups)
        {
            check(group.Elr is >= 0 and <= 5, $"card.elr: '{group.Name}' has an ELR of 0 to 5 (A19.1)");
            check(group.Areas.Count > 0 && group.Areas.Select(area => area.Id).Distinct(StringComparer.Ordinal).Count() == group.Areas.Count,
                $"card.setup: '{group.Name}' names its setup areas once each");
            foreach (var area in group.Areas)
            {
                check(Area(card, area), $"card.setup: '{group.Name}' area '{area.Id}' is not a valid {area.Kind} area (ruling R17.8)");
            }

            foreach (var unit in group.Units)
            {
                var definition = catalog.Definition(unit.Definition);
                check(definition is not null, $"card.ob: '{unit.Definition}' is not in the catalog");
                check(definition is null || definition.Nationality == side.Side, $"card.ob: '{unit.Definition}' is not {side.Side}");
                check(unit.Count >= 1, $"card.ob: '{unit.Definition}' has a count of at least 1");
                check(unit.Area is null || group.Areas.Any(area => area.Id == unit.Area), $"card.ob: '{unit.Definition}' names the unknown area '{unit.Area}'");
            }
        }
    }

    private static bool Area(ScenarioCard card, ScenarioCardSetup area) => area.Kind switch
    {
        "building" => area.Hexes is { Count: > 0 } hexes && hexes.All(hex => HexPattern().IsMatch(hex)) && hexes.Contains(area.Id, StringComparer.Ordinal)
            && (area.Board is null ? card.Boards.Count == 1 : card.Boards.Any(board => board.Board == area.Board)),
        "hex-numbers" => card.Boards.Any(board => board.Board == area.Board) && area.From is >= 0 and <= 10 && area.To is >= 0 and <= 10 && area.From <= area.To,
        "entry" => area.Turn is { } turn && turn >= 1 && turn <= card.Turns.Count && SideState.Edges.Contains(area.Edge ?? string.Empty, StringComparer.Ordinal),
        _ => false,
    } && (area.Counters is null || (area.Kind != "entry" && area.Counters >= 1)) && (area.MinMmc is null || (area.Counters is { } counters && area.MinMmc >= 0 && area.MinMmc <= counters));

    private static void Cited(IReadOnlyList<string> rules, string where, Action<bool, string> check)
    {
        foreach (var rule in rules)
        {
            check(CitableRules.Contains(rule, StringComparer.Ordinal) || RulingPattern().IsMatch(rule),
                $"card.rules: {where} cites '{rule}', which is neither a compared rule fragment nor a ruling (ruling R17.2)");
        }
    }

    /// <summary>The units a side starts with: everything but what enters after Turn 1 (A16.1).</summary>
    private static IEnumerable<(UnitDefinition Definition, int Count)> Starting(ScenarioCardSide side, UnitCatalog catalog) =>
        side.Groups.SelectMany(group => group.Units
            .Where(unit => group.Areas.FirstOrDefault(area => area.Id == unit.Area) is not { Kind: "entry", Turn: > 1 })
            .Select(unit => (Definition: catalog.Definition(unit.Definition), unit.Count)))
            .Where(item => item.Definition is not null)
            .Select(item => (item.Definition!, item.Count));

    /// <summary>A side's starting squad-equivalents: a squad is one, a HS or crew half (A16).</summary>
    public static double SquadEquivalents(ScenarioCard card, ScenarioCardSide side, UnitCatalog catalog)
    {
        ArgumentNullException.ThrowIfNull(side);
        return Starting(side, catalog).Sum(item => item.Definition.Kind switch
        {
            "asl:squad" => 1.0,
            "asl:half-squad" or "asl:crew" => 0.5,
            _ => 0.0,
        } * item.Count);
    }

    /// <summary>A side's Battlefield Integrity total: the BPV of its starting MMC (A16.1), from the catalog.</summary>
    public static int IntegrityBpv(ScenarioCard card, ScenarioCardSide side, UnitCatalog catalog)
    {
        ArgumentNullException.ThrowIfNull(side);
        return Starting(side, catalog)
            .Where(item => Mmc(item.Definition))
            .Sum(item => (item.Definition.Printed("broken", "asl:bpv")?.Value?.Number ?? 0) * item.Count);
    }

    private static bool Mmc(UnitDefinition definition) => definition.Kind is "asl:squad" or "asl:half-squad" or "asl:crew";

    /// <summary>Whether a card's counter is manufactured under R0.3 (sheet MFG).</summary>
    public static bool Manufactured(UnitDefinition definition)
    {
        ArgumentNullException.ThrowIfNull(definition);
        return definition.Counter.Sheet == "MFG";
    }

    private static readonly string[] Months =
        ["January", "February", "March", "April", "May", "June", "July", "August", "September", "October", "November", "December"];

    /// <summary>The card's date as a card prints it: "6 October 1942".</summary>
    public static string DateText(ScenarioCardDate date)
    {
        ArgumentNullException.ThrowIfNull(date);
        return $"{date.Day} {Months[date.Month - 1]} {date.Year}";
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
        return turn >= turns.Count && (!firstSidePhasing || turns.HalfTurn);
    }

    /// <summary>A lettered hexrow's place from west to east (A2.2): A is 0, Z 25, AA 26, and GG 32; null for anything else.</summary>
    public static int? HexrowIndex(string? row) => row switch
    {
        { Length: 1 } when row[0] is >= 'A' and <= 'Z' => row[0] - 'A',
        { Length: 2 } when row[0] == row[1] && row[0] is >= 'A' and <= 'G' => 26 + (row[0] - 'A'),
        _ => null,
    };

    /// <summary>
    /// Whether a Location lies in the card's playable area (A2.1; ruling R20.6): always, when the card enforces none; otherwise on its board, in its
    /// hexrows.
    /// </summary>
    public static bool Playable(ScenarioCard card, BoardLocation at)
    {
        ArgumentNullException.ThrowIfNull(card);
        ArgumentNullException.ThrowIfNull(at);
        if (card.PlayableArea is not { Enforced: true, Hexrows: { } rows })
        {
            return true;
        }

        // The hexrows limit their own board; the card's other boards play whole (referee, pass 20).
        var board = rows.Board ?? (card.Boards.Count == 1 ? card.Boards[0].Board : null);
        if (board is not null && board != at.Board.Value)
        {
            return true;
        }

        var letters = new string(at.Hex.ToString().TakeWhile(char.IsLetter).ToArray());
        return HexrowIndex(letters) is { } index && index >= HexrowIndex(rows.From) && index <= HexrowIndex(rows.To);
    }

    [GeneratedRegex("^([A-Z]|AA|BB|CC|DD|EE|FF|GG)(10|[0-9])$")]
    private static partial Regex HexPattern();

    [GeneratedRegex(@"^R\d+\.\d+$")]
    private static partial Regex RulingPattern();
}
