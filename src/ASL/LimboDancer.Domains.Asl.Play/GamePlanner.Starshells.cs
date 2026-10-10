using System.Text.Json;
using LimboDancer.Dice;
using LimboDancer.Domains.Asl.Maps.Coordinates;
using LimboDancer.Domains.Asl.Maps.Geometry;
using LimboDancer.Domains.Asl.Rules;
using LimboDancer.Domains.Asl.Units.State;

namespace LimboDancer.Domains.Asl.Play;

/// <summary>
/// Starshells (E1.91 to E1.923; backlog pass 16, ruling R16.8): a leader, CE AFV, or MMC fires one in the PFPh, or in the DFPh or as Defensive First Fire,
/// one attempt per hex per phase, after a Usage dr (4 or less for a leader, 2 or less otherwise), by one of the three placement methods; the Starshell
/// Illuminates every Location within three hexes until the end of the CCPh. Firing it is not firing. A Random Direction that leaves the map stops at its
/// last hex (a Starshell off the map is not built). The rules are <see cref="ScenarioA1Starshells"/>'s (pass 32.h); this file reads the request, the state,
/// and the map, and writes the events.
/// </summary>
public sealed partial class GamePlanner
{
    private GamePlan PlanStarshell(GameScope scope, JsonElement arguments, IReadOnlyList<GameEvent> existing, string attemptId, long expected, string label, string actor)
    {
        if (Replay(existing).Current is not { } state)
        {
            return Refused(scope, label, expected, "play.no-game: the game has no state yet");
        }

        if (ScenarioA1Starshells.DayBar(state.Night) is { } dayBar)
        {
            return Refused(scope, label, expected, dayBar);
        }

        if (!Text(arguments, "unitId", out var unitId) || !Text(arguments, "method", out var method) || method is not ("own-hex" or "at-target" or "three-hexes"))
        {
            return Refused(scope, label, expected, "play.invalid-arguments: a Starshell names its firer and a method: own-hex, at-target, or three-hexes (E1.922)");
        }

        if (state.Unit(unitId) is not { Status: InstanceStatus.Active } unit || state.Location(unit.Id)?.Location is not { } from)
        {
            return Refused(scope, label, expected, ScenarioA1Starshells.FirerOffMapText(unitId));
        }

        var leader = unit.Kind == "asl:leader";
        var afv = LiveFire.IsVehicle(unit);
        var mmc = unit.Kind is "asl:squad" or "asl:half-squad" or "asl:crew";
        if (ScenarioA1Starshells.FirerBar(unitId, leader, afv, mmc, afv && ButtonedUpAfv(unit), Is(unit, Conditions.Stunned), Is(unit, Conditions.Shocked),
            GoodOrder(unit), Is(unit, Conditions.Pinned), Is(unit, "asl:ti"), Is(unit, Conditions.Captured)) is { } firerBar)
        {
            return Refused(scope, label, expected, firerBar);
        }

        var phasing = unit.Side == state.PhasingSide;
        if (ScenarioA1Starshells.PhaseBar(state.Phase, phasing) is { } phaseBar)
        {
            return Refused(scope, label, expected, phaseBar);
        }

        var hex = new BoardLocation(from.Board, from.Hex, 0);
        if (ScenarioA1Starshells.OnceBar(state.StarshellAttempts.Contains(hex.ToString(), StringComparer.Ordinal), from.Hex.ToString()) is { } onceBar)
        {
            return Refused(scope, label, expected, onceBar);
        }

        // E1.91, E1.33 (referee, pass 16): the scans for a seen enemy unit and for a Gunflash, read when Rules asks.
        bool Seen(UnitInstance other) => state.Location(other.Id) is { } seen && Los(state, from, seen.Location) is { IsBlocked: false } sight
            && NightSight(state, unit, from, seen.Location, sight.Range, [other]) is { BeyondNvr: false, Reason: null };
        if (ScenarioA1Starshells.FirstBar(state.StarshellUsed,
            () => state.Units.Any(other => other.Status == InstanceStatus.Active && other.Side != unit.Side && other.Kind != UnitKinds.Dummy && Seen(other)),
            () => state.Units.Any(other => other.Status == InstanceStatus.Active && state.Location(other.Id) is { } marked && Gunflash(state, marked.Location))) is { } firstBar)
        {
            return Refused(scope, label, expected, firstBar);
        }

        // E1.921: whether any fire or movement has happened this phase.
        var start = existing.Select((item, index) => (item, index)).LastOrDefault(pair => pair.item.Payload is PhaseChanged).index;
        var acted = existing.Skip(start).Any(item => item.Payload is FireResolved or MovementStepped or VehicleStepped);
        if (ScenarioA1Starshells.TimingBar(state.StarshellUsed, state.StarshellTurn, $"{state.Turn}|{state.PhasingSide}", leader, state.Phase, acted) is { } timingBar)
        {
            return Refused(scope, label, expected, timingBar);
        }

        BoardLocation initial = hex;
        if (method != "own-hex")
        {
            if (!Text(arguments, "at", out var atText) || !BoardLocation.TryParse(atText, out var aimed) || HexDistance(state, from, aimed) is not { } range)
            {
                return Refused(scope, label, expected, "play.invalid-arguments: this Starshell method names the hex it is aimed at (E1.922)");
            }

            initial = new BoardLocation(aimed.Board, aimed.Hex, 0);
            if (ScenarioA1Starshells.ThreeHexesBar(method, aimed.Hex.ToString(), range) is { } threeBar)
            {
                return Refused(scope, label, expected, threeBar);
            }

            if (ScenarioA1Starshells.AtTargetBar(method, range, () => Los(state, from, aimed) is { IsBlocked: false },
                () => state.At(aimed).Any(item => item is UnitInstance { Status: InstanceStatus.Active } seen && seen.Side != unit.Side && VisibleTo(seen, unit.Side) && Seen(seen))
                    || Gunflash(state, aimed), aimed.Hex.ToString(), unitId) is { } aimBar)
            {
                return Refused(scope, label, expected, aimBar);
            }
        }

        var package = ScenarioA1FirePackage.Identity.ToString();
        var need = ScenarioA1Starshells.UsageNeed(leader);
        IReadOnlyList<GameEvent> Build(Func<RollRequest, RollResult> draw)
        {
            var events = new List<GameEvent>();
            string Roll(int count, string purpose)
            {
                var drawn = draw(new RollRequest(count, 6));
                var rollId = $"{attemptId}-roll-{(events.Count(item => item.Payload is DiceRolled) + 1).ToString(System.Globalization.CultureInfo.InvariantCulture)}";
                events.Add(Event(scope, attemptId, events.Count + 1, expected, "dice-rolled",
                    new DiceRolled(rollId, purpose, count, 6, drawn.Values, DiceRolled.SystemSource, actor), package, null));
                return rollId;
            }

            var usage = Roll(1, "starshell-usage");
            var passed = ScenarioA1Starshells.UsagePassed(((DiceRolled)events[^1].Payload).Values[0], need);
            string? placement = null;
            BoardLocation? at = null;
            if (passed)
            {
                // B.8: the colored dr a hexside direction (1 the top hexside, clockwise); the extent is Rules' (pass 32.h); the walk stops at the map's edge.
                placement = Roll(ScenarioA1Starshells.PlacementDice(method), "starshell-placement");
                var dice = ((DiceRolled)events[^1].Payload).Values;
                var extent = ScenarioA1Starshells.PlacementExtent(method, dice);
                var (board, hexName) = (initial.Board, initial.Hex);
                for (var step = 0; step < extent && Next(state, board, hexName, (HexsideDirection)(dice[0] - 1)) is { } next; step++)
                {
                    (board, hexName) = next;
                }

                at = new BoardLocation(board, hexName, 0);
            }

            events.Add(Event(scope, attemptId, events.Count + 1, expected, "starshell-fired",
                new StarshellFired(unit.Id, hex, method, usage, passed, placement, at, at is null ? null : "starshell-" + attemptId), package, null));

            // E1.921: a hidden firer is placed beneath a "?".
            if (Is(unit, Conditions.Hidden))
            {
                events.Add(Event(scope, attemptId, events.Count + 1, expected, "conditions-changed",
                    new ConditionsChanged(unit.Id, ConditionChanges(ScenarioA1Starshells.HiddenFirerConditions())), package, null));
            }

            return events;
        }

        return new GamePlan(GamePlanStatus.Ready, scope, label, expected, [], [ScenarioA1Starshells.Summary(unit.Id, method, need)])
        {
            Roll = new PlannedRoll("starshell", Build),
            FirstEventId = EventId(attemptId, 1),
        };
    }
}
