using System.Globalization;
using System.Text.Json;
using LimboDancer.Domains.Asl.Maps.Coordinates;
using LimboDancer.Domains.Asl.ScenarioA1;
using LimboDancer.Domains.Asl.Units.Documents;
using LimboDancer.Domains.Asl.Units.State;

namespace LimboDancer.Domains.Asl.Play;

/// <summary>
/// Vehicles in live play (unit step 25, rulings R25.3 and R25.8 to R25.10): a vehicle's MP expenditures in its MPh, each opening the
/// DEFENDER's window (D2.1, A8.1): Start (D2.12), a VCA change of one hexspine (D2.11), entering the hex its VCA points at over reviewed
/// terrain (D2.11, D2.16; Terrain Chart p. 698), and Stop (D2.13); the end of its move in Motion or stopped (D2.4); its crew buttoning up
/// or exposing itself (D5.33); and the refusals an AFV's cover and Hindrance bring to Infantry fire (D9.3, D9.4).
/// </summary>
public sealed partial class GamePlanner
{
    // The Terrain Chart's MP Entrance Cost (p. 698; b-terrain-chart-vehicle-mp.transcription.json), in half MP, of the terrain a vehicle may
    // enter (ruling R25.3): Open Ground (a road hex entered otherwise than along its road, B1.11) and Grain.
    private static readonly Dictionary<(string MovementType, string Terrain), int> VehicleEntryHalfMp =
        new Dictionary<(string, string), int>
        {
            [("half-tracked", "open-ground")] = 2,
            [("truck", "open-ground")] = 8,
            [("half-tracked", "grain")] = 2,
            [("truck", "grain")] = 10,
        };

    // A vehicle's facing is a hexspine (D2.11), whose direction counterclockwise from east is 60 degrees per UnitFacing step.
    private static double FacingDegrees(UnitFacing facing) => 60 * (int)facing;

    private static FireDefinition? VehicleDefinition(UnitInstance vehicle) =>
        vehicle.Definition is { } definition ? FireReference.Value.Definitions.GetValueOrDefault(definition.Definition) : null;

    private static bool IsAfv(UnitInstance vehicle) => LiveFire.IsVehicle(vehicle) && VehicleDefinition(vehicle) is { Unarmored: false };

    /// <summary>The half MP a vehicle has spent this MPh and its allotment (D1.1).</summary>
    private static (int Spent, int Allotment) HalfMp(UnitInstance vehicle) =>
        ((vehicle.MfSpent * 2) + (vehicle.HalfMfSpent ? 1 : 0), (VehicleDefinition(vehicle)?.MovementPoints ?? 0) * 2);

    /// <summary>
    /// The two ADJACENT Locations a vehicle's VCA points at (D2.11, C3.2): the neighbors whose bearing is 30 degrees either side of its
    /// facing hexspine, on one unreversed board (the bearing's limit, as for a Gun's Covered Arc).
    /// </summary>
    private IReadOnlyList<BoardLocation> VcaHexes(GameState state, BoardLocation at, UnitFacing facing)
    {
        var face = FacingDegrees(facing);
        return [.. Neighbors(state, at).Where(next => Bearing(state, at, next) is { } bearing
            && Math.Abs(Math.Abs(((bearing - face + 540) % 360) - 180) - 30) < 1)];
    }

    /// <summary>
    /// The half MP a vehicle spends to enter an ADJACENT Location (R25.3): along a road hexside the ½ MP road rate, 1 MP for a BU AFV
    /// (D2.16); otherwise its Terrain Chart cost for Open Ground or Grain, at the same level, crossing no hexside terrain; null otherwise.
    /// </summary>
    private int? VehicleEntryCost(GameState state, UnitInstance vehicle, BoardLocation from, BoardLocation to)
    {
        var (fromRead, toRead, adjacent, crossed) = Step(state, from, to);
        if (fromRead is null || toRead is null || !adjacent || crossed is null || VehicleDefinition(vehicle)?.MovementType is not { } type
            || fromRead.Hex.BaseLevel + fromRead.Level.Level != toRead.Hex.BaseLevel + toRead.Level.Level || crossed.HexsideTerrain is not null || crossed.Cliff
            || crossed.Slope || TerrainKey(toRead) is not { } terrain)
        {
            return null;
        }

        // B15.6: Grain is Open Ground outside its season; its MP cost applies April to September (Terrain Chart p. 698).
        if (terrain == "grain")
        {
            if (state.ScenarioMonth is not { } month)
            {
                return null;
            }

            terrain = month is >= 4 and <= 9 ? "grain" : "open-ground";
        }

        if (!VehicleEntryHalfMp.TryGetValue((type, terrain), out var halfMp))
        {
            return null;
        }

        return crossed.Terrain?.IsRoad == true ? (IsAfv(vehicle) && Is(vehicle, Conditions.ButtonedUp) ? 2 : 1) : halfMp;
    }

    /// <summary>
    /// Why the set-up vehicles are outside the reviewed cases (rulings R25.3, R25.10), or null: a vehicle is placed on the map with a VCA,
    /// not concealed or hidden (A12.2 is not reviewed), at ground level in Open Ground, Grain, or a road hex, alone among vehicles in its
    /// Location, and with no enemy unit there.
    /// </summary>
    private string? VehicleSetupBar(GameState state)
    {
        foreach (var vehicle in state.Units.Where(unit => unit.Status == InstanceStatus.Active && LiveFire.IsVehicle(unit)))
        {
            if (vehicle.Position is not MapPosition { Facing: not null } position || state.Location(vehicle.Id) is not { } at)
            {
                return $"play.setup-vehicle: {vehicle.Id} is set up on the map with its VCA facing a hexspine (D2.11)";
            }

            if (Is(vehicle, Conditions.Concealed) || Is(vehicle, Conditions.Hidden))
            {
                return $"play.setup-vehicle: {vehicle.Id} may not set up concealed or hidden; A12.2 for vehicles is not reviewed (ruling R25.10)";
            }

            if (at.Location.Level != 0 || ReadLocation(state, at.Location) is not { } read || TerrainKey(read) is not ("open-ground" or "grain"))
            {
                return $"play.setup-vehicle: {vehicle.Id} sets up at ground level in Open Ground, Grain, or a road hex, the terrain the Vehicle rules reviewed (ruling R25.3)";
            }

            var here = state.At(at.Location).OfType<UnitInstance>().Where(unit => unit.Status == InstanceStatus.Active && unit.Id != vehicle.Id).ToArray();
            if (here.Any(LiveFire.IsVehicle))
            {
                return $"play.setup-vehicle: {at.Location} holds more than one vehicle; stacking vehicles (D2.14) is not reviewed (ruling R25.3)";
            }

            if (here.Any(unit => unit.Side != vehicle.Side))
            {
                return $"play.setup-vehicle: {vehicle.Id} shares {at.Location} with an enemy unit; OVR and CC against a vehicle are not reviewed (ruling R25.3)";
            }
        }

        return null;
    }

    /// <summary>Whether a unit is an AFV (D1.2): a vehicle whose AF is printed, here the open-topped SPW 251/1.</summary>
    public static bool IsArmoredVehicle(UnitInstance unit) => IsAfv(unit);

    /// <summary>Whether a vehicle has a MG that fires on the IFT (ruling R25.7): the SPW 251/1's MA AAMG; a truck is unarmed (D5.1).</summary>
    public static bool HasVehicleMg(UnitInstance unit) => LiveFire.IsVehicle(unit) && VehicleDefinition(unit) is { MainArmament: "aamg", AntiAircraftMg: not null };

    /// <summary>The half MP a vehicle has spent this MPh and its allotment in half MP (D1.1), for the Play page.</summary>
    public static (int Spent, int Allotment) VehicleHalfMp(UnitInstance vehicle) => HalfMp(vehicle);

    /// <summary>
    /// The hexes a vehicle's VCA points at (D2.11), each with its entry cost in half MP, or null where the reviewed cases do not let it
    /// enter (ruling R25.3), for the Play page.
    /// </summary>
    public IReadOnlyList<(BoardLocation To, int? HalfMp, string? Bar)> VehicleEntries(GameState state, UnitInstance vehicle)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(vehicle);
        if (state.Location(vehicle.Id) is not { } at || vehicle.Position is not MapPosition { Facing: { } facing })
        {
            return [];
        }

        return [.. VcaHexes(state, at.Location, facing).Select(to =>
        {
            var cost = VehicleEntryCost(state, vehicle, at.Location, to);
            var bar = cost is null ? "not a reviewed entry: only Open Ground, Grain, and roads at one level (ruling R25.3)" : VehicleEntryBar(state, vehicle, to);
            return (to, bar is null ? cost : null, bar);
        })];
    }

    /// <summary>An active vehicle of a side other than the one named, in a Location, or null.</summary>
    private static UnitInstance? EnemyVehicleAt(GameState state, string side, BoardLocation at) =>
        state.At(at).OfType<UnitInstance>().FirstOrDefault(unit => unit.Status == InstanceStatus.Active && unit.Side != side && LiveFire.IsVehicle(unit));

    /// <summary>
    /// Whether Residual FP could attack a vehicle (A8.2, A8.222): an unarmored vehicle on the Vehicle line, or an AFV whose crew is
    /// Vulnerable; a BU or Stunned AFV is not attacked.
    /// </summary>
    private static bool ResidualReaches(UnitInstance vehicle) => !IsAfv(vehicle) || LiveFire.CrewExposed(vehicle);

    /// <summary>Why a vehicle may not enter a Location it could otherwise enter (R25.3), or null.</summary>
    private static string? VehicleEntryBar(GameState state, UnitInstance vehicle, BoardLocation to) =>
        state.At(to).OfType<UnitInstance>().Any(unit => unit.Status == InstanceStatus.Active && LiveFire.IsVehicle(unit))
            ? "another vehicle is there (at most one vehicle per Location; D2.14 is not reviewed)"
            : state.At(to).OfType<UnitInstance>().Any(unit => unit.Status == InstanceStatus.Active && unit.Side != vehicle.Side && VisibleTo(unit, vehicle.Side))
                ? "an enemy unit is there (OVR, D7, and CC with vehicles, A11.5, are not reviewed)"
                : state.At(to).OfType<UnitInstance>().Any(unit => unit.Status == InstanceStatus.Active && unit.Side != vehicle.Side)
                ? "the entry is not decided here (a vehicle's entry into a Location the moving side cannot see into is not reviewed)"
                : state.ResidualFire.Any(item => item.Location == to) && ResidualReaches(vehicle)
                    ? "Residual FP is there (Residual FP against a vehicle or its crew is not built; ruling R25.3)"
                    : null;

    private GamePlan PlanMoveVehicle(GameScope scope, JsonElement arguments, IReadOnlyList<GameEvent> existing, string attemptId, long expected, string label)
    {
        if (Replay(existing).Current is not { } state)
        {
            return Refused(scope, label, expected, "play.no-game: the game has no state yet");
        }

        if (!Text(arguments, "vehicleId", out var id) || !Text(arguments, "kind", out var kind))
        {
            return Refused(scope, label, expected, "play.invalid-arguments: a vehicle's move names the vehicle and the kind of MP expenditure");
        }

        if (state.Phase != "mph" || state.Unit(id) is not { Status: InstanceStatus.Active } vehicle || !LiveFire.IsVehicle(vehicle)
            || vehicle.Side != state.PhasingSide || state.Location(vehicle.Id) is not { } at || vehicle.Position is not MapPosition { Facing: { } facing }
            || VehicleDefinition(vehicle) is not { MovementPoints: not null })
        {
            return Refused(scope, label, expected, "play.move-vehicle: a vehicle of the phasing side on the map moves in its MPh (D2.1)");
        }

        if (vehicle.MovementEnded)
        {
            return Refused(scope, label, expected, $"play.move-vehicle: {id} has ended its move this MPh (A4.2)");
        }

        if (new[] { (Conditions.PrepFire, "it Prep Fired (D.3)"), (Conditions.Immobilized, "it is immobilized (D.7)"),
            (Conditions.Stunned, "its crew is Stunned (D5.34)"), (Conditions.Recalled, "it is Recalled (D5.341)") }
            .FirstOrDefault(item => Is(vehicle, item.Item1)) is { Item2: { } why })
        {
            return Refused(scope, label, expected, $"play.move-vehicle: {id} may not move: {why}");
        }

        var current = state.Movement;
        if (current is not null && (!current.Vehicle || !current.Members.SequenceEqual([vehicle.Id], StringComparer.Ordinal)))
        {
            return Refused(scope, label, expected, $"play.move-order: {string.Join(", ", current.Members)} moves until its move ends (A4.2)");
        }

        if (current is { WindowOpen: true })
        {
            return Refused(scope, label, expected, "play.move-window: the DEFENDER may still fire at the vehicle's last MP expenditure (A8.1, A8.11)");
        }

        // A15.43: at the start of the MPh every berserk unit charges before any other unit moves.
        if (current is null && MustCharge(state) is [{ } charging, ..])
        {
            return Refused(scope, label, expected, $"play.berserk-first: {charging.Id} is berserk and charges before any other unit moves (A15.43)");
        }

        var moving = current is not null ? current.Started && !current.Stopped : Is(vehicle, Conditions.Motion);
        var (spent, allotment) = HalfMp(vehicle);
        BoardLocation to = at.Location;
        UnitFacing? turned = null;
        int cost;
        string summary;
        switch (kind)
        {
            case VehicleStepped.Start:
                if (moving)
                {
                    return Refused(scope, label, expected, Is(vehicle, Conditions.Motion) && current is null
                        ? $"play.move-vehicle: {id} is in Motion and needs no Start MP (D2.4)"
                        : $"play.move-vehicle: {id} is already moving; it starts again only after it stops (D2.12, D2.13)");
                }

                cost = 2;
                summary = $"{id} starts in {at.Location} for 1 MP (D2.12)";
                break;
            case VehicleStepped.Turn:
                if (!Text(arguments, "facing", out var facingName) || !UnitFacings.TryParse(facingName, out var next)
                    || Math.Abs(((int)next - (int)facing + 6) % 6) is not (1 or 5))
                {
                    return Refused(scope, label, expected, "play.move-vehicle: a VCA change turns one hexspine (D2.11)");
                }

                turned = next;
                cost = 2;
                summary = $"{id} changes its VCA to {UnitFacings.Name(next)} in {at.Location} for 1 MP (D2.11)";
                break;
            case VehicleStepped.Enter:
                if (!Text(arguments, "to", out var toText) || !BoardLocation.TryParse(toText, out var entered))
                {
                    return Refused(scope, label, expected, "play.invalid-arguments: an entry names the Location entered");
                }

                to = entered;

                if (!VcaHexes(state, at.Location, facing).Contains(to))
                {
                    return Refused(scope, label, expected, $"play.move-vehicle: {id} enters only a hex its VCA points at, {string.Join(" or ", VcaHexes(state, at.Location, facing))} (D2.11)");
                }

                if (VehicleEntryCost(state, vehicle, at.Location, to) is not { } entry)
                {
                    return Refused(scope, label, expected, $"play.move-vehicle-terrain: {to}'s entry by {id} is not reviewed: only Open Ground, Grain, and roads at one level, crossing no hexside terrain (ruling R25.3)");
                }

                if (VehicleEntryBar(state, vehicle, to) is { } bar)
                {
                    return Refused(scope, label, expected, $"play.move-vehicle: {id} may not enter {to}: {bar}");
                }

                cost = entry;
                summary = $"{id} enters {to} for {Mp(entry)} MP (D2.11)";
                break;
            case VehicleStepped.Stop:
                cost = 2;
                summary = $"{id} stops in {at.Location} for 1 MP (D2.13)";
                break;
            default:
                return Refused(scope, label, expected, "play.invalid-arguments: a vehicle's MP expenditure is start, turn, enter, or stop");
        }

        // A8.2 (ruling R25.3): Residual FP attacks a unit expending MP in its Location, which the Vehicle line does not build; the vehicle
        // may leave the Location, but not spend MP in it.
        if (kind != VehicleStepped.Enter && state.ResidualFire.Any(item => item.Location == at.Location) && ResidualReaches(vehicle))
        {
            return Refused(scope, label, expected, $"play.move-vehicle-residual: Residual FP in {at.Location} would attack {id} for this MP, which is not reviewed (A8.2; ruling R25.3)");
        }

        if (kind != VehicleStepped.Start && !moving)
        {
            return Refused(scope, label, expected, $"play.move-vehicle: {id} is not moving; it must start first (D2.12)");
        }

        if (spent + cost > allotment)
        {
            return Refused(scope, label, expected, $"play.move-vehicle-mp: {id} has {Mp(allotment - spent)} of its {Mp(allotment)} MP left, and this costs {Mp(cost)} (D2.1; ESB, D2.5, is not reviewed)");
        }

        var step = (current?.Step ?? 0) + 1;
        return new GamePlan(GamePlanStatus.Ready, scope, label, expected,
            [Event(scope, attemptId, 1, expected, "vehicle-step", new VehicleStepped(id, kind, to, turned, cost, step), null, null)],
            [$"play.move-vehicle: {summary}; {Mp(allotment - spent - cost)} MP left"]);
    }

    /// <summary>The fewest hexspines a VCA turns to point at a neighbor at a bearing: its VCA holds the two hexes 30 degrees either side.</summary>
    private static int VcaTurns(UnitFacing facing, double bearing)
    {
        var turns = int.MaxValue;
        foreach (var candidate in Enum.GetValues<UnitFacing>())
        {
            if (Math.Abs(Math.Abs(((bearing - FacingDegrees(candidate) + 540) % 360) - 180) - 30) < 1)
            {
                var steps = Math.Abs((int)candidate - (int)facing);
                turns = Math.Min(turns, Math.Min(steps, 6 - steps));
            }
        }

        return turns == int.MaxValue ? 6 : turns;
    }

    private static string Mp(int halfMp) => halfMp % 2 == 0 ? (halfMp / 2).ToString(CultureInfo.InvariantCulture)
        : halfMp == 1 ? "½" : $"{(halfMp / 2).ToString(CultureInfo.InvariantCulture)}½";

    /// <summary>
    /// Whether a moving vehicle may end its MPh in Motion (D2.4): when it may not Stop (no MP left for it), or when it cannot pay the entry
    /// of any hex its VCA points at that it may enter; otherwise it must Stop first. Null when it may; otherwise why not.
    /// </summary>
    private string? MotionBar(GameState state, UnitInstance vehicle)
    {
        // A vehicle the DEFENDER's fire immobilized, Stunned, or Recalled has stopped (D.7, D5.34).
        if (state.Location(vehicle.Id) is not { } at || vehicle.Position is not MapPosition { Facing: { } facing } || vehicle.Status != InstanceStatus.Active
            || Is(vehicle, Conditions.Immobilized) || Is(vehicle, Conditions.Stunned) || Is(vehicle, Conditions.Recalled))
        {
            return null;
        }

        var (spent, allotment) = HalfMp(vehicle);
        var left = allotment - spent;
        if (left < 2)
        {
            return null;
        }

        // Each ADJACENT hex it may enter costs its entry plus 1 MP for each hexspine its VCA must turn to point at it (D2.11).
        var entries = Neighbors(state, at.Location).Where(to => VehicleEntryBar(state, vehicle, to) is null)
            .Select(to => VehicleEntryCost(state, vehicle, at.Location, to) is { } entry && Bearing(state, at.Location, to) is { } bearing
                ? entry + (2 * VcaTurns(facing, bearing)) : (int?)null)
            .OfType<int>().ToArray();
        return entries.Any(cost => cost <= left)
            ? $"play.vehicle-motion: {vehicle.Id} has {Mp(left)} MP left, enough to enter an ADJACENT hex, turning its VCA if need be, so it Stops (1 MP) or moves on before ending its move (D2.4)"
            : null;
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

        if (Is(vehicle, Conditions.Stunned) || Is(vehicle, Conditions.Recalled))
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

    /// <summary>
    /// Why fire from a Location at a target Location is refused for an AFV's effect on Infantry (ruling R25.9), or null: the target
    /// Location's Infantry share it with an AFV, whose +1 TEM is not reviewed (D9.3); or an AFV hex may lie between them, whose +1 LOS
    /// Hindrance is not reviewed (D9.4): a hex whose distances from the firer and the target sum to the range.
    /// </summary>
    private string? AfvCover(GameState state, IEnumerable<BoardLocation> firers, BoardLocation target, bool infantryTargets)
    {
        var afvs = state.Units.Where(unit => unit.Status == InstanceStatus.Active && IsAfv(unit)).Select(unit => (unit.Id, At: state.Location(unit.Id)?.Location))
            .Where(item => item.At is not null).ToArray();
        if (infantryTargets && afvs.FirstOrDefault(item => item.At == target) is { Id: { } shared })
        {
            return $"play.fire-afv-cover: the target Location's Infantry share it with the AFV {shared}, whose +1 TEM (D9.3) is not reviewed (ruling R25.9)";
        }

        foreach (var from in firers.Distinct())
        {
            var range = HexDistance(state, from, target);
            foreach (var (afv, at) in afvs.Where(item => item.At != from && item.At != target))
            {
                if (range is null || (HexDistance(state, from, at!) is { } one && HexDistance(state, at!, target) is { } two && one + two == range))
                {
                    return $"play.fire-afv-hindrance: the AFV {afv} in {at} may lie in the LOS, and its +1 Hindrance (D9.4) is not reviewed (ruling R25.9)";
                }
            }
        }

        return null;
    }
}
