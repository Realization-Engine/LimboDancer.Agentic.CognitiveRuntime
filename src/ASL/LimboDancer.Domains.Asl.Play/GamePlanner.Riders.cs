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

        // D6.21: whether the AFV is turreted is its armor reference's (ArmorDefinition.Turreted); where the Riders land is Rules'.
        var entering = kind == VehicleStepped.Enter && entry is not null;
        var cause = entering && ScenarioA1RiderRules.TerrainBailOut(TerrainBailOutFacts(state, vehicle, from, entry!))
            ? ScenarioA1RiderRules.Terrain
            : ScenarioA1RiderRules.TurretBailOut(OrdnanceReference.Value.Armor.Vehicles.GetValueOrDefault(vehicle.Definition?.Definition ?? string.Empty)?.Turreted == true,
                (int?)LiveOrdnance.TurretFacing(state, vehicle), (int?)turret) ? ScenarioA1RiderRules.Turret : null;
        return cause is null ? null
            : ([.. riders.Select(unit => new BailingRider(unit, unit.Id, unit.Definition?.Definition, Is(unit, Conditions.Broken), cause))],
                ScenarioA1RiderRules.BailsOutWhereItWas(cause, entering) ? from : entry!.To);
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

        // Pass 70 (ruling R70.9): the NMCs are resolved by the Morale Check member, with a Rider leader's DRM (A10.7 EXC), a SMC's Wound Severity dr (A17.11),
        // and ELR Replacement, never Disruption (A19.12). A side with no ELR declared keeps the check of pass 65, its SMC's wound included.
        var sideElr = state.Side(riders.Count > 0 ? riders[0].Source.Side ?? string.Empty : string.Empty)?.Elr;
        if (sideElr is not null)
        {
            AddMemberBailOuts(scope, attemptId, expected, actor, package, events, draw, state, riders, vehicleId, at, causes, sideElr.Value, Roll);
            return;
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
                // A17.11 (pass 70, the crash brief 3b names): a SMC's Casualty Reduction is a wound, and its Wound Severity dr is made at once.
                var onFoot = OnFoot(rider, at, broken: true);
                int? severity = null;
                if (WoundSeverityDue(onFoot))
                {
                    Roll(1, ScenarioA1Wounds.SeverityPurpose);
                    severity = ((DiceRolled)events[^1].Payload).Values[0];
                }

                var (type, payload) = CasualtyReduction(onFoot, attemptId, severity);
                events.Add(Event(scope, attemptId, events.Count + 1, expected, type, payload, package, null, causes));
            }
            else if (result == ScenarioA1RiderRules.Eliminated)
            {
                events.Add(Event(scope, attemptId, events.Count + 1, expected, "instance-eliminated", new InstanceEliminated(rider.Id), package, null, causes));
            }
        }
    }

    /// <summary>A Bailing Out Rider on foot beneath its AFV, under its id and definition once the attack's results are written, broken when it is.</summary>
    private static UnitInstance OnFoot(BailingRider rider, BoardLocation at, bool broken)
    {
        var conditions = new Dictionary<string, ConditionState>(rider.Source.Conditions, StringComparer.Ordinal);
        if (broken)
        {
            conditions[Conditions.Broken] = ConditionState.True;
        }

        if (rider.Cause == ScenarioA1RiderRules.Pin && !broken)
        {
            conditions[Conditions.Pinned] = ConditionState.True;
        }

        return rider.Source with
        {
            Id = rider.Id,
            Kind = rider.Definition is { } final && FireReference.Value.Definitions.GetValueOrDefault(final) is { } replaced ? replaced.Kind : rider.Source.Kind,
            Definition = rider.Definition is { } named && rider.Source.Definition is { } reference ? reference with
            {
                Definition = named
            } : rider.Source.Definition,
            Position = new MapPosition(at),
            Conditions = conditions,
        };
    }

    /// <summary>
    /// The Bail Outs of one vehicle's Riders through the Morale Check member (D6.24, A10.7 EXC, A17.11, A19.12, A19.13; pass 70, ruling R70.9): each NMC's DR,
    /// the member's resolution of them all (a Rider leader's DRM reaches his vehicle's Riders; Riders take no Heat of Battle and no LLMC, A15.1, A10.2), and
    /// for each Rider its SW drs, the <c>rider-bailed-out</c> record with its DRM and ELR, and its effects as a fire record's are written, on foot beneath the AFV.
    /// </summary>
    private void AddMemberBailOuts(GameScope scope, string attemptId, long expected, string actor, string? package, List<GameEvent> events, Func<RollRequest, RollResult> draw,
        GameState state, IReadOnlyList<BailingRider> riders, string vehicleId, BoardLocation at, IReadOnlyList<string>? causes, int sideElr, Func<int, string, string> roll)
    {
        var moving = state.Unit(vehicleId) is { } vehicle && VehicleMoved(state, vehicle, false);
        FireTarget Subject(UnitInstance unit, string id, string? definition, bool broken, bool pinned) => LiveFire.Target(state, unit, at) with
        {
            UnitId = id,
            DefinitionId = definition ?? unit.Definition?.Definition,
            Broken = broken,
            Pinned = pinned && !broken,
            Unarmed = Is(unit, Conditions.Unarmed) ? true : null,
            Aboard = new FireAboard(vehicleId, FirePrc.Rider, moving, null),
        };

        var rolls = new Dictionary<string, IReadOnlyList<int>>(StringComparer.Ordinal);
        var rollIds = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var rider in riders)
        {
            // D6.24: a Rider's Morale Level is in the catalog, which boarding and setup check (RiderBar).
            if (rider.Definition is not { } known || FireReference.Value.Definitions.GetValueOrDefault(known) is not { Morale: not null, BrokenMorale: not null })
            {
                throw new InvalidOperationException($"The catalog gives the Rider {rider.Id} no Morale Level (D6.24).");
            }

            var rollId = roll(2, "rider-bail-out");
            rollIds[rider.Id] = rollId;
            rolls[rider.Id] = ((DiceRolled)events[^1].Payload).Values;
        }

        // The vehicle's other Riders may lead the check (A10.7 EXC); a unit of a definition the catalog lacks is left out.
        var bailing = riders.Select(rider => rider.Source.Id).ToHashSet(StringComparer.Ordinal);
        FireTarget[] others = [.. RidersOf(state, vehicleId).Where(unit => !bailing.Contains(unit.Id) && DefinitionOf(unit) is not null)
            .Select(unit => Subject(unit, unit.Id, null, Is(unit, Conditions.Broken), Is(unit, Conditions.Pinned)))];
        FireTarget[] subjects = [.. riders.Select(rider => Subject(rider.Source, rider.Id, rider.Definition, rider.Broken,
            rider.Cause == ScenarioA1RiderRules.Pin || Is(rider.Source, Conditions.Pinned)))];
        var facts = new MoraleCheckFacts(MoraleCheckFacts.Nmc, subjects, sideElr, new FireRolls(null, null, rolls, null, null))
        {
            Others = others.Length > 0 ? others : null,
            NoQuarter = state.NoQuarter.Contains(riders[0].Source.Side ?? string.Empty, StringComparer.Ordinal) ? true : null,
        };
        var (resolution, _, _) = ResolveMoraleCheck(scope, attemptId, expected, actor, events, draw, facts, null, causes);
        foreach (var (rider, subject) in riders.Zip(subjects))
        {
            var effect = resolution.Effects.First(item => item.UnitId == rider.Id);
            var check = effect.Checks.First(item => item.Kind == "MC");
            var replaced = effect.Events.Any(item => item is "replaced-elr" or "replaced-by-two-half-squads");
            var result = ScenarioA1MoraleCheck.BailOutResult(check, rider.Broken, replaced);
            var carrierBroken = ScenarioA1RiderRules.LeavesBroken(result, rider.Broken);
            var weapons = new List<RiderWeapon>();
            foreach (var weapon in state.Equipment.Where(item => item.Status == InstanceStatus.Active && item.Holding is { Role: HoldingRole.Possessed } holding
                && holding.Holder == rider.Source.Id))
            {
                var weaponRoll = carrierBroken ? null : roll(1, "rider-weapon");
                var dr = weaponRoll is null ? (int?)null : ((DiceRolled)events[^1].Payload).Values[0];
                weapons.Add(new RiderWeapon(weapon.Id, weaponRoll, ScenarioA1RiderRules.WeaponFate(dr,
                    weapon.Definition is { } held && FireReference.Value.Definitions.GetValueOrDefault(held.Definition)?.BreakdownRemoves == true, carrierBroken)));
            }

            var recordId = EventId(attemptId, events.Count + 1);
            events.Add(Event(scope, attemptId, events.Count + 1, expected, "rider-bailed-out",
                new RiderBailedOut(rider.Id, vehicleId, at, rider.Cause, rollIds[rider.Id], check.MoraleLevel, result)
                {
                    Weapons = weapons.Count == 0 ? null : weapons,
                    Drm = (int)check.Drm.Sum(item => item.Value),
                    Elr = subject.Elr ?? sideElr,
                }, package, null, causes));

            // What the NMC did, on the unit now on foot beneath the AFV, as a fire record's effect is written.
            var onFoot = OnFoot(rider, at, rider.Broken);
            var footing = state with
            {
                Units = [.. state.Units.Where(unit => unit.Id != rider.Source.Id && unit.Id != rider.Id), onFoot]
            };
            foreach (var payload in EffectEvents(footing, effect, attemptId, false))
            {
                events.Add(Event(scope, attemptId, events.Count + 1, expected, payload.Type, payload.Payload, package, null, [recordId]));
            }
        }
    }

    /// <summary>
    /// What a proposal says of the Riders an MP expenditure makes Bail Out (D6.21, D6.24; ruling R65.17); empty when none does. Pass 70 (ruling R70.32): where
    /// they land with a Known enemy unit, they are in an enemy Location and not held in Melee (D6.24, D6.5), read from the planner's index.
    /// </summary>
    private string BailOutPreview(GameState state, (IReadOnlyList<BailingRider> Riders, BoardLocation At)? bailing)
    {
        if (bailing is not { } bail)
        {
            return string.Empty;
        }

        var index = MemoOf(state).Objects(state);
        var side = index.Unit(bail.Riders[0].Id)?.Side;
        var enemyThere = index.At(bail.At).OfType<UnitInstance>().Any(unit => unit.Status == InstanceStatus.Active && unit.Side != side && !LiveFire.IsVehicle(unit)
            && !Is(unit, Conditions.Captured) && KnownEnemy(unit));
        return $"; {string.Join(", ", bail.Riders.Select(rider => rider.Id))} Bail{(bail.Riders.Count == 1 ? "s" : string.Empty)} Out in {bail.At}, since "
            + ScenarioA1RiderRules.CauseText(bail.Riders[0].Cause).Replace("its AFV", "the AFV", StringComparison.Ordinal)
            // D6.21 (the table player): Riders thrown off by terrain stay in the hex the AFV left, not beneath it.
            + (bail.Riders[0].Cause == ScenarioA1RiderRules.Terrain ? $", each taking a NMC in {bail.At}, the hex the AFV left," : ", each taking a NMC beneath it")
            + " with its move ended, its SW checked for malfunction (D6.24)"
            + (enemyThere ? ScenarioA1PassengerActionRules.EnemyLocationText(bail.At.ToString(), "D6.24, D6.5") : string.Empty);
    }
}
