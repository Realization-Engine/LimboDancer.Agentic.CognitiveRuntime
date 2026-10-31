using System.Text.Json;
using System.Text.Json.Serialization;
using LimboDancer.Domains.Asl.Maps.Coordinates;
using LimboDancer.Domains.Asl.Rules;
using LimboDancer.Domains.Asl.Units.Catalog;
using LimboDancer.Domains.Asl.Units.State;
using LimboDancer.Domains.Asl.Units.Vocabulary;

namespace LimboDancer.Domains.Asl.Play;

/// <summary>
/// The facts of a live fire attack that the game state decides (Fire in Live Play, unit step 18): the phase and side,
/// the fire group and its director, the target Location's units, the target side's ELR, and the month. The map facts
/// (range, levels, LOS, terrain) are added by the planner from the map read.
/// </summary>
public static class LiveFire
{
    private static readonly Lazy<ScenarioA1FireReference> CatalogReference = new(() => new ScenarioA1FirePackage().Reference);

    /// <summary>The built-in vocabulary (embedded, read once), for the one SMC test the planner makes too, <c>IsA(kind, "asl:smc")</c> (R5's finding 10).</summary>
    private static readonly Lazy<UnitVocabulary> Vocabulary = new(UnitVocabulary.Asl);

    private sealed record VehiclePrintedFacts(VehicleBreakdownUseFacts? Breakdown, bool? HasSecondary, int? Passengers, int? Towing);

    // Keep only immutable source facts per vehicle, not another retained copy of the whole catalog.
    private static readonly Lazy<IReadOnlyDictionary<string, VehiclePrintedFacts>> VehiclePrintedNumbers = new(() =>
        UnitCatalogs.Read(UnitCatalogs.ScenarioA1, Vocabulary.Value)!.Catalog!.Definitions.Where(definition => definition.Kind == "asl:vehicle")
            .ToDictionary(definition => definition.Id, definition => new VehiclePrintedFacts(PrintedVehicleBreakdown(definition),
                VehicleArmamentFacts.HasSecondary(definition),
                PrintedCapacity(definition, "asl:passenger-capacity"), PrintedCapacity(definition, "asl:towing")), StringComparer.Ordinal));

    private static int? PrintedCapacity(UnitDefinition definition, string attribute) => definition.Printed("front", attribute) switch
    {
        { State: PrintedState.NotPrinted } => 0,
        { State: PrintedState.Printed, Value.Number: { } number } => number,
        _ => null,
    };

    internal static bool VehicleRecalledAfterDisable(UnitInstance vehicle, bool maDisabled)
    {
        var printed = vehicle.Definition is { Catalog.Catalog: Catalog } definition
            ? VehiclePrintedNumbers.Value.GetValueOrDefault(definition.Definition) : null;
        return printed is not null && VehicleArmamentFacts.RecalledAfterDisable(vehicle, maDisabled, printed.HasSecondary,
            printed.Passengers, printed.Towing);
    }

    /// <summary>D3.7 (PDF pp. 201-202): read printed evidence without treating the old inherent B12 field as a printed number.</summary>
    internal static VehicleBreakdownUseFacts? PrintedVehicleBreakdown(UnitDefinition definition)
    {
        var b = definition.Printed("front", "asl:printed-breakdown");
        var x = definition.Printed("front", "asl:printed-elimination");
        var circled = definition.Printed("front", "asl:breakdown-circled");
        var extension = definition.Printed("front", "asl:breakdown-note-extends");
        static bool KnownNumber(PrintedValue? value) => value is { State: PrintedState.NotPrinted }
            or { State: PrintedState.Printed, Value.Number: not null };
        if (!KnownNumber(b) || !KnownNumber(x) || circled is not { State: PrintedState.Printed, TraitPresent: { } circle }
            || extension is not { State: PrintedState.Printed, TraitPresent: { } extends })
        {
            return null;
        }
        return new(VehicleBreakdownWeapon.MainArmament, b!.Value?.Number, x!.Value?.Number, circle, extends, false, false);
    }

    /// <summary>One authoritative snapshot for command and replay; callers show it only through the established owner/inspection views.</summary>
    internal static (VehicleBreakdownUseFacts? Facts, string? Reason) VehicleBreakdownFacts(GameState state, UnitInstance vehicle,
        VehicleBreakdownWeapon weapon, bool intensive = false)
    {
        var listing = vehicle.Definition is { Catalog.Catalog: Catalog } definition
            ? VehiclePrintedNumbers.Value.GetValueOrDefault(definition.Definition) : null;
        var printed = listing?.Breakdown;
        if (printed is null)
        {
            return (null, "play.vehicle-breakdown-facts: printed B/X, circled B and Vehicle Note evidence must be known (D3.7)");
        }
        if (listing is not { HasSecondary: not null, Passengers: not null, Towing: not null })
        {
            return (null, "play.vehicle-armament-facts: listed Secondary Armament state and Passenger/Towing capability must be known (D3.7)");
        }
        var disabledCondition = weapon switch
        {
            VehicleBreakdownWeapon.MainArmament => Conditions.Disabled,
            VehicleBreakdownWeapon.BowMg => Conditions.BmgDisabled,
            VehicleBreakdownWeapon.CoaxialMg => Conditions.CmgDisabled,
            _ => null,
        };
        if (disabledCondition is { } condition && ScenarioA1VehicleBreakdownResolutionCalculator.WeaponUseRefusal(Is(vehicle, condition)) is { } disabled)
        {
            return (null, disabled);
        }
        if (printed.NoteExtends && weapon != VehicleBreakdownWeapon.MainArmament)
        {
            return (null, "play.vehicle-breakdown-note: the Vehicle Note must identify this armament before its extension can be applied (D3.7)");
        }
        var facts = printed with
        {
            Weapon = weapon,
            LowAmmo = state.LowAmmoVehicles.Contains(vehicle.Id, StringComparer.Ordinal),
            AmmunitionShortage = ScenarioA1UnitSubstitution.AmmunitionShortage(state.SpecialRules, vehicle.Side),
            WinterReduction = GamePlanner.ExtremeWinterReduction(state, vehicle.Side) ?? 0,
            IntensiveFire = intensive,
        };
        return (ScenarioA1VehicleBreakdownResolutionCalculator.WithVehicleCrew(facts, vehicle.TemporaryCrew?.Manning,
            vehicle.TemporaryCrew?.Captured ?? false), null);
    }

    internal static (VehicleCloseCombatFacts? Facts, string? Reason) WithVehicleCloseCombatBreakdown(GameState state, UnitInstance vehicle,
        VehicleCloseCombatFacts facts)
    {
        facts = facts with
        {
            Vehicle = VehicleCloseCombatAvailability(vehicle, facts.Vehicle!)
        };
        return WithVehicleCloseCombatNumbers(state, vehicle, facts);
    }

    internal static VehicleCloseCombatVehicle VehicleCloseCombatAvailability(UnitInstance vehicle, VehicleCloseCombatVehicle facts)
    {
        var availability = new[]
        {
            ("ma", Conditions.Malfunctioned, Conditions.Disabled), ("bmg", Conditions.BmgMalfunctioned, Conditions.BmgDisabled),
            ("cmg", Conditions.CmgMalfunctioned, Conditions.CmgDisabled), ("aamg", Conditions.AamgMalfunctioned, Conditions.AamgDisabled)
        };
        return facts with
        {
            UnavailableWeapons = [.. availability.Where(item => Is(vehicle, item.Item2) || Is(vehicle, item.Item3)).Select(item => item.Item1)]
        };
    }

    private static (VehicleCloseCombatFacts? Facts, string? Reason) WithVehicleCloseCombatNumbers(GameState state, UnitInstance vehicle, VehicleCloseCombatFacts facts)
    {
        if (facts.ByVehicle != true)
        {
            return (facts, null);
        }

        if (CatalogReference.Value.Definitions.GetValueOrDefault(facts.Vehicle!.DefinitionId!) is not { } definition)
        {
            return (null, "play.cc-vehicle-facts: the vehicle definition cannot be read");
        }
        var weapons = new Dictionary<string, VehicleBreakdownUseFacts>(StringComparer.Ordinal);
        foreach (var name in ScenarioA1VehicleCloseCombat.Weapons(facts.Vehicle, definition))
        {
            var kind = name == "ma" ? VehicleBreakdownWeapon.MainArmament
                : name == "cmg" ? VehicleBreakdownWeapon.CoaxialMg : VehicleBreakdownWeapon.AntiAircraftMg;
            var read = VehicleBreakdownFacts(state, vehicle, kind);
            if (read.Facts is null)
            {
                return (null, read.Reason);
            }
            weapons.Add(name, read.Facts);
        }
        return (facts with
        {
            VehicleBreakdown = new(weapons)
        }, null);
    }

    /// <summary>The same main-armament source/use check for the owner's weapon list and the command.</summary>
    public static string? MainArmamentUseRefusal(GameState state, UnitInstance vehicle) =>
        VehicleBreakdownFacts(state, vehicle, VehicleBreakdownWeapon.MainArmament).Reason;

    /// <summary>The moving stack's Assault Movement as fire reads it (A4.61; pass 40): declared, and no mover broken or berserk since.</summary>
    private static bool AssaultOf(GameState state) => state.Movement is { } moving && ScenarioA1FireEligibility.AssaultHolds(moving.Assault,
        moving.Movers.Select(id => state.Unit(id)).OfType<UnitInstance>().Select(unit => (Is(unit, Conditions.Broken), Is(unit, Conditions.Berserk))));

    /// <summary>A21.1 (ruling R13.7): a weapon of another nationality than its possessor's is captured (Rules decides it, pass 32.c).</summary>
    public static bool? CapturedBy(string? weaponDefinition, UnitInstance unit) =>
        ScenarioA1FireEligibility.CapturedBy(
            weaponDefinition is null ? null : CatalogReference.Value.Definitions.GetValueOrDefault(weaponDefinition)?.Nationality,
            unit.Definition is { } holder ? CatalogReference.Value.Definitions.GetValueOrDefault(holder.Definition)?.Nationality : null);

    /// <summary>Whether a dismantled MG fires as its side's LMG (A9.8; pass 45).</summary>
    public static bool FiresAsLmg(EquipmentInstance weapon)
    {
        ArgumentNullException.ThrowIfNull(weapon);
        var definition = weapon.Definition is { } reference ? CatalogReference.Value.Definitions.GetValueOrDefault(reference.Definition) : null;
        return ScenarioA1FireEligibility.DismantledFiresAsLmg(Is(weapon, Conditions.Dismantled), definition?.Nationality, definition?.Size);
    }

    /// <summary>The definition a weapon fires as: its own, or its side's LMG for a dismantled German MMG or HMG (A9.8; pass 45).</summary>
    public static string? FiredAs(EquipmentInstance weapon)
    {
        ArgumentNullException.ThrowIfNull(weapon);
        var own = weapon.Definition?.Definition;
        if (!FiresAsLmg(weapon) || own is null)
        {
            return own;
        }

        return ScenarioA1FireEligibility.LmgDefinition(CatalogReference.Value.Definitions.Values, CatalogReference.Value.Definitions[own].Nationality) ?? own;
    }

    public const string Catalog = "asl-scenario-a1";
    public const string CatalogVersion = "1.19.0";

    /// <summary>Whether the packages read the game's catalog: this catalog at an older or equal version of the same major (ruling R65.4; Rules decides).</summary>
    public static bool ServesCatalog(GameState state)
    {
        ArgumentNullException.ThrowIfNull(state);
        return ScenarioA1Definitions.CatalogServes(Catalog, CatalogVersion, state.Catalog.Catalog, state.Catalog.Version);
    }

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
        IReadOnlyDictionary<string, string>? partners = null, string? molUser = null, bool embarking = true, bool wrecks = true,
        bool legacyVehicleBreakdown = false, bool legacyFinalFire = false)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(firerIds);
        ArgumentNullException.ThrowIfNull(directorIds);
        if (firerIds.Count == 0 || firerIds.Distinct(StringComparer.Ordinal).Count() != firerIds.Count)
        {
            return (null, "play.fire-firers: name each firer once");
        }

        if (!ServesCatalog(state))
        {
            return (null, $"play.fire-catalog: the Fire package reads {Catalog}@{CatalogVersion}, and this game uses {state.Catalog.Catalog}@{state.Catalog.Version}");
        }

        // D1.83, D3.4 (ruling R25.7): a vehicle's MG fires alone, with no leader and no Infantry in its group. Rules decides each block (pass 32.c).
        var vehicleFirers = firerIds.Select(state.Unit).OfType<UnitInstance>().Where(IsVehicle).ToArray();
        // D3.5 (pass 50): the weapons named under the vehicle's own id are its BMG and CMG, not a SW of its group.
        if (ScenarioA1FireEligibility.VehicleFiresAlone(vehicleFirers.Length, firerIds.Count, directorIds.Count,
            weapons?.Keys.Any(key => vehicleFirers.All(vehicle => vehicle.Id != key)) == true) is { } grouped)
        {
            return (null, grouped);
        }

        if (vehicleFirers.Length > 0 && ScenarioA1FireEligibility.AbandonedVehicleBar(vehicleFirers[0].Id, Is(vehicleFirers[0], Conditions.Abandoned)) is { } abandoned)
        {
            return (null, abandoned);
        }

        // D5.311 (pass 65, ruling R65.31): a crew broken inside its AFV fires none of its weapons.
        if (vehicleFirers.Length > 0 && GamePlanner.BrokenCrewBar(state, vehicleFirers[0]) is { } brokenCrew)
        {
            return (null, brokenCrew);
        }

        // A21.22 (pass 65, ruling R65.22): a SMC crew fires only its AAMG while CE, its BMG or CMG while BU, and never in a Player Turn it moved.
        if (vehicleFirers.Length > 0 && vehicleFirers[0] is { TemporaryCrew.Manning: ScenarioA1CapturedVehicleRules.Smc } manned
            && (weapons?.GetValueOrDefault(manned.Id) is { Count: > 0 } mgs ? mgs
                : [manned.Definition is { } own && CatalogReference.Value.Definitions.GetValueOrDefault(own.Definition)?.MainArmament == "aamg" ? "aamg" : "ma"])
                .Select(weapon => GamePlanner.SmcCrewFireBar(state, manned, weapon)).FirstOrDefault(bar => bar is not null) is { } smcBar)
        {
            return (null, smcBar);
        }

        if (vehicleFirers.Length > 0 && ScenarioA1FireEligibility.VehicleWeaponsNamedBar(vehicleFirers[0].Id, !LiveOrdnance.IsTank(vehicleFirers[0]),
            weapons?.GetValueOrDefault(vehicleFirers[0].Id)?.Count ?? 0) is { } unnamed)
        {
            return (null, unnamed);
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
            // A8.3 (pass 45): a unit with no counter that fires a MG marked First Fire uses Subsequent First Fire with it.
            () => [.. firers.Select(item => (Is(item.Unit, Conditions.FinalFire), Is(item.Unit, Conditions.FirstFire)
                || (weapons?.TryGetValue(item.Unit.Id, out var fired) == true
                    && fired.Any(id => state.Find(id) is EquipmentInstance weapon && Is(weapon, Conditions.FirstFire)))))],
            firers.All(item => withoutInherent?.Contains(item.Unit.Id) != true), withoutInherent is { Count: > 0 },
            firers.Where(item => Is(item.Unit, Conditions.FirstFire)).All(item => withoutInherent?.Contains(item.Unit.Id) == true)));
        if (kindRefusal is not null)
        {
            return (null, kindRefusal);
        }

        // A8.1, A7.308, A15.41: who the attack hits, as Rules sorts the target Location's units.
        var movers = state.Phase == "mph" && state.Movement is { } movement && movement.Location == target ? movement.Movers : null;
        UnitInstance[] atTarget = [.. state.At(target).OfType<UnitInstance>()];

        // D6.4, D6.5, D5.43 (pass 65, ruling R65.14): the units boarding or disembarking with the vehicle's expenditure are moving Infantry targets, a
        // loader attacked on foot in the Location it boards in. A record made before the pass does not ask for them (embarking false), and its targets
        // are read as before.
        FireEmbarking? boarding = null;
        if (embarking && movers is not null && state.Movement is { Vehicle: true, Embarking: { Count: > 0 } loaders } loading)
        {
            movers = [.. movers, .. loaders];
            atTarget = [.. atTarget, .. loaders.Where(id => atTarget.All(unit => unit.Id != id)).Select(state.Unit).OfType<UnitInstance>()
                .Where(unit => unit.Status == InstanceStatus.Active && state.Location(unit.Id)?.Location == target)];
            // The two flags are written only when true (the boundary review).
            boarding = new FireEmbarking(loaders, loading.Unloading ? true : null,
                state.Unit(loading.Movers.Count > 0 ? loading.Movers[0] : loading.Members[0])?.Definition?.Definition is { } conveyance
                    && CatalogReference.Value.Definitions.GetValueOrDefault(conveyance)?.Unarmored == true ? true : null);
        }
        else if (embarking && state.Phase == "mph" && state.Movement is { Vehicle: true, WindowOpen: true, BailedOut: { Count: > 0 } bailed }
            && bailed.Where(id => state.Location(id)?.Location == target).ToArray() is { Length: > 0 } bailedHere)
        {
            // D6.24, D6.5 (pass 65, ruling R65.17): Riders that Bailed Out with the AFV's expenditure, in the Location it left or its own, are moving
            // Infantry targets that spent all their MF, with FFNAM as unloaders and no FFMO, since an AFV is no unarmored conveyance.
            movers = [.. movers ?? Array.Empty<string>(), .. bailedHere];
            boarding = new FireEmbarking(bailedHere, true, null);
        }

        // A7.21, A7.211, A15.432 (pass 72, rulings R72.7, R72.21): whether a firer, Infantry or a vehicle, stands in the target Location; Rules decides that
        // the attack is TPBF at the other side's units there outside the MPh's moving stack. Before the pass such an attack was refused, its firer among its
        // own targets, so no record holds one.
        UnitInstance UnitAt(string id) => atTarget.First(unit => unit.Id == id);
        var selection = ScenarioA1FireEligibility.SelectTargets([.. atTarget.Select(unit => new TargetUnitStateFacts(unit.Id, unit.Status == InstanceStatus.Active, unit.Side,
            IsVehicle(unit), unit.Kind == UnitKinds.Dummy, unit.Definition is not null, movers?.Contains(unit.Id)))], movers is not null, side,
            firers.Any(item => atTarget.Any(unit => unit.Id == item.Unit.Id)));
        if (selection.Refusal is { } outside)
        {
            return (null, outside);
        }

        // D10.1 (pass 65, ruling R65.23): an enemy non-burning wreck is attacked as its vehicle, outside the MPh's moving stack. Ruling R25.3's one vehicle
        // a Location's attack reads stands until increment 2k: a wreck joins only as the Location's one vehicle. A record made before the pass lists no
        // wreck (wrecks false), and its vehicles are read as before.
        UnitInstance[] wrecked = wrecks && movers is null && selection.Vehicles.Count == 0
            ? [.. GamePlanner.WrecksAt(state, target).Where(wreck => wreck.Side != side && !GamePlanner.IsBurning(state, wreck))]
            : [];
        if (wrecked.Length != 1)
        {
            wrecked = [];
        }

        var targetSide = ScenarioA1FireEligibility.TargetSide(side, selection.Targets.Select(id => UnitAt(id).Side),
            selection.Vehicles.Count > 0 ? UnitAt(selection.Vehicles[0]).Side : null, state.Sides.FirstOrDefault(item => item.Id != side)?.Id);
        var friendlyTargets = selection.Targets.Any(id => UnitAt(id).Side == side);

        // A9.8 (pass 45): a German dm MMG or HMG fires as a German LMG, so its facts name that definition.
        FireWeapon Weapon(string id) => state.Find(id) is EquipmentInstance equipment
            ? new FireWeapon(equipment.Id, FiredAs(equipment), Is(equipment, Conditions.Malfunctioned), Fired(equipment),
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
                        state.Find(id) is EquipmentInstance held && Is(held, Conditions.Dismantled) && !FiresAsLmg(held),
                        state.Find(id) is EquipmentInstance { Kind: "asl:ft" }))],
                    state.AssaultWeaponUsers.Contains(unit.Id, StringComparer.Ordinal), withoutInherent?.Contains(unit.Id) == true,
                    () => state.Equipment.Any(item => item.Status == InstanceStatus.Active && item.Holding is { Role: HoldingRole.Possessed } other && other.Holder == unit.Id
                        && !named.Contains(item.Id, StringComparer.Ordinal) && (Fired(item) || Is(item, Conditions.FirstFire))),
                    () => state.SupportWeaponUses.Any(item => item.Unit == unit.Id))) is { } weaponBar)
                {
                    return (null, weaponBar);
                }

                used = [.. named.Select(id => Weapon(id) is var weapon && CapturedBy(weapon.DefinitionId, unit) is { } captured ? weapon with { Captured = captured } : weapon)];

                // A4.41 (pass 40): a MMG or HMG carried into a new Location in the MPh does not fire in the AFPh.
                if (ScenarioA1FireEligibility.MovedWeaponBar(state.Phase, used.Select(weapon => (weapon.EquipmentId ?? string.Empty,
                    weapon.EquipmentId is { } carried && state.MovedWeapons.Contains(carried, StringComparer.Ordinal),
                    weapon.DefinitionId is { } definition ? CatalogReference.Value.Definitions.GetValueOrDefault(definition)?.Size : null))) is { } movedBar)
                {
                    return (null, movedBar);
                }
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
                molUser == unit.Id, GreenInexperienced(state, unit), Is(unit, Conditions.Berserk), UnderscoredElr(state, unit))));
        }

        FireDirector Director((UnitInstance Unit, BoardLocation At) item) =>
            ScenarioA1FireEligibility.Director(new DirectorRecordFacts(item.Unit.Id, item.Unit.Definition!.Definition, item.At.ToString(), Is(item.Unit, Conditions.Broken),
                Is(item.Unit, Conditions.Pinned), Is(item.Unit, Conditions.Concealed), Fired(item.Unit), Is(item.Unit, Conditions.FirstFire),
                state.SupportWeaponDirectors.Any(directed => directed.Leader == item.Unit.Id), Is(item.Unit, Conditions.Wounded), Is(item.Unit, Conditions.Cx)));

        // Rulings R12.8, R12.9, R12.11: the firing side's units in a Melee or as prisoners, a prisoner's Guard, and Encirclement; Rules assembles the attack.
        // C6.43, C6.44 (pass 45): a MG fired alone by the unit that set up with it, from its setup Location, at its Bore Sighted Location.
        var boreSighted = state.BoreSights.Count > 0 && ScenarioA1FireEligibility.MgBoreSighted(firerFacts.Select(firer => (firer.UsesInherentFp == false,
            (IReadOnlyList<MgSightingFacts>)[.. (firer.Weapons ?? []).Select(weapon => state.BoreSights.FirstOrDefault(sight => sight.Gun == weapon.EquipmentId) is { } sight
                ? new MgSightingFacts(true, sight.Crew == firer.UnitId, state.Location(firer.UnitId!)?.Location == sight.SetupLocation, sight.Location == target,
                    state.Find(weapon.EquipmentId!) is EquipmentInstance sighted && Is(sighted, Conditions.Dismantled))
                : new MgSightingFacts(false, false, false, false, false))])));
        var attack = ScenarioA1FireEligibility.Assemble(new AttackAssemblyFacts(state.Phase, side == state.PhasingSide, firers[0].At.ToString(), target.ToString(),
            firerFacts, [.. directors.Select(Director)], state.ScenarioMonth,
            [.. selection.Targets.Select(UnitAt).Select(unit => ScenarioA1FireEligibility.AttackTarget(new AttackTargetFacts(Target(state, unit, target), unit.Side == side,
                Is(unit, Conditions.Captured), unit.Custodian, state.Encircled(unit))))],
            targetSide is null ? null : state.Side(targetSide)?.Elr, kind, AssaultOf(state), () => state.Side(side)?.Elr, friendlyTargets,
            [.. selection.Vehicles.Select(id => Vehicle(UnitAt(id), target)), .. wrecked.Select(wreck => Vehicle(wreck, target))], vehicleFirers.Length > 0
                ? VehicleFire(state, vehicleFirers[0], firers[0].At, weapons?.GetValueOrDefault(vehicleFirers[0].Id), !legacyFinalFire) : null,
            targetSide is not null && state.NoQuarter.Contains(targetSide, StringComparer.Ordinal), state.NoQuarter.Contains(side, StringComparer.Ordinal),
            Allies(state, side), [.. selection.Companions.Select(id => Target(state, UnitAt(id), target))])) with
        {
            FinalFireVersion = legacyFinalFire ? null : 1,
            MgBoreSighted = boreSighted ? true : null,
            Embarking = boarding,
            GunCrews = GunCrews(state, selection.Targets),

            // A19.131 (pass 72, rulings R72.11, R72.12): the firing side's Ammunition Shortage, from the game's special rules; Rules says what it does.
            AmmunitionShortage = state.SpecialRules.Count > 0 && ScenarioA1UnitSubstitution.AmmunitionShortage(state.SpecialRules, side) ? true : null,
        };
        return legacyVehicleBreakdown ? (attack, null) : WithVehicleBreakdown(state, attack);
    }

    private static (FireAttack? Attack, string? Reason) WithVehicleBreakdown(GameState state, FireAttack attack)
    {
        if ((attack.VehicleFire?.VehicleId ?? attack.Overrun?.VehicleId) is not { } id || state.Unit(id) is not { } vehicle)
        {
            return (attack, null);
        }
        var names = attack.Overrun is { } overrun && overrun.DefinitionId is { } definition
            && CatalogReference.Value.Definitions.TryGetValue(definition, out var printed)
            ? ScenarioA1FireCalculator.OverrunWeapons(overrun, printed).Select(item => item.Weapon).ToArray()
            : attack.VehicleFire?.Weapons is { Count: > 0 } weapons
                ? weapons.Select(item => item.Weapon!).ToArray() : [FireOverrunEffect.MainArmament];
        var facts = new Dictionary<string, VehicleBreakdownUseFacts>(StringComparer.Ordinal);
        foreach (var name in names)
        {
            var weapon = name switch
            {
                FireOverrunEffect.MainArmament => VehicleBreakdownWeapon.MainArmament,
                FireOverrunEffect.BowMg => VehicleBreakdownWeapon.BowMg,
                FireOverrunEffect.CoaxialMg => VehicleBreakdownWeapon.CoaxialMg,
                _ => (VehicleBreakdownWeapon)(-1),
            };
            var read = VehicleBreakdownFacts(state, vehicle, weapon);
            if (read.Facts is null)
            {
                return (null, read.Reason);
            }
            facts.Add(name, read.Facts);
        }
        return (attack with
        {
            VehicleBreakdown = new FireVehicleBreakdownFacts(facts)
        }, null);
    }

    /// <summary>
    /// The targets that man a Gun (C11; pass 65, ruling R65.28), read from the state in one pass over its equipment; null when none does. Rules keeps
    /// such a crew to ruling R8.3's reading, and reads any other crew as ordinary Infantry (A1.123).
    /// </summary>
    internal static IReadOnlyList<string>? GunCrews(GameState state, IReadOnlyCollection<string>? targets) =>
        targets is not { Count: > 0 } ? null
            : state.Equipment.Where(item => item.Status == InstanceStatus.Active && item.Holding is { Role: HoldingRole.Manned } holding && targets.Contains(holding.Holder))
                .Select(item => item.Holding!.Holder).Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal).ToArray() is { Length: > 0 } crews ? crews : null;

    /// <summary>An attack read from the state with the targets that man a Gun (pass 65, ruling R65.28).</summary>
    private static (FireAttack? Attack, string? Reason) WithGunCrews(GameState state, (FireAttack? Attack, string? Reason) read)
    {
        if (read.Attack is not { } attack)
        {
            return read;
        }

        return (attack with
        {
            GunCrews = GunCrews(state, attack.Targets?.Select(item => item.UnitId!).ToArray())
        }, read.Reason);
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
        if (!ServesCatalog(state))
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
        return WithGunCrews(state, ScenarioA1FireEligibility.DemolitionCharge(new DemolitionChargeFacts(chargeId, chargeOk, charge?.Definition?.Definition, mode, placement is not null,
            placement?.Unit, placement?.Cx, placement?.TargetsConcealed, possessor, user is { Definition: not null }, user?.Definition?.Definition, side,
            user is not null && Is(user, Conditions.Cx), user is not null && Is(user, Conditions.BoundingFire), ok ? GreenInexperienced(state, user!) : null,
            ok ? CapturedBy(charge!.Definition!.Definition, user!) : null, ok ? state.Location(userId!)?.Location.ToString() : null, side == state.PhasingSide, state.Phase,
            target.ToString(), here, AssaultOf(state),
            [.. units.Select(unit => new LocationUnitFacts(unit.Id, unit.Status == InstanceStatus.Active, unit.Side, IsVehicle(unit), unit.Definition is not null,
                Is(unit, Conditions.Captured), here && movement!.Movers.Contains(unit.Id)))],
            targetSide, targetSide is null ? null : state.Side(targetSide)?.Elr, targetSide is not null && state.NoQuarter.Contains(targetSide, StringComparer.Ordinal),
            ok ? state.Side(side!)?.Elr : null, ok && state.NoQuarter.Contains(side, StringComparer.Ordinal), ok ? Allies(state, side!) : null, state.ScenarioMonth,
            id => Target(state, units.First(unit => unit.Id == id), target), id => Vehicle(units.First(unit => unit.Id == id), target),
            id => units.First(unit => unit.Id == id).Custodian, id => state.Encircled(units.First(unit => unit.Id == id)))));
    }

    /// <summary>
    /// Whether a phasing vehicle may fire as Bounding First Fire now (D3.3; ruling R6.9): while it moves, once the DEFENDER has passed on
    /// its last MP expenditure, or at the outset of its MPh while no other move is under way and it has not ended its move.
    /// </summary>
    public static bool MayBoundingFire(GameState state, string vehicleId, bool gunDuel = false)
    {
        ArgumentNullException.ThrowIfNull(state);
        var vehicle = state.Unit(vehicleId);
        return ScenarioA1FireEligibility.MayBoundingFire(new BoundingFireFacts(state.Phase,
            vehicle is { Status: InstanceStatus.Active } && IsVehicle(vehicle) && vehicle.Side == state.PhasingSide,
            state.Movement is not null, state.Movement?.Vehicle == true, state.Movement?.WindowOpen == true,
            state.Movement?.Movers.Contains(vehicleId) == true, vehicle?.MovementEnded == true)
        {
            // C2.2401 (pass 55): a Gun Duel's first shot is made while the DEFENDER's window is open.
            GunDuel = gunDuel,
        });
    }

    /// <summary>A vehicle definition's turret type (D1.3), as the catalog prints it; null when it prints none.</summary>
    public static string? MaTypeOf(string? definition) => definition is null ? null : CatalogReference.Value.Definitions.GetValueOrDefault(definition)?.MaType;

    /// <summary>A vehicle definition's class (catalog 1.18.0; pass 65, ruling R65.3), such as <c>halftrack</c> or <c>carrier</c>; null when it prints none.</summary>
    public static string? VehicleClassOf(string? definition) => definition is null ? null : CatalogReference.Value.Definitions.GetValueOrDefault(definition)?.VehicleClass;

    /// <summary>Whether a vehicle definition is unarmored (D1.1), as the catalog prints it; null when it prints none.</summary>
    public static bool? UnarmoredOf(string? definition) => definition is null ? null : CatalogReference.Value.Definitions.GetValueOrDefault(definition)?.Unarmored;

    /// <summary>Whether a unit is a vehicle (D1).</summary>
    public static bool IsVehicle(UnitInstance unit) => unit.Kind == "asl:vehicle";

    /// <summary>
    /// Whether a wound given the unit now pins it, as Rules reads it from the state (A17.2, A7.8; pass 71, rulings R71.14, R71.27): a SMC, unbroken, past 3 MF
    /// in the MPh.
    /// </summary>
    public static bool WoundPins(GameState state, UnitInstance unit)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(unit);
        return ScenarioA1Wounds.PinnedByWound(Vocabulary.Value.IsA(unit.Kind, "asl:smc"), GameState.Condition(unit, Conditions.Broken) == ConditionState.True, state.Phase,
            unit.MfSpent, unit.HalfMfSpent);
    }

    /// <summary>
    /// A target's recorded wound pin as a record check takes it (A17.2; pass 71, ruling R71.14): from the record, so a game replays as it was played; but a pin
    /// the state's rule does not give (R3's finding 2: a broken SMC, ruling R71.27, or one not past 3 MF) is left out, so the record differs from the state's
    /// facts and is refused. A record made before the pass carries none.
    /// </summary>
    public static bool? RecordedWoundPins(GameState state, string? unitId, bool? recorded)
    {
        ArgumentNullException.ThrowIfNull(state);
        return recorded == true && !(unitId is not null && state.Unit(unitId) is { } unit && WoundPins(state, unit)) ? null : recorded;
    }

    /// <summary>
    /// Whether an AFV's crew is Crew Exposed: an OT AFV is CE unless under a BU, Stun, Shock, or Recall marker (D5.3, D5.34, C7.42); a CT AFV
    /// only when its owner has removed its BU counter (D5.2; ruling R7.11); an unarmored
    /// vehicle has no crew to expose (D5.1). A Carrier's crew is always CE, stunned or shocked included (D6.84; pass 65, ruling R65.18).
    /// </summary>
    public static bool CrewExposed(UnitInstance vehicle) =>
        ScenarioA1CarrierRules.CrewExposed(VehicleClassOf(vehicle.Definition?.Definition),
            ScenarioA1FireEligibility.CrewExposed(new CrewExposedFacts(GameState.Condition(vehicle, Conditions.ButtonedUp) switch
            {
                ConditionState.True => RuleState.True,
                ConditionState.False => RuleState.False,
                _ => RuleState.Unknown,
            },
                Is(vehicle, Conditions.Stunned), Is(vehicle, Conditions.Recalled), Is(vehicle, Conditions.Shocked), Is(vehicle, Conditions.UnconfirmedKill),
                GamePlanner.IsClosedTopped(vehicle))));

    /// <summary>A vehicle in the target Location, with its crew's state (A7.307, A7.308, D.8B).</summary>
    internal static FireVehicle Vehicle(UnitInstance vehicle, BoardLocation at) =>
        ScenarioA1FireEligibility.VehicleTarget(new VehicleTargetFacts(vehicle.Id, vehicle.Definition?.Definition, at.ToString(), CrewExposed(vehicle),
            Is(vehicle, Conditions.Stunned), Is(vehicle, Conditions.Recalled), Is(vehicle, Conditions.StunRecovery), Is(vehicle, Conditions.Immobilized),
            Is(vehicle, Conditions.Concealed), Is(vehicle, Conditions.Hidden))
        {
            CrewMorale = CrewMorale(vehicle),

            // D10.1 (pass 65, ruling R65.23): a wreck attacked as its vehicle.
            Wreck = vehicle.Status == InstanceStatus.Wrecked,
        });

    /// <summary>
    /// The vehicles of an attack read from the state, each with the PRC its record carries (pass 65, ruling R65.5): the fact is taken from the record at
    /// replay, never read again, and a record made before the pass carries none.
    /// </summary>
    internal static IReadOnlyList<FireVehicle>? WithRecordedPrc(IReadOnlyList<FireVehicle>? state, IReadOnlyList<FireVehicle>? recorded) =>
        state is null || recorded is null ? state
            : [.. state.Select(vehicle => recorded.FirstOrDefault(item => item.VehicleId == vehicle.VehicleId) is { } record
                // Pass 65 (ruling R65.21): so are its Shock and Abandonment, written with the PRC; pass 70 (ruling R70.10): and a crew already broken.
                ? vehicle with { Prc = record.Prc, Shocked = record.Shocked, Abandoned = record.Abandoned, CrewBroken = record.CrewBroken }
                : vehicle with { Prc = null })];

    /// <summary>
    /// The AFV a FT, MOL, or DC attack names (A22.34, A22.612, A23.5; pass 60, ruling R60.15): a vehicle target's state facts, and whether it is Immobile,
    /// immobilized or bogged (C7.346). Its Target Facings are the planner's map reads.
    /// </summary>
    internal static OrdnanceVehicleTarget NamedVehicle(GameState state, UnitInstance vehicle) => LiveOrdnance.VehicleTargetFacts(state, vehicle) with
    {
        Immobile = Is(vehicle, Conditions.Immobilized) || Is(vehicle, Conditions.Bogged) ? true : null,
    };

    /// <summary>
    /// The leadership modifier a vehicle's crew brings to its attacks (D3.44, D3.45; pass 55, rulings R55.16, R55.18): its Armor Leader's, or an
    /// Inexperienced Crew's +1; null with neither. Rules decides which.
    /// </summary>
    public static int? ArmorLeadership(UnitInstance vehicle)
    {
        ArgumentNullException.ThrowIfNull(vehicle);
        return ScenarioA1ArmorLeaders.Leadership(vehicle.ArmorLeader?.Leadership, Is(vehicle, Conditions.InexperiencedCrew));
    }

    /// <summary>
    /// The Morale Level a vehicle's crew checks at when it is not its nationality's (D3.42, D3.45; pass 55, rulings R55.17, R55.18): its Armor
    /// Leader's, or an Inexperienced Crew's 6; null with neither.
    /// </summary>
    public static int? CrewMorale(UnitInstance vehicle)
    {
        ArgumentNullException.ThrowIfNull(vehicle);
        return ScenarioA1ArmorLeaders.CrewMorale(vehicle.ArmorLeader?.Morale, Is(vehicle, Conditions.InexperiencedCrew));
    }

    /// <summary>
    /// A vehicle's MA MG attack (ruling R25.7): its crew's state, Motion (D2.42), a pin (A7.82), its MG's malfunction (D3.7), whether it
    /// fired this Player Turn, and whether its last shot this phase kept its Multiple ROF (C2.24), which the state records.
    /// </summary>
    /// <summary>The picker uses the command's gathered actor facts and the same Final Fire refusal for one inherent or weapon row.</summary>
    public static string? FinalFireRefusal(GameState state, string unit, string? weapon, BoardLocation target, int range, string? partner = null)
    {
        if (state.Phase != "dfph")
        {
            return null;
        }
        var (attack, reason) = FromState(state, [unit], [], target,
            new Dictionary<string, IReadOnlyList<string>> { [unit] = weapon is null ? [] : [weapon] },
            weapon is null ? [] : [unit], partner is null ? null : new Dictionary<string, string> { [unit] = partner });
        if (attack is null)
        {
            return reason;
        }
        attack = attack with
        {
            Range = range
        };
        var facts = attack.VehicleFire is { } vehicle
            ? ScenarioA1FinalFireCalculator.VehicleFacts(attack, vehicle)
            : ScenarioA1FinalFireCalculator.InfantryFacts(attack, attack.Firers![0],
                weapon is null ? null : attack.Firers[0].Weapons?.FirstOrDefault(item => item.EquipmentId == weapon), CatalogReference.Value);
        return ScenarioA1FinalFireCalculator.FinalFireRefusal(facts);
    }

    private static FireVehicleFire VehicleFire(GameState state, UnitInstance vehicle, BoardLocation at, IReadOnlyList<string>? named = null, bool currentFinalFire = false)
    {
        var fire = MainArmamentFire(state, vehicle, at);
        if (currentFinalFire)
        {
            var own = state.VehicleWeaponShots.LastOrDefault(item => item.Vehicle == vehicle.Id && item.Weapon == "ma");
            fire = fire with
            {
                FinalFireMarker = ScenarioA1FinalFireCalculator.VehicleMarker(own?.FireCounter, own is not null,
                    Is(vehicle, Conditions.FirstFire), Is(vehicle, Conditions.FinalFire),
                    state.OrdnanceShots.Any(item => item.Gun == vehicle.Id && item.RateOfFireKept)),
            };
        }

        // D3.5, D3.51, D3.7 (pass 50): the BMG and CMG the attack names, against the vehicle's shots of this Player Turn.
        return named is not { Count: > 0 } ? fire : ScenarioA1FireEligibility.WithVehicleWeapons(fire, new VehicleWeaponsFacts(named,
            Is(vehicle, Conditions.BmgMalfunctioned), Is(vehicle, Conditions.CmgMalfunctioned),
            ScenarioA1FireEligibility.FirePhase(state.Phase, vehicle.Side == state.PhasingSide), VehicleShots(state, vehicle.Id),
            vehicle.Definition is { } definition ? CatalogReference.Value.Definitions.GetValueOrDefault(definition.Definition)?.MaType : null,
            Is(vehicle, Conditions.BoundingFire)));
    }

    /// <summary>
    /// The BMG and CMG a vehicle's counter prints, each with its FP and the reason it may not fire now, or null when it may (D1.8, D3.5, D3.7;
    /// pass 50). Empty for a vehicle that prints neither.
    /// </summary>
    public static IReadOnlyList<(string Weapon, int Fp, string? Bar)> VehicleMgs(GameState state, UnitInstance vehicle)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(vehicle);
        if (!IsVehicle(vehicle) || vehicle.Definition is not { } named || CatalogReference.Value.Definitions.GetValueOrDefault(named.Definition) is not { } definition)
        {
            return [];
        }

        var shots = VehicleShots(state, vehicle.Id);
        var otherPhase = ScenarioA1FireEligibility.OtherFirePhase(shots, ScenarioA1FireEligibility.FirePhase(state.Phase, vehicle.Side == state.PhasingSide),
            Is(vehicle, Conditions.BoundingFire));
        return [.. new[] { FireVehicleWeapon.BowMg, FireVehicleWeapon.CoaxialMg }
            .Where(weapon => ScenarioA1FireCalculator.VehicleWeaponFp(definition, weapon) is not null)
            .Select(weapon => (weapon, ScenarioA1FireCalculator.VehicleWeaponFp(definition, weapon)!.Value, ScenarioA1FireEligibility.VehicleMgBar(
                Is(vehicle, weapon == FireVehicleWeapon.BowMg ? Conditions.BmgMalfunctioned : Conditions.CmgMalfunctioned),
                Is(vehicle, weapon == FireVehicleWeapon.BowMg ? Conditions.BmgDisabled : Conditions.CmgDisabled),
                shots.Any(item => item.Weapon == weapon), otherPhase,
                weapon == FireVehicleWeapon.CoaxialMg && definition.TurretBarredWhileCe(CrewExposed(vehicle)))))];
    }

    /// <summary>A vehicle's weapon shots of this Player Turn, as Rules reads them (D3.5, D3.51; pass 50).</summary>
    internal static IReadOnlyList<VehicleWeaponShotFacts> VehicleShots(GameState state, string vehicleId) =>
        [.. state.VehicleWeaponShots.Where(item => item.Vehicle == vehicleId).Select(item => new VehicleWeaponShotFacts(item.Weapon, item.FirePhase, item.Turn, item.Doubled, item.Step))];

    private static FireVehicleFire MainArmamentFire(GameState state, UnitInstance vehicle, BoardLocation at) =>
        ScenarioA1FireEligibility.VehicleFirer(new VehicleFirerFacts(vehicle.Id, vehicle.Definition?.Definition, at.ToString(), CrewExposed(vehicle),
            Is(vehicle, Conditions.Motion), state.Movement is { Vehicle: true, Started: true, Stopped: false } moving && moving.Movers.Contains(vehicle.Id),
            Is(vehicle, Conditions.Pinned),
            Is(vehicle, Conditions.Stunned), Is(vehicle, Conditions.Recalled), Is(vehicle, Conditions.Shocked), Is(vehicle, Conditions.UnconfirmedKill),
            Is(vehicle, Conditions.StunRecovery), Is(vehicle, Conditions.Malfunctioned),
            Fired(vehicle), Is(vehicle, Conditions.FirstFire), Is(vehicle, Conditions.BoundingFire),
            state.OrdnanceShots.Any(item => item.Gun == vehicle.Id && item.RateOfFireKept))
        {
            ArmorLeadership = ArmorLeadership(vehicle),
        }) with
        {
            // A21.22 (pass 65, ruling R65.22): a Temporary crew's captured-weapon penalty, from the state the re-crewing record made.
            CapturedPenalty = CapturedPenalty(vehicle),
        };

    /// <summary>A21.22 (pass 65, ruling R65.22): the captured-weapon penalty of a vehicle's Temporary crew, as a multiple; null for none.</summary>
    public static int? CapturedPenalty(UnitInstance vehicle)
    {
        ArgumentNullException.ThrowIfNull(vehicle);
        return vehicle.TemporaryCrew is { } crew ? ScenarioA1CapturedVehicleRules.WeaponPenalty(crew.Manning, crew.Captured) : null;
    }

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
        return ScenarioA1FireEligibility.Residual(new ResidualFacts(here, AssaultOf(state),
            [.. units.Select(unit => new LocationUnitFacts(unit.Id, unit.Status == InstanceStatus.Active, unit.Side, IsVehicle(unit), unit.Definition is not null,
                Is(unit, Conditions.Captured), movement!.Movers.Contains(unit.Id)))],
            state.PhasingSide, here ? state.Side(state.PhasingSide)?.Elr : null, here && state.NoQuarter.Contains(state.PhasingSide, StringComparer.Ordinal),
            state.ScenarioMonth, fp, target.ToString(),
            id => Target(state, units.First(unit => unit.Id == id), target), id => Vehicle(units.First(unit => unit.Id == id), target)));
    }

    /// <summary>
    /// The state's part of a Canister shot's attack on one Location (C8.4, A7.4; pass 55, ruling R55.9), read from the state: no firers, the enemy
    /// units there as targets and its vehicles on the Vehicle line; the map facts and the Canister fact are the planner's.
    /// </summary>
    public static (FireAttack? Attack, string? Reason) CanisterFromState(GameState state, string gunId, BoardLocation target)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(target);
        var side = state.Find(gunId) switch
        {
            UnitInstance { Status: InstanceStatus.Active } vehicle => vehicle.Side,
            EquipmentInstance { Status: InstanceStatus.Active, Holding: { } holding } => state.Unit(holding.Holder)?.Side,
            _ => null,
        };
        if (side is null || state.Location(gunId)?.Location is not { } from)
        {
            return (null, ScenarioA1OrdnanceEligibility.GunRefusal(gunId));
        }

        UnitInstance[] units = [.. state.At(target).OfType<UnitInstance>()];
        if (units.Any(unit => unit.Status == InstanceStatus.Active && unit.Side != side && unit.Definition is null && unit.Kind != UnitKinds.Dummy))
        {
            return (null, ScenarioA1OrdnanceEligibility.TargetOutsideCatalogRefusal);
        }

        var targetSide = state.Sides.FirstOrDefault(item => item.Id != side)?.Id;
        return WithGunCrews(state, (ScenarioA1FireEligibility.Canister(new CanisterLocationFacts(state.Phase, side == state.PhasingSide, from.ToString(), target.ToString(), side,
            [.. units.Select(unit => new LocationUnitFacts(unit.Id, unit.Status == InstanceStatus.Active, unit.Side, IsVehicle(unit), unit.Definition is not null,
                Is(unit, Conditions.Captured), false))],
            targetSide is null ? null : state.Side(targetSide)?.Elr, targetSide is not null && state.NoQuarter.Contains(targetSide, StringComparer.Ordinal), state.ScenarioMonth,
            id => Target(state, units.First(unit => unit.Id == id), target), id => Vehicle(units.First(unit => unit.Id == id), target))), null));
    }

    /// <summary>
    /// A vehicle's OVR of its Location (D7.1, D7.11; ruling R11.11), read from the state: every non-captured enemy unit there is attacked, Infantry as
    /// targets and vehicles on the Vehicle line or through their Vulnerable crews; the map facts (terrain, SMOKE, a wall crossed) are the planner's.
    /// </summary>
    public static (FireAttack? Attack, string? Reason) OverrunFromState(GameState state, string vehicleId, bool legacyVehicleBreakdown = false)
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
        var read = WithGunCrews(state, ScenarioA1FireEligibility.Overrun(new OverrunFacts(found, isVehicle, at is not null, vehicle?.Definition is not null, vehicleId, vehicle?.Definition?.Definition,
            at?.ToString() ?? string.Empty, vehicle?.Side, ok && CrewExposed(vehicle!), ok && Is(vehicle!, Conditions.Immobilized), ok && Is(vehicle!, Conditions.Bogged),
            vehicle?.Status == InstanceStatus.Wrecked, ok && Is(vehicle!, Conditions.Malfunctioned), ok && Is(vehicle!, Conditions.Disabled),
            ok && Is(vehicle!, Conditions.BmgMalfunctioned), ok && Is(vehicle!, Conditions.CmgMalfunctioned),
            [.. units.Select(unit => new LocationUnitFacts(unit.Id, unit.Status == InstanceStatus.Active, unit.Side, IsVehicle(unit), unit.Definition is not null,
                Is(unit, Conditions.Captured), false))],
            targetSide is null ? null : state.Side(targetSide)?.Elr, targetSide is not null && state.NoQuarter.Contains(targetSide, StringComparer.Ordinal), state.ScenarioMonth,
            id => Target(state, units.First(unit => unit.Id == id), at!), id => Vehicle(units.First(unit => unit.Id == id), at!),
            ok && Is(vehicle!, Conditions.StunRecovery))
        {
            // D7.16 (p. 207; pass 55, ruling R55.16): the OVRing vehicle's Armor Leader modifies its OVR DR.
            ArmorLeadership = ok ? ArmorLeadership(vehicle!) : null,
        }));
        if (legacyVehicleBreakdown || read.Attack?.Overrun is not { } overrun || vehicle is null)
        {
            return read;
        }
        var attack = read.Attack with
        {
            Overrun = overrun with
            {
                DisabledWeapons = [.. new[]
                {
                    (FireOverrunEffect.MainArmament, Conditions.Disabled),
                    (FireOverrunEffect.BowMg, Conditions.BmgDisabled),
                    (FireOverrunEffect.CoaxialMg, Conditions.CmgDisabled),
                }.Where(item => Is(vehicle, item.Item2)).Select(item => item.Item1)],
            },
            AmmunitionShortage = ScenarioA1UnitSubstitution.AmmunitionShortage(state.SpecialRules, vehicle.Side) ? true : null,
            BreakdownReduction = GamePlanner.ExtremeWinterReduction(state, vehicle.Side),
        };
        return WithVehicleBreakdown(state, attack);
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
        Dictionary<string, int>? cowerSelection = null;
        Dictionary<string, int>? shortageSelection = null;
        Dictionary<string, IReadOnlyList<int>>? checks = null;
        Dictionary<string, IReadOnlyList<int>>? leaderLoss = null;
        Dictionary<string, int>? wounds = null;
        Dictionary<string, IReadOnlyList<int>>? heat = null;
        Dictionary<string, IReadOnlyList<int>>? berserk = null;
        Dictionary<string, IReadOnlyList<int>>? crewChecks = null;
        Dictionary<string, int>? unlikelyKill = null;
        int? molCheck = null;
        Dictionary<string, int>? weaponDestruction = null;
        Dictionary<string, IReadOnlyList<int>>? prcSurvival = null;
        Dictionary<string, IReadOnlyList<int>>? vehicleSurvival = null;
        var named = new Dictionary<string, IReadOnlyList<int>>(StringComparer.Ordinal);

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
                case "cowerSelection":
                    if (!Select(ref cowerSelection, unit, roll))
                    {
                        return null;
                    }

                    break;

                // A19.131 (pass 72, ruling R72.11): the Random Selection of the MMC an Original 12 under Ammunition Shortage Replaces.
                case "shortageSelection":
                    if (!Select(ref shortageSelection, unit, roll))
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

                // Pass 60 (rulings R60.15, R60.16): a DC's Position DR and the checks of a named AFV's crew.
                case "position" or "crewSurvival" or "taskCheck" or "collateralCheck" when roll.Count == 2 && unit.Length == 0:
                    named[kind] = roll.Values;
                    break;

                // A9.74 (pass 60, ruling R60.17): a weapon's Random SW/Gun Destruction dr.
                case "weaponDestruction" when roll.Count == 1 && unit.Length > 0:
                    (weaponDestruction ??= new(StringComparer.Ordinal))[unit] = roll.Values[0];
                    break;

                // D6.9 (pass 65, ruling R65.9): a Passenger's or Rider's Survival DR.
                case "prcSurvival" when roll.Count == 2 && unit.Length > 0:
                    (prcSurvival ??= new(StringComparer.Ordinal))[unit] = roll.Values;
                    break;

                // D5.6, D9.2 (pass 65, ruling R65.21): the Crew Survival DR of an armed vehicle the Vehicle line eliminated.
                case "vehicleSurvival" when roll.Count == 2 && unit.Length > 0:
                    (vehicleSurvival ??= new(StringComparer.Ordinal))[unit] = roll.Values;
                    break;
                default:
                    return null;
            }
        }

        return new FireRolls(ift, selection, checks, leaderLoss, wounds)
        {
            WeaponSelection = weaponSelection,
            FirerSelection = firerSelection,
            CowerSelection = cowerSelection,
            ShortageSelection = shortageSelection,
            HeatOfBattle = heat,
            BerserkChecks = berserk,
            CrewChecks = crewChecks,
            UnlikelyKill = unlikelyKill,
            MolCheck = molCheck,
            Position = named.GetValueOrDefault("position"),
            CrewSurvival = named.GetValueOrDefault("crewSurvival"),
            TaskCheck = named.GetValueOrDefault("taskCheck"),
            CollateralCheck = named.GetValueOrDefault("collateralCheck"),
            WeaponDestruction = weaponDestruction,
            PrcSurvival = prcSurvival,
            VehicleSurvival = vehicleSurvival,
        };
    }

    /// <summary>
    /// An attack that carries A9.74 (pass 60, rulings R60.3, R60.17), with the SW and Guns each target unit holds read from the state, as far as Rules
    /// gives the rule's scope. The planner gives it to every attack but an OVR's (whose A9.74 is backlog); replay reads the weapons again only for a
    /// record that carries the rule.
    /// </summary>
    public static FireAttack WithRandomDestruction(GameState state, FireAttack attack)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(attack);
        if (!ScenarioA1GunDestruction.Checks(null, attack.Overrun is not null))
        {
            return attack;
        }

        return attack with
        {
            RandomDestruction = true,
            Targets = attack.Targets is null ? null : [.. attack.Targets.Select(target => target with
            {
                HeldWeapons = state.Equipment.Where(item => item.Status == InstanceStatus.Active && item.Holding is { } holding && holding.Holder == target.UnitId
                        && ScenarioA1GunDestruction.Checks(holding.Role switch { HoldingRole.Possessed => "possessed", HoldingRole.Manned => "manned", _ => "towed" }, false))
                    .OrderBy(item => item.Id, StringComparer.Ordinal)
                    .Select(item => new FireHeldWeapon(item.Id, item.Kind, item.Definition?.Definition)
                    {
                        Malfunctioned = Is(item, Conditions.Malfunctioned) ? true : null
                    })
                    .ToArray() is { Length: > 0 } held ? held : null,
            })],
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
            GreenInexperienced(state, unit), Flamethrowers(state, unit), UnderscoredElr(state, unit)));

    /// <summary>
    /// The SSR ELR of a MMC with an underscored Morale Factor (A19.132; pass 72, ruling R72.13): Rules reads it from the game's special rules, the unit's side
    /// and OB group, and its definition; null without an <c>underscored-elr:</c> SSR, as in every game before the pass.
    /// </summary>
    internal static int? UnderscoredElr(GameState state, UnitInstance unit) =>
        state.SpecialRules.Count == 0 ? null
            : ScenarioA1UnitSubstitution.UnderscoredElr(state.SpecialRules, unit.Side, unit.Group,
                unit.Definition is { } reference ? CatalogReference.Value.Definitions.GetValueOrDefault(reference.Definition) : null);

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
        var mainArmament = state.Phase == "dfph" && unit.Definition is { } named
            && CatalogReference.Value.Definitions.GetValueOrDefault(named.Definition) is { IsVehicle: true } definition
            && (definition.AntiAircraftMg is not null || definition.MainArmament == "mg") && state.Location(unit.Id)?.Location is { } at
            ? ScenarioA1FinalFireCalculator.VehicleFacts(VehicleFire(state, unit, at, currentFinalFire: true)) : null;
        // Rules decides it (pass 32.c); the three state reads are made where the old body made them.
        return ScenarioA1FireEligibility.FireSpent(new FireSpentFacts(state.Phase, Fired(unit), Is(unit, Conditions.FirstFire), IsVehicle(unit),
            // C2.24, D3.5: a vehicle's MG fires again this phase only on the Multiple ROF its last shot kept.
            () => state.OrdnanceShots.Any(item => item.Gun == unit.Id && item.RateOfFireKept),
            // A7.351 (table player, pass 9b): a squad whose only fire is one SW still fires its inherent FP, and an unfired ATR fires like a MG; so does an
            // unfired FT (A22.3; backlog pass 15).
            () => state.SupportWeaponUses.Any(item => item.Unit == unit.Id),
            () => state.Equipment.Any(item => item.Status == InstanceStatus.Active && item.Holding is { Role: HoldingRole.Possessed } holding
                && holding.Holder == unit.Id && (item.Kind is "asl:mg" or "asl:ft" || (item.Definition is { } weapon && LiveOrdnance.LatwType(weapon.Definition) == "atr"))
                && !Fired(item) && !Is(item, Conditions.Malfunctioned)))
        {
            VehicleMgUnfired = () => VehicleMgs(state, unit).Any(item => item.Bar is null),
        }, mainArmament);
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

    /// <summary>
    /// Replays a morale check made outside an attack (pass 70, ruling R70.4): each unit it names is active in its Location with the facts the state gives (the
    /// map reads, the vehicle a unit rides, and the leaders lost before it are taken as recorded), its ELR is its side's, and the Morale Check member gives
    /// the recorded resolution from the facts and the recorded dice.
    /// </summary>
    public string? VerifyMoraleCheck(GameState state, MoraleCheckResolved check, IReadOnlyDictionary<string, DiceRolled> rolls)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(check);
        ArgumentNullException.ThrowIfNull(rolls);
        MoraleCheckFacts? recorded;
        try
        {
            recorded = check.Facts.Deserialize<MoraleCheckFacts>(LiveFire.StrictJson);
        }
        catch (JsonException exception)
        {
            return "The morale check record's facts cannot be read: " + exception.Message;
        }

        if (recorded?.Subjects is null || recorded.Rolls is not null || !BoardLocation.TryParse(check.Location, out var at))
        {
            return "The morale check record's facts are incomplete.";
        }

        FireTarget[] named = [.. recorded.Subjects, .. recorded.Others ?? []];
        if (check.Cause == "rider-bail-out" && recorded.Check != MoraleCheckFacts.Nmc)
        {
            return "A Rider Bail Out requires an NMC.";
        }
        if (!named.Select(item => item.UnitId).SequenceEqual(check.Units))
        {
            return "The morale check record names other units than its facts.";
        }

        string? side = null;
        foreach (var target in named)
        {
            if (state.Unit(target.UnitId ?? string.Empty) is not { Status: InstanceStatus.Active } unit || state.Location(unit.Id)?.Location != at)
            {
                return $"The morale check record names {target.UnitId}, which is not in {at}.";
            }

            side ??= unit.Side;
            var aboard = target.Aboard;
            if (check.Cause == "rider-bail-out")
            {
                if (unit.Position is not ContainedPosition { Role: ContainmentRole.Rider } rider
                    || state.Unit(rider.Container) is not { Status: InstanceStatus.Active } vehicle)
                {
                    return "A Rider Bail Out check must name an actual Rider and its vehicle.";
                }
                aboard = new FireAboard(vehicle.Id, FirePrc.Rider, ScenarioA1PassengerActionRules.VehicleMoved(false, state.Phase == "mph",
                    state.MovedVehicles.Contains(vehicle.Id, StringComparer.Ordinal), GameState.Condition(vehicle, Conditions.Motion) == ConditionState.True), null);
            }
            var expected = LiveFire.Target(state, unit, at) with
            {
                Unarmed = GameState.Condition(unit, Conditions.Unarmed) == ConditionState.True ? true : null,
                Encircled = state.Encircled(unit) ? true : target.Encircled,
                Aboard = aboard,
                SeenByGoodOrderEnemy = target.SeenByGoodOrderEnemy,
                KnownEnemyInLos = target.KnownEnemyInLos,
                Captors = target.Captors,
            };

            // Pass 72 (rulings R72.13, R72.19): a record stamped with the pass has every unit's SSR ELR as an underscored MMC checked against the state; one made
            // before the pass carries none and resolves as it did.
            if (recorded.UnitSubstitution != true && target.UnderscoredElr is null)
            {
                expected = expected with
                {
                    UnderscoredElr = null
                };
            }

            if (unit.Side != side || JsonSerializer.Serialize(expected, LiveFire.Json) != JsonSerializer.Serialize(target, LiveFire.Json))
            {
                return $"The morale check record's facts of {unit.Id} do not match the game state.";
            }
        }

        if (side is not null && recorded.TargetSideElr != state.Side(side)?.Elr)
        {
            return "The morale check record's ELR is not its side's.";
        }

        var dice = new FireRolls(null, null, null, null, null);
        foreach (var (key, id) in check.Rolls)
        {
            var kind = key.Split(':')[0];
            if (!rolls.TryGetValue(id, out var roll) || roll.Sides != 6 || kind is not ("checks" or "leaderLoss" or "heatOfBattle" or "berserkCheck" or "woundSeverity")
                || roll.Count != (kind == "woundSeverity" ? 1 : 2) || !key.Contains(':', StringComparison.Ordinal))
            {
                return "The morale check record names a roll of the wrong shape.";
            }

            dice = GamePlanner.ApplyFireRoll(dice, key, roll.Values);
        }

        var resolution = ScenarioA1MoraleCheck.Resolve(recorded with
        {
            Rolls = dice
        }, reference);
        return resolution.Disposition != FireResolution.Resolved
            ? "The morale check record's facts and rolls do not resolve: " + string.Join("; ", resolution.Reasons)
            : !JsonElement.DeepEquals(JsonSerializer.SerializeToElement(resolution, LiveFire.Json), check.Resolution)
                ? "The morale check record's resolution differs from what its facts and rolls give."
                : null;
    }

    public string? Verify(GameState state, FireResolved fire, IReadOnlyDictionary<string, DiceRolled> rolls)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(fire);
        ArgumentNullException.ThrowIfNull(rolls);
        if (fire.SmokeWiringVersion is not (null or 1 or 2))
        {
            return "Unknown SMOKE wiring version.";
        }
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
        if (recorded.FinalFireVersion is not (null or 1))
        {
            return "Unknown Final Fire version.";
        }
        FireLaneWreckFacts? verifiedWrecks = null;
        if (recorded.FireLaneWrecks is { } wrecks)
        {
            if (recorded.FireLane != true || recorded.FireKind != ScenarioA1FireCalculator.ResidualFire
                || ScenarioA1FireFollowUps.FireLaneWreckHindrance(wrecks) is null)
            {
                return "The Fire Lane wreck facts are malformed or attached to another kind of attack.";
            }
            var lanes = state.FireLanes.Where(lane => lane.Weapon == wrecks.LaneWeaponId).ToArray();
            if (lanes.Length != 1 || state.Location(lanes[0].Operator)?.Location.ToString() != wrecks.SourceLocation
                || !lanes[0].Entries.Any(entry => entry.Location == target && entry.Fp == recorded.ResidualFp))
            {
                return "The Fire Lane wreck facts do not identify the active lane, its source, and its target entry.";
            }
            foreach (var candidate in wrecks.Candidates)
            {
                if (state.Unit(candidate.WreckId) is not { Status: InstanceStatus.Wrecked } wreck || !GamePlanner.IsBurning(state, wreck)
                    || state.Location(wreck.Id)?.Location.ToString() != candidate.Location
                    || GamePlanner.Standing(state, wreck) != candidate.Standing
                    || (wreck.Straddling?.ToString() ?? string.Empty) != candidate.BypassOtherHex)
                {
                    return "The Fire Lane wreck facts disagree with a wreck's current identity, Location, burning state, or movement facts.";
                }
            }
            // As with the other map facts, geometry is recorded. Identity and every state-owned fact above are verified.
            verifiedWrecks = wrecks;
        }
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
            (expected, reason) = LiveFire.OverrunFromState(state, recorded.Overrun?.VehicleId ?? string.Empty, recorded.VehicleBreakdown is null);
        }
        else if (recorded.Canister is { } canister)
        {
            // C8.4 (pass 55, ruling R55.9): a Canister shot's attack on one Location is read from the Gun and the Location's units. What halves
            // its FP and its Case A are the shot's own facts, recorded with the attack; the FP is the Gun Size's.
            (expected, reason) = LiveFire.CanisterFromState(state, canister.GunId ?? string.Empty, target);
            var caliber = state.Find(canister.GunId ?? string.Empty) switch
            {
                UnitInstance { Definition: { } tank } => LiveOrdnance.CaliberOf(tank.Definition),
                EquipmentInstance { Definition: { } gun } => LiveOrdnance.CaliberOf(gun.Definition),
                _ => null,
            };
            expected = expected is null ? null : expected with
            {
                Canister = canister with
                {
                    Firepower = caliber is { } size ? ScenarioA1CanisterCalculator.Firepower(size) : null,
                },
            };
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

            // D3.5 (pass 50): the BMG and CMG a vehicle's record names.
            if (recorded.VehicleFire is { VehicleId: { } armed, Weapons: { Count: > 0 } vehicleWeapons })
            {
                weapons = new Dictionary<string, IReadOnlyList<string>>(StringComparer.Ordinal) { [armed] = [.. vehicleWeapons.Select(item => item.Weapon ?? string.Empty)] };
            }

            // Pass 65 (ruling R65.14): the (un)loaders are targets only of an attack whose record names them; an older one is read as it was made. So is a
            // wreck (ruling R65.23).
            (expected, reason) = LiveFire.FromState(state, fire.Firers, directors, target, weapons, alone, partners is { Count: > 0 } ? partners : null, mol,
                recorded.Embarking is not null, recorded.Vehicles?.Any(item => item.Wreck == true) == true, recorded.VehicleBreakdown is null,
                recorded.FinalFireVersion is null);
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

                    // A7.72 (pass 45): a firer cut off in an upper building level is Encircled by a read of the map, recorded with the attack.
                    Encircled = fact.Encircled ?? record.Encircled,

                    // Pass 45 (ruling R45.4): a firer's berserk state is a fact of attacks planned from the pass on; an older record has none.
                    Berserk = recorded.CoweringMarksAndWeaponResidual == true ? fact.Berserk : record.Berserk,

                    // Pass 72 (ruling R72.13): a firer's SSR ELR as an underscored MMC is checked against the state for an attack planned from the pass on.
                    UnderscoredElr = recorded.UnitSubstitution == true ? fact.UnderscoredElr : record.UnderscoredElr,

                    // Pass 65 (ruling R65.10): a Passenger's Mounted Fire is the planner's read of its vehicle, recorded with the attack.
                    Mounted = record.Mounted,
                })],
            // A7.7 (ruling R12.11): the Encirclement an attack completes is read before the attack, and recorded with it.
            Targets = recorded.Targets is null || expected.Targets is null ? expected.Targets
                : [.. expected.Targets.Zip(recorded.Targets, (fact, record) => fact with
                {
                    KnownEnemyInLos = record.KnownEnemyInLos,
                    Captors = record.Captors,
                    Encircled = fact.Encircled ?? record.Encircled,

                    // Pass 70 (ruling R70.7): whether a target is Unarmed is written with the attack and taken from the record; an older one has none.
                    Unarmed = record.Unarmed,

                    // Pass 71 (ruling R71.14): whether a wound pins the target (A17.2) is written with the attack and taken from the record; an older one has none.
                    // A recorded pin the state does not give is refused (R3's finding 2).
                    WoundPins = LiveFire.RecordedWoundPins(state, fact.UnitId, record.WoundPins),

                    // Pass 72 (ruling R72.13): so is a target's.
                    UnderscoredElr = recorded.UnitSubstitution == true ? fact.UnderscoredElr : record.UnderscoredElr,
                })],
            // D3.52, C5.11 (pass 50): the hexspines a vehicle's MG must turn its CA, and its hex's terrain, are map reads recorded with the attack.
            VehicleFire = expected.VehicleFire is { Weapons: { } stateWeapons } armedVehicle && recorded.VehicleFire is { Weapons: { } recordedWeapons } recordedVehicle
                && stateWeapons.Count == recordedWeapons.Count
                ? armedVehicle with
                {
                    InWoodsOrBuilding = recordedVehicle.InWoodsOrBuilding,
                    Weapons = [.. stateWeapons.Zip(recordedWeapons, (fact, record) => fact with { Turn = record.Turn })],
                }
                : expected.VehicleFire,
            FirerLocationsAdjacent = recorded.FirerLocationsAdjacent,
            WithinSubsequentFirstFireRange = recorded.WithinSubsequentFirstFireRange,
            Companions = recorded.Companions is null ? null : expected.Companions,

            // A7.53 (pass 45): whether the leader directed these MGs earlier in the phase is the planner's read of the records, recorded with the attack.
            Director = expected.Director is null || recorded.Director?.DirectedTheseMgs != true ? expected.Director : expected.Director with
            {
                DirectedTheseMgs = true
            },

            // The owners' answers are declared, and the projector checks them against the choices made (ruling R5.8).
            Choices = recorded.Choices,
            AfvCover = GamePlanner.CoverAt(state, target, expected.Targets?.Select(item => state.Unit(item.UnitId!)?.Side).FirstOrDefault(side => side is not null)),

            // C11 (ruling R8.3): the Gun in the target Location, its Emplacement and gunshield, is a map read recorded with the attack.
            GunTarget = recorded.GunTarget,

            // Pass 65 (ruling R65.28): the targets that man a Gun, as the record carries them; a record made before the pass names none, and its crews
            // were each a Gun's crew alone with the Gun named.
            GunCrews = recorded.GunCrews,

            // Pass 60 (ruling R60.15): the AFV a FT, MOL, or DC names is read from the state, its Target Facings from the record (a map read); a record
            // made before the pass names none.
            TargetVehicle = recorded.TargetVehicle is { VehicleId: { } namedId } recordedNamed && state.Unit(namedId) is { } namedUnit
                ? ScenarioA1ArmorLeaders.AsRecorded(LiveFire.NamedVehicle(state, namedUnit) with
                {
                    HullFacing = recordedNamed.HullFacing,
                    TurretFacing = recordedNamed.TurretFacing,

                    // Pass 65 (ruling R65.5): its PRC as the record carries them.
                    Prc = recordedNamed.Prc,
                }, recordedNamed)
                : null,

            // Pass 65 (ruling R65.5): each vehicle's PRC as the record carries them.
            Vehicles = LiveFire.WithRecordedPrc(expected.Vehicles, recorded.Vehicles),

            // Pass 65 (ruling R65.13): the Passengers an OVR's declaration named, as the record carries them; an older OVR names none.
            Overrun = expected.Overrun is { } overrunFacts && recorded.Overrun is { } overrunRecord
                ? overrunFacts with
                {
                    Passengers = overrunRecord.Passengers
                }
                : expected.Overrun,

            // Backlog pass 10 (rulings R10.4 to R10.8, R10.13): the levels, the wall or hedge TEM with Wall Advantage, Height Advantage, and
            // Hazardous Movement are map reads recorded with the attack; a Snap Shot is the DEFENDER's declaration.
            TargetLevelAbove = recorded.TargetLevelAbove,
            HexsideTem = recorded.HexsideTem,
            HeightAdvantage = recorded.HeightAdvantage,
            HazardousMovement = recorded.HazardousMovement,
            SnapShot = recorded.SnapShot,

            // Pass 40 (A5.12, A5.131): the stacks' penalties are the planner's read of the Locations, recorded with the attack, so a record made
            // before the IFT took them replays as it was resolved.
            FirerOverstack = recorded.FirerOverstack,
            TargetOverstack = recorded.TargetOverstack,

            // Pass 40 (A4.63): whether the fire is at a Dash is the planner's read of the two sides of the road, recorded with the attack.
            Dash = recorded.Dash,

            // Backlog pass 16 (rulings R16.2, R16.3, R16.11 to R16.14): the night and weather facts are the planner's reading of the map and the SSRs.
            LowVisibilityDrm = recorded.LowVisibilityDrm,
            BeyondNvr = recorded.BeyondNvr,
            CushionedOpenGround = recorded.CushionedOpenGround,
            BreakdownReduction = recorded.BreakdownReduction,

            // Pass 31 (ruling R31.3): how a pinned firer's MG fires is recorded with the attack, so an older record replays as it was resolved.
            PinnedMgAreaFire = recorded.PinnedMgAreaFire,

            // Pass 45 (ruling R45.4): so are the Cowering marks and the Residual FP by weapon.
            CoweringMarksAndWeaponResidual = recorded.CoweringMarksAndWeaponResidual,
            TargetsInMelee = recorded.TargetsInMelee,

            // Pass 72 (rulings R72.9, R72.13): and the Inexperienced MG B# and the underscored units' SSR ELR.
            UnitSubstitution = recorded.UnitSubstitution,

            // Pass 72 (rulings R72.11, R72.12): a record that carries the pass's stamp has its Ammunition Shortage checked against the state; an older one
            // carries none and resolves as it did.
            AmmunitionShortage = recorded.UnitSubstitution == true ? expected.AmmunitionShortage : recorded.AmmunitionShortage,

            // Backlog pass 12 (rulings R12.6, R12.7): Spraying Fire is the firer's declaration; a Fire Lane's Residual FP is the lane's in the state.
            SprayingFire = recorded.SprayingFire,
            SprayShare = recorded.SprayShare,
            FireLane = recorded.FireLane == true && state.FireLanes.Any(lane => lane.Entries.Any(entry => entry.Location == target && entry.Fp == recorded.ResidualFp))
                ? true : null,
            FireLaneWrecks = verifiedWrecks,
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
        // A9.74 (pass 60, rulings R60.3, R60.17): a record that carries the rule resolves with the weapons its targets hold read from the state (the
        // record never names them; boundary review, pass 60); an older record carries neither, and its KIA destroys nothing.
        if (recorded.RandomDestruction == true)
        {
            merged = LiveFire.WithRandomDestruction(state, merged);
            recorded = LiveFire.WithRandomDestruction(state, recorded);
        }

        // D3.41, D3.42 (pass 55, rulings R55.15, R55.17): a record keeps a crew's Morale Level only when the attack made that crew check, so one
        // it leaves out is left out of the comparison; the checks it holds are held to the state's below.
        var stateVehicles = merged.Vehicles;
        var stateNamed = recorded.TargetVehicle?.VehicleId is { } aimedId && state.Unit(aimedId) is { } aimedUnit ? LiveFire.NamedVehicle(state, aimedUnit) : null;
        merged = merged with
        {
            Vehicles = ScenarioA1ArmorLeaders.AsRecorded(merged.Vehicles, recorded.Vehicles),
        };
        if (JsonSerializer.Serialize(merged, LiveFire.Json) != JsonSerializer.Serialize(recorded, LiveFire.Json))
        {
            return "The fire record's facts do not match the game state.";
        }

        // D3.52, D3.12 (pass 50): the record turns the turret exactly the hexspines its CMG's facts say, and no other record turns one.
        var cmgTurn = recorded.VehicleFire?.Weapons?.FirstOrDefault(item => item.Weapon == FireVehicleWeapon.CoaxialMg)?.Turn ?? 0;
        var tcaNow = recorded.VehicleFire?.VehicleId is { } turning && state.Unit(turning) is { } turningVehicle ? LiveOrdnance.TurretFacing(state, turningVehicle) : null;
        if ((cmgTurn > 0) != fire.TurretFacing.HasValue || ScenarioA1OrdnanceEligibility.HexspinesTurned((int?)fire.TurretFacing, (int?)tcaNow) != cmgTurn)
        {
            return "The fire record does not turn the turret by the hexspines its CMG's facts give.";
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
                : ScenarioA1ArmorLeaders.RecordRefusal(stateVehicles, recorded.Vehicles, ScenarioA1ArmorLeaders.Checked(resolution))
                    ?? ScenarioA1ArmorLeaders.RecordRefusal(stateNamed, recorded.TargetVehicle, resolution);
    }
}
