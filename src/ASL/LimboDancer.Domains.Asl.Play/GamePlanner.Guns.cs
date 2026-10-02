using System.Globalization;
using System.Text.Json;
using LimboDancer.Dice;
using LimboDancer.Domains.Asl.Maps.Coordinates;
using LimboDancer.Domains.Asl.Rules;
using LimboDancer.Domains.Asl.Units.Documents;
using LimboDancer.Domains.Asl.Units.State;

namespace LimboDancer.Domains.Asl.Play;

/// <summary>
/// Gun handling of the backlog pass 8 (rulings R8.6, R8.9): a CA change without fire in a friendly fire phase (C3.22), a crew pushing its Gun
/// with a Manhandling DR (C10.3), and a truck or halftrack hooking up or unhooking a Gun (C10.11, C10.12).
/// </summary>
public sealed partial class GamePlanner
{
    private GamePlan PlanTurnGun(GameScope scope, JsonElement arguments, IReadOnlyList<GameEvent> existing, string attemptId, long expected, string label)
    {
        if (Replay(existing).Current is not { } state)
        {
            return Refused(scope, label, expected, "play.no-game: the game has no state yet");
        }

        if (!Text(arguments, "gunId", out var gunId) || !Text(arguments, "facing", out var facingText) || !UnitFacings.TryParse(facingText, out var facing))
        {
            return Refused(scope, label, expected, "play.invalid-arguments: a CA change names the Gun and a hexspine");
        }

        if (state.Find(gunId) is not EquipmentInstance { Status: InstanceStatus.Active, Holding: { Role: HoldingRole.Manned } manning, Position: MapPosition at } gun
            || state.Unit(manning.Holder) is not { Status: InstanceStatus.Active } crew)
        {
            return Refused(scope, label, expected, $"play.turn-gun: '{gunId}' is not an active Gun manned by an active unit (C3.22)");
        }

        var friendly = state.Phase is "pfph" or "afph" ? crew.Side == state.PhasingSide : state.Phase == "dfph" && crew.Side != state.PhasingSide;
        var shots = state.OrdnanceShots.FirstOrDefault(item => item.Gun == gun.Id);
        var marked = new[] { Conditions.Malfunctioned, Conditions.PrepFire, Conditions.FinalFire, Conditions.FirstFire, Conditions.IntensiveFire }.Any(name => Is(gun, name));
        if (!friendly || Is(crew, Conditions.Broken) || Is(crew, Conditions.Pinned) || Is(crew, "asl:ti") || Is(gun, "asl:ti")
            || (shots is not null ? !shots.RateOfFireKept : marked) || at.Facing == facing)
        {
            return Refused(scope, label, expected,
                "play.turn-gun: a Gun changes its CA without firing in its side's fire phase, to a new hexspine, while its Good Order, unpinned crew could still fire it (C3.22)");
        }

        var package = ScenarioA1OrdnancePackage.Identity.ToString();
        var events = new List<GameEvent> { Event(scope, attemptId, 1, expected, "gun-turned", new GunTurned(gun.Id, facing), package, null) };

        // A12.141 (ruling R8.5): a CA change in view of a Good Order enemy ground unit within 16 hexes loses the crew's and the Gun's "?".
        if ((Is(crew, Conditions.Concealed) || Is(crew, Conditions.Hidden)) && EnemyGoodOrderInLosWithin16(state, crew.Side, at.Location))
        {
            var revealed = new Dictionary<string, ConditionState>(StringComparer.Ordinal) { [Conditions.Concealed] = ConditionState.False, [Conditions.Hidden] = ConditionState.False };
            events.Add(Event(scope, attemptId, 2, expected, "conditions-changed", new ConditionsChanged(crew.Id, revealed), package, null));
            events.Add(Event(scope, attemptId, 3, expected, "conditions-changed", new ConditionsChanged(gun.Id, revealed), package, null));
        }

        return new GamePlan(GamePlanStatus.Ready, scope, label, expected, events,
            [$"play.turn-gun: {gun.Id} turns to {UnitFacings.Name(facing)} and fires no more this phase (C3.22)"
                + (state.Phase == "pfph" ? "; it and its crew do not move this Player Turn" : string.Empty)]);
    }

    /// <summary>
    /// A crew's push of its Gun (C10.3; ruling R8.6): a Manhandling DR against the M#, with +the hex's TEM (none in the reviewed terrain) and
    /// the MF expended, -2 across a road hexside: below the M# the Gun and crew enter, at it they enter and stop, above it they stay; both are TI
    /// once the push ends.
    /// </summary>
    private GamePlan PushPlan(GameScope scope, string attemptId, long expected, string label, string actor, GameState state, EquipmentInstance gun,
        MovementStepped moved, int drm, string summary)
    {
        var manhandling = OrdnanceReference.Value.Guns[gun.Definition!.Definition].Manhandling!.Value;
        var crew = gun.Holding!.Holder;
        var package = ScenarioA1FirePackage.Identity.ToString();
        IReadOnlyList<GameEvent> Build(Func<RollRequest, RollResult> draw)
        {
            var drawn = draw(new RollRequest(2, 6));
            var rollId = $"{attemptId}-roll-1";
            var result = ManhandlingRolled.For(drawn.Values.Sum() + drm, manhandling);
            var events = new List<GameEvent>
            {
                Event(scope, attemptId, 1, expected, "dice-rolled", new DiceRolled(rollId, "manhandling", 2, 6, drawn.Values, DiceRolled.SystemSource, actor), package, null),
                Event(scope, attemptId, 2, expected, "manhandling-rolled", new ManhandlingRolled(gun.Id, rollId, drm, manhandling, result), package, null),
            };
            if (result != ManhandlingRolled.Stay)
            {
                events.Add(Event(scope, attemptId, 3, expected, "movement-step", moved with
                {
                    PushedGun = gun.Id
                }, package, null, [EventId(attemptId, 2)]));
            }

            // C10.3: "in all three cases" the Gun and its crew are TI once the push ends; a crew still pushing may go on (table player, pass 8). Ruling R26.4:
            // a push off the map leaves nothing to mark.
            if (moved.Exit is null || result == ManhandlingRolled.Stay)
            {
                var ti = new Dictionary<string, ConditionState>(StringComparer.Ordinal) { ["asl:ti"] = ConditionState.True };
                events.Add(Event(scope, attemptId, events.Count + 1, expected, "conditions-changed", new ConditionsChanged(crew, ti), package, null, [EventId(attemptId, 2)]));
                events.Add(Event(scope, attemptId, events.Count + 1, expected, "conditions-changed", new ConditionsChanged(gun.Id, ti), package, null, [EventId(attemptId, 2)]));
            }

            return events;
        }

        return new GamePlan(GamePlanStatus.Ready, scope, label, expected, [],
            [$"{summary}, pushing {gun.Id}: a Manhandling DR {drm:+0;-0;+0} against M{manhandling.ToString(CultureInfo.InvariantCulture)} (C10.3)"])
        {
            Roll = new PlannedRoll("manhandling", Build),
            FirstEventId = EventId(attemptId, 1),
        };
    }

    private GamePlan PlanHookGun(GameScope scope, JsonElement arguments, IReadOnlyList<GameEvent> existing, string attemptId, long expected, string label)
    {
        if (Replay(existing).Current is not { } state)
        {
            return Refused(scope, label, expected, "play.no-game: the game has no state yet");
        }

        if (!Text(arguments, "vehicleId", out var vehicleId) || !Text(arguments, "gunId", out var gunId)
            || !arguments.TryGetProperty("hooked", out var hookedArgument) || hookedArgument.ValueKind is not (JsonValueKind.True or JsonValueKind.False))
        {
            return Refused(scope, label, expected, "play.invalid-arguments: a hook-up names the vehicle, the Gun, and whether it hooks up or unhooks");
        }

        var hook = hookedArgument.ValueKind == JsonValueKind.True;
        UnitFacing? facing = null;
        if (!hook)
        {
            if (!Text(arguments, "facing", out var facingText) || !UnitFacings.TryParse(facingText, out var parsed))
            {
                return Refused(scope, label, expected, "play.invalid-arguments: an unhooked Gun is set down facing a hexspine (C10.12)");
            }

            facing = parsed;
        }

        if (state.Phase != "mph" || state.Unit(vehicleId) is not { Status: InstanceStatus.Active } vehicle || vehicle.Side != state.PhasingSide
            || !LiveFire.IsVehicle(vehicle) || VehicleDefinition(vehicle) is not { MovementPoints: { } mp, Towing: { } towing } || state.Location(vehicle.Id) is not { } at)
        {
            return Refused(scope, label, expected, $"play.hook-vehicle: '{vehicleId}' is not a vehicle of the phasing side that can tow, in its MPh (C10.1)");
        }

        if (Is(vehicle, Conditions.Motion) || Is(vehicle, "asl:ti") || (state.Movement is { } moving && !(moving.Vehicle && moving.Movers.Contains(vehicle.Id) && moving.Stopped)))
        {
            return Refused(scope, label, expected, $"play.hook-stopped: {vehicle.Id} hooks or unhooks a Gun Stopped, and not while another unit moves (C10.11)");
        }

        var cost = (mp + 1) / 2;
        var spent = vehicle.MfSpent + (vehicle.HalfMfSpent ? 1 : 0);
        if (spent + cost > mp)
        {
            return Refused(scope, label, expected, $"play.hook-mp: {vehicle.Id} has {mp - spent} MP left, and a hook-up costs half its MP, {cost} (C10.11)");
        }

        if (state.Find(gunId) is not EquipmentInstance { Status: InstanceStatus.Active, Position: MapPosition gunAt } gun || gunAt.Location != at.Location
            || OrdnanceReference.Value.Guns.GetValueOrDefault(gun.Definition?.Definition ?? string.Empty) is not { Manhandling: { } manhandling } || towing > manhandling)
        {
            return Refused(scope, label, expected, $"play.hook-gun: '{gunId}' is not a Gun in {vehicle.Id}'s hex whose M# its T# does not exceed (C10.1)");
        }

        // C10.2 (ruling R26.1): limbering is not built, so only a QSU Gun is hooked up or unhooked.
        if (gun.Definition is not { } gunType || FireReference.Value.Definitions.GetValueOrDefault(gunType.Definition)?.QuickSetUp != true)
        {
            return Refused(scope, label, expected, $"play.hook-gun: {gun.Id} is not QSU and must be limbered first, which is not built (C10.2; ruling R26.1)");
        }

        // C10.111 (table player, pass 8): a crew or HS on foot, of the vehicle's side; an abandoned Gun is hooked up by any such unit there. C10.12 (ruling
        // R26.2): a crew riding the towing vehicle disembarks as it unhooks.
        bool CanCrew(UnitInstance unit) => unit.Status == InstanceStatus.Active && unit.Side == vehicle.Side
            && (vocabulary.IsA(unit.Kind, "asl:crew") || vocabulary.IsA(unit.Kind, "asl:half-squad")) && !Mans(state, unit);
        UnitInstance? OnFoot() => state.At(at.Location).OfType<UnitInstance>().FirstOrDefault(CanCrew);
        var crew = hook
            ? gun.Holding is { Role: HoldingRole.Manned } manning ? state.Unit(manning.Holder) : gun.Holding is null ? OnFoot() : null
            : gun.Holding is { Role: HoldingRole.Towed } tow && tow.Holder == vehicle.Id ? OnFoot() ?? state.Passengers(vehicle.Id).FirstOrDefault(CanCrew) : null;
        if (crew is null || Is(crew, Conditions.Broken) || Is(crew, Conditions.Pinned))
        {
            return Refused(scope, label, expected, "play.hook-crew: the Gun's Good Order, unpinned crew is on foot in the hex, or rides the vehicle as it unhooks (C10.111, C10.12)");
        }

        // C10.11, C10.13 (ruling R26.2): the crew may board as it hooks up; the ammunition takes four PP (eight at 100mm or more) of the vehicle's capacity,
        // which may leave no room only in an empty vehicle.
        var boards = hook && Flag(arguments, "boards");
        if (hook)
        {
            var reduction = (gun.Definition is { } caliberType ? FireReference.Value.Definitions.GetValueOrDefault(caliberType.Definition)?.Caliber : null) >= 100 ? 8 : 4;
            var room = (PassengerCapacity(state, vehicle) ?? 0) - reduction;
            UnitInstance[] riding = [.. state.Passengers(vehicle.Id), .. boards ? [crew] : Array.Empty<UnitInstance>()];
            var load = riding.Sum(unit => PassengerPp(state, unit) ?? int.MaxValue / 8);
            if (riding.Length > 0 && load > room)
            {
                return Refused(scope, label, expected, boards
                    ? $"play.hook-capacity: with {gun.Id}'s ammunition {vehicle.Id} has {Math.Max(0, room)} PP for Passengers, and they would take {load} (C10.13, D6.1)"
                    : $"play.hook-capacity: {gun.Id}'s ammunition takes {reduction} PP, which {vehicle.Id}'s Passengers need (C10.13)");
            }
        }

        var package = ScenarioA1OrdnancePackage.Identity.ToString();
        var ti = new Dictionary<string, ConditionState>(StringComparer.Ordinal) { ["asl:ti"] = ConditionState.True };
        List<GameEvent> events =
        [
            Event(scope, attemptId, 1, expected, "gun-hooked", new GunHooked(vehicle.Id, gun.Id, crew.Id, hook, cost, facing) { Boards = boards }, package, null),
            Event(scope, attemptId, 2, expected, "conditions-changed", new ConditionsChanged(gun.Id, ti), package, null),
            Event(scope, attemptId, 3, expected, "conditions-changed", new ConditionsChanged(crew.Id, ti), package, null),
        ];
        if (hook)
        {
            // C10.11: the vehicle is TI too.
            events.Add(Event(scope, attemptId, 4, expected, "conditions-changed", new ConditionsChanged(vehicle.Id, ti), package, null));
        }

        return new GamePlan(GamePlanStatus.Ready, scope, label, expected, events,
            [$"play.hook-gun: {vehicle.Id} {(hook ? "hooks up" : "unhooks")} {gun.Id} for {cost} MP"
                + (boards ? $"; {crew.Id} boards it as a Passenger" : !hook && crew.Position is ContainedPosition ? $"; {crew.Id} disembarks to man it" : string.Empty) + " (C10.11, C10.12)"]);
    }
}
