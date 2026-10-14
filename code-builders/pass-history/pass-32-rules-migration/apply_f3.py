"""Pass 32.f commit 3: PlanRout's blocks 1 to 6 call Rules, one function a block, in the old order."""
import pathlib

P = pathlib.Path(r"E:\Archive\GitHub\dlandi\LimboDancer.MCP\src\ASL\LimboDancer.Domains.Asl.Play\GamePlanner.Rout.cs")
text = P.read_bytes().decode("utf-8")
assert "\r\n" not in text


def replace_range(text, start_anchor, end_anchor, new):
    assert text.count(start_anchor) == 1, start_anchor[:60]
    assert text.count(end_anchor) == 1, end_anchor[:60]
    a = text.index(start_anchor)
    b = text.index(end_anchor) + len(end_anchor)
    assert a < b
    return text[:a] + new + text[b:]


START = '        if (state.Phase != "rtph")\n'
END = '''                if (route.Skip(reached + 1).FirstOrDefault(step => !RoutCover(state, step)) is { } beyond)
                {
                    return Refused(scope, label, expected, $"play.rout-destination: having reached {route[reached]}, {unit.Id} goes on only into woods or building Locations, not {beyond} (A10.51)");
                }
            }
        }
'''
NEW = '''        if (ScenarioA1RoutCalculator.RoutPhaseBar(state.Phase) is { } phaseBar)
        {
            return Refused(scope, label, expected, phaseBar);
        }

        // A2.1 (ruling R20.6): a rout never leaves the card's playable area.
        if (route.Select(step => PlayableBar(state, step)).FirstOrDefault(bar => bar is not null) is { } outside)
        {
            return Refused(scope, label, expected, outside);
        }

        if (state.Unit(unitId) is not { Status: InstanceStatus.Active } unit
            || !ScenarioA1RoutCalculator.RoutUnitAllowed(Is(unit, Conditions.Broken), Is(unit, Conditions.Melee), Is(unit, Conditions.Captured))
            || state.Location(unit.Id)?.Location is not { } start)
        {
            return Refused(scope, label, expected, ScenarioA1RoutCalculator.RoutUnitText(unitId));
        }

        if (ScenarioA1RoutCalculator.RoutedBar(unit.Id, state.RoutedThisPhase.Contains(unit.Id)) is { } routedBar)
        {
            return Refused(scope, label, expected, routedBar);
        }

        if (ScenarioA1RoutCalculator.RoutPinnedBar(unit.Id, Is(unit, Conditions.Pinned)) is { } pinnedBar)
        {
            return Refused(scope, label, expected, pinnedBar);
        }

        if (ScenarioA1RoutCalculator.MayRoutBar(unit.Id, MayRout(state, unit)) is { } mayRoutBar)
        {
            return Refused(scope, label, expected, mayRoutBar);
        }

        // A10.5: the ATTACKER's broken units rout first, one at a time, then the DEFENDER's; one with no legal step does not hold the DEFENDER up.
        var attacker = state.PhasingSide;
        if (ScenarioA1RoutCalculator.RoutOrderBar(unit.Side == attacker, () => state.RoutedThisPhase.Any(id => state.Unit(id) is { } routed && routed.Side != attacker)) is { } orderBar)
        {
            return Refused(scope, label, expected, orderBar);
        }

        if (unit.Side != attacker && state.Units.FirstOrDefault(other => ScenarioA1RoutCalculator.AttackerMustRoutFirst(other.Side == attacker, state.RoutedThisPhase.Contains(other.Id),
            Is(other, Conditions.Pinned), () => MustRout(state, other) is not null, () => CanRout(state, other))) is { } first)
        {
            return Refused(scope, label, expected, ScenarioA1RoutCalculator.AttackerFirstText(first.Id));
        }

        // E1.54 (backlog pass 16, ruling R16.6): at night a broken unit always Low Crawls, and surrenders only in CC.
        if (ScenarioA1RoutCalculator.NightRoutBar(unit.Id, state.Night, lowCrawl) is { } nightBar)
        {
            return Refused(scope, label, expected, nightBar);
        }

        // A20.21: a unit ADJACENT to its captors that is Disrupted, Encircled, or can get away only by Interdiction or Low Crawl surrenders instead.
        if (ScenarioA1RoutCalculator.SurrenderCandidate(state.Night, Is(unit, Conditions.Fanatic), state.NoQuarter.Contains(unit.Side, StringComparer.Ordinal))
            && Captors(state, unit) is { Count: > 0 } captors
            && ScenarioA1RoutCalculator.SurrenderCause(Is(unit, Conditions.Disrupted), state.Encircled(unit), () => TrappedByInterdiction(state, unit, start), captors) is { } cause)
        {
            return Refused(scope, label, expected, ScenarioA1RoutCalculator.RoutSurrenderText(unit.Id, cause, captors));
        }

        // A10.4 (read in the PDF, p. 66; ruling R31d.1): before it routs a broken unit leaves in its Location what it carries beyond its IPC, and routs
        // with the most PP it can carry within it; the choice is the owner's only among loads of equal PP. The rout does the leaving.
        IReadOnlyList<RoutLoadItem> left = [];
        IReadOnlyList<string> kept = [];
        if (RoutLoadOf(state, unit) is { Laden: true } load)
        {
            string[]? named = arguments.TryGetProperty("keep", out var keep) && keep.ValueKind == JsonValueKind.Array ? [.. Strings(arguments, "keep").Order(StringComparer.Ordinal)] : null;
            // Loads that differ only in which of two like counters is kept are one choice (the table player, pass 31d), and the game takes it.
            var choices = load.Choices;
            var chosen = ScenarioA1RoutCalculator.ChosenLoad(named, choices, load.BestLoads);
            if (chosen is null)
            {
                return Refused(scope, label, expected, ScenarioA1RoutCalculator.RoutLadenText(unit.Id, load.Total, load.Ipc, choices, load.Pp));
            }

            (kept, left) = (chosen, load.Left(chosen));
        }

        if (ScenarioA1RoutCalculator.RouteShapeBar(route.Count, lowCrawl) is { } shapeBar)
        {
            return Refused(scope, label, expected, shapeBar);
        }

        if (ScenarioA1RoutCalculator.LowCrawlOccupiedBar(lowCrawl, state.Night, () => state.At(start).OfType<UnitInstance>().Any(other => other.Status == InstanceStatus.Active
            && other.Side != unit.Side && other.Kind != UnitKinds.Dummy && !Is(other, Conditions.Captured))) is { } occupiedBar)
        {
            return Refused(scope, label, expected, occupiedBar);
        }

        // A10.51: each step, its cost, and the Known armed enemy units that have had the unit in their LOS; Rules walks the route over the scan (pass 32.f).
        var limit = RoutHalfMf(unit);
        var encircled = state.Encircled(unit);
        var scan = new RoutScan(this, state);
        var enemies = scan.Enemies(unit.Side);
        var seenBy = ScenarioA1RoutCalculator.SeenBy(scan, enemies, scan.Index(start)).ToHashSet(StringComparer.Ordinal);
        int[] steps = [.. route.Select(scan.Index)];
        var (walkBar, costs, _) = ScenarioA1RoutCalculator.RoutRouteWalk(scan, enemies, unit.Id, scan.Index(start), steps, lowCrawl, state.Night, limit, encircled, seenBy);
        if (walkBar is not null)
        {
            return Refused(scope, label, expected, walkBar);
        }

        // A10.51, A10.532: no ending ADJACENT to an armed enemy it began ADJACENT to; it must reach the nearest woods or building Location within its MF
        // this RtPh, not necessarily by a shortest route; with none, any legal route. Low Crawl moves one Location toward it.
        if (ScenarioA1RoutCalculator.RoutDestinationBar(scan, enemies, unit.Id, scan.Index(start), steps, lowCrawl, limit, encircled, state.ScenarioMonth) is { } destinationBar)
        {
            return Refused(scope, label, expected, destinationBar);
        }
'''

text = replace_range(text, START, END, NEW)
P.write_bytes(text.encode("utf-8"))
print("applied")
