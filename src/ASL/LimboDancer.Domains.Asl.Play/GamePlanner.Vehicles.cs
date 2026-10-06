using LimboDancer.Dice;
using System.Globalization;
using System.Text.Json;
using System.Text.Json.Nodes;
using LimboDancer.Domains.Asl.Maps.Coordinates;
using LimboDancer.Domains.Asl.Rules;
using LimboDancer.Domains.Asl.Units.Documents;
using LimboDancer.Domains.Asl.Units.State;

namespace LimboDancer.Domains.Asl.Play;

/// <summary>
/// Vehicles in live play (unit step 25, rulings R25.3 and R25.8 to R25.10; backlog pass 11, rulings R11.1 to R11.12): a vehicle's MP expenditures in
/// its MPh, each opening the DEFENDER's window (D2.1, A8.1): Start, forward or in Reverse (D2.12, D2.23), with a Bog Removal (D8.3) or a Mechanical
/// Reliability DR (D2.51); a VCA change of one hexspine (D2.11); entering a hex forward, in Reverse, or in VBM over the Terrain Chart's costs, with any
/// Bog DR, as a Minimum Move, or with an OVR (D2.2, D2.3, D2.15, D7.1, D8.2); ESB (D2.5); Stop (D2.13); the end of its move in Motion or stopped
/// (D2.4); and its crew buttoning up or exposing itself (D5.33).
/// </summary>
public sealed partial class GamePlanner
{
    // A vehicle's facing is a hexspine (D2.11), whose direction counterclockwise from east is 60 degrees per UnitFacing step.
    private static double FacingDegrees(UnitFacing facing) => ScenarioA1Geometry.FacingDegrees((int)facing);

    private static FireDefinition? VehicleDefinition(UnitInstance vehicle) =>
        vehicle.Definition is { } definition ? FireReference.Value.Definitions.GetValueOrDefault(definition.Definition) : null;

    private static bool IsAfv(UnitInstance vehicle) => LiveFire.IsVehicle(vehicle) && VehicleDefinition(vehicle) is { Unarmored: false };

    /// <summary>Whether a vehicle is a closed-topped AFV (D1.23; ruling R7.11): armored and not open-topped.</summary>
    public static bool IsClosedTopped(UnitInstance vehicle) => IsAfv(vehicle) && VehicleDefinition(vehicle) is { OpenTopped: not true };

    /// <summary>The half MP a vehicle has spent this MPh and its allotment, with the MP a successful ESB added (D1.1, D2.5).</summary>
    private static (int Spent, int Allotment) HalfMp(UnitInstance vehicle) =>
        ((vehicle.MfSpent * 2) + (vehicle.HalfMfSpent ? 1 : 0), ((VehicleDefinition(vehicle)?.MovementPoints ?? 0) + vehicle.EsbMp) * 2);

    /// <summary>A vehicle's printed MP allotment in half MP (D1.1).</summary>
    private static int PrintedHalfMp(UnitInstance vehicle) => (VehicleDefinition(vehicle)?.MovementPoints ?? 0) * 2;

    /// <summary>
    /// The two ADJACENT Locations a vehicle's VCA points at (D2.11, C3.2): the neighbors whose bearing is 30 degrees either side of its
    /// facing hexspine, read in the map's frame on every board, reversed or not (rulings R8.7, R26.6).
    /// </summary>
    private IReadOnlyList<BoardLocation> VcaHexes(GameState state, BoardLocation at, UnitFacing facing)
    {
        var face = FacingDegrees(facing);
        return [.. Neighbors(state, at).Where(next => Bearing(state, at, next) is { } bearing
            && Math.Abs(Math.Abs(((bearing - face + 540) % 360) - 180) - 30) < 1)];
    }

    /// <summary>
    /// The half MP of an outright forward entry by a vehicle over terrain a Recall route may take (ruling R5.17): no Bog DR and no ALL entry; null
    /// otherwise.
    /// </summary>
    private int? VehicleEntryCost(GameState state, UnitInstance vehicle, BoardLocation from, BoardLocation to) =>
        VehicleOutright(state, vehicle, from, to, false, false).Entry is { All: false, BogDrm: null } entry ? entry.HalfMp : null;

    /// <summary>
    /// Why the set-up vehicles are outside the reviewed cases (rulings R25.3, R25.10, R11.6, R11.7), or null: a vehicle is placed on the map with a VCA,
    /// at its hex center, not concealed or hidden outside Concealment Terrain (A12.2), at ground level in Open Ground, Grain, brush, woods, or a road
    /// hex, and with no enemy unit in its Location.
    /// </summary>
    private string? VehicleSetupBar(GameState state)
    {
        foreach (var vehicle in state.Units.Where(unit => unit.Status == InstanceStatus.Active && LiveFire.IsVehicle(unit)))
        {
            // A2.52 (ruling R26.1): a vehicle may wait off board to enter, in Motion, loaded up to its capacity, its Gun in tow.
            if ((TowSetupBar(state, vehicle) ?? PassengerSetupBar(state, vehicle)) is { } loadBar)
            {
                return loadBar;
            }

            if (vehicle.Position is OffMapPosition)
            {
                continue;
            }

            if (vehicle.Position is not MapPosition { Facing: not null } || state.Location(vehicle.Id) is not { } at)
            {
                return $"play.setup-vehicle: {vehicle.Id} is set up on the map with its VCA facing a hexspine, or off board to enter (D2.11, A2.52)";
            }

            if ((Is(vehicle, Conditions.Concealed) || Is(vehicle, Conditions.Hidden)) && !VehicleConcealmentTerrain(state, at.Location))
            {
                return $"play.setup-vehicle: {vehicle.Id} sets up concealed or hidden only in Concealment Terrain, which for a vehicle here is grain in season (A12.2, A12.12, B15.6; ruling R6.7)";
            }

            if (at.Location.Level != 0 || ReadLocation(state, at.Location) is not { } read || TerrainKey(read) is not ("open-ground" or "grain" or "brush" or "woods"))
            {
                return $"play.setup-vehicle: {vehicle.Id} sets up at ground level in Open Ground, Grain, brush, woods, or a road hex, the terrain the Vehicle rules reviewed (rulings R25.3, R11.7)";
            }

            if (state.At(at.Location).OfType<UnitInstance>().Any(unit => unit.Status == InstanceStatus.Active && unit.Id != vehicle.Id && unit.Side != vehicle.Side))
            {
                return $"play.setup-vehicle: {vehicle.Id} shares {at.Location} with an enemy unit at setup, which is not reviewed (ruling R25.3)";
            }
        }

        return null;
    }

    /// <summary>
    /// Why a vehicle's Passengers at setup are refused (A2.52, D6.1; ruling R26.2): they are Personnel of its side and OB group, on foot nowhere else,
    /// within its capacity; null when they are not.
    /// </summary>
    private string? PassengerSetupBar(GameState state, UnitInstance vehicle)
    {
        var riding = state.Passengers(vehicle.Id);
        if (riding.Count == 0)
        {
            return null;
        }

        if (riding.FirstOrDefault(unit => unit.Side != vehicle.Side || unit.Group != vehicle.Group || LiveFire.IsVehicle(unit) || !vocabulary.IsA(unit.Kind, "asl:personnel")) is { } stranger)
        {
            return $"play.setup-passenger: {stranger.Id} sets up as a Passenger of {vehicle.Id}, which takes Personnel of its own side and OB group (A2.52; ruling R26.2)";
        }

        if (riding.FirstOrDefault(unit => Is(unit, Conditions.Concealed) || Is(unit, Conditions.Hidden)) is { } hiding)
        {
            return $"play.setup-passenger: {hiding.Id} sets up as a Passenger, neither under \"?\" nor hidden (ruling R26.2)";
        }

        return CapacityBar(state, vehicle, []) is { } full ? $"play.setup-passenger: {full}" : null;
    }

    /// <summary>
    /// Why a Gun set up in tow is refused (C10.1, C10.2, C10.13; ruling R26.1): the vehicle's T# exceeds the Gun's M#, the Gun is not QSU (limbering is not
    /// built), more than one Gun is in tow, or the ammunition leaves the vehicle's Passengers no room; null when none is.
    /// </summary>
    private static string? TowSetupBar(GameState state, UnitInstance vehicle)
    {
        EquipmentInstance[] towed = [.. state.Equipment.Where(item => item.Status == InstanceStatus.Active && item.Holding is { Role: HoldingRole.Towed } tow && tow.Holder == vehicle.Id)];
        if (towed.Length == 0)
        {
            return null;
        }

        if (towed.Length > 1)
        {
            return $"play.setup-tow: {vehicle.Id} tows one Gun (C10.1)";
        }

        var gun = towed[0];
        if (VehicleDefinition(vehicle)?.Towing is not { } towing || OrdnanceReference.Value.Guns.GetValueOrDefault(gun.Definition?.Definition ?? string.Empty) is not { Manhandling: { } manhandling }
            || towing > manhandling)
        {
            return $"play.setup-tow: {vehicle.Id} tows {gun.Id} only when it has a T# no greater than the Gun's M# (C10.1)";
        }

        if (gun.Definition is not { } reference || FireReference.Value.Definitions.GetValueOrDefault(reference.Definition)?.QuickSetUp != true)
        {
            return $"play.setup-tow: {gun.Id} is not QSU, and limbering is not built, so it is not set up in tow (C10.2; ruling R26.1)";
        }

        return null;
    }

    /// <summary>Whether a unit is an AFV (D1.2): a vehicle whose AF is printed.</summary>
    public static bool IsArmoredVehicle(UnitInstance unit) => IsAfv(unit);

    /// <summary>Whether a vehicle has a MG that fires on the IFT (ruling R25.7): the SPW 251/1's MA AAMG; a truck is unarmed (D5.1).</summary>
    public static bool HasVehicleMg(UnitInstance unit) => LiveFire.IsVehicle(unit) && VehicleDefinition(unit) is { MainArmament: "aamg", AntiAircraftMg: not null };

    /// <summary>The half MP a vehicle has spent this MPh and its allotment in half MP (D1.1, D2.5), for the Play page.</summary>
    public static (int Spent, int Allotment) VehicleHalfMp(UnitInstance vehicle) => HalfMp(vehicle);

    /// <summary>
    /// The entries a vehicle may make now (rulings R11.1, R11.2, R11.7), each with its cost in half MP and whether it needs a Bog DR, is an ALL entry,
    /// or is a VBM, or null with the reason it may not, for the Play page.
    /// </summary>
    public IReadOnlyList<(BoardLocation To, BoardLocation? Straddling, int? HalfMp, string? Bar, bool All, int? BogDrm)> VehicleEntries(GameState state, UnitInstance vehicle,
        bool allMp = false)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(vehicle);
        var reverse = state.Movement is { Vehicle: true, Reverse: true } movement && movement.Members.Contains(vehicle.Id, StringComparer.Ordinal);
        return [.. VehicleMoveOptions(state, vehicle, reverse, allMp).Select(option =>
        {
            var bar = option.Reason ?? VehicleEntryBar(state, vehicle, option.To);
            return (option.To, option.Straddling, bar is null ? option.Entry?.HalfMp : null, bar, option.Entry?.All ?? false, option.Entry?.BogDrm);
        })];
    }

    /// <summary>An active vehicle of a side other than the one named, in a Location, or null.</summary>
    private static UnitInstance? EnemyVehicleAt(GameState state, string side, BoardLocation at) =>
        state.At(at).OfType<UnitInstance>().FirstOrDefault(unit => unit.Status == InstanceStatus.Active && unit.Side != side && LiveFire.IsVehicle(unit));

    /// <summary>
    /// The plan of a vehicle's MP expenditure (rulings R6.6, R11.9 to R11.12): its events, built with the rolls they need (Bog, Bog Removal, Mechanical
    /// Reliability), and the Residual FP attack on it where it spends the MP (A8.2, A8.222): an unarmored vehicle on the Vehicle line, an AFV's
    /// Vulnerable crew Collaterally, and a BU AFV not at all; the attack comes first, alone. <paramref name="build"/> is called with no draw to read
    /// the step before any roll.
    /// </summary>
    private GamePlan VehicleStepPlan(GameScope scope, string label, string attemptId, long expected, string actor, IReadOnlyList<GameEvent> existing,
        Func<Func<RollRequest, RollResult>?, List<GameEvent>> build, bool rolls, BoardLocation at, int step, string summary)
    {
        var state = Replay(existing).Current!;
        var provisional = build(null);
        FireAttack? residualFacts = null;
        if (state.ResidualFire.FirstOrDefault(item => item.Location == at) is { } residual)
        {
            if (Replay([.. existing, .. provisional]).Current is not { } entered
                || LiveFire.ResidualFromState(entered, at, residual.Fp) is not ({ } residualAttack, null)
                || FireMapFacts(entered, residualAttack, at) is not ({ } mapFacts, null))
            {
                return Refused(scope, label, expected, "play.move-vehicle-residual: the Residual FP attack on the vehicle cannot be read");
            }

            // A8.222: an AFV with no Vulnerable crew, and no Infantry with it, is not attacked at all.
            if (residualAttack.Targets is { Count: > 0 } || (residualAttack.Vehicles ?? []).Any(item => item.CrewExposed == true
                || FireReference.Value.Definitions.GetValueOrDefault(item.DefinitionId!) is { Unarmored: true }))
            {
                residualFacts = HeatOfBattleFacts(entered, mapFacts);
                var precheck = ScenarioA1FireCalculator.Precheck(residualFacts, FireReference.Value);
                if (precheck.Count != 0)
                {
                    return Refused(scope, label, expected, ["play.move-vehicle-residual: the Fire package does not decide the Residual FP attack this MP expenditure would suffer", .. precheck]);
                }
            }
        }

        if (residualFacts is null && !rolls)
        {
            return new GamePlan(GamePlanStatus.Ready, scope, label, expected, provisional, [summary]);
        }

        IReadOnlyList<GameEvent> Build(Func<RollRequest, RollResult> draw)
        {
            var events = build(draw);
            if (residualFacts is not null && Replay([.. existing, .. events]).Current is { } entered
                && LiveFire.ResidualFromState(entered, at, state.ResidualFire.First(item => item.Location == at).Fp) is ({ } attack, null)
                && FireMapFacts(entered, attack, at) is ({ } map, null))
            {
                AddFireEvents(scope, attemptId, expected, actor, entered, HeatOfBattleFacts(entered, map), state.PhasingSide, step, events, draw);
            }

            return events;
        }

        return new GamePlan(GamePlanStatus.Ready, scope, label, expected, [],
            residualFacts is null ? [summary] : [summary, $"play.move-vehicle: Residual FP in {at} attacks the vehicle first (A8.2)"])
        {
            Roll = new PlannedRoll(residualFacts is null ? "vehicle-check" : "residual", Build),
            FirstEventId = EventId(attemptId, 1),
        };
    }

    /// <summary>
    /// A vehicle check's dice and record (rulings R11.3, R11.4, R11.9, R11.10): a DR drawn now, or the pass assumed when no draw is given; the events
    /// are added to <paramref name="events"/> and the result returned.
    /// </summary>
    private string AddVehicleCheck(GameScope scope, string attemptId, long expected, string actor, List<GameEvent> events, Func<RollRequest, RollResult>? draw,
        string vehicle, string check, int drm, int mp = 0, Func<IReadOnlyList<int>, int>? final = null)
    {
        if (draw is null)
        {
            return check == VehicleCheckRolled.BogRemoval ? VehicleCheckRolled.Freed : VehicleCheckRolled.Passed;
        }

        var drawn = draw(new RollRequest(2, 6));
        var rollId = $"{attemptId}-roll-{(events.Count(item => item.Payload is DiceRolled) + 1).ToString(CultureInfo.InvariantCulture)}";
        events.Add(Event(scope, attemptId, events.Count + 1, expected, "dice-rolled",
            new DiceRolled(rollId, "vehicle-" + check, 2, 6, drawn.Values, DiceRolled.SystemSource, actor), null, null));
        var total = (final ?? (dice => dice[0] + dice[1]))(drawn.Values) + drm;
        var result = VehicleCheckRolled.For(check, total);
        events.Add(Event(scope, attemptId, events.Count + 1, expected, "vehicle-check-rolled", new VehicleCheckRolled(vehicle, check, rollId, drm, result) { Mp = mp },
            null, null, [EventId(attemptId, events.Count)]));
        return result;
    }

    /// <summary>
    /// Why a vehicle may not enter a Location it could otherwise enter (rulings R11.6, R25.10), or null: a Location with enemy units held in Melee is
    /// not reviewed; any number of vehicles of either side may share a Location, and a vehicle may enter one holding enemy units (D2.1).
    /// </summary>
    private static string? VehicleEntryBar(GameState state, UnitInstance vehicle, BoardLocation to) =>
        state.At(to).OfType<UnitInstance>().Any(unit => unit.Status == InstanceStatus.Active && Is(unit, Conditions.Melee))
            ? "units there are held in Melee, and a vehicle's entry into a Melee Location is not reviewed"
            : null;

    /// <summary>The events of this phase so far.</summary>
    private static IEnumerable<GameEvent> ThisPhase(IReadOnlyList<GameEvent> existing)
    {
        var since = existing.Select((item, index) => (item, index)).LastOrDefault(pair => pair.item.Payload is PhaseChanged or GameStarted).index;
        return existing.Skip(since);
    }

    private static VehicleStepped[] StepsThisPhase(IReadOnlyList<GameEvent> existing, string vehicle) =>
        [.. ThisPhase(existing).Select(item => item.Payload).OfType<VehicleStepped>().Where(step => step.Vehicle == vehicle)];

    /// <summary>
    /// Whether the vehicle has spent no MP this MPh but a Start MP and has not changed its VCA (D2.15, D2.7; ruling R11.5): only then may it make an ALL
    /// entry or a Minimum Move.
    /// </summary>
    private static bool FirstEntry(IReadOnlyList<GameEvent> existing, string vehicle) =>
        StepsThisPhase(existing, vehicle).All(step => step.Kind == VehicleStepped.Start && !step.BogRemoval);

    /// <summary>Whether the vehicle made an ALL entry this MPh, after which it may only Stop or end in Motion (D2.7; ruling R11.5).</summary>
    private static bool AfterAllEntry(IReadOnlyList<GameEvent> existing, UnitInstance vehicle) =>
        StepsThisPhase(existing, vehicle.Id).Any(step => step.Kind == VehicleStepped.Enter && step.All);

    private static bool Flag(JsonElement arguments, string name) => arguments.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.True;

    private GamePlan PlanMoveVehicle(GameScope scope, JsonElement arguments, IReadOnlyList<GameEvent> existing, string attemptId, long expected, string label,
        string actor)
    {
        if (Replay(existing).Current is not { } state)
        {
            return Refused(scope, label, expected, "play.no-game: the game has no state yet");
        }

        if (!Text(arguments, "vehicleId", out var id) || !Text(arguments, "kind", out var kind))
        {
            return Refused(scope, label, expected, "play.invalid-arguments: a vehicle's move names the vehicle and the kind of MP expenditure");
        }

        // A2.52, D2.4 (ruling R26.1): a vehicle waiting off board enters in Motion.
        if (state.Unit(id) is { Status: InstanceStatus.Active, Position: OffMapPosition } waiting && LiveFire.IsVehicle(waiting))
        {
            return PlanVehicleEntry(scope, arguments, existing, attemptId, expected, label, actor, state, waiting, kind);
        }

        if (state.Phase != "mph" || state.Unit(id) is not { Status: InstanceStatus.Active } vehicle || !LiveFire.IsVehicle(vehicle)
            || vehicle.Side != state.PhasingSide || state.Location(vehicle.Id) is not { } at || vehicle.Position is not MapPosition { Facing: { } facing }
            || VehicleDefinition(vehicle) is not { MovementPoints: not null } definition)
        {
            return Refused(scope, label, expected, "play.move-vehicle: a vehicle of the phasing side on the map moves in its MPh (D2.1)");
        }

        if (vehicle.MovementEnded)
        {
            return Refused(scope, label, expected, $"play.move-vehicle: {id} has ended its move this MPh (A4.2)");
        }

        // E1.52 (referee, pass 16): an AFV whose NVR is 0 spends no MP while BU but to Stop.
        if (IsAfv(vehicle) && Is(vehicle, Conditions.ButtonedUp) && NvrOf(state, vehicle) == 0 && kind != VehicleStepped.Stop)
        {
            return Refused(scope, label, expected, $"play.night-bu: {id} is BU with an NVR of 0 and spends no MP but to Stop (E1.52)");
        }

        if (new[] { (Conditions.PrepFire, "it Prep Fired (D.3)"), (Conditions.Immobilized, "it is immobilized (D.7)"),
            (Conditions.Stunned, "its crew is Stunned (D5.34)"), (Conditions.Shocked, "it is Shocked (C7.42)"),
            (Conditions.UnconfirmedKill, "it is an Unconfirmed Kill, still Shocked (C7.42)"), (Conditions.Abandoned, "it is Abandoned (D5.41)"),
            ("asl:ti", "it is TI after hooking up a Gun (C10.11)") }
            // D6.5, D6.1 (referee, pass 26): Passengers leave a vehicle that Prep Fired, is immobilized, or is Abandoned.
            .Where(item => kind != VehicleStepped.Unload || item.Item1 is not (Conditions.PrepFire or Conditions.Immobilized or Conditions.Abandoned))
            .FirstOrDefault(item => Is(vehicle, item.Item1)) is { Item2: { } why })
        {
            return Refused(scope, label, expected, $"play.move-vehicle: {id} may not move: {why}");
        }

        // D5.341: a Recall stops the AFV like a Stun for the rest of that Player Turn; once its counter shows Recall; +1 it must leave.
        var leaving = MustLeave(vehicle);
        if (Is(vehicle, Conditions.Recalled) && !leaving)
        {
            return Refused(scope, label, expected, $"play.move-vehicle: {id} may not move: it is Recalled and stopped for the rest of this Player Turn (D5.341)");
        }

        var current = state.Movement;
        if (current is not null && (!current.Vehicle || !current.Members.SequenceEqual([vehicle.Id], StringComparer.Ordinal)))
        {
            return Refused(scope, label, expected, $"play.move-order: {string.Join(", ", current.Members)} moves until its move ends (A4.2)");
        }

        if (current is { WindowOpen: true })
        {
            return Refused(scope, label, expected, current.Reaction
                ? "play.move-window: the DEFENDER may still make Reaction Fire at the OVRing vehicle (D7.2)"
                : "play.move-window: the DEFENDER may still fire at the vehicle's last MP expenditure (A8.1, A8.11)");
        }

        if (current?.Overrun is { } declared)
        {
            return Refused(scope, label, expected, $"play.move-vehicle-ovr: {id} resolves its OVR of {declared} before it spends more MP (D7.1)");
        }

        // A15.43: at the start of the MPh every berserk unit charges before any other unit moves.
        if (current is null && MustCharge(state) is [{ } charging, ..])
        {
            return Refused(scope, label, expected, $"play.berserk-first: {charging.Id} is berserk and charges before any other unit moves (A15.43)");
        }

        // D8.2, D8.3 (ruling R11.10): a bogged vehicle's only expenditure is its Bog Removal, as the Start MP of its MPh.
        var bogged = Is(vehicle, Conditions.Bogged);
        if (bogged && (kind != VehicleStepped.Start || current is not null))
        {
            return Refused(scope, label, expected, $"play.move-vehicle-bog: {id} is bogged; it may only attempt Bog Removal as its first expenditure of its MPh (D8.2, D8.3)");
        }

        var moving = current is not null ? current.Started && !current.Stopped : Is(vehicle, Conditions.Motion);
        var reverse = current is { Reverse: true } && moving;
        var (spent, allotment) = HalfMp(vehicle);
        var step = (current?.Step ?? 0) + 1;
        var afterAll = AfterAllEntry(existing, vehicle);
        switch (kind)
        {
            case VehicleStepped.Start:
                return PlanStartVehicle(scope, arguments, existing, attemptId, expected, label, actor, state, vehicle, at.Location, moving, bogged, leaving, step);
            case "esb":
                return PlanEsb(scope, arguments, existing, attemptId, expected, label, actor, state, vehicle, moving, afterAll);
            case VehicleStepped.Overrun:
                return PlanDeclareOverrun(scope, existing, attemptId, expected, label, actor, state, vehicle, at.Location, moving, step);
            case VehicleStepped.Load:
                return PlanLoad(scope, arguments, existing, attemptId, expected, label, actor, state, vehicle, at.Location);
            case VehicleStepped.Unload:
                return PlanUnload(scope, arguments, existing, attemptId, expected, label, actor, state, vehicle, at.Location, step);
        }

        if (!moving)
        {
            return Refused(scope, label, expected, $"play.move-vehicle: {id} is not moving; it must start first (D2.12)");
        }

        if (afterAll && kind != VehicleStepped.Stop)
        {
            return Refused(scope, label, expected, $"play.move-vehicle-all: {id} made an ALL entry, so it may only Stop or end its move in Motion (D2.7)");
        }

        BoardLocation to = at.Location;
        UnitFacing? turned = null;
        BoardLocation? straddling = vehicle.Straddling;
        int cost;
        string summary;
        VehicleEntry? entry = null;
        (int? Drm, IReadOnlyList<string> Causes) turnBog = (null, []);
        var overrun = false;
        var minimumMove = false;
        string? exitEdge = null;
        switch (kind)
        {
            case VehicleStepped.Turn:
                if (!Text(arguments, "facing", out var facingName) || !UnitFacings.TryParse(facingName, out var next)
                    || Math.Abs(((int)next - (int)facing + 6) % 6) is not (1 or 5))
                {
                    return Refused(scope, label, expected, "play.move-vehicle: a VCA change turns one hexspine (D2.11)");
                }

                // D2.33 (ruling R11.2): in Bypass, one VCA change at the CAFP, after which the vehicle must move on.
                if (vehicle.Straddling is not null && TurnedAtCafp(state, vehicle))
                {
                    return Refused(scope, label, expected, $"play.move-vehicle-bypass: {id} changed its VCA at its CAFP and must now move on in Bypass (D2.33)");
                }

                turned = next;
                var turnCost = VehicleTurnCost(state, vehicle, at.Location);
                cost = turnCost.HalfMp;
                turnBog = (turnCost.BogDrm, turnCost.Causes);
                summary = $"{id} changes its VCA to {UnitFacings.Name(next)} in {at.Location} for {Mp(cost)} MP (D2.11)"
                    + (turnBog.Drm is { } turnDrm ? $", with a Bog DR at {turnDrm:+0;-0;0} (D8.2)" : string.Empty);
                break;
            case VehicleStepped.Enter:
                if (!Text(arguments, "to", out var toText) || !BoardLocation.TryParse(toText, out var entered))
                {
                    return Refused(scope, label, expected, "play.invalid-arguments: an entry names the Location entered");
                }

                to = entered;

                // A2.1 (ruling R20.6): never out of the card's playable area.
                if (PlayableBar(state, entered) is { } outside)
                {
                    return Refused(scope, label, expected, outside);
                }

                var bypass = Flag(arguments, "bypass");
                overrun = Flag(arguments, "overrun");
                minimumMove = Flag(arguments, "minimumMove");
                var options = VehicleMoveOptions(state, vehicle, reverse, Flag(arguments, "allMp"));
                var option = options.FirstOrDefault(item => item.To == to && (item.Straddling is not null) == bypass);
                if (option.To is null)
                {
                    var offered = options.Select(item => item.Straddling is null ? item.To.ToString() : $"{item.To} in VBM").Distinct().ToArray();
                    return Refused(scope, label, expected, offered.Length == 0
                        ? $"play.move-vehicle: {id} has no entry {(reverse ? "in Reverse " : string.Empty)}from here (D2.11, D2.22, D2.33)"
                        : $"play.move-vehicle: {id} enters {(reverse ? "in Reverse " : string.Empty)}only {string.Join(" or ", offered)} from here (D2.11, D2.22, D2.33)");
                }

                if (option.Entry is not { } found)
                {
                    return Refused(scope, label, expected, $"play.move-vehicle-terrain: {id} may not enter {to}{(bypass ? " in VBM" : string.Empty)}: {option.Reason}");
                }

                if (VehicleEntryBar(state, vehicle, to) is { } bar)
                {
                    return Refused(scope, label, expected, $"play.move-vehicle: {id} may not enter {to}: {bar}");
                }

                entry = found;
                straddling = entry.Straddling;
                var printed = PrintedHalfMp(vehicle);
                var first = FirstEntry(existing, vehicle.Id);
                if (entry.All && !first)
                {
                    return Refused(scope, label, expected, $"play.move-vehicle-all: entering {to} takes {id}'s whole MP allotment, so it is its first expenditure after its Start MP (B13.41, D2.7; ruling R11.5)");
                }

                // D2.15 (ruling R11.5): a Minimum Move enters a hex whose cost exceeds the printed allotment, spending that allotment.
                if (minimumMove)
                {
                    if (entry.HalfMp <= printed || entry.All)
                    {
                        return Refused(scope, label, expected, $"play.move-vehicle-minimum: {to} costs {Mp(entry.HalfMp)} MP, no more than {id}'s allotment, so it needs no Minimum Move (D2.15)");
                    }

                    if (!first || reverse || entry.Bypass)
                    {
                        return Refused(scope, label, expected, $"play.move-vehicle-minimum: a Minimum Move is a vehicle's only entry of its MPh, made forward and not in VBM, with no VCA change (D2.15, D2.24; ruling R11.5)");
                    }
                }
                else if (entry.HalfMp > printed && !entry.All)
                {
                    return Refused(scope, label, expected, $"play.move-vehicle-mp: {to} costs {Mp(entry.HalfMp)} MP, more than {id}'s allotment; only a Minimum Move enters it (D2.15)");
                }

                cost = entry.All || minimumMove ? printed : entry.HalfMp;

                // D7.1, D7.13 (ruling R11.11): an OVR, declared with the entry, costs a quarter of the printed allotment (FRU) more; D2.7 (referee, pass 11):
                // not with an ALL entry or a Minimum Move, which leave no MP for it.
                if (overrun)
                {
                    if (OverrunBar(state, existing, vehicle, to, reverse, entry.Bypass) is { } refused)
                    {
                        return Refused(scope, label, expected, refused);
                    }

                    if (entry.All || minimumMove)
                    {
                        return Refused(scope, label, expected, $"play.move-vehicle-ovr: an ALL entry or a Minimum Move spends {id}'s whole allotment, leaving no MP for an OVR (D2.7, D7.1)");
                    }

                    cost += OverrunHalfMp(vehicle);
                }

                summary = $"{id} enters {to}{(entry.Bypass ? $" in VBM, straddling the hexside with {entry.Straddling}" : string.Empty)}{(reverse ? " in Reverse" : string.Empty)} for {Mp(cost)} MP"
                    + (entry.All ? " (ALL, B13.41)" : minimumMove ? " (Minimum Move, D2.15)" : string.Empty)
                    + (overrun ? $", with an OVR ({Mp(OverrunHalfMp(vehicle))} MP, D7.1)" : string.Empty)
                    + (entry.BogDrm is { } entryBog ? $"; Bog DR at {entryBog:+0;-0;0} (D8.21)" : string.Empty);
                break;
            case VehicleStepped.Stop:
                if (leaving)
                {
                    return Refused(scope, label, expected, $"play.recall-motion: {id} is Recalled and leaves in Motion, so it does not Stop (D5.341)");
                }

                if (EnemyAfvBar(state, vehicle, existing) is { } blocked && MayMoveOn(state, vehicle))
                {
                    return Refused(scope, label, expected, "play.move-vehicle-enemy-afv: " + blocked);
                }

                if (vehicle.Straddling is not null && TurnedAtCafp(state, vehicle) && MayMoveOn(state, vehicle))
                {
                    return Refused(scope, label, expected, $"play.move-vehicle-bypass: {id} changed its VCA at its CAFP and must now move on in Bypass (D2.33)");
                }

                cost = 2;
                summary = $"{id} stops in {at.Location} for 1 MP (D2.13)";
                break;
            case VehicleStepped.Exit:
                // A2.6: an exit from an edge hex into the mirror image of that hex, within its VCA; a Recalled AFV leaves by its Friendly Board Edge.
                if (reverse || vehicle.Straddling is not null)
                {
                    return Refused(scope, label, expected, $"play.move-vehicle-exit: {id} exits forward from its hex center, not in Reverse or Bypass (A2.6; ruling R11.1)");
                }

                var edge = leaving ? state.Side(vehicle.Side)?.FriendlyEdge : Text(arguments, "edge", out var named) ? named : null;
                if (VehicleExits(state, vehicle, edge).OrderBy(item => item.HalfMp).FirstOrDefault() is not { } exit)
                {
                    return Refused(scope, label, expected, $"play.move-vehicle-exit: {id} is not in a map edge hex{(edge is null ? string.Empty : $" of the {edge} edge")} with that edge in its VCA, over reviewed terrain (A2.6, D2.11)");
                }

                cost = exit.HalfMp;
                exitEdge = EdgeSides(state, at.Location).FirstOrDefault(item => item.Side == exit.Direction).Edge;
                summary = $"{id} exits the map from {at.Location} for {Mp(cost)} MP (A2.6)" + (leaving ? " by its Friendly Board Edge (D5.341)" : string.Empty)
                    + (state.Passengers(id) is { Count: > 0 } riding ? $", with {string.Join(", ", riding.Select(unit => unit.Id))} aboard" : string.Empty)
                    + (state.Equipment.FirstOrDefault(item => item.Status == InstanceStatus.Active && item.Holding is { Role: HoldingRole.Towed } tow && tow.Holder == id) is { } towing
                        ? $", towing {towing.Id}" : string.Empty)
                    + (exitEdge is not null ? ExitScoring(state, vehicle.Side, at.Location, exitEdge, false) : string.Empty);
                break;
            default:
                return Refused(scope, label, expected, "play.invalid-arguments: a vehicle's MP expenditure is start, turn, enter, stop, exit, esb, overrun, load, or unload");
        }

        // D5.341 (ruling R5.17): a leaving AFV keeps to a shortest route in MP to its Friendly Board Edge.
        if (leaving)
        {
            var (moves, _, undecided) = RecallRoute(state, vehicle);
            if (undecided is not null)
            {
                return Refused(scope, label, expected, undecided);
            }

            if (!moves.Any(move => move.Kind == kind && (kind != VehicleStepped.Enter || (move.To == to && entry is { Bypass: false })) && (kind != VehicleStepped.Turn || move.Facing == turned)))
            {
                return Refused(scope, label, expected, $"play.recall-route: {id} leaves by a shortest route in MP to its Friendly Board Edge: "
                    + string.Join(", ", moves.Select(move => move.Kind switch
                    {
                        VehicleStepped.Enter => $"enter {move.To}",
                        VehicleStepped.Turn => $"turn to {UnitFacings.Name(move.Facing!.Value)}",
                        _ => "exit",
                    })) + " (D5.341)");
            }
        }

        // D2.1, D2.4, D2.7 (rulings R11.1, R11.5): the MP left must pay the expenditure; after an ALL entry the Stop MP is still allowed; a Minimum Move
        // spends the whole allotment; a vehicle in Reverse keeps one MP to Stop, since Reverse Motion is not built.
        var mayExceed = (kind == VehicleStepped.Stop && afterAll) || minimumMove || entry is { All: true };
        if (!mayExceed && spent + cost > allotment)
        {
            return Refused(scope, label, expected, $"play.move-vehicle-mp: {id} has {Mp(Math.Max(0, allotment - spent))} of its {Mp(allotment)} MP left, and this costs {Mp(cost)}"
                + (Tracked(vehicle) ? "; an ESB DR may add MP (D2.5)" : string.Empty) + " (D2.1)");
        }

        // Referee, pass 11: an ALL entry keeps its Stop MP beyond the allotment (D2.7, B13.41), and a VCA change in Reverse keeps it too.
        if (reverse && ((kind == VehicleStepped.Enter && entry is { All: false }) || kind == VehicleStepped.Turn) && spent + cost + 2 > allotment)
        {
            return Refused(scope, label, expected, $"play.move-vehicle-reverse: {id} in Reverse keeps one MP to Stop, since Reverse Motion is not built (D2.24; ruling R11.1)");
        }

        var text = $"play.move-vehicle: {summary}; {Mp(Math.Max(0, allotment - spent - cost))} MP left";
        if (kind == VehicleStepped.Exit)
        {
            return new GamePlan(GamePlanStatus.Ready, scope, label, expected,
                [Event(scope, attemptId, 1, expected, "vehicle-step", new VehicleStepped(id, kind, to, turned, cost, step) { Edge = exitEdge }, null, null)], [text]);
        }

        // A12.41 (ruling R11.12): an entry, not by VBM or a woods-road, into a Location of concealed enemy Personnel waits for their owner's choice.
        var subject = kind == VehicleStepped.Enter && entry is { Bypass: false } && !(entry.Road && entry.Terrain == "woods")
            ? PaatcSubjects(state, vehicle, to) : [];
        if (subject.Count > 0 && overrun && !KnownTargets(state, vehicle, to))
        {
            return Refused(scope, label, expected, $"play.move-vehicle-ovr: {to} holds only units {id}'s side cannot see; the OVR is declared after their reveal or PAATC (A12.41, D7.1)");
        }

        var bogDrm = entry?.BogDrm ?? turnBog.Drm;
        var hedgeDrm = entry?.HedgeBogDrm;
        var from = at.Location;
        List<GameEvent> Build(Func<RollRequest, RollResult>? draw)
        {
            var events = new List<GameEvent>();
            VehicleStepped Stepped(string stepKind, BoardLocation where) => new(id, stepKind, where, turned, cost, step)
            {
                Reverse = kind == VehicleStepped.Enter && reverse,
                Straddling = kind == VehicleStepped.Enter ? straddling : null,
                Overrunning = overrun,
                MinimumMove = minimumMove,
                All = entry?.All == true,
            };

            // B9.4: a halftrack failing its hedge Bog DR bogs in the hex it attempted to leave, having spent the MP there; one passing it takes the Bog
            // DR of the hex it enters, if any (referee, pass 11).
            if (hedgeDrm is { } hedgeBog && draw is not null)
            {
                var dice = draw(new RollRequest(2, 6));
                var rollId = $"{attemptId}-roll-1";
                var result = VehicleCheckRolled.For(VehicleCheckRolled.Bog, dice.Values[0] + dice.Values[1] + hedgeBog);
                var stuck = result == VehicleCheckRolled.Bogged;
                events.Add(Event(scope, attemptId, 1, expected, "dice-rolled", new DiceRolled(rollId, "vehicle-bog", 2, 6, dice.Values, DiceRolled.SystemSource, actor), null, null));
                events.Add(Event(scope, attemptId, 2, expected, "vehicle-step", Stepped(stuck ? VehicleStepped.Remain : kind, stuck ? from : to) with
                {
                    Straddling = stuck ? null : straddling,
                    Overrunning = !stuck && overrun,
                    All = !stuck && entry?.All == true,
                }, null, null));
                events.Add(Event(scope, attemptId, 3, expected, "vehicle-check-rolled", new VehicleCheckRolled(id, VehicleCheckRolled.Bog, rollId, hedgeBog, result), null, null,
                    [EventId(attemptId, 1)]));
                if (stuck)
                {
                    return events;
                }
            }
            else
            {
                events.Add(Event(scope, attemptId, 1, expected, "vehicle-step", Stepped(kind, to), null, null));
            }

            var boggedThere = bogDrm is { } drm && draw is not null
                && AddVehicleCheck(scope, attemptId, expected, actor, events, draw, id, VehicleCheckRolled.Bog, drm) == VehicleCheckRolled.Bogged;

            // Table-player finding, pass 11: a vehicle that bogs in the Location it entered still makes its concealed units choose (A12.41).
            if (subject.Count > 0)
            {
                events.Add(Event(scope, attemptId, events.Count + 1, expected, "choice-pending", PaatcChoice(state, vehicle, to, subject), null, null));
                return events;
            }

            if (boggedThere)
            {
                return events;
            }

            if (Replay([.. existing, .. events]).Current is { } moved)
            {
                var lost = VehicleConcealmentLost(moved, id, kind is VehicleStepped.Enter or VehicleStepped.Turn || Is(vehicle, Conditions.Motion));
                events.AddRange(RevealEvents(scope, attemptId, expected, events.Count + 1, lost));
            }

            return events;
        }

        if (overrun && subject.Count == 0 && OverrunDeclarationBar(existing, Build(null), id, to) is { } unresolvable)
        {
            return Refused(scope, label, expected, unresolvable);
        }

        return VehicleStepPlan(scope, label, attemptId, expected, actor, existing, Build, bogDrm is not null || hedgeDrm is not null, kind == VehicleStepped.Enter ? to : at.Location, step,
            text + (subject.Count > 0 ? $"; the concealed units in {to} are revealed or take a PAATC, as their owner chooses (A12.41)" : string.Empty));
    }

    /// <summary>
    /// A Start MP (D2.12, D2.23; rulings R11.1, R11.4, R11.10): forward or in Reverse; a bogged vehicle's Start MP is its Bog Removal, costing its
    /// colored dr times its white dr (doubled for a truck), which a colored dr of 1 to 4 frees; an AFV with red MP makes a Mechanical Reliability DR.
    /// </summary>
    private GamePlan PlanStartVehicle(GameScope scope, JsonElement arguments, IReadOnlyList<GameEvent> existing, string attemptId, long expected, string label,
        string actor, GameState state, UnitInstance vehicle, BoardLocation at, bool moving, bool bogged, bool leaving, int step)
    {
        var id = vehicle.Id;
        if (moving)
        {
            return Refused(scope, label, expected, Is(vehicle, Conditions.Motion) && state.Movement is null
                ? $"play.move-vehicle: {id} is in Motion and needs no Start MP (D2.4)"
                : $"play.move-vehicle: {id} is already moving; it starts again only after it stops (D2.12, D2.13)");
        }

        var reverse = Flag(arguments, "reverse");
        if (reverse && (leaving || state.Equipment.Any(item => item.Status == InstanceStatus.Active && item.Holding is { Role: HoldingRole.Towed } tow && tow.Holder == id)))
        {
            return Refused(scope, label, expected, $"play.move-vehicle-reverse: {id} {(leaving ? "leaves forward by its Friendly Board Edge (D5.341)" : "tows a Gun and may not use Reverse movement (D2.2)")}");
        }

        var (spent, allotment) = HalfMp(vehicle);
        if (!bogged && spent + 2 > allotment)
        {
            return Refused(scope, label, expected, $"play.move-vehicle-mp: {id} has {Mp(Math.Max(0, allotment - spent))} MP left, and starting costs 1 (D2.12)");
        }

        if (bogged)
        {
            // D8.3 (ruling R11.10): the Bog Removal is the Start MP of its MPh; the vehicle must not have Prep Fired (barred above).
            var truck = MovementTypeOf(vehicle) == "truck";
            var drm = Is(vehicle, Conditions.Mired) ? 1 : 0;
            List<GameEvent> Removal(Func<RollRequest, RollResult>? draw)
            {
                var events = new List<GameEvent>();
                if (draw is null)
                {
                    events.Add(Event(scope, attemptId, 1, expected, "vehicle-step", new VehicleStepped(id, VehicleStepped.Start, at, null, 2, step) { BogRemoval = true }, null, null));
                    return events;
                }

                var dice = draw(new RollRequest(2, 6));
                var rollId = $"{attemptId}-roll-1";
                var halfMp = dice.Values[0] * dice.Values[1] * (truck ? 2 : 1) * 2;
                var result = VehicleCheckRolled.For(VehicleCheckRolled.BogRemoval, dice.Values[0] + drm);
                events.Add(Event(scope, attemptId, 1, expected, "dice-rolled", new DiceRolled(rollId, "vehicle-bog-removal", 2, 6, dice.Values, DiceRolled.SystemSource, actor), null, null));
                events.Add(Event(scope, attemptId, 2, expected, "vehicle-step", new VehicleStepped(id, VehicleStepped.Start, at, null, halfMp, step) { BogRemoval = true }, null, null));
                events.Add(Event(scope, attemptId, 3, expected, "vehicle-check-rolled", new VehicleCheckRolled(id, VehicleCheckRolled.BogRemoval, rollId, drm, result), null, null,
                    [EventId(attemptId, 1)]));

                // D2.51 (referee, pass 11): the Bog Removal is a Start MP, so a red MP AFV rolls for Mechanical Reliability too.
                if (IsAfv(vehicle) && VehicleDefinition(vehicle)?.MechanicallyUnreliable == true && result != VehicleCheckRolled.Immobilized)
                {
                    AddVehicleCheck(scope, attemptId, expected, actor, events, draw, id, VehicleCheckRolled.Mechanical, 0);
                }

                return events;
            }

            return VehicleStepPlan(scope, label, attemptId, expected, actor, existing, Removal, true, at, step,
                $"play.move-vehicle-bog: {id} attempts Bog Removal as its Start MP: it spends its colored dr times its white dr in MP{(truck ? ", doubled for a truck," : string.Empty)} and is freed on a colored dr of 1 to 4{(drm > 0 ? " (+1 Mired)" : string.Empty)}, Mired on 5, immobilized on 6 or more (D8.3, D8.31)");
        }

        var unreliable = IsAfv(vehicle) && VehicleDefinition(vehicle)?.MechanicallyUnreliable == true;
        List<GameEvent> Build(Func<RollRequest, RollResult>? draw)
        {
            var events = new List<GameEvent>
            {
                Event(scope, attemptId, 1, expected, "vehicle-step", new VehicleStepped(id, VehicleStepped.Start, at, null, 2, step) { Reverse = reverse }, null, null),
            };
            if (unreliable)
            {
                AddVehicleCheck(scope, attemptId, expected, actor, events, draw, id, VehicleCheckRolled.Mechanical, 0);
            }

            return events;
        }

        return VehicleStepPlan(scope, label, attemptId, expected, actor, existing, Build, unreliable, at, step,
            $"play.move-vehicle: {id} starts{(reverse ? " in Reverse" : string.Empty)} in {at} for 1 MP (D2.12{(reverse ? ", D2.23" : string.Empty)})"
                + (unreliable ? "; its red MP make it roll for Mechanical Reliability: a 12 immobilizes it (D2.51)" : string.Empty)
                + $"; {Mp(allotment - spent - 2)} MP left");
    }

    /// <summary>
    /// ESB (D2.5; ruling R11.3): once per MPh a tracked vehicle that is moving declares up to a quarter of its printed MP (FRD) and makes an ESB DR,
    /// +1 per MP and its manufacturer's DRM (+2 German, +1 Russian); 11 or less adds them, 12 or more immobilizes it. Not after an ALL entry, nor in an
    /// enemy AFV's Location where it could not remain (D2.6, D2.7).
    /// </summary>
    private GamePlan PlanEsb(GameScope scope, JsonElement arguments, IReadOnlyList<GameEvent> existing, string attemptId, long expected, string label, string actor,
        GameState state, UnitInstance vehicle, bool moving, bool afterAll)
    {
        var id = vehicle.Id;
        var maximum = (VehicleDefinition(vehicle)?.MovementPoints ?? 0) / 4;
        if (!Tracked(vehicle))
        {
            return Refused(scope, label, expected, $"play.move-vehicle-esb: only a tracked vehicle attempts ESB; {id} is a truck (D2.5)");
        }

        if (!moving)
        {
            return Refused(scope, label, expected, $"play.move-vehicle-esb: {id} attempts ESB while moving: after its Start MP, or in Motion (D2.5; ruling R11.3)");
        }

        if (ThisPhase(existing).Select(item => item.Payload).OfType<VehicleCheckRolled>().Any(check => check.Vehicle == id && check.Check == VehicleCheckRolled.Esb))
        {
            return Refused(scope, label, expected, $"play.move-vehicle-esb: {id} has attempted ESB this MPh (D2.5)");
        }

        if (afterAll)
        {
            return Refused(scope, label, expected, $"play.move-vehicle-esb: {id} made an ALL entry, after which ESB is not allowed (D2.7)");
        }

        if (EnemyAfvBar(state, vehicle, existing) is { } blocked)
        {
            return Refused(scope, label, expected, "play.move-vehicle-esb: " + blocked);
        }

        if (!arguments.TryGetProperty("mp", out var mpValue) || !mpValue.TryGetInt32(out var mp) || mp < 1 || mp > maximum)
        {
            return Refused(scope, label, expected, $"play.move-vehicle-esb: ESB seeks from 1 to {maximum} MP for {id}, a quarter of its printed allotment (FRD) (D2.5)");
        }

        var nationality = VehicleDefinition(vehicle)?.Nationality;
        var national = nationality switch
        {
            "german" => 2,
            "russian" => 1,
            _ => 3,
        };
        var drm = mp + national;
        IReadOnlyList<GameEvent> Build(Func<RollRequest, RollResult> draw)
        {
            var events = new List<GameEvent>();
            if (AddVehicleCheck(scope, attemptId, expected, actor, events, draw, id, VehicleCheckRolled.Esb, drm, mp) == VehicleCheckRolled.Immobilized
                && state.Movement is { Vehicle: true })
            {
                events.Add(Event(scope, attemptId, events.Count + 1, expected, "movement-ended", new MovementEnded([id]), null, null));
            }

            return events;
        }

        return new GamePlan(GamePlanStatus.Ready, scope, label, expected, [],
            [$"play.move-vehicle-esb: {id} seeks {mp} more MP with an ESB DR at +{drm} (+{mp} for the MP, +{national} {nationality}): 11 or less gains them, 12 or more immobilizes it (D2.5)"])
        {
            Roll = new PlannedRoll("vehicle-esb", Build),
            FirstEventId = EventId(attemptId, 1),
        };
    }

    /// <summary>The fewest hexspines a VCA turns to point at a neighbor at a bearing: its VCA holds the two hexes 30 degrees either side.</summary>
    private static int VcaTurns(UnitFacing facing, double bearing) => ScenarioA1Geometry.VcaTurns((int)facing, bearing);

    private static string Mp(int halfMp) => halfMp % 2 == 0 ? (halfMp / 2).ToString(CultureInfo.InvariantCulture)
        : halfMp == 1 ? "½" : $"{(halfMp / 2).ToString(CultureInfo.InvariantCulture)}½";

    /// <summary>Whether a vehicle may spend no more MP this Player Turn: immobilized, bogged, Stunned, or Recalled and not yet leaving (D.7, D5.34, D5.341, D8.2).</summary>
    /// <summary>
    /// Whether a vehicle could still spend MP moving on (table-player finding, pass 11): an entry it can pay for now or, unless it changed its VCA at its
    /// CAFP, a VCA change and a 1 MP entry. One that cannot move on may Stop or end its move where D2.6 or D2.33 would keep it moving: after an ALL entry
    /// or a Minimum Move, on its last MP, or beside a hidden AFV (a reading).
    /// </summary>
    private bool MayMoveOn(GameState state, UnitInstance vehicle)
    {
        if (Halted(vehicle) || state.Location(vehicle.Id) is not { } at)
        {
            return false;
        }

        var (spent, allotment) = HalfMp(vehicle);
        var left = allotment - spent;
        return VehicleEntries(state, vehicle).Any(item => item.HalfMp is { } cost && !item.All && cost <= left)
            || (!(vehicle.Straddling is not null && TurnedAtCafp(state, vehicle)) && VehicleTurnCost(state, vehicle, at.Location).HalfMp + 2 <= left);
    }

    private static bool Halted(UnitInstance vehicle) =>
        vehicle.Status != InstanceStatus.Active || Is(vehicle, Conditions.Immobilized) || Is(vehicle, Conditions.Stunned) || Is(vehicle, Conditions.Shocked)
        || Is(vehicle, Conditions.UnconfirmedKill) || Is(vehicle, Conditions.Abandoned) || Is(vehicle, Conditions.Bogged)
        || (Is(vehicle, Conditions.Recalled) && !Is(vehicle, Conditions.StunRecovery));

    /// <summary>
    /// The half MP to enter an ADJACENT hex from a vehicle's Location, with one MP for each hexspine its VCA must turn to point at it (D2.11), or
    /// null when the hex is not ADJACENT or its entry is not reviewed or allowed.
    /// </summary>
    private int? IntendedEntryCost(GameState state, UnitInstance vehicle, BoardLocation to)
    {
        if (state.Location(vehicle.Id) is not { } at || vehicle.Position is not MapPosition { Facing: { } facing } || !Neighbors(state, at.Location).Contains(to)
            || VehicleEntryBar(state, vehicle, to) is not null)
        {
            return null;
        }

        return VehicleOutright(state, vehicle, at.Location, to, false, false).Entry is { } entry && Bearing(state, at.Location, to) is { } bearing
            ? (entry.All ? PrintedHalfMp(vehicle) : entry.HalfMp) + (VehicleTurnCost(state, vehicle, at.Location).HalfMp * VcaTurns(facing, bearing))
            : null;
    }

    /// <summary>
    /// Whether a moving vehicle may end its MPh in Motion (D2.4, ruling R5.14): when it has no MP left to Stop, or when the hex it wished to
    /// enter next, named by the ATTACKER, costs more than its MP left; a Recalled AFV when no step of its route is affordable or its route is
    /// undecided (ruling R5.17); never in Reverse (ruling R11.1). Null when it may; otherwise why not.
    /// </summary>
    private string? MotionBar(GameState state, UnitInstance vehicle, BoardLocation? intended, bool reverse)
    {
        if (Halted(vehicle))
        {
            return null;
        }

        if (reverse)
        {
            return $"play.vehicle-motion: {vehicle.Id} is moving in Reverse and Stops to end its move, since Reverse Motion is not built (D2.24; ruling R11.1)";
        }

        var (spent, allotment) = HalfMp(vehicle);
        var left = allotment - spent;
        if (MustLeave(vehicle))
        {
            var (moves, _, undecided) = RecallRoute(state, vehicle);
            return undecided is null && moves.Any(move => move.HalfMp <= left)
                ? $"play.recall-route: {vehicle.Id} is Recalled and has the MP to go on along its route to its Friendly Board Edge (D5.341)"
                : null;
        }

        if (left < 2)
        {
            return null;
        }

        if (intended is null)
        {
            return $"play.vehicle-motion: {vehicle.Id} has {Mp(left)} MP left, so it Stops (1 MP), moves on, or names the hex it wished to enter next to end in Motion (D2.4)";
        }

        return IntendedEntryCost(state, vehicle, intended) is not { } cost
            ? $"play.vehicle-motion: {intended} is not an ADJACENT hex {vehicle.Id} could enter over reviewed terrain, so it cannot be the next hex it wished to enter (D2.4)"
            : cost <= left
                ? $"play.vehicle-motion: {vehicle.Id} has {Mp(left)} MP left, enough to enter {intended} for {Mp(cost)}, so it Stops or moves on (D2.4)"
                : null;
    }

    /// <summary>
    /// The end of a vehicle's move (D2.1, D2.4; rulings R5.14 and R5.15): Motion needs the next hex it wished to enter to cost more than its MP
    /// left; MP left are then spent in its hex as one expenditure the DEFENDER may fire at, and the move ends when the DEFENDER passes. A vehicle
    /// that may spend no more MP ends at once. It may not end in an enemy AFV's Location it could not remain in (D2.6), nor after a VCA change at its
    /// CAFP (D2.33).
    /// </summary>
    private GamePlan PlanEndVehicle(GameScope scope, JsonElement arguments, IReadOnlyList<GameEvent> existing, string attemptId, long expected, string label,
        GameState state, MovementState movement, UnitInstance vehicle, string actor)
    {
        if (movement.Ending)
        {
            return Refused(scope, label, expected, "play.end-move: the vehicle has spent its MP left, and its move ends when the DEFENDER passes (D2.1)");
        }

        if (movement.Overrun is { } declared)
        {
            return Refused(scope, label, expected, $"play.end-move: {vehicle.Id} resolves its OVR of {declared} first (D7.1)");
        }

        if (!Halted(vehicle) && EnemyAfvBar(state, vehicle, existing) is { } blocked && MayMoveOn(state, vehicle))
        {
            return Refused(scope, label, expected, "play.end-move: " + blocked);
        }

        if (!Halted(vehicle) && vehicle.Straddling is not null && TurnedAtCafp(state, vehicle) && MayMoveOn(state, vehicle))
        {
            return Refused(scope, label, expected, $"play.end-move: {vehicle.Id} changed its VCA at its CAFP and must move on in Bypass before its move ends (D2.33)");
        }

        BoardLocation? intended = Text(arguments, "intended", out var named) && BoardLocation.TryParse(named, out var parsed) ? parsed : null;
        if (movement.Started && !movement.Stopped && MotionBar(state, vehicle, intended, movement.Reverse) is { } motion)
        {
            return Refused(scope, label, expected, motion);
        }

        var (spent, allotment) = HalfMp(vehicle);
        var left = allotment - spent;
        var inMotion = movement.Started && !movement.Stopped && !Halted(vehicle);
        if (left > 0 && !Halted(vehicle) && state.Location(vehicle.Id) is { } at)
        {
            List<GameEvent> Build(Func<RollRequest, RollResult>? draw) =>
                [Event(scope, attemptId, 1, expected, "vehicle-step", new VehicleStepped(vehicle.Id, VehicleStepped.Remain, at.Location, null, left, movement.Step + 1), null, null)];
            return VehicleStepPlan(scope, label, attemptId, expected, actor, existing, Build, false, at.Location, movement.Step + 1,
                $"play.end-move: {vehicle.Id} spends its {Mp(left)} MP left in {at.Location} (D2.1); the DEFENDER may fire, and its move ends when he passes"
                    + (inMotion ? ", in Motion (D2.4)" : string.Empty));
        }

        return new GamePlan(GamePlanStatus.Ready, scope, label, expected,
            [Event(scope, attemptId, 1, expected, "movement-ended", new MovementEnded([vehicle.Id]), null, null)],
            [$"play.end-move: {vehicle.Id} ends its move" + (inMotion ? " in Motion (D2.4)" : string.Empty)]);
    }

    /// <summary>
    /// The owner places or removes an AFV's BU counter (D5.33, ruling R25.8): in its own MPh or APh, once per phase, not in an MPh after it
    /// Prep Fired, not while Stunned or Recalled, and not while the DEFENDER's window on its own MP expenditure is open.
    /// </summary>
    private GamePlan PlanButtonUp(GameScope scope, JsonElement arguments, IReadOnlyList<GameEvent> existing, string attemptId, long expected, string label)
    {
        if (Replay(existing).Current is not { } state)
        {
            return Refused(scope, label, expected, "play.no-game: the game has no state yet");
        }

        if (!Text(arguments, "vehicleId", out var id) || !arguments.TryGetProperty("buttonedUp", out var value) || value.ValueKind is not (JsonValueKind.True or JsonValueKind.False))
        {
            return Refused(scope, label, expected, "play.invalid-arguments: buttoning up names the AFV and whether it is BU");
        }

        var buttonedUp = value.GetBoolean();
        if (state.Unit(id) is not { Status: InstanceStatus.Active } vehicle || !IsAfv(vehicle))
        {
            return Refused(scope, label, expected, "play.button-up: only an AFV's crew buttons up or exposes itself (D5.2, D5.3)");
        }

        if (vehicle.Side != state.PhasingSide || state.Phase is not ("mph" or "aph"))
        {
            return Refused(scope, label, expected, "play.button-up: a BU counter is placed or removed only in its owner's MPh or APh (D5.33)");
        }

        if (Is(vehicle, Conditions.Stunned) || Is(vehicle, Conditions.Recalled) || Is(vehicle, Conditions.Shocked) || Is(vehicle, Conditions.UnconfirmedKill))
        {
            return Refused(scope, label, expected, $"play.button-up: {id}'s crew is Stunned and stays BU this Player Turn (D5.34)");
        }

        if (state.Phase == "mph" && Is(vehicle, Conditions.PrepFire))
        {
            return Refused(scope, label, expected, $"play.button-up: {id} Prep Fired, so it may not change its CE status this MPh (D5.33)");
        }

        if (state.Movement is { WindowOpen: true } window && window.Movers.Contains(id, StringComparer.Ordinal))
        {
            return Refused(scope, label, expected, "play.button-up: the DEFENDER may still fire at its last MP expenditure (D5.33)");
        }

        if (Is(vehicle, Conditions.ButtonedUp) == buttonedUp)
        {
            return Refused(scope, label, expected, $"play.button-up: {id} is already {(buttonedUp ? "BU" : "CE")}");
        }

        var since = existing.Select((item, index) => (item, index)).LastOrDefault(pair => pair.item.Payload is PhaseChanged or GameStarted).index;
        if (existing.Skip(since).Any(item => item.Type == "crew-exposure-changed" && item.Payload is ConditionsChanged changed && changed.Id == id))
        {
            return Refused(scope, label, expected, $"play.button-up: {id}'s BU counter was already placed or removed this phase (D5.33)");
        }

        return new GamePlan(GamePlanStatus.Ready, scope, label, expected,
            [Event(scope, attemptId, 1, expected, "crew-exposure-changed",
                new ConditionsChanged(id, new Dictionary<string, ConditionState> { [Conditions.ButtonedUp] = buttonedUp ? ConditionState.True : ConditionState.False }),
                null, null)],
            [$"play.button-up: {id}'s crew {(buttonedUp ? "buttons up (D5.2)" : "is exposed (D5.3)")}"]);
    }
}
