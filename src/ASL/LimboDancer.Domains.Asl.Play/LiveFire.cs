using System.Text.Json;
using System.Text.Json.Serialization;
using LimboDancer.Domains.Asl.Maps.Coordinates;
using LimboDancer.Domains.Asl.Rules;
using LimboDancer.Domains.Asl.Units.State;

namespace LimboDancer.Domains.Asl.Play;

/// <summary>
/// The facts of a live fire attack that the game state decides (Fire in Live Play, unit step 18): the phase and side,
/// the fire group and its director, the target Location's units, the target side's ELR, and the month. The map facts
/// (range, levels, LOS, terrain) are added by the planner from the map read.
/// </summary>
public static class LiveFire
{
    private static readonly Lazy<ScenarioA1FireReference> CatalogReference = new(() => new ScenarioA1FirePackage().Reference);

    /// <summary>A21.1 (ruling R13.7): a weapon of another nationality than its possessor's is captured (Rules decides it, pass 32.c).</summary>
    public static bool? CapturedBy(string? weaponDefinition, UnitInstance unit) =>
        ScenarioA1FireEligibility.CapturedBy(
            weaponDefinition is null ? null : CatalogReference.Value.Definitions.GetValueOrDefault(weaponDefinition)?.Nationality,
            unit.Definition is { } holder ? CatalogReference.Value.Definitions.GetValueOrDefault(holder.Definition)?.Nationality : null);

    public const string Catalog = "asl-scenario-a1";
    public const string CatalogVersion = "1.13.0";

    public static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    /// <summary>Reads recorded facts strictly: an unknown member refuses the record.</summary>
    public static readonly JsonSerializerOptions StrictJson = new(JsonSerializerDefaults.Web)
    {
        UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow,
    };

    /// <summary>
    /// The state's part of the attack, or the reason it cannot be read (units steps 18 to 23). The firers may span
    /// Locations; the first firer's Location is the group's. The targets are every active unit in the target Location, in
    /// ordinal id order, Dummies included; in the MPh they are the moving stack only (A8.1); the Location may hold none
    /// (ruling R21.1). <paramref name="weapons"/> names the MGs each firer uses, and <paramref name="withoutInherent"/> the
    /// firers whose MG fires again alone on its Multiple ROF (A9.2).
    /// </summary>
    public static (FireAttack? Attack, string? Reason) FromState(GameState state, IReadOnlyList<string> firerIds, IReadOnlyList<string> directorIds,
        BoardLocation target, IReadOnlyDictionary<string, IReadOnlyList<string>>? weapons = null, IReadOnlyCollection<string>? withoutInherent = null,
        IReadOnlyDictionary<string, string>? partners = null, string? molUser = null)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(firerIds);
        ArgumentNullException.ThrowIfNull(directorIds);
        if (firerIds.Count == 0 || firerIds.Distinct(StringComparer.Ordinal).Count() != firerIds.Count)
        {
            return (null, "play.fire-firers: name each firer once");
        }

        if (state.Catalog.Catalog != Catalog || state.Catalog.Version != CatalogVersion)
        {
            return (null, $"play.fire-catalog: the Fire package reads {Catalog}@{CatalogVersion}, and this game uses {state.Catalog.Catalog}@{state.Catalog.Version}");
        }

        // D1.83, D3.4 (ruling R25.7): a vehicle's MG fires alone, with no leader and no Infantry in its group. Rules decides each block (pass 32.c).
        var vehicleFirers = firerIds.Select(state.Unit).OfType<UnitInstance>().Where(IsVehicle).ToArray();
        if (ScenarioA1FireEligibility.VehicleFiresAlone(vehicleFirers.Length, firerIds.Count, directorIds.Count, weapons is { Count: > 0 }) is { } grouped)
        {
            return (null, grouped);
        }

        var firers = new List<(UnitInstance Unit, BoardLocation At)>();
        foreach (var id in firerIds)
        {
            var unit = state.Unit(id);
            BoardLocation? at = unit is null ? null : state.Location(unit.Id)?.Location;
            if (ScenarioA1FireEligibility.FirerBar(new FirerStateFacts(id, unit is { Status: InstanceStatus.Active, Definition: not null }, at is not null,
                unit is not null && Is(unit, "asl:ti"))) is { } barred)
            {
                return (null, barred);
            }

            firers.Add((unit!, at!));
        }

        var locations = firers.Select(item => item.At).Distinct().ToArray();
        var directors = new List<(UnitInstance Unit, BoardLocation At)>();
        foreach (var id in directorIds)
        {
            var leader = state.Unit(id);
            BoardLocation? at = leader is null ? null : state.Location(leader.Id)?.Location;
            if (ScenarioA1FireEligibility.DirectorBar(new DirectorStateFacts(id, leader is { Status: InstanceStatus.Active, Definition: not null },
                at is { } here ? locations.Contains(here) : null)) is { } barred)
            {
                return (null, barred);
            }

            directors.Add((leader!, at!));
        }

        var side = firers[0].Unit.Side;

        // D3.3, A8.1, A8.3, A8.31 (rulings R6.9, R12.3): the kind of MPh fire; the Bounding Fire read and the firers' marks are read when asked.
        var (kindRefusal, kind) = ScenarioA1FireEligibility.MphFireKind(state.Phase, new FireKindFacts(vehicleFirers.Length > 0,
            vehicleFirers.Length > 0 && vehicleFirers[0].Side == state.PhasingSide, () => MayBoundingFire(state, vehicleFirers[0].Id),
            () => [.. firers.Select(item => (Is(item.Unit, Conditions.FinalFire), Is(item.Unit, Conditions.FirstFire)))],
            firers.All(item => withoutInherent?.Contains(item.Unit.Id) != true), withoutInherent is { Count: > 0 }));
        if (kindRefusal is not null)
        {
            return (null, kindRefusal);
        }

        // A8.1, A7.308, A15.41: who the attack hits, as Rules sorts the target Location's units.
        var movers = state.Phase == "mph" && state.Movement is { } movement && movement.Location == target ? movement.Movers : null;
        UnitInstance[] atTarget = [.. state.At(target).OfType<UnitInstance>()];
        UnitInstance UnitAt(string id) => atTarget.First(unit => unit.Id == id);
        var selection = ScenarioA1FireEligibility.SelectTargets([.. atTarget.Select(unit => new TargetUnitStateFacts(unit.Id, unit.Status == InstanceStatus.Active, unit.Side,
            IsVehicle(unit), unit.Kind == UnitKinds.Dummy, unit.Definition is not null, movers?.Contains(unit.Id)))], movers is not null);
        if (selection.Refusal is { } outside)
        {
            return (null, outside);
        }

        var targetSide = ScenarioA1FireEligibility.TargetSide(side, selection.Targets.Select(id => UnitAt(id).Side),
            selection.Vehicles.Count > 0 ? UnitAt(selection.Vehicles[0]).Side : null, state.Sides.FirstOrDefault(item => item.Id != side)?.Id);
        var friendlyTargets = selection.Targets.Any(id => UnitAt(id).Side == side);

        FireWeapon Weapon(string id) => state.Find(id) is EquipmentInstance equipment
            ? new FireWeapon(equipment.Id, equipment.Definition?.Definition, Is(equipment, Conditions.Malfunctioned), Fired(equipment),
                Is(equipment, Conditions.FirstFire))
            : new FireWeapon(id, null, null, null, null);

        var firerFacts = new List<FireFirer>();
        foreach (var (unit, at) in firers.Where(item => !IsVehicle(item.Unit)))
        {
            IReadOnlyList<FireWeapon>? used = null;
            if (weapons?.TryGetValue(unit.Id, out var named) == true && named.Count > 0)
            {
                // A7.35, A9.8, A22.3, A7.351 (rulings R13.6, R12.4): the limits on the named weapons; the two state scans are read when asked.
                if (ScenarioA1FireEligibility.NamedWeaponsBar(new NamedWeaponsFacts(unit.Id,
                    [.. named.Select(id => new NamedWeaponFacts(id,
                        state.Find(id) is EquipmentInstance { Status: InstanceStatus.Active, Holding: { Role: HoldingRole.Possessed } holding } && holding.Holder == unit.Id,
                        state.Find(id) is EquipmentInstance held && Is(held, Conditions.Dismantled),
                        state.Find(id) is EquipmentInstance { Kind: "asl:ft" }))],
                    state.AssaultWeaponUsers.Contains(unit.Id, StringComparer.Ordinal), withoutInherent?.Contains(unit.Id) == true,
                    () => state.Equipment.Any(item => item.Status == InstanceStatus.Active && item.Holding is { Role: HoldingRole.Possessed } other && other.Holder == unit.Id
                        && !named.Contains(item.Id, StringComparer.Ordinal) && (Fired(item) || Is(item, Conditions.FirstFire))),
                    () => state.SupportWeaponUses.Any(item => item.Unit == unit.Id))) is { } weaponBar)
                {
                    return (null, weaponBar);
                }

                used = [.. named.Select(id => Weapon(id) is var weapon && CapturedBy(weapon.DefinitionId, unit) is { } captured ? weapon with { Captured = captured } : weapon)];
            }

            // A3.3, A7.1, A7.351: the phase's limits on a firer; the Prep Fire scan and the count of its other fired weapons are read when asked.
            if (ScenarioA1FireEligibility.FirerLimitBar(new FirerLimitFacts(unit.Id, state.Phase, Is(unit, Conditions.BoundingFire), unit.Kind == "asl:squad",
                withoutInherent?.Contains(unit.Id) == true, Fired(unit), used is { Count: > 0 },
                () => state.Equipment.Any(item => item.Status == InstanceStatus.Active && item.Holding is { Role: HoldingRole.Possessed } prepped && prepped.Holder == unit.Id
                    && Is(item, Conditions.PrepFire)),
                () => state.Equipment.Count(item => item.Status == InstanceStatus.Active && item.Holding is { Role: HoldingRole.Possessed } holder && holder.Holder == unit.Id
                    && !(used ?? []).Any(weapon => weapon.EquipmentId == item.Id) && (Fired(item) || Is(item, Conditions.FirstFire))))) is { } limit)
            {
                return (null, limit);
            }

            // A7.351 (rulings R9.2, R9.7): a squad whose only fire this phase is one SW use still fires its inherent FP.
            var swOnly = state.SupportWeaponUses.Any(item => item.Unit == unit.Id);

            // A9.12 (ruling R12.4): a leader fires a MG with no inherent FP of his own, alone or with a stacked SMC as his partner.
            var leader = unit.Kind == "asl:leader";
            string? partner = null;
            if (leader && partners?.TryGetValue(unit.Id, out var named2) == true)
            {
                var helper = state.Unit(named2);
                if (ScenarioA1FireEligibility.PartnerBar(unit.Id, new PartnerFacts(named2, helper is { Status: InstanceStatus.Active }, helper?.Kind is "asl:leader" or "asl:hero",
                    helper?.Side == unit.Side, helper is not null && state.Location(helper.Id)?.Location == at, firerIds.Contains(named2) || directorIds.Contains(named2),
                    helper is not null && Fired(helper), helper is not null && Is(helper, Conditions.Broken))) is { } partnerBar)
                {
                    return (null, partnerBar);
                }

                partner = helper!.Id;
            }

            firerFacts.Add(ScenarioA1FireEligibility.Firer(new FirerRecordFacts(unit.Id, unit.Definition!.Definition, at.ToString(), Is(unit, Conditions.Broken),
                Is(unit, Conditions.Pinned), Is(unit, Conditions.Concealed), Fired(unit), used is not null,
                unit.Group is not null ? state.ElrOf(unit) : null, Is(unit, Conditions.FirstFire), Is(unit, Conditions.FinalFire), state.Phase, swOnly,
                state.GunCrewsFired.Contains(unit.Id, StringComparer.Ordinal), unit.Kind == "asl:squad", leader, withoutInherent?.Contains(unit.Id) == true, used,
                Is(unit, Conditions.Fanatic), Is(unit, Conditions.Wounded), Is(unit, Conditions.Cx), Is(unit, Conditions.BoundingFire), state.Encircled(unit), partner,
                molUser == unit.Id, GreenInexperienced(state, unit))));
        }

        FireDirector Director((UnitInstance Unit, BoardLocation At) item) =>
            ScenarioA1FireEligibility.Director(new DirectorRecordFacts(item.Unit.Id, item.Unit.Definition!.Definition, item.At.ToString(), Is(item.Unit, Conditions.Broken),
                Is(item.Unit, Conditions.Pinned), Is(item.Unit, Conditions.Concealed), Fired(item.Unit), Is(item.Unit, Conditions.FirstFire),
                state.SupportWeaponDirectors.Any(directed => directed.Leader == item.Unit.Id), Is(item.Unit, Conditions.Wounded), Is(item.Unit, Conditions.Cx)));

        // Rulings R12.8, R12.9, R12.11: the firing side's units in a Melee or as prisoners, a prisoner's Guard, and Encirclement; Rules assembles the attack.
        return (ScenarioA1FireEligibility.Assemble(new AttackAssemblyFacts(state.Phase, side == state.PhasingSide, firers[0].At.ToString(), target.ToString(),
            firerFacts, [.. directors.Select(Director)], state.ScenarioMonth,
            [.. selection.Targets.Select(UnitAt).Select(unit => ScenarioA1FireEligibility.AttackTarget(new AttackTargetFacts(Target(state, unit, target), unit.Side == side,
                Is(unit, Conditions.Captured), unit.Custodian, state.Encircled(unit))))],
            targetSide is null ? null : state.Side(targetSide)?.Elr, kind, state.Movement?.Assault ?? false, () => state.Side(side)?.Elr, friendlyTargets,
            [.. selection.Vehicles.Select(id => Vehicle(UnitAt(id), target))], vehicleFirers.Length > 0 ? VehicleFire(state, vehicleFirers[0], firers[0].At) : null,
            targetSide is not null && state.NoQuarter.Contains(targetSide, StringComparer.Ordinal), state.NoQuarter.Contains(side, StringComparer.Ordinal),
            Allies(state, side), [.. selection.Companions.Select(id => Target(state, UnitAt(id), target))])), null);
    }

    /// <summary>
    /// A side's nationalities when it holds Allied Troops (A10.7; backlog pass 15, ruling R15.8): every nationality of its units, in order; null when it
    /// holds one alone.
    /// </summary>
    public static IReadOnlyList<string>? Allies(GameState state, string side)
    {
        ArgumentNullException.ThrowIfNull(state);
        return ScenarioA1FireEligibility.Allies(state.Units.Where(unit => unit.Side == side && unit.Definition is { } reference
                && CatalogReference.Value.Definitions.ContainsKey(reference.Definition))
            .Select(unit => CatalogReference.Value.Definitions[unit.Definition!.Definition].Nationality));
    }

    /// <summary>
    /// Whether a Green or Conscript MMC is Inexperienced (A19.2, A19.3; ruling R15.10): a Conscript always, a Green MMC unless stacked with an unbroken
    /// leader of its side; null for any other unit. Rules decides it (pass 32.c) as the Fire facts' own definition, beside the state's.
    /// </summary>
    public static bool? Inexperienced(GameState state, UnitInstance unit)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(unit);
        var definition = unit.Definition is { } reference ? CatalogReference.Value.Definitions.GetValueOrDefault(reference.Definition) : null;
        return ScenarioA1FireEligibility.InexperiencedForFire(definition?.Class, () => StackedWithUnbrokenLeader(state, unit));
    }

    /// <summary>A Green MMC's Inexperience (A19.3; ruling R15.10); null for any other unit.</summary>
    public static bool? GreenInexperienced(GameState state, UnitInstance unit)
    {
        ArgumentNullException.ThrowIfNull(unit);
        var @class = unit.Definition is { } reference ? CatalogReference.Value.Definitions.GetValueOrDefault(reference.Definition)?.Class : null;
        return ScenarioA1FireEligibility.GreenInexperienced(@class, () => StackedWithUnbrokenLeader(state, unit));
    }

    /// <summary>A19.3: whether an unbroken, active leader of the unit's side shares its Location; false off the map.</summary>
    private static bool StackedWithUnbrokenLeader(GameState state, UnitInstance unit)
    {
        ArgumentNullException.ThrowIfNull(state);
        return state.Location(unit.Id) is { } at && state.At(at.Location).OfType<UnitInstance>().Any(other => other.Id != unit.Id
            && other.Side == unit.Side && other.Kind == "asl:leader" && other.Status == InstanceStatus.Active && !Is(other, Conditions.Broken));
    }

    /// <summary>The FT a unit possesses (A22.4; ruling R15.1); null when none.</summary>
    public static int? Flamethrowers(GameState state, UnitInstance unit)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(unit);
        var count = state.Equipment.Count(item => item.Status == InstanceStatus.Active && item.Holding is { Role: HoldingRole.Possessed } holding
            && holding.Holder == unit.Id && item.Definition is { } reference && CatalogReference.Value.Definitions.GetValueOrDefault(reference.Definition)?.IsFt == true);
        return count == 0 ? null : count;
    }

    /// <summary>
    /// The state's part of a DC's attack (A23; rulings R15.2, R15.3): every unit of the target Location, of either side; <paramref name="mode"/> is
    /// <c>placed</c>, <c>thrown</c>, or <c>thrower</c>. A Placed DC's CX and concealment facts are its placement's, which the state keeps; a Thrown one's
    /// are its thrower's now. The map facts (range, level, terrain) are the planner's.
    /// </summary>
    public static (FireAttack? Attack, string? Reason) DemolitionChargeFromState(GameState state, string chargeId, string mode, BoardLocation target)
    {
        ArgumentNullException.ThrowIfNull(state);
        if (state.Catalog.Catalog != Catalog || state.Catalog.Version != CatalogVersion)
        {
            return (null, $"play.fire-catalog: the Fire package reads {Catalog}@{CatalogVersion}, and this game uses {state.Catalog.Catalog}@{state.Catalog.Version}");
        }

        // Rules decides it (pass 32.c): the charge, its placement, its user, and the units of the target Location are read and handed over.
        var charge = state.Find(chargeId) as EquipmentInstance;
        var chargeOk = charge is { Status: InstanceStatus.Active, Definition: not null };
        var placement = chargeOk && mode == FireDemolitionCharge.Placed ? state.PlacedCharges.FirstOrDefault(item => item.Charge == chargeId && item.Operable) : null;
        var possessor = charge?.Holding is { Role: HoldingRole.Possessed } holding ? holding.Holder : null;
        var userId = placement?.Unit ?? possessor;
        var user = chargeOk && userId is not null ? state.Unit(userId) : null;
        var ok = user is { Definition: not null } && (mode != FireDemolitionCharge.Placed || placement is not null);
        var side = user?.Side;
        var movement = state.Movement;
        var here = state.Phase == "mph" && movement is not null && movement.Location == target;
        UnitInstance[] units = ok ? [.. state.At(target).OfType<UnitInstance>()] : [];
        var targetSide = ok ? state.Sides.FirstOrDefault(item => item.Id != side)?.Id : null;
        return ScenarioA1FireEligibility.DemolitionCharge(new DemolitionChargeFacts(chargeId, chargeOk, charge?.Definition?.Definition, mode, placement is not null,
            placement?.Unit, placement?.Cx, placement?.TargetsConcealed, possessor, user is { Definition: not null }, user?.Definition?.Definition, side,
            user is not null && Is(user, Conditions.Cx), user is not null && Is(user, Conditions.BoundingFire), ok ? GreenInexperienced(state, user!) : null,
            ok ? CapturedBy(charge!.Definition!.Definition, user!) : null, ok ? state.Location(userId!)?.Location.ToString() : null, side == state.PhasingSide, state.Phase,
            target.ToString(), here, movement?.Assault ?? false,
            [.. units.Select(unit => new LocationUnitFacts(unit.Id, unit.Status == InstanceStatus.Active, unit.Side, IsVehicle(unit), unit.Definition is not null,
                Is(unit, Conditions.Captured), here && movement!.Movers.Contains(unit.Id)))],
            targetSide, targetSide is null ? null : state.Side(targetSide)?.Elr, targetSide is not null && state.NoQuarter.Contains(targetSide, StringComparer.Ordinal),
            ok ? state.Side(side!)?.Elr : null, ok && state.NoQuarter.Contains(side, StringComparer.Ordinal), ok ? Allies(state, side!) : null, state.ScenarioMonth,
            id => Target(state, units.First(unit => unit.Id == id), target), id => Vehicle(units.First(unit => unit.Id == id), target)));
    }

    /// <summary>
    /// Whether a phasing vehicle may fire as Bounding First Fire now (D3.3; ruling R6.9): while it moves, once the DEFENDER has passed on
    /// its last MP expenditure, or at the outset of its MPh while no other move is under way and it has not ended its move.
    /// </summary>
    public static bool MayBoundingFire(GameState state, string vehicleId)
    {
        ArgumentNullException.ThrowIfNull(state);
        var vehicle = state.Unit(vehicleId);
        return ScenarioA1FireEligibility.MayBoundingFire(new BoundingFireFacts(state.Phase,
            vehicle is { Status: InstanceStatus.Active } && IsVehicle(vehicle) && vehicle.Side == state.PhasingSide,
            state.Movement is not null, state.Movement?.Vehicle == true, state.Movement?.WindowOpen == true,
            state.Movement?.Movers.Contains(vehicleId) == true, vehicle?.MovementEnded == true));
    }

    /// <summary>Whether a unit is a vehicle (D1).</summary>
    public static bool IsVehicle(UnitInstance unit) => unit.Kind == "asl:vehicle";

    /// <summary>
    /// Whether an AFV's crew is Crew Exposed: an OT AFV is CE unless under a BU, Stun, Shock, or Recall marker (D5.3, D5.34, C7.42); a CT AFV
    /// only when its owner has removed its BU counter (D5.2; ruling R7.11); an unarmored
    /// vehicle has no crew to expose (D5.1).
    /// </summary>
    public static bool CrewExposed(UnitInstance vehicle) =>
        ScenarioA1FireEligibility.CrewExposed(new CrewExposedFacts(GameState.Condition(vehicle, Conditions.ButtonedUp) switch
        {
            ConditionState.True => RuleState.True,
            ConditionState.False => RuleState.False,
            _ => RuleState.Unknown,
        },
            Is(vehicle, Conditions.Stunned), Is(vehicle, Conditions.Recalled), Is(vehicle, Conditions.Shocked), Is(vehicle, Conditions.UnconfirmedKill),
            GamePlanner.IsClosedTopped(vehicle)));

    /// <summary>A vehicle in the target Location, with its crew's state (A7.307, A7.308, D.8B).</summary>
    internal static FireVehicle Vehicle(UnitInstance vehicle, BoardLocation at) =>
        ScenarioA1FireEligibility.VehicleTarget(new VehicleTargetFacts(vehicle.Id, vehicle.Definition?.Definition, at.ToString(), CrewExposed(vehicle),
            Is(vehicle, Conditions.Stunned), Is(vehicle, Conditions.Recalled), Is(vehicle, Conditions.StunRecovery), Is(vehicle, Conditions.Immobilized),
            Is(vehicle, Conditions.Concealed), Is(vehicle, Conditions.Hidden)));

    /// <summary>
    /// A vehicle's MA MG attack (ruling R25.7): its crew's state, Motion (D2.42), a pin (A7.82), its MG's malfunction (D3.7), whether it
    /// fired this Player Turn, and whether its last shot this phase kept its Multiple ROF (C2.24), which the state records.
    /// </summary>
    private static FireVehicleFire VehicleFire(GameState state, UnitInstance vehicle, BoardLocation at) =>
        ScenarioA1FireEligibility.VehicleFirer(new VehicleFirerFacts(vehicle.Id, vehicle.Definition?.Definition, at.ToString(), CrewExposed(vehicle),
            Is(vehicle, Conditions.Motion), state.Movement is { Vehicle: true, Started: true, Stopped: false } moving && moving.Movers.Contains(vehicle.Id),
            Is(vehicle, Conditions.Pinned),
            Is(vehicle, Conditions.Stunned), Is(vehicle, Conditions.Recalled), Is(vehicle, Conditions.Shocked), Is(vehicle, Conditions.UnconfirmedKill),
            Is(vehicle, Conditions.StunRecovery), Is(vehicle, Conditions.Malfunctioned),
            Fired(vehicle), Is(vehicle, Conditions.FirstFire), Is(vehicle, Conditions.BoundingFire),
            state.OrdnanceShots.Any(item => item.Gun == vehicle.Id && item.RateOfFireKept)));

    /// <summary>
    /// The state's part of a Residual FP attack on the moving stack as it enters a Location (A8.2, A8.22): no firers, the
    /// counter's FP, and the stack as the targets.
    /// </summary>
    public static (FireAttack? Attack, string? Reason) ResidualFromState(GameState state, BoardLocation target, int fp)
    {
        ArgumentNullException.ThrowIfNull(state);
        // Rules decides it (pass 32.c): the moving stack and the units of its Location; a target's and a vehicle's facts are read for the units the attack takes.
        var movement = state.Movement;
        var here = movement is not null && movement.Location == target;
        UnitInstance[] units = here ? [.. state.At(target).OfType<UnitInstance>()] : [];
        return ScenarioA1FireEligibility.Residual(new ResidualFacts(here, movement?.Assault ?? false,
            [.. units.Select(unit => new LocationUnitFacts(unit.Id, unit.Status == InstanceStatus.Active, unit.Side, IsVehicle(unit), unit.Definition is not null,
                Is(unit, Conditions.Captured), movement!.Movers.Contains(unit.Id)))],
            state.PhasingSide, here ? state.Side(state.PhasingSide)?.Elr : null, here && state.NoQuarter.Contains(state.PhasingSide, StringComparer.Ordinal),
            state.ScenarioMonth, fp, target.ToString(),
            id => Target(state, units.First(unit => unit.Id == id), target), id => Vehicle(units.First(unit => unit.Id == id), target)));
    }

    /// <summary>
    /// A vehicle's OVR of its Location (D7.1, D7.11; ruling R11.11), read from the state: every non-captured enemy unit there is attacked, Infantry as
    /// targets and vehicles on the Vehicle line or through their Vulnerable crews; the map facts (terrain, SMOKE, a wall crossed) are the planner's.
    /// </summary>
    public static (FireAttack? Attack, string? Reason) OverrunFromState(GameState state, string vehicleId)
    {
        ArgumentNullException.ThrowIfNull(state);
        // D7.11 (referee, pass 11): a vehicle destroyed before its declared OVR resolves still makes it, at half FP. Rules decides it (pass 32.c).
        var vehicle = state.Unit(vehicleId);
        var found = vehicle is { Status: InstanceStatus.Active or InstanceStatus.Wrecked };
        var isVehicle = found && IsVehicle(vehicle!);
        BoardLocation? at = isVehicle ? state.Location(vehicleId)?.Location : null;
        var ok = at is not null && vehicle!.Definition is not null;
        UnitInstance[] units = ok ? [.. state.At(at!).OfType<UnitInstance>()] : [];
        var targetSide = ok ? state.Sides.FirstOrDefault(side => side.Id != vehicle!.Side)?.Id : null;
        return ScenarioA1FireEligibility.Overrun(new OverrunFacts(found, isVehicle, at is not null, vehicle?.Definition is not null, vehicleId, vehicle?.Definition?.Definition,
            at?.ToString() ?? string.Empty, vehicle?.Side, ok && CrewExposed(vehicle!), ok && Is(vehicle!, Conditions.Immobilized), ok && Is(vehicle!, Conditions.Bogged),
            vehicle?.Status == InstanceStatus.Wrecked, ok && Is(vehicle!, Conditions.Malfunctioned), ok && Is(vehicle!, Conditions.Disabled),
            ok && Is(vehicle!, Conditions.BmgMalfunctioned), ok && Is(vehicle!, Conditions.CmgMalfunctioned),
            [.. units.Select(unit => new LocationUnitFacts(unit.Id, unit.Status == InstanceStatus.Active, unit.Side, IsVehicle(unit), unit.Definition is not null,
                Is(unit, Conditions.Captured), false))],
            targetSide is null ? null : state.Side(targetSide)?.Elr, targetSide is not null && state.NoQuarter.Contains(targetSide, StringComparer.Ordinal), state.ScenarioMonth,
            id => Target(state, units.First(unit => unit.Id == id), at!), id => Vehicle(units.First(unit => unit.Id == id), at!)));
    }

    /// <summary>The rolls of a record, rebuilt from its roll ids and the recorded dice, in the calculator's shape.</summary>
    public static FireRolls? Rolls(FireResolved fire, FireAttack attack, IReadOnlyDictionary<string, DiceRolled> rolls)
    {
        ArgumentNullException.ThrowIfNull(fire);
        ArgumentNullException.ThrowIfNull(attack);
        ArgumentNullException.ThrowIfNull(rolls);
        IReadOnlyList<int>? ift = null;
        Dictionary<string, int>? selection = null;
        Dictionary<string, int>? weaponSelection = null;
        Dictionary<string, int>? firerSelection = null;
        Dictionary<string, IReadOnlyList<int>>? checks = null;
        Dictionary<string, IReadOnlyList<int>>? leaderLoss = null;
        Dictionary<string, int>? wounds = null;
        Dictionary<string, IReadOnlyList<int>>? heat = null;
        Dictionary<string, IReadOnlyList<int>>? berserk = null;
        Dictionary<string, IReadOnlyList<int>>? crewChecks = null;
        Dictionary<string, int>? unlikelyKill = null;
        int? molCheck = null;

        // A selection roll names the units it selects among, one die each, in order.
        static bool Select(ref Dictionary<string, int>? into, string ids, DiceRolled roll)
        {
            var names = ids.Split(',');
            if (roll.Count != names.Length)
            {
                return false;
            }

            into ??= new(StringComparer.Ordinal);
            foreach (var (id, index) in names.Select((id, index) => (id, index)))
            {
                into[id] = roll.Values[index];
            }

            return true;
        }
        foreach (var (key, id) in fire.Rolls)
        {
            if (!rolls.TryGetValue(id, out var roll) || roll.Sides != 6)
            {
                return null;
            }

            var split = key.IndexOf(':', StringComparison.Ordinal);
            var (kind, unit) = split < 0 ? (key, string.Empty) : (key[..split], key[(split + 1)..]);
            switch (kind)
            {
                case "attack" when roll.Count == 2:
                    ift = roll.Values;
                    break;
                case "randomSelection" when unit.Length == 0 && roll.Count == attack.Targets!.Count:
                    // A record made before the steps 19 to 23 revision draws one die for every target.
                    selection = attack.Targets.Select((target, index) => (target.UnitId!, roll.Values[index]))
                        .ToDictionary(pair => pair.Item1, pair => pair.Item2, StringComparer.Ordinal);
                    break;
                case "randomSelection" when unit.Length > 0:
                    if (!Select(ref selection, unit, roll))
                    {
                        return null;
                    }

                    break;
                case "weaponSelection":
                    if (!Select(ref weaponSelection, unit, roll))
                    {
                        return null;
                    }

                    break;
                case "firerSelection":
                    if (!Select(ref firerSelection, unit, roll))
                    {
                        return null;
                    }

                    break;
                case "checks" when roll.Count == 2:
                    (checks ??= new(StringComparer.Ordinal))[unit] = roll.Values;
                    break;
                case "leaderLoss" when roll.Count == 2:
                    (leaderLoss ??= new(StringComparer.Ordinal))[unit] = roll.Values;
                    break;
                case "woundSeverity" when roll.Count == 1:
                    (wounds ??= new(StringComparer.Ordinal))[unit] = roll.Values[0];
                    break;
                case "heatOfBattle" when roll.Count == 2:
                    (heat ??= new(StringComparer.Ordinal))[unit] = roll.Values;
                    break;
                case "berserkCheck" when roll.Count == 2:
                    (berserk ??= new(StringComparer.Ordinal))[unit] = roll.Values;
                    break;
                case "crewCheck" when roll.Count == 2:
                    (crewChecks ??= new(StringComparer.Ordinal))[unit] = roll.Values;
                    break;
                case "unlikelyKill" when roll.Count == 1:
                    (unlikelyKill ??= new(StringComparer.Ordinal))[unit] = roll.Values[0];
                    break;
                case "molCheck" when roll.Count == 1:
                    molCheck = roll.Values[0];
                    break;
                default:
                    return null;
            }
        }

        return new FireRolls(ift, selection, checks, leaderLoss, wounds)
        {
            WeaponSelection = weaponSelection,
            FirerSelection = firerSelection,
            HeatOfBattle = heat,
            BerserkChecks = berserk,
            CrewChecks = crewChecks,
            UnlikelyKill = unlikelyKill,
            MolCheck = molCheck,
        };
    }

    private static bool Is(IGameObject item, string condition) => GameState.Condition(item, condition) == ConditionState.True;

    /// <summary>
    /// A unit in the target Location, with the Fanatic (A10.8) and heroic (A15.21) states the Fire package reads, its Inexperience (A19.2; ruling
    /// R15.10), and the FT it possesses (A22.4; ruling R15.1).
    /// </summary>
    internal static FireTarget Target(GameState state, UnitInstance unit, BoardLocation at) =>
        ScenarioA1FireEligibility.Target(new TargetUnitFacts(unit.Id, unit.Definition?.Definition, at.ToString(), Is(unit, Conditions.Broken),
            Is(unit, Conditions.Pinned), Is(unit, Conditions.Concealed), Is(unit, Conditions.Hidden), unit.Kind == UnitKinds.Dummy,
            Is(unit, Conditions.Wounded), Is(unit, Conditions.Disrupted),
            // Ruling R18.3: a unit of an OB group takes its group's ELR.
            unit.Group is not null ? state.ElrOf(unit) : null,
            Is(unit, Conditions.Fanatic), Is(unit, Conditions.Heroic), Is(unit, Conditions.Berserk),
            GreenInexperienced(state, unit), Flamethrowers(state, unit)));

    // A7.1: a unit fires in one fire phase per Player Turn; A7.531: a directing leader is marked too.
    internal static bool Fired(IGameObject item) => ScenarioA1FireEligibility.Fired(Is(item, Conditions.PrepFire), Is(item, Conditions.FinalFire));

    /// <summary>
    /// Why a SW may not be chosen to fire now, in a few words, or null when it may (pass 31d, design D10; A9.2, A9.7): it has malfunctioned, or it
    /// carries a Prep Fire or Final Fire counter, which a MG that kept its rate of fire does not. A First Fire counter does not bar it: it may fire
    /// again as Subsequent First Fire or in Final Fire (A8.3, A8.4), which the Fire package decides.
    /// </summary>
    public static string? WeaponBar(EquipmentInstance weapon)
    {
        ArgumentNullException.ThrowIfNull(weapon);
        return ScenarioA1FireEligibility.WeaponBar(Is(weapon, Conditions.Malfunctioned), Fired(weapon));
    }

    /// <summary>
    /// Whether a unit has spent its fire for this phase, as the Fire package reads it for the PFPh, AFPh, and DFPh (A7.1, A8.4): marked
    /// Prep or Final Fire, unless in the DFPh it is marked First Fire, and unless it still possesses a MG that has not fired (A9.2). The
    /// MPh is not read here: Subsequent First Fire and FPF let marked units fire again (A8.3, A8.31).
    /// </summary>
    public static bool FireSpent(GameState state, UnitInstance unit)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(unit);
        // Rules decides it (pass 32.c); the three state reads are made where the old body made them.
        return ScenarioA1FireEligibility.FireSpent(new FireSpentFacts(state.Phase, Fired(unit), Is(unit, Conditions.FirstFire), IsVehicle(unit),
            // C2.24, D3.5: a vehicle's MG fires again this phase only on the Multiple ROF its last shot kept.
            () => state.OrdnanceShots.Any(item => item.Gun == unit.Id && item.RateOfFireKept),
            // A7.351 (table player, pass 9b): a squad whose only fire is one SW still fires its inherent FP, and an unfired ATR fires like a MG; so does an
            // unfired FT (A22.3; backlog pass 15).
            () => state.SupportWeaponUses.Any(item => item.Unit == unit.Id),
            () => state.Equipment.Any(item => item.Status == InstanceStatus.Active && item.Holding is { Role: HoldingRole.Possessed } holding
                && holding.Holder == unit.Id && (item.Kind is "asl:mg" or "asl:ft" || (item.Definition is { } weapon && LiveOrdnance.LatwType(weapon.Definition) == "atr"))
                && !Fired(item) && !Is(item, Conditions.Malfunctioned))));
    }
}

/// <summary>
/// Replays a fire record through the Fire package (Fire in Live Play): the recorded facts must agree with the state the
/// record is made in, and the package must reproduce the recorded resolution from those facts and the recorded dice.
/// The map facts are the planner's, recorded with the attack.
/// </summary>
public sealed class FireRecordVerifier(ScenarioA1FireReference reference) : IFireRecordVerifier
{
    private static readonly Lazy<FireRecordVerifier> Instance = new(() => new FireRecordVerifier(new ScenarioA1FirePackage().Reference));

    public static FireRecordVerifier Shared => Instance.Value;

    public string? Verify(GameState state, FireResolved fire, IReadOnlyDictionary<string, DiceRolled> rolls)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(fire);
        ArgumentNullException.ThrowIfNull(rolls);
        FireAttack? recorded;
        try
        {
            recorded = fire.Facts.Deserialize<FireAttack>(LiveFire.StrictJson);
        }
        catch (JsonException exception)
        {
            return "The fire record's facts cannot be read: " + exception.Message;
        }

        if (recorded is null || recorded.Rolls is not null || !BoardLocation.TryParse(fire.TargetLocation, out var target))
        {
            return "The fire record's facts are incomplete.";
        }

        FireAttack? expected;
        string? reason;
        if (recorded.FireKind == ScenarioA1FireCalculator.ResidualFire)
        {
            (expected, reason) = LiveFire.ResidualFromState(state, target, recorded.ResidualFp ?? 0);
        }
        else if (recorded.DemolitionCharge is { } charge)
        {
            // A23 (rulings R15.2, R15.3): a DC's attack is read from the DC, its placement, and its user.
            (expected, reason) = LiveFire.DemolitionChargeFromState(state, charge.EquipmentId ?? string.Empty, charge.Mode ?? string.Empty, target);
        }
        else if (recorded.FireKind == ScenarioA1FireCalculator.OverrunFire)
        {
            // D7.1 (ruling R11.11): an OVR is read from its vehicle's state; its crew's CE status is recorded as the OVR saw it.
            (expected, reason) = LiveFire.OverrunFromState(state, recorded.Overrun?.VehicleId ?? string.Empty);
        }
        else
        {
            // The recorded choices (directors, weapons, a Multiple ROF shot) select what the state is read for; the state's
            // facts are then compared whole.
            var directors = (recorded.Director is null ? [] : new[] { recorded.Director.UnitId! })
                .Concat(recorded.OtherDirectors?.Select(item => item.UnitId!) ?? []).ToArray();
            var weapons = recorded.Firers?.Where(item => item.Weapons is { Count: > 0 })
                .ToDictionary(item => item.UnitId!, item => (IReadOnlyList<string>)[.. item.Weapons!.Select(weapon => weapon.EquipmentId!)], StringComparer.Ordinal);
            var alone = recorded.Firers?.Where(item => item.UsesInherentFp == false && state.Unit(item.UnitId!)?.Kind != "asl:leader").Select(item => item.UnitId!).ToArray();
            var partners = recorded.Firers?.Where(item => item.Partner is not null).ToDictionary(item => item.UnitId!, item => item.Partner!, StringComparer.Ordinal);
            var mol = recorded.Firers?.FirstOrDefault(item => item.Mol == true)?.UnitId;
            (expected, reason) = LiveFire.FromState(state, fire.Firers, directors, target, weapons, alone, partners is { Count: > 0 } ? partners : null, mol);
        }

        if (expected is null)
        {
            return reason;
        }

        // The state's facts must be the recorded ones; the map facts are taken as recorded: range, levels, LOS, terrain, and, since
        // unit step 30, each unit's LOS to a Known enemy and its ADJACENT captors. A record made before unit step 30 names no
        // companions, and is compared without them.
        var merged = expected with
        {
            Range = recorded.Range,
            SameLevel = recorded.SameLevel,
            Los = recorded.Los,
            TargetTerrain = recorded.TargetTerrain,
            Firers = recorded.Firers is null || expected.Firers is null ? expected.Firers
                : [.. expected.Firers.Zip(recorded.Firers, (fact, record) => fact with
                {
                    Range = record.Range,
                    SameLevel = record.SameLevel,
                    Los = record.Los,
                    KnownEnemyInLos = record.KnownEnemyInLos,
                    Captors = record.Captors,

                    // Pass 31d (ruling R31d.2): who sees a concealed firer is the planner's read of the map, recorded with the attack.
                    SeenByGoodOrderEnemy = record.SeenByGoodOrderEnemy,
                })],
            // A7.7 (ruling R12.11): the Encirclement an attack completes is read before the attack, and recorded with it.
            Targets = recorded.Targets is null || expected.Targets is null ? expected.Targets
                : [.. expected.Targets.Zip(recorded.Targets, (fact, record) => fact with
                {
                    KnownEnemyInLos = record.KnownEnemyInLos,
                    Captors = record.Captors,
                    Encircled = fact.Encircled ?? record.Encircled,
                })],
            FirerLocationsAdjacent = recorded.FirerLocationsAdjacent,
            WithinSubsequentFirstFireRange = recorded.WithinSubsequentFirstFireRange,
            Companions = recorded.Companions is null ? null : expected.Companions,

            // The owners' answers are declared, and the projector checks them against the choices made (ruling R5.8).
            Choices = recorded.Choices,
            AfvCover = GamePlanner.CoverAt(state, target, expected.Targets?.Select(item => state.Unit(item.UnitId!)?.Side).FirstOrDefault(side => side is not null)),

            // C11 (ruling R8.3): the Gun in the target Location, its Emplacement and gunshield, is a map read recorded with the attack.
            GunTarget = recorded.GunTarget,

            // Backlog pass 10 (rulings R10.4 to R10.8, R10.13): the levels, the wall or hedge TEM with Wall Advantage, Height Advantage, and
            // Hazardous Movement are map reads recorded with the attack; a Snap Shot is the DEFENDER's declaration.
            TargetLevelAbove = recorded.TargetLevelAbove,
            HexsideTem = recorded.HexsideTem,
            HeightAdvantage = recorded.HeightAdvantage,
            HazardousMovement = recorded.HazardousMovement,
            SnapShot = recorded.SnapShot,

            // Backlog pass 16 (rulings R16.2, R16.3, R16.11 to R16.14): the night and weather facts are the planner's reading of the map and the SSRs.
            LowVisibilityDrm = recorded.LowVisibilityDrm,
            BeyondNvr = recorded.BeyondNvr,
            CushionedOpenGround = recorded.CushionedOpenGround,
            BreakdownReduction = recorded.BreakdownReduction,

            // Pass 31 (ruling R31.3): how a pinned firer's MG fires is recorded with the attack, so an older record replays as it was resolved.
            PinnedMgAreaFire = recorded.PinnedMgAreaFire,
            TargetsInMelee = recorded.TargetsInMelee,

            // Backlog pass 12 (rulings R12.6, R12.7): Spraying Fire is the firer's declaration; a Fire Lane's Residual FP is the lane's in the state.
            SprayingFire = recorded.SprayingFire,
            SprayShare = recorded.SprayShare,
            FireLane = recorded.FireLane == true && state.FireLanes.Any(lane => lane.Entries.Any(entry => entry.Location == target && entry.Fp == recorded.ResidualFp))
                ? true : null,
        };
        merged = merged with
        {
            // A9.5 (ruling R12.6): the second Location of Spraying Fire is read with the first, before its markers.
            Firers = merged.Firers is null || recorded.Firers is null ? merged.Firers
                : [.. merged.Firers.Zip(recorded.Firers, (fact, record) => recorded.SprayShare == true
                    ? fact with
                    {
                        TargetLevelAbove = record.TargetLevelAbove,
                        FiredThisPlayerTurn = record.FiredThisPlayerTurn,
                        FirstFireMarked = record.FirstFireMarked,
                        FinalFireMarked = record.FinalFireMarked,
                        Weapons = record.Weapons,
                    }
                    : fact with { TargetLevelAbove = record.TargetLevelAbove })],
            Director = recorded.SprayShare == true ? recorded.Director : merged.Director is { } director && recorded.Director is { } directed
                ? director with
                {
                    SeenByGoodOrderEnemy = directed.SeenByGoodOrderEnemy
                }
                : merged.Director,
            OtherDirectors = recorded.SprayShare == true || merged.OtherDirectors is null || recorded.OtherDirectors is null ? (recorded.SprayShare == true ? recorded.OtherDirectors : merged.OtherDirectors)
                : [.. merged.OtherDirectors.Zip(recorded.OtherDirectors, (fact, record) => fact with
                {
                    SeenByGoodOrderEnemy = record.SeenByGoodOrderEnemy
                })],
        };
        if (JsonSerializer.Serialize(merged, LiveFire.Json) != JsonSerializer.Serialize(recorded, LiveFire.Json))
        {
            return "The fire record's facts do not match the game state.";
        }

        if (LiveFire.Rolls(fire, recorded, rolls) is not { } dice)
        {
            return "The fire record names a roll of the wrong shape.";
        }

        var resolution = ScenarioA1FireCalculator.Resolve(recorded with
        {
            Rolls = dice
        }, reference);
        return resolution.Disposition != FireResolution.Resolved
            ? "The fire record's facts and rolls do not resolve: " + string.Join("; ", resolution.Reasons)
            : !JsonElement.DeepEquals(JsonSerializer.SerializeToElement(resolution, LiveFire.Json), fire.Resolution)
                ? "The fire record's resolution differs from what its facts and rolls give."
                : null;
    }
}
