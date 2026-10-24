using System.Text.Json;
using LimboDancer.Dice;
using LimboDancer.Domains.Asl.Maps.Coordinates;
using LimboDancer.Domains.Asl.Maps.Los;
using LimboDancer.Domains.Asl.Rules;
using LimboDancer.Domains.Asl.Units.Documents;
using LimboDancer.Domains.Asl.Units.State;

namespace LimboDancer.Domains.Asl.Play;

/// <summary>
/// The Motion attempt (D2.401, p. 198; pass 55, rulings R55.11 to R55.13): in the enemy MPh, while the DEFENDER's window on a moving unit's
/// expenditure is open, a DEFENDER's vehicle makes a dr against the MF or MP that unit has spent in its LOS in this MPh. This file reads the
/// state, the phase's steps, and the LOS, and writes the events; <see cref="ScenarioA1MotionAttempts"/> decides. The attempt is no fire action
/// and does not close the window.
/// </summary>
public sealed partial class GamePlanner
{
    /// <summary>
    /// A vehicle's Motion attempt as the Play page offers it: why it may not be made now, or the moving unit it answers with the half MF or MP that
    /// unit has spent in the vehicle's LOS; whether the mover is a vehicle (MP) or Infantry (MF); and whether the vehicle may name a TCA or any CA.
    /// </summary>
    public sealed record MotionAttemptOption(string Vehicle, string? Bar, string? Mover, int HalfPoints, bool VehicleMover, bool Turreted, bool Bypass)
    {
        /// <summary>The bar in the few words of the list's option, without the vehicle's name; null when the attempt may be made.</summary>
        public string? BarShort
        {
            get; init;
        }
    }

    /// <summary>The Motion attempts open to a side's vehicles now (D2.401), for the Play page: each vehicle on the map, with its bar or its chance; none outside the enemy MPh's window.</summary>
    public IReadOnlyList<MotionAttemptOption> MotionAttemptOptions(GameState state, IReadOnlyList<GameEvent> existing, string? side = null)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(existing);
        if (state.Phase != "mph" || state.Movement is not { WindowOpen: true } movement)
        {
            return [];
        }

        return [.. state.Units.Where(unit => unit.Status == InstanceStatus.Active && LiveFire.IsVehicle(unit) && unit.Position is MapPosition && unit.Side != state.PhasingSide
                && (side is null || unit.Side == side))
            .OrderBy(unit => unit.Id, StringComparer.Ordinal)
            .Select(unit =>
            {
                var read = MotionAttemptRead(state, existing, unit, unit.Id);
                return new MotionAttemptOption(unit.Id, read.Refusal, read.Mover?.Id, read.HalfPoints, movement.Vehicle, Turreted(unit), unit.Straddling is not null)
                {
                    BarShort = read.Short,
                };
            })];
    }

    private static bool Turreted(UnitInstance vehicle) => LiveFire.MaTypeOf(vehicle.Definition?.Definition) is "t" or "st" or "rst" or "1mt";

    /// <summary>The vehicle's counters as the attempt's bar reads them (D2.401; ruling R55.13).</summary>
    private static MotionAttemptVehicle MotionAttemptFacts(GameState state, UnitInstance vehicle) =>
        new(vehicle.Id, LiveFire.IsVehicle(vehicle) && vehicle.Position is MapPosition { Facing: not null }, vehicle.Side != state.PhasingSide)
        {
            Immobilized = Is(vehicle, Conditions.Immobilized),
            Bogged = Is(vehicle, Conditions.Bogged),
            Mired = Is(vehicle, Conditions.Mired),
            Stunned = Is(vehicle, Conditions.Stunned),
            StunRecovery = Is(vehicle, Conditions.StunRecovery),
            Recalled = Is(vehicle, Conditions.Recalled),
            Shocked = Is(vehicle, Conditions.Shocked),
            UnconfirmedKill = Is(vehicle, Conditions.UnconfirmedKill),
            Abandoned = Is(vehicle, Conditions.Abandoned),
            FireCounter = Is(vehicle, Conditions.FirstFire) ? "First Fire" : Is(vehicle, Conditions.FinalFire) ? "Final Fire" : Is(vehicle, Conditions.IntensiveFire) ? "Intensive Fire" : null,
            Attempted = state.MotionAttempts.Contains(vehicle.Id, StringComparer.Ordinal),
            Hidden = Is(vehicle, Conditions.Hidden),
        };

    /// <summary>
    /// What the vehicle sees of the moving units (D2.401; rulings R55.11, R55.12): for each, the LOS from the vehicle's Location to the Location
    /// it began its move in, which the game's history holds as its Location before its first expenditure of this MPh (a unit entering from off
    /// board had none), and each expenditure with the LOS to the Location it was made in. Rules picks the unit and counts. A LOS that is not
    /// decided is handed over as such for the starting Location; an expenditure in a Location with such a LOS is not counted.
    /// </summary>
    private (MotionAttemptMover? Mover, int HalfPoints, string? Refusal, string? Short) MotionAttemptRead(GameState state, IReadOnlyList<GameEvent> existing, UnitInstance? vehicle,
        string id)
    {
        var windowOpen = state.Movement is { WindowOpen: true };
        var facts = vehicle is null ? null : MotionAttemptFacts(state, vehicle);
        if (ScenarioA1MotionAttempts.Bar(id, state.Phase, windowOpen, facts) is { } bar)
        {
            return (null, 0, bar, ScenarioA1MotionAttempts.BarShort(id, state.Phase, windowOpen, facts));
        }

        var movement = state.Movement!;
        var at = state.Location(vehicle!.Id)!.Location;
        var history = Replay(existing);
        GameEvent[] phase = [.. ThisPhase(existing)];
        var seen = new Dictionary<BoardLocation, bool?>();
        bool? Seen(BoardLocation where)
        {
            if (!seen.TryGetValue(where, out var clear))
            {
                seen[where] = clear = where == at ? true : Los(state, at, where) switch
                {
                    { Status: LosStatus.Clear } => true,
                    { Status: LosStatus.Blocked } => false,
                    _ => null,
                };
            }

            return clear;
        }

        MotionAttemptMover[] movers = [.. movement.Movers.Where(mover => state.Unit(mover) is { Status: InstanceStatus.Active } unit && unit.Side == state.PhasingSide)
            .Select(mover =>
            {
                // D2.401 (pass 65, ruling R65.15): a unit that unloaded this MPh counts its time aboard: its vehicle's steps from its boarding (or the
                // phase's start) to its unloading, and its own MF to board and to disembark.
                var unloaded = phase.LastOrDefault(item => item.Payload is VehicleStepped { Kind: VehicleStepped.Unload } off && off.Units?.Contains(mover, StringComparer.Ordinal) == true);
                var carrier = (unloaded?.Payload as VehicleStepped)?.Vehicle;
                var boarded = carrier is null ? null : phase.LastOrDefault(item => item.Revision < unloaded!.Revision
                    && item.Payload is VehicleStepped { Kind: VehicleStepped.Load } on && on.Vehicle == carrier && on.Units?.Contains(mover, StringComparer.Ordinal) == true);
                bool Carried(GameEvent item) => carrier is not null && item.Payload is VehicleStepped carried && carried.Vehicle == carrier
                    && item.Revision >= (boarded?.Revision ?? 0) && item.Revision <= unloaded!.Revision;
                GameEvent[] steps = [.. phase.Where(item => item.Payload switch
                {
                    MovementStepped stepped => stepped.Movers.Contains(mover, StringComparer.Ordinal),
                    VehicleStepped drove => drove.Vehicle == mover || Carried(item),
                    _ => false,
                })];
                var origin = steps.Length > 0 ? history.At(steps[0].Revision - 1)?.Location(mover)?.Location : state.Location(mover)?.Location;
                return new MotionAttemptMover(mover, origin is null ? false : Seen(origin), [.. steps.Select(item => item.Payload switch
                {
                    MovementStepped stepped => (Seen(stepped.To) == true, stepped.HalfMf),
                    VehicleStepped drove when drove.Vehicle != mover => ScenarioA1MotionAttempts.AboardStep(Seen(drove.At) == true, drove.HalfMp,
                        ReferenceEquals(item, unloaded) || ReferenceEquals(item, boarded)),
                    VehicleStepped drove => (Seen(drove.At) == true, drove.HalfMp),
                    _ => (false, 0),
                })]);
            })];
        return ScenarioA1MotionAttempts.Mover(id, movement.Vehicle, movers);
    }

    private GamePlan PlanMotionAttempt(GameScope scope, JsonElement arguments, IReadOnlyList<GameEvent> existing, string attemptId, long expected, string label,
        string actor)
    {
        if (Replay(existing).Current is not { } state)
        {
            return Refused(scope, label, expected, "play.no-game: the game has no state yet");
        }

        if (!Text(arguments, "vehicleId", out var id))
        {
            return Refused(scope, label, expected, "play.invalid-arguments: a Motion attempt names the vehicle");
        }

        var vehicle = state.Unit(id) is { Status: InstanceStatus.Active } found ? found : null;
        var read = MotionAttemptRead(state, existing, vehicle, id);
        if (read.Refusal is not null || read.Mover is not { } mover)
        {
            return Refused(scope, label, expected, read.Refusal ?? "play.motion-attempt: no enemy unit is moving (D2.401)");
        }

        // D2.401: "allowed to freely change its VCA/TCA": the VCA and the TCA named are taken on a pass; Rules refuses what cannot be taken.
        var position = (MapPosition)vehicle!.Position;
        var current = position.Facing!.Value;
        var facingNamed = Text(arguments, "facing", out var facingName);
        UnitFacing parsedFacing = default;
        var facingRead = facingNamed && UnitFacings.TryParse(facingName, out parsedFacing);
        var turretNamed = Text(arguments, "turret", out var turretName);
        UnitFacing parsedTurret = default;
        var turretRead = turretNamed && UnitFacings.TryParse(turretName, out parsedTurret);
        if (ScenarioA1MotionAttempts.ChangeBar(id, facingNamed, facingRead, turretNamed, turretRead, Turreted(vehicle), vehicle.Straddling is not null) is { } changeBar)
        {
            return Refused(scope, label, expected, changeBar);
        }

        UnitFacing? facing = facingRead && parsedFacing != current ? parsedFacing : null;
        UnitFacing? turret = turretRead && !(facing is null && parsedTurret == LiveOrdnance.TurretFacing(state, vehicle)) ? parsedTurret : null;

        // D2.11, C5.11, D8.2: "one Bog DR per hexspine" of a VCA change in woods, a building, or rubble; D2.51: an AFV with red MP.
        var hexspines = facing is { } next ? ScenarioA1MotionAttempts.Hexspines((int)current, (int)next) : 0;
        var bogDrm = hexspines > 0 ? VehicleTurnCost(state, vehicle, position.Location).BogDrm : null;
        var unreliable = IsAfv(vehicle) && VehicleDefinition(vehicle)?.MechanicallyUnreliable == true;
        var halfPoints = read.HalfPoints;
        IReadOnlyList<GameEvent> Build(Func<RollRequest, RollResult> draw)
        {
            var drawn = draw(new RollRequest(1, 6));
            var rollId = $"{attemptId}-roll-1";
            var events = new List<GameEvent>
            {
                Event(scope, attemptId, 1, expected, "dice-rolled", new DiceRolled(rollId, ScenarioA1MotionAttempts.Purpose, 1, 6, drawn.Values, DiceRolled.SystemSource, actor), null, null),
            };
            var succeeded = ScenarioA1MotionAttempts.Succeeds(drawn.Values[0], halfPoints);

            // D2.401, D2.51: "a successful Motion Attempt results in neither Motion nor VCA change if any mechanical reliability DR results in
            // Immobilization"; then the Bog Check of each hexspine, which stops at the one that bogs the vehicle (ruling R55.13).
            var immobilized = succeeded && unreliable
                && AddVehicleCheck(scope, attemptId, expected, actor, events, draw, id, VehicleCheckRolled.Mechanical, 0) == VehicleCheckRolled.Immobilized;
            var bogged = false;
            if (succeeded && !immobilized && bogDrm is { } drm)
            {
                for (var spine = 0; spine < hexspines && !bogged; spine++)
                {
                    bogged = AddVehicleCheck(scope, attemptId, expected, actor, events, draw, id, VehicleCheckRolled.Bog, drm) == VehicleCheckRolled.Bogged;
                }
            }

            var result = ScenarioA1MotionAttempts.Result(succeeded, immobilized, bogged);
            var passed = result == MotionAttempted.Passed;
            events.Add(Event(scope, attemptId, events.Count + 1, expected, "motion-attempted", new MotionAttempted(id, rollId, halfPoints, result)
            {
                Mover = mover.Id,
                Facing = passed ? facing : null,
                Turret = passed ? turret : null,
            }, null, null, [EventId(attemptId, 1)]));

            // A12.141 (p. 78): "Any other activity which a concealed unit engages in ... causes loss of concealment if that unit is currently in
            // the LOS and within 16 hexes of a Good Order enemy ground unit", a change of CA among its examples; gaining Motion status is no
            // exception of its list. D2.401 spares the "?" only for a failed dr ("no penalty (including "?" loss) for failing").
            if (passed && Replay([.. existing, .. events]).Current is { } moved)
            {
                events.AddRange(RevealEvents(scope, attemptId, expected, events.Count + 1, VehicleConcealmentLost(moved, id, true)));
            }

            return events;
        }

        return new GamePlan(GamePlanStatus.Ready, scope, label, expected, [],
            [ScenarioA1MotionAttempts.Summary(id, mover.Id, halfPoints, state.Movement!.Vehicle, facing is { } vca ? UnitFacings.Name(vca) : null,
                turret is { } tca ? UnitFacings.Name(tca) : null, unreliable, bogDrm, hexspines)])
        {
            Roll = new PlannedRoll(ScenarioA1MotionAttempts.Purpose, Build),
            FirstEventId = EventId(attemptId, 1),
        };
    }
}
