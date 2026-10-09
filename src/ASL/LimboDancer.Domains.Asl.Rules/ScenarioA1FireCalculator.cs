using System.Globalization;
using System.Text.RegularExpressions;

namespace LimboDancer.Domains.Asl.Rules;

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

    /// <summary>A vehicle's fire during its own MPh (D3.3; ruling R6.9).</summary>
    public const string BoundingFirstFire = "bounding-first-fire";

    /// <summary>A vehicle's OVR of the Location it entered, a form of Bounding First Fire (D7.1; backlog pass 11, ruling R11.11).</summary>
    public const string OverrunFire = "overrun";

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
    /// <summary>
    /// A19.11 (referee, pass 18): crews, Commissars, and heroes are never Replaced for failing a MC by more than their ELR, so an attack on them
    /// needs no ELR.
    /// </summary>
    private static bool ElrImmune(string? definitionId, ScenarioA1FireReference reference) =>
        definitionId is { } id && reference.Definitions.TryGetValue(id, out var definition)
        && (definition.IsHero || definition.Kind == "asl:crew" || ScenarioA1FireReference.IsCommissar(id));

    public static IReadOnlyList<string> Precheck(FireAttack attack, ScenarioA1FireReference reference)
    {
        ArgumentNullException.ThrowIfNull(attack);
        ArgumentNullException.ThrowIfNull(reference);
        var first = Resolve(attack with
        {
            Rolls = new FireRolls(null, null, null, null)
            {
                // A22.611 (ruling R15.4): the MOL Check dr is made before the IFT DR; any value reaches the same missing IFT DR.
                MolCheck = attack.Firers?.Any(item => item.Mol == true) == true ? 1 : null,
            }
        }, reference);
        if (first.Disposition != FireResolution.Indeterminate || first.Reasons is not ["asl.a1.fire.roll-missing:attack"])
        {
            return first.Reasons;
        }

        var reasons = new List<string>();
        if (attack.TargetSideElr is null && attack.Targets!.Any(item => item.Dummy != true && item.Elr is null && !ElrImmune(item.DefinitionId, reference)))
        {
            reasons.Add("asl.a1.fire.elr-undecided:elr-undeclared");
        }

        // Ruling R18.3 (referee, pass 18): only the FPF firers and the friendly targets take a MC the firing side's ELR decides.
        if (attack.FiringSideElr is null
            && ((attack.FireKind == FinalProtectiveFire && attack.Firers!.Any(item => item.FinalFireMarked == true && item.Elr is null && !ElrImmune(item.DefinitionId, reference)))
                || attack.Targets!.Any(item => item.Friendly == true && item.Dummy != true && item.Elr is null && !ElrImmune(item.DefinitionId, reference))))
        {
            reasons.Add("asl.a1.fire.elr-undecided:firing-side-elr-undeclared");
        }

        var concealed = attack.Firers?.Any(item => item.Concealed == true) == true || attack.Director?.Concealed == true
            || attack.OtherDirectors?.Any(item => item.Concealed == true) == true;
        if (concealed && !ConcealedFirers(attack).All(item => item.Seen is not null)
            && !(attack.Firers!.All(item => RangeOf(attack, item) <= 16) && attack.Targets!.Any(item => item.Broken == false && item.Dummy != true)))
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
                ScenarioA1FireReference.CasualtyHalfSquadOf(id), ScenarioA1FireReference.HardenedOf(id) }.OfType<string>())
            {
                pending.Push(next);
            }
        }

        return reasons;
    }

    private static FireResolution Refused(string disposition, IReadOnlyList<string> reasons) =>
        new(disposition, reasons, null, [], [], null, []);

    private static bool IsMovementFire(FireAttack attack) => attack.FireKind is not (null or BoundingFirstFire or OverrunFire);

    private static bool IsMultiLocation(FireAttack attack) =>
        attack.Firers is { Count: > 0 } firers && firers.Select(item => item.LocationId).Distinct(StringComparer.Ordinal).Count() > 1;

    private static int? RangeOf(FireAttack attack, FireFirer firer) => firer.Range ?? attack.Range;

    private static bool? SameLevelOf(FireAttack attack, FireFirer firer) => firer.SameLevel ?? attack.SameLevel;

    /// <summary>How many levels the target is above a firer: none at the same level (ruling R10.4).</summary>
    private static int LevelAboveOf(FireAttack attack, FireFirer firer) =>
        SameLevelOf(attack, firer) == true ? 0 : firer.TargetLevelAbove ?? attack.TargetLevelAbove ?? 0;

    private static FireLos? LosOf(FireAttack attack, FireFirer firer) => firer.Los ?? attack.Los;

    private static IEnumerable<FireDirector> Directors(FireAttack attack) =>
        (attack.Director is null ? Array.Empty<FireDirector>() : [attack.Director]).Concat(attack.OtherDirectors ?? []);

    /// <summary>The concealed units that fire or direct, each with the planner's read of whether a Good Order enemy unit sees it (A12.14; pass 31d).</summary>
    private static IEnumerable<(string UnitId, bool? Seen)> ConcealedFirers(FireAttack attack) =>
        (attack.Firers ?? []).Where(item => item.Concealed == true).Select(item => (item.UnitId!, item.SeenByGoodOrderEnemy))
            .Concat(Directors(attack).Where(item => item.Concealed == true).Select(item => (item.UnitId!, item.SeenByGoodOrderEnemy)));

    // A8.4: in the DFPh, a unit already marked First Fire fires again as Area Fire at an adjacent or same-hex target.
    private static bool IsFinalFireAgain(FireAttack attack, FireFirer firer) => attack.Phase == "DFPh" && firer.FirstFireMarked == true;

    // A9.3: a MG firing as Subsequent First Fire or FPF, or in the DFPh while marked First Fire, uses Sustained Fire.
    private static bool IsSustained(FireAttack attack, FireWeapon weapon, ScenarioA1FireReference reference) =>
        (attack.FireKind is SubsequentFirstFire or FinalProtectiveFire || (attack.Phase == "DFPh" && weapon.FirstFireMarked == true))

        // A9.3 (referee, pass 9b): only a MG uses Sustained Fire.
        && reference.Definitions.GetValueOrDefault(weapon.DefinitionId ?? string.Empty)?.IsMg != false;

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
        else if (attack.Overrun is { } overrun)
        {
            Need(overrun.VehicleId, "overrun.vehicleId");
            Need(overrun.DefinitionId, "overrun.definitionId");
            Need(overrun.LocationId, "overrun.locationId");
            Need(overrun.CrewExposed, "overrun.crewExposed");
            Need(overrun.Immobile, "overrun.immobile");
            Need(overrun.MainArmamentMalfunctioned, "overrun.mainArmamentMalfunctioned");
            Need(overrun.BmgMalfunctioned, "overrun.bmgMalfunctioned");
            Need(overrun.CmgMalfunctioned, "overrun.cmgMalfunctioned");
            Need(attack.FirerLocationId, "firerLocationId");
            Need(attack.Range, "range");
            Need(attack.SameLevel, "sameLevel");
            NeedLos(attack.Los, string.Empty);
        }
        else if (attack.DemolitionCharge is { } charge)
        {
            Need(charge.EquipmentId, "demolitionCharge.equipmentId");
            Need(charge.DefinitionId, "demolitionCharge.definitionId");
            Need(charge.Mode, "demolitionCharge.mode");
            Need(charge.UserId, "demolitionCharge.userId");
            Need(charge.UserDefinitionId, "demolitionCharge.userDefinitionId");
            Need(charge.Cx, "demolitionCharge.cx");
            Need(charge.Captured, "demolitionCharge.captured");
            if (charge.Mode == FireDemolitionCharge.Placed)
            {
                Need(charge.ConcealedWhenPlaced, "demolitionCharge.concealedWhenPlaced");
            }

            Need(attack.FirerLocationId, "firerLocationId");
            Need(attack.Range, "range");
            Need(attack.SameLevel, "sameLevel");
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

        // Ruling R10.4: a target at another level names how many levels above the firer it is, for PBF (A7.21).
        if (attack.VehicleFire is null && attack.OrdnanceHit is null && attack.Overrun is null && attack.DemolitionCharge is null && !residual)
        {
            if (attack.SameLevel == false)
            {
                Need(attack.TargetLevelAbove, "targetLevelAbove");
            }

            foreach (var (firer, index) in (attack.Firers ?? []).Select((item, index) => (item, index)).Where(pair => pair.item.SameLevel == false))
            {
                Need(firer.TargetLevelAbove ?? attack.TargetLevelAbove, $"firers[{index}].targetLevelAbove");
            }
        }

        if (attack.HexsideTem is { } hexside)
        {
            Need(hexside.Terrain, "hexsideTem.terrain");
            Need(hexside.Tem, "hexsideTem.tem");
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
            // Ruling R8.1: an ordnance hit of Defensive First Fire attacks the moving units, its FFMO and FFNAM already To Hit Cases J3 and J4.
            // Table player, pass 15 (ruling R15.3): a DC Thrown as Defensive First Fire also attacks its thrower's Location, whose units are not moving.
            ("MPh", "non-phasing", null) => attack.OrdnanceHit is not null || attack.DemolitionCharge is { Mode: FireDemolitionCharge.Thrower },
            ("MPh", "phasing", BoundingFirstFire) => attack.VehicleFire is not null,
            ("MPh", "phasing", OverrunFire) => attack.Overrun is not null,
            _ => false,
        };
        // E1.7, E3.32, E3.741 (rulings R16.3, R16.11, R16.14): the Low Visibility DRM is 0 to 5 (6 blocks the LOS), the Extreme Winter reduction 0 to 2.
        if (attack.LowVisibilityDrm is < 0 or > 5 || attack.BreakdownReduction is < 0 or > 2)
        {
            outside.Add("asl.a1.fire.weather-outside");
        }

        if (!phaseAdmitted)
        {
            outside.Add("asl.a1.fire.phase-outside");
        }

        // Backlog pass 10 (rulings R10.4, R10.5, R10.8, R10.13): a wall or hedge TEM of at most its printed value and Height Advantage for
        // Direct Fire by Infantry, not Residual FP or an ordnance hit; a Snap Shot as Infantry Defensive First Fire with no wall crossed;
        // Hazardous Movement in the MPh.
        // D7.15 (ruling R11.11): an OVR takes the wall or hedge TEM of the hexside its vehicle entered across, never Height Advantage or a Snap Shot.
        var direct = kind != ResidualFire && attack.OrdnanceHit is null && attack.VehicleFire is null;
        if ((attack.HexsideTem is { } wall && (!direct || wall.Terrain is not ("wall" or "hedge") || wall.Tem < 0 || wall.Tem > (wall.Terrain == "wall" ? 2 : 1)))
            || (attack.HeightAdvantage == true && (!direct || attack.Overrun is not null))
            || (attack.SnapShot == true && (!direct || attack.Overrun is not null || phase != "MPh" || kind is not (FirstFire or SubsequentFirstFire) || attack.HexsideTem is not null))
            || (attack.HazardousMovement == true && phase != "MPh"))
        {
            outside.Add("asl.a1.fire.terrain-fact-outside");
        }

        var targets = attack.Targets!;
        var targetDefinitions = targets.Select(item => item.Dummy == true ? DummyDefinition : reference.Definitions.GetValueOrDefault(item.DefinitionId!))
            .ToArray();

        // A7.307, A7.308, D.8B: the vehicles in the target Location are attacked with the attack's IFT DR (unit step 25): one at a time
        // (ruling R25.3 keeps a Location to one vehicle, so the A7.308 limit on vehicles affected never binds), by Residual FP too (A8.2,
        // A8.222; ruling R6.6), never by an ordnance hit here (the Vehicle Target Type is its own shot, R7.2): an unarmored vehicle, or an
        // AFV open-topped or closed-topped, whose CE crew alone is Vulnerable (R25.1, R7.11). An AFV in terrain with a positive TEM (not
        // cumulative with its crew's CE DRM, D5.31) is not reviewed (ruling R25.6).
        var vehicles = attack.Vehicles ?? [];
        if (vehicles.Count > 1 || (vehicles.Count > 0 && attack.OrdnanceHit is not null)
            || ((attack.TargetTerrain is { } vehicleTerrain && ScenarioA1FireReference.Tem.TryGetValue(vehicleTerrain, out var vehicleTem) && vehicleTem > 0)
                && vehicles.Any(item => reference.Definitions.GetValueOrDefault(item.DefinitionId!) is { IsVehicle: true, Unarmored: not true }))
            || vehicles.Any(item => item.LocationId != attack.TargetLocationId
                || reference.Definitions.GetValueOrDefault(item.DefinitionId!) is not { IsVehicle: true }))
        {
            outside.Add("asl.a1.fire.vehicle-outside");
        }

        if (attack.VehicleFire is { } vehicleFire)
        {
            VehicleFireOutside(attack, vehicleFire, reference, targetDefinitions, outside);
            return outside.Distinct(StringComparer.Ordinal).ToList();
        }

        if (attack.Overrun is { } overrun)
        {
            OverrunOutside(attack, overrun, reference, targetDefinitions, outside);
            return outside.Distinct(StringComparer.Ordinal).ToList();
        }

        if (attack.DemolitionCharge is { } charge)
        {
            DemolitionChargeOutside(attack, charge, reference, targetDefinitions, outside);
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

            if (targetDefinitions.Any(item => item?.Kind == "asl:crew") && !GunCrewAlone(attack))
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
        var sides = FiringNationalities(attack, reference);
        var locations = firers.Select(item => item.LocationId!).Distinct(StringComparer.Ordinal).ToArray();
        var multi = locations.Length > 1;

        // A7.5: a group may span Locations each ADJACENT to another of them; Residual FP never joins a group (A8.22).
        if (attack.FireGroupComplete != true || !locations.Contains(attack.FirerLocationId) || (multi && attack.FirerLocationsAdjacent != true)
            || firers.Any(item => item.Broken == true || (item.UsesSupportWeapon == true && item.Weapons is not { Count: > 0 }))
            || firers.Zip(firerDefinitions).Any(pair => pair.Second is null || (!LeaderMg(pair.First, pair.Second)
                && (!(pair.Second.IsMmc || pair.Second.IsHero || pair.Second.Kind == "asl:crew") || pair.Second.Firepower is null || pair.Second.Range is null)))
            || firers.Any(item => item.Partner is not null && !LeaderMg(item, reference.Definitions.GetValueOrDefault(item.DefinitionId!)))
            // A7.352 (ruling R8.4): a crew that fired its Gun this Player Turn has no inherent FP.
            || firers.Any(item => item.GunFired == true && item.UsesInherentFp != false))
        {
            outside.Add("asl.a1.fire.firer-outside");
        }

        if (firerDefinitions.Any(item => item is not null && !sides.Contains(item.Nationality)))
        {
            outside.Add("asl.a1.fire.firers-of-two-sides");
        }

        // A9.1, A7.35 to A7.352: MGs of the firer's own side, not malfunctioned; a squad fires at most two, a HS one.
        foreach (var (firer, definition) in firers.Zip(firerDefinitions))
        {
            var weapons = firer.Weapons ?? [];
            // A15.23 (ruling R15.11): a hero fires one MG or FT.
            if (definition is not null && (weapons.Count > (definition.Kind == "asl:squad" ? 2 : 1)
                || weapons.Any(weapon => reference.Definitions.GetValueOrDefault(weapon.DefinitionId!) is not { Firepower: not null, Range: not null } mg || !(mg.IsMg || mg.IsAtr || mg.IsFt)
                    || (definition.IsHero && mg.IsAtr)
                    || (mg.Nationality != definition.Nationality) != (weapon.Captured == true) || weapon.Malfunctioned == true || !WeaponMayFire(attack, weapon))))
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

        // A22.1, A22.31 to A22.33 (ruling R15.1): a FT attacks alone, with no other firer, weapon, inherent FP, or leader, from an unpinned unit;
        // at Long Range only through a LOS with no Hindrance, and never at a target more than two levels above or below.
        var flamethrower = firers.Any(item => (item.Weapons ?? []).Any(weapon => reference.Definitions.GetValueOrDefault(weapon.DefinitionId ?? string.Empty)?.IsFt == true));
        if (flamethrower && (firers.Count != 1 || firers[0].Weapons is not [_] || firers[0].UsesInherentFp != false || Directors(attack).Any()
            || firers[0].Pinned == true || attack.SprayingFire == true || Math.Abs(LevelAboveOf(attack, firers[0])) > 2
            || (RangeOf(attack, firers[0]) == 2 && LosOf(attack, firers[0])!.HindranceDrm != 0)))
        {
            outside.Add("asl.a1.fire.flamethrower-outside");
        }

        // A22.611 (ruling R15.4): one MOL Check per attack, by an unpinned unit in a PBF or TPBF attack, not Subsequent First Fire or FPF, with no
        // vehicle in the target Location; a squad adds its inherent FP and fires no SW, a HS, crew, or hero no SW.
        var mol = firers.Where(item => item.Mol == true).ToArray();
        if (mol.Length > 1 || mol.Any(item => item.Pinned == true || RangeOf(attack, item) > 1 || item.UsesInherentFp == false || item.Weapons is { Count: > 0 }
            || kind is SubsequentFirstFire or FinalProtectiveFire || flamethrower || (attack.Vehicles ?? []).Count > 0
            || reference.Definitions.GetValueOrDefault(item.DefinitionId ?? string.Empty) is not { } molUser || !(molUser.IsMmc || molUser.IsHero || molUser.Kind == "asl:crew")))
        {
            outside.Add("asl.a1.fire.mol-outside");
        }

        // A8.31 (ruling R12.3): FPF by units already marked Final Fire, at an ADJACENT or same-hex moving unit, directed or not, and with other
        // firers of the Location or ADJACENT ones in its group.
        if (kind == FinalProtectiveFire && (firers.All(item => item.FinalFireMarked != true) || firers.Any(item => item.FinalFireMarked == true && RangeOf(attack, item) > 1)))
        {
            outside.Add("asl.a1.fire.fpf-outside");
        }

        // A8.3: Subsequent First Fire within Normal Range and no farther than the closest armed Known enemy unit; in a mixed FPF group, for its
        // Subsequent First Fire members (referee, pass 12).
        var subsequentMembers = kind == SubsequentFirstFire ? firers.Zip(firerDefinitions).ToArray()
            : kind == FinalProtectiveFire ? firers.Zip(firerDefinitions).Where(pair => pair.First.FinalFireMarked != true).ToArray() : [];
        if (subsequentMembers.Length > 0 && (attack.WithinSubsequentFirstFireRange != true
            || subsequentMembers.Any(pair => pair.Second is { Range: { } range } && RangeOf(attack, pair.First) > range)))
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
            if (definition is null || !definition.IsLeader || definition.Leadership is null || !sides.Contains(definition.Nationality)
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

        // C11 (ruling R8.3): a Gun's crew is a target alone in its Location; a crew with other units, or with no Gun named, is not reviewed.
        if (targetDefinitions.Any(item => item?.Kind == "asl:crew") && !GunCrewAlone(attack))
        {
            outside.Add("asl.a1.fire.crew-target-unreviewed");
        }

        // A7.21 (ruling R10.14): TPBF, by a group in one Location at the enemy units in that Location, never a Snap Shot (A8.15).
        var tpbf = !multi && locations is [{ } only] && only == attack.TargetLocationId && attack.SnapShot != true;
        if ((locations.Contains(attack.TargetLocationId) && !tpbf)
            || targets.Any(item => item.LocationId != attack.TargetLocationId)
            || targets.Zip(targetDefinitions).Any(pair => pair.Second is null
                || (pair.Second != DummyDefinition && sides.Contains(pair.Second.Nationality) != (pair.First.Friendly == true)))
            || !ScenarioA1FireReference.Tem.ContainsKey(attack.TargetTerrain!))
        {
            outside.Add("asl.a1.fire.target-outside");
        }

        // A9.5, A9.52 (ruling R12.6): Spraying Fire by a group whose every firer fires a MG, in a fire phase, never at its own Location.
        if (attack.SprayingFire == true && (phase is not ("PFPh" or "AFPh" or "DFPh") || tpbf || firers.Any(item => item.Weapons is not { Count: > 0 } weapons
            || weapons.Any(weapon => reference.Definitions.GetValueOrDefault(weapon.DefinitionId ?? string.Empty) is not { IsMg: true }))))
        {
            outside.Add("asl.a1.fire.spraying-fire-outside");
        }

        // A7.25 (ruling R12.1): Opportunity Fire is made in the AFPh.
        if (firers.Any(item => item.OpportunityFire == true) && phase != "AFPh")
        {
            outside.Add("asl.a1.fire.phase-outside");
        }

        // A6.11, A7.52 (ruling R12.2): a group whose every firer's LOS is blocked still fires; a group only some of whose LOS is blocked is split
        // by the caller first.
        var blocked = firers.Count(item => LosOf(attack, item)!.Blocked == true);
        if (blocked > 0 && blocked < firers.Count)
        {
            outside.Add("asl.a1.fire.los-blocked");
        }

        foreach (var (firer, definition) in firers.Zip(firerDefinitions))
        {
            var range = RangeOf(attack, firer);
            var inherentOut = firer.UsesInherentFp != false && definition?.Range is { } normal && range > 2 * normal;
            // C13.24 (pass 9b): an ATR has no Long Range.
            var weaponOut = (firer.Weapons ?? []).Any(weapon => reference.Definitions.GetValueOrDefault(weapon.DefinitionId!) is { Range: { } mgRange } weaponDefinition
                && range > (weaponDefinition.IsAtr ? 1 : 2) * mgRange);
            if (range < (tpbf ? 0 : 1) || (tpbf && range != 0) || inherentOut || weaponOut)
            {
                outside.Add("asl.a1.fire.out-of-range");
            }
        }

        if (attack.Rolls is { } rolls && Malformed(rolls))
        {
            outside.Add("asl.a1.fire.roll-malformed");
        }

        return outside.Distinct(StringComparer.Ordinal).ToList();
    }

    /// <summary>
    /// The firing side's nationalities (A10.7; ruling R15.8): those the attack declares, or the first firer's (or a DC's user's) alone.
    /// </summary>
    private static HashSet<string> FiringNationalities(FireAttack attack, ScenarioA1FireReference reference)
    {
        if (attack.FiringNationalities is { Count: > 0 } declared)
        {
            return declared.ToHashSet(StringComparer.Ordinal);
        }

        var first = attack.DemolitionCharge is { } charge ? charge.UserDefinitionId : (attack.Firers is { Count: > 0 } listed ? listed[0].DefinitionId : null);
        return first is not null && reference.Definitions.GetValueOrDefault(first) is { } definition
            ? new HashSet<string>(StringComparer.Ordinal) { definition.Nationality }
            : new HashSet<string>(StringComparer.Ordinal);
    }

    /// <summary>
    /// A DC's attack (A23.1 to A23.63; rulings R15.2, R15.3): a DC of the catalog and its Personnel user; no firer or leader joins it; a Placed DC
    /// detonates in its side's AFPh, a Thrown one in a friendly fire phase or as Defensive First Fire, at an ADJACENT Location (the target) or its
    /// thrower's own (the thrower's); every unit there is attacked, of either side, and a Location holding a vehicle is not reviewed (backlog).
    /// </summary>
    private static void DemolitionChargeOutside(FireAttack attack, FireDemolitionCharge charge, ScenarioA1FireReference reference,
        IReadOnlyList<FireDefinition?> targetDefinitions, List<string> outside)
    {
        var definition = reference.Definitions.GetValueOrDefault(charge.DefinitionId!);
        var user = reference.Definitions.GetValueOrDefault(charge.UserDefinitionId!);
        var phaseAdmitted = (charge.Mode, attack.Phase, attack.FiringSide, attack.FireKind) switch
        {
            (FireDemolitionCharge.Placed, "AFPh", "phasing", null) => true,
            (FireDemolitionCharge.Thrown or FireDemolitionCharge.Thrower, "PFPh" or "AFPh", "phasing", null) => true,
            (FireDemolitionCharge.Thrown or FireDemolitionCharge.Thrower, "DFPh", "non-phasing", null) => true,
            (FireDemolitionCharge.Thrown, "MPh", "non-phasing", FirstFire) => true,
            (FireDemolitionCharge.Thrower, "MPh", "non-phasing", null) => true,
            _ => false,
        };
        if (definition is not { IsDc: true, Firepower: not null } || user is null || !(user.IsMmc || user.IsHero || user.IsLeader || user.Kind == "asl:crew")
            || attack.Firers is { Count: > 0 } || Directors(attack).Any() || !phaseAdmitted
            || (charge.Mode == FireDemolitionCharge.Thrower ? attack.Range != 0 || attack.FirerLocationId != attack.TargetLocationId : attack.Range != 1)
            || (charge.OpportunityFire == true && attack.Phase != "AFPh"))
        {
            outside.Add("asl.a1.fire.demolition-charge-outside");
        }

        var sides = FiringNationalities(attack, reference);
        if (attack.Targets!.Any(item => item.LocationId != attack.TargetLocationId)
            || attack.Targets!.Zip(targetDefinitions).Any(pair => pair.Second is null
                || (pair.Second != DummyDefinition && sides.Contains(pair.Second.Nationality) != (pair.First.Friendly == true)))
            || (attack.Vehicles ?? []).Count > 0 || !ScenarioA1FireReference.Tem.ContainsKey(attack.TargetTerrain!))
        {
            outside.Add("asl.a1.fire.target-outside");
        }

        if (attack.Rolls is { } rolls && Malformed(rolls))
        {
            outside.Add("asl.a1.fire.roll-malformed");
        }
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

        // D3.3, A8.1 (ruling R6.9): in the MPh a vehicle fires as Defensive First Fire or, moving, as Bounding First Fire; never as
        // Subsequent First Fire or FPF, which are Infantry's.
        if (attack.FireKind is not (null or FirstFire or BoundingFirstFire))
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

    /// <summary>
    /// A vehicle's OVR (D7.1, D7.12, D7.13; ruling R11.11): as Bounding First Fire in its own MPh, alone (D7.14), by a vehicle of the catalog at the
    /// enemy units of its own Location, never an AFV (D7.12; its Vulnerable crew is attacked through <see cref="FireAttack.Vehicles"/>), at range 0
    /// with a clear LOS.
    /// </summary>
    private static void OverrunOutside(FireAttack attack, FireOverrun overrun, ScenarioA1FireReference reference,
        IReadOnlyList<FireDefinition?> targetDefinitions, List<string> outside)
    {
        var definition = reference.Definitions.GetValueOrDefault(overrun.DefinitionId!);
        if (definition is not { IsVehicle: true } || attack.Firers is { Count: > 0 } || Directors(attack).Any() || attack.VehicleFire is not null
            || attack.OrdnanceHit is not null || overrun.LocationId != attack.FirerLocationId || overrun.LocationId != attack.TargetLocationId)
        {
            outside.Add("asl.a1.fire.overrun-outside");
        }

        if (attack.Targets!.Any(item => item.LocationId != attack.TargetLocationId)
            || targetDefinitions.Any(item => item is null || (item.Id != "dummy" && definition is not null && item.Nationality == definition.Nationality))
            || (attack.Vehicles ?? []).Any(item => definition is not null && reference.Definitions.GetValueOrDefault(item.DefinitionId!)?.Nationality == definition.Nationality)
            || !ScenarioA1FireReference.Tem.ContainsKey(attack.TargetTerrain!))
        {
            outside.Add("asl.a1.fire.target-outside");
        }

        if (attack.Range != 0 || attack.SameLevel != true)
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

    /// <summary>
    /// The weapons that add FP to an OVR (D7.11; ruling R11.11): the MA when it is a Gun, manned and functioning (the base of 4 FP), or, for the
    /// halftrack, its MA AAMG while its crew is CE; each BMG and CMG not malfunctioned. The RMG and AAMG of tanks are not in the catalog.
    /// </summary>
    public static IReadOnlyList<(string Weapon, int Firepower)> OverrunWeapons(FireOverrun overrun, FireDefinition definition)
    {
        var weapons = new List<(string, int)>();
        if (definition.Unarmored == true)
        {
            return weapons;
        }

        if (definition.MainArmament == "aamg")
        {
            if (overrun.CrewExposed == true && overrun.MainArmamentMalfunctioned != true && definition.AntiAircraftMg is { } aamg)
            {
                weapons.Add((FireOverrunEffect.MainArmament, aamg));
            }
        }
        else if (definition.Caliber is not null && overrun.MainArmamentMalfunctioned != true)
        {
            weapons.Add((FireOverrunEffect.MainArmament, 0));
        }

        if (definition.BowMg is { } bmg && overrun.BmgMalfunctioned != true)
        {
            weapons.Add((FireOverrunEffect.BowMg, bmg));
        }

        if (definition.CoaxialMg is { } cmg && overrun.CmgMalfunctioned != true)
        {
            weapons.Add((FireOverrunEffect.CoaxialMg, cmg));
        }

        return weapons;
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
        || rolls.UnlikelyKill?.Values.Any(dr => dr is < 1 or > 6) == true
        || rolls.MolCheck is < 1 or > 6;

    /// <summary>Whether a unit may fire in this attack under the fire-phase and First Fire rules (A7.1, A8.1, A8.3, A8.31, A8.4, A9.2).</summary>
    /// <summary>A leader firing one MG, alone or with a stacked SMC as his partner (A9.12; ruling R12.4); he has no inherent FP.</summary>
    private static bool LeaderMg(FireFirer firer, FireDefinition? definition) =>
        definition is { IsLeader: true, IsHero: false } && firer.UsesInherentFp == false && firer.Weapons is [_];

    /// <summary>
    /// The arithmetic of an attack before its roll, on an Original DR of 3 (A7.7; ruling R12.11): its columns and DRM, which say whether its FP could
    /// inflict at least a NMC; null when the facts do not reach it.
    /// </summary>
    public static FireArithmetic? Preview(FireAttack attack, ScenarioA1FireReference reference)
    {
        ArgumentNullException.ThrowIfNull(attack);
        ArgumentNullException.ThrowIfNull(reference);
        return new Resolution(attack with
        {
            Rolls = new FireRolls([1, 2], null, null, null)
        }, reference).ArithmeticOnly();
    }

    private static bool FirerMayFire(FireAttack attack, FireFirer firer)
    {
        // A9.5 (ruling R12.6): the second Location of Spraying Fire is fired at with the first, by firers its record already marked.
        if (attack.SprayShare == true)
        {
            return true;
        }

        var firstFire = firer.FirstFireMarked == true;
        var finalFire = firer.FinalFireMarked == true;

        // A9.2: a MG that kept its Multiple ROF fires again alone, though its unit is already marked.
        var rateOfFireShot = firer.UsesInherentFp == false && firer.Weapons is { Count: > 0 };
        return attack.FireKind switch
        {
            FirstFire => rateOfFireShot || (!firstFire && !finalFire && firer.FiredThisPlayerTurn != true),
            SubsequentFirstFire => firstFire && !finalFire,
            // A8.31 (ruling R12.3; referee, pass 12): FPF firers with Subsequent First Fire firers in one group, all then marked Final Fire.
            FinalProtectiveFire => finalFire || firstFire,
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

    /// <summary>Whether the attack's targets are exactly the crew of the Gun it names (ruling R8.3).</summary>
    internal static bool GunCrewAlone(FireAttack attack) => attack.GunTarget is { CrewUnitId: { } crew } && attack.Targets is [{ } only] && only.UnitId == crew;

    /// <summary>Whether a Good Order crew's gunshield faces the attack (C11.5).</summary>
    private static bool GunshieldFaces(FireAttack attack, FireGunTarget gun) => gun.Gunshield == true && attack.Targets is [{ Broken: not true }];

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

        if (attack.FireKind != ResidualFire && attack.OrdnanceHit is null && attack.VehicleFire is null && attack.Overrun is null && attack.DemolitionCharge is null)
        {
            // Ruling R10.4 (backlog pass 10): Infantry fire at another level is decided with the map read's LOS and Hindrance.
            var firers = attack.Firers!;
            if (firers.Select(item => LosOf(attack, item)!).Any(los => los.HindranceAttributed != true || los.HindranceDrm < 0
                || (los.GrainInLos == true && attack.ScenarioMonth is not (>= 6 and <= 9))))
            {
                undecided.Add("asl.a1.fire.hindrance-unattributed");
            }
        }

        // Pass 31 (play test P-02; ruling R31.5): several leaders among the targets are decided: leaders check first by Morale Level (A10.2), each
        // unit takes the best modifier of a leader who may give it (A10.21, A10.22), and each lost leader causes its own LLMC or LLTC.
        var units = targets.Where(item => item.Dummy != true).ToArray();

        // A19.13 (ruling R15.9): an underscored Morale Factor is decided; a MMC whose underline is not recorded is not.
        var checkedFirers = attack.FireKind == FinalProtectiveFire ? attack.Firers ?? [] : [];
        if (units.Select(item => item.DefinitionId).Concat(checkedFirers.Select(item => item.DefinitionId))
            .Any(id => id is not null && reference.Definitions.TryGetValue(id, out var definition) && definition is { IsMmc: true, UnderscoredMorale: null }))
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

        return undecided;
    }

    /// <summary>
    /// The AFV or wreck whose +1 TEM the targets claim (D9.3, D10.3; ruling R6.1), or null: none named, a positive terrain TEM, or an
    /// attack from within the Location.
    /// </summary>
    public static string? Cover(FireAttack attack)
    {
        ArgumentNullException.ThrowIfNull(attack);
        // Rulings R10.5, R10.13: a positive wall or hedge TEM is the target's positive TEM, and a Snap Shot takes no TEM.
        if (attack.AfvCover is not { } cover || !ScenarioA1FireReference.Tem.TryGetValue(attack.TargetTerrain ?? string.Empty, out var tem) || tem > 0
            || attack.HexsideTem is { Tem: > 0 } || attack.SnapShot == true)
        {
            return null;
        }

        // Residual FP is not fired from anywhere; any other attack from within the Location gets no cover.
        var within = attack.FireKind != ResidualFire && (attack.FirerLocationId == attack.TargetLocationId
            || (attack.Firers ?? []).Any(item => item.LocationId == attack.TargetLocationId) || attack.VehicleFire?.LocationId == attack.TargetLocationId
            || attack.Overrun?.LocationId == attack.TargetLocationId);
        return within ? null : cover;
    }

    /// <summary>One resolution, with the target units' changing state.</summary>
    private sealed class Resolution(FireAttack attack, ScenarioA1FireReference reference)
    {
        private readonly List<string> undecided = [];
        private readonly HashSet<string> usedRolls = new(StringComparer.Ordinal);
        private readonly HashSet<string> usedChoices = new(StringComparer.Ordinal);
        private readonly Dictionary<string, TargetState> state = new(StringComparer.Ordinal);

        // A22.611 (ruling R15.4): the attack's MOL Check, and whether a colored dr of 6 broke its user.
        private FireMolCheck? molCheck;

        // A23.4 (ruling R15.2): whether the DC's Original DR reached its malfunction number.
        private bool dcMalfunctioned;

        /// <summary>A FT attack (A22; ruling R15.1): its one firer's one weapon is a FT.</summary>
        private bool Flamethrower => (attack.Firers ?? []).SelectMany(item => item.Weapons ?? [])
            .Any(weapon => reference.Definitions.GetValueOrDefault(weapon.DefinitionId ?? string.Empty)?.IsFt == true);

        /// <summary>
        /// The removal number of a FT or DC (A22.3, A22.5, A23.2, A23.4; rulings R15.1, R15.2): its printed one, two lower in the hands of non-elite
        /// Personnel (a MMC not of the elite class), and two lower again when captured.
        /// </summary>
        private static int AssaultWeaponRemoval(FireDefinition weapon, FireDefinition? user, bool captured, bool inexperienced) =>
            (weapon.Breakdown ?? 12) - (user?.Class is { } cls && cls != "elite" && !(user.Nationality == "finnish" && cls == "1st-line") ? 2 : 0)
            - (captured ? 2 : 0) - (inexperienced || user?.Class == "conscript" ? 1 : 0);

        /// <summary>A Commissar of the unit's side in its Location who is unpinned, unbroken, and not berserk (A25.221; ruling R15.6).</summary>
        private static bool ActiveCommissar(TargetState other, TargetState unit) => other != unit && !other.IsDummy && ScenarioA1FireReference.IsCommissar(other.Definition.Id)
            && !other.Eliminated && !other.Broken && !other.Pinned && !other.Berserk && (other.Target.Friendly == true) == (unit.Target.Friendly == true)
            && other.Target.LocationId == unit.Target.LocationId && other.Target.GuardId is null;

        /// <summary>
        /// A25.221 (ruling R15.6): +1 to the Morale Level of every other friendly Infantry unit in an active Commissar's Location, but not a
        /// Commissar or a unit of Morale Level 10.
        /// </summary>
        private int CommissarBonus(TargetState unit) => !unit.IsDummy && !ScenarioA1FireReference.IsCommissar(unit.Definition.Id) && !unit.Berserk
            && unit.MoraleLevel < 10 && state.Values.Any(other => ActiveCommissar(other, unit)) ? 1 : 0;

        /// <summary>A10.7 (ruling R15.8): a leader influencing Allied Troops of another nationality does so one worse.</summary>
        private static int AlliedPenalty(FireDefinition leader, FireDefinition unit) => leader.Nationality != unit.Nationality ? 1 : 0;

        /// <summary>The arithmetic alone, for <see cref="Preview"/>.</summary>
        public FireArithmetic? ArithmeticOnly()
        {
            foreach (var target in attack.Targets ?? [])
            {
                if (target.Dummy == true || reference.Definitions.ContainsKey(target.DefinitionId ?? string.Empty))
                {
                    state[target.UnitId!] = new TargetState(target, target.Dummy == true ? DummyDefinition : reference.Definitions[target.DefinitionId!]);
                }
            }

            var known = state.Values.Any(unit => !unit.IsConcealedType);
            var concealed = state.Values.Any(unit => unit.IsConcealedType);
            var vehicles = attack.Vehicles ?? [];
            var knownVehicles = vehicles.Count(item => item.Concealed != true);
            return Arithmetic(known || knownVehicles > 0, concealed || knownVehicles < vehicles.Count || (!known && vehicles.Count == 0));
        }

        public FireResolution Run()
        {
            foreach (var target in attack.Targets!)
            {
                state[target.UnitId!] = new TargetState(target, target.Dummy == true ? DummyDefinition : reference.Definitions[target.DefinitionId!])
                {
                    NoQuarter = target.Friendly == true ? attack.FiringSideNoQuarter == true : attack.TargetSideNoQuarter == true,
                };
            }

            // A6.11 (ruling R12.2): fire whose every firer's LOS is blocked affects nothing; its DR decides only Multiple ROF.
            if ((attack.Firers ?? []).Count > 0 && attack.Firers!.All(item => LosOf(attack, item)!.Blocked == true))
            {
                return Blocked();
            }

            var known = state.Values.Where(unit => !unit.IsConcealedType).ToArray();
            var concealed = state.Values.Where(unit => unit.IsConcealedType).ToArray();
            var vehicles = attack.Vehicles ?? [];

            // A Location the firing side sees nothing in is attacked as it would be if a hidden unit were there, so the
            // arithmetic does not tell the firing side which it was (A12.3, A12.13; ruling R21.1). A vehicle is a Known target.
            // A12.13 (ruling R6.7): a concealed vehicle is attacked on the halved FP's column, as a concealed unit is.
            var knownVehicles = vehicles.Count(item => item.Concealed != true);
            var arithmetic = Arithmetic(known.Length > 0 || knownVehicles > 0, concealed.Length > 0 || knownVehicles < vehicles.Count || (known.Length == 0 && vehicles.Count == 0));
            if (arithmetic is null)
            {
                return Refused(FireResolution.Indeterminate, undecided);
            }

            usedRolls.Add("attack");
            var concealedResult = arithmetic.Concealed?.Result ?? arithmetic.Result;

            // A7.83 (ruling R12.3): the pinned movers of a mixed stack take the result of their own Final DR.
            var split = arithmetic.PinnedFinalDr is not null;
            TargetState[] Unpinned(TargetState[] group) => split ? [.. group.Where(unit => !unit.Target.Pinned.GetValueOrDefault())] : group;
            TargetState[] PinnedOf(TargetState[] group) => split ? [.. group.Where(unit => unit.Target.Pinned == true)] : [];
            if (Unpinned(known) is { Length: > 0 } knownUnpinned)
            {
                ApplyGroup(arithmetic.Result, knownUnpinned, arithmetic, concealedGroup: false, pinnedGroup: false);
            }

            if (PinnedOf(known) is { Length: > 0 } knownPinned && undecided.Count == 0)
            {
                ApplyGroup(arithmetic.PinnedResult!, knownPinned, arithmetic, concealedGroup: false, pinnedGroup: true);
            }

            if (Unpinned(concealed) is { Length: > 0 } concealedUnpinned && undecided.Count == 0)
            {
                ApplyGroup(concealedResult, concealedUnpinned, arithmetic, concealedGroup: true, pinnedGroup: false);
            }

            if (PinnedOf(concealed) is { Length: > 0 } concealedPinned && undecided.Count == 0)
            {
                ApplyGroup(arithmetic.PinnedConcealedResult ?? arithmetic.PinnedResult!, concealedPinned, arithmetic, concealedGroup: true, pinnedGroup: true);
            }

            // A20.54 (ruling R12.9): a prisoner is pinned when its Guard is.
            foreach (var prisoner in state.Values.Where(unit => unit.Target.GuardId is { } guard && state.TryGetValue(guard, out var custodian) && custodian.Pinned
                && !unit.Eliminated && !unit.Pinned))
            {
                prisoner.Pin("pinned-with-guard");
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

            var weapons = attack.VehicleFire is { } vehicleFire ? [VehicleWeaponEffect(vehicleFire, arithmetic)] : attack.SprayShare == true ? null : WeaponEffects(arithmetic);
            var overrunEffect = attack.Overrun is { } overrunning ? OverrunEffect(overrunning, arithmetic) : null;
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

            var firerConcealment = attack.SprayShare == true ? [] : FirerConcealment();
            if (undecided.Count != 0)
            {
                return Refused(FireResolution.Indeterminate, undecided);
            }

            return new FireResolution(FireResolution.Resolved, [], arithmetic,
                attack.Targets!.Select(item => state[item.UnitId!].Effect()).ToArray(), Marked(), FireCounter(), firerConcealment)
            {
                WeaponEffects = weapons,
                FirerEffects = firerEffects,
                CompanionEffects = companions,
                VehicleEffects = vehicleEffects,
                OverrunEffect = overrunEffect,
                MolCheck = molCheck,
                DemolitionChargeMalfunctioned = dcMalfunctioned ? true : null,
                FlamethrowerRemoved = weapons?.FirstOrDefault(item => item.Malfunctioned
                    && (attack.Firers ?? []).SelectMany(firer => firer.Weapons ?? []).Any(weapon => weapon.EquipmentId == item.EquipmentId
                        && reference.Definitions.GetValueOrDefault(weapon.DefinitionId ?? string.Empty)?.IsFt == true))?.EquipmentId,
            };
        }

        /// <summary>
        /// A group of targets takes its result; a unit possessing a FT takes the attack's DR one lower per FT, on its group's column (A22.4; ruling
        /// R15.1).
        /// </summary>
        private void ApplyGroup(string result, TargetState[] group, FireArithmetic arithmetic, bool concealedGroup, bool pinnedGroup)
        {
            foreach (var byFt in group.GroupBy(unit => unit.IsDummy ? 0 : unit.Target.Flamethrowers ?? 0).OrderBy(item => item.Key))
            {
                if (undecided.Count != 0)
                {
                    return;
                }

                var own = result;
                if (byFt.Key > 0 && !dcMalfunctioned && arithmetic.OriginalDr > 0)
                {
                    var final = (pinnedGroup ? arithmetic.PinnedFinalDr ?? arithmetic.FinalDr : arithmetic.FinalDr) - byFt.Key;
                    var fp = concealedGroup && arithmetic.Concealed is { } halved ? halved.TotalFirepower : arithmetic.TotalFirepower;
                    own = Column(fp, arithmetic.ColumnShift, final).Result;
                    foreach (var unit in byFt)
                    {
                        unit.Note($"flamethrower-possessed:{-byFt.Key}");
                    }
                }

                Apply(own, [.. byFt]);
            }
        }

        /// <summary>
        /// The units marked as having fired or directed: every firer and directing leader, a leader's partner (A9.12), and the firing vehicle, except a
        /// squad that fires one MG apart from its inherent FP, which keeps that FP for another attack (A7.351; ruling R12.4).
        /// </summary>
        private string[] Marked() => attack.SprayShare == true ? [] : [.. (attack.Firers ?? [])
            .Where(item => !(item.UsesInherentFp == false && item.Weapons is [_] && reference.Definitions.GetValueOrDefault(item.DefinitionId!)?.Kind == "asl:squad"))
            .Select(item => item.UnitId!)
            .Concat((attack.Firers ?? []).Select(item => item.Partner).OfType<string>())
            .Concat(Directors(attack).Select(item => item.UnitId!))
            .Concat(attack.VehicleFire is { } firing ? [firing.VehicleId!] : attack.Overrun is { } ovr ? [ovr.VehicleId!] : [])
            .Concat(attack.DemolitionCharge is { Mode: FireDemolitionCharge.Thrown } thrown ? [thrown.UserId!] : [])];

        /// <summary>
        /// A6.11 (ruling R12.2): fire at a Location whose LOS is blocked for every firer. Its DR is made: each MG keeps Multiple ROF on its colored dr, none
        /// malfunctions; the firers are marked and a concealed firer loses its "?" as for any fire; nothing in the target Location is affected.
        /// </summary>
        private FireResolution Blocked()
        {
            if (attack.Rolls!.Attack is not { } dice)
            {
                return Refused(FireResolution.Indeterminate, ["asl.a1.fire.roll-missing:attack"]);
            }

            usedRolls.Add("attack");
            var original = dice[0] + dice[1];
            var arithmetic = new FireArithmetic([], 0, null, 0, false, null, dice.ToArray(), original, [], original, "none");
            var weapons = WeaponEffects(arithmetic, malfunction: false);
            var extra = ExtraRolls();
            if (extra.Count != 0)
            {
                return Refused(FireResolution.Abstained, extra);
            }

            var firerConcealment = FirerConcealment();
            if (undecided.Count != 0)
            {
                return Refused(FireResolution.Indeterminate, undecided);
            }

            return new FireResolution(FireResolution.Resolved, [], arithmetic, attack.Targets!.Select(item => state[item.UnitId!].Effect()).ToArray(), Marked(),
                FireCounter(), firerConcealment)
            {
                WeaponEffects = weapons,
                LosBlocked = true,
            };
        }

        /// <summary>
        /// D7.17 (ruling R11.11): an OVR's Original IFT DR of 12 malfunctions one weapon that added FP, chosen among several by Random Selection
        /// (A9.71; the highest dr, ties included), or immobilizes a vehicle none of whose weapons added FP. The OVR itself is resolved normally.
        /// </summary>
        private FireOverrunEffect? OverrunEffect(FireOverrun overrun, FireArithmetic arithmetic)
        {
            if (arithmetic.OriginalDr != 12 || undecided.Count != 0)
            {
                return null;
            }

            var id = overrun.VehicleId!;
            var weapons = OverrunWeapons(overrun, reference.Definitions[overrun.DefinitionId!]).Select(item => item.Weapon).ToArray();
            if (weapons.Length == 0)
            {
                return new FireOverrunEffect(id, [], true);
            }

            if (weapons.Length == 1)
            {
                return new FireOverrunEffect(id, weapons, false);
            }

            var keys = weapons.Select(weapon => id + ":" + weapon).ToArray();
            var selection = new Dictionary<string, int>(StringComparer.Ordinal);
            foreach (var key in keys)
            {
                if (attack.Rolls!.WeaponSelection?.TryGetValue(key, out var dr) != true)
                {
                    undecided.Add("asl.a1.fire.roll-missing:weaponSelection:" + string.Join(",", keys));
                    return null;
                }

                selection[key] = dr;
                usedRolls.Add("weaponSelection:" + key);
            }

            var highest = selection.Values.Max();
            return new FireOverrunEffect(id, [.. weapons.Where(weapon => selection[id + ":" + weapon] == highest)], false);
        }

        // The DRM of an attack that belong to its Personnel targets rather than to the attack: TEM, an AFV's or wreck's cover, and the First
        // Fire movement DRM.
        private static bool TargetOwn(FireModifier modifier) =>
            modifier.Name.StartsWith("tem:", StringComparison.Ordinal) || modifier.Name.StartsWith("critical-hit-tem:", StringComparison.Ordinal)
            || modifier.Name.StartsWith("afv-cover:", StringComparison.Ordinal) || modifier.Name is "ffnam" or "ffmo";

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

            var drm = arithmetic.Drm.Where(item => !TargetOwn(item)).ToList();
            var final = arithmetic.OriginalDr + (int)drm.Sum(item => item.Value);
            var effects = new List<FireVehicleEffect>();
            foreach (var vehicle in attack.Vehicles!)
            {
                // A12.13 (ruling R6.7): a concealed vehicle in a Location with known targets takes the halved FP's column.
                var columnFp = vehicle.Concealed == true && arithmetic.Concealed is { } halved ? halved.ColumnFp : arithmetic.ColumnFp;
                var column = columnFp is { } fp ? Array.IndexOf(ScenarioA1FireReference.ColumnFp, fp) : -1;
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
            // E3.741 (ruling R16.14): Extreme Winter lowers the B#.
            var breakdown = (definition.Breakdown ?? 12) - (attack.BreakdownReduction ?? 0);
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

                // A25.223 (ruling R15.6): every friendly Infantry unit of a berserk Commissar's Location subject to Heat of Battle goes berserk, with no NTC.
                if (ScenarioA1FireReference.IsCommissar(leader.Definition.Id))
                {
                    foreach (var unit in others)
                    {
                        unit.GoBerserk();
                        unit.Note("berserk-with-commissar:" + leader.Id);
                        if (companions.Contains(unit))
                        {
                            checkedCompanions.Add(unit);
                        }
                    }

                    continue;
                }

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
            BoundingFirstFire or OverrunFire => "bounding-fire",
            SubsequentFirstFire or FinalProtectiveFire => "final-fire",
            _ => attack.Phase is "PFPh" or "AFPh" ? "prep-fire" : "final-fire",
        };

        /// <summary>A25.45, A25.7 (referee, pass 15): British elite and 1st Line units, and Finns but their Conscripts, are immune to Cowering.</summary>
        private static bool NeverCowers(FireDefinition definition) =>
            (definition.Nationality == "british" && definition.Class is "elite" or "1st-line") || (definition.Nationality == "finnish" && definition.Class != "conscript");

        private FireArithmetic? Arithmetic(bool hasKnown, bool hasConcealed)
        {
            var dice = attack.Rolls!.Attack;
            var residual = attack.FireKind == ResidualFire;
            var hit = attack.OrdnanceHit;
            var firers = new List<FirerFirepower>();
            decimal known = 0, vsConcealed = 0;

            // A22.611 (ruling R15.4): the MOL Check dr, made before the IFT DR: 1 to 3 passes, +1 for a HS or crew, +2 for a SMC, +1 when CX, and +1
            // against a non-AFV (always, here: an AFV target is not reviewed).
            if ((attack.Firers ?? []).FirstOrDefault(item => item.Mol == true) is { } molFirer && molCheck is null)
            {
                if (attack.Rolls!.MolCheck is not { } molDr)
                {
                    undecided.Add("asl.a1.fire.roll-missing:molCheck");
                    return null;
                }

                usedRolls.Add("molCheck");
                var user = reference.Definitions[molFirer.DefinitionId!];
                var molDrm = new List<FireModifier>();
                if (user.Kind is "asl:half-squad" or "asl:crew")
                {
                    molDrm.Add(new FireModifier("hs-or-crew", 1m, "A22.611"));
                }
                else if (user.IsHero || user.IsLeader)
                {
                    molDrm.Add(new FireModifier("smc", 2m, "A22.611"));
                }

                if (molFirer.Cx == true)
                {
                    molDrm.Add(new FireModifier("cx", 1m, "A22.611"));
                }

                molDrm.Add(new FireModifier("non-afv", 1m, "A22.611"));
                var molFinal = molDr + (int)molDrm.Sum(item => item.Value);
                // A22.6111: an Original colored dr of 6 on the IFT DR breaks the MOL's user and voids its FP and the MOL's.
                molCheck = new FireMolCheck(molFirer.UnitId!, molDr, molDrm, molFinal, molFinal <= 3, molFinal <= 3 && dice is [6, _]);
            }

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
            else if (attack.Overrun is { } overrun)
            {
                if (hasKnown)
                {
                    var fp = OverrunFirepower(overrun, false);
                    firers.AddRange(fp);
                    known = fp.Sum(item => item.Firepower);
                }

                if (hasConcealed)
                {
                    var fp = OverrunFirepower(overrun, true).Select(item => hasKnown ? item with { VsConcealed = true } : item).ToArray();
                    firers.AddRange(fp);
                    vsConcealed = fp.Sum(item => item.Firepower);
                }
            }
            else if (attack.DemolitionCharge is { } charge)
            {
                // A23.1 (rulings R15.2, R15.3): 30 FP, never modified for PBF, TPBF, or the AFPh; halved only as Area Fire for concealment when a
                // Thrown DC is thrown, or every target was concealed when a Placed one was operably Placed.
                var printed = reference.Definitions[charge.DefinitionId!].Firepower!.Value;
                var placedConcealed = charge.Mode == FireDemolitionCharge.Placed && charge.ConcealedWhenPlaced == true;
                FirerFirepower Charge(bool concealedTargets)
                {
                    List<FireModifier> multipliers = placedConcealed || (charge.Mode != FireDemolitionCharge.Placed && concealedTargets)
                        ? [new FireModifier("area-fire-concealed-target", 0.5m, "A23.1")] : [];
                    return new FirerFirepower(charge.EquipmentId!, printed, multipliers, multipliers.Aggregate((decimal)printed, (value, item) => value * item.Value));
                }

                if (hasKnown)
                {
                    firers.Add(Charge(false));
                    known = firers[^1].Firepower;
                }

                if (hasConcealed)
                {
                    var concealedFp = Charge(true);
                    firers.Add(hasKnown ? concealedFp with
                    {
                        VsConcealed = true
                    } : concealedFp);
                    vsConcealed = concealedFp.Firepower;
                }
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

                // A22.611 (ruling R15.4): a passed MOL adds four FP after every other modification, unless its user broke on the colored dr.
                if (molCheck is { Passed: true, UserBroken: false } bonus)
                {
                    if (hasKnown)
                    {
                        firers.Add(new FirerFirepower("mol:" + bonus.UnitId, 4, [], 4m) { Operator = bonus.UnitId });
                        known += 4;
                    }

                    if (hasConcealed)
                    {
                        firers.Add(new FirerFirepower("mol:" + bonus.UnitId, 4, [], 4m) { Operator = bonus.UnitId, VsConcealed = hasKnown ? true : null });
                        vsConcealed += 4;
                    }
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
            // A7.9: Cowering never affects a DC (ruling R15.2).
            var cowered = !residual && hit is null && attack.VehicleFire is null && attack.Overrun is null && attack.DemolitionCharge is null && dice[0] == dice[1] && !directed
                && attack.Firers!.Any(item => !reference.Definitions[item.DefinitionId!].IsHero && !reference.Definitions[item.DefinitionId!].IsLeader && item.Fanatic != true
                    && !NeverCowers(reference.Definitions[item.DefinitionId!]))
                && attack.FireLane != true;
            var inexperienced = (attack.Firers ?? []).Any(item => reference.Definitions[item.DefinitionId!].Class is "green" or "conscript");
            var shift = cowered ? (inexperienced ? 2 : 1) : 0;

            var drm = new List<FireModifier>();
            var tem = ScenarioA1FireReference.Tem[attack.TargetTerrain!];
            var flamethrower = Flamethrower;
            var dcPlaced = attack.DemolitionCharge?.Mode == FireDemolitionCharge.Placed;
            if (flamethrower)
            {
                // A22.2 (ruling R15.1): a FT attack takes no TEM of any kind.
            }
            else if (hit is { Area: true } && attack.TargetTerrain == "woods")
            {
                // B13.3, C3.71 (ruling R9.3): a mortar's hit in woods takes the -1 of Air Bursts instead of the +1, never reversed.
                drm.Add(new FireModifier("air-burst", -1m, "B13.3"));
            }
            else if (hit is { Area: true } && hit.CriticalHit != true && tem != 0)
            {
                // C3.331 (ruling R9.3): an Area Target Type hit takes its TEM on the Effects DR.
                drm.Add(new FireModifier("tem:" + attack.TargetTerrain, tem, "C3.331"));
            }
            else if (hit is { Area: true } && hit.CriticalHit != true && attack.CushionedOpenGround == true && attack.TargetTerrain == "open-ground")
            {
                // E3.62, E3.731 (rulings R16.12, R16.13): Mud or Deep Snow cushions HE in Open Ground: +1 TEM on an Area Target Type hit's Effects DR.
                drm.Add(new FireModifier("weather-cushion", 1m, "E3.62"));
            }
            else if (hit is not null)
            {
                // C.3: the TEM of an Infantry Target Type hit modifies its TH DR, not the Effects DR; C3.71: a Critical Hit reverses a
                // positive TEM into a negative Effects DRM (a Direct Fire hit has no Air Burst, B13.3).
                if (hit.CriticalHit == true && tem != 0)
                {
                    drm.Add(new FireModifier("critical-hit-tem:" + attack.TargetTerrain, -Math.Abs(tem), "C3.71"));
                }
            }
            else if (GunCrewAlone(attack) && attack.GunTarget is { } gunTarget && Math.Max(gunTarget.Emplaced == true ? 2 : 0, GunshieldFaces(attack, gunTarget) ? 2 : 0) > tem)
            {
                // C11.2, C11.5 (ruling R8.3): the crew takes the Emplacement TEM or its gunshield instead of a lower positive TEM, never both.
                drm.Add(gunTarget.Emplaced == true
                    ? new FireModifier("emplacement:" + gunTarget.GunId, 2m, "C11.2")
                    : new FireModifier("gunshield:" + gunTarget.GunId, 2m, "C11.5"));
            }
            else if (attack.SnapShot == true)
            {
                // A8.15 (ruling R10.13): a Snap Shot takes no TEM of the target hex.
            }
            else if (attack.HexsideTem is { Tem: > 0 } hexside && hexside.Tem > tem && !dcPlaced)
            {
                // B9.3, B9.31 (ruling R10.5): the wall or hedge TEM instead of a lower in-hex TEM, never both.
                drm.Add(new FireModifier(hexside.Terrain!, hexside.Tem!.Value, "B9.3"));
            }
            else if (tem != 0)
            {
                drm.Add(new FireModifier("tem:" + attack.TargetTerrain, tem, "A7.6"));
            }

            // B10.31 (ruling R10.4): Height Advantage, +1 TEM for a target with no other positive TEM.
            var heightAdvantage = hit is null && !flamethrower && attack.HeightAdvantage == true && drm.All(item => item.Value <= 0) && Cover(attack) is null
                && !(GunCrewAlone(attack) && attack.GunTarget is { } shieldedCrew && (shieldedCrew.Emplaced == true || GunshieldFaces(attack, shieldedCrew)));
            if (heightAdvantage)
            {
                drm.Add(new FireModifier("height-advantage", 1m, "B10.31"));
            }

            // B9.3, B10.31, A8.15: a positive wall or hedge TEM, Height Advantage, or a Snap Shot leaves no FFMO.
            var noFfmo = heightAdvantage || attack.SnapShot == true || drm.Any(item => item.Name is "wall" or "hedge");

            // D9.3, D10.3 (ruling R6.1): the +1 TEM of a wreck, a friendly AFV, or an abandoned enemy AFV, only where the terrain gives no
            // positive TEM and not against an attack from within the Location; a Critical Hit reverses it (C3.71).
            if (!flamethrower && Cover(attack) is { } cover)
            {
                if (hit is null || hit is { Area: true, CriticalHit: not true })
                {
                    drm.Add(new FireModifier("afv-cover:" + cover, 1m, "D9.3"));
                }
                else if (hit.CriticalHit == true)
                {
                    drm.Add(new FireModifier("critical-hit-tem:afv-cover:" + cover, -1m, "C3.71"));
                }
            }

            // C3.71 (referee, pass 8): a Critical Hit of Defensive First Fire keeps FFNAM and FFMO on its Effects DR.
            if (hit is { CriticalHit: true } && attack.Phase == "MPh" && attack.TargetMovement is { } movedBy)
            {
                if (movedBy.AssaultMovement != true)
                {
                    drm.Add(new FireModifier("ffnam", -1m, "C3.71"));
                }

                if (attack.TargetTerrain == "open-ground" && (attack.Los?.HindranceDrm ?? 0) == 0 && Cover(attack) is null && !noFfmo)
                {
                    drm.Add(new FireModifier("ffmo", -1m, "C3.71"));
                }
            }

            // A7.52: the worst Hindrance of the group's LOS applies to all of it; Residual FP has no LOS Hindrance but takes the SMOKE of
            // the target Location, which the game supplies as its LOS Hindrance (A8.2; ruling R6.6); an ordnance hit's Hindrance modifies its
            // TH DR (C.3, C6.9).
            var hindrance = residual ? attack.Los?.HindranceDrm ?? 0
                : hit is not null ? 0
                : attack.VehicleFire is not null || attack.Overrun is not null ? attack.Los!.HindranceDrm!.Value
                // A23.1 (ruling R15.2): a DC takes no LOS Hindrance.
                : attack.DemolitionCharge is not null ? 0
                : attack.Firers!.Max(item => LosOf(attack, item)!.HindranceDrm!.Value);
            // A9.222 (referee, pass 12): the SMOKE, grain, brush, and marsh a Fire Lane's LOS crosses cancel FFMO but apply no DRM.
            if (hindrance > 0 && attack.FireLane != true)
            {
                drm.Add(new FireModifier("los-hindrance", hindrance, "A6.7"));
            }

            // E1.7, E3.1, E3.32 (rulings R16.3, R16.11): the Low Visibility Hindrance DRM of night and weather, which never negates FFMO; an
            // ordnance hit takes it on its TH DR, and Residual FP never (the game supplies none for either).
            if (attack.LowVisibilityDrm is > 0 and var lowVisibility && !residual && hit is null && attack.DemolitionCharge is null && attack.FireLane != true)
            {
                drm.Add(new FireModifier("lv-hindrance", lowVisibility, "E1.7"));
            }

            // A7.7 (ruling R12.11): +1 when an Encircled unit fires in the group, once however many do (A7.52).
            if ((attack.Firers ?? []).FirstOrDefault(item => item.Encircled == true) is { } encircled)
            {
                drm.Add(new FireModifier("encircled:" + encircled.UnitId, 1m, "A7.7"));
            }

            // A4.51 (ruling R5.2): +1 when a CX unit makes or directs the attack, once however many do.
            if (((attack.Firers ?? []).FirstOrDefault(item => item.Cx == true)?.UnitId ?? Directors(attack).FirstOrDefault(item => item.Cx == true)?.UnitId
                ?? (attack.DemolitionCharge is { Cx: true } cxCharge ? cxCharge.UserId : null)) is { } exhausted)
            {
                drm.Add(new FireModifier("cx:" + exhausted, 1m, attack.DemolitionCharge is null ? "A4.51" : "A23.4"));
            }

            // A23.6, A23.62 (ruling R15.3): a Thrown DC is +2 at its target and +3 at its thrower's Location, each +1 more in the AFPh unless its
            // thrower is an Opportunity Firer.
            if (attack.DemolitionCharge is { Mode: not FireDemolitionCharge.Placed } thrown)
            {
                drm.Add(thrown.Mode == FireDemolitionCharge.Thrown ? new FireModifier("thrown-dc", 2m, "A23.6") : new FireModifier("thrower-location", 3m, "A23.6"));
                if (attack.Phase == "AFPh" && thrown.OpportunityFire != true)
                {
                    drm.Add(new FireModifier("thrown-dc-afph", 1m, "A23.62"));
                }
            }

            // A15.23 (ruling R15.11): a hero firing a MG adds one, as for a SW that needs two men.
            foreach (var hero in (attack.Firers ?? []).Where(item => reference.Definitions[item.DefinitionId!].IsHero
                && (item.Weapons ?? []).Any(weapon => reference.Definitions[weapon.DefinitionId!].IsMg)))
            {
                drm.Add(new FireModifier("hero-mg:" + hero.UnitId, 1m, "A15.23"));
            }

            // D5.34: a vehicle under Stun +1 adds one to its MG IFT DR.
            if (attack.VehicleFire is { StunRecovery: true } recovering)
            {
                drm.Add(new FireModifier("stun-recovery:" + recovering.VehicleId, 1m, "D5.34"));
            }

            // A7.531: the leadership of the directing leader, the worst of them for a group spanning Locations; A17.3: one
            // worse when wounded.
            // A10.7 (ruling R15.8): one worse when the group holds Allied Troops of another nationality than his.
            var leadership = Directors(attack)
                .Select(director => (director.UnitId, Value: reference.Definitions[director.DefinitionId!].Leadership!.Value + (director.Wounded == true ? 1 : 0)
                    + ((attack.Firers ?? []).Any(firer => AlliedPenalty(reference.Definitions[director.DefinitionId!], reference.Definitions[firer.DefinitionId!]) > 0) ? 1 : 0)))
                .OrderByDescending(item => item.Value).FirstOrDefault();
            if (leadership.UnitId is not null)
            {
                drm.Add(new FireModifier("leadership:" + leadership.UnitId, leadership.Value, "A7.531"));
            }

            // A15.24: each hero firing within its Normal Range (of his weapon when he fires one; ruling R15.11) lowers the group's DR by one, with any
            // leadership DRM; never for a FT (A15.24).
            foreach (var hero in (attack.Firers ?? []).Where(item => reference.Definitions[item.DefinitionId!].IsHero && !flamethrower
                && RangeOf(attack, item) <= (item.Weapons is [{ } weapon] ? reference.Definitions[weapon.DefinitionId!].Range!.Value
                    : NormalRange(reference.Definitions[item.DefinitionId!], item))))
            {
                drm.Add(new FireModifier("heroic:" + hero.UnitId, -1m, "A15.24"));
            }

            // A4.6, A4.61, A8.13: FFNAM unless Assault Movement, and FFMO in Open Ground with no Hindrance, in Defensive
            // First Fire only; A7.83: a pinned mover takes neither.
            // A4.62 (ruling R10.8): Hazardous Movement is -2 to any attack on the unpinned movers, with no FFMO or FFNAM; A8.15 (ruling R10.13):
            // a Snap Shot has neither.
            if (attack.HazardousMovement == true && state.Values.Any(unit => !unit.IsDummy && !unit.Pinned))
            {
                drm.Add(new FireModifier("hazardous-movement", -2m, "A4.62"));
            }
            else if (attack.Overrun is not null && attack.TargetTerrain == "open-ground" && state.Values.Any(unit => !unit.IsDummy))
            {
                // D7.15 (ruling R11.11): an OVR against Infantry in Open Ground takes FFMO, cumulative with its TEM and SMOKE, moving or not.
                drm.Add(new FireModifier("ffmo", -1m, "D7.15"));
            }
            else if (IsMovementFire(attack) && attack.SnapShot != true && state.Values.Any(unit => !unit.IsDummy && !unit.Pinned))
            {
                if (attack.TargetMovement!.AssaultMovement != true)
                {
                    drm.Add(new FireModifier("ffnam", -1m, "A4.6"));
                }

                if (attack.TargetTerrain == "open-ground" && hindrance == 0 && Cover(attack) is null && !noFfmo)
                {
                    drm.Add(new FireModifier("ffmo", -1m, "A4.6"));
                }
            }

            var final = original + (int)drm.Sum(item => item.Value);
            var main = hasKnown ? known : vsConcealed;
            var (column, shifted, result) = Column(main, shift, final);

            // A7.83 (ruling R12.3): the pinned movers of a stack that also holds unpinned ones take the DR without FFNAM, FFMO, or Hazardous Movement.
            var moving = (int)drm.Where(item => item.Name is "ffnam" or "ffmo" or "hazardous-movement").Sum(item => item.Value);
            var movers = state.Values.Where(unit => !unit.IsDummy).ToArray();
            int? pinnedFinal = attack.Overrun is null && moving != 0 && movers.Any(unit => unit.Target.Pinned == true) && movers.Any(unit => unit.Target.Pinned != true)
                ? final - moving : null;

            // C11.4 (ruling R8.3): an HE hit whose DR gives no KIA or K on the Gun is a Near Miss, and the crew's gunshield adds +2 to it.
            if (hit is { CriticalHit: false } && GunCrewAlone(attack) && attack.GunTarget is { } shielded && GunshieldFaces(attack, shielded)
                && !Regex.IsMatch(result, "^([1-7])?KIA$") && !Regex.IsMatch(result, "^K/([1-4])$"))
            {
                drm.Add(new FireModifier("gunshield:" + shielded.GunId, 2m, "C11.4"));
                final += 2;
                (column, shifted, result) = Column(main, shift, final);
            }
            FireColumn? second = null;
            if (hasKnown && hasConcealed && !residual && hit is null)
            {
                var (c2, s2, r2) = Column(vsConcealed, shift, final);
                second = new FireColumn(vsConcealed, c2, s2, r2);
            }

            // A23.4 (ruling R15.2): a DC's Original DR at its malfunction number removes it with no effect.
            if (attack.DemolitionCharge is { Mode: not FireDemolitionCharge.Thrower } exploded && original >= AssaultWeaponRemoval(reference.Definitions[exploded.DefinitionId!],
                reference.Definitions.GetValueOrDefault(exploded.UserDefinitionId ?? string.Empty), exploded.Captured == true, exploded.Inexperienced == true))
            {
                dcMalfunctioned = true;
                result = "none";
                second = second is null ? null : second with
                {
                    Result = "none"
                };
                pinnedFinal = null;
            }

            var arithmetic = new FireArithmetic(firers, main, column, shift, cowered, shifted, dice.ToArray(), original, drm, final, result)
            {
                Concealed = second,
                PinnedFinalDr = pinnedFinal,
                PinnedResult = pinnedFinal is { } pf ? Column(main, shift, pf).Result : null,
                PinnedConcealedResult = pinnedFinal is { } pc && second is not null ? Column(vsConcealed, shift, pc).Result : null,
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
            // A8.223 (referee, pass 10): a Snap Shot leaves no Residual FP.
            if (attack.FireKind is not (FirstFire or SubsequentFirstFire or FinalProtectiveFire) || attack.SnapShot == true)
            {
                return null;
            }

            // C13.24 (pass 9b): an ATR leaves no Residual FP, even in a fire group.
            // Referee, pass 9b: only the known targets' entry, not the halved one against concealed targets.
            var atr = arithmetic.Firers.Where(item => item.VsConcealed != true && (attack.Firers ?? []).SelectMany(firer => firer.Weapons ?? [])
                .Any(weapon => weapon.EquipmentId == item.UnitId && reference.Definitions.GetValueOrDefault(weapon.DefinitionId ?? string.Empty)?.IsAtr == true)).Sum(item => item.Firepower);
            var highest = atr == 0 ? Math.Max(arithmetic.ColumnFp ?? 0, arithmetic.Concealed?.ColumnFp ?? 0)
                : ScenarioA1FireReference.ColumnFp.Where(fp => fp <= arithmetic.TotalFirepower - atr).DefaultIfEmpty(0).Max();
            var index = Array.FindLastIndex(ResidualCounters, fp => fp <= highest / 2m);
            // B9.31 (ruling R10.5): a wall or hedge TEM claimed against the attack lowers the Residual FP left as a Hindrance does.
            index -= hindrance + Math.Max(leadership, 0) + (attack.HexsideTem?.Tem ?? 0)
                // E3.62, E3.731 (rulings R16.12, R16.13): an HE attack cushioned by Mud or Deep Snow in Open Ground leaves one counter less.
                + (attack.OrdnanceHit is not null && attack.CushionedOpenGround == true && attack.TargetTerrain == "open-ground" ? 1 : 0);
            return index < 0 ? null : ResidualCounters[index];
        }

        /// <summary>
        /// An OVR's FP (D7.11; ruling R11.11): a base of 1 for an unarmored vehicle, 2 for an AFV, or 4 for an AFV whose MA is a manned, functioning
        /// Gun; plus each MG that adds FP, tripled for TPBF and halved as Bounding First Fire; the whole halved when the vehicle became Immobile
        /// before the OVR resolved, and against concealed targets (A12.13); never for Motion.
        /// </summary>
        private IEnumerable<FirerFirepower> OverrunFirepower(FireOverrun overrun, bool vsConcealed)
        {
            var definition = reference.Definitions[overrun.DefinitionId!];
            var weapons = OverrunWeapons(overrun, definition);
            var common = new List<FireModifier>();
            if (overrun.Immobile == true)
            {
                common.Add(new FireModifier("immobile-before-ovr", 0.5m, "D7.11"));
            }

            if (vsConcealed)
            {
                common.Add(new FireModifier("area-fire-concealed-target", 0.5m, "A12.13"));
            }

            decimal Apply(decimal value, IEnumerable<FireModifier> multipliers) => multipliers.Aggregate(value, (total, item) => total * item.Value);
            var gun = weapons.Any(item => item.Weapon == FireOverrunEffect.MainArmament && item.Firepower == 0);
            var basis = definition.Unarmored == true ? 1 : gun ? 4 : 2;
            yield return new FirerFirepower(overrun.VehicleId! + ":base", basis, common, Apply(basis, common));
            foreach (var (weapon, printed) in weapons.Where(item => item.Firepower > 0))
            {
                List<FireModifier> multipliers = [new FireModifier("tpbf", 3m, "D7.11"), new FireModifier("bounding-fire", 0.5m, "D7.11"), .. common];
                yield return new FirerFirepower(overrun.VehicleId! + ":" + weapon, printed, multipliers, Apply(printed, multipliers));
            }
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
            else if (attack.BeyondNvr == true)
            {
                // E1.81 (ruling R16.2): as at a concealed target, halved once only.
                multipliers.Add(new FireModifier("area-fire-gunflash", 0.5m, "E1.81"));
            }

            if (attack.Phase == "AFPh")
            {
                multipliers.Add(new FireModifier("advancing-fire", 0.5m, "D3.53"));
            }

            // D3.31 (ruling R6.9): Bounding (First) Fire halves a vehicle's MG, and D2.42 halves it again while it is Non-Stopped.
            if (attack.FireKind == BoundingFirstFire)
            {
                multipliers.Add(new FireModifier("bounding-fire", 0.5m, "D3.31"));
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

                // A7.351, A7.352: a squad keeps its inherent FP with one SW, not two; a HS loses it with any; A15.23 (ruling R15.11): so does a hero.
                var inherent = firer.UsesInherentFp != false && !(definition.Kind == "asl:squad" && weapons.Count >= 2)
                    && !((definition.Kind == "asl:half-squad" || definition.IsHero) && weapons.Count >= 1);

                // A22.611, A22.6111 (ruling R15.4): a HS, crew, or SMC that fails its MOL Check has spent its attack; a MOL user broken by the colored dr
                // adds nothing.
                if (firer.Mol == true && molCheck is { } check && (check.UserBroken || (!check.Passed && !(definition.Kind == "asl:squad"))))
                {
                    yield return new FirerFirepower(firer.UnitId!, definition.Firepower!.Value,
                        [new FireModifier(check.UserBroken ? "mol-user-broken" : "mol-check-failed", 0m, check.UserBroken ? "A22.6111" : "A22.611")], 0m);
                    continue;
                }

                if (inherent)
                {
                    var multipliers = Multipliers(range, NormalRange(definition, firer), vsConcealed, IsFinalFireAgain(attack, firer), sustained: false, LevelAboveOf(attack, firer), firer);
                    if (firer.Pinned == true)
                    {
                        multipliers.Add(new FireModifier("pinned-firer", 0.5m, "A7.8"));
                    }

                    var printed = firer.Wounded == true && definition.WoundedFirepower is { } woundedFp ? woundedFp : definition.Firepower!.Value;
                    var fp = multipliers.Aggregate((decimal)printed, (value, item) => value * item.Value);

                    // A7.36: Assault Fire adds one FP after every other modification, rounded up, but not at Long Range or to Opportunity Fire (referee,
                    // pass 12).
                    if (attack.Phase == "AFPh" && definition.AssaultFire == true && range <= definition.Range && firer.OpportunityFire != true)
                    {
                        multipliers.Add(new FireModifier("assault-fire", 1m, "A7.36"));
                        fp = Math.Ceiling(fp + 1);
                    }

                    yield return new FirerFirepower(firer.UnitId!, printed, multipliers, fp);
                }

                foreach (var weapon in weapons)
                {
                    var mg = reference.Definitions[weapon.DefinitionId!];
                    var multipliers = Multipliers(range, mg.Range!.Value, vsConcealed, IsFinalFireAgain(attack, firer), IsSustained(attack, weapon, reference), LevelAboveOf(attack, firer), firer);
                    if (mg.IsFt)
                    {
                        // A22.1, A22.32 (ruling R15.1): never raised for PBF or TPBF, not halved in the AFPh, and halved at a target two levels away.
                        multipliers.RemoveAll(item => item.Name is "point-blank-fire" or "triple-point-blank-fire" or "advancing-fire");
                        if (Math.Abs(LevelAboveOf(attack, firer)) == 2)
                        {
                            multipliers.Add(new FireModifier("two-levels", 0.5m, "A22.32"));
                        }
                    }

                    // Pass 31 (play test R-05; ruling R31.3; A7.81): pinned Infantry fires its MG as Area Fire, as it fires its own FP.
                    if (attack.PinnedMgAreaFire == true && firer.Pinned == true && !mg.IsFt)
                    {
                        multipliers.Add(new FireModifier("pinned-firer", 0.5m, "A7.81"));
                    }

                    // A9.12 (ruling R12.4): a leader fires a MG alone as Area Fire; two SMC together fire it at full FP.
                    if (definition.IsLeader && firer.Partner is null)
                    {
                        multipliers.Add(new FireModifier("smc-area-fire", 0.5m, "A9.12"));
                    }

                    var fp = multipliers.Aggregate((decimal)mg.Firepower!.Value, (value, item) => value * item.Value);
                    yield return new FirerFirepower(weapon.EquipmentId!, mg.Firepower.Value, multipliers, fp) { Operator = firer.UnitId };
                }
            }
        }

        /// <summary>A firer's Normal Range: a wounded hero's is his wounded side's (A15.2).</summary>
        private static int NormalRange(FireDefinition definition, FireFirer firer) =>
            firer.Wounded == true && definition.WoundedRange is { } wounded ? wounded : definition.Range!.Value;

        private List<FireModifier> Multipliers(int range, int normalRange, bool vsConcealed, bool finalFireAgain, bool sustained, int levelAbove, FireFirer firer)
        {
            var multipliers = new List<FireModifier>();

            // A7.21 (rulings R10.4, R10.14): PBF at an adjacent target at most one level above the firer; TPBF in the firer's own Location.
            if (range == 1 && levelAbove <= 1)
            {
                multipliers.Add(new FireModifier("point-blank-fire", 2m, "A7.21"));
            }
            else if (range == 0)
            {
                multipliers.Add(new FireModifier("triple-point-blank-fire", 3m, "A7.21"));
            }

            // A8.15 (ruling R10.13): a Snap Shot is Area Fire.
            if (attack.SnapShot == true)
            {
                multipliers.Add(new FireModifier("snap-shot", 0.5m, "A8.15"));
            }

            if (range > normalRange)
            {
                multipliers.Add(new FireModifier("long-range-fire", 0.5m, "A7.22"));
            }

            if (vsConcealed)
            {
                multipliers.Add(new FireModifier("area-fire-concealed-target", 0.5m, "A7.23"));
            }
            else if (attack.BeyondNvr == true)
            {
                // E1.81 (ruling R16.2): fire at a Gunflash beyond the firer's NVR is as at a concealed target, halved once only.
                multipliers.Add(new FireModifier("area-fire-gunflash", 0.5m, "E1.81"));
            }

            // A8.3, A8.31, A8.4, A9.3: Subsequent First Fire, FPF, a First-Fire-marked unit's Final Fire, and Sustained Fire
            // are Area Fire; in a group mixing FPF with other fire, each firer's own kind decides (ruling R12.3).
            var ownKind = attack.FireKind == FinalProtectiveFire
                ? firer.FinalFireMarked == true ? FinalProtectiveFire : firer.FirstFireMarked == true ? SubsequentFirstFire : FirstFire
                : attack.FireKind;
            if (ownKind is SubsequentFirstFire or FinalProtectiveFire || finalFireAgain || sustained)
            {
                multipliers.Add(new FireModifier("area-fire", 0.5m, ownKind switch
                {
                    SubsequentFirstFire => "A8.3",
                    FinalProtectiveFire => "A8.31",
                    _ => finalFireAgain ? "A8.4" : "A9.3",
                }));
            }

            // A9.5 (ruling R12.6): Spraying Fire is Area Fire.
            if (attack.SprayingFire == true)
            {
                multipliers.Add(new FireModifier("spraying-fire", 0.5m, "A9.5"));
            }

            // A7.24, A7.25 (ruling R12.1): AFPh fire is halved, unless it is Opportunity Fire.
            if (attack.Phase == "AFPh" && firer.OpportunityFire != true)
            {
                multipliers.Add(new FireModifier("advancing-fire", 0.5m, "A7.24"));
            }

            return multipliers;
        }

        /// <summary>What the attack did to each MG: malfunction on the Original DR (A9.7, A9.71) and Multiple ROF on the colored die (A9.2).</summary>
        /// <summary>A19.32 (referee, pass 9b): an ATR's B# is one lower in Inexperienced hands; the MG's is in backlog section 19.</summary>
        private int AtrInexperience(FireWeapon weapon) =>
            reference.Definitions.GetValueOrDefault(weapon.DefinitionId ?? string.Empty)?.IsAtr == true
            && (attack.Firers ?? []).FirstOrDefault(firer => firer.Weapons?.Contains(weapon) == true) is { } operatorUnit
            && reference.Definitions.GetValueOrDefault(operatorUnit.DefinitionId ?? string.Empty)?.Class is "green" or "conscript" ? 1 : 0;

        private List<FireWeaponEffect>? WeaponEffects(FireArithmetic arithmetic, bool malfunction = true)
        {
            var weapons = (attack.Firers ?? []).SelectMany(item => item.Weapons ?? []).ToArray();
            if (weapons.Length == 0 || undecided.Count != 0)
            {
                return null;
            }

            var original = arithmetic.OriginalDr;
            // A22.3, A22.5 (ruling R15.1): a FT's removal number is ten, two lower for a non-elite user and two lower again when captured.
            var breakdown = weapons.ToDictionary(weapon => weapon.EquipmentId!,
                weapon => reference.Definitions[weapon.DefinitionId!] is { IsFt: true } ft
                    ? -(attack.BreakdownReduction ?? 0) + AssaultWeaponRemoval(ft, (attack.Firers ?? []).FirstOrDefault(firer => firer.Weapons?.Contains(weapon) == true) is { } user
                        ? reference.Definitions[user.DefinitionId!] : null, weapon.Captured == true,
                        (attack.Firers ?? []).FirstOrDefault(firer => firer.Weapons?.Contains(weapon) == true)?.Inexperienced == true)
                    : (reference.Definitions[weapon.DefinitionId!].Breakdown ?? 12) - (IsSustained(attack, weapon, reference) ? 2 : 0) - AtrInexperience(weapon)
                    - (weapon.Captured == true ? 2 : 0) - (attack.BreakdownReduction ?? 0), StringComparer.Ordinal);
            var reached = malfunction ? weapons.Where(weapon => original >= breakdown[weapon.EquipmentId!]).Select(weapon => weapon.EquipmentId!).ToArray() : [];
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
                var sustained = IsSustained(attack, weapon, reference);
                // A7.25 (ruling R12.1): only an Opportunity Firer's MG keeps Multiple ROF in the AFPh; A8.31 (ruling R12.3): an FPF firer's MG never does.
                var operatorUnit = (attack.Firers ?? []).FirstOrDefault(firer => firer.Weapons?.Contains(weapon) == true);
                // A7.81 (ruling R31.3): pinned Infantry uses no Multiple ROF.
                var retained = !malfunctioned.Contains(id) && !sustained && !(attack.PinnedMgAreaFire == true && operatorUnit?.Pinned == true)
                    && !(attack.FireKind == FinalProtectiveFire && operatorUnit?.FinalFireMarked == true)
                    && (attack.Phase != "AFPh" || operatorUnit?.OpportunityFire == true)
                    && reference.Definitions[weapon.DefinitionId!].RateOfFire is { } rof && colored <= rof - (weapon.Captured == true ? 1 : 0);
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

            var firers = attack.Firers!.Where(firer => firer.FinalFireMarked == true).Select(firer => new TargetState(
                new FireTarget(firer.UnitId, firer.DefinitionId, firer.LocationId, false, firer.Pinned, firer.Concealed, false, false, firer.Wounded == true, false)
                {
                    Elr = firer.Elr,
                    Fanatic = firer.Fanatic,
                    Inexperienced = firer.Inexperienced,
                    KnownEnemyInLos = firer.KnownEnemyInLos,
                    Captors = firer.Captors,
                },
                reference.Definitions[firer.DefinitionId!])
            {
                NoQuarter = attack.FiringSideNoQuarter == true,
            }).Concat(Directors(attack).Select(director => new TargetState(
                new FireTarget(director.UnitId, director.DefinitionId, director.LocationId, false, director.Pinned, director.Concealed, false, false,
                    director.Wounded == true, false),
                reference.Definitions[director.DefinitionId!])
            {
                NoQuarter = attack.FiringSideNoQuarter == true,
            })).ToArray();
            var leadership = arithmetic.Drm.Where(item => item.Name.StartsWith("leadership:", StringComparison.Ordinal)).ToList();
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
                // A Casualty MC falls only on the selected firer; the others take the NMC as failed. A8.31 (ruling R12.3): the directing leader's
                // leadership modifies every NMC but his own.
                MoraleOutcome(firer, dice, [.. leadership.Where(item => item.Name != "leadership:" + firer.Id)], "NMC", firingSide: true,
                    casualty: firer == casualty || original != 12);
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

        // Leaders check first, a Commissar before any (A25.221; ruling R15.6), then higher Morale Level first (A10.2), then the other units in the
        // declared order.
        private static TargetState[] CheckOrder(IEnumerable<TargetState> units)
        {
            var list = units.ToArray();
            return list.Where(unit => unit.Definition.IsLeader).OrderByDescending(unit => ScenarioA1FireReference.IsCommissar(unit.Definition.Id))
                .ThenByDescending(unit => unit.Definition.Morale)
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
            // Rulings R12.8, R12.9: a unit of the firing side in a Melee Location or as a prisoner checks against its own side's ELR.
            firingSide |= unit.Target.Friendly == true;
            var brokenBefore = unit.Broken;
            var morale = unit.MoraleLevel + CommissarBonus(unit);
            if (morale is null)
            {
                undecided.Add("asl.a1.fire.leaders-interact:broken-morale-unrecorded:" + unit.Id);
                return;
            }

            var original = dice[0] + dice[1];
            var final = original + (int)drm.Sum(item => item.Value);
            var passed = final <= morale && original != 12;
            string consequence;

            // A20.54 (ruling R12.9): a prisoner is never broken; one that fails a MC suffers Casualty Reduction, and it takes no Heat of Battle.
            if (unit.Target.GuardId is not null)
            {
                if (!passed && !Reduce(unit, "casualty-reduced-prisoner"))
                {
                    return;
                }

                consequence = passed ? "passed" : unit.Eliminated ? "eliminated" : "casualty-reduced";
                unit.Checks.Add(new FireCheck(kind, dice.ToArray(), original, drm, final, morale.Value, passed, consequence));
                return;
            }

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
                if (ElrOf(unit, firingSide) is not { } limit)
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
                if (ElrOf(unit, firingSide) is not { } limit)
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

        /// <summary>A1.23, A19.13 (referee, pass 15): a MMC with an underscored Morale Factor has an ELR of 5; any other unit its side's.</summary>
        private int? ElrOf(TargetState unit, bool firingSide)
        {
            // Ruling R18.3: a unit's own ELR, from its OB group, before its side's; a unit ELR never Replaces needs none (A19.11).
            var elr = unit.Target.Elr ?? (ElrImmune(unit.Definition.Id, reference) && (firingSide ? attack.FiringSideElr : attack.TargetSideElr) is null ? 5 : Elr(firingSide));
            return unit.Definition is { IsMmc: true, UnderscoredMorale: true } && elr is not null ? 5 : elr;
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
            // A19.11 (ruling R15.6): a Commissar or a crew is never Replaced for an ELR failure; it breaks.
            if (ScenarioA1FireReference.IsCommissar(unit.Definition.Id) || unit.Definition.Kind == "asl:crew")
            {
                unit.Note("not-subject-to-replacement");
                unit.Break(null);
                return;
            }

            // A19.13 (ruling R15.9): a squad with an underscored Morale Factor is Replaced by its two broken HS; such a HS is Disrupted instead,
            // unless Fanatic (A19.12).
            if (unit.Definition is { IsMmc: true, UnderscoredMorale: true })
            {
                if (unit.Definition.Kind == "asl:squad" && ScenarioA1FireReference.HalfSquadOf(unit.Definition.Id) is { } own)
                {
                    unit.ReduceTo(reference.Definitions[own], "replaced-by-two-half-squads");
                    unit.Split = true;
                }
                else if (!unit.Fanatic)
                {
                    unit.Disrupt();
                }
                else
                {
                    unit.Note("fanatic-not-disrupted");
                }

                unit.Break(null);
                return;
            }

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
                var lesser = ScenarioA1FireReference.CasualtyHalfSquadOf(half);
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
            // A15.2: a hero, or a heroic leader, is not subject to enforced Pin results, so takes no PTC; A20.54 (ruling R12.9): nor is a prisoner.
            foreach (var unit in CheckOrder(units).Where(unit => !unit.Eliminated && !unit.Broken && !unit.Pinned && !unit.IsHeroType && !unit.Berserk
                && unit.Target.GuardId is null))
            {
                var check = Check(unit, "NTC", "checks", 0, useLeadership: true);
                if (check is null)
                {
                    return;
                }

                var (dice, drm) = check.Value;
                var morale = unit.MoraleLevel!.Value + CommissarBonus(unit);
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

        private (IReadOnlyList<int> Dice, List<FireModifier> Drm)? Check(TargetState unit, string kind, string rolls, int modifier, bool useLeadership, string? key = null)
        {
            // Pass 31 (ruling R31.5): a unit's second and later Leader Loss checks in one attack have their own rolls, keyed apart from its first.
            key ??= unit.Id;
            var source = rolls == "checks" ? attack.Rolls!.Checks : attack.Rolls!.LeaderLoss;
            if (source?.TryGetValue(key, out var dice) != true)
            {
                undecided.Add($"asl.a1.fire.roll-missing:{rolls}:{key}");
                return null;
            }

            usedRolls.Add(rolls + ":" + key);
            var drm = new List<FireModifier>();
            if (modifier != 0)
            {
                drm.Add(new FireModifier("ift-" + kind.ToLowerInvariant(), modifier, "A7.304"));
            }

            // A25.221 (ruling R15.6): a Commissar never takes another leader's DRM.
            if (useLeadership && !unit.Berserk && !ScenarioA1FireReference.IsCommissar(unit.Definition.Id))
            {
                // A10.21, A10.22: one unbroken, unpinned leader of the Location other than the checker, and of higher
                // morale when the checker is a leader; A10.72: a non-zero modifier, a wounded leader's +1 included, cannot
                // be declined.
                // Rulings R12.8, R12.9: only a leader of the unit's own side, and not a prisoner.
                // A25.221 (ruling R15.6): an active Commissar's leadership alone applies in his Location.
                // Pass 31 (ruling R31.5): with several such leaders the owner chooses (A10.21), so the most favorable modifier is taken.
                var leader = state.Values.FirstOrDefault(other => ActiveCommissar(other, unit))
                    ?? state.Values.Where(other => other.Definition.IsLeader && other != unit && !other.Eliminated
                    && !other.Broken && !other.Pinned && !other.IsDummy && other.Target.Berserk != true
                    && (other.Target.Friendly == true) == (unit.Target.Friendly == true) && other.Target.GuardId is null
                    && (!unit.Definition.IsLeader || other.MoraleLevel > unit.MoraleLevel))
                    .OrderBy(other => (other.Leadership ?? 0) + AlliedPenalty(other.Definition, unit.Definition)).FirstOrDefault();

                // A10.7 (ruling R15.8): one worse for Allied Troops of another nationality.
                if (leader?.Leadership is { } leadership && leadership + AlliedPenalty(leader.Definition, unit.Definition) is var allied and not 0)
                {
                    drm.Add(new FireModifier("leadership:" + leader.Id, allied, AlliedPenalty(leader.Definition, unit.Definition) > 0 ? "A10.7" : "A10.21"));
                }
            }

            return (dice!, drm);
        }

        private void LeaderLoss()
        {
            // Referee, pass 31 (A10.2, A11.141): units in Melee neither take nor cause a LLMC or LLTC.
            if (undecided.Count != 0 || attack.TargetsInMelee == true)
            {
                return;
            }

            // Pass 31 (ruling R31.5; A10.2): every lost leader is taken in turn, one lost to another's LLMC included, the higher Morale Level first; a
            // leader that broke and is then eliminated is taken again for his LLMC.
            var taken = new HashSet<string>(StringComparer.Ordinal);
            var checksOf = new Dictionary<string, int>(StringComparer.Ordinal);
            while (state.Values.Where(unit => unit.Definition.IsLeader && (unit.Eliminated || unit.BrokeInThisAttack) && !taken.Contains(unit.Id + (unit.Eliminated ? ":e" : ":b")))
                .OrderByDescending(unit => unit.InitialMorale ?? 0).FirstOrDefault() is { } leader)
            {
                taken.Add(leader.Id + (leader.Eliminated ? ":e" : ":b"));
                var leaderMorale = leader.Eliminated ? leader.MoraleAtLoss : leader.InitialMorale;
                if (leaderMorale is null)
                {
                    undecided.Add("asl.a1.fire.leaders-interact:broken-morale-unrecorded:" + leader.Id);
                    return;
                }

                // A10.2: an eliminated leader causes LLMC; an unbroken leader that broke causes LLTC, which cannot pin a hero
                // or a heroic leader (A15.2).
                var eliminated = leader.Eliminated;
                // Rulings R12.8, R12.9: a leader's loss checks his own side's units only, and never a prisoner (A20.54).
                // A25.221 (ruling R15.6): a Commissar takes no LLMC or LLTC, and his own loss checks every other unit, whatever its Morale Level.
                var commissar = ScenarioA1FireReference.IsCommissar(leader.Definition.Id);
                foreach (var unit in state.Values.Where(unit => unit != leader && !unit.Eliminated && !unit.IsDummy && !unit.Berserk
                    && (unit.Target.Friendly == true) == (leader.Target.Friendly == true) && unit.Target.GuardId is null && leader.Target.GuardId is null
                    && !ScenarioA1FireReference.IsCommissar(unit.Definition.Id)
                    && (eliminated || (!unit.Broken && !unit.IsHeroType)) && (commissar || unit.MoraleLevel < leaderMorale)).ToArray())
                {
                    var nth = checksOf.GetValueOrDefault(unit.Id);
                    var check = Check(unit, eliminated ? "LLMC" : "LLTC", "leaderLoss", 0, useLeadership: false, key: nth == 0 ? unit.Id : $"{unit.Id}#{nth + 1}");
                    if (check is null)
                    {
                        return;
                    }

                    checksOf[unit.Id] = nth + 1;

                    // A10.7 (ruling R15.8): Allied Troops take it on the leader's reduced modifier.
                    var (dice, drm) = check.Value;
                    if (leader.Leadership + AlliedPenalty(leader.Definition, unit.Definition) is int negative and < 0)
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
                        var morale = unit.MoraleLevel!.Value + CommissarBonus(unit);
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
                .Concat(rolls.UnlikelyKill?.Keys.Select(id => "unlikelyKill:" + id) ?? [])
                .Concat(rolls.MolCheck is null ? [] : ["molCheck"]);
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

            // Pass 31d (ruling R31d.2): the planner's read decides, for each unit, when the attack carries it for every concealed unit: a Good
            // Order enemy ground unit within 16 hexes has a LOS to it, and its "?" is lost, or none has, and it keeps it. The loss is taken as forced
            // whenever such a unit sees (A12.14 leaves a concealed viewer the choice). The read is asked first (the referee, pass 31d): the target
            // Location's units are no measure of it when one of them is hidden, a prisoner of the firing side, or a unit of it in a Melee.
            var read = ConcealedFirers(attack).ToArray();
            if (read.All(item => item.Seen is not null))
            {
                return [.. read.Where(item => item.Seen == true).Select(item => item.UnitId)];
            }

            // A12.14, for an attack recorded without the read: the package sees only the target Location, so it decides only when one of its
            // units was unbroken and no Dummy when the attack was made, as it did before the read was given.
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

        /// <summary>Whether an underscored squad was Replaced by two broken HS (A19.13; ruling R15.9).</summary>
        public bool Split
        {
            get; set;
        }

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
            target.Berserk == true) - (Encircles(target) ? 1 : 0);

        /// <summary>
        /// The current Morale Level: one lower when wounded (A17.3); a hero's is printed on his wounded side and never lowered
        /// (A15.2); a heroic leader's is at least 9 (A15.21: a 10-2 is a 1-4-10 hero); a Fanatic unit's is one higher (A10.8);
        /// a hero's or heroic leader's never exceeds 10, or 9 if wounded (A15.2); a berserk unit's is 10, never lowered, and one
        /// higher when Fanatic (A15.42, ruling R30.3).
        /// </summary>
        public int? MoraleLevel => Morale(Definition, Broken, Wounded, Fanatic, Heroic, Berserk) - (Encircles(Target) && !Berserk && !Heroic ? 1 : 0);

        /// <summary>A7.7 (ruling R12.11): an Encircled unit's Morale Level is one lower, unless it is berserk or heroic.</summary>
        private static bool Encircles(FireTarget target) => target.Encircled == true && target.Berserk != true && target.Heroic != true;

        private static int? Morale(FireDefinition definition, bool broken, bool wounded, bool fanatic, bool heroic, bool berserk)
        {
            if (berserk)
            {
                // A15.42: a berserk unit's Morale Level is 10. A.18 (pass 35, task 35.3): Fanaticism does not raise it to 11, as ruling R30.3 had it.
                return ScenarioA1Definitions.MoraleCeiling(10 + (fanatic ? 1 : 0));
            }

            int? level = definition.IsHero ? (wounded ? definition.WoundedMorale : definition.Morale)
                : definition.IsLeader && heroic ? (definition.Morale is { } leader ? Math.Max(leader, 9) - (wounded ? 1 : 0) : null)
                : (broken ? definition.BrokenMorale : definition.Morale) - (wounded ? 1 : 0);
            level += fanatic ? 1 : 0;
            if (level is { } value && (definition.IsHero || (definition.IsLeader && heroic)))
            {
                return Math.Min(value, wounded ? 9 : 10);
            }

            // A.18 (pass 35, task 35.3): never beyond 10.
            return level is { } raised ? ScenarioA1Definitions.MoraleCeiling(raised) : level;
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
            SplitIntoHalfSquads = Split && !Eliminated && Definition.Kind == "asl:half-squad" ? true : null,
        };
    }
}
