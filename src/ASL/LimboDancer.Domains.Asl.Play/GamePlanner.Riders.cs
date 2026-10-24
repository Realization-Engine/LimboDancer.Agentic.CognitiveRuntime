using System.Globalization;
using LimboDancer.Dice;
using LimboDancer.Domains.Asl.Maps.Coordinates;
using LimboDancer.Domains.Asl.Rules;
using LimboDancer.Domains.Asl.Units.Documents;
using LimboDancer.Domains.Asl.Units.State;

namespace LimboDancer.Domains.Asl.Play;

/// <summary>
/// Riders Bailing Out (pass 65, task 65.4; ruling R65.17): the terrain and the TCA change that make them Bail Out as their AFV moves (D6.21, B13.43),
/// a pin or a break that does (D6.23), and the Bail Out's NMC and SW drs (D6.24). Who rides which vehicle is read from the state's index once per state
/// (ruling R65.25); Rules decides each rule, and the events here write what it decided.
/// </summary>
public sealed partial class GamePlanner
{
    /// <summary>A Rider that Bails Out: the unit as the state holds it, its id once the attack's results are written, its definition then, and whether it is broken.</summary>
    private sealed record BailingRider(UnitInstance Source, string Id, string? Definition, bool Broken, string Cause);

    /// <summary>The Riders of a vehicle, read from the state's index (ruling R65.25).</summary>
    private UnitInstance[] RidersOf(GameState state, string vehicleId) => [.. MemoOf(state).Objects(state).Aboard(vehicleId, ContainmentRole.Rider)];

    /// <summary>
    /// The Riders a vehicle's MP expenditure makes Bail Out, why, and where (D6.21, B13.43; ruling R65.17): an entry into or out of terrain Riders may not
    /// ride through puts them in the Location it leaves; a turreted AFV's TCA change puts them in its Location. Null when none must.
    /// </summary>
    private (IReadOnlyList<BailingRider> Riders, BoardLocation At)? StepBailOut(GameState state, UnitInstance vehicle, string kind, BoardLocation from, VehicleEntry? entry,
        UnitFacing? turret)
    {
        var riders = RidersOf(state, vehicle.Id);
        if (riders.Length == 0)
        {
            return null;
        }

        var cause = kind == VehicleStepped.Enter && entry is not null && ScenarioA1RiderRules.TerrainBailOut(TerrainBailOutFacts(state, vehicle, from, entry))
            ? ScenarioA1RiderRules.Terrain
            : ScenarioA1RiderRules.TurretBailOut(LiveFire.MaTypeOf(vehicle.Definition?.Definition) is "t" or "st" or "rst" or "1mt",
                (int?)LiveOrdnance.TurretFacing(state, vehicle), (int?)turret) ? ScenarioA1RiderRules.Turret : null;
        return cause is null ? null
            : ([.. riders.Select(unit => new BailingRider(unit, unit.Id, unit.Definition?.Definition, Is(unit, Conditions.Broken), cause))],
                cause == ScenarioA1RiderRules.Terrain || kind != VehicleStepped.Enter || entry is null ? from : entry.To);
    }

    /// <summary>
    /// The Riders a turreted AFV's TCA change other than by an MP expenditure makes Bail Out in its Location (D6.21; the referee's review of pass 65): its
    /// ordnance or Canister shot's turn, its CMG's, or a Motion attempt's. Null when the vehicle is no AFV in play on the map, does not turn, or has no Riders.
    /// </summary>
    private (IReadOnlyList<BailingRider> Riders, BoardLocation At)? TurretTurnBailOut(GameState state, string? vehicleId, UnitFacing? turnedTo)
    {
        if (turnedTo is null || vehicleId is null || state.Unit(vehicleId) is not { Status: InstanceStatus.Active, Position: MapPosition } vehicle
            || MemoOf(state).Objects(state).Location(vehicleId)?.Location is not { } at)
        {
            return null;
        }

        // A kind other than an entry reads the TCA change alone, and puts the Riders in the Location it is in.
        return StepBailOut(state, vehicle, VehicleStepped.Turn, at, null, turnedTo);
    }

    /// <summary>The terrain of a vehicle's entry as D6.21 and B13.43 read it: the map's reads of the Locations left and entered and of the hexside crossed.</summary>
    private RiderTerrainFacts TerrainBailOutFacts(GameState state, UnitInstance vehicle, BoardLocation from, VehicleEntry entry)
    {
        var (fromRead, toRead, _, crossed) = Step(state, from, entry.To);
        var enteredName = toRead is null ? null : (toRead.Level.Terrain ?? toRead.Hex.Center.Terrain)?.Name;

        // A Vehicular-Sized Entrance (B23.742) is not on the map read, so a building is always entered without one.
        return new RiderTerrainFacts(fromRead is null ? null : TerrainKey(fromRead), toRead is null ? null : TerrainKey(toRead), enteredName, crossed?.HexsideTerrain?.Name,
            entry.Road, entry.Bypass || vehicle.Straddling is not null, enteredName?.Contains("Stream", StringComparison.Ordinal) == true, false);
    }

    /// <summary>
    /// The Riders an attack's results make Bail Out (D6.23; ruling R65.17): each Rider a Collateral Attack left pinned or broken, not eliminated, of a
    /// vehicle the attack did not destroy, named as the attack's records leave it (a squad Reduced to a HS is the HS).
    /// </summary>
    private static List<BailingRider> CombatBailOuts(GameState state, FireVehicleEffect vehicle, string attemptId)
    {
        if (vehicle.Result is FireVehicleEffect.Eliminated or FireVehicleEffect.BurningWreck)
        {
            return [];
        }

        var bailing = new List<BailingRider>();
        foreach (var effect in (vehicle.PrcAttacks ?? []).SelectMany(attack => attack.Effects))
        {
            if (state.Unit(effect.UnitId) is { Status: InstanceStatus.Active, Position: ContainedPosition { Role: ContainmentRole.Rider } } unit
                && ScenarioA1RiderRules.CombatBailOut(true, effect.Eliminated, effect.Pinned, effect.Broken))
            {
                var replaced = effect.FinalDefinitionId != effect.DefinitionId;
                bailing.Add(new BailingRider(unit, replaced ? $"{attemptId}-{unit.Id}" : unit.Id, effect.FinalDefinitionId, effect.Broken || Is(unit, Conditions.Broken),
                    effect.Broken ? ScenarioA1RiderRules.Break : ScenarioA1RiderRules.Pin));
            }
        }

        return bailing;
    }

    /// <summary>
    /// The events of Riders Bailing Out (D6.24; ruling R65.17): for each, its NMC's DR and each SW's dr, the <c>rider-bailed-out</c> record, and what the
    /// NMC did: a pin, a break, a Casualty Reduction, or an elimination. A SW is eliminated with no dr when the unit breaks.
    /// </summary>
    private void AddBailOuts(GameScope scope, string attemptId, long expected, string actor, string? package, List<GameEvent> events, Func<RollRequest, RollResult> draw,
        GameState state, IReadOnlyList<BailingRider> riders, string vehicleId, BoardLocation at, IReadOnlyList<string>? causes)
    {
        string Roll(int count, string purpose)
        {
            var drawn = draw(new RollRequest(count, 6));
            var rollId = $"{attemptId}-roll-{(events.Count(item => item.Payload is DiceRolled) + 1).ToString(CultureInfo.InvariantCulture)}";
            events.Add(Event(scope, attemptId, events.Count + 1, expected, "dice-rolled", new DiceRolled(rollId, purpose, count, 6, drawn.Values, DiceRolled.SystemSource, actor),
                package, null, causes));
            return rollId;
        }

        foreach (var rider in riders)
        {
            // D6.24: a Rider's Morale Level is in the catalog, which boarding and setup check (RiderBar).
            var definition = rider.Definition is { } known ? FireReference.Value.Definitions.GetValueOrDefault(known) : null;
            var morale = (rider.Broken ? definition?.BrokenMorale : definition?.Morale)
                ?? throw new InvalidOperationException($"The catalog gives the Rider {rider.Id} no Morale Level (D6.24).");
            var drawn = draw(new RollRequest(2, 6));
            var rollId = $"{attemptId}-roll-{(events.Count(item => item.Payload is DiceRolled) + 1).ToString(CultureInfo.InvariantCulture)}";
            events.Add(Event(scope, attemptId, events.Count + 1, expected, "dice-rolled",
                new DiceRolled(rollId, "rider-bail-out", 2, 6, drawn.Values, DiceRolled.SystemSource, actor), package, null, causes));
            var result = ScenarioA1RiderRules.Check(drawn.Values[0], drawn.Values[1], morale, rider.Broken);
            var carrierBroken = ScenarioA1RiderRules.LeavesBroken(result, rider.Broken);
            var weapons = new List<RiderWeapon>();
            foreach (var weapon in state.Equipment.Where(item => item.Status == InstanceStatus.Active && item.Holding is { Role: HoldingRole.Possessed } holding
                && holding.Holder == rider.Source.Id))
            {
                var weaponRoll = carrierBroken ? null : Roll(1, "rider-weapon");
                var dr = weaponRoll is null ? (int?)null : ((DiceRolled)events[^1].Payload).Values[0];
                weapons.Add(new RiderWeapon(weapon.Id, weaponRoll, ScenarioA1RiderRules.WeaponFate(dr,
                    weapon.Definition is { } held && FireReference.Value.Definitions.GetValueOrDefault(held.Definition)?.BreakdownRemoves == true, carrierBroken)));
            }

            events.Add(Event(scope, attemptId, events.Count + 1, expected, "rider-bailed-out",
                new RiderBailedOut(rider.Id, vehicleId, at, rider.Cause, rollId, morale, result) { Weapons = weapons.Count == 0 ? null : weapons }, package, null, causes));

            // A7.8, A10.3, A10.31: what the NMC did, on the unit now on foot beneath the AFV. An unbroken unit it breaks or Reduces is broken first, so a HS
            // left by the Reduction keeps the broken counter.
            if (!rider.Broken && result is ScenarioA1RiderRules.Pinned or ScenarioA1RiderRules.Broken or ScenarioA1RiderRules.Reduced)
            {
                events.Add(Event(scope, attemptId, events.Count + 1, expected, "conditions-changed", new ConditionsChanged(rider.Id,
                    new Dictionary<string, ConditionState>(StringComparer.Ordinal)
                    {
                        [result == ScenarioA1RiderRules.Pinned ? Conditions.Pinned : Conditions.Broken] = ConditionState.True
                    }), package, null, causes));
            }

            if (result == ScenarioA1RiderRules.Reduced)
            {
                var (type, payload) = CasualtyReduction(rider.Source with
                {
                    Id = rider.Id,
                    Kind = rider.Definition is { } final && FireReference.Value.Definitions.GetValueOrDefault(final) is { } replaced ? replaced.Kind : rider.Source.Kind,
                    Definition = rider.Definition is { } named && rider.Source.Definition is { } reference ? reference with
                    {
                        Definition = named
                    } : rider.Source.Definition,
                    Position = new MapPosition(at),
                    Conditions = new Dictionary<string, ConditionState>(rider.Source.Conditions, StringComparer.Ordinal) { [Conditions.Broken] = ConditionState.True },
                }, attemptId, null);
                events.Add(Event(scope, attemptId, events.Count + 1, expected, type, payload, package, null, causes));
            }
            else if (result == ScenarioA1RiderRules.Eliminated)
            {
                events.Add(Event(scope, attemptId, events.Count + 1, expected, "instance-eliminated", new InstanceEliminated(rider.Id), package, null, causes));
            }
        }
    }

    /// <summary>What a proposal says of the Riders an MP expenditure makes Bail Out (D6.21, D6.24; ruling R65.17); empty when none does.</summary>
    private static string BailOutPreview((IReadOnlyList<BailingRider> Riders, BoardLocation At)? bailing) =>
        bailing is not { } bail ? string.Empty
            : $"; {string.Join(", ", bail.Riders.Select(rider => rider.Id))} Bail{(bail.Riders.Count == 1 ? "s" : string.Empty)} Out in {bail.At}, since "
                + ScenarioA1RiderRules.CauseText(bail.Riders[0].Cause).Replace("its AFV", "the AFV", StringComparison.Ordinal)
                + ", each taking a NMC beneath it with its move ended, its SW checked for malfunction (D6.24)";
}
