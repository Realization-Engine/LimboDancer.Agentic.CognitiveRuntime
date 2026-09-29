using System.Text.Json;
using LimboDancer.Dice;
using LimboDancer.Domains.Asl.Maps.Coordinates;
using LimboDancer.Domains.Asl.Maps.Geometry;
using LimboDancer.Domains.Asl.ScenarioA1;
using LimboDancer.Domains.Asl.Units.State;

namespace LimboDancer.Domains.Asl.Play;

/// <summary>
/// Starshells (E1.91 to E1.923; backlog pass 16, ruling R16.8): a leader, CE AFV, or MMC fires one in the PFPh, or in the DFPh or as Defensive First Fire,
/// one attempt per hex per phase, after a Usage dr (4 or less for a leader, 2 or less otherwise), by one of the three placement methods; the Starshell
/// Illuminates every Location within three hexes until the end of the CCPh. Firing it is not firing. A Random Direction that leaves the map stops at its
/// last hex (a Starshell off the map is not built).
/// </summary>
public sealed partial class GamePlanner
{
    private GamePlan PlanStarshell(GameScope scope, JsonElement arguments, IReadOnlyList<GameEvent> existing, string attemptId, long expected, string label, string actor)
    {
        if (Replay(existing).Current is not { } state)
        {
            return Refused(scope, label, expected, "play.no-game: the game has no state yet");
        }

        if (!state.Night)
        {
            return Refused(scope, label, expected, "play.starshell-day: Starshells are fired only at night (E1.9)");
        }

        if (!Text(arguments, "unitId", out var unitId) || !Text(arguments, "method", out var method) || method is not ("own-hex" or "at-target" or "three-hexes"))
        {
            return Refused(scope, label, expected, "play.invalid-arguments: a Starshell names its firer and a method: own-hex, at-target, or three-hexes (E1.922)");
        }

        if (state.Unit(unitId) is not { Status: InstanceStatus.Active } unit || state.Location(unit.Id)?.Location is not { } from)
        {
            return Refused(scope, label, expected, $"play.starshell-firer: '{unitId}' is not a unit on the map");
        }

        var leader = unit.Kind == "asl:leader";
        var afv = LiveFire.IsVehicle(unit);
        var mmc = unit.Kind is "asl:squad" or "asl:half-squad" or "asl:crew";
        if ((!leader && !afv && !mmc) || (afv && (Is(unit, Conditions.ButtonedUp) || Is(unit, Conditions.Stunned) || Is(unit, Conditions.Shocked)))
            || (!afv && !GoodOrder(unit)) || Is(unit, Conditions.Pinned) || Is(unit, "asl:ti") || Is(unit, Conditions.Captured))
        {
            return Refused(scope, label, expected, $"play.starshell-firer: {unitId} is not a Good Order, unpinned, not TI leader, CE AFV, or MMC (E1.92, E1.921)");
        }

        var phasing = unit.Side == state.PhasingSide;
        if (!(state.Phase == "pfph" && phasing) && !(state.Phase is "dfph" or "mph" && !phasing))
        {
            return Refused(scope, label, expected, "play.starshell-phase: a Starshell is fired in the PFPh by the phasing side, or in the DFPh or as Defensive First Fire by the other (E1.92)");
        }

        var hex = new BoardLocation(from.Board, from.Hex, 0);
        if (state.StarshellAttempts.Contains(hex.ToString(), StringComparer.Ordinal))
        {
            return Refused(scope, label, expected, $"play.starshell-once: a Starshell attempt was made from {from.Hex} this phase; one per hex (E1.92)");
        }

        // E1.91: the first Starshell of the game needs an enemy unit in the firer's LOS, or a Gunflash on the map.
        // E1.33 (referee, pass 16): at night an enemy unit is seen within the firer's NVR or Illuminated.
        bool Seen(UnitInstance other) => state.Location(other.Id) is { } seen && Los(state, from, seen.Location) is { IsBlocked: false } sight
            && NightSight(state, unit, from, seen.Location, sight.Range, [other]) is { BeyondNvr: false, Reason: null };
        if (!state.StarshellUsed && !state.Units.Any(other => other.Status == InstanceStatus.Active && other.Side != unit.Side && other.Kind != UnitKinds.Dummy && Seen(other))
            && !state.Units.Any(other => other.Status == InstanceStatus.Active && state.Location(other.Id) is { } marked && Gunflash(state, marked.Location)))
        {
            return Refused(scope, label, expected, "play.starshell-first: no Starshell is fired until the firer sees an enemy unit or a Gunflash is placed (E1.91)");
        }

        // E1.921: after the Player Turn of the first Starshell, a firer other than a leader fires it at the start of the PFPh or the enemy MPh, before any
        // fire or movement.
        var start = existing.Select((item, index) => (item, index)).LastOrDefault(pair => pair.item.Payload is PhaseChanged).index;
        var acted = existing.Skip(start).Any(item => item.Payload is FireResolved or MovementStepped or VehicleStepped);
        if (state.StarshellUsed && state.StarshellTurn != $"{state.Turn}|{state.PhasingSide}" && !leader && (state.Phase == "dfph" || acted))
        {
            return Refused(scope, label, expected, "play.starshell-timing: after the first Starshell, only a leader fires one after the start of the PFPh or the enemy MPh (E1.921)");
        }

        BoardLocation initial = hex;
        if (method != "own-hex")
        {
            if (!Text(arguments, "at", out var atText) || !BoardLocation.TryParse(atText, out var aimed) || HexDistance(state, from, aimed) is not { } range)
            {
                return Refused(scope, label, expected, "play.invalid-arguments: this Starshell method names the hex it is aimed at (E1.922)");
            }

            initial = new BoardLocation(aimed.Board, aimed.Hex, 0);
            if (method == "three-hexes" && range != 3)
            {
                return Refused(scope, label, expected, $"play.starshell-placement: {aimed.Hex} is {range} hexes away, not exactly three (E1.922)");
            }

            // E1.922 method 2: a Gunflash or Known enemy unit in the firer's LOS, less than nine hexes away, at most six (placement along the LOS is not built).
            if (method == "at-target" && (range > 6 || Los(state, from, aimed) is not { IsBlocked: false }
                || !(state.At(aimed).Any(item => item is UnitInstance { Status: InstanceStatus.Active } seen && seen.Side != unit.Side && VisibleTo(seen, unit.Side)
                    && Seen(seen)) || Gunflash(state, aimed))))
            {
                return Refused(scope, label, expected, $"play.starshell-placement: {aimed.Hex} is not a Gunflash or Known enemy unit in {unitId}'s LOS within six hexes (E1.922)");
            }
        }

        var package = ScenarioA1FirePackage.Identity.ToString();
        var need = leader ? 4 : 2;
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
            var passed = ((DiceRolled)events[^1].Payload).Values[0] <= need;
            string? placement = null;
            BoardLocation? at = null;
            if (passed)
            {
                // B.8: the colored dr a hexside direction (1 the top hexside, clockwise); one hex from the firer's hex, or the white dr (halved, FRU, for method 2).
                placement = Roll(method == "own-hex" ? 1 : 2, "starshell-placement");
                var dice = ((DiceRolled)events[^1].Payload).Values;
                var extent = method == "own-hex" ? 1 : method == "at-target" ? (dice[1] + 1) / 2 : dice[1];
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
                events.Add(Event(scope, attemptId, events.Count + 1, expected, "conditions-changed", new ConditionsChanged(unit.Id,
                    new Dictionary<string, ConditionState> { [Conditions.Hidden] = ConditionState.False, [Conditions.Concealed] = ConditionState.True }), package, null));
            }

            return events;
        }

        return new GamePlan(GamePlanStatus.Ready, scope, label, expected, [],
            [$"play.starshell: {unit.Id} tries to fire a Starshell ({method}): a Usage dr of {need} or less fires it, and it Illuminates three hexes around where it lands until the end of the CCPh (E1.92, E1.923)"])
        {
            Roll = new PlannedRoll("starshell", Build),
            FirstEventId = EventId(attemptId, 1),
        };
    }
}
