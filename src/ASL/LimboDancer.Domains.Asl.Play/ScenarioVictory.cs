using System.Globalization;
using LimboDancer.Domains.Asl.Maps.Coordinates;
using LimboDancer.Domains.Asl.Units.State;

namespace LimboDancer.Domains.Asl.Play;

/// <summary>
/// What a side Controls that the Victory Conditions name (ruling R21.1): a building of the card, or a hex of one; null when neither side does. A hex in
/// Melee that a card counts for neither says so (table player, pass 21).
/// </summary>
public sealed record VictoryControl(string Id, string Kind, IReadOnlyList<BoardLocation> Hexes, string? Side)
{
    /// <summary>Whether a hex count of the card treats this hex, in Melee now, as Controlled by neither (ruling R21.3).</summary>
    public bool InMelee
    {
        get; init;
    }
}

/// <summary>A side's standing for the Victory Conditions (rulings R21.2, R21.3): its CVP, Exit VP, and unbroken squad-equivalents.</summary>
public sealed record VictorySide(string Side, int Cvp, int ExitVp, double UnbrokenSquads);

/// <summary>
/// The Victory Conditions read against a game (rulings R21.1 to R21.4): the Control of what they name, each side's standing, the result an immediate
/// condition gives now (null when none holds), and the result the game has at its end.
/// </summary>
public sealed record VictoryReport(IReadOnlyList<VictoryControl> Control, IReadOnlyList<VictorySide> Sides, GameResult? Immediate, GameResult AtEnd);

/// <summary>
/// The Victory Conditions of a card evaluated over a game's history (A26; pass 21 of the Scenario Card Games Plan, rulings R21.1 to R21.4): Control gained
/// at scenario start from the setup areas (A26.11) and during play by armed Good Order Infantry MMC (A26.11, A26.13, A26.14), kept until the enemy gains it;
/// VP (A26.211), CVP (A26.22, A26.222), and Exit VP (A26.23); and the card's outcomes in order, else its Avoidance or draw (A26.3). A pure function of the
/// card and the states, so the planner, the Play page, and tests read the same answer.
/// </summary>
public static class ScenarioVictory
{
    private static readonly string[] Mmc = ["asl:squad", "asl:half-squad", "asl:crew"];

    /// <summary>
    /// Evaluates a card's Victory Conditions over the states of a game, the last being the present. <paramref name="vp"/> gives a unit's VP (A26.211),
    /// and <paramref name="adjacent"/> the hexes adjacent to a Location. <paramref name="ended"/> doubles captured units' VP (A26.222). Null when the card
    /// has no structured Victory Conditions or play has not started. <paramref name="knownTo"/> reads them as that side may know them (A26.15; pass 23,
    /// ruling R23.4): the enemy's concealed and hidden units neither gain nor prevent Control, since their Control need not be declared until game end,
    /// and count for none of its unbroken squad-equivalents.
    /// </summary>
    public static VictoryReport? Evaluate(ScenarioCard card, IReadOnlyList<GameState> states, Func<UnitInstance, int> vp, Func<BoardLocation, IEnumerable<BoardLocation>> adjacent,
        bool ended, string? knownTo = null)
    {
        ArgumentNullException.ThrowIfNull(card);
        ArgumentNullException.ThrowIfNull(states);
        ArgumentNullException.ThrowIfNull(vp);
        ArgumentNullException.ThrowIfNull(adjacent);
        if (card.VictoryConditions.Outcomes is not { } outcomes || card.VictoryConditions.Otherwise is not { } otherwise
            || states.FirstOrDefault(state => state.SetupClosed) is null)
        {
            return null;
        }

        var control = Control(card, states, knownTo);
        var present = states[^1];

        // Table player, pass 21: a hex a hex count treats as neither's while it is in Melee is shown so.
        if (outcomes.SelectMany(outcome => outcome.Any).Any(condition => condition.Type == "control-count" && condition.MeleeUncontrolled == true))
        {
            foreach (var (id, item) in control.Where(pair => pair.Value.Kind == "hex").ToArray())
            {
                control[id] = item with
                {
                    InMelee = InMelee(present, item.Hexes[0])
                };
            }
        }
        var sides = card.Sides.Select(side => side.Side).ToArray();
        var standing = sides.Select(side => new VictorySide(side, Cvp(card, present, side, vp, adjacent, ended), ExitVp(card, present, side, vp, adjacent),
            UnbrokenSquads(present, side, knownTo))).ToArray();

        GameResult? Decide(bool immediateOnly)
        {
            foreach (var outcome in outcomes.Where(outcome => !immediateOnly || outcome.Immediate))
            {
                foreach (var condition in outcome.Any)
                {
                    if (Holds(card, condition, control, standing, present) is { } reason)
                    {
                        return new GameResult(outcome.Winner == "draw" ? null : outcome.Winner, reason, Facts(control, standing));
                    }
                }
            }

            return null;
        }

        var atEnd = Decide(false) ?? new GameResult(otherwise == "draw" ? null : otherwise,
            otherwise == "draw" ? "no Victory Condition holds" : "no Victory Condition of the other side holds (A26.3)", Facts(control, standing));
        return new VictoryReport([.. control.Values], standing, Decide(true), atEnd);
    }

    /// <summary>
    /// The Control of each building the conditions name and of each hex of a building a hex count names (rulings R21.1): at scenario start the side whose
    /// setup area holds it alone (A26.11), then, state by state, the side whose armed Good Order Infantry MMC occupies it with no armed enemy ground unit
    /// there (A26.11, ground level for a hex, A26.13, any level for a building, A26.14), not in Bypass; Dummies neither gain nor prevent it (A26.15).
    /// </summary>
    private static Dictionary<string, VictoryControl> Control(ScenarioCard card, IReadOnlyList<GameState> states, string? knownTo)
    {
        var tracked = new Dictionary<string, VictoryControl>(StringComparer.Ordinal);
        var conditions = card.VictoryConditions.Outcomes!.SelectMany(outcome => outcome.Any).ToArray();
        foreach (var id in conditions.SelectMany(condition => (condition.Buildings ?? []).Concat(condition.Versus ?? []).Concat(condition.Building is { } one ? [one] : [])).Distinct(StringComparer.Ordinal))
        {
            var hexes = ScenarioCards.BuildingHexes(card, id)!;
            tracked[id] = new VictoryControl(id, "building", hexes, StartSide(card, hexes));
        }

        foreach (var hex in conditions.Where(condition => condition.Type == "control-count").SelectMany(condition => ScenarioCards.BuildingHexes(card, condition.Building!)!).Distinct())
        {
            tracked[HexId(hex)] = new VictoryControl(HexId(hex), "hex", [hex], StartSide(card, [hex]));
        }

        foreach (var state in states.Where(state => state.SetupClosed))
        {
            foreach (var (id, item) in tracked.ToArray())
            {
                if (Gainer(state, item, knownTo) is { } side && side != item.Side)
                {
                    tracked[id] = item with
                    {
                        Side = side
                    };
                }
            }
        }

        return tracked;
    }

    private static string HexId(BoardLocation hex) => $"{hex.Board.Value}:{hex.Hex}";

    /// <summary>A26.11: the side whose setup areas alone hold all the hexes, or, for hexes no area holds, the only side setting up on their board.</summary>
    private static string? StartSide(ScenarioCard card, IReadOnlyList<BoardLocation> hexes)
    {
        string[] Holding(BoardLocation hex) => [.. card.Sides.Where(side => side.Groups.SelectMany(group => group.Areas)
            .Any(area => area.Kind != "entry" && ScenarioSetup.Within(card, area, hex))).Select(side => side.Side)];
        var holders = hexes.Select(Holding).ToArray();
        if (holders.All(sides => sides.Length == 1) && holders.Select(sides => sides[0]).Distinct(StringComparer.Ordinal).Count() == 1)
        {
            return holders[0][0];
        }

        if (holders.All(sides => sides.Length == 0))
        {
            var boards = hexes.Select(hex => hex.Board.Value).Distinct(StringComparer.Ordinal).ToArray();
            var setting = card.Sides.Where(side => side.Groups.SelectMany(group => group.Areas).Any(area => area.Kind != "entry"
                && (area.Board ?? (card.Boards.Count == 1 ? card.Boards[0].Board : null)) is { } board && boards.Contains(board))).Select(side => side.Side).ToArray();
            return boards.Length == 1 && setting.Length == 1 ? setting[0] : null;
        }

        return null;
    }

    private static bool Is(UnitInstance unit, string condition) => GameState.Condition(unit, condition) == ConditionState.True;

    /// <summary>A.7, A26.11: an armed Good Order Infantry MMC, not in Bypass, gains Control; any armed enemy ground unit but a Dummy or a prisoner prevents it.</summary>
    private static string? Gainer(GameState state, VictoryControl item, string? knownTo)
    {
        var inside = state.Units.Where(unit => unit.Status == InstanceStatus.Active && state.Location(unit.Id)?.Location is { } at
            && item.Hexes.Any(hex => hex.Board == at.Board && hex.Hex == at.Hex) && !Undeclared(unit, knownTo)).ToArray();
        var gainers = inside.Where(unit => Mmc.Contains(unit.Kind) && !Is(unit, Conditions.Unarmed) && !Is(unit, Conditions.Broken) && !Is(unit, Conditions.Berserk)
            && !Is(unit, Conditions.Captured) && !Is(unit, Conditions.Melee)
            && (item.Kind == "building" || state.Location(unit.Id)!.Location.Level == 0)
            && !(state.Movement is { Bypass.Count: > 0 } movement && movement.Movers.Contains(unit.Id, StringComparer.Ordinal))).Select(unit => unit.Side).Distinct(StringComparer.Ordinal).ToArray();
        return gainers is [var side] && !inside.Any(unit => unit.Side != side && unit.Kind != UnitKinds.Dummy && !Is(unit, Conditions.Captured) && !Is(unit, Conditions.Unarmed))
            ? side : null;
    }

    /// <summary>An enemy unit a side does not know as such (A26.15; ruling R23.4): concealed or hidden, when the Victory Conditions are read for that side.</summary>
    private static bool Undeclared(UnitInstance unit, string? knownTo) =>
        knownTo is not null && unit.Side != knownTo && (Is(unit, Conditions.Concealed) || Is(unit, Conditions.Hidden));

    /// <summary>
    /// A side's unbroken squad-equivalents on the map (A16): a squad one, a HS or crew half; not broken, not a prisoner. Read for <paramref name="knownTo"/>,
    /// another side's concealed and hidden units are not counted (ruling R23.4).
    /// </summary>
    public static double UnbrokenSquads(GameState state, string side, string? knownTo = null)
    {
        ArgumentNullException.ThrowIfNull(state);
        return state.Units.Where(unit => unit.Status == InstanceStatus.Active && unit.Side == side && state.Location(unit.Id) is not null && !Is(unit, Conditions.Broken)
            && !Is(unit, Conditions.Captured) && !Undeclared(unit, knownTo)).Sum(unit => unit.Kind switch
            {
                "asl:squad" => 1.0,
                "asl:half-squad" or "asl:crew" => 0.5,
                _ => 0.0,
            });
    }

    /// <summary>Whether an exit counts for one of its side's Exit VP conditions (A26.23): off the condition's edge, from a hex on or adjacent to one it names.</summary>
    private static bool Qualifies(ScenarioCard card, UnitExit exit, string side, Func<BoardLocation, IEnumerable<BoardLocation>> adjacent) =>
        card.VictoryConditions.Outcomes!.SelectMany(outcome => outcome.Any).Any(condition => condition.Type == "exit-vp" && condition.Side == side && condition.Edge == exit.Edge
            && condition.Near!.Select(hex => BoardLocation.Parse(hex + ":0")).Any(near => (near.Board == exit.From.Board && near.Hex == exit.From.Hex)
                || adjacent(near).Any(next => next.Board == exit.From.Board && next.Hex == exit.From.Hex)));

    /// <summary>A26.23: the VP a side has exited through its exit conditions' areas, none for broken Personnel.</summary>
    private static int ExitVp(ScenarioCard card, GameState state, string side, Func<UnitInstance, int> vp, Func<BoardLocation, IEnumerable<BoardLocation>> adjacent) =>
        state.Exits.Where(exit => !exit.Broken && state.Unit(exit.Unit) is { Status: InstanceStatus.Exited } unit && unit.Side == side && Qualifies(card, exit, side, adjacent))
            .Sum(exit => vp(state.Unit(exit.Unit)!));

    /// <summary>
    /// A26.22, A26.221, A26.222: a side's CVP: the VP of the enemy units eliminated, or that left the map other than by their own exit conditions, and of
    /// the enemy units it holds prisoner, double once the game has ended.
    /// </summary>
    private static int Cvp(ScenarioCard card, GameState state, string side, Func<UnitInstance, int> vp, Func<BoardLocation, IEnumerable<BoardLocation>> adjacent, bool ended) =>
        state.Units.Where(unit => unit.Side != side && unit.Kind != UnitKinds.Dummy).Sum(unit => unit.Status switch
        {
            InstanceStatus.Eliminated => vp(unit),
            InstanceStatus.Exited when state.Exits.LastOrDefault(exit => exit.Unit == unit.Id) is not { } exit || !Qualifies(card, exit, unit.Side, adjacent) => vp(unit),
            InstanceStatus.Active when Is(unit, Conditions.Captured) => vp(unit) * (ended ? 2 : 1),
            _ => 0,
        });

    /// <summary>Why a condition holds (its reason for the result), or null when it does not.</summary>
    private static string? Holds(ScenarioCard card, ScenarioCardCondition condition, Dictionary<string, VictoryControl> control, IReadOnlyList<VictorySide> standing,
        GameState present)
    {
        var other = card.Sides.First(side => side.Side != condition.Side).Side;
        var mine = standing.Single(item => item.Side == condition.Side);
        var theirs = standing.Single(item => item.Side == other);
        switch (condition.Type)
        {
            case "control-margin":
                var held = condition.Buildings!.Count(id => control[id].Side == condition.Side);
                var lost = condition.Versus!.Count(id => control[id].Side == other);
                return held - lost >= condition.Margin
                    ? $"{condition.Side} Controls {held} of {string.Join(", ", condition.Buildings!)} and {other} {lost} of {string.Join(", ", condition.Versus!)}, a margin of at least {condition.Margin} (A26.14)"
                    : null;
            case "control-count":
                var hexes = ScenarioCards.BuildingHexes(card, condition.Building!)!;
                var melee = condition.MeleeUncontrolled == true ? hexes.Where(hex => InMelee(present, hex)).ToHashSet() : [];
                var count = hexes.Count(hex => !melee.Contains(hex) && control[HexId(hex)].Side == condition.Side);
                return count >= condition.AtLeast
                    ? $"{condition.Side} Controls {count} hexes of building {condition.Building}, at least {condition.AtLeast} (A26.13)"
                    : null;
            case "squad-ratio":
                return mine.UnbrokenSquads > 0 && mine.UnbrokenSquads >= condition.Ratio * theirs.UnbrokenSquads
                    ? $"{condition.Side} has {Number(mine.UnbrokenSquads)} unbroken squad-equivalents against {Number(theirs.UnbrokenSquads)}, at least {Number(condition.Ratio!.Value)} times as many"
                    : null;
            case "sole-unbroken":
                var building = ScenarioCards.BuildingHexes(card, condition.Building!)!;
                string[] there = [.. present.Units.Where(unit => unit.Status == InstanceStatus.Active && unit.Kind != UnitKinds.Dummy && !Is(unit, Conditions.Broken)
                    && !Is(unit, Conditions.Captured) && present.Location(unit.Id)?.Location is { } at && building.Any(hex => hex.Board == at.Board && hex.Hex == at.Hex))
                    .Select(unit => unit.Side).Distinct(StringComparer.Ordinal)];
                return there is [var alone] && alone == condition.Side
                    ? $"{condition.Side} alone has an unbroken unit in building {condition.Building}"
                    : null;
            case "exit-vp":
                return mine.ExitVp >= condition.AtLeast
                    ? $"{condition.Side} has exited {mine.ExitVp} Exit VP off the {condition.Edge} edge near {string.Join(", ", condition.Near!)}, at least {condition.AtLeast} (A26.23)"
                    : null;
            case "cvp":
                return mine.Cvp >= condition.AtLeast ? $"{condition.Side} has {mine.Cvp} CVP, at least {condition.AtLeast} (A26.22)" : null;
            default:
                return null;
        }
    }

    /// <summary>Whether units of both sides are in Melee in a hex (A11.15).</summary>
    private static bool InMelee(GameState state, BoardLocation hex) =>
        state.Units.Where(unit => unit.Status == InstanceStatus.Active && Is(unit, Conditions.Melee) && state.Location(unit.Id)?.Location is { } at
            && at.Board == hex.Board && at.Hex == hex.Hex).Select(unit => unit.Side).Distinct(StringComparer.Ordinal).Count() > 1;

    private static string Number(double value) => value.ToString("0.#", CultureInfo.InvariantCulture);

    private static List<string> Facts(Dictionary<string, VictoryControl> control, IReadOnlyList<VictorySide> standing) =>
    [
        .. control.Values.Where(item => item.Kind == "building").Select(item => $"building {item.Id}: {item.Side ?? "not Controlled"}"),
        .. control.Values.Where(item => item.Kind == "hex").GroupBy(item => item.InMelee ? "in Melee, neither's for the card" : item.Side ?? "not Controlled")
            .Select(group => $"hexes {string.Join(", ", group.Select(item => item.Id))}: {group.Key}"),
        .. standing.Select(side => $"{side.Side}: {side.Cvp} CVP, {side.ExitVp} Exit VP, {Number(side.UnbrokenSquads)} unbroken squad-equivalents"),
    ];
}
