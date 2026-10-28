using System.Globalization;
using System.Text.Json;
using LimboDancer.Dice;
using LimboDancer.Domains.Asl.Maps.Coordinates;
using LimboDancer.Domains.Asl.Rules;
using LimboDancer.Domains.Asl.Units.State;

namespace LimboDancer.Domains.Asl.Play;

/// <summary>
/// CC with vehicles (backlog pass 11, rulings R11.13 to R11.16): a DEFENDER unit's CC Reaction Fire at a vehicle in its Location in the MPh, after any
/// PAATC (D7.21, A11.6); and in the CCPh, the sequential CC of a Location holding a vehicle (A11.31): Infantry attack the vehicle (A11.5), the vehicle
/// attacks Infantry (A11.62), a side passes, and an unarmed vehicle alone with enemy Infantry is captured (A11.52).
/// </summary>
public sealed partial class GamePlanner
{
    /// <summary>Whether a vehicle has a CC attack (A11.62): a CMG not malfunctioned, or an AAMG while CE, with its crew able to fight.</summary>
    private static bool VehicleCcArmament(UnitInstance vehicle) => VehicleDefinition(vehicle) is var definition
        && ScenarioA1VehicleCloseCombatRules.VehicleCcArmament(definition is not null, Is(vehicle, Conditions.Stunned), Is(vehicle, Conditions.Shocked), Is(vehicle, Conditions.UnconfirmedKill),
            Is(vehicle, Conditions.Abandoned), Is(vehicle, Conditions.Recalled), Is(vehicle, Conditions.Captured), definition?.CoaxialMg is not null, Is(vehicle, Conditions.CmgMalfunctioned),
            definition?.AntiAircraftMg is not null, LiveFire.CrewExposed(vehicle), definition?.MainArmament == "aamg", Is(vehicle, Conditions.Malfunctioned), Is(vehicle, Conditions.Disabled));

    /// <summary>Whether an Infantry unit may make a CC attack in the CCPh (A11.16, A20.5): active, unbroken, and not a prisoner.</summary>
    private bool CcAttacker(UnitInstance unit) => ScenarioA1VehicleCloseCombatRules.CcAttacker(unit.Status == InstanceStatus.Active, LiveFire.IsVehicle(unit), vocabulary.IsA(unit.Kind, "asl:personnel"),
        Is(unit, Conditions.Broken), Is(unit, Conditions.Captured), unit.Kind == UnitKinds.Dummy);

    /// <summary>
    /// A unit of a Location holding a vehicle as Rules reads it (pass 32.g): its CC armament, attacker status, Personnel, prisoner, berserk, and Known
    /// flags (a Gun's crew is ordinary Infantry since pass 70, ruling R70.27).
    /// </summary>
    private VehicleCcUnitFacts VehicleCcUnit(UnitInstance unit) => new(unit.Id, unit.Side, LiveFire.IsVehicle(unit), VehicleCcArmament(unit), CcAttacker(unit),
        vocabulary.IsA(unit.Kind, "asl:personnel"), Is(unit, Conditions.Captured), Is(unit, Conditions.Berserk), KnownEnemy(unit));

    /// <summary>
    /// The units of a Location holding a vehicle as the sequential CC reads them (pass 70, rulings R70.30, R70.31), from the planner's index: the active
    /// units on foot and the vehicles, each vehicle armed also by the Passengers who join its attack (A11.62), and the Riders of each vehicle, which attack
    /// and are attacked in Infantry against Infantry CC (A11.611).
    /// </summary>
    private VehicleCcUnitFacts[] VehicleCcUnits(GameState state, BoardLocation location)
    {
        var index = MemoOf(state).Objects(state);
        var here = index.At(location).OfType<UnitInstance>().Where(unit => unit.Status == InstanceStatus.Active).ToArray();
        return [.. here.Select(unit => LiveFire.IsVehicle(unit) && !VehicleCcArmament(unit) && PassengersJoin(index, unit) ? VehicleCcUnit(unit) with { Armed = true } : VehicleCcUnit(unit)),
            .. here.Where(LiveFire.IsVehicle).SelectMany(vehicle => index.Aboard(vehicle.Id, ContainmentRole.Rider)).Select(rider => VehicleCcUnit(rider) with { Rider = true })];
    }

    /// <summary>
    /// Whether Passengers join a vehicle's CC attack on Infantry (A11.62; pass 70, ruling R70.31), read from the planner's index: some do, as Rules reads each
    /// one's part, and the vehicle is neither Stunned, Shocked, Abandoned, nor captured.
    /// </summary>
    private static bool PassengersJoin(ObjectIndex index, UnitInstance vehicle) =>
        ScenarioA1VehicleCloseCombatRules.PassengersJoin(Is(vehicle, Conditions.Stunned), Is(vehicle, Conditions.Shocked), Is(vehicle, Conditions.Abandoned), Is(vehicle, Conditions.Captured),
            LiveCloseCombat.JoiningPassengers(vehicle, index.Aboard(vehicle.Id)).Count);

    /// <summary>
    /// The sides with an attack left in a CC Location holding a vehicle (ruling R11.16): Infantry that have not attacked and face an enemy vehicle, and
    /// vehicles with CC armament that have not attacked and face enemy Infantry; a side that passed has none. Since pass 70 (ruling R70.30), Infantry facing
    /// enemy Infantry no Infantry has attacked (<paramref name="infantryAttacked"/>) have one too.
    /// </summary>
    private HashSet<string> SidesWithAttacks(GameState state, BoardLocation location, IReadOnlyCollection<string> attacked, IReadOnlyCollection<string> passed,
        IReadOnlyCollection<string> infantryAttacked)
    {
        return ScenarioA1VehicleCloseCombatRules.SidesWithAttacks(VehicleCcUnits(state, location), attacked, passed, infantryAttacked);
    }

    /// <summary>
    /// The side that attacks first in a CC Location holding a vehicle (A11.31): the non-vehicular side, or with vehicles on both sides the ATTACKER; after an
    /// Ambush there the ambusher (A11.34; pass 70, ruling R70.30).
    /// </summary>
    private static string FirstCcSide(GameState state, BoardLocation location)
    {
        return ScenarioA1VehicleCloseCombatRules.FirstCcSide([.. state.At(location).OfType<UnitInstance>().Where(unit => unit.Status == InstanceStatus.Active && !Is(unit, Conditions.Captured))
            .Select(unit => (unit.Side, LiveFire.IsVehicle(unit)))], state.PhasingSide!, state.CloseCombats.FirstOrDefault(item => item.Location == location)?.Ambusher);
    }

    /// <summary>
    /// After an attack or a pass (A11.31; ruling R11.16): the side to attack next, the other side while it has attacks left, else the same side, and
    /// whether the Location's CC is over; after an Ambush the ambusher while it has attacks left (A11.32, A11.34; pass 70, ruling R70.30).
    /// </summary>
    private (string? Next, bool Closed) NextCcSide(GameState after, BoardLocation location, string moved, IReadOnlyCollection<string> attacked, IReadOnlyCollection<string> passed,
        IReadOnlyCollection<string> infantryAttacked)
    {
        return ScenarioA1VehicleCloseCombatRules.NextCcSide(SidesWithAttacks(after, location, attacked, passed, infantryAttacked), moved,
            after.CloseCombats.FirstOrDefault(item => item.Location == location)?.Ambusher);
    }

    /// <summary>
    /// The unit (of <paramref name="side"/>, or of either side) that still owes an attack in a Location's sequential CC this CCPh, as Rules decides it
    /// (<see cref="ScenarioA1VehicleCloseCombatRules.OwingAttack"/>): a berserk unit with a Known enemy vehicle (A15.43, A11.31; ruling R27.2), or with Known
    /// enemy Infantry no Infantry has attacked, or a unit that advanced into the Melee there (A11.15; pass 70, fix round C). The units are the planner's
    /// index's, Riders among them. Null when none does.
    /// </summary>
    private (string Id, bool Berserk, string Target)? OwingSequentialAttack(GameState state, BoardLocation location, string? side)
    {
        var entry = state.CloseCombats.FirstOrDefault(item => item.Location == location);
        if (!ScenarioA1VehicleCloseCombatRules.OwingPossible(state.Phase, entry is { Closed: true }, VehicleCloseCombatTurn(state, location).Open))
        {
            return null;
        }

        var objects = MemoOf(state).Objects(state);
        return ScenarioA1VehicleCloseCombatRules.OwingAttack([.. VehicleCcUnits(state, location).Select(unit => objects.Unit(unit.Id) is { } found ? unit with
        {
            AdvancedHere = state.Advances.Any(item => item.Unit == unit.Id && item.To == location),
            Melee = Is(found, Conditions.Melee),
            Concealed = Is(found, Conditions.Concealed),
        } : unit)], side, entry?.Attacking ?? [], entry?.InfantryAttacked ?? []);
    }

    /// <summary>
    /// The Location of a berserk Infantry OVR onto a lone SMC whose CC is still to be resolved this MPh (A4.152, A15.432; ruling R27.3): the charging
    /// stack, with a berserk MMC (A15.432, A4.15: a SMC alone does not OVR; referee and table player, pass 27), has entered the Location of the charge,
    /// the DEFENDER's window on that entry has closed, the only enemy unit there is a Known SMC not held in Melee, and the Close Combat package
    /// accepts the CC (otherwise the units are simply left together for the CCPh, so the MPh never stalls; table player, pass 27). Null when there
    /// is none.
    /// </summary>
    public BoardLocation? BerserkOverrunPending(GameState state)
    {
        ArgumentNullException.ThrowIfNull(state);
        // A4.152, A20.54 (pass 40): the same holds for the entry of a declared Infantry OVR and of a recapture of Unarmed units.
        var movement = state.Movement;
        if ((movement?.Charge ?? movement?.InfantryOverrun) is not { } charged || !ScenarioA1VehicleCloseCombatRules.OverrunPendingPossible(state.Phase, movement!.WindowOpen, true,
            movement.Location == charged, state.CloseCombats.Any(item => item.Location == charged)))
        {
            return null;
        }

        var here = state.At(charged).OfType<UnitInstance>().Where(unit => unit.Status == InstanceStatus.Active && !Is(unit, Conditions.Captured)).ToArray();
        var enemies = here.Where(unit => unit.Side != state.PhasingSide).ToArray();
        var smc = enemies.Length > 0 ? enemies[0] : null;
        var owed = smc is not null && (movement.Charge is not null
            ? ScenarioA1VehicleCloseCombatRules.OverrunSmc(here.Any(unit => unit.Side == state.PhasingSide && movement.Members.Contains(unit.Id, StringComparer.Ordinal) && Is(unit, Conditions.Berserk)
                && vocabulary.IsA(unit.Kind, "asl:mmc")), enemies.Length, vocabulary.IsA(smc.Kind, "asl:smc"), KnownEnemy(smc), Is(smc, Conditions.Melee))
            : ScenarioA1InfantryOverrun.CloseCombatOwed(here.Any(unit => unit.Side == state.PhasingSide && movement.Members.Contains(unit.Id, StringComparer.Ordinal)
                && vocabulary.IsA(unit.Kind, "asl:mmc")), OverrunDefenders(state, charged, state.PhasingSide!)));
        if (smc is null || !owed)
        {
            return null;
        }

        var terrain = ReadLocation(state, charged) is { } read ? TerrainKey(read) : null;
        var (facts, _) = LiveCloseCombat.FromState(state, charged, terrain, OverrunAttacks(state, charged, smc), null, overrun: true);
        return facts is not null && ScenarioA1CloseCombatCalculator.Precheck(facts, CloseCombatReference.Value) is { Count: 0 } ? charged : null;
    }

    /// <summary>
    /// The attacks of a berserk OVR's CC (A4.152, A15.432; ruling R27.3): every berserk unit of the phasing side there attacks the SMC, and the SMC attacks
    /// them back when it may attack in CC (not broken or captured), which never costs it anything.
    /// </summary>
    public IReadOnlyList<CloseCombatDeclaration> OverrunAttacks(GameState state, BoardLocation location, UnitInstance smc)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(smc);
        // A4.152, A20.54 (pass 40): for a declared OVR or a recapture, the stack's units there attack the enemy units there.
        if (state.Movement is { Charge: null, InfantryOverrun: not null } entered)
        {
            UnitInstance[] present = [.. state.At(location).OfType<UnitInstance>().Where(unit => unit.Status == InstanceStatus.Active && !Is(unit, Conditions.Captured))];
            return ScenarioA1VehicleCloseCombatRules.EntryAttacks(
                // The stack's MMC make the attack the page first offers; a leader with them joins it only as the CC's stacking is declared (A11.14).
                [.. present.Where(unit => unit.Side == state.PhasingSide && entered.Members.Contains(unit.Id, StringComparer.Ordinal) && vocabulary.IsA(unit.Kind, "asl:mmc"))
                    .Select(unit => unit.Id).Order(StringComparer.Ordinal)],
                [.. present.Where(unit => unit.Side != state.PhasingSide).Select(unit => unit.Id).Order(StringComparer.Ordinal)],
                [.. present.Where(unit => unit.Side != state.PhasingSide && CcAttacker(unit)).Select(unit => unit.Id).Order(StringComparer.Ordinal)]);
        }

        string[] berserk = [.. state.At(location).OfType<UnitInstance>().Where(unit => unit.Status == InstanceStatus.Active && unit.Side == state.PhasingSide
            && Is(unit, Conditions.Berserk)).Select(unit => unit.Id).Order(StringComparer.Ordinal)];
        return ScenarioA1VehicleCloseCombatRules.OverrunAttacks(berserk, smc.Id, CcAttacker(smc));
    }

    /// <summary>
    /// The units of a Location holding a vehicle that its sequential CC reads, for the Play page's panel (pass 70, rulings R70.30, R70.31): the active units
    /// there and the vehicles, then each vehicle's Riders with the vehicle each rides, and the Passengers of each unarmored stationary vehicle, which an attack
    /// may try to capture (A11.52; ruling R70.34), from the planner's index (no scan of the state per render). The page reads its side's view of them.
    /// </summary>
    public IReadOnlyList<(UnitInstance Unit, string? Rides, bool Passenger)> VehicleCloseCombatUnits(GameState state, BoardLocation location)
    {
        ArgumentNullException.ThrowIfNull(state);
        var index = MemoOf(state).Objects(state);
        var here = index.At(location).OfType<UnitInstance>().Where(unit => unit.Status == InstanceStatus.Active).ToArray();
        return [.. here.Select(unit => (unit, (string?)null, false)),
            .. here.Where(LiveFire.IsVehicle).SelectMany(vehicle => index.Aboard(vehicle.Id, ContainmentRole.Rider).Select(rider => (rider, (string?)vehicle.Id, false))),
            .. here.Where(vehicle => LiveFire.IsVehicle(vehicle) && LiveCloseCombat.PassengersCapturable(vehicle))
                .SelectMany(vehicle => index.Aboard(vehicle.Id, ContainmentRole.Passenger).Where(unit => !Is(unit, Conditions.Captured)).Select(passenger => (passenger, (string?)vehicle.Id, true)))];
    }

    /// <summary>The sides and units a CC Location holding a vehicle waits for, for the Play page (ruling R11.16).</summary>
    public (string? Next, bool Open) VehicleCloseCombatTurn(GameState state, BoardLocation location)
    {
        ArgumentNullException.ThrowIfNull(state);
        var entry = state.CloseCombats.FirstOrDefault(item => item.Location == location);
        if (!ScenarioA1VehicleCloseCombatRules.TurnOpen(state.Phase, entry is { Closed: true }, state.At(location).OfType<UnitInstance>().Any(unit => unit.Status == InstanceStatus.Active && LiveFire.IsVehicle(unit))))
        {
            return (null, false);
        }

        return ScenarioA1VehicleCloseCombatRules.Turn(SidesWithAttacks(state, location, entry?.Attacking ?? [], entry?.Passed ?? [], entry?.InfantryAttacked ?? []),
            entry?.Next ?? FirstCcSide(state, location));
    }

    private GamePlan PlanVehicleCloseCombat(GameScope scope, JsonElement arguments, IReadOnlyList<GameEvent> existing, string attemptId, long expected, string label,
        string actor)
    {
        if (Replay(existing).Current is not { } state)
        {
            return Refused(scope, label, expected, "play.no-game: the game has no state yet");
        }

        if (!Text(arguments, "location", out var locationText) || !BoardLocation.TryParse(locationText, out var location))
        {
            return Refused(scope, label, expected, "play.invalid-arguments: CC names its Location");
        }

        var attackers = Strings(arguments, "attackers").ToArray();
        var defenders = Strings(arguments, "defenders").ToArray();
        var vehicleId = Text(arguments, "vehicleId", out var named) ? named : null;
        var pass = Flag(arguments, "pass");
        var capture = Flag(arguments, "capture");
        if (ScenarioA1VehicleCloseCombatRules.VehicleCcPhaseBar(state.Phase) is { } phaseBar)
        {
            return Refused(scope, label, expected, phaseBar);
        }

        // D10.1 (pass 65, ruling R65.23): CC at a wreck is pass 70's.
        if (vehicleId is not null && state.Unit(vehicleId) is { Status: InstanceStatus.Wrecked })
        {
            return Refused(scope, label, expected, ScenarioA1WreckRules.CloseCombatBar(vehicleId));
        }

        // A11.52, A21.2 (pass 65, ruling R65.22): a capture attempt is made in the CCPh, on an Abandoned AFV.
        if (capture && (ScenarioA1CapturedVehicleRules.CaptureAttemptPhaseBar(state.Phase)
            ?? (vehicleId is not null && state.Unit(vehicleId) is { } target
                ? ScenarioA1CapturedVehicleRules.CaptureAttemptBar(target.Id, Armed(VehicleDefinition(target)), Is(target, Conditions.Abandoned)) : null)) is { } captureBar)
        {
            return Refused(scope, label, expected, captureBar);
        }

        if (state.Phase == "mph")
        {
            return PlanReactionFire(scope, existing, attemptId, expected, label, actor, state, location, attackers, vehicleId);
        }

        // A11.31, A11.14, A11.141 (pass 70, ruling R70.30): with no vehicle named, Infantry attack enemy Infantry there, with the SMC stacking and director
        // the attack declares; A11.52, A20.22 (ruling R70.34): a capture attempt, the one way to attack a vehicle's Passengers there.
        if (vehicleId is null && !pass && attackers.Length > 0 && defenders.Length > 0)
        {
            return PlanInfantrySequential(scope, attemptId, expected, label, actor, state, location, attackers, defenders, Map(arguments, "stacking"),
                Text(arguments, "director", out var director) ? director : null, capture);
        }

        return PlanSequentialCloseCombat(scope, existing, attemptId, expected, label, actor, state, location, attackers, defenders, vehicleId, pass, capture);
    }

    /// <summary>
    /// CC Reaction Fire (D7.21, D7.213; ruling R11.13): in the DEFENDER's window after the moving vehicle's MP expenditure in their Location, one
    /// unbroken, unpinned, armed DEFENDER unit not in Melee, alone or with a SMC, attacks it, after a PAATC against a manned AFV unless exempt or passed
    /// this phase; units marked Final Fire (FPF CC Reaction Fire) are not built.
    /// </summary>
    private GamePlan PlanReactionFire(GameScope scope, IReadOnlyList<GameEvent> existing, string attemptId, long expected, string label, string actor,
        GameState state, BoardLocation location, string[] attackers, string? vehicleId)
    {
        var window = state.Movement;
        var vehicle = window is { Vehicle: true, Members: [{ } moving] } ? state.Unit(moving) : null;
        if (ScenarioA1VehicleCloseCombatRules.ReactionWindowBar(window is { WindowOpen: true, Vehicle: true, Members: [_] } && vehicle is not null && state.Location(vehicle.Id)?.Location == location
            && (vehicleId is null || vehicleId == vehicle.Id)) is { } windowBar)
        {
            return Refused(scope, label, expected, windowBar);
        }

        // D7.1, D7.2 (referee, pass 11): Reaction Fire at an OVRing vehicle comes after the OVR is resolved.
        if (ScenarioA1VehicleCloseCombatRules.ReactionOverrunBar(window!.Overrun is not null) is { } overrunBar)
        {
            return Refused(scope, label, expected, overrunBar);
        }

        var units = attackers.Select(state.Unit).ToArray();
        if (ScenarioA1VehicleCloseCombatRules.ReactionAttackersBar(units.Length, units.Any(unit => !ScenarioA1VehicleCloseCombatRules.ReactionAttacker(unit is { Status: InstanceStatus.Active }, unit?.Side != vehicle!.Side,
            unit is not null && state.Location(unit.Id)?.Location == location, unit is not null && LiveFire.IsVehicle(unit), unit is not null && Is(unit, Conditions.Broken),
            unit is not null && Is(unit, Conditions.Pinned), unit is not null && Is(unit, Conditions.Captured), unit is not null && Is(unit, Conditions.Melee))), location.ToString()) is { } attackersBar)
        {
            return Refused(scope, label, expected, attackersBar);
        }

        if (ScenarioA1VehicleCloseCombatRules.ReactionFinalFireBar(units.FirstOrDefault(unit => Is(unit!, Conditions.FinalFire))?.Id) is { } finalFireBar)
        {
            return Refused(scope, label, expected, finalFireBar);
        }

        return PlanVehicleAttack(scope, existing, attemptId, expected, label, actor, state, location, vehicle!, [.. units.Select(unit => unit!)], true, false);
    }

    /// <summary>
    /// Whether a vehicle may be the target of a capture attempt (A11.52, A21.2; pass 65, the UI review): Rules decides from whether it is armed and Abandoned,
    /// as the plan's own bar does. For the Play page's CC panel.
    /// </summary>
    public static bool MayBeCaptureAttempted(UnitInstance vehicle)
    {
        ArgumentNullException.ThrowIfNull(vehicle);
        return ScenarioA1CapturedVehicleRules.CaptureAttemptBar(vehicle.Id, Armed(VehicleDefinition(vehicle)), Is(vehicle, Conditions.Abandoned)) is null;
    }

    /// <summary>
    /// A11.52 (ruling R11.16): at the start of the CCPh an unarmed vehicle, not in Motion, with no Personnel of its side in its Location but enemy
    /// Infantry there, is captured. Records made before pass 65 marked it Captured and Abandoned; since then it passes to its captor with a Temporary
    /// Driver (A21.21; ruling R65.22).
    /// </summary>
    private IEnumerable<UnitInstance> CapturedVehicles(GameState state) => state.Units.Where(vehicle => ScenarioA1VehicleCloseCombatRules.CapturedVehicle(vehicle.Status == InstanceStatus.Active, LiveFire.IsVehicle(vehicle),
        Is(vehicle, Conditions.Captured), Is(vehicle, Conditions.Motion), VehicleDefinition(vehicle) is { MainArmament: null, AntiAircraftMg: null, CoaxialMg: null, BowMg: null },
        state.Location(vehicle.Id)?.Location is not null,
        state.Location(vehicle.Id)?.Location is { } at && state.At(at).OfType<UnitInstance>().Any(unit => unit.Status == InstanceStatus.Active && unit.Side == vehicle.Side && !LiveFire.IsVehicle(unit) && !Is(unit, Conditions.Captured)),
        state.Location(vehicle.Id)?.Location is { } there && state.At(there).OfType<UnitInstance>().Any(unit => unit.Status == InstanceStatus.Active && unit.Side != vehicle.Side && CcAttacker(unit)),
        // A11.52 (pass 70, ruling R70.34): a vehicle carrying Passengers is captured only with them, by CC.
        MemoOf(state).Objects(state).Aboard(vehicle.Id, ContainmentRole.Passenger).Any(unit => !Is(unit, Conditions.Captured))))
        .OrderBy(vehicle => vehicle.Id, StringComparer.Ordinal);

    /// <summary>
    /// The CCPh in a Location holding a vehicle (A11.31; rulings R11.14 to R11.16): the side named attacks once, Infantry against an enemy vehicle or a
    /// vehicle against enemy Infantry, or passes. Infantry against Infantry there is <see cref="PlanInfantrySequential"/>'s (pass 70, ruling R70.30).
    /// </summary>
    private GamePlan PlanSequentialCloseCombat(GameScope scope, IReadOnlyList<GameEvent> existing, string attemptId, long expected, string label, string actor,
        GameState state, BoardLocation location, string[] attackers, string[] defenders, string? vehicleId, bool pass, bool capture)
    {
        var (next, open) = VehicleCloseCombatTurn(state, location);
        if (!open || next is null)
        {
            return Refused(scope, label, expected, ScenarioA1VehicleCloseCombatRules.NoVehicleCcText(location.ToString()));
        }

        if (ScenarioA1VehicleCloseCombatRules.OtherOpenBar(state.CloseCombats.Any(item => !item.Closed && item.Location != location)) is { } otherBar)
        {
            return Refused(scope, label, expected, otherBar);
        }

        var entry = state.CloseCombats.FirstOrDefault(item => item.Location == location);
        var attacked = entry?.Attacking ?? [];
        var passed = entry?.Passed ?? [];
        if (pass)
        {
            // A15.43 (ruling R27.2): a side with a berserk unit facing a Known enemy vehicle there does not pass; the berserk unit attacks. Since pass 70 (fix
            // round C) nor one owing an attack on enemy Infantry there, or holding a unit that advanced into the Melee (A11.15).
            if (OwingSequentialAttack(state, location, next) is { } owing)
            {
                return Refused(scope, label, expected, ScenarioA1VehicleCloseCombatRules.OwingNoPassText(owing.Id, location.ToString(), owing.Berserk, owing.Target));
            }

            var (following, closed) = NextCcSide(state, location, next, attacked, [.. passed, next], entry?.InfantryAttacked ?? []);
            return new GamePlan(GamePlanStatus.Ready, scope, label, expected,
                [Event(scope, attemptId, 1, expected, "vehicle-close-combat-passed", new VehicleCloseCombatPassed(location, next, following, closed), null, null)],
                [ScenarioA1VehicleCloseCombatRules.PassSummary(next, location.ToString())]);
        }

        var vehicle = vehicleId is null ? null : state.Unit(vehicleId);
        if (ScenarioA1VehicleCloseCombatRules.VehicleNamedBar(vehicle is { Status: InstanceStatus.Active } && LiveFire.IsVehicle(vehicle) && state.Location(vehicle.Id)?.Location == location) is { } namedBar)
        {
            return Refused(scope, label, expected, namedBar);
        }

        if (defenders.Length > 0)
        {
            // A11.62 (ruling R11.15): the vehicle attacks enemy Infantry in its Location.
            if (ScenarioA1VehicleCloseCombatRules.VehicleAttackOrderBar(next, vehicle!.Side == next, attacked.Contains(vehicle.Id)) is { } orderBar)
            {
                return Refused(scope, label, expected, orderBar);
            }

            return PlanVehicleAttacksInfantry(scope, existing, attemptId, expected, label, actor, state, location, vehicle!, defenders, attacked, passed);
        }

        var units = attackers.Select(state.Unit).ToArray();
        if (ScenarioA1VehicleCloseCombatRules.InfantryAttackersBar(units.Length, units.Any(unit => !ScenarioA1VehicleCloseCombatRules.SequentialAttacker(unit is not null, unit is not null && CcAttacker(unit), unit?.Side == next,
            unit?.Side != vehicle!.Side, unit is not null && state.Location(unit.Id)?.Location == location, unit is not null && attacked.Contains(unit.Id))), next, location.ToString()) is { } attackersBar)
        {
            return Refused(scope, label, expected, attackersBar);
        }

        return PlanVehicleAttack(scope, existing, attemptId, expected, label, actor, state, location, vehicle!, [.. units.Select(unit => unit!)], false, capture);
    }

    /// <summary>
    /// One Infantry attack on Infantry in a Location holding a vehicle (A11.31, A11.12 and its EXC, the H3 example, pp. 72 to 74; pass 70, rulings R70.30,
    /// R70.31): the side named next attacks, with units that have not attacked this CCPh, enemy Infantry that no Infantry has attacked, a Rider of a vehicle
    /// there among either (A11.611) and no other PRC; there is no simultaneous reply. The Close Combat package resolves it as a sequential round, with the
    /// Ambush of the Location if one occurred (A11.34), and the record names the side that attacks next.
    /// </summary>
    private GamePlan PlanInfantrySequential(GameScope scope, string attemptId, long expected, string label, string actor, GameState state, BoardLocation location,
        string[] attackers, string[] defenders, IReadOnlyDictionary<string, string>? stacking, string? director, bool capture)
    {
        var (next, open) = VehicleCloseCombatTurn(state, location);
        if (!open || next is null)
        {
            return Refused(scope, label, expected, ScenarioA1VehicleCloseCombatRules.NoVehicleCcText(location.ToString()));
        }

        if (ScenarioA1VehicleCloseCombatRules.OtherOpenBar(state.CloseCombats.Any(item => !item.Closed && item.Location != location)) is { } otherBar)
        {
            return Refused(scope, label, expected, otherBar);
        }

        var entry = state.CloseCombats.FirstOrDefault(item => item.Location == location);
        var attacked = entry?.Attacking ?? [];
        var infantryAttacked = entry?.InfantryAttacked ?? [];
        var objects = MemoOf(state).Objects(state);

        // A11.611: of the PRC only a Rider fights apart; a Passenger or a crew inside shares its vehicle's fate.
        bool Here(UnitInstance unit) => objects.Location(unit.Id)?.Location == location && (objects.VehicleOf(unit.Id) is null || unit.Position is ContainedPosition { Role: ContainmentRole.Rider });
        var units = attackers.Select(objects.Unit).ToArray();
        if (ScenarioA1VehicleCloseCombatRules.InfantryRoundAttackersBar(attackers.Distinct(StringComparer.Ordinal).Count() != attackers.Length
            || units.Any(unit => !ScenarioA1VehicleCloseCombatRules.InfantryRoundAttacker(unit is { Status: InstanceStatus.Active }, unit is not null && CcAttacker(unit), unit?.Side == next,
                unit is not null && Here(unit), unit is not null && attacked.Contains(unit.Id))), next, location.ToString()) is { } attackersBar)
        {
            return Refused(scope, label, expected, attackersBar);
        }

        // A11.52 (ruling R70.34): an attack to capture may name the Passengers of an unarmored stationary vehicle there.
        UnitInstance? CapturableVehicle(UnitInstance unit) => unit.Position is ContainedPosition { Role: ContainmentRole.Passenger } aboard && objects.Unit(aboard.Container) is { } vehicle
            && objects.Location(vehicle.Id)?.Location == location && LiveCloseCombat.PassengersCapturable(vehicle) ? vehicle : null;
        var targets = defenders.Select(objects.Unit).ToArray();
        if (ScenarioA1VehicleCloseCombatRules.InfantryRoundDefendersBar(defenders.Distinct(StringComparer.Ordinal).Count() != defenders.Length
            || targets.Any(unit => !ScenarioA1VehicleCloseCombatRules.InfantryRoundDefender(unit is { Status: InstanceStatus.Active }, unit is not null && unit.Side != next,
                unit is not null && vocabulary.IsA(unit.Kind, "asl:personnel"), unit is not null && LiveFire.IsVehicle(unit), unit is not null && Is(unit, Conditions.Captured),
                unit is not null && (Here(unit) || (capture && CapturableVehicle(unit) is not null)), unit is not null && infantryAttacked.Contains(unit.Id))), location.ToString()) is { } defendersBar)
        {
            return Refused(scope, label, expected, defendersBar);
        }

        var terrain = ReadLocation(state, location) is { } read ? TerrainKey(read) : null;
        var declaration = new CloseCombatDeclaration(attackers, defenders)
        {
            Director = director,

            // A20.22: a capture attempt; with several defenders the defender's choice at the Kill Number is read in the order named (a reading of the page).
            Capture = capture ? true : null,
            Yield = capture && defenders.Length > 1 ? defenders : null,
        };
        var passengerVehicles = capture ? targets.OfType<UnitInstance>().Select(CapturableVehicle).OfType<UnitInstance>().DistinctBy(vehicle => vehicle.Id).ToArray() : [];
        var (facts, reason) = LiveCloseCombat.FromState(state, location, terrain, [declaration], stacking, null, CloseCombatFacts.Sequential);
        if (facts is null)
        {
            return Refused(scope, label, expected, reason!);
        }

        var reference = CloseCombatReference.Value;
        var precheck = ScenarioA1CloseCombatCalculator.Precheck(facts, reference);
        if (precheck.Count != 0)
        {
            return Refused(scope, label, expected, RefusalReasons.Refusal("play.cc-refused", "Close Combat", "round", precheck));
        }

        var package = ScenarioA1CloseCombatPackage.Identity.ToString();
        IReadOnlyList<GameEvent> Build(Func<RollRequest, RollResult> draw)
        {
            var events = new List<GameEvent>();
            var rollIds = new Dictionary<string, string>(StringComparer.Ordinal);
            var rolls = new CloseCombatRolls(null);
            CloseCombatResolution resolution;
            while ((resolution = ScenarioA1CloseCombatCalculator.Resolve(facts with
            {
                Rolls = rolls
            }, reference)).Disposition != CloseCombatResolution.Resolved)
            {
                var step = ScenarioA1CloseCombatRules.NextStep(resolution.Disposition, resolution.Reasons);
                if (step.Undecided is { } undecided)
                {
                    throw new InvalidOperationException("The Close Combat package left a sequential attack it had accepted undecided: " + undecided);
                }

                var (kind, rest, selected, count, purpose) = (step.Kind!, step.Rest!, step.Selected, step.Count, step.Purpose!);
                var drawn = draw(new RollRequest(count, 6));
                var rollId = $"{attemptId}-roll-{(rollIds.Count + 1).ToString(CultureInfo.InvariantCulture)}";
                rollIds[kind + ":" + rest] = rollId;
                events.Add(Event(scope, attemptId, events.Count + 1, expected, "dice-rolled",
                    new DiceRolled(rollId, purpose, count, 6, drawn.Values, DiceRolled.SystemSource, actor), package, null));
                rolls = kind switch
                {
                    "attack" => rolls with { Attacks = With(rolls.Attacks, rest, (IReadOnlyList<int>)drawn.Values) },
                    "randomSelection" => rolls with
                    {
                        RandomSelection = selected.Select((id, index) => (id, index)).Aggregate(rolls.RandomSelection,
                            (map, pair) => With(map, ScenarioA1CloseCombatRules.SelectionKey(rest, pair.id), drawn.Values[pair.index]))
                    },
                    "woundSeverity" => rolls with { WoundSeverity = With(rolls.WoundSeverity, rest, drawn.Values[0]) },
                    "leaderCreation" => rolls with { LeaderCreation = With(rolls.LeaderCreation, rest, drawn.Values[0]) },
                    "leaderStack" => rolls with
                    {
                        LeaderStack = selected.Select((id, index) => (id, index)).Aggregate(rolls.LeaderStack,
                            (map, pair) => With(map, ScenarioA1CloseCombatRules.SelectionKey(rest, pair.id), drawn.Values[pair.index]))
                    },
                    _ => rolls with { WeaponLoss = With(rolls.WeaponLoss, rest, drawn.Values[0]) },
                };
            }

            // A11.52 (ruling R70.34): a vehicle whose Passengers this attack tried to capture is captured once every one is, with no enemy Personnel left.
            var taken = passengerVehicles.Where(vehicle => CapturedWithPassengers(state, location, vehicle, resolution.Effects)).ToArray();

            // A11.31: the side that attacks next, read with the units this attack eliminated or captured gone, and a vehicle it captured the attackers'.
            var gone = resolution.Effects.Where(effect => effect.Eliminated || effect.Captured == true).Select(effect => effect.UnitId).ToHashSet(StringComparer.Ordinal);
            var after = gone.Count == 0 && taken.Length == 0 ? state : state with
            {
                Units = [.. state.Units.Select(unit => gone.Contains(unit.Id) ? unit with { Status = InstanceStatus.Eliminated }
                    : taken.Any(vehicle => vehicle.Id == unit.Id) ? unit with { Side = next } : unit)],
            };
            var (following, closed) = NextCcSide(after, location, next, [.. attacked, .. attackers], entry?.Passed ?? [], [.. infantryAttacked, .. defenders]);
            var recordId = EventId(attemptId, events.Count + 1);
            events.Add(Event(scope, attemptId, events.Count + 1, expected, "close-combat-resolved",
                new CloseCombatResolved(location, CloseCombatFacts.Sequential, attackers, defenders, rollIds,
                    JsonSerializer.SerializeToElement(facts, LiveFire.Json), JsonSerializer.SerializeToElement(resolution, LiveFire.Json))
                {
                    Next = following,
                    Closed = closed,
                    Captors = passengerVehicles.Length > 0 ? attackers : null,
                }, package, null));
            foreach (var (type, payload) in CloseCombatEffects(state, location, resolution, attemptId, []))
            {
                events.Add(Event(scope, attemptId, events.Count + 1, expected, type, payload, package, null, [recordId]));
            }

            // A20.5 (ruling R70.34): a Passenger captured leaves its vehicle for its Guard's Location.
            foreach (var effect in resolution.Effects.Where(effect => effect.Captured == true && objects.Unit(effect.UnitId)?.Position is ContainedPosition { Role: ContainmentRole.Passenger }))
            {
                var (captiveHalf, _) = ScenarioA1PrisonerCalculator.CapturedHalfIds(attemptId, effect.UnitId);
                var captive = effect.CapturedHalf == true ? captiveHalf : effect.UnitId;
                events.Add(Event(scope, attemptId, events.Count + 1, expected, "instance-moved", new InstanceMoved(captive, new MapPosition(location)) { Ended = true }, package, null, [recordId]));
            }

            // A11.52, A21.21 (ruling R70.34): an unarmed vehicle so captured takes its Temporary Driver as one captured alone with the captors' Infantry does; an
            // armed one is the captors', unmanned, until a crew enters it (A21.22), which a captor may do this CCPh (D5.42).
            foreach (var vehicle in taken)
            {
                var (cause, markAbandoned) = ScenarioA1CapturedVehicleRules.PassengerCapture(Armed(VehicleDefinition(vehicle)), Is(vehicle, Conditions.Abandoned));
                events.Add(Event(scope, attemptId, events.Count + 1, expected, "vehicle-captured", cause == ScenarioA1CapturedVehicleRules.Unarmed ? UnarmedCapture(state, vehicle)
                    : new VehicleCaptured(vehicle.Id, location, next, cause), package, null, [recordId]));
                if (markAbandoned)
                {
                    events.Add(Event(scope, attemptId, events.Count + 1, expected, "conditions-changed",
                        new ConditionsChanged(vehicle.Id, new Dictionary<string, ConditionState> { [Conditions.Abandoned] = ConditionState.True }), package, null, [recordId]));
                }
            }

            return events;
        }

        return new GamePlan(GamePlanStatus.Ready, scope, label, expected, [],
            [ScenarioA1VehicleCloseCombatRules.InfantryRoundSummary(attackers, defenders, location.ToString(), entry?.Ambusher)
                + (passengerVehicles.Length > 0 ? ScenarioA1CapturedVehicleRules.PassengerCaptureText() : string.Empty)])
        {
            Roll = new PlannedRoll("cc", Build),
            FirstEventId = EventId(attemptId, 1),
        };
    }

    /// <summary>
    /// Whether an attack to capture a vehicle's Passengers captures the vehicle (A11.52; pass 70, ruling R70.34), as Rules reads it from the planner's index and
    /// the attack's effects: the Passengers it captured and eliminated, those left aboard (a squad captured at the Kill Number leaves its other HS), and the
    /// vehicle side's Personnel left in the Location, on foot or as Riders.
    /// </summary>
    private bool CapturedWithPassengers(GameState state, BoardLocation location, UnitInstance vehicle, IReadOnlyList<CloseCombatUnitEffect> effects)
    {
        var index = MemoOf(state).Objects(state);
        var aboard = index.Aboard(vehicle.Id).Where(unit => unit.Status == InstanceStatus.Active && unit.Position is ContainedPosition { Role: ContainmentRole.Passenger }
            && !Is(unit, Conditions.Captured)).ToArray();
        var outcome = aboard.Select(unit => effects.FirstOrDefault(effect => effect.UnitId == unit.Id)).ToArray();
        var captured = outcome.Count(effect => effect?.Captured == true);
        var left = outcome.Count(effect => effect is null || (!effect.Eliminated && effect.Captured != true)) + outcome.Count(effect => effect?.CapturedHalf == true);
        var here = index.At(location).OfType<UnitInstance>().Where(unit => !LiveFire.IsVehicle(unit))
            .Concat(index.At(location).OfType<UnitInstance>().Where(LiveFire.IsVehicle).SelectMany(other => index.Aboard(other.Id, ContainmentRole.Rider)));
        var enemyLeft = here.Any(unit => unit.Status == InstanceStatus.Active && unit.Side == vehicle.Side && vocabulary.IsA(unit.Kind, "asl:personnel") && !Is(unit, Conditions.Captured)
            && effects.FirstOrDefault(effect => effect.UnitId == unit.Id) is not ({ Eliminated: true } or { Captured: true }));
        return ScenarioA1CapturedVehicleRules.CapturedWithPassengers(captured, outcome.Any(effect => effect?.Eliminated == true), left, enemyLeft);
    }

    /// <summary>The Infantry of a CC attack on a vehicle, with each unit's Inexperience as A19.2 reads it (ruling R11.14).</summary>
    private VehicleCloseCombatFacts WithInexperience(GameState state, VehicleCloseCombatFacts facts) => ScenarioA1VehicleCloseCombatRules.WithInexperience(facts,
        id => (state.Unit(id) is { } found ? Experience.Inexperienced(state, found, catalogs, vocabulary) : ConditionState.Unknown) switch
        {
            ConditionState.True => true,
            ConditionState.False => false,
            _ => null,
        });

    /// <summary>
    /// Infantry attack a vehicle (A11.5; rulings R11.13, R11.14): in CC Reaction Fire a MMC takes its PAATC first against a manned AFV (a failure pins
    /// it and ends the attack); the package resolves the attack; a destroyed vehicle leaves a wreck (A11.611: its crew does not survive), an
    /// immobilized one loses Motion; the attackers of CC Reaction Fire are marked First Fire (Final Fire when already First Fire) and, while the vehicle
    /// survives, with a CC counter.
    /// </summary>
    private GamePlan PlanVehicleAttack(GameScope scope, IReadOnlyList<GameEvent> existing, string attemptId, long expected, string label, string actor,
        GameState state, BoardLocation location, UnitInstance vehicle, IReadOnlyList<UnitInstance> units, bool reaction, bool capture)
    {
        var (facts, reason) = LiveCloseCombat.VehicleFromState(state, location, vehicle.Id, [.. units.Select(unit => unit.Id)], [], false, reaction);
        if (facts is null)
        {
            return Refused(scope, label, expected, reason!);
        }

        // A21.2 (pass 65, ruling R65.22): the declaration of a capture attempt. A11.8, A18.12 (pass 70, rulings R70.33, R70.34): in the CCPh, the attackers that
        // qualify for Street Fighting, and the Leader Creation an Original 2 by a MMC draws. A18.2 (pass 71, ruling R71.17): with it, whether the vehicle is
        // an AFV, for the table's "-1 CC vs AFV".
        facts = WithInexperience(state, facts) with
        {
            Capture = capture ? true : null,
            StreetFighters = reaction ? null : StreetFighters(state, location, [.. units.Select(unit => unit.Id)]),
            LeaderCreation = reaction ? null : true,
            AfvDrm = ScenarioA1VehicleCloseCombat.AfvDrmDeclared(!reaction, LiveFire.UnarmoredOf(vehicle.Definition?.Definition)),
        };
        var reference = CloseCombatReference.Value;
        var precheck = ScenarioA1VehicleCloseCombat.Precheck(facts, reference);
        if (precheck.Count != 0)
        {
            return Refused(scope, label, expected, RefusalReasons.Refusal("play.cc-vehicle-refused", "Close Combat", "attack", precheck));
        }

        var testing = reaction ? units.Where(unit => NeedsPaatc(state, unit, vehicle)).ToArray() : [];
        var entry = state.CloseCombats.FirstOrDefault(item => item.Location == location);
        var package = ScenarioA1CloseCombatPackage.Identity.ToString();
        IReadOnlyList<GameEvent> Build(Func<RollRequest, RollResult> draw)
        {
            var events = new List<GameEvent>();
            if (testing.Length > 0)
            {
                AddPaatc(scope, attemptId, expected, actor, state, testing, vehicle, location, events, draw, out var passed);
                if (!passed)
                {
                    return events;
                }
            }

            var rollIds = new Dictionary<string, string>(StringComparer.Ordinal);
            var rolls = new VehicleCloseCombatRolls(null);
            VehicleCloseCombatResolution resolution;
            while ((resolution = ScenarioA1VehicleCloseCombat.Resolve(facts with
            {
                Rolls = rolls
            }, reference)).Disposition != CloseCombatResolution.Resolved)
            {
                if (ScenarioA1VehicleCloseCombatRules.VehicleRollMissing(resolution.Reasons) is not { } asked)
                {
                    throw new InvalidOperationException(ScenarioA1VehicleCloseCombatRules.VehicleAttackUndecidedText(resolution.Reasons));
                }

                var (key, kind, rest) = asked;
                var count = ScenarioA1VehicleCloseCombatRules.VehicleAttackDice(kind);
                var drawn = draw(new RollRequest(count, 6));
                var rollId = $"{attemptId}-roll-{(events.Count(item => item.Payload is DiceRolled) + 1).ToString(CultureInfo.InvariantCulture)}";
                rollIds[key] = rollId;
                events.Add(Event(scope, attemptId, events.Count + 1, expected, "dice-rolled",
                    new DiceRolled(rollId, "cc-vehicle-" + kind, count, 6, drawn.Values, DiceRolled.SystemSource, actor), package, null));
                rolls = ScenarioA1VehicleCloseCombatRules.WithVehicleAttackRoll(rolls, kind, rest, drawn.Values);
            }

            var destroyed = ScenarioA1VehicleCloseCombatRules.Destroyed(resolution.VehicleResult);
            var captured = resolution.VehicleResult == VehicleCloseCombatResolution.Captured;

            // A18.12 (pass 70, ruling R70.34): the leader an Original 2 created made the attack with his MMC, so he has attacked.
            var leader = resolution.LeaderCreation?.LeaderDefinitionId is not null ? ScenarioA1PrisonerCalculator.CreatedLeaderId(attemptId, 0) : null;
            var (following, closed) = reaction ? (null, false) : NextAfter(state, location, vehicle, units, destroyed, entry, captured, leader);
            var recordId = EventId(attemptId, events.Count + 1);
            events.Add(Event(scope, attemptId, events.Count + 1, expected, "vehicle-close-combat-resolved",
                new VehicleCloseCombatResolved(location, vehicle.Id, [.. units.Select(unit => unit.Id)], [], false, reaction, rollIds,
                    JsonSerializer.SerializeToElement(facts, LiveFire.Json), JsonSerializer.SerializeToElement(resolution, LiveFire.Json))
                {
                    Next = following,
                    Closed = closed,

                    // A9.223 (pass 60, rulings R60.3, R60.20): a reaction planned from the pass on carries the Fire Lane's cancellation.
                    CancelsFireLanes = reaction,
                    CreatedLeader = leader,
                }, package, null));

            // A18.12: the created leader is placed in the Location, as the CC of Infantry places one (Good Order, unconcealed, Fanatic with a Fanatic MMC).
            if (leader is not null)
            {
                var founder = units.FirstOrDefault(unit => !vocabulary.IsA(unit.Kind, "asl:smc")) ?? units[0];
                events.Add(Event(scope, attemptId, events.Count + 1, expected, "instance-created", new InstanceCreated(new NewInstance(leader, "asl:leader",
                    resolution.LeaderCreation!.LeaderDefinitionId, founder.Side, new MapPosition(location), null,
                    ConditionChanges(ScenarioA1PrisonerCalculator.CreatedCcLeaderConditions(Is(founder, Conditions.Fanatic))))), package, null, [recordId]));
            }
            if (destroyed)
            {
                var burning = resolution.VehicleResult == VehicleCloseCombatResolution.BurningWreck;
                events.Add(Event(scope, attemptId, events.Count + 1, expected, "vehicle-wrecked", new VehicleWrecked(vehicle.Id, burning), package, null, [recordId]));

                // B25.14 (p. 143; pass 35, task 35.9): a Wreck Blaze is a Blaze counter on the wreck, as fire and ordnance place it; without it the wreck
                // gave cover and no smoke.
                if (burning && state.Location(vehicle.Id)?.Location is { } burningAt)
                {
                    events.Add(Event(scope, attemptId, events.Count + 1, expected, "instance-created",
                        new InstanceCreated(new NewInstance(BlazeId(vehicle.Id), "asl:fire", null, null, new MapPosition(burningAt), null, new Dictionary<string, ConditionState>())),
                        package, null, [recordId]));
                }
            }
            else if (captured)
            {
                // A21.2 (pass 65, ruling R65.22): the capture attempt's success; the AFV passes to the attackers' side, still Abandoned.
                events.Add(Event(scope, attemptId, events.Count + 1, expected, "vehicle-captured",
                    new VehicleCaptured(vehicle.Id, location, units[0].Side, VehicleCaptured.CloseCombat), package, null, [recordId]));
            }
            else if (resolution.VehicleResult == VehicleCloseCombatResolution.Immobilized)
            {
                events.Add(Event(scope, attemptId, events.Count + 1, expected, "conditions-changed", new ConditionsChanged(vehicle.Id,
                    ConditionChanges(ScenarioA1VehicleCloseCombatRules.ImmobilizedConditions())), package, null, [recordId]));
            }

            foreach (var (type, payload) in CloseCombatEffects(state, location, new CloseCombatResolution(CloseCombatResolution.Resolved, [], [], resolution.Effects, [], []), attemptId, []))
            {
                events.Add(Event(scope, attemptId, events.Count + 1, expected, type, payload, package, null, [recordId]));
            }

            // A15.46 (pass 72, ruling R72.7): the berserk attackers that destroyed the vehicle and left no other Known enemy unit in the Location return to normal.
            foreach (var id in BerserkReturnsAfterVehicleKill(state, location, vehicle, units, destroyed, resolution))
            {
                events.Add(Event(scope, attemptId, events.Count + 1, expected, "conditions-changed",
                    new ConditionsChanged(id, ConditionChanges(ScenarioA1SequenceCalculator.BerserkEndsConditions())), package, null, [recordId]));
            }

            // D7.21: the attackers of CC Reaction Fire carry a First Fire counter (Final Fire when already marked First Fire), and a CC counter while the
            // vehicle survives.
            if (reaction)
            {
                foreach (var unit in units.Where(unit => ScenarioA1VehicleCloseCombatRules.Survives(resolution.Effects, unit.Id)))
                {
                    var conditions = ConditionChanges(ScenarioA1VehicleCloseCombatRules.ReactionFireConditions(Is(unit, Conditions.FirstFire), destroyed));
                    events.Add(Event(scope, attemptId, events.Count + 1, expected, "conditions-changed", new ConditionsChanged(unit.Id, conditions), package, null, [recordId]));

                    // D7.21 (pass 60, ruling R60.20): its SW and Guns carry the CC counter too.
                    var weaponConditions = ConditionChanges(ScenarioA1VehicleCloseCombatRules.ReactionWeaponConditions(destroyed));
                    foreach (var weapon in state.Equipment.Where(item => item.Status == InstanceStatus.Active && item.Holding is { Role: HoldingRole.Possessed or HoldingRole.Manned } holding
                        && holding.Holder == unit.Id && weaponConditions.Count > 0).OrderBy(item => item.Id, StringComparer.Ordinal))
                    {
                        events.Add(Event(scope, attemptId, events.Count + 1, expected, "conditions-changed", new ConditionsChanged(weapon.Id, weaponConditions), package, null, [recordId]));
                    }
                }
            }

            return events;
        }

        var paatc = ScenarioA1VehicleCloseCombatRules.PaatcText([.. testing.Select(unit => unit.Id)]);
        return new GamePlan(GamePlanStatus.Ready, scope, label, expected, [],
            [ScenarioA1VehicleCloseCombatRules.VehicleAttackSummary([.. units.Select(unit => unit.Id)], vehicle.Id, reaction, paatc)
                + (capture ? $" as a capture attempt: a result that would eliminate or burn {vehicle.Id} captures it (A21.2)" : string.Empty)
                + (ScenarioA1StreetFighting.Applies(facts.Ambusher, facts.StreetFighters, units.Select(unit => unit.Id)) ? ScenarioA1StreetFighting.DrmText(false) : string.Empty)])
        {
            Roll = new PlannedRoll("cc-vehicle", Build),
            FirstEventId = EventId(attemptId, 1),
        };
    }

    /// <summary>
    /// The berserk attackers of a CC attack on a vehicle that return to normal (A15.46; pass 72, ruling R72.7), as Rules decides it: the vehicle destroyed, the
    /// berserk attackers the attack left, and, only when Rules asks, the Known enemy units of the Location as the sequential CC reads them (the planner's
    /// index, Riders among them), the vehicle read as gone, as <see cref="NextAfter"/> reads it.
    /// </summary>
    private string[] BerserkReturnsAfterVehicleKill(GameState state, BoardLocation location, UnitInstance vehicle, IReadOnlyList<UnitInstance> attackers, bool destroyed,
        VehicleCloseCombatResolution resolution)
    {
        var lost = resolution.Effects.Where(effect => effect.Eliminated).Select(effect => effect.UnitId).ToHashSet(StringComparer.Ordinal);
        string[] berserk = [.. attackers.Where(unit => Is(unit, Conditions.Berserk) && !lost.Contains(unit.Id)).Select(unit => unit.Id).Order(StringComparer.Ordinal)];
        var side = attackers[0].Side;
        KnownEnemyCount Known()
        {
            VehicleCcUnitFacts[] known = [.. VehicleCcUnits(state, location).Where(unit => unit.Side != side && unit.Known)];
            return new KnownEnemyCount(known.Length, known.Count(unit => unit.Id != vehicle.Id && !lost.Contains(unit.Id)));
        }

        return [.. ScenarioA1SequenceCalculator.BerserkReturnsAfterAttack(new BerserkAttackFacts(state.Phase, false, destroyed, berserk), Known)];
    }

    /// <summary>
    /// The next side after an attack on a vehicle, reading a destroyed vehicle as gone (ruling R11.16), and a captured one as the attackers' (A21.2; pass 65,
    /// ruling R65.22).
    /// </summary>
    private (string? Next, bool Closed) NextAfter(GameState state, BoardLocation location, UnitInstance vehicle, IReadOnlyList<UnitInstance> attackers, bool destroyed,
        CloseCombatLocation? entry, bool captured, string? createdLeader)
    {
        var after = destroyed || captured ? state with
        {
            Units = [.. state.Units.Select(unit => unit.Id != vehicle.Id ? unit : destroyed ? unit with { Status = InstanceStatus.Wrecked } : unit with { Side = attackers[0].Side })]
        } : state;
        return NextCcSide(after, location, attackers[0].Side, [.. entry?.Attacking ?? [], .. attackers.Select(unit => unit.Id), .. createdLeader is null ? [] : new[] { createdLeader }],
            entry?.Passed ?? [], entry?.InfantryAttacked ?? []);
    }

    /// <summary>
    /// A vehicle's CC attack on Infantry (A11.62; ruling R11.15): the package resolves it on the CCT; its effects follow as Infantry CC effects.
    /// </summary>
    private GamePlan PlanVehicleAttacksInfantry(GameScope scope, IReadOnlyList<GameEvent> existing, string attemptId, long expected, string label, string actor,
        GameState state, BoardLocation location, UnitInstance vehicle, string[] defenders, IReadOnlyList<string> attacked, IReadOnlyList<string> passed)
    {
        var (facts, reason) = LiveCloseCombat.VehicleFromState(state, location, vehicle.Id, [], defenders, true, false);
        if (facts is null)
        {
            return Refused(scope, label, expected, reason!);
        }

        // A11.8 (pass 70, ruling R70.33): the defenders that qualify for Street Fighting, against whom the vehicle's attack takes +1.
        facts = WithInexperience(state, facts) with
        {
            StreetFighters = StreetFighters(state, location, defenders),
        };
        var reference = CloseCombatReference.Value;
        var precheck = ScenarioA1VehicleCloseCombat.Precheck(facts, reference);
        if (precheck.Count != 0)
        {
            return Refused(scope, label, expected, RefusalReasons.Refusal("play.cc-vehicle-refused", "Close Combat", "attack", precheck));
        }

        var package = ScenarioA1CloseCombatPackage.Identity.ToString();
        IReadOnlyList<GameEvent> Build(Func<RollRequest, RollResult> draw)
        {
            var events = new List<GameEvent>();
            var rollIds = new Dictionary<string, string>(StringComparer.Ordinal);
            var rolls = new VehicleCloseCombatRolls(null);
            VehicleCloseCombatResolution resolution;
            while ((resolution = ScenarioA1VehicleCloseCombat.Resolve(facts with
            {
                Rolls = rolls
            }, reference)).Disposition != CloseCombatResolution.Resolved)
            {
                if (ScenarioA1VehicleCloseCombatRules.VehicleRollMissing(resolution.Reasons) is not { } asked)
                {
                    throw new InvalidOperationException(ScenarioA1VehicleCloseCombatRules.VehicleOwnAttackUndecidedText(resolution.Reasons));
                }

                var (key, kind, rest) = asked;
                var selected = ScenarioA1VehicleCloseCombatRules.RandomSelected(kind, rest);
                var count = ScenarioA1VehicleCloseCombatRules.VehicleInfantryDice(kind, selected.Length);
                var drawn = draw(new RollRequest(count, 6));
                var rollId = $"{attemptId}-roll-{(events.Count(item => item.Payload is DiceRolled) + 1).ToString(CultureInfo.InvariantCulture)}";
                rollIds[key] = rollId;
                events.Add(Event(scope, attemptId, events.Count + 1, expected, "dice-rolled",
                    new DiceRolled(rollId, "cc-vehicle-" + kind, count, 6, drawn.Values, DiceRolled.SystemSource, actor), package, null));
                rolls = ScenarioA1VehicleCloseCombatRules.WithVehicleInfantryRoll(rolls, kind, rest, selected, drawn.Values);
            }

            var after = state with
            {
                Units = [.. state.Units.Select(unit => resolution.Effects.FirstOrDefault(effect => effect.UnitId == unit.Id) is { Eliminated: true } ? unit with { Status = InstanceStatus.Eliminated } : unit)],
            };
            var (following, closed) = NextCcSide(after, location, vehicle.Side, [.. attacked, vehicle.Id], passed,
                state.CloseCombats.FirstOrDefault(item => item.Location == location)?.InfantryAttacked ?? []);
            var recordId = EventId(attemptId, events.Count + 1);
            events.Add(Event(scope, attemptId, events.Count + 1, expected, "vehicle-close-combat-resolved",
                new VehicleCloseCombatResolved(location, vehicle.Id, [vehicle.Id], defenders, true, false, rollIds,
                    JsonSerializer.SerializeToElement(facts, LiveFire.Json), JsonSerializer.SerializeToElement(resolution, LiveFire.Json))
                {
                    Next = following,
                    Closed = closed,
                }, package, null));
            foreach (var (type, payload) in CloseCombatEffects(state, location, new CloseCombatResolution(CloseCombatResolution.Resolved, [], [], resolution.Effects, [], []), attemptId, []))
            {
                events.Add(Event(scope, attemptId, events.Count + 1, expected, type, payload, package, null, [recordId]));
            }

            return events;
        }

        return new GamePlan(GamePlanStatus.Ready, scope, label, expected, [],
            [ScenarioA1VehicleCloseCombatRules.VehicleAttacksInfantrySummary(vehicle.Id, defenders)
                + ScenarioA1VehicleCloseCombatRules.PassengersJoinText([.. (facts.Passengers ?? []).Select(unit => unit.UnitId!)])
                + (defenders.Any(id => ScenarioA1StreetFighting.Applies(facts.Ambusher, facts.StreetFighters, [id])) ? ScenarioA1StreetFighting.DrmText(true) : string.Empty)])
        {
            Roll = new PlannedRoll("cc-vehicle", Build),
            FirstEventId = EventId(attemptId, 1),
        };
    }
}
