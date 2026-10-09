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
}
