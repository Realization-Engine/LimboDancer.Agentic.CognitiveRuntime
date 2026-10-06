using LimboDancer.Dice;
using System.Text.Json;
using LimboDancer.Domains.Asl.Maps.Coordinates;
using LimboDancer.Domains.Asl.Maps.Geometry;
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
    /// <summary>A vehicle's Passenger capacity in PP (D6.1), less four PP, or eight at 100mm or more, for a Gun it tows (C10.13); null when it carries none.</summary>
    public static int? PassengerCapacity(GameState state, UnitInstance vehicle)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(vehicle);
        if (VehicleDefinition(vehicle)?.PassengerCapacity is not { } printed)
        {
            return null;
        }

        return printed - state.Equipment.Where(item => item.Status == InstanceStatus.Active && item.Holding is { Role: HoldingRole.Towed } tow && tow.Holder == vehicle.Id)
            .Sum(gun => (gun.Definition is { } reference ? FireReference.Value.Definitions.GetValueOrDefault(reference.Definition)?.Caliber : null) >= 100 ? 8 : 4);
    }

    /// <summary>The PP a unit takes as a Passenger (D6.1): a squad ten, a HS or crew five, a SMC none, plus the SW it carries; null when a SW's PP are not recorded.</summary>
    private int? PassengerPp(GameState state, UnitInstance unit) =>
        Portage(state, unit) is { } carried
            ? (vocabulary.IsA(unit.Kind, "asl:squad") ? 10 : vocabulary.IsA(unit.Kind, "asl:half-squad") || vocabulary.IsA(unit.Kind, "asl:crew") ? 5 : 0) + carried.Sum()
            : null;

    /// <summary>
    /// Why a vehicle may not carry its Passengers with <paramref name="boarding"/> added (D6.1, A5.5, C10.13; ruling R26.2): it has no Passenger capacity,
    /// their PP exceed it, or more than four SMC ride; null when it may.
    /// </summary>
    internal string? CapacityBar(GameState state, UnitInstance vehicle, IEnumerable<UnitInstance> boarding)
    {
        if (PassengerCapacity(state, vehicle) is not { } capacity)
        {
            return $"{vehicle.Id} carries no Passengers (D6.1)";
        }

        UnitInstance[] aboard = [.. state.Passengers(vehicle.Id).Concat(boarding).DistinctBy(unit => unit.Id)];
        if (aboard.Count(unit => vocabulary.IsA(unit.Kind, "asl:smc")) > 4)
        {
            return $"more than four SMC would ride {vehicle.Id}; up to four count as zero PP (D6.1, A5.5)";
        }

        var total = 0;
        foreach (var unit in aboard)
        {
            if (PassengerPp(state, unit) is not { } pp)
            {
                return $"the PP of what {unit.Id} carries are not recorded (A4.4)";
            }

            total += pp;
        }

        return total > capacity
            ? $"its Passengers would take {total} PP, and {vehicle.Id} carries {capacity} PP{(capacity < (VehicleDefinition(vehicle)?.PassengerCapacity ?? 0) ? " with its towed Gun's ammunition (C10.13)" : string.Empty)} (D6.1)"
            : null;
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
                ?? (cost is { All: false } && cost.HalfMp > PrintedHalfMp(vehicle) ? $"entering {at} costs {Mp(cost.HalfMp)} MP, more than {vehicle.Id}'s allotment" : null);
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
    /// </summary>
    private GamePlan PlanLoad(GameScope scope, JsonElement arguments, IReadOnlyList<GameEvent> existing, string attemptId, long expected, string label, string actor,
        GameState state, UnitInstance vehicle, BoardLocation at)
    {
        var id = vehicle.Id;
        if (state.Movement is not null || Is(vehicle, Conditions.Motion) || vehicle.MfSpent != 0 || vehicle.HalfMfSpent || StepsThisPhase(existing, id).Length > 0
            || vehicle.Straddling is not null)
        {
            return Refused(scope, label, expected, $"play.load: {id} takes Passengers only stopped, not in Motion or Bypass, before it spends any MP this MPh (D6.4)");
        }

        if (UnitIds(arguments) is not { Count: > 0 } ids)
        {
            return Refused(scope, label, expected, "play.invalid-arguments: a load names the units boarding");
        }

        var boarding = new List<UnitInstance>();
        var least = int.MaxValue;
        foreach (var unitId in ids)
        {
            if (state.Unit(unitId) is not { Status: InstanceStatus.Active } unit || unit.Side != vehicle.Side || LiveFire.IsVehicle(unit) || !vocabulary.IsA(unit.Kind, "asl:personnel")
                || state.Aboard(unit.Id) is not null || state.Location(unit.Id)?.Location != at || unit.MovementEnded)
            {
                return Refused(scope, label, expected, $"play.load: {unitId} is not Infantry of {vehicle.Side} on foot in {at} that may still move (D6.4)");
            }

            if (new[] { Conditions.Broken, Conditions.Pinned, Conditions.Berserk, Conditions.Captured, Conditions.Melee, "asl:ti" }.FirstOrDefault(name => Is(unit, name)) is { } barred)
            {
                return Refused(scope, label, expected, $"play.load: {unitId} is marked {barred.Replace("asl:", string.Empty, StringComparison.Ordinal)} and may not board (D6.4)");
            }

            if (MfAllotment(state, unit, 0, Is(unit, Conditions.Cx)) is not { } allotment)
            {
                return Refused(scope, label, expected, $"play.load: {unitId}'s MF are not decided by the catalog (A4.1)");
            }

            var left = (allotment * 2) - ((unit.MfSpent * 2) + (unit.HalfMfSpent ? 1 : 0)) - 2;
            if (left < 0)
            {
                return Refused(scope, label, expected, $"play.load: {unitId} has no MF left to board, which costs one (D6.4)");
            }

            least = Math.Min(least, left / 2);
            boarding.Add(unit);
        }

        if (CapacityBar(state, vehicle, boarding) is { } full)
        {
            return Refused(scope, label, expected, $"play.load: {full}");
        }

        var printed = PrintedHalfMp(vehicle);
        var kept = printed * Math.Min(least, 4) / 4;
        var cost = printed - kept;
        var text = $"play.load: {string.Join(", ", boarding.Select(unit => unit.Id))} board{(boarding.Count == 1 ? "s" : string.Empty)} {id} in {at} for one MF; {id} keeps {Mp(kept)} of its {Mp(printed)} MP (D6.4; ruling R26.2)";
        return VehicleStepPlan(scope, label, attemptId, expected, actor, existing,
            _ => [Event(scope, attemptId, 1, expected, "vehicle-step", new VehicleStepped(id, VehicleStepped.Load, at, null, cost, 1) { Units = [.. boarding.Select(unit => unit.Id)] }, null, null)],
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

        if (UnitIds(arguments) is not { Count: > 0 } ids)
        {
            return Refused(scope, label, expected, "play.invalid-arguments: an unload names the Passengers disembarking");
        }

        var aboard = state.Passengers(id);
        if (ids.FirstOrDefault(unitId => aboard.All(unit => unit.Id != unitId)) is { } stranger)
        {
            return Refused(scope, label, expected, $"play.unload: {stranger} is not a Passenger of {id} (D6.5)");
        }

        var printed = PrintedHalfMp(vehicle);
        var (spent, allotment) = HalfMp(vehicle);
        if (spent * 4 > printed * 3)
        {
            return Refused(scope, label, expected, $"play.unload: {id} has spent more than three-fourths of its MP, so its Passengers may not unload this MPh (D6.5)");
        }

        var quarter = ((printed / 2) + 3) / 4 * 2;
        if (spent + quarter > allotment)
        {
            return Refused(scope, label, expected, $"play.unload: unloading costs {id} a quarter of its MP, {Mp(quarter)}, and it has {Mp(Math.Max(0, allotment - spent))} left (D6.5)");
        }

        var before = printed == 0 ? 0 : ((spent * 4) + printed - 1) / printed;
        var mf = 1 + before;
        var text = $"play.unload: {string.Join(", ", ids)} disembark{(ids.Count == 1 ? "s" : string.Empty)} beneath {id} in {at} for {Mp(quarter)} of its MP; "
            + $"{(ids.Count == 1 ? "it spends" : "each spends")} {mf} MF more"
            + (Is(vehicle, Conditions.PrepFire) ? $" and stays in {at} this MPh, since {id} Prep Fired" : " and may move on with the MF left") + " (D6.5; ruling R26.2)";
        return VehicleStepPlan(scope, label, attemptId, expected, actor, existing,
            _ => [Event(scope, attemptId, 1, expected, "vehicle-step", new VehicleStepped(id, VehicleStepped.Unload, at, null, quarter, step) { Units = [.. ids], UnitMf = mf }, null, null)],
            false, at, step, text);
    }

    /// <summary>The two hexes a vehicle's VCA points at (D2.11), for the hex it wished to enter next (D2.4; backlog section 21); empty off the map.</summary>
    public IReadOnlyList<BoardLocation> VehicleVcaHexes(GameState state, UnitInstance vehicle)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(vehicle);
        return state.Location(vehicle.Id) is { } at && vehicle.Position is MapPosition { Facing: { } facing } ? VcaHexes(state, at.Location, facing) : [];
    }

    /// <summary>The unit ids an action's <c>units</c> argument names.</summary>
    private static List<string>? UnitIds(JsonElement arguments) =>
        arguments.TryGetProperty("units", out var units) && units.ValueKind == JsonValueKind.Array
            ? [.. units.EnumerateArray().Where(item => item.ValueKind == JsonValueKind.String).Select(item => item.GetString()!)]
            : null;

    /// <summary>
    /// Why an action names a unit riding a vehicle (ruling R26.2): Passengers only ride, and unload (D6.5); their fire, rout, and Close Combat are plan
    /// pass 34. Null when no unit named is aboard.
    /// </summary>
    private static string? AboardBar(GameState state, JsonElement arguments)
    {
        foreach (var text in Strings(arguments))
        {
            if (state.Find(text) is UnitInstance { Status: InstanceStatus.Active } unit && state.Aboard(unit.Id) is { } vehicle)
            {
                return $"play.aboard: {unit.Id} rides {vehicle} as a Passenger; until it unloads (D6.5) it acts only with its vehicle, since Passengers' fire, rout, and Close Combat are not built (ruling R26.2)";
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
