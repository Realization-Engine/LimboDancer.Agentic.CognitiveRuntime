using LimboDancer.Domains.Asl.Maps.Coordinates;
using LimboDancer.Domains.Asl.Rules;

namespace LimboDancer.Domains.Asl.Play;

/// <summary>
/// A counter set up in a game from a card (pass 19, rulings R19.1 to R19.6): its side and OB group (a SW takes its holder's), its definition and
/// kind, its Location (a SW its holder's), whether it is concealed or hidden, a Dummy, or a SW, and whether this setup proposal places it.
/// </summary>
public sealed record SetupCounter(string Id, string Side, string? Group, string? Definition, string Kind, BoardLocation? At, bool Concealed, bool Hidden,
    bool Dummy, bool Equipment, bool New)
{
    /// <summary>Whether the counter waits off board to enter (A2.51; ruling R20.5).</summary>
    public bool OffBoard
    {
        get; init;
    }

    /// <summary>Whether the counter is set up broken, which no card allows (ruling R20.7).</summary>
    public bool Broken
    {
        get; init;
    }

    /// <summary>Whether its "?" is a non-OB one, placed after both sides set up (A12.12; ruling R23.6), which the OB allotment does not count.</summary>
    public bool NonOb
    {
        get; init;
    }

    /// <summary>The entry area an off-board counter's setup named (A2.5; ruling R25.4), or its vehicle's for a Passenger; null when it named none.</summary>
    public string? Entry
    {
        get; init;
    }

    /// <summary>The vehicle a Passenger rides (D6.1; ruling R26.2), which takes it out of its Location's stacking; null on foot.</summary>
    public string? Aboard
    {
        get; init;
    }

    /// <summary>For a crew or HS, whether it mans a Gun, stacking as a squad (A5.5); for a Gun, whether it is manned (ruling R26.3).</summary>
    public bool Manning
    {
        get; init;
    }

    /// <summary>For a Gun, whether it is set up in tow (C10.1; ruling R26.1).</summary>
    public bool Towed
    {
        get; init;
    }
}

/// <summary>An OB line still to set up: its group, the area the card names for it (null for any of the group's areas), its definition, and how many.</summary>
public sealed record SetupNeed(string Group, string? Area, string Definition, int Count);

/// <summary>
/// Where an OB group stands in the setup (ruling R19.2): its side, its place in the order, whether it sets up on board, whether it is done, what it
/// still has to set up, and how many of its "?" are left; and what it still has to set up off board to enter (ruling R20.5).
/// </summary>
public sealed record SetupGroup(string Side, string Id, string Name, int Order, bool SetsUp, bool Complete, IReadOnlyList<SetupNeed> Remaining, int DummiesLeft,
    IReadOnlyList<ScenarioCardSetup> Areas)
{
    /// <summary>The group's counters still to set up off board (A2.51; ruling R20.5): empty when every one waits there.</summary>
    public IReadOnlyList<SetupNeed> OffBoard { get; init; } = [];

    /// <summary>Whether the group has counters that enter (ruling R20.5).</summary>
    public bool Enters
    {
        get; init;
    }
}

/// <summary>The setup's state (ruling R19.2): each group's, the order being set up now, and why the counters as placed are refused (empty when they are not).</summary>
public sealed record SetupReport(IReadOnlyList<SetupGroup> Groups, int? CurrentOrder, IReadOnlyList<string> Reasons)
{
    /// <summary>Every group that sets up on board has finished, and every entering counter waits off board (rulings R19.2, R20.5).</summary>
    public bool Complete => ScenarioA1ResultTables.SetupComplete(Groups.Select(group => (group.SetsUp, group.Complete, group.OffBoard.Count)));
}

/// <summary>
/// The setup of a game from a scenario card (pass 19 of the Scenario Card Games Plan; rulings R19.1 to R19.6): each OB group sets up its own
/// counters, in its own areas, in the card's order (A2.9, A12.12), never overstacked (A5.1) nor where it could not enter in play (A2.9), with
/// its OB "?" only in Concealment Terrain (A12.12), and with up to 10% of a side's squads Deployed (A2.9). A pure function of the card and the
/// counters, so the planner, the Play page, and tests read the same answer.
/// </summary>
public static class ScenarioSetup
{
    /// <summary>The order a group sets up in (R17.12): its own, else 1 for the side that sets up first and 2 for the other.</summary>
    public static int OrderOf(ScenarioCard card, ScenarioCardSide side, ScenarioCardGroup group)
    {
        ArgumentNullException.ThrowIfNull(card);
        ArgumentNullException.ThrowIfNull(side);
        ArgumentNullException.ThrowIfNull(group);
        return ScenarioA1ResultTables.SetupOrder(group.SetupOrder, side.Side == card.Turns.SetsUpFirst);
    }

    /// <summary>
    /// The entry area an OB line enters by (ruling R20.5): the entry area it names, or, when it names none, its group's entry area if the group has
    /// an area whose SSR fixes its counters (Gambit's British: the rest of the group enters); null when the line sets up on board.
    /// </summary>
    public static ScenarioCardSetup? EntryOf(ScenarioCardGroup group, ScenarioCardUnit line)
    {
        ArgumentNullException.ThrowIfNull(group);
        ArgumentNullException.ThrowIfNull(line);
        var entries = group.Areas.Where(area => area.Kind == "entry").OrderBy(area => area.Turn).ToArray();
        return ScenarioA1ResultTables.EntryAreaIndex([.. entries.Select(area => area.Id)], line.Area, group.Areas.Any(area => area.Counters is not null),
            group.Areas.All(area => area.Kind == "entry")) is { } index ? entries[index] : null;
    }

    /// <summary>Whether a Location lies in a setup area of the card (R17.8, R19.3): a building's hexes, or a board's hex numbers.</summary>
    public static bool Within(ScenarioCard card, ScenarioCardSetup area, BoardLocation at)
    {
        ArgumentNullException.ThrowIfNull(card);
        ArgumentNullException.ThrowIfNull(area);
        ArgumentNullException.ThrowIfNull(at);
        return ScenarioA1SetupCalculator.Within(area.Kind, area.Hexes, area.Board, area.From, area.To, card.Boards.Count, card.Boards.Count == 1 ? card.Boards[0].Board : null,
            at.Board.Value, at.Hex.ToString(), at.Hex.RowNumber);
    }

    /// <summary>
    /// The setup's state and why it is refused. <paramref name="terrain"/> gives a Location's terrain key (as the planner reads it), and
    /// <paramref name="enterable"/> whether Infantry could enter that terrain in play; <paramref name="halfSquadOf"/> names a squad's HS.
    /// </summary>
    public static SetupReport Check(ScenarioCard card, IReadOnlyList<SetupCounter> counters, Func<BoardLocation, string?> terrain, Func<string?, bool> enterable,
        Func<string, string?> halfSquadOf, int? month, string? balance = null)
    {
        ArgumentNullException.ThrowIfNull(card);
        ArgumentNullException.ThrowIfNull(counters);
        ArgumentNullException.ThrowIfNull(terrain);
        ArgumentNullException.ThrowIfNull(enterable);
        ArgumentNullException.ThrowIfNull(halfSquadOf);
        var located = counters.Where(counter => counter.At is not null).Select(counter => counter.At!).Distinct().ToDictionary(at => at.ToString(), at => at, StringComparer.Ordinal);
        var facts = new SetupCardFacts(
            [.. card.Sides.Select(side => new SetupSideFacts(side.Side, side.Nation, side.Side == card.Turns.SetsUpFirst,
                [.. (side.BalanceUnits ?? []).Select(unit => new SetupLineFacts(unit.Definition, unit.Count, unit.Area))],
                [.. side.Groups.Select((group, index) => new SetupGroupFacts(ScenarioCards.GroupId(side.Side, index), group.Name, group.SetupOrder, group.Dummies,
                    [.. group.Areas.Select(area => new SetupAreaFacts(area.Id, area.Kind, area.Hexes, area.Board, area.From, area.To, area.Turn, area.Counters, area.MinMmc, area.Concealed))],
                    [.. group.Units.Select(unit => new SetupLineFacts(unit.Definition, unit.Count, unit.Area))]))]))],
            card.Boards.Count, card.Boards.Count == 1 ? card.Boards[0].Board : null,
            card.PlayableArea is { Enforced: true, Hexrows: not null }, card.PlayableArea?.Hexrows?.From, card.PlayableArea?.Hexrows?.To, card.PlayableArea?.Hexrows?.Board,
            card.PlayableArea?.Text, card.Tokens);
        var verdict = ScenarioA1SetupCalculator.Check(facts, [.. counters.Select(counter => new SetupCounterFacts(counter.Id, counter.Side, counter.Group, counter.Definition, counter.Kind,
                counter.At is { } at ? new SetupLocationFacts(at.ToString(), at.Board.Value, at.Hex.ToString(), at.Hex.RowNumber) : null, counter.Concealed, counter.Hidden, counter.Dummy,
                counter.Equipment, counter.New, counter.OffBoard, counter.Broken, counter.NonOb, counter.Entry, counter.Aboard, counter.Manning, counter.Towed))],
            text => terrain(located[text]), enterable, halfSquadOf, month, balance);
        var areas = card.Sides.SelectMany(side => side.Groups.Select((group, index) => (side.Side, Id: ScenarioCards.GroupId(side.Side, index), group.Areas)))
            .ToDictionary(item => (item.Side, item.Id), item => item.Areas);
        return new SetupReport([.. verdict.Groups.Select(group => new SetupGroup(group.Side, group.Id, group.Name, group.Order, group.SetsUp, group.Complete,
            [.. group.Remaining.Select(Need)], group.DummiesLeft, areas[(group.Side, group.Id)]) { OffBoard = [.. group.OffBoard.Select(Need)], Enters = group.Enters })],
            verdict.CurrentOrder, verdict.Reasons);
    }

    private static SetupNeed Need(SetupNeedVerdict need) => new(need.Group, need.Area, need.Definition, need.Count);

    /// <summary>
    /// Whether a nationality's squads may Deploy (A25.2; ruling R31.4): every nationality the game has but the Russian. One list for play and for
    /// setup. A Guard of prisoners (A20.5) and a temporary crew (A21.22) are the rule's exceptions and are not built.
    /// </summary>
    public static bool MayDeploy(string? nationality) => ScenarioA1Definitions.MayDeploy(nationality);

    /// <summary>
    /// The squad-equivalents a side may set up hidden (A12.3; ruling R23.5): the largest n of its SSR tokens <c>hip:&lt;side&gt;:&lt;n&gt;</c>; null when it has
    /// none.
    /// </summary>
    public static decimal? HipAllowance(IEnumerable<string> tokens, string side) => ScenarioA1Definitions.HipAllowance(tokens, side);

    /// <summary>Concealment Terrain for setup (A12.12, as the planner reads it): grain only June to September.</summary>
    public static bool ConcealmentTerrain(string terrain, int? month) => ScenarioA1Definitions.IsConcealmentTerrain(terrain, month);
}
