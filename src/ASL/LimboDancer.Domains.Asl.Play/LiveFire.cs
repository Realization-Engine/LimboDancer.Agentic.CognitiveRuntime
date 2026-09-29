using System.Text.Json;
using System.Text.Json.Serialization;
using LimboDancer.Domains.Asl.Maps.Coordinates;
using LimboDancer.Domains.Asl.ScenarioA1;
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

    /// <summary>A21.1 (ruling R13.7): a weapon of another nationality than its possessor's is captured.</summary>
    public static bool? CapturedBy(string? weaponDefinition, UnitInstance unit) =>
        weaponDefinition is not null && unit.Definition is { } holder
            && CatalogReference.Value.Definitions.GetValueOrDefault(weaponDefinition) is { } weapon
            && CatalogReference.Value.Definitions.GetValueOrDefault(holder.Definition) is { } firer
            && weapon.Nationality != firer.Nationality ? true : null;

    public const string Catalog = "asl-scenario-a1";
    public const string CatalogVersion = "1.10.0";

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

        // D1.83, D3.4 (ruling R25.7): a vehicle's MG fires alone, with no leader and no Infantry in its group.
        var vehicleFirers = firerIds.Select(state.Unit).OfType<UnitInstance>().Where(IsVehicle).ToArray();
        if (vehicleFirers.Length > 0 && (firerIds.Count > 1 || directorIds.Count > 0 || weapons is { Count: > 0 }))
        {
            return (null, "play.fire-vehicle-group: a vehicle's MG fires alone, with no leader or Infantry in its fire group (D3.4)");
        }

        var firers = new List<(UnitInstance Unit, BoardLocation At)>();
        foreach (var id in firerIds)
        {
            if (state.Unit(id) is not { Status: InstanceStatus.Active, Definition: not null } unit || state.Location(unit.Id)?.Location is not { } at)
            {
                return (null, $"play.fire-firers: '{id}' is not an active unit from the catalog on the map");
            }

            // A4.8, C10.3 (table player, pass 8): a TI unit does not fire.
            if (Is(unit, "asl:ti"))
            {
                return (null, $"play.fire-ti: '{id}' is TI and does not fire this Player Turn (A4.8, C10.3)");
            }

            firers.Add((unit, at));
        }

        var locations = firers.Select(item => item.At).Distinct().ToArray();
        var directors = new List<(UnitInstance Unit, BoardLocation At)>();
        foreach (var id in directorIds)
        {
            if (state.Unit(id) is not { Status: InstanceStatus.Active, Definition: not null } leader || state.Location(leader.Id)?.Location is not { } at
                || !locations.Contains(at))
            {
                return (null, $"play.fire-director: '{id}' is not an active unit in a Location of the fire group");
            }

            directors.Add((leader, at));
        }

        var side = firers[0].Unit.Side;
        var kind = (string?)null;
        if (state.Phase == "mph" && vehicleFirers.Length > 0)
        {
            // D3.3, A8.1 (ruling R6.9): the DEFENDER's vehicle fires as Defensive First Fire; the moving vehicle, once the DEFENDER has
            // passed on its last MP expenditure, as Bounding First Fire.
            if (vehicleFirers[0].Side != state.PhasingSide)
            {
                kind = ScenarioA1FireCalculator.FirstFire;
            }
            else if (MayBoundingFire(state, vehicleFirers[0].Id))
            {
                kind = ScenarioA1FireCalculator.BoundingFirstFire;
            }
            else
            {
                return (null, "play.fire-vehicle-phase: in its own MPh a vehicle fires only while it moves, as Bounding First Fire, after the DEFENDER has passed on its last MP expenditure (D3.3)");
            }
        }
        else if (state.Phase == "mph")
        {
            // A8.1, A8.3, A8.31: the firers' markers decide the kind of Defensive fire.
            // A8.31 (ruling R12.3): FPF firers may group with other Defensive fire; First Fire and Subsequent First Fire firers may not.
            var marks = firers.Select(item => Is(item.Unit, Conditions.FinalFire) ? 2 : Is(item.Unit, Conditions.FirstFire) ? 1 : 0).Distinct().ToArray();
            if (marks.Length != 1 && !(marks.Contains(2) && !marks.Contains(0)) && firers.All(item => withoutInherent?.Contains(item.Unit.Id) != true))
            {
                return (null, "play.fire-kind: a group mixes firers marked for different kinds of Defensive fire (A8.3, A8.31)");
            }

            kind = marks.Max() switch
            {
                0 => ScenarioA1FireCalculator.FirstFire,
                1 => withoutInherent is { Count: > 0 } ? ScenarioA1FireCalculator.FirstFire : ScenarioA1FireCalculator.SubsequentFirstFire,
                _ => ScenarioA1FireCalculator.FinalProtectiveFire,
            };
        }

        var movers = state.Phase == "mph" && state.Movement is { } movement && movement.Location == target ? movement.Movers : null;
        UnitInstance[] attacked = [.. state.At(target).OfType<UnitInstance>()
            .Where(unit => unit.Status == InstanceStatus.Active && (movers is null || movers.Contains(unit.Id)))
            .OrderBy(unit => unit.Id, StringComparer.Ordinal)];

        // A7.308: the target Location's vehicles share the attack's DR; Defensive First Fire attacks only the moving stack or vehicle.
        UnitInstance[] targets = [.. attacked.Where(unit => !IsVehicle(unit))];
        UnitInstance[] vehicles = [.. attacked.Where(IsVehicle)];

        // A15.41: the target side's units the attack does not attack (in the MPh, those not moving) are a berserk leader's companions.
        UnitInstance[] companions = movers is null || targets.Length == 0 ? [] : [.. state.At(target).OfType<UnitInstance>()
            .Where(unit => unit.Status == InstanceStatus.Active && !movers.Contains(unit.Id) && unit.Side == targets[0].Side && unit.Definition is not null
                && !IsVehicle(unit))
            .OrderBy(unit => unit.Id, StringComparer.Ordinal)];
        if (targets.Any(unit => unit.Definition is null && unit.Kind != UnitKinds.Dummy))
        {
            return (null, "play.fire-target: the target Location holds a unit outside the catalog");
        }

        var phase = state.Phase switch
        {
            "pfph" => "PFPh",
            "dfph" => "DFPh",
            "afph" => "AFPh",
            "mph" => "MPh",
            _ => state.Phase,
        };
        var targetSide = targets.FirstOrDefault(unit => unit.Side != side)?.Side ?? vehicles.FirstOrDefault()?.Side ?? state.Sides.FirstOrDefault(item => item.Id != side)?.Id;
        var friendlyTargets = targets.Any(unit => unit.Side == side);
        var multi = locations.Length > 1;

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
                // A7.35: a SW fires only when possessed by its unit.
                if (named.Any(id => state.Find(id) is not EquipmentInstance { Status: InstanceStatus.Active, Holding: { Role: HoldingRole.Possessed } holding }
                    || holding.Holder != unit.Id))
                {
                    return (null, $"play.fire-weapon: every weapon '{unit.Id}' fires must be one it possesses (A7.35)");
                }

                // A9.8 (ruling R13.6): a dismantled weapon is not fired.
                if (named.FirstOrDefault(id => state.Find(id) is EquipmentInstance held && Is(held, Conditions.Dismantled)) is { } dismantled)
                {
                    return (null, $"play.fire-weapon: {dismantled} is dismantled and is not fired until it is assembled (A9.8)");
                }

                used = [.. named.Select(id => Weapon(id) is var weapon && CapturedBy(weapon.DefinitionId, unit) is { } captured ? weapon with { Captured = captured } : weapon)];

                // A22.3 (table player, pass 15): a unit uses one FT or DC in a Player Turn.
                if (named.Any(id => state.Find(id) is EquipmentInstance { Kind: "asl:ft" }) && state.AssaultWeaponUsers.Contains(unit.Id, StringComparer.Ordinal))
                {
                    return (null, $"play.fire-ft-once: {unit.Id} has used a FT or DC this Player Turn and uses no other (A22.3)");
                }

                // A7.351 (ruling R12.4): a squad that fired one MG apart from its inherent FP fires no second one with that FP.
                if (withoutInherent?.Contains(unit.Id) != true && state.Equipment.Any(item => item.Status == InstanceStatus.Active
                    && item.Holding is { Role: HoldingRole.Possessed } other && other.Holder == unit.Id && !named.Contains(item.Id, StringComparer.Ordinal)
                    && (Fired(item) || Is(item, Conditions.FirstFire))))
                {
                    return (null, $"play.fire-sw-limit: {unit.Id} fired one MG this phase; with its inherent FP it fires no second SW (A7.351)");
                }

                // A7.351 (table player, pass 9): a squad that used one SW (a mortar, a PF Check, or spotting) fires its inherent FP with no second SW.
                if (state.SupportWeaponUses.Any(item => item.Unit == unit.Id))
                {
                    return (null, $"play.fire-sw-limit: {unit.Id} has used a SW this phase; with its inherent FP it fires no other (A7.351)");
                }
            }

            // A3.3, A7.1 (table player, pass 15): a unit whose SW fired alone in the PFPh has Prep Fired, and does not fire in the AFPh.
            if (state.Phase == "afph" && !Is(unit, Conditions.BoundingFire) && state.Equipment.Any(item => item.Status == InstanceStatus.Active
                && item.Holding is { Role: HoldingRole.Possessed } prepped && prepped.Holder == unit.Id && Is(item, Conditions.PrepFire)))
            {
                return (null, $"play.fire-prep-fired: {unit.Id} fired a SW in the PFPh, so it has Prep Fired and does not fire in the AFPh (A3.3, A7.1)");
            }

            // A7.351 (referee, pass 12): a squad that fired two SW this phase has no inherent FP left, and one that fired its inherent FP and one SW fires
            // no other.
            var otherFired = state.Equipment.Count(item => item.Status == InstanceStatus.Active && item.Holding is { Role: HoldingRole.Possessed } holder
                && holder.Holder == unit.Id && !(used ?? []).Any(weapon => weapon.EquipmentId == item.Id) && (Fired(item) || Is(item, Conditions.FirstFire)));
            if (unit.Kind == "asl:squad" && ((withoutInherent?.Contains(unit.Id) != true && otherFired >= 2)
                || (withoutInherent?.Contains(unit.Id) == true && Fired(unit) && otherFired >= 1 && used is { Count: > 0 })))
            {
                return (null, $"play.fire-sw-limit: {unit.Id} fires no more than two SW in a phase, and no inherent FP with two (A7.351)");
            }

            // A7.351 (rulings R9.2, R9.7): a squad whose only fire this phase is one SW use still fires its inherent FP.
            var swOnly = state.SupportWeaponUses.Any(item => item.Unit == unit.Id);

            // A9.12 (ruling R12.4): a leader fires a MG with no inherent FP of his own, alone or with a stacked SMC as his partner.
            var leader = unit.Kind == "asl:leader";
            string? partner = null;
            if (leader && partners?.TryGetValue(unit.Id, out var named2) == true)
            {
                if (state.Unit(named2) is not { Status: InstanceStatus.Active } helper || helper.Kind is not ("asl:leader" or "asl:hero") || helper.Side != unit.Side
                    || state.Location(helper.Id)?.Location != at || firerIds.Contains(helper.Id) || directorIds.Contains(helper.Id) || Fired(helper)
                    || Is(helper, Conditions.Broken))
                {
                    return (null, $"play.fire-partner: '{named2}' is not a Good Order SMC stacked with {unit.Id} that has not fired (A9.12)");
                }

                partner = helper.Id;
            }

            firerFacts.Add(new FireFirer(unit.Id, unit.Definition!.Definition, at.ToString(), Is(unit, Conditions.Broken), Is(unit, Conditions.Pinned),
                Is(unit, Conditions.Concealed), Fired(unit) && !swOnly, used is not null)
            {
                FirstFireMarked = Is(unit, Conditions.FirstFire) && !swOnly ? true : null,
                FinalFireMarked = state.Phase == "mph" && Is(unit, Conditions.FinalFire) ? true : null,
                // A7.351, A7.352: a crew, HS, or SMC that fired a Gun loses its inherent FP; a squad does not.
                GunFired = state.GunCrewsFired.Contains(unit.Id, StringComparer.Ordinal) && unit.Kind != "asl:squad" ? true : null,
                Weapons = used,
                UsesInherentFp = withoutInherent?.Contains(unit.Id) == true || leader ? false : null,
                Fanatic = Is(unit, Conditions.Fanatic) ? true : null,
                Wounded = Is(unit, Conditions.Wounded) ? true : null,
                Cx = Is(unit, Conditions.Cx) ? true : null,
                // A7.25 (ruling R12.1): an Opportunity Firer fires in the AFPh under its Bounding Fire counter.
                OpportunityFire = state.Phase == "afph" && Is(unit, Conditions.BoundingFire) ? true : null,
                Encircled = state.Encircled(unit) ? true : null,
                Partner = partner,
                // A22.611 (ruling R15.4): the unit that makes the attack's MOL Check.
                Mol = molUser == unit.Id ? true : null,
                // A19.2 (ruling R15.10): a Green firer's Inexperience, for the Heat of Battle of an FPF NMC and a FT's removal number (A19.32).
                Inexperienced = GreenInexperienced(state, unit),
            });
        }

        FireDirector Director((UnitInstance Unit, BoardLocation At) item) =>
            new(item.Unit.Id, item.Unit.Definition!.Definition, item.At.ToString(), Is(item.Unit, Conditions.Broken), Is(item.Unit, Conditions.Pinned),
                Is(item.Unit, Conditions.Concealed), Fired(item.Unit) || Is(item.Unit, Conditions.FirstFire)
                    || state.SupportWeaponDirectors.Any(directed => directed.Leader == item.Unit.Id), Is(item.Unit, Conditions.Wounded))
            {
                Cx = Is(item.Unit, Conditions.Cx) ? true : null,
            };

        return (new FireAttack(
            phase,
            side == state.PhasingSide ? "phasing" : "non-phasing",
            true,
            firers[0].At.ToString(),
            target.ToString(),
            firerFacts,
            directors.Count == 0 ? null : Director(directors[0]),
            null,
            null,
            null,
            state.ScenarioMonth,
            null,
            [.. targets.Select(unit => Target(state, unit, target) with
            {
                // Rulings R12.8, R12.9, R12.11: the firing side's units in a Melee or as prisoners, a prisoner's Guard, and Encirclement.
                Friendly = unit.Side == side ? true : null,
                GuardId = Is(unit, Conditions.Captured) ? unit.Custodian : null,
                Encircled = state.Encircled(unit) ? true : null,
            })],
            targetSide is null ? null : state.Side(targetSide)?.Elr,
            null)
        {
            FireKind = kind,
            TargetMovement = kind is null or ScenarioA1FireCalculator.BoundingFirstFire ? null : new FireMovement(state.Movement?.Assault ?? false),
            FiringSideElr = kind == ScenarioA1FireCalculator.FinalProtectiveFire || friendlyTargets ? state.Side(side)?.Elr : null,
            OtherDirectors = directors.Count > 1 ? [.. directors.Skip(1).Select(Director)] : null,
            Vehicles = vehicles.Length > 0 ? [.. vehicles.Select(unit => Vehicle(unit, target))] : null,
            VehicleFire = vehicleFirers.Length > 0 ? VehicleFire(state, vehicleFirers[0], firers[0].At) : null,
            TargetSideNoQuarter = targetSide is not null && state.NoQuarter.Contains(targetSide, StringComparer.Ordinal) ? true : null,
            FiringSideNoQuarter = (kind == ScenarioA1FireCalculator.FinalProtectiveFire || friendlyTargets) && state.NoQuarter.Contains(side, StringComparer.Ordinal) ? true : null,
            FiringNationalities = Allies(state, side),
            Companions = companions.Length > 0 ? [.. companions.Select(unit => Target(state, unit, target))] : null,
        }, null);
    }

    /// <summary>
    /// A side's nationalities when it holds Allied Troops (A10.7; backlog pass 15, ruling R15.8): every nationality of its units, in order; null when it
    /// holds one alone.
    /// </summary>
    public static IReadOnlyList<string>? Allies(GameState state, string side)
    {
        ArgumentNullException.ThrowIfNull(state);
        string[] nationalities = [.. state.Units.Where(unit => unit.Side == side && unit.Definition is { } reference
                && CatalogReference.Value.Definitions.ContainsKey(reference.Definition))
            .Select(unit => CatalogReference.Value.Definitions[unit.Definition!.Definition].Nationality)
            .Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal)];
        return nationalities.Length > 1 ? nationalities : null;
    }

    /// <summary>
    /// Whether a Green or Conscript MMC is Inexperienced (A19.2, A19.3; ruling R15.10): a Conscript always, a Green MMC unless stacked with an unbroken
    /// leader of its side; null for any other unit.
    /// </summary>
    public static bool? Inexperienced(GameState state, UnitInstance unit)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(unit);
        var definition = unit.Definition is { } reference ? CatalogReference.Value.Definitions.GetValueOrDefault(reference.Definition) : null;
        return definition?.Class switch
        {
            "conscript" => true,
            "green" => state.Location(unit.Id) is not { } at || !state.At(at.Location).OfType<UnitInstance>().Any(other => other.Id != unit.Id
                && other.Side == unit.Side && other.Kind == "asl:leader" && other.Status == InstanceStatus.Active && !Is(other, Conditions.Broken)),
            _ => null,
        };
    }

    /// <summary>A Green MMC's Inexperience (A19.3; ruling R15.10); null for any other unit.</summary>
    public static bool? GreenInexperienced(GameState state, UnitInstance unit)
    {
        ArgumentNullException.ThrowIfNull(unit);
        return unit.Definition is { } reference && CatalogReference.Value.Definitions.GetValueOrDefault(reference.Definition)?.Class == "green"
            ? Inexperienced(state, unit) : null;
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

        if (state.Find(chargeId) is not EquipmentInstance { Status: InstanceStatus.Active, Definition: { } dcReference } charge)
        {
            return (null, $"play.dc: '{chargeId}' is not a DC in play");
        }

        var placement = mode == FireDemolitionCharge.Placed ? state.PlacedCharges.FirstOrDefault(item => item.Charge == chargeId && item.Operable) : null;
        var userId = placement?.Unit ?? (charge.Holding is { Role: HoldingRole.Possessed } holding ? holding.Holder : null);
        if (userId is null || state.Unit(userId) is not { Definition: not null } user || (mode == FireDemolitionCharge.Placed && placement is null))
        {
            return (null, $"play.dc: '{chargeId}' has no unit to Place or Throw it");
        }

        var side = user.Side;
        var phase = state.Phase switch
        {
            "pfph" => "PFPh",
            "dfph" => "DFPh",
            "afph" => "AFPh",
            "mph" => "MPh",
            _ => state.Phase,
        };
        var movers = state.Phase == "mph" && state.Movement is { } movement && movement.Location == target ? movement.Movers : null;
        UnitInstance[] attacked = [.. state.At(target).OfType<UnitInstance>()
            .Where(unit => unit.Status == InstanceStatus.Active && (movers is null || movers.Contains(unit.Id) || unit.Side == side))
            .OrderBy(unit => unit.Id, StringComparer.Ordinal)];
        UnitInstance[] targets = [.. attacked.Where(unit => !IsVehicle(unit))];
        UnitInstance[] vehicles = [.. attacked.Where(IsVehicle)];
        var targetSide = state.Sides.FirstOrDefault(item => item.Id != side)?.Id;
        var friendlyTargets = targets.Any(unit => unit.Side == side);
        var kind = state.Phase == "mph" && mode != FireDemolitionCharge.Thrower ? ScenarioA1FireCalculator.FirstFire : null;
        return (new FireAttack(phase, side == state.PhasingSide ? "phasing" : "non-phasing", null, state.Location(userId)?.Location.ToString() ?? target.ToString(),
            target.ToString(), null, null, null, null, null, state.ScenarioMonth, null,
            [.. targets.Select(unit => Target(state, unit, target) with
            {
                Friendly = unit.Side == side ? true : null,
                GuardId = Is(unit, Conditions.Captured) ? unit.Custodian : null,
                Encircled = state.Encircled(unit) ? true : null,
            })],
            targetSide is null ? null : state.Side(targetSide)?.Elr, null)
        {
            FireKind = kind,
            TargetMovement = kind is null ? null : new FireMovement(state.Movement?.Assault ?? false),
            FiringSideElr = friendlyTargets ? state.Side(side)?.Elr : null,
            Vehicles = vehicles.Length > 0 ? [.. vehicles.Select(unit => Vehicle(unit, target))] : null,
            TargetSideNoQuarter = targetSide is not null && state.NoQuarter.Contains(targetSide, StringComparer.Ordinal) ? true : null,
            FiringSideNoQuarter = friendlyTargets && state.NoQuarter.Contains(side, StringComparer.Ordinal) ? true : null,
            FiringNationalities = Allies(state, side),
            DemolitionCharge = new FireDemolitionCharge(chargeId, dcReference.Definition, mode, userId, user.Definition!.Definition,
                placement?.Cx ?? Is(user, Conditions.Cx), CapturedBy(dcReference.Definition, user) == true,
                mode == FireDemolitionCharge.Placed ? placement!.TargetsConcealed : null,
                mode != FireDemolitionCharge.Placed && state.Phase == "afph" && Is(user, Conditions.BoundingFire) ? true : null)
            {
                Inexperienced = GreenInexperienced(state, user),
            },
        }, null);
    }

    /// <summary>
    /// Whether a phasing vehicle may fire as Bounding First Fire now (D3.3; ruling R6.9): while it moves, once the DEFENDER has passed on
    /// its last MP expenditure, or at the outset of its MPh while no other move is under way and it has not ended its move.
    /// </summary>
    public static bool MayBoundingFire(GameState state, string vehicleId)
    {
        ArgumentNullException.ThrowIfNull(state);
        return state.Phase == "mph" && state.Unit(vehicleId) is { Status: InstanceStatus.Active } vehicle && IsVehicle(vehicle) && vehicle.Side == state.PhasingSide
            && (state.Movement is { Vehicle: true, WindowOpen: false } moving ? moving.Movers.Contains(vehicleId) : state.Movement is null && !vehicle.MovementEnded);
    }

    /// <summary>Whether a unit is a vehicle (D1).</summary>
    public static bool IsVehicle(UnitInstance unit) => unit.Kind == "asl:vehicle";

    /// <summary>
    /// Whether an AFV's crew is Crew Exposed: an OT AFV is CE unless under a BU, Stun, Shock, or Recall marker (D5.3, D5.34, C7.42); a CT AFV
    /// only when its owner has removed its BU counter (D5.2; ruling R7.11); an unarmored
    /// vehicle has no crew to expose (D5.1).
    /// </summary>
    public static bool CrewExposed(UnitInstance vehicle) =>
        !Is(vehicle, Conditions.ButtonedUp) && !Is(vehicle, Conditions.Stunned) && !Is(vehicle, Conditions.Recalled) && !Is(vehicle, Conditions.Shocked)
        && !Is(vehicle, Conditions.UnconfirmedKill)
        && (!GamePlanner.IsClosedTopped(vehicle) || GameState.Condition(vehicle, Conditions.ButtonedUp) == ConditionState.False);

    /// <summary>A vehicle in the target Location, with its crew's state (A7.307, A7.308, D.8B).</summary>
    internal static FireVehicle Vehicle(UnitInstance vehicle, BoardLocation at) =>
        new(vehicle.Id, vehicle.Definition?.Definition, at.ToString(), CrewExposed(vehicle), Is(vehicle, Conditions.Stunned) || Is(vehicle, Conditions.Recalled),
            Is(vehicle, Conditions.StunRecovery), Is(vehicle, Conditions.Immobilized))
        {
            Concealed = Is(vehicle, Conditions.Concealed) || Is(vehicle, Conditions.Hidden) ? true : null,
        };

    /// <summary>
    /// A vehicle's MA MG attack (ruling R25.7): its crew's state, Motion (D2.42), a pin (A7.82), its MG's malfunction (D3.7), whether it
    /// fired this Player Turn, and whether its last shot this phase kept its Multiple ROF (C2.24), which the state records.
    /// </summary>
    private static FireVehicleFire VehicleFire(GameState state, UnitInstance vehicle, BoardLocation at) =>
        new(vehicle.Id, vehicle.Definition?.Definition, at.ToString(), CrewExposed(vehicle),
            Is(vehicle, Conditions.Motion) || (state.Movement is { Vehicle: true, Started: true, Stopped: false } moving && moving.Movers.Contains(vehicle.Id)),
            Is(vehicle, Conditions.Pinned),
            Is(vehicle, Conditions.Stunned) || Is(vehicle, Conditions.Recalled) || Is(vehicle, Conditions.Shocked) || Is(vehicle, Conditions.UnconfirmedKill),
            Is(vehicle, Conditions.StunRecovery), Is(vehicle, Conditions.Malfunctioned),
            Fired(vehicle) || Is(vehicle, Conditions.FirstFire) || Is(vehicle, Conditions.BoundingFire),
            state.OrdnanceShots.Any(item => item.Gun == vehicle.Id && item.RateOfFireKept));

    /// <summary>
    /// The state's part of a Residual FP attack on the moving stack as it enters a Location (A8.2, A8.22): no firers, the
    /// counter's FP, and the stack as the targets.
    /// </summary>
    public static (FireAttack? Attack, string? Reason) ResidualFromState(GameState state, BoardLocation target, int fp)
    {
        ArgumentNullException.ThrowIfNull(state);
        if (state.Movement is not { } movement || movement.Location != target)
        {
            return (null, "play.fire-residual: Residual FP attacks the moving stack in its Location (A8.2)");
        }

        UnitInstance[] targets = [.. state.At(target).OfType<UnitInstance>()
            .Where(unit => unit.Status == InstanceStatus.Active && movement.Movers.Contains(unit.Id) && !IsVehicle(unit)).OrderBy(unit => unit.Id, StringComparer.Ordinal)];

        // A8.2, A8.222 (ruling R6.6): a moving vehicle is attacked on the Vehicle line, or its Vulnerable crew Collaterally.
        UnitInstance[] vehicles = [.. state.At(target).OfType<UnitInstance>()
            .Where(unit => unit.Status == InstanceStatus.Active && movement.Movers.Contains(unit.Id) && IsVehicle(unit))];
        var targetSide = state.PhasingSide;
        UnitInstance[] companions = [.. state.At(target).OfType<UnitInstance>()
            .Where(unit => unit.Status == InstanceStatus.Active && !movement.Movers.Contains(unit.Id) && unit.Side == targetSide && unit.Definition is not null
                && !IsVehicle(unit))
            .OrderBy(unit => unit.Id, StringComparer.Ordinal)];
        return (new FireAttack("MPh", "non-phasing", null, null, target.ToString(), null, null, null, null, null, state.ScenarioMonth, null,
            [.. targets.Select(unit => Target(state, unit, target))],
            state.Side(targetSide)?.Elr, null)
        {
            FireKind = ScenarioA1FireCalculator.ResidualFire,
            TargetMovement = new FireMovement(movement.Assault),
            ResidualFp = fp,
            Vehicles = vehicles.Length > 0 ? [.. vehicles.Select(unit => Vehicle(unit, target))] : null,
            Companions = companions.Length > 0 ? [.. companions.Select(unit => Target(state, unit, target))] : null,
            TargetSideNoQuarter = state.NoQuarter.Contains(targetSide, StringComparer.Ordinal) ? true : null,
        }, null);
    }

    /// <summary>
    /// A vehicle's OVR of its Location (D7.1, D7.11; ruling R11.11), read from the state: every non-captured enemy unit there is attacked, Infantry as
    /// targets and vehicles on the Vehicle line or through their Vulnerable crews; the map facts (terrain, SMOKE, a wall crossed) are the planner's.
    /// </summary>
    public static (FireAttack? Attack, string? Reason) OverrunFromState(GameState state, string vehicleId)
    {
        ArgumentNullException.ThrowIfNull(state);
        // D7.11 (referee, pass 11): a vehicle destroyed before its declared OVR resolves still makes it, at half FP.
        if (state.Unit(vehicleId) is not { Status: InstanceStatus.Active or InstanceStatus.Wrecked } vehicle || !IsVehicle(vehicle) || state.Location(vehicleId)?.Location is not { } at
            || vehicle.Definition is not { } definition)
        {
            return (null, "play.overrun: an OVR is made by a vehicle on the map (D7.1)");
        }

        UnitInstance[] enemies = [.. state.At(at).OfType<UnitInstance>()
            .Where(unit => unit.Status == InstanceStatus.Active && unit.Side != vehicle.Side && !Is(unit, Conditions.Captured)).OrderBy(unit => unit.Id, StringComparer.Ordinal)];
        var targetSide = state.Sides.FirstOrDefault(side => side.Id != vehicle.Side)?.Id;
        var immobile = Is(vehicle, Conditions.Immobilized) || Is(vehicle, Conditions.Bogged) || vehicle.Status == InstanceStatus.Wrecked;
        var vehicles = enemies.Where(IsVehicle).ToArray();
        return (new FireAttack("MPh", "phasing", null, at.ToString(), at.ToString(), null, null, 0, true, null, state.ScenarioMonth, null,
            [.. enemies.Where(unit => !IsVehicle(unit)).Select(unit => Target(state, unit, at))],
            targetSide is null ? null : state.Side(targetSide)?.Elr, null)
        {
            FireKind = ScenarioA1FireCalculator.OverrunFire,
            Overrun = new FireOverrun(vehicle.Id, definition.Definition, at.ToString(), CrewExposed(vehicle), immobile,
                Is(vehicle, Conditions.Malfunctioned) || Is(vehicle, Conditions.Disabled), Is(vehicle, Conditions.BmgMalfunctioned), Is(vehicle, Conditions.CmgMalfunctioned)),
            Vehicles = vehicles.Length > 0 ? [.. vehicles.Select(unit => Vehicle(unit, at))] : null,
            TargetSideNoQuarter = targetSide is not null && state.NoQuarter.Contains(targetSide, StringComparer.Ordinal) ? true : null,
        }, null);
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
        new(unit.Id, unit.Definition?.Definition, at.ToString(), Is(unit, Conditions.Broken),
            Is(unit, Conditions.Pinned), Is(unit, Conditions.Concealed), Is(unit, Conditions.Hidden), unit.Kind == UnitKinds.Dummy,
            Is(unit, Conditions.Wounded), Is(unit, Conditions.Disrupted))
        {
            Fanatic = Is(unit, Conditions.Fanatic) ? true : null,
            Heroic = Is(unit, Conditions.Heroic) ? true : null,
            Berserk = Is(unit, Conditions.Berserk) ? true : null,
            Inexperienced = GreenInexperienced(state, unit),
            Flamethrowers = Flamethrowers(state, unit),
        };

    // A7.1: a unit fires in one fire phase per Player Turn; A7.531: a directing leader is marked too.
    internal static bool Fired(IGameObject item) => Is(item, Conditions.PrepFire) || Is(item, Conditions.FinalFire);

    /// <summary>
    /// Whether a unit has spent its fire for this phase, as the Fire package reads it for the PFPh, AFPh, and DFPh (A7.1, A8.4): marked
    /// Prep or Final Fire, unless in the DFPh it is marked First Fire, and unless it still possesses a MG that has not fired (A9.2). The
    /// MPh is not read here: Subsequent First Fire and FPF let marked units fire again (A8.3, A8.31).
    /// </summary>
    public static bool FireSpent(GameState state, UnitInstance unit)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(unit);
        if (state.Phase is not ("pfph" or "afph" or "dfph") || !Fired(unit) || (state.Phase == "dfph" && Is(unit, Conditions.FirstFire)))
        {
            return false;
        }

        // C2.24, D3.5: a vehicle's MG fires again this phase only on the Multiple ROF its last shot kept.
        if (IsVehicle(unit))
        {
            return !state.OrdnanceShots.Any(item => item.Gun == unit.Id && item.RateOfFireKept);
        }

        // A7.351 (table player, pass 9b): a squad whose only fire is one SW still fires its inherent FP, and an unfired ATR fires like a MG; so does an
        // unfired FT (A22.3; backlog pass 15).
        return !state.SupportWeaponUses.Any(item => item.Unit == unit.Id)
            && !state.Equipment.Any(item => item.Status == InstanceStatus.Active && item.Holding is { Role: HoldingRole.Possessed } holding
                && holding.Holder == unit.Id && (item.Kind is "asl:mg" or "asl:ft" || (item.Definition is { } weapon && LiveOrdnance.LatwType(weapon.Definition) == "atr"))
                && !Fired(item) && !Is(item, Conditions.Malfunctioned));
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
            Director = recorded.SprayShare == true ? recorded.Director : merged.Director,
            OtherDirectors = recorded.SprayShare == true ? recorded.OtherDirectors : merged.OtherDirectors,
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
