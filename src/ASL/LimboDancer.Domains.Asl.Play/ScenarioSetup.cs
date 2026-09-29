using LimboDancer.Domains.Asl.Maps.Coordinates;
using LimboDancer.Domains.Asl.ScenarioA1;

namespace LimboDancer.Domains.Asl.Play;

/// <summary>
/// A counter set up in a game from a card (pass 19, rulings R19.1 to R19.6): its side and OB group (a SW takes its holder's), its definition and
/// kind, its Location (a SW its holder's), whether it is concealed or hidden, a Dummy, or a SW, and whether this setup proposal places it.
/// </summary>
public sealed record SetupCounter(string Id, string Side, string? Group, string? Definition, string Kind, BoardLocation? At, bool Concealed, bool Hidden,
    bool Dummy, bool Equipment, bool New);

/// <summary>An OB line still to set up: its group, the area the card names for it (null for any of the group's areas), its definition, and how many.</summary>
public sealed record SetupNeed(string Group, string? Area, string Definition, int Count);

/// <summary>
/// Where an OB group stands in the setup (ruling R19.2): its side, its place in the order, whether it sets up on board, whether it is done, what it
/// still has to set up, and how many of its "?" are left.
/// </summary>
public sealed record SetupGroup(string Side, string Id, string Name, int Order, bool SetsUp, bool Complete, IReadOnlyList<SetupNeed> Remaining, int DummiesLeft,
    IReadOnlyList<ScenarioCardSetup> Areas);

/// <summary>The setup's state (ruling R19.2): each group's, the order being set up now, and why the counters as placed are refused (empty when they are not).</summary>
public sealed record SetupReport(IReadOnlyList<SetupGroup> Groups, int? CurrentOrder, IReadOnlyList<string> Reasons)
{
    public bool Complete => Groups.All(group => !group.SetsUp || group.Complete);
}

/// <summary>
/// The setup of a game from a scenario card (pass 19 of the Scenario Card Games Plan; rulings R19.1 to R19.6): each OB group sets up its own
/// counters, in its own areas, in the card's order (A2.9, A12.12), never overstacked (A5.1) nor where it could not enter in play (A2.9), with
/// its OB "?" only in Concealment Terrain (A12.12), and with up to 10% of a side's squads Deployed (A2.9). A pure function of the card and the
/// counters, so the planner, the Play page, and tests read the same answer.
/// </summary>
public static class ScenarioSetup
{
    private static readonly string[] SmcKinds = ["asl:leader", "asl:hero"];

    /// <summary>The order a group sets up in (R17.12): its own, else 1 for the side that sets up first and 2 for the other.</summary>
    public static int OrderOf(ScenarioCard card, ScenarioCardSide side, ScenarioCardGroup group)
    {
        ArgumentNullException.ThrowIfNull(card);
        ArgumentNullException.ThrowIfNull(side);
        ArgumentNullException.ThrowIfNull(group);
        return group.SetupOrder ?? (side.Side == card.Turns.SetsUpFirst ? 1 : 2);
    }

    /// <summary>Whether a Location lies in a setup area of the card (R17.8, R19.3): a building's hexes, or a board's hex numbers.</summary>
    public static bool Within(ScenarioCard card, ScenarioCardSetup area, BoardLocation at)
    {
        ArgumentNullException.ThrowIfNull(card);
        ArgumentNullException.ThrowIfNull(area);
        ArgumentNullException.ThrowIfNull(at);
        var board = area.Board ?? (card.Boards.Count == 1 ? card.Boards[0].Board : null);
        return board == at.Board.Value && area.Kind switch
        {
            "building" => area.Hexes?.Contains(at.Hex.ToString(), StringComparer.Ordinal) == true,
            "hex-numbers" => at.Hex.RowNumber >= area.From && at.Hex.RowNumber <= area.To,
            _ => false,
        };
    }

    /// <summary>
    /// The setup's state and why it is refused. <paramref name="terrain"/> gives a Location's terrain key (as the planner reads it), and
    /// <paramref name="enterable"/> whether Infantry could enter that terrain in play; <paramref name="halfSquadOf"/> names a squad's HS.
    /// </summary>
    public static SetupReport Check(ScenarioCard card, IReadOnlyList<SetupCounter> counters, Func<BoardLocation, string?> terrain, Func<string?, bool> enterable,
        Func<string, string?> halfSquadOf, int? month)
    {
        ArgumentNullException.ThrowIfNull(card);
        ArgumentNullException.ThrowIfNull(counters);
        ArgumentNullException.ThrowIfNull(terrain);
        ArgumentNullException.ThrowIfNull(enterable);
        ArgumentNullException.ThrowIfNull(halfSquadOf);
        var reasons = new List<string>();
        var groups = new List<SetupGroup>();
        var placed = counters.Where(counter => counter.Kind != "asl:sniper").ToArray();

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
            foreach (var (group, index) in side.Groups.Select((group, index) => (group, index)))
            {
                var id = ScenarioCards.GroupId(side.Side, index);
                var mine = placed.Where(counter => counter.Side == side.Side && counter.Group == id).ToArray();
                var setsUp = group.Areas.Any(area => area.Kind != "entry");
                var (remaining, groupReasons, deployed) = Fill(card, group, id, mine, halfSquadOf);
                deployedSide += deployed;
                reasons.AddRange(groupReasons);

                // R19.3: each counter within one of its group's setup areas.
                foreach (var counter in mine.Where(counter => counter.At is not null))
                {
                    if (!group.Areas.Any(area => area.Kind != "entry" && Within(card, area, counter.At!)))
                    {
                        reasons.Add($"play.setup-area: {counter.Id} sets up at {counter.At}, outside the setup areas of {group.Name} (ruling R19.3)");
                    }
                }

                // R19.5 (A12.11, A12.12): the group's OB "?" are its Dummies plus each Location of its concealed real units.
                var dummies = mine.Count(counter => counter.Dummy);
                var concealedStacks = mine.Where(counter => counter.Concealed && !counter.Dummy && !counter.Equipment && counter.At is not null)
                    .Select(counter => counter.At!).Distinct().Count();
                var allotment = group.Dummies ?? 0;
                if (dummies + concealedStacks > allotment)
                {
                    reasons.Add($"play.setup-concealment: {group.Name} uses {dummies + concealedStacks} \"?\" at setup and has {allotment} (A12.12; ruling R19.5)");
                }

                foreach (var counter in mine.Where(counter => (counter.Concealed || counter.Dummy) && counter.At is not null))
                {
                    var key = terrain(counter.At!);
                    var area = group.Areas.FirstOrDefault(item => item.Kind != "entry" && Within(card, item, counter.At!));
                    if (key is null || !ConcealmentTerrain(key, month) || area?.Concealed == false)
                    {
                        reasons.Add($"play.setup-concealment: {counter.Id} sets up under \"?\" at {counter.At}, which is not Concealment Terrain"
                            + (area?.Concealed == false ? " or the SSRs forbid \"?\" there" : string.Empty) + " (A12.12; ruling R19.5)");
                    }
                }

                if (mine.Any(counter => counter.Hidden))
                {
                    reasons.Add($"play.setup-hidden: {group.Name} has no HIP by SSR (A12.3; ruling R19.5)");
                }

                // R19.4: an area whose SSR fixes its counters takes exactly that many, none a "?", with at least the MMC it names.
                var complete = setsUp && remaining.Count == 0;
                var limitedComplete = setsUp;
                foreach (var area in group.Areas.Where(area => area.Counters is not null))
                {
                    var inArea = mine.Where(counter => !counter.Dummy && counter.At is not null && Within(card, area, counter.At)).ToArray();
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
                        var inArea = mine.Where(counter => !counter.Dummy && counter.At is not null && Within(card, area, counter.At)).ToArray();
                        var mmc = inArea.Count(counter => counter.Kind is "asl:squad" or "asl:half-squad" or "asl:crew");
                        if (inArea.Length < area.Counters)
                        {
                            remaining.Add(new SetupNeed(id, area.Id, "counters", area.Counters!.Value - inArea.Length));
                        }

                        if (mmc < (area.MinMmc ?? 0))
                        {
                            remaining.Add(new SetupNeed(id, area.Id, "MMC among them", area.MinMmc!.Value - mmc));
                        }
                    }
                }

                groups.Add(new SetupGroup(side.Side, id, group.Name, OrderOf(card, side, group), setsUp, complete, remaining, allotment - dummies - concealedStacks, group.Areas));
            }

            var allowed = (sideSquads + deployedSide + 9) / 10;
            if (deployedSide > allowed)
            {
                reasons.Add($"play.setup-deployment: {side.Side} Deploys {deployedSide} squad(s) before setup, and 10% (FRU) of its {sideSquads + deployedSide} is {allowed} (A2.9; ruling R19.6)");
            }
        }

        // R19.3 (A2.9, A5.1): never where Infantry could not enter, never overstacked.
        foreach (var counter in placed.Where(counter => counter.At is not null && !counter.Equipment))
        {
            if (!enterable(terrain(counter.At!)))
            {
                reasons.Add($"play.setup-terrain: {counter.Id} sets up at {counter.At}, terrain it could not enter in play (A2.9; ruling R19.3)");
            }
        }

        foreach (var stack in placed.Where(counter => counter.At is not null && !counter.Equipment && !counter.Dummy).GroupBy(counter => (counter.Side, counter.At!)))
        {
            var smc = stack.Count(counter => SmcKinds.Contains(counter.Kind));
            // A5.5 (referee, pass 19): four SMC count nothing; beyond that, five SMC equal a HS.
            var squads = stack.Count(counter => counter.Kind == "asl:squad") + (stack.Count(counter => counter.Kind is "asl:half-squad" or "asl:crew") / 2m)
                + (smc > 4 ? smc / 10m : 0m);
            if (squads > 3)
            {
                reasons.Add($"play.setup-stacking: {stack.Key.Side} sets up {squads:0.#} squad-equivalents at {stack.Key.Item2}, and the limit is 3 (A5.1; ruling R19.3)");
            }
        }

        // R19.2: a proposal places only groups of the order now setting up; the order advances when all its groups are complete.
        var current = groups.Where(group => group.SetsUp && !group.Complete).Select(group => (int?)group.Order).Min();
        foreach (var counter in placed.Where(counter => counter.New && counter.Group is not null))
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
                && placed.Any(other => !other.New && other.Side == item.Side && other.Group == item.Id));
            if (group is not null && later is not null)
            {
                reasons.Add($"play.setup-order: {counter.Id} of {group.Name} may not set up once {later.Name} has begun setting up (A2.9, A12.12; ruling R19.2)");
                break;
            }
        }

        return new SetupReport(groups, current, [.. reasons.Distinct(StringComparer.Ordinal)]);
    }

    /// <summary>
    /// Matches a group's placed counters to its OB lines (R19.1): each counter fills a line of its definition whose area holds it (or any area),
    /// a Deployed squad's two HS fill one squad line (A2.9). What is left, why a counter fits no line, and how many squads were Deployed.
    /// </summary>
    private static (List<SetupNeed> Remaining, List<string> Reasons, int Deployed) Fill(ScenarioCard card, ScenarioCardGroup group, string id,
        IReadOnlyList<SetupCounter> mine, Func<string, string?> halfSquadOf)
    {
        var reasons = new List<string>();
        var lines = group.Units.Select(unit => (Unit: unit, Left: unit.Count)).ToArray();
        var halves = new Dictionary<(string Definition, string? Area), int>();
        foreach (var counter in mine.Where(counter => !counter.Dummy))
        {
            // Referee, pass 19: a counter off the map fills no line; entering is pass 20.
            if (counter.At is null)
            {
                reasons.Add($"play.setup-area: {counter.Id} of {group.Name} is not set up on the map (A2.9; ruling R19.3)");
                continue;
            }

            bool Fits(ScenarioCardUnit unit) => unit.Area is null
                || group.Areas.FirstOrDefault(area => area.Id == unit.Area) is { } area && Within(card, area, counter.At);
            // A counter outside all its group's areas takes only the area reason (table player, pass 19).
            if (!group.Areas.Any(area => area.Kind != "entry" && Within(card, area, counter.At)))
            {
                continue;
            }

            var line = Array.FindIndex(lines, item => item.Left > 0 && item.Unit.Definition == counter.Definition && Fits(item.Unit));
            if (line >= 0)
            {
                lines[line].Left--;
                continue;
            }

            // A2.9: a HS of a squad in the group's OB, set up as half of a Deployed squad: the second HS pairs with the first.
            var pairing = Array.FindIndex(lines, item => halfSquadOf(item.Unit.Definition) == counter.Definition && Fits(item.Unit)
                && halves.GetValueOrDefault((item.Unit.Definition, item.Unit.Area)) % 2 == 1);
            if (pairing >= 0)
            {
                var pairKey = (lines[pairing].Unit.Definition, lines[pairing].Unit.Area);
                halves[pairKey] = halves[pairKey] + 1;
                continue;
            }

            var parent = Array.FindIndex(lines, item => item.Left > 0 && halfSquadOf(item.Unit.Definition) == counter.Definition && Fits(item.Unit));
            if (parent >= 0)
            {
                var key = (lines[parent].Unit.Definition, lines[parent].Unit.Area);
                halves[key] = halves.GetValueOrDefault(key) + 1;
                if (halves[key] % 2 == 1)
                {
                    lines[parent].Left--;
                }

                continue;
            }

            reasons.Add($"play.setup-pool: {counter.Id} ({counter.Definition}) is not left in the OB of {group.Name}"
                + (counter.At is not null ? $" at {counter.At}" : string.Empty) + " (ruling R19.1)");
        }

        foreach (var ((definition, _), count) in halves.Where(item => item.Value % 2 == 1))
        {
            reasons.Add($"play.setup-deployment: a Deployed {definition} of {group.Name} sets up both its HS (A2.9; ruling R19.6)");
        }

        // What a group with only setup areas must still place; a group whose area fixes its counters is judged by that area (R19.4).
        var limited = group.Areas.Any(area => area.Counters is not null);
        var remaining = limited || group.Areas.All(area => area.Kind == "entry") ? []
            : lines.Where(item => item.Left > 0).Select(item => new SetupNeed(id, item.Unit.Area, item.Unit.Definition, item.Left)).ToList();
        return (remaining, reasons, halves.Values.Sum(value => (value + 1) / 2));
    }

    /// <summary>Concealment Terrain for setup (A12.12, as the planner reads it): grain only June to September.</summary>
    public static bool ConcealmentTerrain(string terrain, int? month) =>
        terrain is "brush" or "woods" or "orchard" or "marsh" or "wooden-building" or "stone-building" or "wooden-rubble" or "stone-rubble"
        || (terrain == "grain" && month is >= 6 and <= 9);
}
