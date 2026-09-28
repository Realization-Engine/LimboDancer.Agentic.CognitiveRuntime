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
    public const string Catalog = "asl-scenario-a1";
    public const string CatalogVersion = "1.9.0";

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
        BoardLocation target, IReadOnlyDictionary<string, IReadOnlyList<string>>? weapons = null, IReadOnlyCollection<string>? withoutInherent = null)
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
            var marks = firers.Select(item => Is(item.Unit, Conditions.FinalFire) ? 2 : Is(item.Unit, Conditions.FirstFire) ? 1 : 0).Distinct().ToArray();
            if (marks.Length != 1 && firers.All(item => withoutInherent?.Contains(item.Unit.Id) != true))
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
        var targetSide = targets.FirstOrDefault()?.Side ?? vehicles.FirstOrDefault()?.Side ?? state.Sides.FirstOrDefault(item => item.Id != side)?.Id;
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

                used = [.. named.Select(Weapon)];

                // A7.351 (table player, pass 9): a squad that used one SW (a mortar, a PF Check, or spotting) fires its inherent FP with no second SW.
                if (state.SupportWeaponUses.Any(item => item.Unit == unit.Id))
                {
                    return (null, $"play.fire-sw-limit: {unit.Id} has used a SW this phase; with its inherent FP it fires no other (A7.351)");
                }
            }

            // A7.351 (rulings R9.2, R9.7): a squad whose only fire this phase is one SW use still fires its inherent FP.
            var swOnly = state.SupportWeaponUses.Any(item => item.Unit == unit.Id);
            firerFacts.Add(new FireFirer(unit.Id, unit.Definition!.Definition, at.ToString(), Is(unit, Conditions.Broken), Is(unit, Conditions.Pinned),
                Is(unit, Conditions.Concealed), Fired(unit) && !swOnly, used is not null)
            {
                FirstFireMarked = Is(unit, Conditions.FirstFire) && !swOnly ? true : null,
                FinalFireMarked = state.Phase == "mph" && Is(unit, Conditions.FinalFire) ? true : null,
                // A7.351, A7.352: a crew, HS, or SMC that fired a Gun loses its inherent FP; a squad does not.
                GunFired = state.GunCrewsFired.Contains(unit.Id, StringComparer.Ordinal) && unit.Kind != "asl:squad" ? true : null,
                Weapons = used,
                UsesInherentFp = withoutInherent?.Contains(unit.Id) == true ? false : null,
                Fanatic = Is(unit, Conditions.Fanatic) ? true : null,
                Wounded = Is(unit, Conditions.Wounded) ? true : null,
                Cx = Is(unit, Conditions.Cx) ? true : null,
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
            [.. targets.Select(unit => Target(unit, target))],
            targetSide is null ? null : state.Side(targetSide)?.Elr,
            null)
        {
            FireKind = kind,
            TargetMovement = kind is null or ScenarioA1FireCalculator.BoundingFirstFire ? null : new FireMovement(state.Movement?.Assault ?? false),
            FiringSideElr = kind == ScenarioA1FireCalculator.FinalProtectiveFire ? state.Side(side)?.Elr : null,
            OtherDirectors = directors.Count > 1 ? [.. directors.Skip(1).Select(Director)] : null,
            Companions = companions.Length > 0 ? [.. companions.Select(unit => Target(unit, target))] : null,
            Vehicles = vehicles.Length > 0 ? [.. vehicles.Select(unit => Vehicle(unit, target))] : null,
            VehicleFire = vehicleFirers.Length > 0 ? VehicleFire(state, vehicleFirers[0], firers[0].At) : null,
            TargetSideNoQuarter = targetSide is not null && state.NoQuarter.Contains(targetSide, StringComparer.Ordinal) ? true : null,
            FiringSideNoQuarter = kind == ScenarioA1FireCalculator.FinalProtectiveFire && state.NoQuarter.Contains(side, StringComparer.Ordinal) ? true : null,
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
            [.. targets.Select(unit => Target(unit, target))],
            state.Side(targetSide)?.Elr, null)
        {
            FireKind = ScenarioA1FireCalculator.ResidualFire,
            TargetMovement = new FireMovement(movement.Assault),
            ResidualFp = fp,
            Vehicles = vehicles.Length > 0 ? [.. vehicles.Select(unit => Vehicle(unit, target))] : null,
            Companions = companions.Length > 0 ? [.. companions.Select(unit => Target(unit, target))] : null,
            TargetSideNoQuarter = state.NoQuarter.Contains(targetSide, StringComparer.Ordinal) ? true : null,
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
        };
    }

    private static bool Is(IGameObject item, string condition) => GameState.Condition(item, condition) == ConditionState.True;

    /// <summary>A unit in the target Location, with the Fanatic (A10.8) and heroic (A15.21) states the Fire package reads.</summary>
    internal static FireTarget Target(UnitInstance unit, BoardLocation at) =>
        new(unit.Id, unit.Definition?.Definition, at.ToString(), Is(unit, Conditions.Broken),
            Is(unit, Conditions.Pinned), Is(unit, Conditions.Concealed), Is(unit, Conditions.Hidden), unit.Kind == UnitKinds.Dummy,
            Is(unit, Conditions.Wounded), Is(unit, Conditions.Disrupted))
        {
            Fanatic = Is(unit, Conditions.Fanatic) ? true : null,
            Heroic = Is(unit, Conditions.Heroic) ? true : null,
            Berserk = Is(unit, Conditions.Berserk) ? true : null,
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

        // A7.351 (table player, pass 9b): a squad whose only fire is one SW still fires its inherent FP, and an unfired ATR fires like a MG.
        return !state.SupportWeaponUses.Any(item => item.Unit == unit.Id)
            && !state.Equipment.Any(item => item.Status == InstanceStatus.Active && item.Holding is { Role: HoldingRole.Possessed } holding
                && holding.Holder == unit.Id && (item.Kind == "asl:mg" || (item.Definition is { } weapon && LiveOrdnance.LatwType(weapon.Definition) == "atr"))
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
        else
        {
            // The recorded choices (directors, weapons, a Multiple ROF shot) select what the state is read for; the state's
            // facts are then compared whole.
            var directors = (recorded.Director is null ? [] : new[] { recorded.Director.UnitId! })
                .Concat(recorded.OtherDirectors?.Select(item => item.UnitId!) ?? []).ToArray();
            var weapons = recorded.Firers?.Where(item => item.Weapons is { Count: > 0 })
                .ToDictionary(item => item.UnitId!, item => (IReadOnlyList<string>)[.. item.Weapons!.Select(weapon => weapon.EquipmentId!)], StringComparer.Ordinal);
            var alone = recorded.Firers?.Where(item => item.UsesInherentFp == false).Select(item => item.UnitId!).ToArray();
            (expected, reason) = LiveFire.FromState(state, fire.Firers, directors, target, weapons, alone);
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
            Targets = recorded.Targets is null || expected.Targets is null ? expected.Targets
                : [.. expected.Targets.Zip(recorded.Targets, (fact, record) => fact with { KnownEnemyInLos = record.KnownEnemyInLos, Captors = record.Captors })],
            FirerLocationsAdjacent = recorded.FirerLocationsAdjacent,
            WithinSubsequentFirstFireRange = recorded.WithinSubsequentFirstFireRange,
            Companions = recorded.Companions is null ? null : expected.Companions,

            // The owners' answers are declared, and the projector checks them against the choices made (ruling R5.8).
            Choices = recorded.Choices,
            AfvCover = GamePlanner.CoverAt(state, target, expected.Targets?.Select(item => state.Unit(item.UnitId!)?.Side).FirstOrDefault(side => side is not null)),

            // C11 (ruling R8.3): the Gun in the target Location, its Emplacement and gunshield, is a map read recorded with the attack.
            GunTarget = recorded.GunTarget,
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
