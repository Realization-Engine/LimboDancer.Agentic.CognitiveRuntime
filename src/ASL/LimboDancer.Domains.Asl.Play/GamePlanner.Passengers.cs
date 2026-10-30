using LimboDancer.Dice;
using System.Text.Json;
using LimboDancer.Domains.Asl.Maps.Coordinates;
using LimboDancer.Domains.Asl.Maps.Geometry;
using LimboDancer.Domains.Asl.Rules;
using LimboDancer.Domains.Asl.Units.Documents;
using LimboDancer.Domains.Asl.Units.State;

namespace LimboDancer.Domains.Asl.Play;

/// <summary>
/// Vehicles on a card (pass 26 of the Card Play and Map Studio Redesign Plan; rulings R26.1 and R26.2): a vehicle waiting off board enters in Motion
/// across its edge as its first MP expenditure (A2.52, D2.4); Infantry board a vehicle as Passengers (D6.4) and disembark beneath it (D6.5), within its
/// Passenger capacity (D6.1, C10.13).
/// </summary>
public sealed partial class GamePlanner
{
    /// <summary>
    /// A vehicle's Passenger capacity in PP (D6.1), less four PP, or eight at 100mm or more, for a Gun it tows (C10.13); null when it carries none. A
    /// Carrier's is four PP, none with a mortar, FT, or 6 FP AAMG as MA (D6.81; pass 65, ruling R65.18).
    /// </summary>
    public static int? PassengerCapacity(GameState state, UnitInstance vehicle)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(vehicle);
        var definition = VehicleDefinition(vehicle);
        return TurnWorkEffects.PassengerCapacity(ScenarioA1PassengerCalculator.PassengerCapacity(ScenarioA1CarrierRules.PassengerCapacity(definition?.VehicleClass, definition?.PassengerCapacity,
                definition?.MainArmament, definition?.Caliber, definition?.AntiAircraftMg),
            state.Equipment.Where(item => item.Status == InstanceStatus.Active && item.Holding is { Role: HoldingRole.Towed } tow && tow.Holder == vehicle.Id)
                .Select(gun => gun.Definition is { } reference ? FireReference.Value.Definitions.GetValueOrDefault(reference.Definition)?.Caliber : null)), state.TurnAmmoSupplies.Any(supply => supply.Available && supply.Vehicle == vehicle.Id));
    }

    /// <summary>Whether a Carrier's inherent HS is aboard and may unload as its Passengers do (D6.83; pass 65, ruling R65.18); Rules decides.</summary>
    public static bool CrewMayUnload(GameState state, UnitInstance vehicle)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(vehicle);
        return ScenarioA1CarrierRules.IsCarrier(VehicleDefinition(vehicle)?.VehicleClass)
            && ScenarioA1CarrierRules.CrewUnloadBar(vehicle.Id, InherentHalfSquad(state, vehicle), Is(vehicle, Conditions.Abandoned)) is null;
    }

    /// <summary>The Passengers and Riders of a vehicle, read from the state's index once per state (ruling R65.25), as <see cref="GameState.Passengers"/> has them.</summary>
    public IReadOnlyList<UnitInstance> Riding(GameState state, string vehicle) => MemoOf(state).Objects(state).Aboard(vehicle);

    /// <summary>The vehicle an id rides, as <see cref="GameState.Aboard"/> has it, read from the same index.</summary>
    internal string? VehicleOf(GameState state, string id) => MemoOf(state).Objects(state).VehicleOf(id);

    /// <summary>The PP a unit takes as a Passenger (D6.1): a squad ten, a HS or crew five, a SMC none, plus the SW it carries; null when a SW's PP are not recorded.</summary>
    private int? PassengerPp(GameState state, UnitInstance unit) =>
        ScenarioA1PassengerCalculator.PassengerPp(vocabulary.IsA(unit.Kind, "asl:squad"), vocabulary.IsA(unit.Kind, "asl:half-squad") || vocabulary.IsA(unit.Kind, "asl:crew"),
            Portage(state, unit));

    /// <summary>
    /// Why a vehicle may not carry its Passengers with <paramref name="boarding"/> added (D6.1, A5.5, C10.13; ruling R26.2): it has no Passenger capacity,
    /// their PP exceed it, or more than four SMC ride; null when it may.
    /// </summary>
    internal string? CapacityBar(GameState state, UnitInstance vehicle, IEnumerable<UnitInstance> boarding)
    {
        // D6.2 (pass 65, ruling R65.16): Riders are counted apart, against the Rider capacity (RiderBar); a vehicle carrying only Riders has no Passengers to bar.
        UnitInstance[] aboard = [.. Riding(state, vehicle.Id).Where(unit => unit.Position is not ContainedPosition { Role: ContainmentRole.Rider }).Concat(boarding)
            .DistinctBy(unit => unit.Id)];
        if (aboard.Length == 0)
        {
            return null;
        }

        var capacity = PassengerCapacity(state, vehicle);
        if (capacity is null)
        {
            return ScenarioA1PassengerCalculator.CapacityBar(vehicle.Id, null, null, 0, []);
        }

        // D6.4 (pass 65, ruling R65.15): a SW left aboard counts against the Passenger PP capacity.
        return ScenarioA1PassengerCalculator.CapacityBar(vehicle.Id, capacity, VehicleDefinition(vehicle)?.PassengerCapacity, aboard.Count(unit => vocabulary.IsA(unit.Kind, "asl:smc")),
            aboard.Select(unit => (unit.Id, (Func<int?>)(() => PassengerPp(state, unit))))
                .Concat(MemoOf(state).Objects(state).Stowed(vehicle.Id).Select(weapon => (weapon.Id, (Func<int?>)(() => PortageOf(weapon))))));
    }

    /// <summary>
    /// Why a vehicle may not carry its Riders with <paramref name="boarding"/> added (D6.2, A5.5; pass 65, ruling R65.16): Riders are not allowed in the
    /// game's year for the vehicle's side, it is not an AFV that takes Riders, or their PP exceed its Rider capacity, counted apart from its Passenger PP;
    /// the referee's review: the vehicle is in terrain Riders are not allowed in (D6.21, B13.43), or the catalog gives a Rider no Morale Level for its Bail
    /// Out's NMC (D6.24). Null when it may, or when nobody rides it as a Rider.
    /// </summary>
    internal string? RiderBar(GameState state, UnitInstance vehicle, IEnumerable<UnitInstance> boarding)
    {
        UnitInstance[] riders = [.. RidersOf(state, vehicle.Id).Concat(boarding).DistinctBy(unit => unit.Id)];
        if (riders.Length == 0)
        {
            return null;
        }

        var definition = VehicleDefinition(vehicle);
        var bar = ScenarioA1RiderRules.DateBar(vehicle.Id, state.ScenarioYear, state.Side(vehicle.Side)?.Nationality)
            ?? ScenarioA1RiderRules.CapacityBar(vehicle.Id, ScenarioA1RiderRules.Capacity(definition?.VehicleClass, definition?.Unarmored == true),
                riders.Select(unit => (unit.Id, vocabulary.IsA(unit.Kind, "asl:smc"), (Func<int?>)(() => PassengerPp(state, unit)))));
        if (bar is null && vehicle.Position is MapPosition && MemoOf(state).Objects(state).Location(vehicle.Id)?.Location is { } at && ReadLocation(state, at) is { } read)
        {
            var terrain = read.Level.Terrain ?? read.Hex.Center.Terrain;
            bar = ScenarioA1RiderRules.LocationBar(vehicle.Id, TerrainKey(read), terrain?.Name, terrain?.IsRoad == true,
                terrain?.Name?.Contains("Stream", StringComparison.Ordinal) == true, vehicle.Straddling is not null);
        }

        foreach (var unit in riders)
        {
            var own = unit.Definition is { } reference ? FireReference.Value.Definitions.GetValueOrDefault(reference.Definition) : null;
            bar ??= ScenarioA1RiderRules.MoraleBar(unit.Id, own?.Morale, own?.BrokenMorale, own?.IsHero == true, own?.WoundedMorale);
        }

        return bar;
    }

    /// <summary>The Rider capacity of a vehicle in PP, or null when it takes none (D6.2), with whether it is one SMC's; for the Play page.</summary>
    public static RiderCapacity? RiderCapacityOf(UnitInstance vehicle)
    {
        ArgumentNullException.ThrowIfNull(vehicle);
        return VehicleDefinition(vehicle) is { } definition ? ScenarioA1RiderRules.Capacity(definition.VehicleClass, definition.Unarmored == true) : null;
    }

    /// <summary>
    /// Whether units board a vehicle as Riders (D6.2, D6.4; pass 65, the UI review): Rules decides from whether it takes Riders and Passengers and whether
    /// the player asked it. With <paramref name="asked"/> false, whether it takes Riders and no Passengers. For the Play page.
    /// </summary>
    public static bool BoardsAsRiders(GameState state, UnitInstance vehicle, bool asked) =>
        ScenarioA1RiderRules.BoardsAsRiders(RiderCapacityOf(vehicle) is not null, PassengerCapacity(state, vehicle) is not null, asked);

    /// <summary>Whether a vehicle keeps a SW its Passengers leave aboard (D6.4; pass 65, the UI review): Rules decides from its class. For the Play page.</summary>
    public static bool KeepsSwAboard(UnitInstance vehicle)
    {
        ArgumentNullException.ThrowIfNull(vehicle);
        return ScenarioA1PassengerCalculator.KeepsSwAboard(VehicleDefinition(vehicle)?.VehicleClass);
    }

    /// <summary>The Rider PP a vehicle has left (D6.2), its Riders read from the state's index; null when it takes no Riders. For the Play page.</summary>
    public int? RiderPpLeft(GameState state, UnitInstance vehicle)
    {
        ArgumentNullException.ThrowIfNull(state);
        return RiderCapacityOf(vehicle) is { } capacity ? capacity.Pp - RidersOf(state, vehicle.Id).Sum(unit => PassengerPp(state, unit) ?? 0) : null;
    }

    /// <summary>The two VCAs of a vehicle entering across a hexside: its VCA holds the hex it enters, so it faces a hexspine 30 degrees either side of its travel (D2.11).</summary>
    private static UnitFacing[] EntryFacings(HexsideDirection side) => [.. Rules.ScenarioA1Geometry.EntryFacings((int)side).Select(facing => (UnitFacing)facing)];

    /// <summary>
    /// The entries a vehicle waiting off board may make in this MPh (A2.5, A2.52, D2.4; ruling R26.1), each hex of its entry open to it this Game Turn with
    /// the two VCAs it may enter with, its cost in half MP, or why it may not enter there; empty when its entry turn has not come.
    /// </summary>
    public IReadOnlyList<(BoardLocation To, UnitFacing Facing, int? HalfMp, string? Bar)> VehicleEntryOptions(GameState state, UnitInstance vehicle)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(vehicle);
        if (vehicle.Position is not OffMapPosition || EntryFor(state, vehicle) is not { } entry || entry.Turn > state.Turn)
        {
            return [];
        }

        var options = new List<(BoardLocation, UnitFacing, int?, string?)>();
        foreach (var at in EntryHexesFor(state, entry))
        {
            if (EdgeSides(state, at).FirstOrDefault(item => item.Edge == entry.Edge) is not { Edge: not null } crossing)
            {
                continue;
            }

            var (cost, reason) = VehicleEdgeEntry(state, vehicle, at, crossing.Side, false);
            var bar = EntryHexBar(state, vehicle.Side, at, vehicle: true) ?? reason
                ?? (cost is not null ? ScenarioA1PassengerCalculator.EntryAllotmentBar(vehicle.Id, at.ToString(), cost.HalfMp, cost.All, PrintedHalfMp(vehicle)) : null);
            foreach (var facing in EntryFacings(crossing.Side))
            {
                options.Add((at, facing, bar is null ? (cost!.All ? PrintedHalfMp(vehicle) : cost.HalfMp) : null, bar));
            }
        }

        return options;
    }

    /// <summary>
    /// Why the phasing side's MPh may not end (A2.5; ruling R26.1): a vehicle whose entry turn has come waits off board and may enter a hex open to it.
    /// Vehicles cannot advance, so the MPh, not the APh, holds them.
    /// </summary>
    internal string? VehicleEntryDue(GameState state)
    {
        var waiting = state.Units.Where(unit => unit.Status == InstanceStatus.Active && unit.Side == state.PhasingSide && LiveFire.IsVehicle(unit) && unit.Position is OffMapPosition
            && !Is(unit, Conditions.Immobilized) && VehicleDefinition(unit) is { MovementPoints: not null }).OrderBy(unit => unit.Id, StringComparer.Ordinal)
            .Select(unit => (Unit: unit, Open: VehicleEntryOptions(state, unit).Where(option => option.Bar is null).Select(option => option.To).Distinct().ToArray()))
            .Where(item => item.Open.Length > 0).ToArray();
        return waiting.Length == 0 ? null
            : $"play.entry-due-vehicle: {string.Join(", ", waiting.Select(item => item.Unit.Id))} {(waiting.Length == 1 ? "waits" : "wait")} off board and must enter this MPh, "
                + $"since vehicles cannot advance: enter a hex of the entry edge, such as {string.Join(", ", waiting.SelectMany(item => item.Open).Distinct().Take(6))} (A2.5; ruling R26.1)";
    }

    /// <summary>
    /// A vehicle's entry from off board (A2.51, A2.52, D2.4; ruling R26.1): in its side's MPh of its entry Game Turn or later, in Motion so with no Start MP, as
    /// its first MP expenditure, into a hex of its edge open to it, from the mirror-image hex beyond, at that hex's cost, with a VCA holding the hex; its
    /// Passengers and towed Gun enter with it. The DEFENDER may fire at it as at any entry (A8.1).
    /// </summary>
    private GamePlan PlanVehicleEntry(GameScope scope, JsonElement arguments, IReadOnlyList<GameEvent> existing, string attemptId, long expected, string label, string actor,
        GameState state, UnitInstance vehicle, string kind)
    {
        var id = vehicle.Id;
        if (state.Phase != "mph" || vehicle.Side != state.PhasingSide || VehicleDefinition(vehicle) is not { MovementPoints: not null })
        {
            return Refused(scope, label, expected, $"play.entry-vehicle: {id} waits off board and enters in its side's MPh (A2.52; ruling R26.1)");
        }

        if (kind != VehicleStepped.Enter)
        {
            return Refused(scope, label, expected, $"play.entry-vehicle: {id} waits off board; its first MP expenditure is its entry onto the map, in Motion (A2.52, D2.4; ruling R26.1)");
        }

        if (state.Movement is { } current)
        {
            return Refused(scope, label, expected, $"play.move-order: {string.Join(", ", current.Members)} moves until its move ends (A4.2)");
        }

        if (MustCharge(state) is [{ } charging, ..])
        {
            return Refused(scope, label, expected, $"play.berserk-first: {charging.Id} is berserk and charges before any other unit moves (A15.43)");
        }

        if (!Text(arguments, "to", out var toText) || !BoardLocation.TryParse(toText, out var to))
        {
            return Refused(scope, label, expected, "play.invalid-arguments: an entry names the Location entered");
        }

        var (edge, side, reason) = EntryCheck(state, [vehicle], to, false, vehicleEntry: true);
        if (reason is not null)
        {
            return Refused(scope, label, expected, reason);
        }

        var facings = EntryFacings(side);
        var facing = facings[0];
        if (Text(arguments, "facing", out var facingText) && (!UnitFacings.TryParse(facingText, out facing) || !facings.Contains(facing)))
        {
            return Refused(scope, label, expected, $"play.entry-vca: {id} enters {to} with its VCA holding it: facing {UnitFacings.Name(facings[0])} or {UnitFacings.Name(facings[1])} (D2.11; ruling R26.1)");
        }

        var allMp = Flag(arguments, "allMp");
        var (entry, terrainBar) = VehicleEdgeEntry(state, vehicle, to, side, allMp);
        if (entry is null)
        {
            return Refused(scope, label, expected, $"play.move-vehicle-terrain: {id} may not enter {to} from off board: {terrainBar}");
        }

        var printed = PrintedHalfMp(vehicle);
        if (entry.HalfMp > printed && !entry.All)
        {
            return Refused(scope, label, expected, $"play.move-vehicle-mp: {to} costs {Mp(entry.HalfMp)} MP, more than {id}'s allotment (D2.1)");
        }

        var cost = entry.All ? printed : entry.HalfMp;
        var subject = PaatcSubjects(state, vehicle, to);
        var text = $"play.move-vehicle: {id} enters {to} from off board across the {edge} edge, in Motion, facing {UnitFacings.Name(facing)}, for {Mp(cost)} MP"
            + (entry.All ? " (ALL, B13.41)" : string.Empty) + (entry.BogDrm is { } bog ? $"; Bog DR at {bog:+0;-0;0} (D8.21)" : string.Empty)
            + (state.Passengers(id) is { Count: > 0 } riding ? $", with {string.Join(", ", riding.Select(unit => unit.Id))} aboard" : string.Empty)
            + $"; {Mp(Math.Max(0, HalfMp(vehicle).Allotment - cost))} MP left (A2.52, D2.4; ruling R26.1)";
        List<GameEvent> Build(Func<RollRequest, RollResult>? draw)
        {
            var events = new List<GameEvent>
            {
                Event(scope, attemptId, 1, expected, "vehicle-step", new VehicleStepped(id, VehicleStepped.Enter, to, facing, cost, 1) { Entry = edge, All = entry.All }, null, null),
            };
            if (entry.BogDrm is { } drm && draw is not null)
            {
                AddVehicleCheck(scope, attemptId, expected, actor, events, draw, id, VehicleCheckRolled.Bog, drm);
            }

            if (subject.Count > 0)
            {
                events.Add(Event(scope, attemptId, events.Count + 1, expected, "choice-pending", PaatcChoice(state, vehicle, to, subject), null, null));
            }

            return events;
        }

        return VehicleStepPlan(scope, label, attemptId, expected, actor, existing, Build, entry.BogDrm is not null, to, 1,
            text + (subject.Count > 0 ? $"; the concealed units in {to} are revealed or take a PAATC, as their owner chooses (A12.41)" : string.Empty));
    }

    /// <summary>
    /// Infantry board a vehicle (D6.4; ruling R26.2): the vehicle is stopped, not in Motion, not in Bypass, and has spent no MP this MPh; the units are
    /// Good Order, unpinned Personnel of its side in its Location that have not ended their move, each spending one MF and then riding with its move ended;
    /// the vehicle keeps a quarter of its MP allotment for each MF left to the unit that spent the most (FRD to the half MP). Within its capacity (D6.1).
    /// Since pass 65 (ruling R65.15) a moving stack boards as the last step of its own move, and a leader boarding with a laden MMC lends it his IPC (A4.42).
    /// </summary>
    private GamePlan PlanLoad(GameScope scope, JsonElement arguments, IReadOnlyList<GameEvent> existing, string attemptId, long expected, string label, string actor,
        GameState state, UnitInstance vehicle, BoardLocation at)
    {
        var id = vehicle.Id;
        var moving = state.Movement;
        if (moving is { Vehicle: true } || Is(vehicle, Conditions.Motion) || vehicle.MfSpent != 0 || vehicle.HalfMfSpent || StepsThisPhase(existing, id).Length > 0
            || vehicle.Straddling is not null)
        {
            return Refused(scope, label, expected, $"play.load: {id} takes Passengers only stopped, not in Motion or Bypass, before it spends any MP this MPh (D6.4)");
        }

        if (UnitIds(arguments) is not { Count: > 0 } ids)
        {
            return Refused(scope, label, expected, "play.invalid-arguments: a load names the units boarding");
        }

        // D6.4, A4.2 (pass 65, ruling R65.15): boarding as the last step of a stack's own move.
        if (ScenarioA1PassengerCalculator.EndOfMoveLoadBar(id, moving is not null, moving?.WindowOpen == true, moving?.Members ?? [], ids) is { } afterMoveBar)
        {
            return Refused(scope, label, expected, afterMoveBar);
        }

        var boarding = new List<UnitInstance>();
        var least = int.MaxValue;
        var index = MemoOf(state).Objects(state);
        foreach (var unitId in ids)
        {
            if (state.Unit(unitId) is not { Status: InstanceStatus.Active } unit || unit.Side != vehicle.Side || LiveFire.IsVehicle(unit) || !vocabulary.IsA(unit.Kind, "asl:personnel")
                || index.VehicleOf(unit.Id) is not null || index.Location(unit.Id)?.Location != at || unit.MovementEnded)
            {
                return Refused(scope, label, expected, $"play.load: {unitId} is not Infantry of {vehicle.Side} on foot in {at} that may still move (D6.4)");
            }

            if (new[] { Conditions.Broken, Conditions.Pinned, Conditions.Berserk, Conditions.Captured, Conditions.Melee, "asl:ti" }.FirstOrDefault(name => Is(unit, name)) is { } barred)
            {
                return Refused(scope, label, expected, $"play.load: {unitId} is marked {barred.Replace("asl:", string.Empty, StringComparison.Ordinal)} and may not board (D6.4)");
            }

            // A4.5, A4.11 (pass 40): no boarding after Double Time, and four MF for a unit that mounts.
            if (ScenarioA1PassengerCalculator.BoardingDoubleTimeBar(unitId, unit.DoubleTimeMf) is { } tiredBar)
            {
                return Refused(scope, label, expected, tiredBar);
            }

            boarding.Add(unit);
        }

        // A4.42, D6.4 (pass 65, ruling R65.15): a leader boarding with a laden MMC of his stack lends it his IPC, as he would on the move.
        var (recipient, lender) = LeaderIpcRecipient(state, [.. boarding], moving?.IpcTo);
        foreach (var unit in boarding)
        {
            if (MfAllotment(state, unit, 0, Is(unit, Conditions.Cx), 0, ScenarioA1PassengerCalculator.BoardingIpcBonus(unit.Id, recipient, lender),
                ScenarioA1PassengerCalculator.ConveyanceAllowance) is not { } allotment)
            {
                return Refused(scope, label, expected, $"play.load: {unit.Id}'s MF are not decided by the catalog (A4.1)");
            }

            var left = ScenarioA1PassengerCalculator.BoardingHalfMfLeft(allotment, unit.MfSpent, unit.HalfMfSpent);
            if (left < 0)
            {
                return Refused(scope, label, expected, $"play.load: {unit.Id} has no MF left to board, which costs one (D6.4)");
            }

            least = Math.Min(least, left / 2);
        }

        // D6.2 (pass 65, ruling R65.16): a load may board Riders, within the dates and the Rider capacity, counted apart from the Passengers'.
        var riders = Flag(arguments, "riders");
        if ((riders ? RiderBar(state, vehicle, boarding) : CapacityBar(state, vehicle, boarding)) is { } full)
        {
            return Refused(scope, label, expected, $"play.load: {full}");
        }

        var printed = PrintedHalfMp(vehicle);
        var kept = ScenarioA1PassengerCalculator.LoadKeptHalfMp(printed, least);
        var cost = printed - kept;
        var text = $"play.load: {string.Join(", ", boarding.Select(unit => unit.Id))} board{(boarding.Count == 1 ? "s" : string.Empty)} {id} in {at}"
            + (riders ? " as Riders (D6.2)" : string.Empty) + " for one MF"
            + (moving is not null ? $", the last step of {(boarding.Count == 1 ? "its" : "their")} move" : string.Empty)
            + (recipient is not null ? $", {lender} lending {recipient} his IPC (A4.42)" : string.Empty)
            + $"; {id} keeps {Mp(kept)} of its {Mp(printed)} MP (D6.4; ruling R26.2)"
            + "; the DEFENDER may fire at the boarding units as Infantry on foot with FFNAM, and one left pinned or broken does not board (D5.43, D6.5)";
        return VehicleStepPlan(scope, label, attemptId, expected, actor, existing,
            _ => [Event(scope, attemptId, 1, expected, "vehicle-step", new VehicleStepped(id, VehicleStepped.Load, at, null, cost, 1) { Units = [.. boarding.Select(unit => unit.Id)], Riders = riders }, null, null)],
            false, at, 1, text);
    }

    /// <summary>
    /// Passengers disembark beneath their vehicle (D6.5; ruling R26.2): only while it is stopped in its own MPh, not in Bypass, and not after it has spent more
    /// than three-fourths of its printed MP; the vehicle spends a quarter of its allotment (FRU) for the stack, and each unit one MF plus one for each quarter
    /// (FRU) the vehicle spent before. They may then move on with the MF left.
    /// </summary>
    private GamePlan PlanUnload(GameScope scope, JsonElement arguments, IReadOnlyList<GameEvent> existing, string attemptId, long expected, string label, string actor,
        GameState state, UnitInstance vehicle, BoardLocation at, int step)
    {
        var id = vehicle.Id;
        var current = state.Movement;
        var stopped = current is null ? !Is(vehicle, Conditions.Motion) : current.Stopped || !current.Started;
        if (!stopped || vehicle.Straddling is not null)
        {
            return Refused(scope, label, expected, $"play.unload: {id} unloads Passengers only while stopped, not in Motion or Bypass; it must Stop first (D6.5)");
        }

        // D6.83 (pass 65, ruling R65.18): a Carrier's inherent HS may unload as its Passengers do, with them or alone, and the Carrier is Abandoned.
        var crewUnloads = arguments.TryGetProperty("crew", out var crewArgument) && crewArgument.ValueKind == JsonValueKind.True;
        if ((UnitIds(arguments) ?? (crewUnloads ? new List<string>() : null)) is not { } ids || (ids.Count == 0 && !crewUnloads))
        {
            return Refused(scope, label, expected, "play.invalid-arguments: an unload names the Passengers disembarking");
        }

        InstanceCreated? crewLeaving = null;
        if (crewUnloads)
        {
            if (ScenarioA1CarrierRules.CrewUnloadBar(id, InherentHalfSquad(state, vehicle), Is(vehicle, Conditions.Abandoned)) is { } crewBar)
            {
                return Refused(scope, label, expected, crewBar);
            }

            // D5.42 (pass 65, ruling R65.20): a unit that re-crewed the Carrier leaves it as itself, by Abandoning it.
            if (MemoOf(state).Objects(state).CrewOf(id) is { } recrewed)
            {
                return Refused(scope, label, expected, $"play.unload: {recrewed.Id} crews {id} from inside and leaves it by Abandoning it (D5.41, D5.42)");
            }

            // The HS is created aboard, so the step unloads it as a Passenger: beneath the vehicle, with its MF and the DEFENDER's FFNAM (D6.5).
            // D3.43, D5.34 (the referee's review): it takes the Armor Leader, dormant, and the Stun "+1" with it.
            crewLeaving = LeavingCrew(state, vehicle, attemptId, Is(vehicle, Conditions.StunRecovery), vehicle.ArmorLeader) is { } placed
                ? new InstanceCreated(placed.Instance with
                {
                    Position = new ContainedPosition(id, ContainmentRole.Passenger)
                }) : null;
            if (crewLeaving is null)
            {
                return Refused(scope, label, expected, $"play.unload: the catalog has no HS or vehicle crew of {id}'s nationality (D6.82)");
            }
        }

        var aboard = state.Passengers(id);
        if (ids.FirstOrDefault(unitId => aboard.All(unit => unit.Id != unitId)) is { } stranger)
        {
            return Refused(scope, label, expected, $"play.unload: {stranger} is not a Passenger of {id} (D6.5)");
        }

        // D6.5, D.3 (pass 65, ruling R65.10): a Passenger that fired, or added its FP to the OVR, does not unload in that phase.
        var overran = OverranThisPhase(existing);
        foreach (var unit in aboard.Where(unit => ids.Contains(unit.Id)))
        {
            if (ScenarioA1PassengerActionRules.UnloadAfterFireBar(unit.Id, LiveFire.Fired(unit), overran.Contains(unit.Id)) is { } firedBar)
            {
                return Refused(scope, label, expected, firedBar);
            }
        }

        var printed = PrintedHalfMp(vehicle);
        var (spent, allotment) = HalfMp(vehicle);
        if (ScenarioA1PassengerCalculator.UnloadTooLate(spent, printed))
        {
            return Refused(scope, label, expected, $"play.unload: {id} has spent more than three-fourths of its MP, so its Passengers may not unload this MPh (D6.5)");
        }

        var quarter = ScenarioA1PassengerCalculator.UnloadHalfMp(printed);
        if (spent + quarter > allotment)
        {
            return Refused(scope, label, expected, $"play.unload: unloading costs {id} a quarter of its MP, {Mp(quarter)}, and it has {Mp(Math.Max(0, allotment - spent))} left (D6.5)");
        }

        // D6.4 (pass 65, ruling R65.15): the SW the unloaders leave aboard a halftrack or Carrier, and those left aboard that they take with them, since
        // "SW carried by a vehicle's Passenger PP capacity can be unloaded/Recovered only by Passengers of the same vehicle" (D6.5).
        var index = MemoOf(state).Objects(state);
        string[] leave = [.. Strings(arguments, "leave")];
        if (ScenarioA1PassengerCalculator.LeaveAboardBar(id, VehicleDefinition(vehicle)?.VehicleClass, leave.Select(weapon => (weapon,
            state.Find(weapon) is EquipmentInstance { Status: InstanceStatus.Active, Holding: { Role: HoldingRole.Possessed } holding } && ids.Contains(holding.Holder)))) is { } leaveBar)
        {
            return Refused(scope, label, expected, leaveBar);
        }

        var stowed = index.Stowed(id);
        var take = new List<(string Weapon, string Unit)>();
        if (arguments.TryGetProperty("take", out var takeArgument) && takeArgument.ValueKind == JsonValueKind.Object)
        {
            foreach (var pick in takeArgument.EnumerateObject())
            {
                if (stowed.All(weapon => weapon.Id != pick.Name) || pick.Value.ValueKind != JsonValueKind.String || !ids.Contains(pick.Value.GetString()!))
                {
                    return Refused(scope, label, expected, $"play.unload: {pick.Name} is not a SW left aboard {id} taken by a Passenger that unloads (D6.5)");
                }

                take.Add((pick.Name, pick.Value.GetString()!));
            }
        }

        // D6.5, A4.14: "PRC may unload ... in an enemy-occupied hex with no special rules or consequences"; a CC counter shows they are not held in Melee.
        // Pass 70 (ruling R70.32): the counter is no stored condition, and the line names only a Known enemy unit, which the unloading side may see.
        var enemyThere = EnemyLocation(index.At(at), vehicle.Side);
        var mf = ScenarioA1PassengerCalculator.UnloadMf(spent, printed);
        List<string> leavers = [.. ids];
        List<string> named = [.. ids];
        if (crewLeaving is not null)
        {
            // The UI review: the HS the step creates is named by its definition, which the page says as its counter ("its inherent 2-4-7 half-squad").
            leavers.Add(crewLeaving.Instance.Id);
            named.Add($"its inherent {crewLeaving.Instance.Definition}");
        }

        var text = $"play.unload: {string.Join(", ", named)} disembark{(leavers.Count == 1 ? "s" : string.Empty)} beneath {id} in {at} for {Mp(quarter)} of its MP; "
            + $"{(leavers.Count == 1 ? "it spends" : "each spends")} {mf} MF more"
            + (Is(vehicle, Conditions.PrepFire) ? $" and stays in {at} this MPh, since {id} Prep Fired" : " and may move on with the MF left, at most four, without Double Time, once "
                + $"{id} ends its move and before any other unit moves (A4.11, A4.5)") + " (D6.5; ruling R26.2)"
            + (leave.Length > 0 ? $"; {string.Join(", ", leave)} {(leave.Length == 1 ? "stays" : "stay")} aboard (D6.4)" : string.Empty)
            + string.Concat(take.Select(item => $"; {item.Unit} takes {item.Weapon} with it (D6.5)"))
            + (enemyThere ? ScenarioA1PassengerActionRules.EnemyLocationText(at.ToString(), "D6.5") : string.Empty)
            + "; the DEFENDER may fire at them with FFNAM, and FFMO in Open Ground from an unarmored vehicle (D6.5)"
            + (crewLeaving is not null ? $"; its inherent HS leaves {id}, so {id} is Abandoned, and the HS neither Removes its armament nor destroys it (D6.83)"
                : string.Empty);
        List<GameEvent> Build(Func<RollRequest, RollResult>? draw)
        {
            // D6.83: the inherent HS is created aboard and the Carrier Abandoned (D5.41) before the step unloads it, as an Abandoned vehicle's Passengers unload.
            List<GameEvent> events = crewLeaving is null ? []
                : [Event(scope, attemptId, 1, expected, "instance-created", crewLeaving, null, null),
                    Event(scope, attemptId, 2, expected, "conditions-changed", new ConditionsChanged(id, ConditionChanges(ScenarioA1OrdnanceEventRules.AbandonedConditions)), null, null)];
            events.Add(Event(scope, attemptId, events.Count + 1, expected, "vehicle-step",
                new VehicleStepped(id, VehicleStepped.Unload, at, null, quarter, step) { Units = [.. leavers], UnitMf = mf, Interim = true }, null, null));

            foreach (var weapon in leave)
            {
                events.Add(Event(scope, attemptId, events.Count + 1, expected, "equipment-transferred",
                    new EquipmentTransferred(weapon, null, new ContainedPosition(id, ContainmentRole.Passenger)), null, null));
            }

            foreach (var (weapon, unit) in take)
            {
                events.Add(Event(scope, attemptId, events.Count + 1, expected, "equipment-transferred",
                    new EquipmentTransferred(weapon, new Holding(unit, HoldingRole.Possessed), null), null, null));
            }

            return events;
        }

        return VehicleStepPlan(scope, label, attemptId, expected, actor, existing, Build, false, at, step, text);
    }

    /// <summary>The two hexes a vehicle's VCA points at (D2.11), for the hex it wished to enter next (D2.4; backlog section 21); empty off the map.</summary>
    public IReadOnlyList<BoardLocation> VehicleVcaHexes(GameState state, UnitInstance vehicle)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(vehicle);
        return state.Location(vehicle.Id) is { } at && vehicle.Position is MapPosition { Facing: { } facing } ? VcaHexes(state, at.Location, facing) : [];
    }

    /// <summary>The units that boarded or disembarked a vehicle this MPh (A4.11, A4.5; pass 65, ruling R65.15), read once per plan from the phase's steps.</summary>
    private static HashSet<string> ConveyedThisPhase(IReadOnlyList<GameEvent> existing) =>
        new(ThisPhase(existing).Select(item => item.Payload).OfType<VehicleStepped>().Where(step => step.Kind is VehicleStepped.Load or VehicleStepped.Unload)
            .SelectMany(step => step.Units ?? []), StringComparer.Ordinal);

    /// <summary>The unit ids an action's <c>units</c> argument names.</summary>
    private static List<string>? UnitIds(JsonElement arguments) =>
        arguments.TryGetProperty("units", out var units) && units.ValueKind == JsonValueKind.Array
            ? [.. units.EnumerateArray().Where(item => item.ValueKind == JsonValueKind.String).Select(item => item.GetString()!)]
            : null;

    /// <summary>
    /// Why an action names a unit riding a vehicle (ruling R26.2): Passengers ride, unload (D6.5), and, since pass 65, fire, pass a SW, and rout beneath
    /// their vehicle (rulings R65.10, R65.11); their other actions are not built. Null when no unit named is aboard.
    /// </summary>
    private string? AboardBar(GameState state, JsonElement arguments)
    {
        // Ruling R65.25: the index answers Aboard for every argument; only an id aboard is looked up.
        var index = MemoOf(state).Objects(state);
        foreach (var text in Strings(arguments))
        {
            if (index.VehicleOf(text) is { } vehicle && state.Find(text) is UnitInstance { Status: InstanceStatus.Active } unit)
            {
                return unit.Position is ContainedPosition { Role: ContainmentRole.Crew }
                    ? $"play.aboard: {unit.Id} crews {vehicle} from inside; it acts only as the vehicle's crew until it Abandons it (D5.41, D5.42)"
                    : $"play.aboard: {unit.Id} rides {vehicle} as a {(unit.Position is ContainedPosition { Role: ContainmentRole.Rider } ? "Rider" : "Passenger")}; until it unloads (D6.5) it fires, passes a SW, routs beneath, and otherwise acts only with its vehicle, since Passengers' other actions are not built (ruling R26.2)";
            }
        }

        return null;

        static IEnumerable<string> Strings(JsonElement element)
        {
            switch (element.ValueKind)
            {
                case JsonValueKind.String:
                    yield return element.GetString()!;
                    break;
                case JsonValueKind.Array:
                    foreach (var item in element.EnumerateArray().SelectMany(Strings))
                    {
                        yield return item;
                    }

                    break;
                case JsonValueKind.Object:
                    foreach (var property in element.EnumerateObject().SelectMany(property => Strings(property.Value)))
                    {
                        yield return property;
                    }

                    break;
            }
        }
    }
}
