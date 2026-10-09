using System.Text.RegularExpressions;

namespace LimboDancer.Domains.Asl.Rules;

/// <summary>A board of a card as a fact (A2.1; ruling R17.3): its name, whether the name is a board, and its slot.</summary>
public sealed record CardBoardFacts(string Board, bool IsBoard, int Column, int Row);

/// <summary>
/// A setup area of a card as facts (ruling R17.8): its id and kind, its hexes, whether it names a board and whether that board is on the card, its hex
/// numbers, its entry turn and edge, and the SSR counts.
/// </summary>
public sealed record CardAreaFacts(string Id, string Kind, IReadOnlyList<string>? Hexes, bool BoardNamed, bool BoardOnCard, int? From, int? To, int? Turn, string? Edge,
    int? Counters, int? MinMmc);

/// <summary>An OB line of a card as facts: its definition, whether the catalog has it and it is of the side's nationality, its count, and whether its area is one of the group's.</summary>
public sealed record CardLineFacts(string Definition, bool Known, bool OwnNationality, int Count, string? Area, bool AreaKnown);

/// <summary>An OB group of a card as facts (A19.1; rulings R17.5, R17.8): its name, ELR, areas, and lines.</summary>
public sealed record CardGroupFacts(string Name, int Elr, IReadOnlyList<CardAreaFacts> Areas, IReadOnlyList<CardLineFacts> Units);

/// <summary>
/// A side of a card as facts (A14.1, A20.53, A25.8, A26.4; rulings R17.7, R20.4, R22.3, R27.1): its id, SAN, nation, Friendly Board Edge and basis,
/// whether it enters along that edge, its own ELR, its Balance text, its Balance counters (whether each is a known definition of the side, with a count and
/// no area), and its groups.
/// </summary>
public sealed record CardSideFacts(string Side, int San, string? Nation, string Edge, string Basis, bool EntersAlongEdge, int? Elr, string? Balance,
    IReadOnlyList<CardLineFacts> BalanceUnits, IReadOnlyList<CardGroupFacts> Groups);

/// <summary>An SSR of a card as facts (ruling R17.10): its number, status, token count, note, text, cited rules, and its HIP tokens' named sides.</summary>
public sealed record CardRuleFacts(int Number, string Status, int Tokens, string? Note, string Text, IReadOnlyList<string> Rules, IReadOnlyList<(string Token, string? Side)> HipTokens);

/// <summary>A structured Victory Condition of a card as facts (ruling R21.3), with the fields its type reads.</summary>
public sealed record CardConditionFacts(string Type, string Side, IReadOnlyList<string>? Buildings, IReadOnlyList<string>? Versus, int? Margin, string? Building, int? AtLeast,
    double? Ratio, string? Edge, IReadOnlyList<string>? Near);

/// <summary>
/// The rules of a scenario card and of a setup from it (pass 32.j, S10): a card's checks beyond its format (A2.1, A3.9, A12.11, A12.12, A14.1, A16, A16.1,
/// A19.1, A20.53, A25.8, A26, A26.3, A26.4; rulings R17.3 to R17.12, R20.4, R20.6, R21.3, R22.3, R23.5, R27.1), the playable area and the setup areas
/// (rulings R17.8, R19.3, R20.6), and the setup's legality and fill (A2.9, A5.1, A5.5, A12.3, A12.11, A12.12, A12.34, A25.2, A26.4; rulings R19.1 to R19.6,
/// R20.4, R20.5, R20.7, R23.5, R25.4, R26.2, R26.3, R26.5, R31.4). Play reads the card, the catalog, the state, and the map, hands the facts over, and
/// keeps the card records (the design's D11).
/// </summary>
public static class ScenarioA1SetupCalculator
{
    private static readonly Regex HexPattern = new("^([A-Z]|AA|BB|CC|DD|EE|FF|GG)(10|[0-9])$", RegexOptions.CultureInvariant);

    private static readonly Regex RulingPattern = new(@"^R\d+\.\d+$", RegexOptions.CultureInvariant);

    // The card's boards and its playable area (A2.1; rulings R17.3, R20.6).

    /// <summary>A card names its boards, each a board in its own slot, named once, and a North edge of the map.</summary>
    public static IEnumerable<string> BoardDiagnostics(IReadOnlyList<CardBoardFacts> boards, string north, IReadOnlyList<string> edges)
    {
        ArgumentNullException.ThrowIfNull(boards);
        ArgumentNullException.ThrowIfNull(edges);
        if (boards.Count == 0)
        {
            yield return "card.boards: a card names its boards (A2.1)";
        }

        foreach (var board in boards)
        {
            if (!(board.IsBoard && board.Column >= 0 && board.Row >= 0))
            {
                yield return $"card.boards: '{board.Board}' is not a board in a slot";
            }
        }

        if (boards.Select(board => (board.Column, board.Row)).Distinct().Count() != boards.Count)
        {
            yield return "card.boards: two boards share a slot";
        }

        if (boards.Select(board => board.Board).Distinct(StringComparer.Ordinal).Count() != boards.Count)
        {
            yield return "card.boards: a board is named twice";
        }

        if (!edges.Contains(north, StringComparer.Ordinal))
        {
            yield return "card.north: North is the map's top, bottom, left, or right";
        }
    }

    /// <summary>A2.1 (ruling R20.6): an enforced playable area names its hexrows, A to GG, from and to, on a board of the card; null when it does.</summary>
    public static string? PlayableAreaDiagnostic(bool enforced, bool hexrowsGiven, string? from, string? to, bool boardNamed, int boardCount, bool boardOnCard) =>
        !enforced || (hexrowsGiven && HexrowIndex(from) is { } first && HexrowIndex(to) is { } last && first <= last && (!boardNamed ? boardCount == 1 : boardOnCard))
            ? null
            : "card.playable: an enforced playable area names its hexrows from and to, A to GG, on a board of the card (A2.1; ruling R20.6)";

    /// <summary>A lettered hexrow's place from west to east (A2.2): A is 0, Z 25, AA 26, and GG 32; null for anything else.</summary>
    public static int? HexrowIndex(string? row) => row switch
    {
        { Length: 1 } when row[0] is >= 'A' and <= 'Z' => row[0] - 'A',
        { Length: 2 } when row[0] == row[1] && row[0] is >= 'A' and <= 'G' => 26 + (row[0] - 'A'),
        _ => null,
    };

    /// <summary>
    /// Whether a Location lies in the card's playable area (A2.1; ruling R20.6): always, when the card enforces none; otherwise on its board, in its hexrows.
    /// The hexrows limit their own board; the card's other boards play whole (referee, pass 20).
    /// </summary>
    public static bool Playable(bool enforced, string? from, string? to, string? rowsBoard, int boardCount, string? firstBoard, string atBoard, string hexName)
    {
        ArgumentNullException.ThrowIfNull(hexName);
        if (!enforced)
        {
            return true;
        }

        var board = rowsBoard ?? (boardCount == 1 ? firstBoard : null);
        if (board is not null && board != atBoard)
        {
            return true;
        }

        var letters = new string(hexName.TakeWhile(char.IsLetter).ToArray());
        return HexrowIndex(letters) is { } index && index >= HexrowIndex(from) && index <= HexrowIndex(to);
    }

    /// <summary>Whether a Location lies in a setup area of the card (R17.8, R19.3): a building's hexes, or a board's hex numbers; the area's board, or the card's only one.</summary>
    public static bool Within(string kind, IReadOnlyList<string>? hexes, string? areaBoard, int? from, int? to, int boardCount, string? firstBoard, string atBoard, string hexName, int rowNumber)
    {
        var board = areaBoard ?? (boardCount == 1 ? firstBoard : null);
        return board == atBoard && kind switch
        {
            "building" => hexes?.Contains(hexName, StringComparer.Ordinal) == true,
            "hex-numbers" => rowNumber >= from && rowNumber <= to,
            _ => false,
        };
    }

    // The Turn Record Chart and the setup order (A3.9, A12.11, A12.12; rulings R17.4, R17.12).

    /// <summary>Two sides; 1 to 30 Game Turns; a first setter and a first mover, or a note; a sequential setup numbered from 1 without gaps; "?" counts of 0 or more.</summary>
    public static IEnumerable<string> TurnDiagnostics(IReadOnlyList<string> sides, int turnCount, string setsUpFirst, string? movesFirst, string? movesFirstNote,
        IReadOnlyList<(string Side, int? SetupOrder, int? Dummies)> groups)
    {
        ArgumentNullException.ThrowIfNull(sides);
        ArgumentNullException.ThrowIfNull(groups);
        if (!(sides.Count == 2 && sides.Distinct(StringComparer.Ordinal).Count() == 2))
        {
            yield return "card.sides: a card has two sides";
        }

        if (turnCount is not (>= 1 and <= 30))
        {
            yield return "card.turns: a card has 1 to 30 Game Turns (A3.9)";
        }

        if (!(sides.Contains(setsUpFirst, StringComparer.Ordinal) && (movesFirst is null ? !string.IsNullOrWhiteSpace(movesFirstNote) : sides.Contains(movesFirst, StringComparer.Ordinal))))
        {
            yield return "card.turns: the side that sets up first and the side that moves first are the card's sides, or a note says how the first move is decided (A3.9)";
        }

        if (groups.Any(item => item.SetupOrder is not null))
        {
            var orders = groups.Select(item => item.SetupOrder ?? 0).ToArray();
            if (!(orders.All(order => order >= 1) && orders.Max() == orders.Distinct().Count() && orders.Distinct().Count() <= orders.Length
                && groups.Where(item => item.SetupOrder == 1).All(item => item.Side == setsUpFirst)))
            {
                yield return "card.setup: a sequential setup numbers the groups from 1, without gaps, beginning with the side that sets up first (A12.12)";
            }
        }

        if (!groups.All(item => item.Dummies is null or >= 0))
        {
            yield return "card.ob: a group's \"?\" are a count of 0 or more (A12.11)";
        }
    }

    // A side (A14.1, A19.1, A20.53, A25.8, A26.4; rulings R17.5, R17.7, R17.8, R20.4, R22.3, R27.1).

    /// <summary>SAN 0 to 7; only an Axis Minor side names a nation; the Friendly Board Edge and its basis; ELR; the Balance and its counters; the OB groups, their ELR, areas, and lines.</summary>
    public static IEnumerable<string> SideDiagnostics(CardSideFacts side, bool minimal, IReadOnlyList<string> edges, IReadOnlyList<string> edgeBases, int boardCount, int turnCount)
    {
        ArgumentNullException.ThrowIfNull(side);
        ArgumentNullException.ThrowIfNull(edges);
        ArgumentNullException.ThrowIfNull(edgeBases);
        if (side.San is not (>= 0 and <= 7))
        {
            yield return $"card.san: {side.Side}'s SAN is 0 to 7 (A14.1)";
        }

        if (!(side.Side == "axis-minor" ? ScenarioA1Definitions.AxisMinorNations.Contains(side.Nation ?? "", StringComparer.Ordinal) : side.Nation is null))
        {
            yield return $"card.nation: an Axis Minor side names its nation ({string.Join(", ", ScenarioA1Definitions.AxisMinorNations)}), and no other side names one (A25.8; ruling R27.1)";
        }

        if (!((minimal && side.Edge.Length == 0 && side.Basis == "none")
            || (edges.Contains(side.Edge, StringComparer.Ordinal) && edgeBases.Contains(side.Basis, StringComparer.Ordinal) && side.Basis != "none")))
        {
            yield return $"card.edge: {side.Side}'s Friendly Board Edge is the map's top, bottom, left, or right, from an SSR, entry, setup, or R0.3 (A20.53); a minimal card may leave it unnamed (ruling R22.3)";
        }

        if (!(side.Elr is null || (minimal && side.Elr is >= 0 and <= 5)))
        {
            yield return $"card.elr: only a minimal card gives {side.Side} an ELR of its own, 0 to 5 (A19.1; ruling R22.3)";
        }

        if (!(side.Basis != "entry" || side.EntersAlongEdge))
        {
            yield return $"card.edge: {side.Side}'s Friendly Board Edge rests on its entry, and it enters along no such edge (A20.53)";
        }

        if (!(minimal || !string.IsNullOrWhiteSpace(side.Balance)))
        {
            yield return $"card.balance: {side.Side} has its Balance provision (A26.4)";
        }

        foreach (var unit in side.BalanceUnits)
        {
            if (!(unit.Known && unit.OwnNationality && unit.Count >= 1 && unit.Area is null))
            {
                yield return $"card.balance: the Balance counter '{unit.Definition}' is a {side.Side} definition of the catalog, at least one, with no area of its own (ruling R20.4)";
            }
        }

        if (!(side.Groups.Count > 0 || minimal))
        {
            yield return $"card.ob: {side.Side} has an OB, unless the card is a minimal card, whose sides have none (ruling R22.3)";
        }

        foreach (var group in side.Groups)
        {
            if (group.Elr is not (>= 0 and <= 5))
            {
                yield return $"card.elr: '{group.Name}' has an ELR of 0 to 5 (A19.1)";
            }

            if (!(group.Areas.Count > 0 && group.Areas.Select(area => area.Id).Distinct(StringComparer.Ordinal).Count() == group.Areas.Count))
            {
                yield return $"card.setup: '{group.Name}' names its setup areas once each";
            }

            foreach (var area in group.Areas)
            {
                if (!AreaValid(area, boardCount, turnCount, edges))
                {
                    yield return $"card.setup: '{group.Name}' area '{area.Id}' is not a valid {area.Kind} area (ruling R17.8)";
                }
            }

            foreach (var unit in group.Units)
            {
                if (!unit.Known)
                {
                    yield return $"card.ob: '{unit.Definition}' is not in the catalog";
                }

                if (!(!unit.Known || unit.OwnNationality))
                {
                    yield return $"card.ob: '{unit.Definition}' is not {side.Side}";
                }

                if (unit.Count < 1)
                {
                    yield return $"card.ob: '{unit.Definition}' has a count of at least 1";
                }

                if (!unit.AreaKnown)
                {
                    yield return $"card.ob: '{unit.Definition}' names the unknown area '{unit.Area}'";
                }
            }
        }
    }

    /// <summary>
    /// The form of a setup area (ruling R17.8): building hexes that include the area's id, on the card's board or its own; hex numbers 0 to 10 on a board of
    /// the card; or an entry on a Game Turn of the card along an edge, with optional entry hexes on their board (A2.5; ruling R25.2); SSR counts that fit.
    /// </summary>
    public static bool AreaValid(CardAreaFacts area, int boardCount, int turnCount, IReadOnlyList<string> edges)
    {
        ArgumentNullException.ThrowIfNull(area);
        ArgumentNullException.ThrowIfNull(edges);
        return area.Kind switch
        {
            "building" => area.Hexes is { Count: > 0 } hexes && hexes.All(hex => HexPattern.IsMatch(hex)) && hexes.Contains(area.Id, StringComparer.Ordinal)
                && (!area.BoardNamed ? boardCount == 1 : area.BoardOnCard),
            "hex-numbers" => area.BoardOnCard && area.From is >= 0 and <= 10 && area.To is >= 0 and <= 10 && area.From <= area.To,
            "entry" => area.Turn is { } turn && turn >= 1 && turn <= turnCount && edges.Contains(area.Edge ?? string.Empty, StringComparer.Ordinal)
                && (area.Hexes is null || (area.Hexes.Count > 0 && area.Hexes.All(hex => HexPattern.IsMatch(hex)) && (!area.BoardNamed ? boardCount == 1 : area.BoardOnCard))),
            _ => false,
        } && (area.Counters is null || (area.Kind != "entry" && area.Counters >= 1)) && (area.MinMmc is null || (area.Counters is { } counters && area.MinMmc >= 0 && area.MinMmc <= counters));
    }

    // Battlefield Integrity (A16, A16.1; ruling R17.4).

    /// <summary>The units a side starts with: every OB line but those whose area is an entry after Turn 1 (A16.1).</summary>
    public static bool StartsOnTurnOne(string? areaKind, int? areaTurn) => !(areaKind == "entry" && areaTurn > 1);

    /// <summary>A squad is one squad-equivalent, a HS or crew half (A16).</summary>
    public static double SquadEquivalent(string kind) => kind switch
    {
        "asl:squad" => 1.0,
        "asl:half-squad" or "asl:crew" => 0.5,
        _ => 0.0,
    };

    /// <summary>A MMC is a squad, HS, or crew.</summary>
    public static bool Mmc(string kind) => kind is "asl:squad" or "asl:half-squad" or "asl:crew";

    /// <summary>A side's Battlefield Integrity total: the broken-side BPV of its starting MMC times their counts (A16.1).</summary>
    public static int IntegrityBpv(IEnumerable<(string Kind, int? Bpv, int Count)> starting)
    {
        ArgumentNullException.ThrowIfNull(starting);
        return starting.Where(item => Mmc(item.Kind)).Sum(item => (item.Bpv ?? 0) * item.Count);
    }

    /// <summary>A printed Battlefield Integrity total needs a BPV on every starting MMC, ten squad-equivalents a side, and equals the BPV sum of the starting MMC.</summary>
    public static IEnumerable<string> IntegrityDiagnostics(string side, int printed, bool everyStartingMmcHasBpv, IReadOnlyList<double> equivalents, int computed)
    {
        ArgumentNullException.ThrowIfNull(equivalents);
        if (!everyStartingMmcHasBpv)
        {
            yield return $"card.integrity: {side} prints a Battlefield Integrity total, and one of its starting MMC has no BPV in the catalog (A16.1)";
        }

        if (!equivalents.All(count => count >= 10))
        {
            yield return $"card.integrity: {side} prints a Battlefield Integrity total, and a side starts with fewer than ten squad-equivalents (A16)";
        }

        if (printed != computed)
        {
            yield return $"card.integrity: {side} prints [{printed}], and the BPV of its starting MMC is {computed} (A16.1)";
        }
    }

    // The Scenario Defender (Index, Scenario Attacker/Defender; ruling R17.6).

    /// <summary>A Scenario Defender is a side of the card, sets up wholly or partly on board, and faces a side that enters wholly from off board; a minimal card is exempt.</summary>
    public static IEnumerable<string> DefenderDiagnostics(string defender, IReadOnlyList<string> sides, bool minimal, bool defenderSetsUpOnBoard, bool attackerEntersWholly)
    {
        ArgumentNullException.ThrowIfNull(sides);
        if (!sides.Contains(defender, StringComparer.Ordinal))
        {
            yield return $"card.defender: '{defender}' is not a side of the card";
        }

        if (!(minimal || defenderSetsUpOnBoard))
        {
            yield return "card.defender: a Scenario Defender sets up wholly or partly on board (Index, Scenario Attacker/Defender)";
        }

        if (!(minimal || attackerEntersWholly))
        {
            yield return "card.defender: a Scenario Defender faces a side that enters wholly from offboard (Index, Scenario Attacker/Defender)";
        }
    }

    // The SSRs (rulings R17.2, R17.10, R23.5).

    /// <summary>The SSRs are numbered 1 onward, each with a status, tokens exactly when read, a note when not enforced, its text, cited rules, and HIP tokens that name a side.</summary>
    public static IEnumerable<string> RuleDiagnostics(IReadOnlyList<CardRuleFacts> rules, IReadOnlyList<string> statuses, IReadOnlyList<string> citable)
    {
        ArgumentNullException.ThrowIfNull(rules);
        ArgumentNullException.ThrowIfNull(statuses);
        if (!rules.Select(rule => rule.Number).SequenceEqual(Enumerable.Range(1, rules.Count)))
        {
            yield return "card.ssr: the SSRs are numbered 1 onward";
        }

        foreach (var rule in rules)
        {
            if (!statuses.Contains(rule.Status, StringComparer.Ordinal))
            {
                yield return $"card.ssr: SSR {rule.Number} has no status {string.Join(", ", statuses)}";
            }

            if (!(rule.Status == "token" ? rule.Tokens > 0 : rule.Tokens == 0))
            {
                yield return $"card.ssr: SSR {rule.Number} has tokens exactly when the game reads it";
            }

            if (!(rule.Status != "not-enforced" || !string.IsNullOrWhiteSpace(rule.Note)))
            {
                yield return $"card.ssr: SSR {rule.Number} is not enforced and says why";
            }

            if (string.IsNullOrWhiteSpace(rule.Text))
            {
                yield return $"card.ssr: SSR {rule.Number} has its text";
            }

            foreach (var diagnostic in CitedDiagnostics(rule.Rules, $"SSR {rule.Number}", citable))
            {
                yield return diagnostic;
            }

            // Ruling R23.5 (referee, pass 23): a HIP token names a side of the card.
            foreach (var (token, named) in rule.HipTokens)
            {
                if (named is null)
                {
                    yield return $"card.ssr: SSR {rule.Number}'s '{token}' names no side of the card";
                }
            }
        }
    }

    /// <summary>Ruling R17.2: a cited rule is a compared fragment of the registry, or a ruling.</summary>
    public static IEnumerable<string> CitedDiagnostics(IReadOnlyList<string> rules, string where, IReadOnlyList<string> citable)
    {
        ArgumentNullException.ThrowIfNull(rules);
        ArgumentNullException.ThrowIfNull(citable);
        foreach (var rule in rules)
        {
            if (!(citable.Contains(rule, StringComparer.Ordinal) || RulingPattern.IsMatch(rule)))
            {
                yield return $"card.rules: {where} cites '{rule}', which is neither a compared rule fragment nor a ruling (ruling R17.2)";
            }
        }
    }

    // The Victory Conditions (A26, A26.3; rulings R17.11, R21.3, R22.3).

    /// <summary>
    /// The Victory Conditions have a kind, their text, and the rules they rest on; a minimal card names no outcomes; structured outcomes name a side or a draw
    /// and complete conditions over the card's buildings and hexes. <paramref name="buildingHexes"/> gives a named building's hex count, null for none;
    /// <paramref name="hexOnCard"/> whether a named hex parses and lies on a board of the card.
    /// </summary>
    public static IEnumerable<string> VictoryDiagnostics(string kind, string text, IReadOnlyList<string> rules, bool minimal, IReadOnlyList<string> kinds, IReadOnlyList<string> citable,
        string? otherwise, IReadOnlyList<(string Winner, IReadOnlyList<CardConditionFacts>? Any)>? outcomes, IReadOnlyList<string> sides, IReadOnlyList<string> edges,
        Func<string, int?> buildingHexes, Func<string, bool> hexOnCard)
    {
        ArgumentNullException.ThrowIfNull(rules);
        ArgumentNullException.ThrowIfNull(kinds);
        ArgumentNullException.ThrowIfNull(sides);
        ArgumentNullException.ThrowIfNull(edges);
        ArgumentNullException.ThrowIfNull(buildingHexes);
        ArgumentNullException.ThrowIfNull(hexOnCard);
        if (!(kinds.Contains(kind, StringComparer.Ordinal) && !string.IsNullOrWhiteSpace(text) && (rules.Count > 0 || minimal)))
        {
            yield return "card.victory: the Victory Conditions have a kind, their text, and the A26 rules they rest on";
        }

        foreach (var diagnostic in CitedDiagnostics(rules, "the Victory Conditions", citable))
        {
            yield return diagnostic;
        }

        if (!(!minimal || outcomes is null))
        {
            yield return "card.victory: a minimal card names no outcomes; the players judge the result (ruling R22.3)";
        }

        if (outcomes is null)
        {
            yield break;
        }

        bool SideOrDraw(string? winner) => winner == "draw" || sides.Contains(winner, StringComparer.Ordinal);
        if (!(outcomes.Count > 0 && SideOrDraw(otherwise)))
        {
            yield return "card.victory: structured Victory Conditions list their outcomes and the result when none holds at game end, a side or a draw (A26.3; ruling R21.3)";
        }

        foreach (var (winner, any) in outcomes)
        {
            if (!(SideOrDraw(winner) && any is { Count: > 0 }))
            {
                yield return "card.victory: an outcome names a side of the card or a draw, and its conditions (ruling R21.3)";
            }

            foreach (var condition in any ?? [])
            {
                if (!ConditionValid(condition, sides, edges, buildingHexes, hexOnCard))
                {
                    yield return $"card.victory: the '{condition.Type}' condition of {condition.Side} is not complete, or names a building or hex the card lacks (ruling R21.3)";
                }
            }
        }
    }

    /// <summary>Ruling R21.3: a condition names a side of the card and the fields its type needs, over buildings and hexes the card has.</summary>
    public static bool ConditionValid(CardConditionFacts condition, IReadOnlyList<string> sides, IReadOnlyList<string> edges, Func<string, int?> buildingHexes, Func<string, bool> hexOnCard)
    {
        ArgumentNullException.ThrowIfNull(condition);
        ArgumentNullException.ThrowIfNull(sides);
        ArgumentNullException.ThrowIfNull(edges);
        ArgumentNullException.ThrowIfNull(buildingHexes);
        ArgumentNullException.ThrowIfNull(hexOnCard);
        bool Buildings(IReadOnlyList<string>? ids) => ids is { Count: > 0 } && ids.All(id => buildingHexes(id) is not null);
        return sides.Contains(condition.Side, StringComparer.Ordinal) && condition.Type switch
        {
            "control-margin" => Buildings(condition.Buildings) && Buildings(condition.Versus) && condition.Margin is >= 1,
            "control-count" => condition.Building is { } building && buildingHexes(building) is { } hexes && condition.AtLeast is { } least && least >= 1 && least <= hexes,
            "squad-ratio" => condition.Ratio is > 0,
            "sole-unbroken" => condition.Building is { } only && buildingHexes(only) is not null,
            "exit-vp" => condition.AtLeast is >= 1 && edges.Contains(condition.Edge ?? string.Empty, StringComparer.Ordinal)
                && condition.Near is { Count: > 0 } near && near.All(hexOnCard),
            "cvp" => condition.AtLeast is >= 1,
            _ => false,
        };
    }

    // The setup's legality and fill (A2.9, A5.1, A5.5, A12.3, A12.11, A12.12, A12.34, A25.2, A26.4, C10; rulings R19.1 to R19.6, R20.4, R20.5, R20.7, R23.5, R25.4, R26.2, R26.3, R26.5, R31.4).

    /// <summary>
    /// The setup's state and why it is refused (rulings R19.1 to R19.6): each group's standing, the order being set up now, and the reasons in the order the
    /// checks give them. <paramref name="terrain"/> gives a Location's terrain key by the Location's text, <paramref name="enterable"/> whether Infantry could
    /// enter that terrain in play, and <paramref name="halfSquadOf"/> a squad definition's HS.
    /// </summary>
    public static SetupVerdict Check(SetupCardFacts card, IReadOnlyList<SetupCounterFacts> counters, Func<string, string?> terrain, Func<string?, bool> enterable,
        Func<string, string?> halfSquadOf, int? month, string? balance)
    {
        ArgumentNullException.ThrowIfNull(card);
        ArgumentNullException.ThrowIfNull(counters);
        ArgumentNullException.ThrowIfNull(terrain);
        ArgumentNullException.ThrowIfNull(enterable);
        ArgumentNullException.ThrowIfNull(halfSquadOf);
        var reasons = new List<string>();
        var groups = new List<SetupGroupVerdict>();
        var placed = counters.Where(counter => counter.Kind != "asl:sniper").ToArray();

        // R20.7 (table player, pass 20): a card's OB sets up in Good Order.
        foreach (var counter in placed.Where(counter => counter.Broken))
        {
            reasons.Add($"play.setup-broken: {counter.Id} sets up broken; a card's OB sets up in Good Order (ruling R20.7)");
        }

        // R19.1: every counter of a game from a card belongs to an OB group of its side.
        foreach (var counter in placed.Where(counter => counter.Group is null))
        {
            reasons.Add(counter.Equipment
                ? $"play.setup-group: {counter.Id} sets up possessed by a unit of its OB group (ruling R19.1)"
                : $"play.setup-group: {counter.Id} belongs to no OB group of {counter.Side} (ruling R19.1)");
        }

        foreach (var side in card.Sides)
        {
            // A2.9: up to 10% (FRU) of the squads that set up on board may be Deployed before setup; the squads counted are those set up on
            // board, whole or Deployed (referee, pass 19).
            var sideSquads = placed.Count(counter => counter.Side == side.Side && counter.At is not null && counter.Kind == "asl:squad");
            var deployedSide = 0;

            // A2.9 (ruling R20.5): and up to 10% (FRU) of the squads that enter in a given turn.
            var entering = new Dictionary<int, (int Squads, int Deployed)>();

            // A26.4 (ruling R20.4): the counters the side's Balance adds, set up with any of its groups.
            var pool = side.Side == balance ? side.BalanceUnits.GroupBy(unit => unit.Definition, StringComparer.Ordinal)
                .ToDictionary(item => item.Key, item => item.Sum(unit => unit.Count), StringComparer.Ordinal) : new Dictionary<string, int>(StringComparer.Ordinal);
            foreach (var group in side.Groups)
            {
                var id = group.Id;
                var mine = placed.Where(counter => counter.Side == side.Side && counter.Group == id).ToArray();
                var setsUp = group.Areas.Any(area => area.Kind != "entry");
                var (remaining, groupReasons, deployed, offBoard, enteringHere) = Fill(card, side, group, id, mine, halfSquadOf, pool);
                deployedSide += deployed;
                foreach (var (turn, count) in enteringHere)
                {
                    var sum = entering.GetValueOrDefault(turn);
                    entering[turn] = (sum.Squads + count.Squads, sum.Deployed + count.Deployed);
                }

                reasons.AddRange(groupReasons);

                // R19.3: each counter within one of its group's setup areas; R20.6: and within the playable area.
                foreach (var counter in mine.Where(counter => counter.At is not null))
                {
                    if (!group.Areas.Any(area => area.Kind != "entry" && In(card, area, counter.At!)))
                    {
                        reasons.Add($"play.setup-area: {counter.Id} sets up at {counter.At}, outside the setup areas of {group.Name} (ruling R19.3)");
                    }
                    else if (!Playable(card, counter.At!))
                    {
                        reasons.Add($"play.setup-playable: {counter.Id} sets up at {counter.At}, outside the playable area: {card.PlayableText} (A2.1; ruling R20.6)");
                    }
                }

                // R20.5: off board a counter is neither "?" nor hidden.
                foreach (var counter in mine.Where(counter => counter.OffBoard && (counter.Concealed || counter.Hidden || counter.Dummy)))
                {
                    reasons.Add($"play.setup-offboard: {counter.Id} waits off board to enter, neither under \"?\" nor hidden (A2.51; ruling R20.5)");
                }

                // R19.5 (A12.11, A12.12): the group's OB "?" are its Dummies plus each Location of its concealed real units.
                var dummies = mine.Count(counter => counter.Dummy);
                var concealedStacks = mine.Where(counter => counter.Concealed && !counter.NonOb && !counter.Dummy && !counter.Equipment && counter.At is not null)
                    .Select(counter => counter.At!.Text).Distinct(StringComparer.Ordinal).Count();
                var allotment = group.Dummies ?? 0;
                if (dummies + concealedStacks > allotment)
                {
                    reasons.Add($"play.setup-concealment: {group.Name} uses {dummies + concealedStacks} \"?\" at setup and has {allotment} (A12.12; ruling R19.5)");
                }

                foreach (var counter in mine.Where(counter => ((counter.Concealed && !counter.NonOb) || counter.Dummy) && counter.At is not null))
                {
                    var key = terrain(counter.At!.Text);
                    var area = group.Areas.FirstOrDefault(item => item.Kind != "entry" && In(card, item, counter.At!));
                    if (key is null || !ScenarioA1Definitions.IsConcealmentTerrain(key, month) || area?.Concealed == false)
                    {
                        reasons.Add($"play.setup-concealment: {counter.Id} sets up under \"?\" at {counter.At}, which is not Concealment Terrain"
                            + (area?.Concealed == false ? " or the SSRs forbid \"?\" there" : string.Empty) + " (A12.12; ruling R19.5)");
                    }
                }

                // R19.4: an area whose SSR fixes its counters takes exactly that many, none a "?", with at least the MMC it names.
                var complete = setsUp && remaining.Count == 0;
                var limitedComplete = setsUp;
                foreach (var area in group.Areas.Where(area => area.Counters is not null))
                {
                    var inArea = mine.Where(counter => !counter.Dummy && counter.At is not null && In(card, area, counter.At)).ToArray();
                    var mmc = inArea.Count(counter => counter.Kind is "asl:squad" or "asl:half-squad" or "asl:crew");
                    if (inArea.Length > area.Counters)
                    {
                        reasons.Add($"play.setup-limit: {inArea.Length} counters set up in {area.Id} of {group.Name}, and its SSR allows {area.Counters} (ruling R19.4)");
                    }

                    limitedComplete &= inArea.Length == area.Counters && mmc >= (area.MinMmc ?? 0);
                }

                if (group.Areas.Any(area => area.Counters is not null))
                {
                    complete = limitedComplete;

                    // Table player, pass 19: what a limited area still owes, for the page and the start-of-play refusal.
                    foreach (var area in group.Areas.Where(area => area.Counters is not null))
                    {
                        var inArea = mine.Where(counter => !counter.Dummy && counter.At is not null && In(card, area, counter.At)).ToArray();
                        var mmc = inArea.Count(counter => counter.Kind is "asl:squad" or "asl:half-squad" or "asl:crew");
                        if (inArea.Length < area.Counters)
                        {
                            remaining.Add(new SetupNeedVerdict(id, area.Id, "counters", area.Counters!.Value - inArea.Length));
                        }

                        if (mmc < (area.MinMmc ?? 0))
                        {
                            remaining.Add(new SetupNeedVerdict(id, area.Id, "MMC among them", area.MinMmc!.Value - mmc));
                        }
                    }
                }

                groups.Add(new SetupGroupVerdict(side.Side, id, group.Name, ScenarioA1ResultTables.SetupOrder(group.SetupOrder, side.SetsUpFirst), setsUp, complete, remaining,
                    allotment - dummies - concealedStacks, offBoard, group.Units.Any(line => EntryOf(group, line) is not null)));
            }

            var allowed = (sideSquads + deployedSide + 9) / 10;
            if (deployedSide > allowed)
            {
                reasons.Add($"play.setup-deployment: {side.Side} Deploys {deployedSide} squad(s) before setup, and 10% (FRU) of its {sideSquads + deployedSide} is {allowed} (A2.9; ruling R19.6)");
            }

            // R20.4 (table player, pass 20): the Balance counters are part of the OB, so the side's first group that sets up on board owes those not yet
            // set up, and play does not start without them.
            if (pool.Where(item => item.Value > 0).ToArray() is { Length: > 0 } owed
                && groups.FindIndex(group => group.Side == side.Side && group.SetsUp) is var owing and >= 0)
            {
                groups[owing] = groups[owing] with
                {
                    Complete = false,
                    Remaining = [.. groups[owing].Remaining, .. owed.Select(item => new SetupNeedVerdict(groups[owing].Id, "the Balance", item.Key, item.Value))],
                };
            }

            foreach (var (turn, (squads, deployedThen)) in entering.OrderBy(item => item.Key))
            {
                var enteringAllowed = (squads + deployedThen + 9) / 10;
                if (deployedThen > enteringAllowed)
                {
                    reasons.Add($"play.setup-deployment: {side.Side} Deploys {deployedThen} squad(s) entering on Turn {turn}, and 10% (FRU) of its {squads + deployedThen} is {enteringAllowed} (A2.9; ruling R20.5)");
                }
            }
        }

        // R26.3 (A5.5, C10): a Gun sets up manned by a crew or HS of its group, or in tow, never alone on the map.
        foreach (var gun in placed.Where(counter => counter.Kind == "asl:gun" && counter.At is not null && !counter.Manning && !counter.Towed))
        {
            reasons.Add($"play.setup-gun: {gun.Id} sets up manned by a crew or HS of its OB group, or in tow (A5.5, C10; ruling R26.3)");
        }

        // R26.5 (A12.34): an Emplaced Gun, set up manned and not in tow, and its manning crew may use HIP in Concealment Terrain with no SSR, together.
        var emplacedHidden = placed.Where(counter => counter.Kind == "asl:gun" && counter.Hidden && counter.Manning && !counter.Towed && counter.At is { } at
            && terrain(at.Text) is { } key && ScenarioA1Definitions.IsConcealmentTerrain(key, month)
            && placed.Any(crew => !crew.Equipment && crew.Manning && crew.Hidden && crew.At?.Text == at.Text && crew.Side == counter.Side)).ToArray();
        bool HiddenWithGun(SetupCounterFacts counter) => !counter.Equipment && counter.Manning && emplacedHidden.Any(gun => gun.At?.Text == counter.At?.Text && gun.Side == counter.Side);

        // R23.5 (A12.3): HIP only by an SSR token hip:<side>:<n>, for up to n squad-equivalents of MMC with the SMC set up with them, only in Concealment
        // Terrain, and never a Dummy or a unit also under "?".
        foreach (var side in card.Sides)
        {
            var hidden = placed.Where(counter => counter.Side == side.Side && counter.Hidden && !counter.Equipment && !HiddenWithGun(counter)).ToArray();
            foreach (var counter in placed.Where(counter => counter.Side == side.Side && counter.Hidden && !emplacedHidden.Contains(counter)
                && (counter.Equipment || !ScenarioA1Definitions.HiddenSetupKinds.Contains(counter.Kind))))
            {
                reasons.Add(counter.Kind == "asl:gun"
                    ? $"play.setup-hidden: {counter.Id} sets up hidden only as an Emplaced Gun, manned and not in tow, in Concealment Terrain, with its manning crew hidden too (A12.34; ruling R26.5)"
                    : $"play.setup-hidden: {counter.Id} sets up hidden; HIP is built for Infantry and Emplaced Guns only, and hidden vehicles are not built (A12.3, A12.34; rulings R23.5, R26.5)");
            }

            // A12.34 (ruling R26.5): a hidden crew manning a Gun hides with it.
            foreach (var crew in placed.Where(counter => counter.Side == side.Side && counter.Hidden && !counter.Equipment && counter.Manning && !HiddenWithGun(counter)))
            {
                reasons.Add($"play.setup-hidden: {crew.Id} mans a Gun, so it is hidden only with its Gun, Emplaced in Concealment Terrain (A12.34; ruling R26.5)");
            }

            if (hidden.Length == 0)
            {
                continue;
            }

            if (ScenarioA1Definitions.HipAllowance(card.Tokens, side.Side) is not { } allowance)
            {
                reasons.Add($"play.setup-hidden: no SSR gives {side.Side} HIP (A12.3; an SSR hip:{side.Side}:n; ruling R23.5)");
                continue;
            }

            var used = hidden.Count(counter => counter.Kind == "asl:squad") + (hidden.Count(counter => counter.Kind is "asl:half-squad" or "asl:crew") / 2m);
            if (used > allowance)
            {
                reasons.Add($"play.setup-hidden: {side.Side} sets up {used:0.#} squad-equivalents hidden, and its SSR allows {allowance:0.#} (A12.3; ruling R23.5)");
            }

            foreach (var counter in hidden)
            {
                if (counter.Dummy || counter.Concealed)
                {
                    reasons.Add($"play.setup-hidden: {counter.Id} is hidden or under \"?\", not both, and a Dummy is never hidden (A12.3; ruling R23.5)");
                }
                else if (counter.At is { } at && (terrain(at.Text) is not { } key || !ScenarioA1Definitions.IsConcealmentTerrain(key, month)))
                {
                    reasons.Add($"play.setup-hidden: {counter.Id} sets up hidden at {at}, which is not Concealment Terrain (A12.3; ruling R23.5)");
                }
                else if (ScenarioA1Definitions.SmcKinds.Contains(counter.Kind) && !hidden.Any(other => other.At?.Text == counter.At?.Text && !ScenarioA1Definitions.SmcKinds.Contains(other.Kind) && !other.Dummy))
                {
                    reasons.Add($"play.setup-hidden: {counter.Id} is hidden only with a hidden MMC of its Location (A12.3; ruling R23.5)");
                }
            }
        }

        // R19.3 (A2.9, A5.1): never where Infantry could not enter, never overstacked.
        foreach (var counter in placed.Where(counter => counter.At is not null && !counter.Equipment))
        {
            if (!enterable(terrain(counter.At!.Text)))
            {
                reasons.Add($"play.setup-terrain: {counter.Id} sets up at {counter.At}, terrain it could not enter in play (A2.9; ruling R19.3)");
            }
        }

        // Ruling R26.2: Passengers are not stacked in their vehicle's Location.
        foreach (var stack in placed.Where(counter => counter.At is not null && !counter.Equipment && !counter.Dummy && counter.Aboard is null).GroupBy(counter => (counter.Side, counter.At!.Text)))
        {
            var smc = stack.Count(counter => ScenarioA1Definitions.SmcKinds.Contains(counter.Kind));
            // A5.5 (referee, pass 19): four SMC count nothing; beyond that, five SMC equal a HS; a crew or HS manning a Gun counts as a squad (ruling R26.3).
            var squads = SetupStackSquadEquivalents(stack, smc);
            if (squads > 3)
            {
                reasons.Add($"play.setup-stacking: {stack.Key.Side} sets up {squads:0.#} squad-equivalents at {stack.Key.Text}, and the limit is 3 (A5.1; ruling R19.3)");
            }
        }

        // R19.2: a proposal places only groups of the order now setting up; the order advances when all its groups are complete. Counters set up off
        // board wait outside the order (A2.51; ruling R20.5).
        var current = groups.Where(group => group.SetsUp && !group.Complete).Select(group => (int?)group.Order).Min();
        foreach (var counter in placed.Where(counter => counter.New && counter.Group is not null && !counter.OffBoard))
        {
            var group = groups.FirstOrDefault(item => item.Side == counter.Side && item.Id == counter.Group);
            var earlier = groups.Where(item => item.SetsUp && group is not null && item.Order < group.Order && !item.Complete).ToArray();
            if (group is not null && earlier.Length > 0)
            {
                reasons.Add($"play.setup-order: {counter.Id} of {group.Name} may not set up until {earlier[0].Name} has finished (A2.9; ruling R19.2)");
                break;
            }

            // A12.12 (table player, pass 19): once a later group has set up, an earlier one adds nothing, "?" included.
            var later = groups.FirstOrDefault(item => group is not null && item.Order > group.Order
                && placed.Any(other => !other.New && !other.OffBoard && other.Side == item.Side && other.Group == item.Id));
            if (group is not null && later is not null)
            {
                reasons.Add($"play.setup-order: {counter.Id} of {group.Name} may not set up once {later.Name} has begun setting up (A2.9, A12.12; ruling R19.2)");
                break;
            }
        }

        return new SetupVerdict(groups, current, [.. reasons.Distinct(StringComparer.Ordinal)]);
    }

    /// <summary>
    /// A5.5 (referee, pass 19): a setup stack's squad-equivalents: a squad one, a crew or HS manning a Gun one (ruling R26.3), other HS and crews a half,
    /// SMC nothing up to four and a tenth each beyond. The design's section 12 keeps this count apart from the entering stack's (<see cref="EntryStackOverstacked"/>).
    /// </summary>
    public static decimal SetupStackSquadEquivalents(IEnumerable<SetupCounterFacts> stack, int smc)
    {
        ArgumentNullException.ThrowIfNull(stack);
        var counters = stack.ToArray();
        return counters.Count(counter => counter.Kind == "asl:squad" || (counter.Kind is "asl:half-squad" or "asl:crew" && counter.Manning))
            + (counters.Count(counter => counter.Kind is "asl:half-squad" or "asl:crew" && !counter.Manning) / 2m)
            + (smc > 4 ? smc / 10m : 0m);
    }

    /// <summary>A5.1, A2.51 (referee, pass 25): an entering stack holds at most three squad-equivalents (a HS or crew a half) and four SMC.</summary>
    public static bool EntryStackOverstacked(double squadEquivalents, int smc) => squadEquivalents > 3 || smc > 4;

    // The planner's reading of a setup (rulings R19.1 to R19.6, R20.6, R22.3, R23.3, R26.2, R26.5).

    /// <summary>A2.51 (ruling R20.5): a counter waits off board when its position is off the map or inside a carrier and it has no Location.</summary>
    public static bool SetupCounterOffBoard(bool offMapOrContained, bool located) => offMapOrContained && !located;

    /// <summary>A12.34 (ruling R26.5): a manned Gun's hidden status is its own; a SW is hidden only with its holder, never of itself.</summary>
    public static bool EquipmentHiddenOfItsOwn(bool hidden, bool possessed) => hidden && !possessed;

    /// <summary>A SW belongs to its holder's group, at its holder's Location; equipment on its own belongs to no group (referee, pass 19).</summary>
    public static string EquipmentSetupSide(string? holderSide, string? ownSide) => holderSide ?? ownSide ?? string.Empty;

    /// <summary>A2.9 (ruling R19.3): Infantry could enter a Location in play when it is marsh or its terrain has an entry cost.</summary>
    public static bool InfantryCouldEnter(string? terrain, Func<string, bool> hasEntryCost)
    {
        ArgumentNullException.ThrowIfNull(hasEntryCost);
        return terrain is "marsh" || (terrain is not null && hasEntryCost(terrain));
    }

    /// <summary>A2.1 (ruling R20.6): why a Location is refused as outside the card's playable area; null when it is inside.</summary>
    public static string? PlayableBar(bool outside, string at, string? areaText) =>
        outside ? $"play.playable: {at} is outside the playable area: {areaText} (A2.1; ruling R20.6)" : null;

    /// <summary>
    /// Why a game from a card may not start play yet (ruling R19.2): nothing once play has begun; the card gone or changed (its text is Play's, since it reads
    /// the library); a group that sets up on board not finished; a group still owing counters off board (A2.51; ruling R20.5); null when it may.
    /// </summary>
    public static string? SetupIncompleteBar(bool pastSetup, Func<bool> cardGoneOrChanged, Func<string> cardGoneText, Func<IReadOnlyList<SetupGroupVerdict>?> setup)
    {
        ArgumentNullException.ThrowIfNull(cardGoneOrChanged);
        ArgumentNullException.ThrowIfNull(cardGoneText);
        ArgumentNullException.ThrowIfNull(setup);
        if (pastSetup)
        {
            return null;
        }

        // Referee, pass 19: a card changed or gone since the game started cannot say whether the setup is done; the library is read only here.
        if (cardGoneOrChanged())
        {
            return cardGoneText();
        }

        if (setup() is not { } groups)
        {
            return null;
        }

        return groups.FirstOrDefault(group => group.SetsUp && !group.Complete) is { } open
            ? $"play.setup-incomplete: {open.Name} ({open.Side}) has not finished setting up"
                + (open.Remaining.Count > 0 ? $": {string.Join(", ", open.Remaining.Select(need => $"{need.Count} {need.Definition}{(need.Area is { } area ? $" in {area}" : string.Empty)}"))} left" : string.Empty)
                + " (A2.9; ruling R19.2)"
            : groups.FirstOrDefault(group => group.OffBoard.Count > 0) is { } waiting
            ? $"play.setup-incomplete: {waiting.Name} ({waiting.Side}) still sets up off board to enter: "
                + $"{string.Join(", ", waiting.OffBoard.Select(need => $"{need.Count} {need.Definition}"))} (A2.51; ruling R20.5)"
            : null;
    }

    /// <summary>
    /// The OB groups a perspective may not see at all now (A12.12; ruling R23.3): during the setup of a game from a card, the enemy groups of the order
    /// setting up now; none for the adjudicator, once play has started, or when no order is open. <paramref name="setup"/> is read only when it may matter.
    /// </summary>
    public static IEnumerable<string> OutOfSight(bool adjudicator, bool setupClosed, string viewer, Func<(int? CurrentOrder, IReadOnlyList<SetupGroupVerdict> Groups)?> setup)
    {
        ArgumentNullException.ThrowIfNull(setup);
        if (adjudicator || setupClosed || setup() is not { CurrentOrder: { } now } report)
        {
            return [];
        }

        return report.Groups.Where(group => group.SetsUp && group.Order == now && group.Side != viewer).Select(group => group.Id);
    }

    private static bool In(SetupCardFacts card, SetupAreaFacts area, SetupLocationFacts at) =>
        Within(area.Kind, area.Hexes, area.Board, area.From, area.To, card.BoardCount, card.FirstBoard, at.Board, at.Hex, at.RowNumber);

    private static bool Playable(SetupCardFacts card, SetupLocationFacts at) =>
        !card.PlayableEnforced || Playable(true, card.PlayableFrom, card.PlayableTo, card.PlayableBoard, card.BoardCount, card.FirstBoard, at.Board, at.Hex);

    /// <summary>The entry area an OB line enters by (ruling R20.5), or null when the line sets up on board.</summary>
    public static SetupAreaFacts? EntryOf(SetupGroupFacts group, SetupLineFacts line)
    {
        ArgumentNullException.ThrowIfNull(group);
        ArgumentNullException.ThrowIfNull(line);
        var entries = group.Areas.Where(area => area.Kind == "entry").OrderBy(area => area.Turn).ToArray();
        return ScenarioA1ResultTables.EntryAreaIndex([.. entries.Select(area => area.Id)], line.Area, group.Areas.Any(area => area.Counters is not null),
            group.Areas.All(area => area.Kind == "entry")) is { } index ? entries[index] : null;
    }

    /// <summary>
    /// Matches a group's placed counters to its OB lines (R19.1): each counter fills a line of its definition whose area holds it (or any area),
    /// a Deployed squad's two HS fill one squad line (A2.9). What is left, why a counter fits no line, how many squads were Deployed, what is owed
    /// off board, and the squads entering by turn.
    /// </summary>
    private static (List<SetupNeedVerdict> Remaining, List<string> Reasons, int Deployed, List<SetupNeedVerdict> OffBoard, Dictionary<int, (int Squads, int Deployed)> Entering) Fill(
        SetupCardFacts card, SetupSideFacts owner, SetupGroupFacts group, string id, IReadOnlyList<SetupCounterFacts> mine, Func<string, string?> halfSquadOf, Dictionary<string, int> pool)
    {
        var reasons = new List<string>();
        var lines = group.Units.Select(unit => (Unit: unit, Left: unit.Count)).ToArray();
        var halves = new Dictionary<(string Definition, string? Area, bool OffBoard), int>();
        var enteringSquads = new Dictionary<int, int>();
        var deploys = ScenarioA1Definitions.MayDeploy(owner.Nation ?? owner.Side);
        foreach (var counter in mine.Where(counter => !counter.Dummy))
        {
            // Referee, pass 19: a counter with no Location fills no line, unless it waits off board to enter (ruling R20.5).
            if (counter.At is null && !counter.OffBoard)
            {
                reasons.Add($"play.setup-area: {counter.Id} of {group.Name} is not set up on the map (A2.9; ruling R19.3)");
                continue;
            }

            // R25.4: an off-board counter may name one of its group's entry areas.
            if (counter.OffBoard && counter.Entry is { } named && !group.Areas.Any(area => area.Kind == "entry" && area.Id == named))
            {
                reasons.Add($"play.setup-entry: {counter.Id} of {group.Name} names the entry area '{named}', which its group does not have (A2.5; ruling R25.4)");
                continue;
            }

            // R20.5: an off-board counter fills a line that enters, by the entry it named (ruling R25.4); an on-board one a line that sets up, in its area.
            bool Fits(SetupLineFacts unit) => counter.OffBoard ? EntryOf(group, unit) is not null && (counter.Entry is null || unit.Area is null || unit.Area == counter.Entry)
                : unit.Area is null ? EntryOf(group, unit) is null || group.Areas.Any(area => area.Counters is not null)
                : group.Areas.FirstOrDefault(area => area.Id == unit.Area) is { Kind: not "entry" } area && In(card, area, counter.At!);
            // A counter outside all its group's areas takes only the area reason (table player, pass 19).
            if (!counter.OffBoard && !group.Areas.Any(area => area.Kind != "entry" && In(card, area, counter.At!)))
            {
                continue;
            }

            if (counter.OffBoard && counter.Kind == "asl:squad" && lines.FirstOrDefault(item => item.Unit.Definition == counter.Definition && Fits(item.Unit)).Unit is { } entryLine
                && (group.Areas.FirstOrDefault(area => area.Kind == "entry" && area.Id == counter.Entry) ?? EntryOf(group, entryLine))?.Turn is { } entryTurn)
            {
                enteringSquads[entryTurn] = enteringSquads.GetValueOrDefault(entryTurn) + 1;
            }

            var line = Array.FindIndex(lines, item => item.Left > 0 && item.Unit.Definition == counter.Definition && Fits(item.Unit));
            if (line >= 0)
            {
                lines[line].Left--;
                continue;
            }

            // A2.9, A25.2 (ruling R31.4; referee, pass 31): squads set up Deployed only "if the nationality is capable of Deployment", and Russian
            // squads may not Deploy. A HS the OB itself lists was placed by the line above; this is a squad's HS.
            if (!deploys && Array.FindIndex(lines, item => halfSquadOf(item.Unit.Definition) == counter.Definition && Fits(item.Unit)) >= 0)
            {
                reasons.Add($"play.setup-deployment: {counter.Id} is a HS of a squad of {group.Name}, and Russian squads may not Deploy, so they do not set up Deployed (A2.9, A25.2; ruling R31.4)");
                continue;
            }

            // A2.9: a HS of a squad in the group's OB, set up as half of a Deployed squad: the second HS pairs with the first.
            var pairing = Array.FindIndex(lines, item => halfSquadOf(item.Unit.Definition) == counter.Definition && Fits(item.Unit)
                && halves.GetValueOrDefault((item.Unit.Definition, item.Unit.Area, counter.OffBoard)) % 2 == 1);
            if (pairing >= 0)
            {
                var pairKey = (lines[pairing].Unit.Definition, lines[pairing].Unit.Area, counter.OffBoard);
                halves[pairKey] = halves[pairKey] + 1;
                continue;
            }

            var parent = Array.FindIndex(lines, item => item.Left > 0 && halfSquadOf(item.Unit.Definition) == counter.Definition && Fits(item.Unit));
            if (parent >= 0)
            {
                var key = (lines[parent].Unit.Definition, lines[parent].Unit.Area, counter.OffBoard);
                halves[key] = halves.GetValueOrDefault(key) + 1;
                if (halves[key] % 2 == 1)
                {
                    lines[parent].Left--;
                }

                continue;
            }

            // A26.4 (ruling R20.4): a counter the side's Balance adds, with any of its groups.
            if (counter.Definition is { } added && pool.GetValueOrDefault(added) > 0 && (!counter.OffBoard || group.Areas.Any(area => area.Kind == "entry")))
            {
                pool[added]--;
                continue;
            }

            reasons.Add($"play.setup-pool: {counter.Id} ({counter.Definition}) is not left in the OB of {group.Name}"
                + (counter.At is not null ? $" at {counter.At}" : string.Empty) + " (ruling R19.1)");
        }

        foreach (var ((definition, _, _), count) in halves.Where(item => item.Value % 2 == 1))
        {
            reasons.Add($"play.setup-deployment: a Deployed {definition} of {group.Name} sets up both its HS (A2.9; ruling R19.6)");
        }

        // What a group with only setup areas must still place; a group whose area fixes its counters is judged by that area (R19.4); what enters is
        // owed off board (R20.5).
        var limited = group.Areas.Any(area => area.Counters is not null);
        var remaining = limited || group.Areas.All(area => area.Kind == "entry") ? []
            : lines.Where(item => item.Left > 0 && EntryOf(group, item.Unit) is null).Select(item => new SetupNeedVerdict(id, item.Unit.Area, item.Unit.Definition, item.Left)).ToList();
        var offBoard = lines.Where(item => item.Left > 0 && EntryOf(group, item.Unit) is not null)
            .Select(item => new SetupNeedVerdict(id, EntryOf(group, item.Unit)!.Id, item.Unit.Definition, item.Left)).ToList();

        // A2.9 (ruling R20.5): the squads entering in each turn, whole or Deployed, and how many of them were Deployed.
        var entering = new Dictionary<int, (int Squads, int Deployed)>();
        foreach (var (turn, squads) in enteringSquads)
        {
            entering[turn] = (squads, 0);
        }

        foreach (var ((definition, area, offBoardHalf), count) in halves.Where(item => item.Key.OffBoard))
        {
            if (lines.FirstOrDefault(item => item.Unit.Definition == definition && item.Unit.Area == area).Unit is { } squadLine && EntryOf(group, squadLine)?.Turn is { } turn)
            {
                var sum = entering.GetValueOrDefault(turn);
                entering[turn] = (sum.Squads, sum.Deployed + ((count + 1) / 2));
            }
        }

        return (remaining, reasons, halves.Where(item => !item.Key.OffBoard).Sum(item => (item.Value + 1) / 2), offBoard, entering);
    }
}

/// <summary>A Location of a setup counter as facts: its text as the game writes it, its board, its hex name, and the hex's row number.</summary>
public sealed record SetupLocationFacts(string Text, string Board, string Hex, int RowNumber)
{
    public override string ToString() => Text;
}

/// <summary>
/// A counter set up in a game from a card as facts (pass 19, rulings R19.1 to R19.6): its side and OB group, definition and kind, Location, whether it
/// is concealed, hidden, a Dummy, or a SW, whether this proposal places it, whether it waits off board, sets up broken, bears a non-OB "?", the entry it
/// named, the vehicle it rides, and whether it mans a Gun or is a Gun manned or in tow.
/// </summary>
public sealed record SetupCounterFacts(string Id, string Side, string? Group, string? Definition, string Kind, SetupLocationFacts? At, bool Concealed, bool Hidden, bool Dummy,
    bool Equipment, bool New, bool OffBoard, bool Broken, bool NonOb, string? Entry, string? Aboard, bool Manning, bool Towed);

/// <summary>A setup area of a card as the setup reads it (rulings R17.8, R19.4): its id, kind, hexes, board, hex numbers, entry turn, and SSR counts.</summary>
public sealed record SetupAreaFacts(string Id, string Kind, IReadOnlyList<string>? Hexes, string? Board, int? From, int? To, int? Turn, int? Counters, int? MinMmc, bool? Concealed);

/// <summary>An OB line as the setup reads it: its definition, count, and area.</summary>
public sealed record SetupLineFacts(string Definition, int Count, string? Area);

/// <summary>An OB group as the setup reads it: its game id, name, setup order, "?" allotment, areas, and lines.</summary>
public sealed record SetupGroupFacts(string Id, string Name, int? SetupOrder, int? Dummies, IReadOnlyList<SetupAreaFacts> Areas, IReadOnlyList<SetupLineFacts> Units);

/// <summary>A side as the setup reads it: its id, nation, whether it sets up first, its Balance counters, and its groups.</summary>
public sealed record SetupSideFacts(string Side, string? Nation, bool SetsUpFirst, IReadOnlyList<SetupLineFacts> BalanceUnits, IReadOnlyList<SetupGroupFacts> Groups);

/// <summary>A card as the setup reads it: its sides, its boards (the count and the only one), its enforced playable area, and its SSR tokens.</summary>
public sealed record SetupCardFacts(IReadOnlyList<SetupSideFacts> Sides, int BoardCount, string? FirstBoard, bool PlayableEnforced, string? PlayableFrom, string? PlayableTo,
    string? PlayableBoard, string? PlayableText, IReadOnlyList<string> Tokens);

/// <summary>An OB line still to set up: its group, the area the card names for it (null for any of the group's areas), its definition, and how many.</summary>
public sealed record SetupNeedVerdict(string Group, string? Area, string Definition, int Count);

/// <summary>Where an OB group stands in the setup (rulings R19.2, R20.5): its side, id, name, order, whether it sets up on board, is done, what it still owes on and off board, its "?" left, and whether it enters.</summary>
public sealed record SetupGroupVerdict(string Side, string Id, string Name, int Order, bool SetsUp, bool Complete, IReadOnlyList<SetupNeedVerdict> Remaining, int DummiesLeft,
    IReadOnlyList<SetupNeedVerdict> OffBoard, bool Enters);

/// <summary>The setup's state (ruling R19.2): each group's, the order being set up now, and why the counters as placed are refused.</summary>
public sealed record SetupVerdict(IReadOnlyList<SetupGroupVerdict> Groups, int? CurrentOrder, IReadOnlyList<string> Reasons);
