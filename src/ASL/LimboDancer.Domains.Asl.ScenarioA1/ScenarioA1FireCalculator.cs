using System.Globalization;
using System.Text.RegularExpressions;

namespace LimboDancer.Domains.Asl.ScenarioA1;

/// <summary>
/// Resolves a declared fire attack under the reviewed Fire case matrix (unit step 17, revised at steps 18 and 19 to 23;
/// Scenario A1 Fire Review 2026-09-26). It is a pure function of the attack and the pinned reference data: it rolls
/// nothing and changes nothing. Facts outside the reviewed scope abstain; facts the review leaves undecided, and missing
/// rolls, are Indeterminate.
/// </summary>
public static class ScenarioA1FireCalculator
{
    public const string FirstFire = "first-fire";
    public const string SubsequentFirstFire = "subsequent-first-fire";
    public const string FinalProtectiveFire = "final-protective-fire";
    public const string ResidualFire = "residual-fp";

    /// <summary>The Residual FP counters (A8.2: at most 12; A7.372: the highest counter at most half the FP used).</summary>
    public static readonly int[] ResidualCounters = [1, 2, 4, 6, 8, 12];

    private static readonly string[] MovementKinds = [FirstFire, SubsequentFirstFire, FinalProtectiveFire, ResidualFire];

    // The Dummy of a concealment stack (A12.11): no unit, no printed values.
    private static readonly FireDefinition DummyDefinition = new("dummy", "asl:dummy", string.Empty, null, null, null, null, null, null, null);

    public static FireResolution Resolve(FireAttack attack, ScenarioA1FireReference reference)
    {
        ArgumentNullException.ThrowIfNull(attack);
        ArgumentNullException.ThrowIfNull(reference);
        var missing = Missing(attack);
        if (missing.Count != 0)
        {
            return Refused(FireResolution.Indeterminate, missing);
        }

        var outside = Outside(attack, reference);
        if (outside.Count != 0)
        {
            return Refused(FireResolution.Abstained, outside);
        }

        var undecided = Undecided(attack, reference);
        if (undecided.Count != 0)
        {
            return Refused(FireResolution.Indeterminate, undecided);
        }

        return new Resolution(attack, reference).Run();
    }

    /// <summary>
    /// The reasons an attack cannot be committed before any roll (Fire in Live Play, unit step 18): empty when every
    /// outcome the dice can reach is decided. Every reason the package gives other than a missing roll depends on the
    /// facts alone, so the check is exact: the facts must stop only at the missing IFT roll, the target side's ELR (and
    /// the firing side's, for FPF) must be declared, a concealed firer or director must have a Good Order target within
    /// 16 hexes, and every unit a Reduction or Replacement can produce must have its Morale Levels.
    /// </summary>
    public static IReadOnlyList<string> Precheck(FireAttack attack, ScenarioA1FireReference reference)
    {
        ArgumentNullException.ThrowIfNull(attack);
        ArgumentNullException.ThrowIfNull(reference);
        var first = Resolve(attack with
        {
            Rolls = new FireRolls(null, null, null, null)
        }, reference);
        if (first.Disposition != FireResolution.Indeterminate || first.Reasons is not ["asl.a1.fire.roll-missing:attack"])
        {
            return first.Reasons;
        }

        var reasons = new List<string>();
        if (attack.TargetSideElr is null && attack.Targets!.Any(item => item.Dummy != true))
        {
            reasons.Add("asl.a1.fire.elr-undecided:elr-undeclared");
        }

        if (attack.FireKind == FinalProtectiveFire && attack.FiringSideElr is null)
        {
            reasons.Add("asl.a1.fire.elr-undecided:firing-side-elr-undeclared");
        }

        var concealed = attack.Firers?.Any(item => item.Concealed == true) == true || attack.Director?.Concealed == true
            || attack.OtherDirectors?.Any(item => item.Concealed == true) == true;
        if (concealed && !(attack.Firers!.All(item => RangeOf(attack, item) <= 16) && attack.Targets!.Any(item => item.Broken == false && item.Dummy != true)))
        {
            reasons.Add("asl.a1.fire.concealment-unreviewed:firer-concealment");
        }

        // A15.44, A15.5: a unit subject to Heat of Battle needs the planner's reads of its LOS and ADJACENT captors, since any MC
        // it takes can reach a Berserk or Surrender result.
        foreach (var (target, index) in attack.Targets!.Select((item, index) => (item, index))
            .Where(pair => pair.item.Dummy != true
                && ScenarioA1HeatOfBattle.Subject(reference.Definitions[pair.item.DefinitionId!], pair.item.Heroic == true, pair.item.Berserk == true)))
        {
            if (target.KnownEnemyInLos is null)
            {
                reasons.Add($"asl.a1.fire.fact-missing:targets[{index}].knownEnemyInLos");
            }

            if (target.Captors is null)
            {
                reasons.Add($"asl.a1.fire.fact-missing:targets[{index}].captors");
            }
        }

        if (attack.FireKind == FinalProtectiveFire)
        {
            foreach (var (firer, index) in attack.Firers!.Select((item, index) => (item, index))
                .Where(pair => ScenarioA1HeatOfBattle.Subject(reference.Definitions[pair.item.DefinitionId!], heroic: false)))
            {
                if (firer.KnownEnemyInLos is null || firer.Captors is null)
                {
                    reasons.Add($"asl.a1.fire.fact-missing:firers[{index}].knownEnemyInLos-or-captors");
                }
            }
        }

        var seen = new HashSet<string>(StringComparer.Ordinal);
        var checkers = attack.Targets!.Where(item => item.Dummy != true).Select(item => item.DefinitionId!);
        if (attack.FireKind == FinalProtectiveFire)
        {
            checkers = checkers.Concat(attack.Firers!.Select(item => item.DefinitionId!));
        }

        var pending = new Stack<string>(checkers);
        while (pending.TryPop(out var id))
        {
            if (!seen.Add(id))
            {
                continue;
            }

            // A hero never breaks, so his Morale Levels are his two sides' (A15.2); every other unit needs its broken one.
            if (!reference.Definitions.TryGetValue(id, out var definition) || definition.Morale is null
                || (definition.IsHero ? definition.WoundedMorale is null : definition.BrokenMorale is null)
                || (definition.IsLeader && definition.Leadership is null))
            {
                reasons.Add("asl.a1.fire.definition-incomplete:" + id);
                continue;
            }

            // Every unit the attack can turn this one into: its HS, its Replacement, and its Battle Hardened unit (A15.3).
            foreach (var next in new[] { ScenarioA1FireReference.HalfSquadOf(id), ScenarioA1FireReference.ReplacementOf(id),
                ScenarioA1FireReference.HardenedOf(id) }.OfType<string>())
            {
                pending.Push(next);
            }
        }

        return reasons;
    }

    private static FireResolution Refused(string disposition, IReadOnlyList<string> reasons) =>
        new(disposition, reasons, null, [], [], null, []);

    private static bool IsMovementFire(FireAttack attack) => attack.FireKind is not null;

    private static bool IsMultiLocation(FireAttack attack) =>
        attack.Firers is { Count: > 0 } firers && firers.Select(item => item.LocationId).Distinct(StringComparer.Ordinal).Count() > 1;

    private static int? RangeOf(FireAttack attack, FireFirer firer) => firer.Range ?? attack.Range;

    private static bool? SameLevelOf(FireAttack attack, FireFirer firer) => firer.SameLevel ?? attack.SameLevel;

    private static FireLos? LosOf(FireAttack attack, FireFirer firer) => firer.Los ?? attack.Los;

    private static IEnumerable<FireDirector> Directors(FireAttack attack) =>
        (attack.Director is null ? Array.Empty<FireDirector>() : [attack.Director]).Concat(attack.OtherDirectors ?? []);

    // A8.4: in the DFPh, a unit already marked First Fire fires again as Area Fire at an adjacent or same-hex target.
    private static bool IsFinalFireAgain(FireAttack attack, FireFirer firer) => attack.Phase == "DFPh" && firer.FirstFireMarked == true;

    // A9.3: a MG firing as Subsequent First Fire or FPF, or in the DFPh while marked First Fire, uses Sustained Fire.
    private static bool IsSustained(FireAttack attack, FireWeapon weapon) =>
        attack.FireKind is SubsequentFirstFire or FinalProtectiveFire || (attack.Phase == "DFPh" && weapon.FirstFireMarked == true);

    private static List<string> Missing(FireAttack attack)
    {
        var missing = new List<string>();
        void Need(object? value, string name)
        {
            if (value is null || (value is string text && string.IsNullOrWhiteSpace(text)))
            {
                missing.Add("asl.a1.fire.fact-missing:" + name);
            }
        }

        void NeedLos(FireLos? los, string at)
        {
            Need(los, at + "los");
            Need(los?.Blocked, at + "los.blocked");
            Need(los?.HindranceDrm, at + "los.hindranceDrm");
            Need(los?.HindranceAttributed, at + "los.hindranceAttributed");
            Need(los?.GrainInLos, at + "los.grainInLos");
        }

        var residual = attack.FireKind == ResidualFire;
        Need(attack.Phase, "phase");
        Need(attack.FiringSide, "firingSide");
        Need(attack.TargetLocationId, "targetLocationId");
        Need(attack.TargetTerrain, "targetTerrain");
        Need(attack.Rolls, "rolls");
        if (IsMovementFire(attack))
        {
            Need(attack.TargetMovement?.AssaultMovement, "targetMovement.assaultMovement");
        }

        if (residual)
        {
            Need(attack.ResidualFp, "residualFp");
        }
        else if (attack.OrdnanceHit is { } hit)
        {
            Need(hit.GunId, "ordnanceHit.gunId");
            Need(hit.Firepower, "ordnanceHit.firepower");
            Need(hit.CriticalHit, "ordnanceHit.criticalHit");
        }
        else if (attack.VehicleFire is { } vehicle)
        {
            Need(vehicle.VehicleId, "vehicleFire.vehicleId");
            Need(vehicle.DefinitionId, "vehicleFire.definitionId");
            Need(vehicle.LocationId, "vehicleFire.locationId");
            Need(vehicle.CrewExposed, "vehicleFire.crewExposed");
            Need(vehicle.InMotion, "vehicleFire.inMotion");
            Need(vehicle.Pinned, "vehicleFire.pinned");
            Need(vehicle.Stunned, "vehicleFire.stunned");
            Need(vehicle.StunRecovery, "vehicleFire.stunRecovery");
            Need(vehicle.Malfunctioned, "vehicleFire.malfunctioned");
            Need(vehicle.FiredThisPlayerTurn, "vehicleFire.firedThisPlayerTurn");
            Need(vehicle.RateOfFireShot, "vehicleFire.rateOfFireShot");
            Need(attack.FirerLocationId, "firerLocationId");
            Need(attack.Range, "range");
            Need(attack.SameLevel, "sameLevel");
            NeedLos(attack.Los, string.Empty);
        }
        else
        {
            Need(attack.FireGroupComplete, "fireGroupComplete");
            Need(attack.FirerLocationId, "firerLocationId");
            Need(attack.Range, "range");
            Need(attack.SameLevel, "sameLevel");
            NeedLos(attack.Los, string.Empty);
            if (attack.FireKind == SubsequentFirstFire)
            {
                Need(attack.WithinSubsequentFirstFireRange, "withinSubsequentFirstFireRange");
            }

            if (attack.Firers is null || attack.Firers.Count == 0)
            {
                missing.Add("asl.a1.fire.fact-missing:firers");
            }
            else
            {
                var multi = IsMultiLocation(attack);
                if (multi)
                {
                    Need(attack.FirerLocationsAdjacent, "firerLocationsAdjacent");
                }

                foreach (var (firer, index) in attack.Firers.Select((item, index) => (item, index)))
                {
                    var at = $"firers[{index}].";
                    Need(firer.UnitId, at + "unitId");
                    Need(firer.DefinitionId, at + "definitionId");
                    Need(firer.LocationId, at + "locationId");
                    Need(firer.Broken, at + "broken");
                    Need(firer.Pinned, at + "pinned");
                    Need(firer.Concealed, at + "concealed");
                    Need(firer.FiredThisPlayerTurn, at + "firedThisPlayerTurn");
                    Need(firer.UsesSupportWeapon, at + "usesSupportWeapon");
                    if (multi)
                    {
                        Need(firer.Range, at + "range");
                        Need(firer.SameLevel, at + "sameLevel");
                        NeedLos(firer.Los, at);
                    }

                    foreach (var (weapon, slot) in (firer.Weapons ?? []).Select((item, slot) => (item, slot)))
                    {
                        var w = $"{at}weapons[{slot}].";
                        Need(weapon.EquipmentId, w + "equipmentId");
                        Need(weapon.DefinitionId, w + "definitionId");
                        Need(weapon.Malfunctioned, w + "malfunctioned");
                        Need(weapon.FiredThisPlayerTurn, w + "firedThisPlayerTurn");
                        Need(weapon.FirstFireMarked, w + "firstFireMarked");
                    }
                }
            }

            foreach (var (director, index) in Directors(attack).Select((item, index) => (item, index)))
            {
                var at = index == 0 ? "director." : $"otherDirectors[{index - 1}].";
                Need(director.UnitId, at + "unitId");
                Need(director.DefinitionId, at + "definitionId");
                Need(director.LocationId, at + "locationId");
                Need(director.Broken, at + "broken");
                Need(director.Pinned, at + "pinned");
                Need(director.Concealed, at + "concealed");
                Need(director.DirectedThisPlayerTurn, at + "directedThisPlayerTurn");
                Need(director.Wounded, at + "wounded");
            }
        }

        foreach (var (vehicle, index) in (attack.Vehicles ?? []).Select((item, index) => (item, index)))
        {
            var at = $"vehicles[{index}].";
            Need(vehicle.VehicleId, at + "vehicleId");
            Need(vehicle.DefinitionId, at + "definitionId");
            Need(vehicle.LocationId, at + "locationId");
            Need(vehicle.CrewExposed, at + "crewExposed");
            Need(vehicle.Stunned, at + "stunned");
            Need(vehicle.StunRecovery, at + "stunRecovery");
            Need(vehicle.Immobilized, at + "immobilized");
        }

        // An empty target Location is admitted (ruling R21.1): the attack resolves against nothing.
        if (attack.Targets is null)
        {
            missing.Add("asl.a1.fire.fact-missing:targets");
        }
        else
        {
            foreach (var (target, index) in attack.Targets.Select((item, index) => (item, index)))
            {
                var at = $"targets[{index}].";
                Need(target.UnitId, at + "unitId");
                Need(target.Dummy, at + "dummy");
                if (target.Dummy != true)
                {
                    Need(target.DefinitionId, at + "definitionId");
                }

                Need(target.LocationId, at + "locationId");
                Need(target.Broken, at + "broken");
                Need(target.Pinned, at + "pinned");
                Need(target.Concealed, at + "concealed");
                Need(target.Hidden, at + "hidden");
                Need(target.Wounded, at + "wounded");
                Need(target.Disrupted, at + "disrupted");
            }
        }

        return missing;
    }

    private static List<string> Outside(FireAttack attack, ScenarioA1FireReference reference)
    {
        var outside = new List<string>();
        var phase = attack.Phase!;
        var kind = attack.FireKind;
        var phaseAdmitted = (phase, attack.FiringSide, kind) switch
        {
            ("PFPh", "phasing", null) => true,
            ("AFPh", "phasing", null) => true,
            ("DFPh", "non-phasing", null) => true,
            ("MPh", "non-phasing", { } movement) => MovementKinds.Contains(movement),
            _ => false,
        };
        if (!phaseAdmitted)
        {
            outside.Add("asl.a1.fire.phase-outside");
        }

        var targets = attack.Targets!;
        var targetDefinitions = targets.Select(item => item.Dummy == true ? DummyDefinition : reference.Definitions.GetValueOrDefault(item.DefinitionId!))
            .ToArray();

        // A7.307, A7.308, D.8B: the vehicles in the target Location are attacked with the attack's IFT DR (unit step 25): one at a time
        // (ruling R25.3 keeps a Location to one vehicle, so the A7.308 limit on vehicles affected never binds), never by Residual FP or an
        // ordnance hit here (the Vehicle Target Type is not reviewed, R24.2; a vehicle never enters Residual FP, R25.3), and only an
        // unarmored vehicle or an open-topped AFV (R25.1). Infantry sharing a Location with an AFV (its +1 TEM, D9.3) and an AFV in terrain
        // with a positive TEM (not cumulative with its crew's CE DRM, D5.31) are not reviewed (rulings R25.6, R25.9).
        var vehicles = attack.Vehicles ?? [];
        if (vehicles.Count > 1 || (vehicles.Count > 0 && (kind == ResidualFire || attack.OrdnanceHit is not null))
            || ((targets.Count > 0 || (attack.TargetTerrain is { } vehicleTerrain && ScenarioA1FireReference.Tem.TryGetValue(vehicleTerrain, out var vehicleTem) && vehicleTem > 0))
                && vehicles.Any(item => reference.Definitions.GetValueOrDefault(item.DefinitionId!) is { IsVehicle: true, Unarmored: not true }))
            || vehicles.Any(item => item.LocationId != attack.TargetLocationId
                || reference.Definitions.GetValueOrDefault(item.DefinitionId!) is not { IsVehicle: true } definition
                || (definition.Unarmored != true && definition.OpenTopped != true)))
        {
            outside.Add("asl.a1.fire.vehicle-outside");
        }

        if (attack.VehicleFire is { } vehicleFire)
        {
            VehicleFireOutside(attack, vehicleFire, reference, targetDefinitions, outside);
            return outside.Distinct(StringComparer.Ordinal).ToList();
        }

        if (kind == ResidualFire)
        {
            // A8.22: Residual FP always attacks alone.
            if (attack.Firers is { Count: > 0 } || attack.Director is not null || attack.OtherDirectors is { Count: > 0 }
                || !ResidualCounters.Contains(attack.ResidualFp!.Value))
            {
                outside.Add("asl.a1.fire.residual-outside");
            }

            if (targets.Any(item => item.LocationId != attack.TargetLocationId) || targetDefinitions.Any(item => item is null)
                || !ScenarioA1FireReference.Tem.ContainsKey(attack.TargetTerrain!))
            {
                outside.Add("asl.a1.fire.target-outside");
            }

            if (attack.Rolls is { } residualRolls && Malformed(residualRolls))
            {
                outside.Add("asl.a1.fire.roll-malformed");
            }

            return outside;
        }

        // C3.32, C.6: an ordnance hit attacks the enemy units of the target Location on the IFT column of the Gun's HE FP, with no
        // firers or leader of its own; the Ordnance package decides the hit.
        if (attack.OrdnanceHit is { } hit)
        {
            if (attack.Firers is { Count: > 0 } || attack.Director is not null || attack.OtherDirectors is { Count: > 0 } || attack.FireKind is not null
                || !ScenarioA1FireReference.ColumnFp.Contains(hit.Firepower!.Value))
            {
                outside.Add("asl.a1.fire.ordnance-hit-outside");
            }

            if (targets.Any(item => item.LocationId != attack.TargetLocationId) || targetDefinitions.Any(item => item is null)
                || !ScenarioA1FireReference.Tem.ContainsKey(attack.TargetTerrain!))
            {
                outside.Add("asl.a1.fire.target-outside");
            }

            if (targetDefinitions.Any(item => item?.Kind == "asl:crew"))
            {
                outside.Add("asl.a1.fire.crew-target-unreviewed");
            }

            if (attack.Rolls is { } hitRolls && Malformed(hitRolls))
            {
                outside.Add("asl.a1.fire.roll-malformed");
            }

            return outside;
        }

        var firers = attack.Firers!;
        var ids = firers.Select(item => item.UnitId!).Concat(targets.Select(item => item.UnitId!))
            .Concat(Directors(attack).Select(item => item.UnitId!))
            .Concat(firers.SelectMany(item => item.Weapons ?? []).Select(item => item.EquipmentId!)).ToArray();
        if (ids.Distinct(StringComparer.Ordinal).Count() != ids.Length)
        {
            outside.Add("asl.a1.fire.unit-listed-twice");
        }

        var firerDefinitions = firers.Select(item => reference.Definitions.GetValueOrDefault(item.DefinitionId!)).ToArray();
        var side = firerDefinitions.FirstOrDefault()?.Nationality;
        var locations = firers.Select(item => item.LocationId!).Distinct(StringComparer.Ordinal).ToArray();
        var multi = locations.Length > 1;

        // A7.5: a group may span Locations each ADJACENT to another of them; Residual FP never joins a group (A8.22).
        if (attack.FireGroupComplete != true || !locations.Contains(attack.FirerLocationId) || (multi && attack.FirerLocationsAdjacent != true)
            || firers.Any(item => item.Broken == true || (item.UsesSupportWeapon == true && item.Weapons is not { Count: > 0 }))
            || firerDefinitions.Any(item => item is null || !(item.IsMmc || item.IsHero) || item.Firepower is null || item.Range is null))
        {
            outside.Add("asl.a1.fire.firer-outside");
        }

        if (firerDefinitions.Any(item => item is not null && item.Nationality != side))
        {
            outside.Add("asl.a1.fire.firers-of-two-sides");
        }

        // A9.1, A7.35 to A7.352: MGs of the firer's own side, not malfunctioned; a squad fires at most two, a HS one.
        foreach (var (firer, definition) in firers.Zip(firerDefinitions))
        {
            var weapons = firer.Weapons ?? [];
            // A15.23: a hero's use of a SW is not reviewed.
            if (definition is not null && (weapons.Count > (definition.Kind == "asl:squad" ? 2 : definition.IsHero ? 0 : 1)
                || weapons.Any(weapon => reference.Definitions.GetValueOrDefault(weapon.DefinitionId!) is not { IsMg: true, Firepower: not null, Range: not null } mg
                    || mg.Nationality != definition.Nationality || weapon.Malfunctioned == true || !WeaponMayFire(attack, weapon))))
            {
                outside.Add("asl.a1.fire.weapon-outside");
            }

            if (firer.UsesInherentFp == false && weapons.Count == 0)
            {
                outside.Add("asl.a1.fire.firer-outside");
            }
        }

        if (firers.Any(item => !FirerMayFire(attack, item)))
        {
            outside.Add("asl.a1.fire.firer-already-fired");
        }

        // A8.31: FPF by units already marked Final Fire, at an ADJACENT or same-hex moving unit, undirected and not mixed
        // with other fire (the review leaves direction and mixed groups out).
        if (kind == FinalProtectiveFire && (Directors(attack).Any() || firers.Any(item => RangeOf(attack, item) > 1)))
        {
            outside.Add("asl.a1.fire.fpf-outside");
        }

        // A8.3: Subsequent First Fire within Normal Range and no farther than the closest armed Known enemy unit.
        if (kind == SubsequentFirstFire && (attack.WithinSubsequentFirstFireRange != true
            || firers.Zip(firerDefinitions).Any(pair => pair.Second is { Range: { } range } && RangeOf(attack, pair.First) > range)))
        {
            outside.Add("asl.a1.fire.subsequent-first-fire-outside");
        }

        // A8.4: a First-Fire-marked unit fires again in the DFPh only at an adjacent or same-hex target.
        if (firers.Any(item => IsFinalFireAgain(attack, item) && RangeOf(attack, item) > 1))
        {
            outside.Add("asl.a1.fire.final-fire-outside");
        }

        var directors = Directors(attack).ToArray();
        var redirect = kind is SubsequentFirstFire || firers.Any(item => IsFinalFireAgain(attack, item));
        foreach (var director in directors)
        {
            // A7.53, A7.531: an unbroken, unpinned leader of the group's side in one of its Locations; A10.7: he may direct
            // Subsequent First Fire and Final Fire again.
            var definition = reference.Definitions.GetValueOrDefault(director.DefinitionId!);
            if (definition is null || !definition.IsLeader || definition.Leadership is null || definition.Nationality != side
                || !locations.Contains(director.LocationId) || director.Broken == true || director.Pinned == true
                || (director.DirectedThisPlayerTurn == true && !redirect))
            {
                outside.Add("asl.a1.fire.director-outside");
            }
        }

        // A7.531: in a group spanning Locations, direction counts only with a directing leader in every Location; the
        // review admits direction only then.
        if (directors.Length > 0 && (directors.Select(item => item.LocationId).Distinct(StringComparer.Ordinal).Count() != directors.Length
            || (multi && !locations.All(location => directors.Any(item => item.LocationId == location)))
            || (!multi && directors.Length > 1)))
        {
            outside.Add("asl.a1.fire.director-outside");
        }

        // C11: crews (and the Guns they man) as targets are not reviewed (ruling R24.3).
        if (targetDefinitions.Any(item => item?.Kind == "asl:crew"))
        {
            outside.Add("asl.a1.fire.crew-target-unreviewed");
        }

        if (locations.Contains(attack.TargetLocationId)
            || targets.Any(item => item.LocationId != attack.TargetLocationId)
            || targetDefinitions.Any(item => item is null || (item != DummyDefinition && item.Nationality == side))
            || !ScenarioA1FireReference.Tem.ContainsKey(attack.TargetTerrain!))
        {
            outside.Add("asl.a1.fire.target-outside");
        }

        foreach (var (firer, definition) in firers.Zip(firerDefinitions))
        {
            var range = RangeOf(attack, firer);
            var inherentOut = firer.UsesInherentFp != false && definition?.Range is { } normal && range > 2 * normal;
            var weaponOut = (firer.Weapons ?? []).Any(weapon => reference.Definitions.GetValueOrDefault(weapon.DefinitionId!)?.Range is { } mgRange
                && range > 2 * mgRange);
            if (range < 1 || inherentOut || weaponOut)
            {
                outside.Add("asl.a1.fire.out-of-range");
            }

            if (LosOf(attack, firer)!.Blocked == true)
            {
                outside.Add("asl.a1.fire.los-blocked");
            }
        }

        if (attack.Rolls is { } rolls && Malformed(rolls))
        {
            outside.Add("asl.a1.fire.roll-malformed");
        }

        return outside.Distinct(StringComparer.Ordinal).ToList();
    }

    /// <summary>
    /// A vehicle's MA MG attack (ruling R25.7): the MA AAMG, fired in its side's PFPh, AFPh, or DFPh by a CE crew that is not Stunned
    /// (D1.83, D5.3, D5.34), once per Player Turn unless it kept its Multiple ROF (D3.5, C2.24), not malfunctioned (D3.7), within twice
    /// its Normal Range of eight hexes (D1.83, A7.22), with a clear LOS, at an enemy Location other than its own; no Infantry fire group
    /// or leader joins it (D3.4; D6.64 is not reviewed); never in the PFPh by a vehicle in Motion (D2.4).
    /// </summary>
    private static void VehicleFireOutside(FireAttack attack, FireVehicleFire vehicle, ScenarioA1FireReference reference,
        IReadOnlyList<FireDefinition?> targetDefinitions, List<string> outside)
    {
        var definition = reference.Definitions.GetValueOrDefault(vehicle.DefinitionId!);
        if (definition is not { IsVehicle: true, MainArmament: "aamg", AntiAircraftMg: not null } || attack.Firers is { Count: > 0 } || Directors(attack).Any()
            || vehicle.LocationId != attack.FirerLocationId || vehicle.CrewExposed != true || vehicle.Stunned == true || vehicle.Malfunctioned == true
            || (attack.Phase == "PFPh" && vehicle.InMotion == true))
        {
            outside.Add("asl.a1.fire.vehicle-fire-outside");
        }

        if (attack.FireKind is not null)
        {
            outside.Add("asl.a1.fire.phase-outside");
        }

        if (vehicle.FiredThisPlayerTurn == true && vehicle.RateOfFireShot != true)
        {
            outside.Add("asl.a1.fire.firer-already-fired");
        }

        if (attack.TargetLocationId == attack.FirerLocationId || attack.Targets!.Any(item => item.LocationId != attack.TargetLocationId)
            || targetDefinitions.Any(item => item is null || (item.Id != "dummy" && definition is not null && item.Nationality == definition.Nationality))
            || (attack.Vehicles ?? []).Any(item => definition is not null && reference.Definitions.GetValueOrDefault(item.DefinitionId!)?.Nationality == definition.Nationality)
            || !ScenarioA1FireReference.Tem.ContainsKey(attack.TargetTerrain!))
        {
            outside.Add("asl.a1.fire.target-outside");
        }

        if (attack.Range is < 1 or > 16)
        {
            outside.Add("asl.a1.fire.out-of-range");
        }

        if (attack.Los!.Blocked == true)
        {
            outside.Add("asl.a1.fire.los-blocked");
        }

        if (attack.Rolls is { } rolls && Malformed(rolls))
        {
            outside.Add("asl.a1.fire.roll-malformed");
        }
    }

    private static bool Malformed(FireRolls rolls) =>
        !Dice(rolls.Attack, allowEmpty: true)
        || rolls.RandomSelection?.Values.Any(dr => dr is < 1 or > 6) == true
        || rolls.Checks?.Values.Any(dice => !Dice(dice, allowEmpty: false)) == true
        || rolls.LeaderLoss?.Values.Any(dice => !Dice(dice, allowEmpty: false)) == true
        || rolls.WoundSeverity?.Values.Any(dr => dr is < 1 or > 6) == true
        || rolls.WeaponSelection?.Values.Any(dr => dr is < 1 or > 6) == true
        || rolls.FirerSelection?.Values.Any(dr => dr is < 1 or > 6) == true
        || rolls.HeatOfBattle?.Values.Any(dice => !Dice(dice, allowEmpty: false)) == true
        || rolls.BerserkChecks?.Values.Any(dice => !Dice(dice, allowEmpty: false)) == true
        || rolls.CrewChecks?.Values.Any(dice => !Dice(dice, allowEmpty: false)) == true
        || rolls.UnlikelyKill?.Values.Any(dr => dr is < 1 or > 6) == true;

    /// <summary>Whether a unit may fire in this attack under the fire-phase and First Fire rules (A7.1, A8.1, A8.3, A8.31, A8.4, A9.2).</summary>
    private static bool FirerMayFire(FireAttack attack, FireFirer firer)
    {
        var firstFire = firer.FirstFireMarked == true;
        var finalFire = firer.FinalFireMarked == true;

        // A9.2: a MG that kept its Multiple ROF fires again alone, though its unit is already marked.
        var rateOfFireShot = firer.UsesInherentFp == false && firer.Weapons is { Count: > 0 };
        return attack.FireKind switch
        {
            FirstFire => rateOfFireShot || (!firstFire && !finalFire && firer.FiredThisPlayerTurn != true),
            SubsequentFirstFire => firstFire && !finalFire,
            FinalProtectiveFire => finalFire,
            _ when attack.Phase == "DFPh" => rateOfFireShot || (!finalFire && (firstFire || firer.FiredThisPlayerTurn != true)),
            _ => rateOfFireShot || firer.FiredThisPlayerTurn != true,
        };
    }

    /// <summary>Whether a MG may fire in this attack: not yet marked, or marked First Fire where Sustained Fire allows it (A8.3, A8.31, A8.4, A9.3).</summary>
    private static bool WeaponMayFire(FireAttack attack, FireWeapon weapon) => attack.FireKind switch
    {
        FinalProtectiveFire => true,
        SubsequentFirstFire => weapon.FiredThisPlayerTurn != true,
        _ when attack.Phase == "DFPh" => weapon.FiredThisPlayerTurn != true,
        _ => weapon.FiredThisPlayerTurn != true && weapon.FirstFireMarked != true,
    };

    private static bool Dice(IReadOnlyList<int>? dice, bool allowEmpty) =>
        dice is null ? allowEmpty : dice.Count == 2 && dice.All(die => die is >= 1 and <= 6);

    private static List<string> Undecided(FireAttack attack, ScenarioA1FireReference reference)
    {
        var undecided = new List<string>();
        var targets = attack.Targets!;
        if (attack.VehicleFire is not null)
        {
            if (attack.SameLevel != true)
            {
                undecided.Add("asl.a1.fire.levels-differ");
            }

            if (attack.Los is { } los && (los.HindranceAttributed != true || los.HindranceDrm < 0
                || (los.GrainInLos == true && attack.ScenarioMonth is not (>= 6 and <= 9))))
            {
                undecided.Add("asl.a1.fire.hindrance-unattributed");
            }
        }

        // D5.1: an AFV crew checks morale at its nationality's best elite Infantry MMC Morale Level, read from the catalog.
        foreach (var vehicle in attack.Vehicles ?? [])
        {
            if (reference.Definitions[vehicle.DefinitionId!] is { Unarmored: false } afv && reference.AfvCrewMorale(afv.Nationality) is null)
            {
                undecided.Add("asl.a1.fire.definition-incomplete:crew-morale:" + afv.Nationality);
            }
        }

        if (attack.FireKind != ResidualFire && attack.OrdnanceHit is null && attack.VehicleFire is null)
        {
            var firers = attack.Firers!;
            if (firers.Any(item => SameLevelOf(attack, item) != true))
            {
                undecided.Add("asl.a1.fire.levels-differ");
            }

            if (firers.Select(item => LosOf(attack, item)!).Any(los => los.HindranceAttributed != true || los.HindranceDrm < 0
                || (los.GrainInLos == true && attack.ScenarioMonth is not (>= 6 and <= 9))))
            {
                undecided.Add("asl.a1.fire.hindrance-unattributed");
            }
        }

        var units = targets.Where(item => item.Dummy != true).ToArray();
        if (units.Count(item => reference.Definitions[item.DefinitionId!].IsLeader) > 1)
        {
            undecided.Add("asl.a1.fire.leaders-interact");
        }

        // A19.13's exception for an underscored Morale Factor is not reviewed, for the targets or for FPF firers, whose NMC
        // can Replace them too.
        var checkedFirers = attack.FireKind == FinalProtectiveFire ? attack.Firers ?? [] : [];
        if (units.Select(item => item.DefinitionId).Concat(checkedFirers.Select(item => item.DefinitionId))
            .Any(id => id is not null && reference.Definitions.TryGetValue(id, out var definition) && definition is { IsMmc: true, UnderscoredMorale: not false }))
        {
            undecided.Add("asl.a1.fire.elr-undecided:underscored-morale");
        }

        // A15.1: a Green or Conscript unit's Heat of Battle DRM depends on whether it is Inexperienced (A19.2), which the
        // caller declares; so does an FPF firer's.
        foreach (var (target, index) in targets.Select((item, index) => (item, index)).Where(pair => pair.item.Dummy != true))
        {
            if (ScenarioA1HeatOfBattle.NeedsInexperience(reference.Definitions[target.DefinitionId!]) && target.Inexperienced is null)
            {
                undecided.Add($"asl.a1.fire.fact-missing:targets[{index}].inexperienced");
            }
        }

        if (attack.FireKind == FinalProtectiveFire)
        {
            foreach (var (firer, index) in attack.Firers!.Select((item, index) => (item, index)))
            {
                if (ScenarioA1HeatOfBattle.NeedsInexperience(reference.Definitions[firer.DefinitionId!]) && firer.Inexperienced is null)
                {
                    undecided.Add($"asl.a1.fire.fact-missing:firers[{index}].inexperienced");
                }
            }
        }

        // A7.83: a pinned mover takes no FFNAM or FFMO, so one attack on a stack mixing pinned and unpinned movers would
        // need two DRM; the review leaves that out.
        if (IsMovementFire(attack) && units.Any(item => item.Pinned == true) && units.Any(item => item.Pinned != true))
        {
            undecided.Add("asl.a1.fire.movement-drm-differs");
        }

        return undecided;
    }

    /// <summary>One resolution, with the target units' changing state.</summary>
    private sealed class Resolution(FireAttack attack, ScenarioA1FireReference reference)
    {
        private readonly List<string> undecided = [];
        private readonly HashSet<string> usedRolls = new(StringComparer.Ordinal);
        private readonly HashSet<string> usedChoices = new(StringComparer.Ordinal);
        private readonly Dictionary<string, TargetState> state = new(StringComparer.Ordinal);

        public FireResolution Run()
        {
            foreach (var target in attack.Targets!)
            {
                state[target.UnitId!] = new TargetState(target, target.Dummy == true ? DummyDefinition : reference.Definitions[target.DefinitionId!])
                {
                    NoQuarter = attack.TargetSideNoQuarter == true,
                };
            }

            var known = state.Values.Where(unit => !unit.IsConcealedType).ToArray();
            var concealed = state.Values.Where(unit => unit.IsConcealedType).ToArray();
            var vehicles = attack.Vehicles ?? [];

            // A Location the firing side sees nothing in is attacked as it would be if a hidden unit were there, so the
            // arithmetic does not tell the firing side which it was (A12.3, A12.13; ruling R21.1). A vehicle is a Known target.
            var arithmetic = Arithmetic(known.Length > 0 || vehicles.Count > 0, concealed.Length > 0 || (known.Length == 0 && vehicles.Count == 0));
            if (arithmetic is null)
            {
                return Refused(FireResolution.Indeterminate, undecided);
            }

            usedRolls.Add("attack");
            var concealedResult = arithmetic.Concealed?.Result ?? arithmetic.Result;
            if (known.Length > 0)
            {
                Apply(arithmetic.Result, known);
            }

            if (concealed.Length > 0 && undecided.Count == 0)
            {
                Apply(concealedResult, concealed);
            }

            LeaderLoss();
            var companions = BerserkTaskChecks();
            if (undecided.Count != 0)
            {
                return Refused(FireResolution.Indeterminate, undecided.Distinct().ToArray());
            }

            // A12.14: a concealed target loses "?" on a PTC or worse result; a hidden unit is placed without "?" (A12.3,
            // A12.31); a Dummy that loses "?" in the LOS of a Good Order enemy is removed (A12.11).
            if (concealedResult != "none")
            {
                foreach (var unit in concealed)
                {
                    if (unit.IsDummy)
                    {
                        unit.Eliminate("dummy-removed");
                    }
                    else
                    {
                        unit.ConcealmentLost = true;
                    }
                }
            }

            var weapons = attack.VehicleFire is { } vehicleFire ? [VehicleWeaponEffect(vehicleFire, arithmetic)] : WeaponEffects(arithmetic);
            var firerEffects = attack.FireKind == FinalProtectiveFire ? FinalProtectiveFireChecks(arithmetic) : null;
            var vehicleEffects = vehicles.Count == 0 ? null : VehicleEffects(arithmetic);
            if (undecided.Count != 0)
            {
                return Refused(FireResolution.Indeterminate, undecided.Distinct().ToArray());
            }

            var extra = ExtraRolls();
            if (extra.Count != 0)
            {
                return Refused(FireResolution.Abstained, extra);
            }

            if (attack.Choices?.Keys.FirstOrDefault(key => !usedChoices.Contains(key)) is { } unasked)
            {
                return Refused(FireResolution.Abstained, ["asl.a1.fire.extra-choice:" + unasked]);
            }

            var firerConcealment = FirerConcealment();
            if (undecided.Count != 0)
            {
                return Refused(FireResolution.Indeterminate, undecided);
            }

            var firers = attack.Firers ?? [];
            var marked = firers.Select(item => item.UnitId!).Concat(Directors(attack).Select(item => item.UnitId!))
                .Concat(attack.VehicleFire is { } firing ? [firing.VehicleId!] : []).ToArray();
            return new FireResolution(FireResolution.Resolved, [], arithmetic,
                attack.Targets!.Select(item => state[item.UnitId!].Effect()).ToArray(), marked, FireCounter(), firerConcealment)
            {
                WeaponEffects = weapons,
                FirerEffects = firerEffects,
                CompanionEffects = companions,
                VehicleEffects = vehicleEffects,
            };
        }

        // The DRM of an attack that belong to its Personnel targets rather than to the attack: TEM and the First Fire movement DRM.
        private static bool TargetOwn(FireModifier modifier) =>
            modifier.Name.StartsWith("tem:", StringComparison.Ordinal) || modifier.Name.StartsWith("critical-hit-tem:", StringComparison.Ordinal)
            || modifier.Name is "ffnam" or "ffmo";

        /// <summary>
        /// A7.307 to A7.309, D.8B (rulings R25.4 to R25.6): each vehicle in the target Location takes the attack's Original IFT DR with the
        /// attack's own DRM (Hindrance, a vehicle firer's Stun +1), never the Personnel targets' TEM or FFMO/FFNAM (A7.308 EX, A4.6, D.6). An
        /// unarmored vehicle is resolved on the Vehicle line of the attack's column: a Final DR at most half the Kill Number makes a burning
        /// wreck, below it eliminates, and equal to it immobilizes; an Original 2 that does neither rolls the Unlikely Kill dr (A7.309). An
        /// armored vehicle is unharmed (A7.307), but a Vulnerable crew (CE and not Stunned, D5.3, D5.34) takes a General Collateral Attack on
        /// the same column with the +2 CE DRM (D.8B, D5.31): a KIA or K result Recalls it (D5.341), a failed MC Stuns it (D5.34; Recalls a
        /// crew already under Stun +1, D5.342), and a failed PTC pins it (A7.82). Crews are not subject to Heat of Battle (A15.1).
        /// </summary>
        private List<FireVehicleEffect>? VehicleEffects(FireArithmetic arithmetic)
        {
            if (undecided.Count != 0)
            {
                return null;
            }

            var column = arithmetic.ColumnFp is { } fp ? Array.IndexOf(ScenarioA1FireReference.ColumnFp, fp) : -1;
            var drm = arithmetic.Drm.Where(item => !TargetOwn(item)).ToList();
            var final = arithmetic.OriginalDr + (int)drm.Sum(item => item.Value);
            var effects = new List<FireVehicleEffect>();
            foreach (var vehicle in attack.Vehicles!)
            {
                var id = vehicle.VehicleId!;
                var definition = reference.Definitions[vehicle.DefinitionId!];
                if (definition.Unarmored == true)
                {
                    var kill = column < 0 ? (int?)null : reference.KillNumber(column);
                    var result = kill is not { } number ? FireVehicleEffect.None
                        : final * 2 <= number ? FireVehicleEffect.BurningWreck
                        : final < number ? FireVehicleEffect.Eliminated
                        : final == number ? FireVehicleEffect.Immobilized
                        : FireVehicleEffect.None;
                    int? unlikely = null;
                    bool? declined = null;

                    // A7.309: after any Original 2 the firer may make the Unlikely Kill dr; a dr worse than the Original 2's own result does not
                    // cancel it (ruling R5.8). A burning wreck cannot be bettered.
                    if (arithmetic.OriginalDr == 2 && result != FireVehicleEffect.BurningWreck && kill is not null)
                    {
                        var key = "unlikelyKill:" + id;
                        var take = true;
                        if (attack.Choices is { } answers)
                        {
                            if (!answers.TryGetValue(key, out var answer))
                            {
                                undecided.Add("asl.a1.fire.choice-missing:" + key);
                                return null;
                            }

                            usedChoices.Add(key);
                            take = answer == "take";
                            declined = take ? null : true;
                        }

                        if (take)
                        {
                            if (attack.Rolls!.UnlikelyKill?.TryGetValue(id, out var dr) != true)
                            {
                                undecided.Add("asl.a1.fire.roll-missing:unlikelyKill:" + id);
                                return null;
                            }

                            usedRolls.Add("unlikelyKill:" + id);
                            unlikely = dr;
                            var subsequent = dr switch
                            {
                                1 => FireVehicleEffect.BurningWreck,
                                2 => FireVehicleEffect.Eliminated,
                                3 => FireVehicleEffect.Immobilized,
                                _ => FireVehicleEffect.None,
                            };
                            result = Severity(subsequent) > Severity(result) ? subsequent : result;
                        }
                    }

                    effects.Add(new FireVehicleEffect(id, definition.Id, result, kill, drm, final, unlikely, null, FireVehicleEffect.None) { UnlikelyKillDeclined = declined });
                    continue;
                }

                if (vehicle.CrewExposed != true || vehicle.Stunned == true)
                {
                    effects.Add(new FireVehicleEffect(id, definition.Id, FireVehicleEffect.None, null, drm, final, null, null, FireVehicleEffect.NotVulnerable));
                    continue;
                }

                List<FireModifier> crewDrm = [.. drm, new FireModifier("crew-exposed", 2m, "D5.31")];
                var crewFinal = arithmetic.OriginalDr + (int)crewDrm.Sum(item => item.Value);
                var outcome = column < 0 ? "none" : reference.Result(crewFinal, column);
                var morale = reference.AfvCrewMorale(definition.Nationality)!.Value;
                var kia = Regex.IsMatch(outcome, "^([1-7])KIA$") || Regex.IsMatch(outcome, "^K/([1-4])$");
                var mc = Regex.Match(outcome, "^([1-4])MC$");
                FireCheck? check = null;
                var crewResult = FireVehicleEffect.None;
                if (kia)
                {
                    crewResult = FireVehicleEffect.Recalled;
                }
                else if (mc.Success || outcome == "NMC" || outcome == "PTC")
                {
                    if (attack.Rolls!.CrewChecks?.TryGetValue(id, out var dice) != true)
                    {
                        undecided.Add("asl.a1.fire.roll-missing:crewCheck:" + id);
                        return null;
                    }

                    usedRolls.Add("crewCheck:" + id);
                    var pin = outcome == "PTC";
                    var modifier = mc.Success ? int.Parse(mc.Groups[1].Value, CultureInfo.InvariantCulture) : 0;
                    var checkDrm = new List<FireModifier>();
                    if (modifier > 0)
                    {
                        checkDrm.Add(new FireModifier("ift-mc", modifier, "A7.304"));
                    }

                    if (vehicle.StunRecovery == true)
                    {
                        checkDrm.Add(new FireModifier("stun-recovery", 1m, "D5.34"));
                    }

                    var original = dice![0] + dice[1];
                    var checkFinal = original + (int)checkDrm.Sum(item => item.Value);
                    var passed = checkFinal <= morale;
                    // A10.31, D5.341: an Original 12 on a MC is a Casualty MC, which Recalls an Inherent crew.
                    crewResult = !pin && original == 12 ? FireVehicleEffect.Recalled
                        : passed ? FireVehicleEffect.None
                        : pin ? FireVehicleEffect.Pinned
                        : vehicle.StunRecovery == true ? FireVehicleEffect.Recalled
                        : FireVehicleEffect.Stunned;
                    check = new FireCheck(pin ? "NTC" : mc.Success ? outcome : "NMC", dice.ToArray(), original, checkDrm, checkFinal, morale, passed, crewResult);
                }

                effects.Add(new FireVehicleEffect(id, definition.Id, FireVehicleEffect.None, null, crewDrm, crewFinal, null, check, crewResult));
            }

            return effects;
        }

        private static int Severity(string result) => result switch
        {
            FireVehicleEffect.BurningWreck => 3,
            FireVehicleEffect.Eliminated => 2,
            FireVehicleEffect.Immobilized => 1,
            _ => 0,
        };

        /// <summary>
        /// A vehicle's MA MG after its attack (D3.7, C2.24, A9.2): it malfunctions on an Original IFT DR of 12 (its B# of 12), and keeps its
        /// Multiple ROF on an Original colored dr at most its ROF, unless its crew is pinned (A7.82) or it fired in the AFPh (A7.25: only
        /// Opportunity Fire uses Multiple ROF in the AFPh).
        /// </summary>
        private FireWeaponEffect VehicleWeaponEffect(FireVehicleFire vehicle, FireArithmetic arithmetic)
        {
            var definition = reference.Definitions[vehicle.DefinitionId!];
            var breakdown = definition.Breakdown ?? 12;
            var malfunctioned = arithmetic.OriginalDr >= breakdown;
            var retained = !malfunctioned && vehicle.Pinned != true && attack.Phase != "AFPh" && definition.RateOfFire is { } rof && arithmetic.Dice[0] <= rof;
            return new FireWeaponEffect(vehicle.VehicleId!, breakdown, malfunctioned, retained, false, FireCounter(), null);
        }

        /// <summary>
        /// A15.41: after the attack, a leader who went berserk tries to take every other friendly unit in his Location subject to
        /// Heat of Battle with him: each takes a NTC with his leadership DRM, and on a pass goes berserk too, rallying if broken.
        /// Units the attack did not attack are its companions; the effects of those that took the NTC are returned.
        /// </summary>
        private List<FireUnitEffect>? BerserkTaskChecks()
        {
            if (undecided.Count != 0)
            {
                return null;
            }

            var leaders = state.Values.Where(unit => unit.WentBerserk && unit.Definition.IsLeader && !unit.Eliminated).ToArray();
            if (leaders.Length == 0)
            {
                return null;
            }

            var companions = (attack.Companions ?? []).Select(item => new TargetState(item, reference.Definitions[item.DefinitionId!])
            {
                NoQuarter = attack.TargetSideNoQuarter == true,
            }).ToArray();
            var checkedCompanions = new List<TargetState>();
            foreach (var leader in leaders)
            {
                var others = state.Values.Where(unit => unit != leader).Concat(companions)
                    .Where(unit => !unit.IsDummy && !unit.Eliminated && unit.Target.LocationId == leader.Target.LocationId
                        && ScenarioA1HeatOfBattle.Subject(unit.Definition, unit.IsHeroType, unit.Berserk))
                    .ToArray();
                foreach (var unit in CheckOrder(others))
                {
                    if (attack.Rolls!.BerserkChecks?.TryGetValue(unit.Id, out var dice) != true)
                    {
                        undecided.Add("asl.a1.fire.roll-missing:berserkCheck:" + unit.Id);
                        return null;
                    }

                    usedRolls.Add("berserkCheck:" + unit.Id);
                    var morale = unit.MoraleLevel!.Value;
                    List<FireModifier> drm = [new FireModifier("berserk-leader:" + leader.Id, leader.Leadership!.Value, "A15.41")];
                    var original = dice![0] + dice[1];
                    var final = original + (int)drm.Sum(item => item.Value);
                    var passed = final <= morale;
                    if (passed)
                    {
                        unit.GoBerserk();
                    }

                    unit.Checks.Add(new FireCheck("Berserk TC", dice.ToArray(), original, drm, final, morale, passed, passed ? "berserk" : "no-change"));
                    if (companions.Contains(unit))
                    {
                        checkedCompanions.Add(unit);
                    }
                }
            }

            return checkedCompanions.Count == 0 ? null : [.. checkedCompanions.Select(item => item.Effect())];
        }

        /// <summary>The fire counter the attack places (A3.2, A3.4, A3.5, A8.1, A8.3, A8.31, A8.4); none for Residual FP.</summary>
        private string? FireCounter() => attack.FireKind switch
        {
            _ when attack.OrdnanceHit is not null => null,
            ResidualFire => null,
            FirstFire => "first-fire",
            SubsequentFirstFire or FinalProtectiveFire => "final-fire",
            _ => attack.Phase is "PFPh" or "AFPh" ? "prep-fire" : "final-fire",
        };

        private FireArithmetic? Arithmetic(bool hasKnown, bool hasConcealed)
        {
            var dice = attack.Rolls!.Attack;
            var residual = attack.FireKind == ResidualFire;
            var hit = attack.OrdnanceHit;
            var firers = new List<FirerFirepower>();
            decimal known = 0, vsConcealed = 0;
            if (residual)
            {
                // A8.2, A8.22: Residual FP attacks alone, on its own column, never halved.
                known = vsConcealed = attack.ResidualFp!.Value;
            }
            else if (hit is not null)
            {
                // C.6: the Gun's HE FP column; C3.53, C.4: never halved for a concealed target; C3.71: doubled by a Critical Hit.
                known = vsConcealed = hit.Firepower!.Value * (hit.CriticalHit == true ? 2 : 1);
            }
            else if (attack.VehicleFire is { } vehicle)
            {
                if (hasKnown)
                {
                    firers.Add(VehicleFirepower(vehicle, false));
                    known = firers[^1].Firepower;
                }

                if (hasConcealed)
                {
                    var concealedFp = VehicleFirepower(vehicle, true);
                    firers.Add(hasKnown ? concealedFp with
                    {
                        VsConcealed = true
                    } : concealedFp);
                    vsConcealed = concealedFp.Firepower;
                }
            }
            else
            {
                if (hasKnown)
                {
                    firers.AddRange(Firepower(false));
                    known = firers.Where(item => item.VsConcealed != true).Sum(item => item.Firepower);
                }

                if (hasConcealed)
                {
                    var concealedFp = Firepower(true).Select(item => hasKnown ? item with { VsConcealed = true } : item).ToArray();
                    firers.AddRange(concealedFp);
                    vsConcealed = concealedFp.Sum(item => item.Firepower);
                }
            }

            if (dice is null)
            {
                undecided.Add("asl.a1.fire.roll-missing:attack");
                return null;
            }

            var original = dice[0] + dice[1];
            var directed = Directors(attack).Any();

            // A7.9: a doubles DR with no directing leader shifts the column; Residual FP is never subject to Cowering (A8.224);
            // heroes and Fanatic units are not subject to it, but a group with any other member Cowers (A15.2, A15.24, A10.8).
            // A7.9: no form of vehicular fire Cowers.
            var cowered = !residual && hit is null && attack.VehicleFire is null && dice[0] == dice[1] && !directed
                && attack.Firers!.Any(item => !reference.Definitions[item.DefinitionId!].IsHero && item.Fanatic != true);
            var inexperienced = (attack.Firers ?? []).Any(item => reference.Definitions[item.DefinitionId!].Class is "green" or "conscript");
            var shift = cowered ? (inexperienced ? 2 : 1) : 0;

            var drm = new List<FireModifier>();
            var tem = ScenarioA1FireReference.Tem[attack.TargetTerrain!];
            if (hit is not null)
            {
                // C.3: the TEM of an Infantry Target Type hit modifies its TH DR, not the Effects DR; C3.71: a Critical Hit reverses a
                // positive TEM into a negative Effects DRM (a Direct Fire hit has no Air Burst, B13.3).
                if (hit.CriticalHit == true && tem != 0)
                {
                    drm.Add(new FireModifier("critical-hit-tem:" + attack.TargetTerrain, -Math.Abs(tem), "C3.71"));
                }
            }
            else if (tem != 0)
            {
                drm.Add(new FireModifier("tem:" + attack.TargetTerrain, tem, "A7.6"));
            }

            // A7.52: the worst Hindrance of the group's LOS applies to all of it; Residual FP has none (A8.2), and an ordnance hit's
            // Hindrance modifies its TH DR (C.3, C6.9).
            var hindrance = residual || hit is not null ? 0
                : attack.VehicleFire is not null ? attack.Los!.HindranceDrm!.Value
                : attack.Firers!.Max(item => LosOf(attack, item)!.HindranceDrm!.Value);
            if (hindrance > 0)
            {
                drm.Add(new FireModifier("los-hindrance", hindrance, "A6.7"));
            }

            // A4.51 (ruling R5.2): +1 when a CX unit makes or directs the attack, once however many do.
            if (((attack.Firers ?? []).FirstOrDefault(item => item.Cx == true)?.UnitId ?? Directors(attack).FirstOrDefault(item => item.Cx == true)?.UnitId) is { } exhausted)
            {
                drm.Add(new FireModifier("cx:" + exhausted, 1m, "A4.51"));
            }

            // D5.34: a vehicle under Stun +1 adds one to its MG IFT DR.
            if (attack.VehicleFire is { StunRecovery: true } recovering)
            {
                drm.Add(new FireModifier("stun-recovery:" + recovering.VehicleId, 1m, "D5.34"));
            }

            // A7.531: the leadership of the directing leader, the worst of them for a group spanning Locations; A17.3: one
            // worse when wounded.
            var leadership = Directors(attack)
                .Select(director => (director.UnitId, Value: reference.Definitions[director.DefinitionId!].Leadership!.Value + (director.Wounded == true ? 1 : 0)))
                .OrderByDescending(item => item.Value).FirstOrDefault();
            if (leadership.UnitId is not null)
            {
                drm.Add(new FireModifier("leadership:" + leadership.UnitId, leadership.Value, "A7.531"));
            }

            // A15.24: each hero firing within its Normal Range lowers the group's DR by one, with any leadership DRM.
            foreach (var hero in (attack.Firers ?? []).Where(item => reference.Definitions[item.DefinitionId!].IsHero
                && RangeOf(attack, item) <= NormalRange(reference.Definitions[item.DefinitionId!], item)))
            {
                drm.Add(new FireModifier("heroic:" + hero.UnitId, -1m, "A15.24"));
            }

            // A4.6, A4.61, A8.13: FFNAM unless Assault Movement, and FFMO in Open Ground with no Hindrance, in Defensive
            // First Fire only; A7.83: a pinned mover takes neither.
            if (IsMovementFire(attack) && state.Values.Any(unit => !unit.IsDummy && !unit.Pinned))
            {
                if (attack.TargetMovement!.AssaultMovement != true)
                {
                    drm.Add(new FireModifier("ffnam", -1m, "A4.6"));
                }

                if (attack.TargetTerrain == "open-ground" && hindrance == 0)
                {
                    drm.Add(new FireModifier("ffmo", -1m, "A4.6"));
                }
            }

            var final = original + (int)drm.Sum(item => item.Value);
            var main = hasKnown ? known : vsConcealed;
            var (column, shifted, result) = Column(main, shift, final);
            FireColumn? second = null;
            if (hasKnown && hasConcealed && !residual && hit is null)
            {
                var (c2, s2, r2) = Column(vsConcealed, shift, final);
                second = new FireColumn(vsConcealed, c2, s2, r2);
            }

            var arithmetic = new FireArithmetic(firers, main, column, shift, cowered, shifted, dice.ToArray(), original, drm, final, result)
            {
                Concealed = second,
            };
            return arithmetic with
            {
                ResidualFp = Residual(arithmetic, hindrance, leadership.UnitId is null ? 0 : leadership.Value)
            };
        }

        private (int? Column, int? Shifted, string Result) Column(decimal total, int shift, int final)
        {
            var column = Array.FindLastIndex(ScenarioA1FireReference.ColumnFp, fp => fp <= total);
            var shifted = column < 0 ? -1 : column - shift;
            return (column < 0 ? null : ScenarioA1FireReference.ColumnFp[column], shifted < 0 ? null : ScenarioA1FireReference.ColumnFp[shifted],
                shifted < 0 ? "none" : reference.Result(final, shifted));
        }

        /// <summary>
        /// The Residual FP a Defensive First Fire, Subsequent First Fire, or FPF attack leaves (A8.2, A7.372): the highest
        /// counter at most half the highest column used, up to 12, one counter lower for each point of positive DRM arising
        /// outside the target hex (A8.26: LOS Hindrance and positive leadership).
        /// </summary>
        private int? Residual(FireArithmetic arithmetic, int hindrance, int leadership)
        {
            if (attack.FireKind is not (FirstFire or SubsequentFirstFire or FinalProtectiveFire))
            {
                return null;
            }

            var highest = Math.Max(arithmetic.ColumnFp ?? 0, arithmetic.Concealed?.ColumnFp ?? 0);
            var index = Array.FindLastIndex(ResidualCounters, fp => fp <= highest / 2m);
            index -= hindrance + Math.Max(leadership, 0);
            return index < 0 ? null : ResidualCounters[index];
        }

        /// <summary>
        /// A vehicle's MA AAMG FP (D1.83: Normal Range eight hexes): doubled at Point Blank Range (A7.21), halved beyond its Normal Range
        /// (A7.22), against concealed targets (A7.23), in the AFPh (D3.53), in Motion (D2.42), and when its crew is pinned (A7.82).
        /// </summary>
        private FirerFirepower VehicleFirepower(FireVehicleFire vehicle, bool vsConcealed)
        {
            var definition = reference.Definitions[vehicle.DefinitionId!];
            var range = attack.Range!.Value;
            var multipliers = new List<FireModifier>();
            if (range == 1)
            {
                multipliers.Add(new FireModifier("point-blank-fire", 2m, "A7.21"));
            }

            if (range > 8)
            {
                multipliers.Add(new FireModifier("long-range-fire", 0.5m, "A7.22"));
            }

            if (vsConcealed)
            {
                multipliers.Add(new FireModifier("area-fire-concealed-target", 0.5m, "A7.23"));
            }

            if (attack.Phase == "AFPh")
            {
                multipliers.Add(new FireModifier("advancing-fire", 0.5m, "D3.53"));
            }

            if (vehicle.InMotion == true)
            {
                multipliers.Add(new FireModifier("motion-fire", 0.5m, "D2.42"));
            }

            if (vehicle.Pinned == true)
            {
                multipliers.Add(new FireModifier("pinned-crew", 0.5m, "A7.82"));
            }

            var printed = definition.AntiAircraftMg!.Value;
            return new FirerFirepower(vehicle.VehicleId!, printed, multipliers, multipliers.Aggregate((decimal)printed, (value, item) => value * item.Value));
        }

        private IEnumerable<FirerFirepower> Firepower(bool vsConcealed)
        {
            foreach (var firer in attack.Firers!)
            {
                var definition = reference.Definitions[firer.DefinitionId!];
                var range = RangeOf(attack, firer)!.Value;
                var weapons = firer.Weapons ?? [];

                // A7.351, A7.352: a squad keeps its inherent FP with one SW, not two; a HS loses it with any.
                var inherent = firer.UsesInherentFp != false && !(definition.Kind == "asl:squad" && weapons.Count >= 2)
                    && !(definition.Kind == "asl:half-squad" && weapons.Count >= 1);
                if (inherent)
                {
                    var multipliers = Multipliers(range, NormalRange(definition, firer), vsConcealed, IsFinalFireAgain(attack, firer), sustained: false);
                    if (firer.Pinned == true)
                    {
                        multipliers.Add(new FireModifier("pinned-firer", 0.5m, "A7.8"));
                    }

                    var printed = firer.Wounded == true && definition.WoundedFirepower is { } woundedFp ? woundedFp : definition.Firepower!.Value;
                    var fp = multipliers.Aggregate((decimal)printed, (value, item) => value * item.Value);

                    // A7.36: Assault Fire adds one FP after every other modification, rounded up, but not at Long Range.
                    if (attack.Phase == "AFPh" && definition.AssaultFire == true && range <= definition.Range)
                    {
                        multipliers.Add(new FireModifier("assault-fire", 1m, "A7.36"));
                        fp = Math.Ceiling(fp + 1);
                    }

                    yield return new FirerFirepower(firer.UnitId!, printed, multipliers, fp);
                }

                foreach (var weapon in weapons)
                {
                    var mg = reference.Definitions[weapon.DefinitionId!];
                    var multipliers = Multipliers(range, mg.Range!.Value, vsConcealed, IsFinalFireAgain(attack, firer), IsSustained(attack, weapon));
                    var fp = multipliers.Aggregate((decimal)mg.Firepower!.Value, (value, item) => value * item.Value);
                    yield return new FirerFirepower(weapon.EquipmentId!, mg.Firepower.Value, multipliers, fp) { Operator = firer.UnitId };
                }
            }
        }

        /// <summary>A firer's Normal Range: a wounded hero's is his wounded side's (A15.2).</summary>
        private static int NormalRange(FireDefinition definition, FireFirer firer) =>
            firer.Wounded == true && definition.WoundedRange is { } wounded ? wounded : definition.Range!.Value;

        private List<FireModifier> Multipliers(int range, int normalRange, bool vsConcealed, bool finalFireAgain, bool sustained)
        {
            var multipliers = new List<FireModifier>();
            if (range == 1)
            {
                multipliers.Add(new FireModifier("point-blank-fire", 2m, "A7.21"));
            }

            if (range > normalRange)
            {
                multipliers.Add(new FireModifier("long-range-fire", 0.5m, "A7.22"));
            }

            if (vsConcealed)
            {
                multipliers.Add(new FireModifier("area-fire-concealed-target", 0.5m, "A7.23"));
            }

            // A8.3, A8.31, A8.4, A9.3: Subsequent First Fire, FPF, a First-Fire-marked unit's Final Fire, and Sustained Fire
            // are Area Fire.
            if (attack.FireKind is SubsequentFirstFire or FinalProtectiveFire || finalFireAgain || sustained)
            {
                multipliers.Add(new FireModifier("area-fire", 0.5m, attack.FireKind switch
                {
                    SubsequentFirstFire => "A8.3",
                    FinalProtectiveFire => "A8.31",
                    _ => finalFireAgain ? "A8.4" : "A9.3",
                }));
            }

            if (attack.Phase == "AFPh")
            {
                multipliers.Add(new FireModifier("advancing-fire", 0.5m, "A7.24"));
            }

            return multipliers;
        }

        /// <summary>What the attack did to each MG: malfunction on the Original DR (A9.7, A9.71) and Multiple ROF on the colored die (A9.2).</summary>
        private List<FireWeaponEffect>? WeaponEffects(FireArithmetic arithmetic)
        {
            var weapons = (attack.Firers ?? []).SelectMany(item => item.Weapons ?? []).ToArray();
            if (weapons.Length == 0 || undecided.Count != 0)
            {
                return null;
            }

            var original = arithmetic.OriginalDr;
            var breakdown = weapons.ToDictionary(weapon => weapon.EquipmentId!,
                weapon => (reference.Definitions[weapon.DefinitionId!].Breakdown ?? 12) - (IsSustained(attack, weapon) ? 2 : 0), StringComparer.Ordinal);
            var reached = weapons.Where(weapon => original >= breakdown[weapon.EquipmentId!]).Select(weapon => weapon.EquipmentId!).ToArray();
            var malfunctioned = new HashSet<string>(StringComparer.Ordinal);
            var selection = new Dictionary<string, int>(StringComparer.Ordinal);
            if (reached.Length == 1)
            {
                malfunctioned.Add(reached[0]);
            }
            else if (reached.Length > 1)
            {
                // A9.71: Random Selection among the MGs whose B# the DR reached; the highest dr, ties included.
                foreach (var id in reached)
                {
                    if (attack.Rolls!.WeaponSelection?.TryGetValue(id, out var dr) != true)
                    {
                        undecided.Add("asl.a1.fire.roll-missing:weaponSelection:" + string.Join(",", reached));
                        return null;
                    }

                    selection[id] = dr;
                    usedRolls.Add("weaponSelection:" + id);
                }

                var highest = selection.Values.Max();
                malfunctioned.UnionWith(selection.Where(item => item.Value == highest).Select(item => item.Key));
            }

            // A9.2: the Original colored dr (the first die of the IFT DR) at most the MG's ROF keeps its Multiple ROF; A9.3:
            // Sustained Fire forfeits it.
            var colored = arithmetic.Dice[0];
            var counter = FireCounter();
            return weapons.Select(weapon =>
            {
                var id = weapon.EquipmentId!;
                var sustained = IsSustained(attack, weapon);
                var retained = !malfunctioned.Contains(id) && !sustained && attack.FireKind != FinalProtectiveFire
                    && reference.Definitions[weapon.DefinitionId!].RateOfFire is { } rof && colored <= rof;
                return new FireWeaponEffect(id, breakdown[id], malfunctioned.Contains(id), retained, sustained,
                    retained ? null : sustained ? "final-fire" : counter, selection.TryGetValue(id, out var dr) ? dr : null);
            }).ToList();
        }

        /// <summary>
        /// The NMC FPF inflicts on its firers (A8.31): the Original IFT DR, modified only by leadership (none: the review
        /// admits FPF undirected), against each FPF firer; a Casualty MC falls on one of two or more by Random Selection.
        /// </summary>
        private List<FireUnitEffect>? FinalProtectiveFireChecks(FireArithmetic arithmetic)
        {
            if (undecided.Count != 0)
            {
                return null;
            }

            var firers = attack.Firers!.Select(firer => new TargetState(
                new FireTarget(firer.UnitId, firer.DefinitionId, firer.LocationId, false, firer.Pinned, firer.Concealed, false, false, firer.Wounded == true, false)
                {
                    Fanatic = firer.Fanatic,
                    Inexperienced = firer.Inexperienced,
                    KnownEnemyInLos = firer.KnownEnemyInLos,
                    Captors = firer.Captors,
                },
                reference.Definitions[firer.DefinitionId!])
            {
                NoQuarter = attack.FiringSideNoQuarter == true,
            }).ToArray();
            var dice = arithmetic.Dice;
            var original = arithmetic.OriginalDr;
            TargetState? casualty = null;
            if (original == 12 && firers.Length > 1)
            {
                var drs = new Dictionary<string, int>(StringComparer.Ordinal);
                foreach (var firer in firers)
                {
                    if (attack.Rolls!.FirerSelection?.TryGetValue(firer.Id, out var dr) != true)
                    {
                        undecided.Add("asl.a1.fire.roll-missing:firerSelection:" + string.Join(",", firers.Select(item => item.Id)));
                        return null;
                    }

                    drs[firer.Id] = dr;
                    usedRolls.Add("firerSelection:" + firer.Id);
                }

                // The highest dr is affected; on a tie, the first of them in the declared order.
                var highest = drs.Values.Max();
                casualty = firers.First(item => drs[item.Id] == highest);
                casualty.RandomSelectionDr = highest;
            }
            else if (original == 12)
            {
                casualty = firers[0];
            }

            foreach (var firer in firers)
            {
                // A Casualty MC falls only on the selected firer; the others take the NMC as failed.
                MoraleOutcome(firer, dice, [], "NMC", firingSide: true, casualty: firer == casualty || original != 12);
                if (undecided.Count != 0)
                {
                    return null;
                }
            }

            return firers.Select(item => item.Effect()).ToList();
        }

        private void Apply(string result, IReadOnlyList<TargetState> group)
        {
            var units = group.Where(unit => !unit.IsDummy).ToArray();
            if (units.Length == 0)
            {
                return;
            }

            var kia = Regex.Match(result, "^([1-7])KIA$");
            var k = Regex.Match(result, "^K/([1-4])$");
            var mc = Regex.Match(result, "^([1-4])MC$");
            if (kia.Success)
            {
                var count = int.Parse(kia.Groups[1].Value, CultureInfo.InvariantCulture);
                var drs = RandomSelection(units);
                if (drs is null)
                {
                    return;
                }

                // A7.301: the # highest drs are eliminated, ties at the cut included; the rest break, and a unit that cannot
                // break (a hero or a heroic leader, A15.2) or is already broken suffers Casualty Reduction instead.
                var ordered = drs.OrderByDescending(item => item.Value).ToArray();
                var cut = count >= ordered.Length ? int.MinValue : ordered[count - 1].Value;
                foreach (var (id, dr) in ordered)
                {
                    var unit = state[id];
                    if (dr >= cut)
                    {
                        unit.Eliminate("eliminated-kia");
                    }
                    else if (!unit.Broken && !unit.IsHeroType && !unit.Berserk)
                    {
                        unit.Break("broken-kia");
                    }
                    else if (!Reduce(unit, "casualty-reduced-kia"))
                    {
                        return;
                    }
                }
            }
            else if (k.Success)
            {
                var drs = RandomSelection(units);
                if (drs is null)
                {
                    return;
                }

                // A7.302: the highest dr, ties included, is Casualty Reduced; every surviving unit then takes the #MC.
                var highest = drs.Values.Max();
                foreach (var (id, dr) in drs.Where(item => item.Value == highest))
                {
                    // Rolls are asked for one at a time: stop at the first one missing.
                    if (!Reduce(state[id], "casualty-reduced-k"))
                    {
                        return;
                    }
                }

                MoraleChecks(int.Parse(k.Groups[1].Value, CultureInfo.InvariantCulture), units);
            }
            else if (mc.Success)
            {
                MoraleChecks(int.Parse(mc.Groups[1].Value, CultureInfo.InvariantCulture), units);
            }
            else if (result == "NMC")
            {
                MoraleChecks(0, units);
            }
            else if (result == "PTC")
            {
                PinTaskChecks(units);
            }
        }

        private Dictionary<string, int>? RandomSelection(IReadOnlyList<TargetState> units)
        {
            // One dr for each unit of the group the result applies to (A.9, A7.301, A7.302).
            var drs = new Dictionary<string, int>(StringComparer.Ordinal);
            foreach (var unit in units)
            {
                if (attack.Rolls!.RandomSelection?.TryGetValue(unit.Id, out var dr) != true)
                {
                    undecided.Add("asl.a1.fire.roll-missing:randomSelection:" + string.Join(",", units.Select(item => item.Id)));
                    return null;
                }

                drs[unit.Id] = dr;
                unit.RandomSelectionDr = dr;
                usedRolls.Add("randomSelection:" + unit.Id);
            }

            return drs;
        }

        // Leaders check first, higher Morale Level first (A10.2), then the other units in the declared order.
        private static TargetState[] CheckOrder(IEnumerable<TargetState> units)
        {
            var list = units.ToArray();
            return list.Where(unit => unit.Definition.IsLeader).OrderByDescending(unit => unit.Definition.Morale)
                .Concat(list.Where(unit => !unit.Definition.IsLeader)).ToArray();
        }

        private void MoraleChecks(int modifier, IReadOnlyList<TargetState> units)
        {
            foreach (var unit in CheckOrder(units).Where(unit => !unit.Eliminated))
            {
                var check = Check(unit, "MC", "checks", modifier, useLeadership: true);
                if (check is null)
                {
                    return;
                }

                MoraleOutcome(unit, check.Value.Dice, check.Value.Drm);
                if (undecided.Count != 0)
                {
                    return;
                }
            }
        }

        private void MoraleOutcome(TargetState unit, IReadOnlyList<int> dice, List<FireModifier> drm, string kind = "MC", bool firingSide = false,
            bool casualty = true)
        {
            var brokenBefore = unit.Broken;
            var morale = unit.MoraleLevel;
            if (morale is null)
            {
                undecided.Add("asl.a1.fire.leaders-interact:broken-morale-unrecorded:" + unit.Id);
                return;
            }

            var original = dice[0] + dice[1];
            var final = original + (int)drm.Sum(item => item.Value);
            var passed = final <= morale && original != 12;
            string consequence;
            if (unit.Berserk)
            {
                // A15.42: a berserk unit that fails a MC suffers Casualty Reduction; it never breaks and is never pinned, and it takes
                // no Heat of Battle DR (A15.1). A10.31: an Original 12 on an unbroken unit not subject to breaking eliminates it, and
                // wounds a berserk leader with +1 to the Wound Severity dr (ruling R30.3).
                if (original == 12 && casualty)
                {
                    if (unit.Definition.IsLeader)
                    {
                        if (!Wound(unit, asIfWounded: true))
                        {
                            return;
                        }
                    }
                    else
                    {
                        unit.Eliminate("eliminated-casualty-mc-berserk");
                    }
                }
                else if (!passed && !Reduce(unit, "casualty-reduced-berserk"))
                {
                    return;
                }

                consequence = passed ? "passed" : unit.Eliminated ? "eliminated" : unit.Definition.IsLeader ? "wounded" : "casualty-reduced";
                unit.Checks.Add(new FireCheck(kind, dice.ToArray(), original, drm, final, morale.Value, passed, consequence));
                return;
            }

            if (unit.IsHeroType)
            {
                // A15.2, A15.21: a hero, or a heroic leader, who fails a MC is wounded, and eliminated if already wounded; he
                // never breaks, is never pinned by a check, and takes no Heat of Battle.
                if (passed)
                {
                    consequence = "passed";
                }
                else if (unit.Wounded)
                {
                    unit.Eliminate("eliminated-wounded-hero");
                    consequence = "eliminated";
                }
                else
                {
                    // A10.31 EXC: an Original 12 wounds him with +1 to the Wound Severity dr, as if already wounded.
                    if (!Wound(unit, casualty && original == 12))
                    {
                        return;
                    }

                    consequence = unit.Eliminated ? "eliminated" : "wounded";
                }

                unit.Checks.Add(new FireCheck(kind, dice.ToArray(), original, drm, final, morale.Value, passed, consequence));
                return;
            }

            if (original == 12 && casualty && !unit.Broken)
            {
                // A10.31: a Casualty MC, after any ELR Replacement (A19.13).
                if (Elr(firingSide) is not { } limit)
                {
                    return;
                }

                if (final - morale.Value > limit ? !ReduceBeyondElr(unit) : !Reduce(unit, "casualty-mc"))
                {
                    return;
                }

                if (!unit.Eliminated)
                {
                    unit.Break(null);
                }

                consequence = unit.Eliminated ? "eliminated" : "casualty-reduced-and-broken";
            }
            else if (original == 12 && casualty)
            {
                unit.Eliminate("eliminated-casualty-mc");
                consequence = "eliminated";
            }
            else if (!passed && !unit.Broken)
            {
                if (Elr(firingSide) is not { } limit)
                {
                    return;
                }

                if (final - morale.Value > limit)
                {
                    Replace(unit);
                    consequence = unit.Disrupted ? "disrupted" : "replaced";
                }
                else
                {
                    unit.Break("broken-" + kind.ToLowerInvariant());
                    consequence = "broken";
                }
            }
            else if (!passed)
            {
                if (!Reduce(unit, "casualty-reduced-" + kind.ToLowerInvariant()))
                {
                    return;
                }

                consequence = unit.Eliminated ? "eliminated" : unit.Definition.IsLeader ? "wounded" : "casualty-reduced";
            }
            else if (!unit.Broken && final == morale)
            {
                // A7.8: passing with the highest passing DR pins an unbroken unit.
                unit.Pin("pinned-highest-passing-dr");
                consequence = "pinned";
            }
            else
            {
                consequence = "passed";
            }

            unit.Checks.Add(new FireCheck(kind, dice.ToArray(), original, drm, final, morale.Value, passed, consequence));

            // A15.1: an Original MC DR of 2 calls for a Heat of Battle DR, with the +1 for a unit broken before it or by it; a second Original
            // 2 in the attack (its LLMC after its MC) calls for a second, under its own roll key, unless the first left the unit surrendered to
            // a captor; one only Disrupted by a Surrender result with no captor is still subject (A15.5; ruling R5.10). A unit made berserk or
            // heroic by the first is no longer subject to it.
            if (original == 2 && !unit.Eliminated && ScenarioA1HeatOfBattle.Subject(unit.Definition, unit.IsHeroType, unit.Berserk))
            {
                if (unit.HeatOfBattleOutcome is null)
                {
                    HeatOfBattle(unit, brokenBefore || unit.Broken, second: false);
                }
                else if (unit.SecondHeatOfBattleOutcome is null && !(unit.HeatOfBattleOutcome.Result == HeatOfBattleOutcome.Surrender && unit.HeatOfBattleOutcome.Captors is { Count: > 0 }))
                {
                    HeatOfBattle(unit, brokenBefore || unit.Broken, second: true);
                }
            }
        }

        /// <summary>
        /// The Heat of Battle DR after an Original MC DR of 2 (A15.1 to A15.5): a hero created or a leader made heroic, the unit
        /// Battle Hardened or made Fanatic, berserk (or Battle Hardened with no Known enemy in its LOS, A15.44), or surrendering.
        /// </summary>
        private void HeatOfBattle(TargetState unit, bool broken, bool second)
        {
            var key = second ? unit.Id + ":2" : unit.Id;
            if (attack.Rolls!.HeatOfBattle?.TryGetValue(key, out var dice) != true)
            {
                undecided.Add("asl.a1.fire.roll-missing:heatOfBattle:" + key);
                return;
            }

            usedRolls.Add("heatOfBattle:" + key);
            // A15.5: the captors are Good Order when the unit surrenders; one of the attack's own units it broke, pinned, eliminated, or made
            // berserk is not (the FPF firers' targets, A8.31).
            var captors = unit.Target.Captors?.Where(id => !state.TryGetValue(id, out var other) || !(other.Eliminated || other.Broken || other.Pinned || other.Berserk)).ToArray();
            var (outcome, reason) = ScenarioA1HeatOfBattle.Resolve(unit.Definition, broken, unit.Target.Inexperienced, unit.Fanatic, dice!, reference.Definitions,
                unit.Target.KnownEnemyInLos, captors, unit.NoQuarter);
            if (outcome is null)
            {
                undecided.Add(reason!);
                return;
            }

            // A15.3, ruling R5.8: the owner may refuse a Battle Hardening that would change the unit.
            if (attack.Choices is { } answers && outcome.HardeningMatters(unit.Broken, unit.Pinned, unit.Disrupted))
            {
                var choice = "battleHardening:" + key;
                if (!answers.TryGetValue(choice, out var answer))
                {
                    undecided.Add("asl.a1.fire.choice-missing:" + choice);
                    return;
                }

                usedChoices.Add(choice);
                if (answer != "take")
                {
                    outcome = outcome.WithHardeningRefused();
                }
            }

            unit.TakeHeatOfBattle(outcome, outcome.HardenedDefinitionId is { } next ? reference.Definitions[next] : null, second);
        }

        private int? Elr(bool firingSide)
        {
            // A19.1: the ELR of the checking unit's side is a declared fact: the target side's, or the firing side's for
            // the NMC FPF inflicts on its firers.
            if ((firingSide ? attack.FiringSideElr : attack.TargetSideElr) is { } value)
            {
                return value;
            }

            undecided.Add(firingSide ? "asl.a1.fire.elr-undecided:firing-side-elr-undeclared" : "asl.a1.fire.elr-undecided:elr-undeclared");
            return null;
        }

        private void Replace(TargetState unit)
        {
            // A19.13: Replaced by a broken unit of lesser quality; A19.12: Disrupted when none exists.
            var replacement = ScenarioA1FireReference.ReplacementOf(unit.Definition.Id);
            if (replacement is null && !unit.Fanatic)
            {
                unit.Disrupt();
            }
            else if (replacement is null)
            {
                unit.Note("fanatic-not-disrupted");
            }
            else
            {
                unit.ReduceTo(reference.Definitions[replacement], "replaced-elr");
            }

            unit.Break(null);
        }

        private bool ReduceBeyondElr(TargetState unit)
        {
            // A19.13: a squad whose Casualty MC also exceeds its ELR is Reduced to a broken HS of lesser quality; a
            // Conscript squad, which has none, becomes its own Conscript HS, Disrupted (user ruling, 2026-09-26). A HS is
            // eliminated by Casualty Reduction anyway. A leader is Replaced, then wounded.
            if (unit.Definition.Kind == "asl:squad")
            {
                var half = ScenarioA1FireReference.HalfSquadOf(unit.Definition.Id)!;
                var lesser = ScenarioA1FireReference.ReplacementOf(half);
                unit.ReduceTo(reference.Definitions[lesser ?? half], "casualty-reduced-beyond-elr");
                if (lesser is null && !unit.Fanatic)
                {
                    unit.Disrupt();
                }

                return true;
            }

            if (unit.Definition.IsLeader)
            {
                Replace(unit);
            }

            return Reduce(unit, "casualty-mc");
        }

        private void PinTaskChecks(IReadOnlyList<TargetState> units)
        {
            // A15.2: a hero, or a heroic leader, is not subject to enforced Pin results, so takes no PTC.
            foreach (var unit in CheckOrder(units).Where(unit => !unit.Eliminated && !unit.Broken && !unit.Pinned && !unit.IsHeroType && !unit.Berserk))
            {
                var check = Check(unit, "NTC", "checks", 0, useLeadership: true);
                if (check is null)
                {
                    return;
                }

                var (dice, drm) = check.Value;
                var morale = unit.MoraleLevel!.Value;
                var original = dice[0] + dice[1];
                var final = original + (int)drm.Sum(item => item.Value);
                var passed = final <= morale;
                if (!passed)
                {
                    unit.Pin("pinned-ptc");
                }

                unit.Checks.Add(new FireCheck("NTC", dice.ToArray(), original, drm, final, morale, passed, passed ? "passed" : "pinned"));
            }
        }

        private (IReadOnlyList<int> Dice, List<FireModifier> Drm)? Check(TargetState unit, string kind, string rolls, int modifier, bool useLeadership)
        {
            var source = rolls == "checks" ? attack.Rolls!.Checks : attack.Rolls!.LeaderLoss;
            if (source?.TryGetValue(unit.Id, out var dice) != true)
            {
                undecided.Add($"asl.a1.fire.roll-missing:{rolls}:{unit.Id}");
                return null;
            }

            usedRolls.Add(rolls + ":" + unit.Id);
            var drm = new List<FireModifier>();
            if (modifier != 0)
            {
                drm.Add(new FireModifier("ift-" + kind.ToLowerInvariant(), modifier, "A7.304"));
            }

            if (useLeadership && !unit.Berserk)
            {
                // A10.21, A10.22: one unbroken, unpinned leader of the Location other than the checker, and of higher
                // morale when the checker is a leader; A10.72: a non-zero modifier, a wounded leader's +1 included, cannot
                // be declined.
                var leader = state.Values.FirstOrDefault(other => other.Definition.IsLeader && other != unit && !other.Eliminated
                    && !other.Broken && !other.Pinned && !other.IsDummy && other.Target.Berserk != true
                    && (!unit.Definition.IsLeader || other.MoraleLevel > unit.MoraleLevel));
                if (leader?.Leadership is { } leadership and not 0)
                {
                    drm.Add(new FireModifier("leadership:" + leader.Id, leadership, "A10.21"));
                }
            }

            return (dice!, drm);
        }

        private void LeaderLoss()
        {
            if (undecided.Count != 0)
            {
                return;
            }

            foreach (var leader in state.Values.Where(unit => unit.Definition.IsLeader && (unit.Eliminated || unit.BrokeInThisAttack)).ToArray())
            {
                var leaderMorale = leader.Eliminated ? leader.MoraleAtLoss : leader.InitialMorale;
                if (leaderMorale is null)
                {
                    undecided.Add("asl.a1.fire.leaders-interact:broken-morale-unrecorded:" + leader.Id);
                    return;
                }

                // A10.2: an eliminated leader causes LLMC; an unbroken leader that broke causes LLTC, which cannot pin a hero
                // or a heroic leader (A15.2).
                var eliminated = leader.Eliminated;
                foreach (var unit in state.Values.Where(unit => unit != leader && !unit.Eliminated && !unit.IsDummy && !unit.Berserk
                    && (eliminated || (!unit.Broken && !unit.IsHeroType)) && unit.MoraleLevel < leaderMorale).ToArray())
                {
                    var check = Check(unit, eliminated ? "LLMC" : "LLTC", "leaderLoss", 0, useLeadership: false);
                    if (check is null)
                    {
                        return;
                    }

                    var (dice, drm) = check.Value;
                    if (leader.Leadership is int negative and < 0)
                    {
                        drm.Add(new FireModifier("reversed-leadership:" + leader.Id, -negative, "A10.2"));
                    }

                    if (eliminated)
                    {
                        MoraleOutcome(unit, dice, drm, "LLMC");
                        if (undecided.Count != 0)
                        {
                            return;
                        }
                    }
                    else
                    {
                        var morale = unit.MoraleLevel!.Value;
                        var original = dice[0] + dice[1];
                        var final = original + (int)drm.Sum(item => item.Value);
                        var passed = final <= morale;
                        if (!passed)
                        {
                            unit.Pin("pinned-lltc");
                        }

                        unit.Checks.Add(new FireCheck("LLTC", dice.ToArray(), original, drm, final, morale, passed, passed ? "passed" : "pinned"));
                    }
                }
            }
        }

        private bool Reduce(TargetState unit, string reason)
        {
            // A7.302: a HS is eliminated, a squad becomes its HS with the same broken status, a SMC (a leader or a hero) is
            // wounded (A15.2).
            if (unit.Definition.IsLeader || unit.Definition.IsHero)
            {
                return Wound(unit);
            }

            if (unit.Definition.Kind == "asl:half-squad")
            {
                unit.Eliminate(reason);
                return true;
            }

            var half = ScenarioA1FireReference.HalfSquadOf(unit.Definition.Id);
            if (half is null)
            {
                undecided.Add("asl.a1.fire.reduction-counter-missing:" + unit.Id);
                return false;
            }

            unit.ReduceTo(reference.Definitions[half], reason);
            return true;
        }

        private bool Wound(TargetState unit, bool asIfWounded = false)
        {
            // A17.11: a Wound Severity dr, +1 if already wounded (or a hero's Casualty MC, A10.31); 5 or more is mortal.
            if (attack.Rolls!.WoundSeverity?.TryGetValue(unit.Id, out var dr) != true)
            {
                undecided.Add("asl.a1.fire.roll-missing:woundSeverity:" + unit.Id);
                return false;
            }

            usedRolls.Add("woundSeverity:" + unit.Id);
            if (dr + (unit.Wounded || asIfWounded ? 1 : 0) >= 5)
            {
                unit.Eliminate("eliminated-mortal-wound");
            }
            else
            {
                unit.Wound();
            }

            return true;
        }

        private List<string> ExtraRolls()
        {
            var rolls = attack.Rolls!;
            var supplied = (rolls.RandomSelection?.Keys.Select(id => "randomSelection:" + id) ?? [])
                .Concat(rolls.Checks?.Keys.Select(id => "checks:" + id) ?? [])
                .Concat(rolls.LeaderLoss?.Keys.Select(id => "leaderLoss:" + id) ?? [])
                .Concat(rolls.WoundSeverity?.Keys.Select(id => "woundSeverity:" + id) ?? [])
                .Concat(rolls.WeaponSelection?.Keys.Select(id => "weaponSelection:" + id) ?? [])
                .Concat(rolls.FirerSelection?.Keys.Select(id => "firerSelection:" + id) ?? [])
                .Concat(rolls.HeatOfBattle?.Keys.Select(id => "heatOfBattle:" + id) ?? [])
                .Concat(rolls.BerserkChecks?.Keys.Select(id => "berserkCheck:" + id) ?? [])
                .Concat(rolls.CrewChecks?.Keys.Select(id => "crewCheck:" + id) ?? [])
                .Concat(rolls.UnlikelyKill?.Keys.Select(id => "unlikelyKill:" + id) ?? []);
            return supplied.Where(key => !usedRolls.Contains(key)).Select(key => "asl.a1.fire.extra-roll:" + key).ToList();
        }

        private List<string> FirerConcealment()
        {
            var concealed = (attack.Firers ?? []).Where(item => item.Concealed == true).Select(item => item.UnitId!)
                .Concat(Directors(attack).Where(item => item.Concealed == true).Select(item => item.UnitId!)).ToArray();
            if (concealed.Length == 0)
            {
                return [];
            }

            // A12.14: a concealed unit that fires or directs fire loses "?" in the LOS of a Good Order enemy ground
            // unit within 16 hexes. The package sees only the target Location, so it decides only when one of its
            // units was Good Order when the attack was made.
            if (attack.Firers!.All(item => RangeOf(attack, item) <= 16) && attack.Targets!.Any(item => item.Broken == false && item.Dummy != true))
            {
                return concealed.ToList();
            }

            undecided.Add("asl.a1.fire.concealment-unreviewed:firer-concealment");
            return [];
        }
    }

    private sealed class TargetState(FireTarget target, FireDefinition definition)
    {
        private readonly List<string> events = [];

        public FireTarget Target { get; } = target;

        public string Id { get; } = target.UnitId!;

        public FireDefinition Definition { get; private set; } = definition;

        public bool IsDummy { get; } = target.Dummy == true;

        /// <summary>Concealed, hidden (A12.3), or a Dummy: attacked on the halved column of A12.13.</summary>
        public bool IsConcealedType { get; } = target.Concealed == true || target.Hidden == true || target.Dummy == true;

        public bool Broken { get; private set; } = target.Broken == true;

        public bool Pinned { get; private set; } = target.Pinned == true;

        public bool Eliminated
        {
            get; private set;
        }

        public bool BrokeInThisAttack
        {
            get; private set;
        }

        public int? MoraleAtLoss
        {
            get; private set;
        }

        public bool ConcealmentLost
        {
            get; set;
        }

        public int? RandomSelectionDr
        {
            get; set;
        }

        public List<FireCheck> Checks { get; } = [];

        public bool Wounded { get; private set; } = target.Wounded == true;

        public bool Disrupted { get; private set; } = target.Disrupted == true;

        public bool Fanatic { get; private set; } = target.Fanatic == true;

        public bool Heroic { get; private set; } = target.Heroic == true;

        /// <summary>Berserk (A15.42), before the attack or by its Heat of Battle DR or a Berserk TC (A15.41).</summary>
        public bool Berserk { get; private set; } = target.Berserk == true;

        /// <summary>Whether the unit went berserk in this attack.</summary>
        public bool WentBerserk
        {
            get; private set;
        }

        /// <summary>A hero, or a heroic leader: wounded rather than broken by a failed MC (A15.2, A15.21).</summary>
        public bool IsHeroType => Definition.IsHero || (Definition.IsLeader && Heroic);

        public HeatOfBattleOutcome? HeatOfBattleOutcome
        {
            get; private set;
        }

        /// <summary>The second Heat of Battle DR of the attack (ruling R5.10).</summary>
        public HeatOfBattleOutcome? SecondHeatOfBattleOutcome
        {
            get; private set;
        }

        /// <summary>Whether the unit's side is faced with No Quarter (A20.3): a Surrender is Berserk.</summary>
        public bool NoQuarter
        {
            get; init;
        }

        private bool Hidden => Target.Concealed == true || Target.Hidden == true;

        /// <summary>The Morale Level before this attack, which an unbroken leader's LLTC compares against (A10.2).</summary>
        public int? InitialMorale
        {
            get;
        } = Morale(definition, target.Broken == true, target.Wounded == true, target.Fanatic == true, target.Heroic == true,
            target.Berserk == true);

        /// <summary>
        /// The current Morale Level: one lower when wounded (A17.3); a hero's is printed on his wounded side and never lowered
        /// (A15.2); a heroic leader's is at least 9 (A15.21: a 10-2 is a 1-4-10 hero); a Fanatic unit's is one higher (A10.8);
        /// a hero's or heroic leader's never exceeds 10, or 9 if wounded (A15.2); a berserk unit's is 10, never lowered, and one
        /// higher when Fanatic (A15.42, ruling R30.3).
        /// </summary>
        public int? MoraleLevel => Morale(Definition, Broken, Wounded, Fanatic, Heroic, Berserk);

        private static int? Morale(FireDefinition definition, bool broken, bool wounded, bool fanatic, bool heroic, bool berserk)
        {
            if (berserk)
            {
                return 10 + (fanatic ? 1 : 0);
            }

            int? level = definition.IsHero ? (wounded ? definition.WoundedMorale : definition.Morale)
                : definition.IsLeader && heroic ? (definition.Morale is { } leader ? Math.Max(leader, 9) - (wounded ? 1 : 0) : null)
                : (broken ? definition.BrokenMorale : definition.Morale) - (wounded ? 1 : 0);
            level += fanatic ? 1 : 0;
            if (level is { } value && (definition.IsHero || (definition.IsLeader && heroic)))
            {
                return Math.Min(value, wounded ? 9 : 10);
            }

            return level;
        }

        /// <summary>The leadership modifier, one worse when wounded (A17.3).</summary>
        public int? Leadership => Definition.Leadership + (Wounded ? 1 : 0);

        public void Note(string item) => events.Add(item);

        public void Eliminate(string reason)
        {
            MoraleAtLoss = MoraleLevel;
            Eliminated = true;
            ConcealmentLost = Hidden;
            events.Add(reason);
        }

        public void Break(string? reason)
        {
            if (!Broken)
            {
                BrokeInThisAttack = true;
            }

            Broken = true;
            Pinned = false;
            ConcealmentLost = Hidden;
            if (reason is not null)
            {
                events.Add(reason);
            }
        }

        public void Wound()
        {
            Wounded = true;
            ConcealmentLost = Hidden;
            events.Add("wounded");
        }

        public void Disrupt()
        {
            Disrupted = true;
            events.Add("disrupted");
        }

        public void Pin(string reason)
        {
            Pinned = true;
            events.Add(reason);
        }

        public void ReduceTo(FireDefinition half, string reason)
        {
            Definition = half;
            ConcealmentLost = Hidden;
            events.Add(reason);
        }

        /// <summary>
        /// A Heat of Battle result (A15.21, A15.3): a heroic leader rallies; a Battle Hardened unit is exchanged for an unbroken,
        /// unpinned unit of the next higher quality; a unit of the highest quality becomes Fanatic.
        /// </summary>
        public void TakeHeatOfBattle(HeatOfBattleOutcome outcome, FireDefinition? hardened, bool second = false)
        {
            if (second)
            {
                SecondHeatOfBattleOutcome = outcome;
            }
            else
            {
                HeatOfBattleOutcome = outcome;
            }

            events.Add("heat-of-battle:" + outcome.Result);
            if (outcome.HardeningRefused == true)
            {
                events.Add("battle-hardening-refused");
            }
            if (outcome.Heroic == true)
            {
                Heroic = true;
                Broken = false;
                Pinned = false;
            }

            if (outcome.HeroDefinitionId is { } hero)
            {
                events.Add("hero-created:" + hero);
            }

            if (outcome.Hardening)
            {
                // A15.3: exchanged "(even if broken)" for an unbroken, unpinned unit, so no longer Disrupted either; a unit
                // with no better class keeps its counter (ruling R28.7).
                Broken = false;
                Pinned = false;
                Disrupted = false;
            }

            if (hardened is not null)
            {
                Definition = hardened;
                events.Add("battle-hardened");
            }

            if (outcome.Fanatic == true)
            {
                Fanatic = true;
                events.Add("became-fanatic");
            }

            // A15.4, A15.5: Berserk rallies the unit; Surrender breaks and Disrupts it, and it surrenders to one of its captors.
            if (outcome.Result == HeatOfBattleOutcome.Berserk)
            {
                GoBerserk();
            }
            else if (outcome.Result == HeatOfBattleOutcome.Surrender)
            {
                Break(Broken ? null : "broken-surrender");
                Disrupt();
                if (outcome.Captors is { Count: > 0 })
                {
                    events.Add("surrendered");
                }
            }
        }

        /// <summary>A15.4, A15.42: the unit goes berserk: rallied if broken, no longer pinned or Disrupted, and without "?".</summary>
        public void GoBerserk()
        {
            Berserk = true;
            WentBerserk = true;
            Broken = false;
            Pinned = false;
            Disrupted = false;
            ConcealmentLost = Hidden;
            events.Add("went-berserk");
        }

        public FireUnitEffect Effect() => new(Id, Target.DefinitionId ?? DummyDefinition.Id, Definition.Id, RandomSelectionDr, Eliminated,
            Broken && !Eliminated, Pinned && !Broken && !Eliminated, Wounded && !Eliminated, Disrupted && !Eliminated, ConcealmentLost,
            events.ToArray(), Checks.ToArray())
        {
            HeatOfBattle = HeatOfBattleOutcome,
            SecondHeatOfBattle = SecondHeatOfBattleOutcome,
            Fanatic = Fanatic && !Eliminated ? true : null,
            Heroic = Heroic && !Eliminated ? true : null,
            Berserk = Berserk && !Eliminated ? true : null,
        };
    }
}
