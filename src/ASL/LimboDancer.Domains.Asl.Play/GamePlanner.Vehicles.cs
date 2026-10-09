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

    private static bool IsAfv(UnitInstance vehicle) =>
        ScenarioA1VehicleMovementCalculator.IsAfv(LiveFire.IsVehicle(vehicle), LiveFire.IsVehicle(vehicle) ? VehicleDefinition(vehicle)?.Unarmored : null);

    /// <summary>Whether a vehicle is a closed-topped AFV (D1.23; ruling R7.11): armored and not open-topped.</summary>
    public static bool IsClosedTopped(UnitInstance vehicle) =>
        ScenarioA1VehicleMovementCalculator.IsClosedTopped(IsAfv(vehicle), IsAfv(vehicle) ? VehicleDefinition(vehicle)?.OpenTopped : null);

    /// <summary>The half MP a vehicle has spent this MPh and its allotment, with the MP a successful ESB added (D1.1, D2.5).</summary>
    private static (int Spent, int Allotment) HalfMp(UnitInstance vehicle) =>
        ScenarioA1VehicleMovementCalculator.HalfMp(vehicle.MfSpent, vehicle.HalfMfSpent, VehicleDefinition(vehicle)?.MovementPoints ?? 0, vehicle.EsbMp);

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
            && ScenarioA1VehicleMovementCalculator.InVca(bearing, face))];
    }

    /// <summary>
    /// The half MP of an outright forward entry by a vehicle over terrain a Recall route may take (ruling R5.17): no Bog DR and no ALL entry; null
    /// otherwise.
    /// </summary>
    private int? VehicleEntryCost(GameState state, UnitInstance vehicle, BoardLocation from, BoardLocation to) =>
        VehicleOutright(state, vehicle, from, to, false, false).Entry is { } entry ? ScenarioA1VehicleMovementCalculator.RecallEntryHalfMp(entry.All, entry.BogDrm, entry.HalfMp) : null;

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
            var at = vehicle.Position is MapPosition { Facing: not null } ? state.Location(vehicle.Id) : null;
            if (ScenarioA1VehicleMovementCalculator.VehicleSetupBar(vehicle.Id, () => TowSetupBar(state, vehicle) ?? PassengerSetupBar(state, vehicle),
                vehicle.Position is OffMapPosition, at?.Location.ToString(), Is(vehicle, Conditions.Concealed) || Is(vehicle, Conditions.Hidden),
                () => VehicleConcealmentTerrain(state, at!.Location), at?.Location.Level ?? 0,
                () => ReadLocation(state, at!.Location) is { } read ? TerrainKey(read) : null,
                () => state.At(at!.Location).OfType<UnitInstance>().Any(unit => unit.Status == InstanceStatus.Active && unit.Id != vehicle.Id && unit.Side != vehicle.Side))
                is { } bar)
            {
                return bar;
            }
        }

        return null;
    }

    /// <summary>
    /// Why a vehicle's Passengers at setup are refused (A2.52, D6.1; ruling R26.2): they are Personnel of its side and OB group, on foot nowhere else,
    /// within its capacity; null when they are not.
    /// </summary>
    private string? PassengerSetupBar(GameState state, UnitInstance vehicle) =>
        ScenarioA1VehicleMovementCalculator.PassengerSetupBar(vehicle.Side, vehicle.Group, [.. state.Passengers(vehicle.Id).Select(unit => new PassengerSetupFacts(unit.Id,
                unit.Side, unit.Group, LiveFire.IsVehicle(unit), vocabulary.IsA(unit.Kind, "asl:personnel"), Is(unit, Conditions.Concealed) || Is(unit, Conditions.Hidden)))],
            vehicle.Id, () => CapacityBar(state, vehicle, []));

    /// <summary>
    /// Why a Gun set up in tow is refused (C10.1, C10.2, C10.13; ruling R26.1): the vehicle's T# exceeds the Gun's M#, the Gun is not QSU (limbering is not
    /// built), more than one Gun is in tow, or the ammunition leaves the vehicle's Passengers no room; null when none is.
    /// </summary>
    private static string? TowSetupBar(GameState state, UnitInstance vehicle)
    {
        EquipmentInstance[] towed = [.. state.Equipment.Where(item => item.Status == InstanceStatus.Active && item.Holding is { Role: HoldingRole.Towed } tow && tow.Holder == vehicle.Id)];
        var gun = towed.Length == 1 ? towed[0] : null;
        return ScenarioA1VehicleMovementCalculator.TowSetupBar(vehicle.Id, [.. towed.Select(item => item.Id)], gun is null ? null : VehicleDefinition(vehicle)?.Towing,
            gun is null ? null : OrdnanceReference.Value.Guns.GetValueOrDefault(gun.Definition?.Definition ?? string.Empty)?.Manhandling,
            gun?.Definition is { } reference && FireReference.Value.Definitions.GetValueOrDefault(reference.Definition)?.QuickSetUp == true);
    }

    /// <summary>Whether a unit is an AFV (D1.2): a vehicle whose AF is printed.</summary>
    public static bool IsArmoredVehicle(UnitInstance unit) => IsAfv(unit);

    /// <summary>Whether a vehicle has a MG that fires on the IFT (ruling R25.7): the SPW 251/1's MA AAMG; a truck is unarmed (D5.1).</summary>
    public static bool HasVehicleMg(UnitInstance unit) =>
        ScenarioA1VehicleMovementCalculator.HasVehicleMg(LiveFire.IsVehicle(unit), VehicleDefinition(unit)?.MainArmament, VehicleDefinition(unit)?.AntiAircraftMg is not null);

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
        ScenarioA1VehicleMovementCalculator.EntryBar(state.At(to).OfType<UnitInstance>().Any(unit => unit.Status == InstanceStatus.Active && Is(unit, Conditions.Melee)));

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
        ScenarioA1VehicleMovementCalculator.FirstEntry(StepsThisPhase(existing, vehicle).Select(step => (step.Kind == VehicleStepped.Start, step.BogRemoval)));

    /// <summary>Whether the vehicle made an ALL entry this MPh, after which it may only Stop or end in Motion (D2.7; ruling R11.5).</summary>
    private static bool AfterAllEntry(IReadOnlyList<GameEvent> existing, UnitInstance vehicle) =>
        ScenarioA1VehicleMovementCalculator.AfterAllEntry(StepsThisPhase(existing, vehicle.Id).Select(step => (step.Kind == VehicleStepped.Enter, step.All)));

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

        // D5.341: a Recall stops the AFV like a Stun for the rest of that Player Turn; once its counter shows Recall; +1 it must leave.
        var leaving = MustLeave(vehicle);
        var current = state.Movement;
        if (ScenarioA1VehicleMovementCalculator.MoveBar(id, kind, vehicle.MovementEnded,
            () => ButtonedUpAfv(vehicle) && NvrOf(state, vehicle) == 0, condition => Is(vehicle, condition), leaving,
            current is not null && (!current.Vehicle || !current.Members.SequenceEqual([vehicle.Id], StringComparer.Ordinal)) ? current.Members : null,
            current is { WindowOpen: true }, current?.Reaction == true, current?.Overrun?.ToString(),
            () => current is null && MustCharge(state) is [{ } charging, ..] ? charging.Id : null, current is not null) is { } moveBar)
        {
            return Refused(scope, label, expected, moveBar);
        }

        var bogged = Is(vehicle, Conditions.Bogged);

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

        if (ScenarioA1VehicleMovementCalculator.MovingBar(id, kind, moving, afterAll) is { } movingBar)
        {
            return Refused(scope, label, expected, movingBar);
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
                UnitFacing? next = Text(arguments, "facing", out var facingName) && UnitFacings.TryParse(facingName, out var parsedFacing) ? parsedFacing : null;
                if (ScenarioA1VehicleMovementCalculator.TurnBar(id, (int?)next, (int)facing, vehicle.Straddling is not null, () => TurnedAtCafp(state, vehicle)) is { } turnBar)
                {
                    return Refused(scope, label, expected, turnBar);
                }

                turned = next!.Value;
                var turnCost = VehicleTurnCost(state, vehicle, at.Location);
                cost = turnCost.HalfMp;
                turnBog = (turnCost.BogDrm, turnCost.Causes);
                summary = $"{id} changes its VCA to {UnitFacings.Name(next.Value)} in {at.Location} for {Mp(cost)} MP (D2.11)"
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
                if (ScenarioA1VehicleMovementCalculator.EntryCostBar(id, to.ToString(), entry.All, entry.HalfMp, printed, first, minimumMove, reverse, entry.Bypass) is { } costBar)
                {
                    return Refused(scope, label, expected, costBar);
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

                    if (ScenarioA1VehicleMovementCalculator.OverrunAllBar(id, entry.All, minimumMove) is { } allBar)
                    {
                        return Refused(scope, label, expected, allBar);
                    }

                    cost += OverrunHalfMp(vehicle);
                }

                summary = $"{id} enters {to}{(entry.Bypass ? $" in VBM, straddling the hexside with {entry.Straddling}" : string.Empty)}{(reverse ? " in Reverse" : string.Empty)} for {Mp(cost)} MP"
                    + (entry.All ? " (ALL, B13.41)" : minimumMove ? " (Minimum Move, D2.15)" : string.Empty)
                    + (overrun ? $", with an OVR ({Mp(OverrunHalfMp(vehicle))} MP, D7.1)" : string.Empty)
                    + (entry.BogDrm is { } entryBog ? $"; Bog DR at {entryBog:+0;-0;0} (D8.21)" : string.Empty);
                break;
            case VehicleStepped.Stop:
                if (ScenarioA1VehicleMovementCalculator.StopBar(id, leaving, () => EnemyAfvBar(state, vehicle, existing), () => MayMoveOn(state, vehicle),
                    vehicle.Straddling is not null, () => TurnedAtCafp(state, vehicle)) is { } stopBar)
                {
                    return Refused(scope, label, expected, stopBar);
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
        if (ScenarioA1VehicleMovementCalculator.ExpenditureBar(id, kind, afterAll, minimumMove, entry?.All, reverse, spent, allotment, cost, Tracked(vehicle)) is { } mpBar)
        {
            return Refused(scope, label, expected, mpBar);
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
        var reverse = Flag(arguments, "reverse");
        var (spent, allotment) = HalfMp(vehicle);
        if (ScenarioA1VehicleMovementCalculator.StartBar(id, moving, Is(vehicle, Conditions.Motion) && state.Movement is null, reverse, leaving,
            () => state.Equipment.Any(item => item.Status == InstanceStatus.Active && item.Holding is { Role: HoldingRole.Towed } tow && tow.Holder == id), bogged, spent,
            allotment) is { } startBar)
        {
            return Refused(scope, label, expected, startBar);
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
                var halfMp = ScenarioA1VehicleMovementCalculator.BogRemovalHalfMp(dice.Values[0], dice.Values[1], truck);
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
        int? requested = arguments.TryGetProperty("mp", out var mpValue) && mpValue.TryGetInt32(out var parsedMp) ? parsedMp : null;
        if (ScenarioA1VehicleMovementCalculator.EsbBar(id, Tracked(vehicle), moving,
            () => ThisPhase(existing).Select(item => item.Payload).OfType<VehicleCheckRolled>().Any(check => check.Vehicle == id && check.Check == VehicleCheckRolled.Esb),
            afterAll, () => EnemyAfvBar(state, vehicle, existing), requested, maximum) is { } esbBar)
        {
            return Refused(scope, label, expected, esbBar);
        }

        var mp = requested!.Value;
        var nationality = VehicleDefinition(vehicle)?.Nationality;
        var national = ScenarioA1VehicleMovementCalculator.EsbNationalDrm(nationality);
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

    private static string Mp(int halfMp) => ScenarioA1VehicleMovementCalculator.Mp(halfMp);

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
        return ScenarioA1VehicleMovementCalculator.MayMoveOn(spent, allotment, () => VehicleEntries(state, vehicle).Select(item => (item.HalfMp, item.All)),
            vehicle.Straddling is not null, () => TurnedAtCafp(state, vehicle), () => VehicleTurnCost(state, vehicle, at.Location).HalfMp);
    }

    private static bool Halted(UnitInstance vehicle) =>
        ScenarioA1VehicleMovementCalculator.Halted(vehicle.Status == InstanceStatus.Active, Is(vehicle, Conditions.Immobilized), Is(vehicle, Conditions.Stunned),
            Is(vehicle, Conditions.Shocked), Is(vehicle, Conditions.UnconfirmedKill), Is(vehicle, Conditions.Abandoned), Is(vehicle, Conditions.Bogged),
            Is(vehicle, Conditions.Recalled), Is(vehicle, Conditions.StunRecovery));

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
            ? ScenarioA1VehicleMovementCalculator.IntendedEntryHalfMp(entry.All, entry.HalfMp, PrintedHalfMp(vehicle), VehicleTurnCost(state, vehicle, at.Location).HalfMp,
                VcaTurns(facing, bearing))
            : null;
    }

    /// <summary>
    /// Whether a moving vehicle may end its MPh in Motion (D2.4, ruling R5.14): when it has no MP left to Stop, or when the hex it wished to
    /// enter next, named by the ATTACKER, costs more than its MP left; a Recalled AFV when no step of its route is affordable or its route is
    /// undecided (ruling R5.17); never in Reverse (ruling R11.1). Null when it may; otherwise why not.
    /// </summary>
    private string? MotionBar(GameState state, UnitInstance vehicle, BoardLocation? intended, bool reverse)
    {
        var (spent, allotment) = HalfMp(vehicle);
        var left = allotment - spent;
        return ScenarioA1VehicleMovementCalculator.MotionBar(vehicle.Id, Halted(vehicle), reverse, left, MustLeave(vehicle),
            () => RecallRoute(state, vehicle) is var (moves, _, undecided) && undecided is null && moves.Any(move => move.HalfMp <= left),
            intended?.ToString(), () => intended is null ? null : IntendedEntryCost(state, vehicle, intended));
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
        if (ScenarioA1VehicleMovementCalculator.EndMoveBar(vehicle.Id, movement.Ending, movement.Overrun?.ToString(), Halted(vehicle),
            () => EnemyAfvBar(state, vehicle, existing), () => MayMoveOn(state, vehicle), vehicle.Straddling is not null, () => TurnedAtCafp(state, vehicle),
            () =>
            {
                BoardLocation? intended = Text(arguments, "intended", out var named) && BoardLocation.TryParse(named, out var parsed) ? parsed : null;
                return movement.Started && !movement.Stopped ? MotionBar(state, vehicle, intended, movement.Reverse) : null;
            }) is { } endBar)
        {
            return Refused(scope, label, expected, endBar);
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
        var vehicle = state.Unit(id) is { Status: InstanceStatus.Active } unit && IsAfv(unit) ? unit : null;
        if (ScenarioA1VehicleMovementCalculator.ButtonUpBar(id, vehicle is not null,
            vehicle is not null && vehicle.Side == state.PhasingSide && state.Phase is "mph" or "aph",
            vehicle is not null && (Is(vehicle, Conditions.Stunned) || Is(vehicle, Conditions.Recalled) || Is(vehicle, Conditions.Shocked) || Is(vehicle, Conditions.UnconfirmedKill)),
            vehicle is not null && state.Phase == "mph" && Is(vehicle, Conditions.PrepFire),
            state.Movement is { WindowOpen: true } window && window.Movers.Contains(id, StringComparer.Ordinal),
            vehicle is not null && ButtonedUpAfv(vehicle) == buttonedUp, buttonedUp,
            () =>
            {
                var since = existing.Select((item, index) => (item, index)).LastOrDefault(pair => pair.item.Payload is PhaseChanged or GameStarted).index;
                return existing.Skip(since).Any(item => item.Type == "crew-exposure-changed" && item.Payload is ConditionsChanged changed && changed.Id == id);
            }) is { } buttonBar)
        {
            return Refused(scope, label, expected, buttonBar);
        }

        return new GamePlan(GamePlanStatus.Ready, scope, label, expected,
            [Event(scope, attemptId, 1, expected, "crew-exposure-changed",
                new ConditionsChanged(id, new Dictionary<string, ConditionState> { [Conditions.ButtonedUp] = buttonedUp ? ConditionState.True : ConditionState.False }),
                null, null)],
            [$"play.button-up: {id}'s crew {(buttonedUp ? "buttons up (D5.2)" : "is exposed (D5.3)")}"]);
    }
}
