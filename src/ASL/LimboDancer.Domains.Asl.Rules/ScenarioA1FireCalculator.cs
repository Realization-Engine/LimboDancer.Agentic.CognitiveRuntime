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
        => ResolveCore(attack, reference, null);

    /// <summary>D3.7/D3.71 opt-in vehicle breakdown reading. The recorded FireAttack and the legacy entry point remain unchanged.</summary>
    public static FireResolution Resolve(FireAttack attack, ScenarioA1FireReference reference, FireVehicleBreakdownFacts vehicleBreakdown)
    {
        ArgumentNullException.ThrowIfNull(vehicleBreakdown);
        ArgumentNullException.ThrowIfNull(vehicleBreakdown.Weapons);
        return ResolveCore(attack, reference, vehicleBreakdown);
    }

    private static FireResolution ResolveCore(FireAttack attack, ScenarioA1FireReference reference, FireVehicleBreakdownFacts? vehicleBreakdown)
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

        if (vehicleBreakdown is not null)
        {
            var use = attack.Overrun is not null ? VehicleBreakdownUse.Overrun : VehicleBreakdownUse.MachineGun;
            foreach (var facts in vehicleBreakdown.Weapons.Values)
            {
                if (ScenarioA1VehicleBreakdownResolutionCalculator.FactsRefusal(facts, use) is { } refusal)
                {
                    return Refused(FireResolution.Abstained, ["asl.a1.fire." + refusal]);
                }
            }
            var names = attack.VehicleFire is { } vehicle
                ? vehicle.Weapons is { Count: > 0 } named ? named.Select(item => item.Weapon!).ToArray() : [FireOverrunEffect.MainArmament]
                : attack.Overrun is { } overrun ? OverrunWeapons(overrun, reference.Definitions[overrun.DefinitionId!]).Select(item => item.Weapon).ToArray() : null;
            if (names is null || names.Length != vehicleBreakdown.Weapons.Count || names.Any(name => !vehicleBreakdown.Weapons.ContainsKey(name))
                || vehicleBreakdown.Weapons.Any(item => item.Value is null || item.Value.Weapon != (item.Key switch
                {
                    FireOverrunEffect.MainArmament => VehicleBreakdownWeapon.MainArmament,
                    FireOverrunEffect.BowMg => VehicleBreakdownWeapon.BowMg,
                    FireOverrunEffect.CoaxialMg => VehicleBreakdownWeapon.CoaxialMg,
                    _ => (VehicleBreakdownWeapon)(-1),
                })))
            {
                return Refused(FireResolution.Indeterminate, ["asl.a1.fire.vehicle-breakdown: supply exactly the participating vehicle weapons"]);
            }
            if (vehicleBreakdown.Weapons.Values.Any(facts => facts.IntensiveFire
                || facts.WinterReduction != (attack.BreakdownReduction ?? 0)
                || facts.AmmunitionShortage != (attack.AmmunitionShortage == true)
                || attack.VehicleFire is { } firingVehicle
                    && ScenarioA1NonQualifiedUseCalculator.Multiple(facts.Captured, facts.NonQualified) != (firingVehicle.CapturedPenalty ?? 0)))
            {
                return Refused(FireResolution.Abstained, ["asl.a1.fire.vehicle-breakdown-facts-conflict"]);
            }
        }

        return new Resolution(attack, reference, vehicleBreakdown).Run();
    }

    /// <summary>
    /// The check-only branch of the calculator (pass 70, ruling R70.3) that <see cref="ScenarioA1MoraleCheck"/> calls with facts it has checked: the
    /// subjects and the other units of the Location are the attack's targets, with the rolls, the choices, the side's ELR, No Quarter, and Melee.
    /// </summary>
    internal static FireResolution ResolveCheck(MoraleCheckFacts facts, ScenarioA1FireReference reference)
    {
        var attack = new FireAttack(null, null, null, null, null, null, null, null, null, null, null, null, [.. facts.Subjects!, .. facts.Others ?? []], facts.TargetSideElr,
            facts.Rolls)
        {
            TargetSideNoQuarter = facts.NoQuarter,
            TargetsInMelee = facts.InMelee,
            Choices = facts.Choices,
        };
        return new Resolution(attack, reference).RunCheck(facts);
    }

    /// <summary>
    /// A weapon's B# as the recorded attack lowered it, in words for its record line (A9.7, A.11; pass 72, the reviews R3.3): the number and each cause, "B10
    /// (B11, less 1 for Ammunition Shortage, A19.131)", or for a weapon the attack removed its X#, "X11 (B11, lowered to B10: ...)". Null for a weapon the
    /// attack did not use, a FT, and a B# as printed. It reads the record's facts only, as the resolution did.
    /// </summary>
    public static string? BreakdownWords(FireAttack attack, FireWeaponEffect effect, ScenarioA1FireReference reference)
    {
        ArgumentNullException.ThrowIfNull(attack);
        ArgumentNullException.ThrowIfNull(effect);
        ArgumentNullException.ThrowIfNull(reference);
        return (attack.Firers ?? []).SelectMany(firer => firer.Weapons ?? []).FirstOrDefault(weapon => weapon.EquipmentId == effect.EquipmentId) is { } weapon
            ? new Resolution(attack, reference).BreakdownWords(weapon, effect.Removed == true) : null;
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
        && (definition.IsHero || ScenarioA1Definitions.IsCrew(definition.Kind) || ScenarioA1FireReference.IsCommissar(id));

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
        // A19.11 (pass 70, ruling R70.7): nor does an Unarmed target.
        if (attack.TargetSideElr is null && attack.Targets!.Any(item => item.Dummy != true && item.Elr is null && item.Unarmed != true && !ElrImmune(item.DefinitionId, reference)))
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
                && ScenarioA1HeatOfBattle.Subject(reference.Definitions[pair.item.DefinitionId!], pair.item.Heroic == true, pair.item.Berserk == true, pair.item.Unarmed == true)))
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

    // A8.4: in the DFPh, a unit already marked First Fire fires again as Area Fire at an adjacent or same-hex target. It reads the unit's own counter,
    // the right one for the unit's inherent FP (the A8.41 claim, ruling RA8.2); the two sites that read it for the unit's weapons keep their answers.
    private static bool IsFinalFireAgain(FireAttack attack, FireFirer firer) => ScenarioA1FinalFireCalculator.FiresAgain(attack.Phase == "DFPh", firer.FirstFireMarked == true);

    // A9.3: a MG firing as Subsequent First Fire or FPF, or in the DFPh while marked First Fire, uses Sustained Fire (the A8.41 claim: the same
    // answer on every input, from the Final Fire calculator). A9.3 (referee, pass 9b): only a MG uses Sustained Fire.
    private static bool IsSustained(FireAttack attack, FireWeapon weapon, ScenarioA1FireReference reference)
    {
        var subsequent = attack.FireKind is SubsequentFirstFire or FinalProtectiveFire
            && ((attack.Firers ?? []).FirstOrDefault(firer => firer.Weapons?.Contains(weapon) == true) is not { } user || UsesSubsequentFire(attack, user));
        var dfph = attack.Phase == "DFPh";
        var marked = weapon.FirstFireMarked == true;
        // The definition is read only when a case applies, as before the claim (the build review, finding 9).
        var isMg = subsequent || (dfph && marked) ? reference.Definitions.GetValueOrDefault(weapon.DefinitionId ?? string.Empty)?.IsMg : false;
        return ScenarioA1FinalFireCalculator.Sustained(subsequent, dfph, marked, isMg);
    }

    /// <summary>
    /// A firer's own kind in an attack of Subsequent First Fire or FPF, by its own counter (A8.3, A8.31, pp. 61 to 62; pass 45, ruling R45.1): "A unit
    /// using FPF may form a FG with units not using FPF, but only those units using FPF are affected by its adverse affects". In any other attack
    /// it is the attack's kind.
    /// </summary>
    private static string? OwnKind(FireAttack attack, FireFirer firer) => attack.FireKind is SubsequentFirstFire or FinalProtectiveFire
        ? firer.FinalFireMarked == true ? FinalProtectiveFire : firer.FirstFireMarked == true ? SubsequentFirstFire : FirstFire
        : attack.FireKind;

    /// <summary>
    /// Whether a firer of a Subsequent First Fire or FPF attack uses that fire itself (A8.3): it is marked, or a MG it fires is marked First Fire.
    /// "Whenever a unit uses Subsequent First Fire, it must use all MG/IFE in its possession ... as Subsequent First Fire", and "that unit and all
    /// its SW/Guns are marked with a Final Fire counter"; a unit with no counter beside it fires and is marked as in First Fire.
    /// </summary>
    private static bool UsesSubsequentFire(FireAttack attack, FireFirer firer) => attack.FireKind is SubsequentFirstFire or FinalProtectiveFire
        && (firer.FirstFireMarked == true || firer.FinalFireMarked == true || (firer.Weapons ?? []).Any(weapon => weapon.FirstFireMarked == true));

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
            foreach (var (weapon, index) in (vehicle.Weapons ?? []).Select((item, index) => (item, index)))
            {
                Need(weapon.Weapon, $"vehicleFire.weapons[{index}].weapon");
                Need(weapon.Malfunctioned, $"vehicleFire.weapons[{index}].malfunctioned");
                Need(weapon.Fired, $"vehicleFire.weapons[{index}].fired");
                Need(weapon.Turn, $"vehicleFire.weapons[{index}].turn");
            }

            Need(attack.FirerLocationId, "firerLocationId");
            Need(attack.Range, "range");
            Need(attack.SameLevel, "sameLevel");
            NeedLos(attack.Los, string.Empty);
        }
        else if (attack.Canister is { } canister)
        {
            // C8.4 (pass 55): a Canister shot's attack on one Location, from the Gun's Location along its LOS.
            Need(canister.GunId, "canister.gunId");
            Need(canister.Firepower, "canister.firepower");
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

        // A22.34, A22.612, A23.5 (pass 60, ruling R60.15): the AFV a FT, MOL, or DC attack names, with the state facts its To Kill and Position DR read.
        if (attack.TargetVehicle is { } named)
        {
            Need(named.VehicleId, "targetVehicle.vehicleId");
            Need(named.DefinitionId, "targetVehicle.definitionId");
            Need(named.Moving, "targetVehicle.moving");
            Need(named.NonStopped, "targetVehicle.nonStopped");
            Need(named.Concealed, "targetVehicle.concealed");
            Need(named.CrewImpaired, "targetVehicle.crewImpaired");
            Need(named.CrewMayTakeTc, "targetVehicle.crewMayTakeTc");
        }

        // Ruling R10.4: a target at another level names how many levels above the firer it is, for PBF (A7.21).
        if (attack.VehicleFire is null && attack.OrdnanceHit is null && attack.Overrun is null && attack.DemolitionCharge is null && attack.Canister is null && !residual)
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

        // C2.6, A7.21 (pass 55): a vehicle's MG at a target at another level names the levels too, for the range it needs and for PBF.
        if (attack.VehicleFire is not null && attack.Overrun is null && attack.SameLevel == false)
        {
            Need(attack.TargetLevelAbove, "targetLevelAbove");
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

            // D3.3, C5.3 (pass 50): the hit of a vehicle's MA fired in its own MPh as Bounding First Fire.
            ("MPh", "phasing", null) => attack.OrdnanceHit is not null,
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
        // B10.31 (pass 55): a vehicle's MG fire is Direct Fire too, and its target above it claims Height Advantage.
        var direct = kind != ResidualFire && attack.OrdnanceHit is null && attack.VehicleFire is null;
        if ((attack.HexsideTem is { } wall && (!direct || wall.Terrain is not ("wall" or "hedge") || wall.Tem < 0 || wall.Tem > (wall.Terrain == "wall" ? 2 : 1)))
            || (attack.HeightAdvantage == true && ((!direct && attack.VehicleFire is null) || attack.Overrun is not null))
            || (attack.SnapShot == true && (!direct || attack.Overrun is not null || phase != "MPh" || kind is not (FirstFire or SubsequentFirstFire) || attack.HexsideTem is not null)))
        {
            outside.Add("asl.a1.fire.terrain-fact-outside");
        }

        var targets = attack.Targets!;
        var targetDefinitions = targets.Select(item => item.Dummy == true ? DummyDefinition : reference.Definitions.GetValueOrDefault(item.DefinitionId!))
            .ToArray();

        // A7.307, A7.308, D.8B: the vehicles in the target Location are attacked with the attack's IFT DR (unit step 25): one at a time
        // (ruling R25.3 keeps a Location to one vehicle for an attack made before pass 65, so the A7.308 limit binds only since), by Residual FP too (A8.2,
        // A8.222; ruling R6.6), by an ordnance hit only as below (the Vehicle Target Type is its own shot, R7.2): an unarmored vehicle, or an
        // AFV open-topped or closed-topped, whose CE crew alone is Vulnerable (R25.1, R7.11). An AFV in terrain with a positive TEM (not
        // cumulative with its crew's CE DRM, D5.31) is not reviewed (ruling R25.6).
        // C3.32, A7.308 (pp. 169, 56; pass 60, ruling R60.8): an Infantry Target Type hit attacks every unarmored vehicle it hit on the Vehicle line,
        // and the Vulnerable crew of each AFV whose own TH# it reached with the CE DRM alone (D5.31: not cumulative with a positive TEM).
        // C3.332, C1.55 (pp. 170, 165; pass 60, ruling R60.10): an Area Target Type hit attacks its unarmored vehicles on the Vehicle line and the
        // Vulnerable crews of its AFVs alike; the Ordnance package resolves each AFV itself on the IFT.
        // D5.31 (pass 60, ruling R60.15): the CE crew of an AFV a FT, MOL, or DC names takes the CE DRM alone, in a positive TEM too.
        // D5.31 (p. 203; pass 65, ruling R65.7): so does the crew of an AFV that carries the pass's fact of its PRC under any other IFT attack, which lifts
        // ruling R25.6's refusal; each PRC names a unit of the catalog, as a Passenger or a Rider.
        var vehicles = attack.Vehicles ?? [];
        var ordnanceHit = attack.OrdnanceHit is not null;
        // A7.308 (p. 56; pass 65, ruling R65.26): an attack that carries every vehicle's PRC meets every vehicle of the Location, Random Selection picking
        // those the DR affects; an older record keeps ruling R25.3's one vehicle.
        if ((vehicles.Count > 1 && !ordnanceHit && vehicles.Any(item => item.Prc is null))
            || (!ordnanceHit && attack.TargetVehicle is null
                && (attack.TargetTerrain is { } vehicleTerrain && ScenarioA1FireReference.Tem.TryGetValue(vehicleTerrain, out var vehicleTem) && vehicleTem > 0)
                && vehicles.Any(item => item.Prc is null && reference.Definitions.GetValueOrDefault(item.DefinitionId!) is { IsVehicle: true, Unarmored: not true }))
            || vehicles.Any(item => item.LocationId != attack.TargetLocationId
                || reference.Definitions.GetValueOrDefault(item.DefinitionId!) is not { IsVehicle: true })
            || vehicles.Any(item => PrcMalformed(item.Prc, reference)))
        {
            outside.Add("asl.a1.fire.vehicle-outside");
        }

        // A22.34, A22.612, A23.5 (pp. 89 to 91; pass 60, ruling R60.15): only a FT, a MOL, or a Placed or Thrown DC names an AFV, an enemy armored vehicle
        // in the target Location; its Target Facing is that of the hexside crossed, and none from within its Location (the rear). A Location that also
        // holds concealed or hidden units or Dummies is not built: the record of an attack on the AFV would name them.
        if (attack.TargetVehicle is { } named)
        {
            var assault = (attack.Firers ?? []).Any(item => item.Mol == true)
                || (attack.Firers ?? []).SelectMany(item => item.Weapons ?? []).Any(weapon => reference.Definitions.GetValueOrDefault(weapon.DefinitionId ?? string.Empty)?.IsFt == true)
                || attack.DemolitionCharge is { Mode: FireDemolitionCharge.Placed or FireDemolitionCharge.Thrown };
            var within = attack.FirerLocationId == attack.TargetLocationId;
            if (!assault || attack.OrdnanceHit is not null || attack.VehicleFire is not null || attack.Overrun is not null || attack.Canister is not null
                || reference.Armor.GetValueOrDefault(named.DefinitionId!) is not { Unarmored: false, FrontAf: not null } armor
                || FiringNationalities(attack, reference).Contains(armor.Nationality)
                || !vehicles.Any(item => item.VehicleId == named.VehicleId && item.DefinitionId == named.DefinitionId)
                || (within ? named.HullFacing is not null : named.HullFacing is not ("front" or "side" or "rear"))
                || named.TurretFacing is not (null or "front" or "side" or "rear") || PrcMalformed(named.Prc, reference))
            {
                outside.Add("asl.a1.fire.target-vehicle-outside");
            }

            // A12.3, ruling R21.1 (pass 60, the table player): only the "?" the firer sees bars the naming; a hidden unit is attacked silently and
            // gives itself away by no refusal.
            if (targets.Any(item => (item.Concealed == true || item.Dummy == true) && item.Hidden != true))
            {
                outside.Add("asl.a1.fire.target-vehicle-concealed");
            }
        }

        // C8.4, C8.41 (p. 178; pass 55, ruling R55.9): a Canister shot's attack on one Location has no firers, leader, or fire group (A7.51), the
        // FP of a Gun Size, and a Location at the firer's elevation and in its LOS; it attacks no unit of its own side (A7.4) and is no Snap
        // Shot (A8.15), Spraying Fire, or Fire Lane. In the MPh it is not built.
        if (attack.Canister is { } canister)
        {
            if (attack.Firers is { Count: > 0 } || attack.Director is not null || attack.OtherDirectors is { Count: > 0 } || kind is not null
                || attack.OrdnanceHit is not null || attack.VehicleFire is not null || attack.Overrun is not null || attack.DemolitionCharge is not null
                || canister.Firepower is not (12 or 20 or 24) || canister.AdjacentHexes is not (null or 1 or 2) || canister.CaseA is < 0
                || attack.SameLevel != true || attack.Los!.Blocked == true || attack.SnapShot == true || attack.SprayingFire == true || attack.SprayShare == true
                || attack.FireLane == true || attack.HeightAdvantage == true || attack.MgBoreSighted == true)
            {
                outside.Add("asl.a1.fire.canister-outside");
            }

            if (targets.Any(item => item.LocationId != attack.TargetLocationId || item.Friendly == true) || targetDefinitions.Any(item => item is null)
                || !ScenarioA1FireReference.Tem.ContainsKey(attack.TargetTerrain!))
            {
                outside.Add("asl.a1.fire.target-outside");
            }

            if (attack.Rolls is { } canisterRolls && Malformed(canisterRolls))
            {
                outside.Add("asl.a1.fire.roll-malformed");
            }

            return outside.Distinct(StringComparer.Ordinal).ToList();
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

            if (GunCrewUnreviewed(attack, targets, targetDefinitions))
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
                && (!(pair.Second.IsMmc || pair.Second.IsHero || ScenarioA1Definitions.IsCrew(pair.Second.Kind)) || pair.Second.Firepower is null || pair.Second.Range is null)))
            || firers.Any(item => item.Partner is not null && !LeaderMg(item, reference.Definitions.GetValueOrDefault(item.DefinitionId!)))
            // A7.352, A7.353 (p. 56; ruling R8.4): a crew that fired its Gun this Player Turn has no inherent FP, but "Subsequent First Fire (8.3),
            // FPF (8.31), and Final Fire (8.4) vs adjacent units retain halved inherent FP": its own fire of those kinds, which is Area Fire.
            || firers.Any(item => item.UsesInherentFp != false && !ScenarioA1MandatoryFire.CrewAddsInherentFp(item.GunFired == true,
                OwnKind(attack, item) is FinalProtectiveFire or SubsequentFirstFire || IsFinalFireAgain(attack, item))))
        {
            outside.Add("asl.a1.fire.firer-outside");
        }

        // A7.54 (p. 57; pass 45): "A berserk unit may never be part of a multi-Location FG."
        if (multi && firers.Any(item => item.Berserk == true))
        {
            outside.Add("asl.a1.fire.berserk-group-outside");
        }

        if (firerDefinitions.Any(item => item is not null && !sides.Contains(item.Nationality)))
        {
            outside.Add("asl.a1.fire.firers-of-two-sides");
        }

        // A9.1, A7.35 to A7.352: MGs of the firer's own side, not malfunctioned; a squad fires at most two, a HS one.
        foreach (var (firer, definition) in firers.Zip(firerDefinitions))
        {
            var weapons = firer.Weapons ?? [];
            // A15.23 (ruling R15.11): a hero fires one MG or FT; C13.24, C13.25 (p. 183; pass 72, ruling R72.8): or an ATR as Small Arms, "as if he were a
            // leader", with A15.24's heroic DRM and no hero-mg DRM (it is no MG).
            if (definition is not null && (weapons.Count > (definition.Kind == "asl:squad" ? 2 : 1)
                || weapons.Any(weapon => reference.Definitions.GetValueOrDefault(weapon.DefinitionId!) is not { Firepower: not null, Range: not null } mg || !(mg.IsMg || mg.IsAtr || mg.IsFt)
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
        // vehicle in the target Location; a squad adds its inherent FP and fires no SW, a HS, crew, or hero no SW. A22.612 (pass 60, ruling R60.15): the
        // vehicle it names (a MOL at an unarmored vehicle is not built).
        var mol = firers.Where(item => item.Mol == true).ToArray();
        if (mol.Length > 1 || mol.Any(item => item.Pinned == true || RangeOf(attack, item) > 1 || item.UsesInherentFp == false || item.Weapons is { Count: > 0 }
            || kind is SubsequentFirstFire or FinalProtectiveFire || flamethrower || ((attack.Vehicles ?? []).Count > 0 && attack.TargetVehicle is null)
            || reference.Definitions.GetValueOrDefault(item.DefinitionId ?? string.Empty) is not { } molUser || !(molUser.IsMmc || molUser.IsHero || ScenarioA1Definitions.IsCrew(molUser.Kind))))
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
        var subsequentMembers = kind is SubsequentFirstFire or FinalProtectiveFire
            ? firers.Zip(firerDefinitions).Where(pair => pair.First.FinalFireMarked != true && UsesSubsequentFire(attack, pair.First)).ToArray() : [];

        // A8.3 (pass 45): an attack of Subsequent First Fire has a firer that uses it.
        if (kind == SubsequentFirstFire && subsequentMembers.Length == 0)
        {
            outside.Add("asl.a1.fire.subsequent-first-fire-outside");
        }

        if (subsequentMembers.Length > 0 && (attack.WithinSubsequentFirstFireRange != true
            || subsequentMembers.Any(pair => pair.Second is { Range: { } range } && RangeOf(attack, pair.First) > range)))
        {
            outside.Add("asl.a1.fire.subsequent-first-fire-outside");
        }

        // A9.3 (p. 63; pass 45): "Sustained Fire cannot be used ... by a MG fired by a lone SMC." A leader with no partner fires no MG in Sustained
        // Fire; an attack recorded before the pass stands.
        if (attack.CoweringMarksAndWeaponResidual == true && firers.Zip(firerDefinitions).Any(pair => (pair.Second is { IsLeader: true } || pair.Second is { IsHero: true }) && pair.First.Partner is null
            && (pair.First.Weapons ?? []).Any(weapon => IsSustained(attack, weapon, reference))))
        {
            outside.Add("asl.a1.fire.weapon-outside");
        }

        // A8.4: a First-Fire-marked unit fires again in the DFPh only at an adjacent or same-hex target.
        // Keeps its answer (the A8.41 claim, ruling RA8.6): ScenarioA1FinalFireCalculator.FinalFireRefusal replaces it and differs. This reads the
        // unit's counter for its weapons too, so a First-Fire-marked MG held by an unmarked unit fires here at any range, and an unmarked MG fired
        // alone by a First-Fire-marked unit is refused beyond range 1; the calculator reads each weapon's own counter, and an unknown range is not
        // in reach there (here it passes).
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
                // A7.53 (pass 45): he directs the later shots of a MG he has directed this phase, when nothing but those MGs fires.
                // A7.53 (review of pass 45; the example of p. 62: "leader direction is NA; 7.53"): his later direction "can only include firers he
                // directed during First Fire", which a unit with no counter in a mixed group is not.
                || (director.DirectedThisPlayerTurn == true && kind is SubsequentFirstFire or FinalProtectiveFire && firers.Any(item => !UsesSubsequentFire(attack, item)))
                || (director.DirectedThisPlayerTurn == true && !redirect
                    && !(director.DirectedTheseMgs == true && firers.All(item => item.UsesInherentFp == false && item.Weapons is { Count: > 0 }))))
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

        // C11 (ruling R8.3): a Gun's crew is a target alone in its Location; with other units, or with no Gun named, it is not reviewed. A1.123 (p. 44;
        // pass 65, ruling R65.28): a crew that mans no Gun is an ordinary Infantry target, alone or with other units.
        if (GunCrewUnreviewed(attack, targets, targetDefinitions))
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
        // Pass 45 (A7.34, A9.52, pp. 56 and 64; ruling R45.5): "Spraying Fire cannot be used in a FG attack unless all members of the FG are capable
        // of Spraying Fire". A MG is; a squad's own FP is when its Range is underscored, to "a maximum range of three hexes"; no other unit's FP is.
        // An attack recorded before the pass keeps ruling R12.6's test: every firer fires a MG, and its holder's FP goes with it.
        // A7.351, A7.352: a squad firing two SW, and a HS or hero firing one, add no FP of their own, as in any attack.
        bool AddsOwnFp(FireFirer item, FireDefinition? definition) => item.UsesInherentFp != false
            && !(definition?.Kind == "asl:squad" && item.Weapons!.Count >= 2) && !((definition?.Kind == "asl:half-squad" || definition?.IsHero == true) && item.Weapons!.Count >= 1);
        bool Sprays(FireFirer item, FireDefinition? definition) => item.Weapons is { } used
            && used.All(weapon => reference.Definitions.GetValueOrDefault(weapon.DefinitionId ?? string.Empty) is { IsMg: true })
            && (!AddsOwnFp(item, definition) ? used.Count > 0
                : definition is { Kind: "asl:squad", SprayingFire: true } && RangeOf(attack, item) <= 3);
        var capable = attack.CoweringMarksAndWeaponResidual == true
            ? firers.Zip(firerDefinitions).All(pair => Sprays(pair.First with { Weapons = pair.First.Weapons ?? [] }, pair.Second))
            : firers.All(item => item.Weapons is { Count: > 0 } weapons
                && weapons.All(weapon => reference.Definitions.GetValueOrDefault(weapon.DefinitionId ?? string.Empty) is { IsMg: true }));
        if (attack.SprayingFire == true && (phase is not ("PFPh" or "AFPh" or "DFPh") || tpbf || !capable))
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
            // A7.21, B23.26 (pp. 54, 136; pass 45): "Attacks up or down a stairwell hex to other vertical levels of the same building hex are limited
            // to the next higher or lower level." Range 0 from another Location is the next level of the target's hex, whose LOS the planner read.
            var nextLevel = !tpbf && range == 0 && firer.LocationId != attack.TargetLocationId && Math.Abs(LevelAboveOf(attack, firer)) == 1;

            // A9.4 (p. 64; pass 45): "MG fire is limited to a 16 hex maximum range unless an Infantry leader (even a 0 or + DRM leader) is directing
            // that fire". An attack recorded before the pass has no such limit.
            if (attack.CoweringMarksAndWeaponResidual == true && range > 16 && directors.Length == 0
                && (firer.Weapons ?? []).Any(weapon => reference.Definitions.GetValueOrDefault(weapon.DefinitionId!) is { IsMg: true }))
            {
                outside.Add("asl.a1.fire.mg-direction-outside");
            }
            if ((range < (tpbf ? 0 : 1) && !nextLevel) || (tpbf && range != 0) || inherentOut || weaponOut)
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
    /// thrower's own (the thrower's); every unit there is attacked, of either side, and a Location holding a vehicle is not reviewed (backlog) unless the
    /// DC names it, an AFV (A23.5; pass 60, ruling R60.16).
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
        if (definition is not { IsDc: true, Firepower: not null } || user is null || !(user.IsMmc || user.IsHero || user.IsLeader || ScenarioA1Definitions.IsCrew(user.Kind))
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
            || (attack.Vehicles ?? []).Any(item => item.VehicleId != attack.TargetVehicle?.VehicleId) || !ScenarioA1FireReference.Tem.ContainsKey(attack.TargetTerrain!))
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
    /// or leader joins it (D3.4; D6.64 is not reviewed); never in the PFPh by a vehicle in Motion (D2.4). Keeps its answer (the A8.41 claim,
    /// ruling RA8.3): an MA MG marked First Fire is refused here (ruling R50.2, D3.5 alone), where A9.3's EXC lets it sustain once in Final Fire at
    /// an ADJACENT or same-hex target, as <see cref="ScenarioA1FinalFireCalculator.MaySustain"/> and
    /// <see cref="ScenarioA1FinalFireCalculator.FinalFireRefusal"/> answer.
    /// </summary>
    private static void VehicleFireOutside(FireAttack attack, FireVehicleFire vehicle, ScenarioA1FireReference reference,
        IReadOnlyList<FireDefinition?> targetDefinitions, List<string> outside)
    {
        var definition = reference.Definitions.GetValueOrDefault(vehicle.DefinitionId!);

        // D1.81, D1.82, D3.5 (pp. 195, 200; pass 50): the BMG and CMG it names, each printed on the counter, working, and named once. A BMG fires
        // only inside the VCA; a CMG's turret turns for the shot (D3.52), but never in Bounding First Fire (C5.13: "must always fire within its
        // current CA"); the CMG of a CE tank with a RST or 1MT turret does not fire (D1.321, D1.322). Neither needs a CE crew (D1.83).
        // D1.3, D1.83 (pass 60): the MA that is a MG: the AAMG, fired CE, or a plain MG (the Bren Carrier's, with its nationality's LMG FP).
        var named = vehicle.Weapons is { Count: > 0 } list ? list : null;
        var weaponsOutside = named is null
            ? definition is not { IsVehicle: true } || ScenarioA1MachineGunKill.MainArmamentFirepower(reference, definition) is null
                || (definition.MainArmament == "aamg" && vehicle.CrewExposed != true) || vehicle.Malfunctioned == true
            : definition is not { IsVehicle: true } || named.Select(item => item.Weapon).Distinct(StringComparer.Ordinal).Count() != named.Count
                || named.Any(item => VehicleWeaponFp(definition, item.Weapon) is null || item.Malfunctioned == true
                    || (item.Weapon == FireVehicleWeapon.CoaxialMg && definition.TurretBarredWhileCe(vehicle.CrewExposed))
                    || (item.Turn > 0 && (item.Weapon != FireVehicleWeapon.CoaxialMg || attack.FireKind == BoundingFirstFire)));
        if (weaponsOutside || attack.Firers is { Count: > 0 } || Directors(attack).Any() || vehicle.LocationId != attack.FirerLocationId || vehicle.Stunned == true
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

        // D3.5 (pass 50): a BMG or CMG makes one fire attack a Player Turn, in the fire phase the vehicle's other weapons fire in.
        if (named is null ? vehicle.FiredThisPlayerTurn == true && vehicle.RateOfFireShot != true : vehicle.OtherPhase == true || named.Any(item => item.Fired == true))
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

        // A7.22: within twice the Normal Range of each MG that fires: eight hexes for a BMG and an AAMG, twelve for a CMG (D1.81 to D1.83).
        if (attack.Range is < 1 || attack.Range > (named is null ? 16 : named.Min(item => 2 * VehicleWeaponRange(item.Weapon))))
        {
            outside.Add("asl.a1.fire.out-of-range");
        }

        // C2.6 (p. 168; pass 55): "a Gun may fire-at/affect a different-level target only if the range is >= the elevation difference between
        // them", and "These restrictions also apply to non-AA vehicular MG". The MA AAMG is held to it as well: what an AA weapon may do at a
        // higher target is not built.
        if (attack.SameLevel == false && attack.Range < Math.Abs(attack.TargetLevelAbove ?? 0))
        {
            outside.Add("asl.a1.fire.elevation-outside");
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

    /// <summary>The printed FP of a vehicle's BMG or CMG (D1.8, p. 195; pass 50); null when the counter prints none or the name is neither.</summary>
    public static int? VehicleWeaponFp(FireDefinition definition, string? weapon)
    {
        ArgumentNullException.ThrowIfNull(definition);
        return weapon switch
        {
            FireVehicleWeapon.BowMg => definition.BowMg,
            FireVehicleWeapon.CoaxialMg => definition.CoaxialMg,
            _ => null,
        };
    }

    /// <summary>The Normal Range of a vehicle's MG (D1.81 to D1.83, p. 195; pass 50): twelve hexes for a CMG, eight for a BMG and an AAMG.</summary>
    public static int VehicleWeaponRange(string? weapon) => weapon == FireVehicleWeapon.CoaxialMg ? 12 : 8;

    /// <summary>
    /// The Case A DRM a vehicle's MG attack adds to its IFT DR (D3.52, D3.51, C5.11, pp. 172 and 201; pass 50): for a CMG whose turret turns, the
    /// To Hit DRM of Case A for the turret's type, doubled in woods, a building, or rubble; for one that fires within the current TCA after
    /// another turret weapon turned it in this fire phase, what that weapon paid; the worst of the weapons that fire together.
    /// </summary>
    public static int VehicleCaseA(FireVehicleFire vehicle, FireDefinition definition)
    {
        ArgumentNullException.ThrowIfNull(vehicle);
        ArgumentNullException.ThrowIfNull(definition);
        return (vehicle.Weapons ?? []).Select(item => item.Turn is > 0 and var turn
            ? ScenarioA1ArmorCalculator.CaseA(definition.MaType, turn) * (vehicle.InWoodsOrBuilding == true ? 2 : 1)
            : item.MaintainedCaseA ?? 0).DefaultIfEmpty(0).Max();
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
        else if (definition.Caliber is not null && overrun.MainArmamentMalfunctioned != true && !definition.TurretBarredWhileCe(overrun.CrewExposed))
        {
            // D1.321, D1.322 (pass 35, task 35.13 c): a CE RST or 1MT AFV fires neither its MA nor its CMG.
            weapons.Add((FireOverrunEffect.MainArmament, 0));
        }

        if (definition.BowMg is { } bmg && overrun.BmgMalfunctioned != true)
        {
            weapons.Add((FireOverrunEffect.BowMg, bmg));
        }

        if (definition.CoaxialMg is { } cmg && overrun.CmgMalfunctioned != true && !definition.TurretBarredWhileCe(overrun.CrewExposed))
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
        || rolls.CowerSelection?.Values.Any(dr => dr is < 1 or > 6) == true
        || rolls.ShortageSelection?.Values.Any(dr => dr is < 1 or > 6) == true
        || rolls.HeatOfBattle?.Values.Any(dice => !Dice(dice, allowEmpty: false)) == true
        || rolls.BerserkChecks?.Values.Any(dice => !Dice(dice, allowEmpty: false)) == true
        || rolls.CrewChecks?.Values.Any(dice => !Dice(dice, allowEmpty: false)) == true
        || rolls.UnlikelyKill?.Values.Any(dr => dr is < 1 or > 6) == true
        || rolls.MolCheck is < 1 or > 6
        || !Dice(rolls.Position, allowEmpty: true) || !Dice(rolls.CrewSurvival, allowEmpty: true) || !Dice(rolls.TaskCheck, allowEmpty: true)
        || !Dice(rolls.CollateralCheck, allowEmpty: true)
        || rolls.WeaponDestruction?.Values.Any(dr => dr is < 1 or > 6) == true
        || rolls.VehicleSurvival?.Values.Any(dice => !Dice(dice, allowEmpty: false)) == true;

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
            // A8.31, A8.312 (pass 45, ruling R45.1): a unit with no counter fires First Fire in one group with Subsequent First Fire and FPF firers.
            SubsequentFirstFire => !finalFire && (firstFire || firer.FiredThisPlayerTurn != true),
            FinalProtectiveFire => finalFire || firstFire || firer.FiredThisPlayerTurn != true,
            _ when attack.Phase == "DFPh" => rateOfFireShot || ScenarioA1FinalFireCalculator.UnitMayFinalFire(firstFire, finalFire, firer.FiredThisPlayerTurn == true),
            _ => rateOfFireShot || firer.FiredThisPlayerTurn != true,
        };
    }

    /// <summary>
    /// Whether a MG may fire in this attack: not yet marked, or marked First Fire where Sustained Fire allows it (A8.3, A8.31, A8.4, A9.3). The DFPh
    /// arm reads the counter alone, from <see cref="ScenarioA1FinalFireCalculator.WeaponMayFinalFire"/> (the A8.41 claim, ruling RA8.8); the range
    /// of a First-Fire-marked weapon is <see cref="ScenarioA1FinalFireCalculator.FinalFireRefusal"/>'s, which no caller asks yet.
    /// </summary>
    private static bool WeaponMayFire(FireAttack attack, FireWeapon weapon) => attack.FireKind switch
    {
        FinalProtectiveFire => true,
        SubsequentFirstFire => weapon.FiredThisPlayerTurn != true,
        _ when attack.Phase == "DFPh" => ScenarioA1FinalFireCalculator.WeaponMayFinalFire(weapon.FiredThisPlayerTurn == true),
        _ => weapon.FiredThisPlayerTurn != true && weapon.FirstFireMarked != true,
    };

    /// <summary>Whether the attack's targets are exactly the crew of the Gun it names (ruling R8.3).</summary>
    internal static bool GunCrewAlone(FireAttack attack) => attack.GunTarget is { CrewUnitId: { } crew } && attack.Targets is [{ } only] && only.UnitId == crew;

    /// <summary>
    /// Whether the attack's targets hold a Gun's crew outside ruling R8.3's reading (C11): with other units, or with no Gun named. A crew is a Gun's
    /// crew when the attack's Gun target names it or its <see cref="FireAttack.GunCrews"/> fact lists it; any other crew is an ordinary Infantry
    /// target (A1.123, p. 44; pass 65, ruling R65.28).
    /// </summary>
    private static bool GunCrewUnreviewed(FireAttack attack, IReadOnlyList<FireTarget> targets, IReadOnlyList<FireDefinition?> definitions) =>
        !GunCrewAlone(attack) && targets.Zip(definitions).Any(pair => ScenarioA1Definitions.IsCrew(pair.Second?.Kind)
            && (attack.GunTarget?.CrewUnitId == pair.First.UnitId || attack.GunCrews?.Contains(pair.First.UnitId ?? string.Empty, StringComparer.Ordinal) == true));

    /// <summary>
    /// Whether an AFV of an Area Target Type hit is attacked as unarmored (D5.311, B13.3, p. 203; pass 60, ruling R60.12): an OT AFV in woods, whose
    /// crew's CE DRM the Air Bursts reduce, is resolved on the Vehicle line with no Air Burst TEM.
    /// </summary>
    internal static bool AirBurstUnarmored(bool area, string? terrain, FireDefinition definition) =>
        area && terrain == "woods" && definition.Unarmored != true && definition.OpenTopped == true;

    /// <summary>Whether a Good Order crew's gunshield faces the attack (C11.5).</summary>
    private static bool GunshieldFaces(FireAttack attack, FireGunTarget gun) => gun.Gunshield == true && attack.Targets is [{ Broken: not true }];

    /// <summary>Whether a vehicle's PRC fact names a unit the catalog lacks, or a role other than Passenger or Rider (pass 65, ruling R65.5).</summary>
    internal static bool PrcMalformed(IReadOnlyList<FirePrc>? prc, ScenarioA1FireReference reference) =>
        prc?.Any(item => item.UnitId is null || item.Role is not (FirePrc.Passenger or FirePrc.Rider)
            || reference.Definitions.GetValueOrDefault(item.DefinitionId ?? string.Empty) is not { IsVehicle: false }) == true;

    /// <summary>
    /// A Specific Collateral Attack on the PRC of the vehicle a hit names (D.8A, D6.71, p. 192; pass 65, ruling R65.7), for the Ordnance package: what
    /// <see cref="Resolution.SpecificOnPrc"/> gives, on the PRC's own rolls, or the missing roll's reason.
    /// </summary>
    internal static (IReadOnlyList<FirePrcAttack>? Attacks, IReadOnlyList<string>? Desperation, string? Missing) SpecificCollateralOnPrc(OrdnanceVehicleTarget target,
        int column, int originalDr, FireRolls? rolls, ScenarioA1FireReference reference)
    {
        var shell = new FireAttack(null, null, null, null, null, null, null, null, null, null, null, null, [], null, rolls ?? new FireRolls(null, null, null, null));
        var resolution = new Resolution(shell, reference);
        var made = resolution.SpecificOnPrc(target, column, originalDr, ordnanceHit: true);
        return made is { } done ? (done.Attacks, done.Desperation, null) : (null, null, resolution.FirstUndecided);
    }

    /// <summary>
    /// The Survival DR of each Passenger and Rider of a destroyed vehicle (D6.9, D5.6, pp. 207, 204; pass 65, ruling R65.9): one DR each against the CS# or
    /// cs#, +1 if broken, no Shock or Stun DRM. Null with no PRC or no CS#; the key of the first missing roll otherwise.
    /// </summary>
    internal static (IReadOnlyList<PrcSurvival>? Survival, string? Missing) PrcSurvivals(IReadOnlyList<FirePrc>? prc, int? crewSurvival,
        IReadOnlyDictionary<string, IReadOnlyList<int>>? rolls)
    {
        if (prc is not { Count: > 0 } aboard || crewSurvival is not { } cs)
        {
            return (null, null);
        }

        var made = new List<PrcSurvival>();
        foreach (var unit in aboard)
        {
            if (rolls?.GetValueOrDefault(unit.UnitId!) is not { Count: 2 } dice)
            {
                return (null, "prcSurvival:" + unit.UnitId);
            }

            var drm = unit.Broken == true ? 1 : 0;
            var final = dice[0] + dice[1] + drm;
            made.Add(new PrcSurvival(unit.UnitId!, [.. dice], drm, final, cs, final <= cs));
        }

        return (made, null);
    }

    /// <summary>
    /// What an IFT result does to an Unprotected crew (D5.311, p. 203; pass 65, ruling R65.26): "not susceptible to Stun/Recall ... it is instead subject to
    /// PTC/MC/K/KIA results" as Infantry. A KIA or K eliminates it (a crew's Casualty Reduction, A7.302), as does an Original 12 on a MC (A10.31); a failed
    /// MC breaks it, and a failed PTC pins it. <paramref name="dice"/> is the check's DR, needed when <see cref="CrewChecks"/> says so. Pass 70 (ruling R70.10;
    /// A7.302, A7.8): a crew already broken (<paramref name="crewBroken"/>) that fails a MC is eliminated by Casualty Reduction, and takes no PTC.
    /// </summary>
    internal static (FireCheck? Check, string Result) UnprotectedCrewOutcome(string outcome, IReadOnlyList<int>? dice, int morale, int? crewMorale, bool crewBroken = false)
    {
        if (Regex.IsMatch(outcome, "^([1-7])KIA$") || Regex.IsMatch(outcome, "^K/([1-4])$"))
        {
            return (null, FireVehicleEffect.Eliminated);
        }

        if (!UnprotectedCrewChecks(outcome, crewBroken) || dice is null)
        {
            return (null, FireVehicleEffect.None);
        }

        var mc = Regex.Match(outcome, "^([1-4])MC$");
        var pin = outcome == "PTC";
        List<FireModifier> checkDrm = mc.Success ? [new FireModifier("ift-mc", int.Parse(mc.Groups[1].Value, CultureInfo.InvariantCulture), "A7.304")] : [];
        var original = dice[0] + dice[1];
        var checkFinal = original + (int)checkDrm.Sum(item => item.Value);
        var passed = checkFinal <= morale;
        var result = !pin && original == 12 ? FireVehicleEffect.Eliminated
            : passed ? FireVehicleEffect.None
            : pin ? FireVehicleEffect.Pinned
            : crewBroken ? FireVehicleEffect.Eliminated
            : FireVehicleEffect.Broken;
        return (new FireCheck(pin ? "NTC" : mc.Success ? outcome : "NMC", [.. dice], original, checkDrm, checkFinal, morale, passed, result)
        {
            MoraleSource = ScenarioA1ArmorLeaders.MoraleSource(crewMorale),
        }, result);
    }

    /// <summary>Whether a Vulnerable crew's IFT result calls on it to check (D.8, A7.303 to A7.305): a MC, a NMC, or a PTC.</summary>
    internal static bool CrewChecks(string outcome) => Regex.IsMatch(outcome, "^([1-4])MC$") || outcome is "NMC" or "PTC";

    /// <summary>Whether an Unprotected crew checks (D5.311): as <see cref="CrewChecks"/>, but a crew already broken takes no PTC (A7.8; pass 70, ruling R70.10).</summary>
    internal static bool UnprotectedCrewChecks(string outcome, bool crewBroken) => CrewChecks(outcome) && !(crewBroken && outcome == "PTC");

    /// <summary>
    /// What a Collateral Attack's IFT result does to a Vulnerable crew (D.8, D5.34, D5.341, A7.82; rulings R25.5, R25.6; pass 60, ruling R60.7): a KIA
    /// or K Recalls it; a failed MC Stuns it, or Recalls a crew already under Stun +1 (D5.342); a failed PTC pins it; an Original 12 on a MC Recalls it
    /// (A10.31). <paramref name="dice"/> is the check's DR, needed when <see cref="CrewChecks"/> says so. The one <paramref name="stunRecovery"/> gives both
    /// the MC's +1 and D5.342's second Stun, so a caller that passes a merely Stunned crew in it adds a +1 D5.34 gives only to the "+1" side (the D5.34
    /// claim, ruling RD5.4): the overload with the two facts apart, under <see cref="ScenarioA1StunCalculator.Drm"/> and
    /// <see cref="ScenarioA1StunCalculator.SecondStun"/>, replaces it, and this one forwards to it with both equal, keeping its answer.
    /// </summary>
    internal static (FireCheck? Check, string Result) CrewOutcome(string outcome, IReadOnlyList<int>? dice, bool stunRecovery, int morale, int? crewMorale) =>
        CrewOutcome(outcome, dice, stunRecovery ? 1 : 0, stunRecovery, morale, crewMorale);

    /// <summary>
    /// <see cref="CrewOutcome(string, IReadOnlyList{int}?, bool, int, int?)"/> with D5.34's +1 and D5.342's second Stun apart (the D5.34 claim, ruling RD5.4):
    /// <paramref name="plusOne"/> is the MC's Stun +1 (<see cref="ScenarioA1StunCalculator.Drm"/>), and <paramref name="secondStun"/> makes a failed MC a
    /// Recall (<see cref="ScenarioA1StunCalculator.SecondStun"/>).
    /// </summary>
    internal static (FireCheck? Check, string Result) CrewOutcome(string outcome, IReadOnlyList<int>? dice, int plusOne, bool secondStun, int morale, int? crewMorale)
    {
        if (Regex.IsMatch(outcome, "^([1-7])KIA$") || Regex.IsMatch(outcome, "^K/([1-4])$"))
        {
            return (null, FireVehicleEffect.Recalled);
        }

        if (!CrewChecks(outcome) || dice is null)
        {
            return (null, FireVehicleEffect.None);
        }

        var mc = Regex.Match(outcome, "^([1-4])MC$");
        var pin = outcome == "PTC";
        var modifier = mc.Success ? int.Parse(mc.Groups[1].Value, CultureInfo.InvariantCulture) : 0;
        var checkDrm = new List<FireModifier>();
        if (modifier > 0)
        {
            checkDrm.Add(new FireModifier("ift-mc", modifier, "A7.304"));
        }

        if (plusOne > 0)
        {
            checkDrm.Add(new FireModifier("stun-recovery", plusOne, "D5.34"));
        }

        var original = dice[0] + dice[1];
        var checkFinal = original + (int)checkDrm.Sum(item => item.Value);
        var passed = checkFinal <= morale;
        // A10.31, D5.341: an Original 12 on a MC is a Casualty MC, which Recalls an Inherent crew.
        var result = !pin && original == 12 ? FireVehicleEffect.Recalled
            : passed ? FireVehicleEffect.None
            : pin ? FireVehicleEffect.Pinned
            : secondStun ? FireVehicleEffect.Recalled
            : FireVehicleEffect.Stunned;
        return (new FireCheck(pin ? "NTC" : mc.Success ? outcome : "NMC", [.. dice], original, checkDrm, checkFinal, morale, passed, result)
        {
            MoraleSource = ScenarioA1ArmorLeaders.MoraleSource(crewMorale),
        }, result);
    }

    private static bool Dice(IReadOnlyList<int>? dice, bool allowEmpty) =>
        dice is null ? allowEmpty : dice.Count == 2 && dice.All(die => die is >= 1 and <= 6);

    private static List<string> Undecided(FireAttack attack, ScenarioA1FireReference reference)
    {
        var undecided = new List<string>();
        var targets = attack.Targets!;
        if (attack.VehicleFire is not null || attack.Canister is not null)
        {
            // C2.6 (pass 55): a vehicle's MG fires at another level when the range allows it; the attack is then decided as at its own level.
            // C8.4 (pass 55): a Canister attack takes its Location's LOS Hindrance, which must be attributed as any other.
            if (attack.Los is { } los && (los.HindranceAttributed != true || los.HindranceDrm < 0
                || (los.GrainInLos == true && attack.ScenarioMonth is null)))
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

        if (attack.FireKind != ResidualFire && attack.OrdnanceHit is null && attack.VehicleFire is null && attack.Overrun is null && attack.DemolitionCharge is null
            && attack.Canister is null)
        {
            // Ruling R10.4 (backlog pass 10): Infantry fire at another level is decided with the map read's LOS and Hindrance.
            var firers = attack.Firers!;
            if (firers.Select(item => LosOf(attack, item)!).Any(los => los.HindranceAttributed != true || los.HindranceDrm < 0
                || (los.GrainInLos == true && attack.ScenarioMonth is null)))
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
        // B23.741 (the B23.74 claim, RB23F.5): a Factory's TEM, when the attack carries one, in place of the terrain key's.
        if (attack.AfvCover is not { } cover || !ScenarioA1FireReference.Tem.TryGetValue(attack.TargetTerrain ?? string.Empty, out var tem) || (attack.FactoryTem ?? tem) > 0
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

    /// <summary>
    /// What a proposal states of a FT, MOL, or DC at the AFV it names before any roll (A22.34, A22.612, C7.346; pass 60, rulings R60.15, R60.16): the
    /// Final TK# with its modifiers, or the Position DRM; null for any other attack.
    /// </summary>
    public static string? NamedVehicleTerms(FireAttack attack, ScenarioA1FireReference reference)
    {
        ArgumentNullException.ThrowIfNull(attack);
        ArgumentNullException.ThrowIfNull(reference);
        return attack.TargetVehicle is { DefinitionId: { } id } named && reference.Armor.TryGetValue(id, out var armor) && (attack.Firers is { Count: > 0 } || attack.DemolitionCharge is not null)
            ? new Resolution(attack, reference).Terms(named, armor) : null;
    }

    /// <summary>One resolution, with the target units' changing state.</summary>
    private sealed class Resolution(FireAttack attack, ScenarioA1FireReference reference, FireVehicleBreakdownFacts? vehicleBreakdown = null)
    {
        private readonly List<string> undecided = [];
        private readonly HashSet<string> usedRolls = new(StringComparer.Ordinal);

        // A7.9 (pass 45): the units the attack's Cowering marks, decided once the arithmetic is known.
        private readonly HashSet<string> cowering = new(StringComparer.Ordinal);

        // A8.26: the Hindrance and leadership the Residual FP was first reduced by, kept for the figure the weapons' fates give (A8.221, A8.23).
        private int residualHindrance;
        private int residualLeadership;
        private readonly HashSet<string> usedChoices = new(StringComparer.Ordinal);
        private readonly Dictionary<string, TargetState> state = new(StringComparer.Ordinal);

        // A22.611 (ruling R15.4): the attack's MOL Check, and whether a colored dr of 6 broke its user.
        private FireMolCheck? molCheck;

        // A23.4 (ruling R15.2): whether the DC's Original DR reached its malfunction number.
        private bool dcMalfunctioned;

        // Pass 60 (rulings R60.15, R60.16): a FT, MOL, or DC at a named AFV: its Position DR, To Kill DR, and Specific Collateral Attack, and whether the
        // attack spares the Location's units or the AFV's crew the IFT attack.
        private FirePosition? position;
        private OrdnanceKill? vehicleKill;
        private OrdnanceCollateral? specificCollateral;
        private bool targetsSpared;
        private bool vehicleSpared;

        // A9.74, C11.6 (pass 60, ruling R60.17): the result and IFT column index each target unit took.
        private readonly Dictionary<string, (string Result, int Column)> taken = new(StringComparer.Ordinal);

        // Pass 65 (ruling R65.7): the PRC a Collateral Attack reaches, apart from the target Location's units: no leader of the Location leads their
        // checks, no leader's loss checks them, and they take no Heat of Battle (D6.651 and their leaders are increment 2c's).
        private readonly Dictionary<string, TargetState> prc = new(StringComparer.Ordinal);

        // A12.141 (pass 70, ruling R70.17): the leaders whose modifier the resolution applied to a check, which a morale check reads for their "?".
        private readonly HashSet<string> leading = new(StringComparer.Ordinal);

        /// <summary>A Passenger or Rider: a Collateral Attack's PRC, or a unit a morale check names aboard (pass 70, ruling R70.8). Neither takes Heat of Battle (A15.1).</summary>
        private bool IsPrc(TargetState unit) => prc.ContainsKey(unit.Id) || unit.Target.Aboard is not null;

        /// <summary>
        /// D6.651, A10.7 EXC (pp. 206, 68; pass 70, ruling R70.8): whether a leader's MC modifier reaches a unit, read only where one of them is aboard
        /// with the pass's facts. A PRC of an older record takes no leader's modifier and gives none, as before.
        /// </summary>
        private bool LeadershipReaches(TargetState leader, TargetState unit)
        {
            var (leaderAboard, unitAboard) = (leader.Target.Aboard, unit.Target.Aboard);
            if ((prc.ContainsKey(leader.Id) && leaderAboard is null) || (prc.ContainsKey(unit.Id) && unitAboard is null))
            {
                return false;
            }

            return (leaderAboard is null && unitAboard is null)
                || ScenarioA1PassengerActionRules.LeaderReaches(leaderAboard?.VehicleId, leaderAboard?.CeInUnmovedHalftrack == true, unitAboard?.VehicleId,
                    unitAboard?.VehicleMoving == true);
        }

        /// <summary>
        /// A10.2 (p. 65; pass 70, ruling R70.8): whether a leader's loss checks a unit: "Riders ... neither take or cause LLMC/LLTC", and "Passengers take/cause
        /// LLMC/LLTC only to fellow passengers of the same vehicle". Units on foot as before.
        /// </summary>
        private static bool LeaderLossReaches(TargetState leader, TargetState unit)
        {
            var (leaderAboard, unitAboard) = (leader.Target.Aboard, unit.Target.Aboard);
            if (leaderAboard?.Role == FirePrc.Rider || unitAboard?.Role == FirePrc.Rider)
            {
                return false;
            }

            return (leaderAboard is null && unitAboard is null) || (leaderAboard?.VehicleId is { } vehicle && unitAboard?.VehicleId == vehicle);
        }

        /// <summary>The first reason the resolution is undecided so far; null when none.</summary>
        public string? FirstUndecided => undecided.Count > 0 ? undecided[0] : null;

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

            // A22.34 (pass 60, ruling R60.15): a FT at a named AFV attacks it alone, at its full FP.
            var flameAtAfv = attack.TargetVehicle is not null && Flamethrower;
            var arithmetic = flameAtAfv ? Arithmetic(true, false)
                : Arithmetic(known.Length > 0 || knownVehicles > 0, concealed.Length > 0 || knownVehicles < vehicles.Count || (known.Length == 0 && vehicles.Count == 0));
            if (arithmetic is null)
            {
                return Refused(FireResolution.Indeterminate, undecided);
            }

            usedRolls.Add("attack");
            var concealedResult = arithmetic.Concealed?.Result ?? arithmetic.Result;

            // A22.34, A22.612, A23.5, C7.346 (pass 60, rulings R60.15, R60.16): a FT, MOL, or DC at the AFV it names.
            if (attack.TargetVehicle is not null && !NamedVehicleAttack(arithmetic))
            {
                return Refused(FireResolution.Indeterminate, undecided.Distinct().ToArray());
            }

            // A7.83 (ruling R12.3): the pinned movers of a mixed stack take the result of their own Final DR.
            var split = arithmetic.PinnedFinalDr is not null;
            TargetState[] Unpinned(TargetState[] group) => split ? [.. group.Where(unit => !unit.Target.Pinned.GetValueOrDefault())] : group;
            TargetState[] PinnedOf(TargetState[] group) => split ? [.. group.Where(unit => unit.Target.Pinned == true)] : [];
            // Pass 60 (rulings R60.15, R60.16): the units of the Location are spared an attack that falls on the named AFV alone.
            if (Unpinned(known) is { Length: > 0 } knownUnpinned && !targetsSpared)
            {
                ApplyGroup(arithmetic.Result, knownUnpinned, arithmetic, concealedGroup: false, pinnedGroup: false);
            }

            if (PinnedOf(known) is { Length: > 0 } knownPinned && undecided.Count == 0 && !targetsSpared)
            {
                ApplyGroup(arithmetic.PinnedResult!, knownPinned, arithmetic, concealedGroup: false, pinnedGroup: true);
            }

            if (Unpinned(concealed) is { Length: > 0 } concealedUnpinned && undecided.Count == 0 && !targetsSpared)
            {
                ApplyGroup(concealedResult, concealedUnpinned, arithmetic, concealedGroup: true, pinnedGroup: false);
            }

            if (PinnedOf(concealed) is { Length: > 0 } concealedPinned && undecided.Count == 0 && !targetsSpared)
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
            if (concealedResult != "none" && !targetsSpared)
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

            // A7.9 (pass 45): who cowered, before the weapons' ROF and counters are read; then the Residual FP the weapons leave (A8.221, A8.23).
            if (!DecideCowering(arithmetic))
            {
                return Refused(FireResolution.Indeterminate, undecided.Distinct().ToArray());
            }

            var weapons = attack.VehicleFire is { } vehicleFire ? VehicleWeaponEffects(vehicleFire, arithmetic) : attack.SprayShare == true ? null : WeaponEffects(arithmetic);
            if (attack.VehicleFire is null && weapons is not null)
            {
                arithmetic = WithWeaponResidual(arithmetic, weapons);
            }

            var overrunEffect = attack.Overrun is { } overrunning ? OverrunEffect(overrunning, arithmetic) : null;
            var firerEffects = attack.FireKind == FinalProtectiveFire ? FinalProtectiveFireChecks(arithmetic) : null;

            // A19.131 (pass 72, ruling R72.11): after the SW's malfunction or removal, the Unit Replacement of an Original 12 under Ammunition Shortage.
            var shortageEffects = ShortageReplacements(arithmetic, firerEffects);
            var vehicleEffects = vehicles.Count == 0 || vehicleSpared ? null : VehicleEffects(arithmetic);
            var destruction = WeaponDestruction();
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
                FirerCounters = FirerCounters(),
                CoweredUnitIds = cowering.Count == 0 ? null : [.. (attack.Firers ?? []).Select(item => item.UnitId!).Where(cowering.Contains)],
                WeaponEffects = weapons,
                FirerEffects = firerEffects,
                ShortageEffects = shortageEffects,
                CompanionEffects = companions,
                VehicleEffects = vehicleEffects,
                OverrunEffect = overrunEffect,
                MolCheck = molCheck,
                DemolitionChargeMalfunctioned = dcMalfunctioned ? true : null,
                FlamethrowerRemoved = weapons?.FirstOrDefault(item => item.Malfunctioned
                    && (attack.Firers ?? []).SelectMany(firer => firer.Weapons ?? []).Any(weapon => weapon.EquipmentId == item.EquipmentId
                        && reference.Definitions.GetValueOrDefault(weapon.DefinitionId ?? string.Empty)?.IsFt == true))?.EquipmentId,
                Position = position,
                VehicleKill = vehicleKill,
                SpecificCollateral = specificCollateral,
                WeaponDestruction = destruction,
            };
        }

        /// <summary>
        /// A morale check made outside an attack (pass 70, ruling R70.3): the check its subjects take, resolved as an attack's result is (A10.1, A10.3,
        /// A15.1, A17.11, A19.13), then the Leader Loss checks of every leader the check, or the cause outside it, lost (A10.2), and the Berserk TC of a
        /// leader made berserk (A15.41). No IFT DR, no arithmetic. The other units of the Location take no check but may lead one or take a LLMC; the lost
        /// leaders are gone already, and their effects are not given.
        /// </summary>
        public FireResolution RunCheck(MoraleCheckFacts facts)
        {
            var subjects = facts.Subjects!;
            var others = facts.Others ?? [];
            foreach (var target in subjects.Concat(others))
            {
                state[target.UnitId!] = new TargetState(target, reference.Definitions[target.DefinitionId!])
                {
                    NoQuarter = facts.NoQuarter == true,
                };
            }

            // A10.2: "For each leader eliminated (whether by breaking when already broken, a mortal wound, or any other cause) ... must take a LLMC", and
            // "Should an unbroken leader break ... must take a LLTC".
            foreach (var (leader, eliminated) in (facts.LostLeaders ?? []).Select(item => (item, true)).Concat((facts.BrokenLeaders ?? []).Select(item => (item, false))))
            {
                var lost = new TargetState(leader, reference.Definitions[leader.DefinitionId!]);
                if (eliminated)
                {
                    lost.Eliminate("lost-outside-fire");
                }
                else
                {
                    lost.Break("broken-outside-fire");
                }

                state[leader.UnitId!] = lost;
            }

            TargetState[] checking = [.. subjects.Select(item => state[item.UnitId!])];
            var mc = Regex.Match(facts.Check!, "^([1-4])MC$");
            if (facts.Check == MoraleCheckFacts.Nmc)
            {
                MoraleChecks(0, checking);
            }
            else if (mc.Success)
            {
                MoraleChecks(int.Parse(mc.Groups[1].Value, CultureInfo.InvariantCulture), checking);
            }
            else if (facts.Check == MoraleCheckFacts.Ptc)
            {
                PinTaskChecks(checking);
            }

            LeaderLoss();
            var companions = BerserkTaskChecks();
            if (undecided.Count != 0)
            {
                return Refused(FireResolution.Indeterminate, undecided.Distinct().ToArray());
            }

            // A12.141 (p. 78; ruling R70.17): a concealed unit seen by a Good Order enemy ground unit within 16 hexes loses its "?" by a LLMC or LLTC it takes,
            // and by its leadership modifier applied to another unit's check.
            TargetState[] named = [.. subjects.Concat(others).Select(item => state[item.UnitId!])];
            foreach (var unit in named.Where(unit => (unit.Target.Concealed == true || unit.Target.Hidden == true) && unit.Target.SeenByGoodOrderEnemy == true
                && (unit.Checks.Any(check => check.Kind is "LLMC" or "LLTC") || leading.Contains(unit.Id))))
            {
                unit.ConcealmentLost = true;
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

            return new FireResolution(FireResolution.Resolved, [], null, [.. named.Select(unit => unit.Effect())], [], null, [])
            {
                CompanionEffects = companions,
            };
        }

        /// <summary>
        /// A9.74, C8.42, C11.6 (pp. 65, 178, 182; pass 60, rulings R60.3, R60.17), when the attack carries the rule: each SW and Gun held by a unit a Final
        /// KIA eliminated rolls a dr, read as an IFT DR on that unit's column with no DRM; a KIA destroys it, a K malfunctions it, or destroys it if it was
        /// malfunctioned or has an X#. A unit eliminated by a K or by Casualty Reduction is no KIA's. The Gun an ordnance hit or a DC attacks follows the
        /// table's own column instead (C11.4): a DC destroys a Gun its target units man on their KIA and malfunctions it on their K. Null when a roll is
        /// missing or nothing was checked.
        /// </summary>
        private List<FireWeaponDestruction>? WeaponDestruction()
        {
            if (attack.RandomDestruction != true || undecided.Count != 0)
            {
                return null;
            }

            var ordnanceGun = attack.OrdnanceHit is not null ? attack.GunTarget?.GunId : null;
            var fates = new List<FireWeaponDestruction>();
            foreach (var target in attack.Targets ?? [])
            {
                if (!taken.TryGetValue(target.UnitId!, out var own))
                {
                    continue;
                }

                var unit = state[target.UnitId!];
                foreach (var weapon in target.HeldWeapons ?? [])
                {
                    if (weapon.EquipmentId is not { } id)
                    {
                        continue;
                    }

                    if (attack.DemolitionCharge is not null && weapon.Kind == "asl:gun")
                    {
                        if (ScenarioA1GunDestruction.Fate(ScenarioA1GunDestruction.DemolitionCharge, own.Result, false) is { } dcFate)
                        {
                            fates.Add(new FireWeaponDestruction(id, unit.Id, own.Result, dcFate, "C11.6"));
                        }

                        continue;
                    }

                    if (id == ordnanceGun || !unit.KilledByKia || own.Column < 0)
                    {
                        continue;
                    }

                    var key = "weaponDestruction:" + id;
                    if (attack.Rolls!.WeaponDestruction?.TryGetValue(id, out var dr) != true)
                    {
                        undecided.Add("asl.a1.fire.roll-missing:" + key);
                        return null;
                    }

                    usedRolls.Add(key);
                    var result = reference.Result(dr, own.Column);
                    var fate = ScenarioA1GunDestruction.Random(result, weapon.Malfunctioned == true,
                        reference.Definitions.GetValueOrDefault(weapon.DefinitionId ?? string.Empty)?.BreakdownRemoves == true);
                    fates.Add(new FireWeaponDestruction(id, unit.Id, result, fate ?? "none", "A9.74")
                    {
                        Dr = dr
                    });
                }
            }

            return fates.Count == 0 ? null : fates;
        }

        /// <summary>
        /// A FT, MOL, or DC at the AFV it names (A22.34, A22.612, A23.5, C7.344, C7.346, D.8A; pass 60, rulings R60.15, R60.16). A FT's DR is its To Kill
        /// DR on the FT/MOL column, and the Location's other units are spared. A MOL that passed its Check makes the same DR its To Kill DR, and its four FP
        /// go to the crew's Specific Collateral Attack alone; one that failed, or broke its user, attacks as before, the AFV's CE crew by a General
        /// Collateral Attack. A DC rolls its Position DR: at the malfunction number nothing detonates; 12 or more spares the AFV and attacks the other
        /// units; less spares them, and its second DR, the IFT DR, is the To Kill DR (8 or less) or the crew's attack (9 to 11). If the AFV stands, its
        /// CE crew takes the Specific Collateral Attack on that DR. False when a roll is missing.
        /// </summary>
        private bool NamedVehicleAttack(FireArithmetic arithmetic)
        {
            var named = attack.TargetVehicle!;
            var armor = reference.Armor[named.DefinitionId!];
            var dice = arithmetic.Dice;
            decimal collateralFp;
            var shift = 0;
            OrdnanceKill? kill = null;
            if (attack.DemolitionCharge is { } charge)
            {
                if (attack.Rolls!.Position is not { } positionDice)
                {
                    undecided.Add("asl.a1.fire.roll-missing:position");
                    return false;
                }

                usedRolls.Add("position");
                position = Position(charge, named, armor, positionDice);
                vehicleSpared = true;

                // A23.4 (p. 90): "An Original malfunction DR of 12 (or >= 10 if captured) for a Placed/Thrown DC attack" removes it without detonation.
                if (ScenarioA1DemolitionChargeRules.DcMalfunction(position.OriginalDr, AssaultWeaponRemoval(reference.Definitions[charge.DefinitionId!],
                    reference.Definitions.GetValueOrDefault(charge.UserDefinitionId ?? string.Empty), charge.Captured == true, charge.Inexperienced == true)))
                {
                    dcMalfunctioned = true;
                    targetsSpared = true;
                    return true;
                }

                if (position.Result == FirePosition.None)
                {
                    return true;
                }

                targetsSpared = true;
                collateralFp = reference.Definitions[charge.DefinitionId!].Firepower!.Value;
                if (position.Result != FirePosition.Poor)
                {
                    kill = ChargeKill(named, armor, position, dice);
                    if (kill is null)
                    {
                        return false;
                    }
                }
            }
            else if (Flamethrower)
            {
                targetsSpared = vehicleSpared = true;
                collateralFp = arithmetic.TotalFirepower;
                shift = arithmetic.ColumnShift;
                kill = FlameKill(ScenarioA1FlameKill.Flamethrower, named, armor, dice);
            }
            else
            {
                if (molCheck is not { Passed: true, UserBroken: false })
                {
                    return true;
                }

                vehicleSpared = true;
                collateralFp = arithmetic.TotalFirepower + 4;
                shift = arithmetic.ColumnShift;
                kill = FlameKill(ScenarioA1FlameKill.Molotov, named, armor, dice);
            }

            // D10.1 (p. 210; pass 65, ruling R65.23): a wreck attacked as its vehicle burns on a Burn and takes nothing else; it has no crew and no PRC.
            if (named.Wreck == true)
            {
                vehicleKill = kill is null ? null : ScenarioA1WreckRules.WreckKill(kill);
                return true;
            }

            if (kill is not null && !KillChecks(ref kill, named, armor))
            {
                return false;
            }

            vehicleKill = kill;

            // D.8, D.8A (p. 192; pass 60): an AFV left standing exposes its Vulnerable crew to a full-strength attack with the CE DRM alone, on the same
            // Original DR; none after a Dud, nor at the crew after a Shock or an Abandonment, nor at a crew stunned, shocked, or BU. The Collateral Attack
            // Table's note 1 (p. 193; pass 65, the referee's review): its PRC are attacked whenever the AFV is not destroyed, its Passengers spared once
            // the crew is or becomes shocked; only an attack that carries the PRC (ruling R65.5) attacks them, so an older record reads as before.
            var standing = kill is null || kill.Result is not (OrdnanceKill.Dud or OrdnanceKill.Burn or OrdnanceKill.Eliminated);
            var shocked = kill is not null && (kill.Result == OrdnanceKill.Shock || kill.Shocked == true);
            var becomeImpaired = shocked || kill?.Result == OrdnanceKill.Stun;
            var crewAttacked = !shocked && kill?.Abandoned != true;
            var column = Array.FindLastIndex(ScenarioA1FireReference.ColumnFp, fp => fp <= collateralFp) - shift;
            if (!standing || column < 0)
            {
                return true;
            }

            FireVehicleEffect? effect = null;
            string? result = null;
            if (crewAttacked && named.CrewExposed == true && named.CrewImpaired != true && named.Abandoned != true)
            {
                var morale = named.CrewMorale ?? reference.AfvCrewMorale(armor.Nationality)!.Value;
                var final = dice[0] + dice[1] + 2;
                result = reference.Result(final, column);
                if (CrewChecks(result))
                {
                    if (attack.Rolls!.CollateralCheck is null)
                    {
                        undecided.Add("asl.a1.fire.roll-missing:collateralCheck");
                        return false;
                    }

                    usedRolls.Add("collateralCheck");
                }

                var (check, crew) = CrewOutcome(result, attack.Rolls!.CollateralCheck, named.StunRecovery == true, morale, named.CrewMorale);
                effect = new FireVehicleEffect(named.VehicleId!, named.DefinitionId!, FireVehicleEffect.None, null, [new FireModifier("crew-exposed", 2m, "D5.31")], final,
                    null, check, crew);
            }

            // D.8A (pass 65, ruling R65.7): the PRC the named AFV carries take the same attack, when the record carries them.
            var reached = !becomeImpaired ? named : named with
            {
                CrewImpaired = true
            };
            if (SpecificOnPrc(reached, column, dice[0] + dice[1], ordnanceHit: false) is not { } onPrc)
            {
                return false;
            }

            if (onPrc.Attacks is not null || onPrc.Desperation is not null)
            {
                effect = (effect ?? new FireVehicleEffect(named.VehicleId!, named.DefinitionId!, FireVehicleEffect.None, null, [], dice[0] + dice[1], null, null,
                    FireVehicleEffect.NotVulnerable)) with
                {
                    PrcAttacks = onPrc.Attacks,
                    PrcDesperation = onPrc.Desperation,
                };
            }

            if (effect is not null)
            {
                specificCollateral = new OrdnanceCollateral(ScenarioA1FireReference.ColumnFp[column], result ?? onPrc.Attacks?[0].Result ?? reference.Result(dice[0] + dice[1], column),
                    effect);
            }

            return true;
        }

        /// <summary>What a proposal states of the attack on the named AFV: the FT's or MOL's Final TK# with its modifiers, or the DC's Position DRM.</summary>
        public string? Terms(OrdnanceVehicleTarget named, ArmorDefinition armor)
        {
            if (attack.DemolitionCharge is null && !Flamethrower && (attack.Firers ?? []).All(item => item.Mol != true))
            {
                return null;
            }

            if (attack.DemolitionCharge is { } charge)
            {
                var drm = Position(charge, named, armor, [1, 1]).Drm;
                return $"a Position DR at {named.VehicleId} first{ScenarioA1FlameKill.ModifierText(drm)} (C7.346): 5 or less, a To Kill DR against 16 less its Aerial AF; "
                    + "6 to 8, against 16 less its AF; 9 to 11, its CE crew alone; 12 or more, the Location's other units alone, as Area Fire";
            }

            var ft = Flamethrower;
            var kill = FlameKill(ft ? ScenarioA1FlameKill.Flamethrower : ScenarioA1FlameKill.Molotov, named, armor, [1, 1]);
            return $"a To Kill DR at {named.VehicleId} on the {(ft ? "FT" : "MOL")} column, Basic TK# {kill.BasicTk}{ScenarioA1FlameKill.ModifierText(kill.Modifications)}, "
                + $"no AF: Final TK# {kill.FinalTk}; below it burns, equal eliminates (C7.34, C7.344){(ft ? string.Empty : ", if the MOL Check passes (A22.612)")}";
        }

        /// <summary>
        /// A DC's Position DR at an AFV (C7.346, p. 177; D2.41, A23.4, A23.6, A23.62): +2 for a Non-Stopped, Motion, or concealed target; +2 Thrown; +1
        /// through the hull front; +1 CE; +1 Thrown in the AFPh but by an Opportunity Firer; +1 CX; -1 through the hull rear; -1 (-2 OT) for an elevation
        /// advantage (Case B, applied inversely); -2 Immobile; -2 OT. The Bypass DRM is never reached: a DC is not Placed or Thrown into its own Location.
        /// A colored dr below the white strikes a turret (C3.9).
        /// </summary>
        private FirePosition Position(FireDemolitionCharge charge, OrdnanceVehicleTarget named, ArmorDefinition armor, IReadOnlyList<int> dice)
        {
            var drm = new List<FireModifier>();
            if (named.NonStopped == true || named.Concealed == true)
            {
                drm.Add(new FireModifier("non-stopped-or-concealed", 2m, "C7.346"));
            }

            if (charge.Mode == FireDemolitionCharge.Thrown)
            {
                drm.Add(new FireModifier("thrown-dc", 2m, "A23.6"));
            }

            if (named.HullFacing == "front")
            {
                drm.Add(new FireModifier("hull-front", 1m, "C7.346"));
            }

            if (named.CrewExposed == true)
            {
                drm.Add(new FireModifier("crew-exposed", 1m, "C7.346"));
            }

            if (charge.Mode == FireDemolitionCharge.Thrown && attack.Phase == "AFPh" && charge.OpportunityFire != true)
            {
                drm.Add(new FireModifier("thrown-dc-afph", 1m, "A23.62"));
            }

            if (charge.Cx == true)
            {
                drm.Add(new FireModifier("cx:" + charge.UserId, ScenarioA1DemolitionChargeRules.DcCxDrm(true), "A23.4"));
            }

            if (named.HullFacing == "rear")
            {
                drm.Add(new FireModifier("hull-rear", -1m, "C7.346"));
            }

            if (attack.SameLevel == false && attack.TargetLevelAbove is < 0)
            {
                drm.Add(new FireModifier("case-b", armor.OpenTopped ? -2m : -1m, "C7.22"));
            }

            if (named.Immobile == true)
            {
                drm.Add(new FireModifier("immobile", -2m, "C7.346"));
            }

            if (armor.OpenTopped)
            {
                drm.Add(new FireModifier("open-topped", -2m, "C7.346"));
            }

            var original = dice[0] + dice[1];
            var final = original + (int)drm.Sum(item => item.Value);
            return new FirePosition([.. dice], original, drm, final, armor.Turreted && dice[0] < dice[1] ? "turret" : "hull", ScenarioA1FlameKill.PositionResult(final));
        }

        /// <summary>
        /// A DC's To Kill DR (C7.346, C7.21, C7.12; p. 190): the Basic TK# of 16, +1 through the rear Target Facing of the part struck, less the Aerial AF
        /// (an optimal Position) or the AF of that part's facing; results on the DC column.
        /// </summary>
        private static OrdnanceKill ChargeKill(OrdnanceVehicleTarget named, ArmorDefinition armor, FirePosition position, IReadOnlyList<int> dice)
        {
            var location = position.HitLocation;
            var facing = location == "turret" ? named.TurretFacing ?? named.HullFacing ?? "rear" : named.HullFacing ?? "rear";
            var basic = ScenarioA1FlameKill.BasicTk(ScenarioA1FlameKill.DemolitionCharge);
            var modifiers = new List<FireModifier>();
            if (facing == "rear")
            {
                modifiers.Add(new FireModifier("case-a", 1m, "C7.21"));
            }

            var modified = basic + (int)modifiers.Sum(item => item.Value);
            var af = position.Result == FirePosition.Optimal ? ScenarioA1FlameKill.AerialAf(armor)!.Value : ScenarioA1ArmorReference.ArmorFactor(armor, location, facing)!.Value;
            var finalTk = modified - af;
            var dr = dice[0] + dice[1];

            // D5.7, C7.7 note A (pass 65, ruling R65.21): a red CS# burns on a Final TK DR one lower, for the burn alone.
            var result = ScenarioA1FlameKill.ChargeResult(dr, finalTk, location == "turret");
            return new OrdnanceKill(ScenarioA1FlameKill.DemolitionCharge, location, facing, basic, modifiers, modified, af, finalTk, [.. dice], dr,
                result == OrdnanceKill.Eliminated && ScenarioA1CrewSurvivalRules.BurnsRed(armor.CsRed, dr, lower => lower * 2 <= finalTk) ? OrdnanceKill.Burn : result);
        }

        /// <summary>
        /// A FT's or MOL's To Kill DR (A22.34, A22.612, C7.344, C7.21, C7.22, C.8; p. 190), on its Original DR with nothing added: a FT's Basic TK# of 8,
        /// halved at Long Range before +1 CE and +2 OT (both if both); a MOL's of 6, +2 OT or else +1 CE, -2 against a moving target, and +1 (+2 OT) from at
        /// least a level higher; both +1 through the rear (any attack from within the AFV's Location); no AF. Results on the FT/MOL column.
        /// </summary>
        private OrdnanceKill FlameKill(string weapon, OrdnanceVehicleTarget named, ArmorDefinition armor, IReadOnlyList<int> dice)
        {
            var basic = ScenarioA1FlameKill.BasicTk(weapon);
            var modifiers = new List<FireModifier>();
            var firer = attack.Firers![0];
            if (weapon == ScenarioA1FlameKill.Flamethrower)
            {
                // C7.344: halved first, so the halving is the first modification.
                var ft = firer.Weapons!.Select(item => reference.Definitions[item.DefinitionId!]).First(item => item.IsFt);
                if (RangeOf(attack, firer) > ft.Range)
                {
                    modifiers.Add(new FireModifier("long-range", -basic / 2m, "C7.344"));
                }

                if (named.CrewExposed == true)
                {
                    modifiers.Add(new FireModifier("crew-exposed", 1m, "A22.34"));
                }

                if (armor.OpenTopped)
                {
                    modifiers.Add(new FireModifier("open-topped", 2m, "A22.34"));
                }
            }
            else
            {
                if (armor.OpenTopped)
                {
                    modifiers.Add(new FireModifier("open-topped", 2m, "A22.612"));
                }
                else if (named.CrewExposed == true)
                {
                    modifiers.Add(new FireModifier("crew-exposed", 1m, "A22.612"));
                }

                if (named.Moving == true)
                {
                    modifiers.Add(new FireModifier("moving-target", -2m, "C.8"));
                }

                var mol = attack.Firers!.First(item => item.Mol == true);
                if (LevelAboveOf(attack, mol) < 0)
                {
                    modifiers.Add(new FireModifier("case-b", armor.OpenTopped ? 2m : 1m, "C7.22"));
                }
            }

            var facing = named.HullFacing ?? "rear";
            if (facing == "rear")
            {
                modifiers.Add(new FireModifier("case-a", 1m, "C7.21"));
            }

            var finalTk = basic + (int)modifiers.Sum(item => item.Value);
            var dr = dice[0] + dice[1];

            // D5.7, C7.7 note A (pass 65, ruling R65.21): a red CS# burns on a Final TK DR one lower, for the burn alone.
            var result = ScenarioA1FlameKill.FlameResult(dr, finalTk);
            return new OrdnanceKill(weapon, "hull", facing, basic, modifiers, finalTk, null, finalTk, [.. dice], dr,
                result == OrdnanceKill.Eliminated && ScenarioA1CrewSurvivalRules.BurnsRed(armor.CsRed, dr, lower => lower < finalTk) ? OrdnanceKill.Burn : result);
        }

        /// <summary>
        /// The crew's checks after a To Kill result (C7.42, D5.5, D5.6), as an ordnance hit's are: a DC's Shock is automatic; its Immobilization makes the
        /// crew take a TC, failure Abandoning the AFV; an elimination that is no burning wreck gives the crew its Crew Survival DR, +1 when it was
        /// Stunned, Shocked, Recalled, or under a "+1" counter. False when a roll is missing.
        /// </summary>
        private bool KillChecks(ref OrdnanceKill kill, OrdnanceVehicleTarget named, ArmorDefinition armor)
        {
            var plusOne = named.StunRecovery == true ? 1 : 0;
            if (kill.Result == OrdnanceKill.Shock)
            {
                kill = kill with
                {
                    Shocked = true
                };
            }
            else if (kill.Result == OrdnanceKill.Immobilized && named.CrewMayTakeTc == true)
            {
                if (attack.Rolls!.TaskCheck is not { } check)
                {
                    undecided.Add("asl.a1.fire.roll-missing:taskCheck");
                    return false;
                }

                usedRolls.Add("taskCheck");
                var tc = ScenarioA1CrewSurvivalRules.TaskCheck(check, named.CrewMorale ?? reference.AfvCrewMorale(armor.Nationality)!.Value, plusOne == 1, named.CrewMorale);
                kill = kill with
                {
                    CrewCheck = tc,
                    Abandoned = tc.Passed ? null : true,
                };
            }
            else if (kill.Result == OrdnanceKill.Eliminated && ScenarioA1CrewSurvivalRules.CrewSurvival(armor, named.Abandoned == true) is { } cs)
            {
                if (attack.Rolls!.CrewSurvival is not { } survival)
                {
                    undecided.Add("asl.a1.fire.roll-missing:crewSurvival");
                    return false;
                }

                usedRolls.Add("crewSurvival");
                kill = kill with
                {
                    CrewSurvival = ScenarioA1CrewSurvivalRules.Survival(cs, survival,
                        ScenarioA1CrewSurvivalRules.SurvivalDrm(false, named.CrewImpaired == true, false, named.StunRecovery == true, named.Prc is not null))
                };
            }

            // D6.9 (p. 207; pass 65, ruling R65.9): each Passenger and Rider of an AFV eliminated, not burned, rolls its own Survival DR.
            if (kill.Result == OrdnanceKill.Eliminated)
            {
                var (prcSurvival, missing) = PrcSurvivals(named.Prc, armor.CrewSurvival, attack.Rolls!.PrcSurvival);
                if (missing is not null)
                {
                    undecided.Add("asl.a1.fire.roll-missing:" + missing);
                    return false;
                }

                foreach (var made in prcSurvival ?? [])
                {
                    usedRolls.Add("prcSurvival:" + made.UnitId);
                }

                kill = kill with
                {
                    PrcSurvival = prcSurvival
                };
            }

            return true;
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

                var columnFp = concealedGroup && arithmetic.Concealed is { } concealedColumn ? concealedColumn.ColumnFp : arithmetic.ColumnFp;
                foreach (var unit in byFt)
                {
                    taken[unit.Id] = (own, columnFp is { } fpColumn ? Array.IndexOf(ScenarioA1FireReference.ColumnFp, fpColumn) : -1);
                }

                Apply(own, [.. byFt]);
            }
        }

        /// <summary>
        /// The units marked as having fired or directed: every firer and directing leader, a leader's partner (A9.12), and the firing vehicle, except a
        /// squad that fires one MG apart from its inherent FP, which keeps that FP for another attack (A7.351; ruling R12.4).
        /// </summary>
        private string[] Marked() => attack.SprayShare == true ? [] : [.. (attack.Firers ?? [])
            // A7.9 (pass 45): a unit that cowers is marked "regardless of whether it was using its inherent FP".
            .Where(item => cowering.Contains(item.UnitId!)
                || !(item.UsesInherentFp == false && item.Weapons is [_] && reference.Definitions.GetValueOrDefault(item.DefinitionId!)?.Kind == "asl:squad"))
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
            if (vehicleBreakdown is not null)
            {
                if (undecided.Count != 0)
                {
                    return null;
                }
                var outcomes = VehicleBreakdowns(overrun.VehicleId!, VehicleBreakdownUse.Overrun, arithmetic.OriginalDr);
                return outcomes is null ? null : new FireOverrunEffect(overrun.VehicleId!,
                    [.. outcomes.Where(item => item.Value.Outcome == VehicleBreakdownOutcome.Malfunction).Select(item => item.Key)],
                    outcomes.Count == 0 && arithmetic.OriginalDr == 12)
                {
                    VehicleBreakdowns = outcomes
                };
            }

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

            // C11.2, C11.5 (p. 182; pass 60, the first referee): a Gun's gunshield and Emplacement protect "the Good Order manning crew (only)", never a
            // vehicle of its Location. Applied by an attack that carries the pass's fact (ruling R60.3), as every attack planned since does.
            bool Own(FireModifier modifier) => TargetOwn(modifier) || (attack.RandomDestruction == true
                && (modifier.Name.StartsWith("gunshield:", StringComparison.Ordinal) || modifier.Name.StartsWith("emplacement:", StringComparison.Ordinal)));
            var drm = arithmetic.Drm.Where(item => !Own(item)).ToList();
            var final = arithmetic.OriginalDr + (int)drm.Sum(item => item.Value);
            if (VehicleSelection(arithmetic) is not { } selection)
            {
                return null;
            }

            var (selectionDrs, spared) = selection;

            var effects = new List<FireVehicleEffect>();
            foreach (var vehicle in attack.Vehicles!)
            {
                // A12.13 (ruling R6.7): a concealed vehicle in a Location with known targets takes the halved FP's column.
                var columnFp = vehicle.Concealed == true && arithmetic.Concealed is { } halved ? halved.ColumnFp : arithmetic.ColumnFp;
                var column = columnFp is { } fp ? Array.IndexOf(ScenarioA1FireReference.ColumnFp, fp) : -1;
                var id = vehicle.VehicleId!;
                var definition = reference.Definitions[vehicle.DefinitionId!];
                if (definition.Unarmored == true || AirBurstUnarmored(attack.OrdnanceHit is { Area: true }, attack.TargetTerrain, definition))
                {
                    // C3.331, C3.332 and its EX, C3.71, D5.311 (pp. 170, 203; pass 60, rulings R60.10, R60.12): on the Area Target Type the vehicle takes
                    // the TEM that gives it no HD, reversed by a Critical Hit, and the Air Bursts in woods; an OT AFV hit by Air Bursts takes none of them.
                    // C3.71 (p. 170; pass 60, the first referee): a Critical Hit on the Infantry Target Type reverses the vehicle's own positive Case Q,
                    // Height Advantage included, as it does its Location's Infantry's.
                    var line = attack.OrdnanceHit is { Area: not true, CriticalHit: true }
                        ? [.. drm, .. arithmetic.Drm.Where(item => item.Name.StartsWith("critical-hit-tem:", StringComparison.Ordinal)
                            && !item.Name.StartsWith("critical-hit-tem:afv-cover:", StringComparison.Ordinal))]
                        : attack.OrdnanceHit is not { Area: true } ? drm
                        : AirBurstUnarmored(attack.OrdnanceHit is { Area: true }, attack.TargetTerrain, definition) ? [.. drm.Where(item => item.Name != "air-burst")]
                        : [.. arithmetic.Drm.Where(item => !Own(item) || item.Name.StartsWith("tem:", StringComparison.Ordinal)
                            || (item.Name.StartsWith("critical-hit-tem:", StringComparison.Ordinal) && item.Name != "critical-hit-tem:height-advantage"))];
                    var lineFinal = arithmetic.OriginalDr + (int)line.Sum(item => item.Value);
                    var kill = column < 0 ? (int?)null : reference.KillNumber(column);
                    var result = kill is not { } number ? FireVehicleEffect.None
                        : lineFinal * 2 <= number ? FireVehicleEffect.BurningWreck
                        : lineFinal < number ? FireVehicleEffect.Eliminated
                        : lineFinal == number ? FireVehicleEffect.Immobilized
                        : FireVehicleEffect.None;
                    int? unlikely = null;
                    bool? declined = null;

                    // A7.308 (p. 56; pass 65, ruling R65.26): a vehicle Random Selection leaves out is not affected by the DR; its PRC still are.
                    var notSelected = spared.Contains(id);
                    if (notSelected)
                    {
                        result = FireVehicleEffect.None;
                    }

                    // A7.309: after any Original 2 the firer may make the Unlikely Kill dr; a dr worse than the Original 2's own result does not
                    // cancel it (ruling R5.8). A burning wreck cannot be bettered.
                    if (arithmetic.OriginalDr == 2 && result != FireVehicleEffect.BurningWreck && kill is not null && !notSelected)
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

                    // D10.1 (p. 210; pass 65, ruling R65.23): an unarmored wreck burns on the line's burning wreck and takes nothing else: no crew, no PRC.
                    if (vehicle.Wreck == true)
                    {
                        effects.Add(new FireVehicleEffect(id, definition.Id, ScenarioA1WreckRules.WreckLineResult(result), kill, line, lineFinal, unlikely, null, FireVehicleEffect.None)
                        {
                            UnlikelyKillDeclined = declined,
                        });
                        continue;
                    }

                    // D5.311 (p. 203; pass 65, ruling R65.26): the Inherent crew of an OT AFV under Air Bursts is attacked as Personnel, where the attack carries
                    // the vehicle's PRC fact; a crew it breaks or eliminates takes no TC.
                    FireCheck? unprotectedCheck = null;
                    var unprotectedResult = FireVehicleEffect.None;
                    int? unprotectedFinal = null;
                    if (vehicle.Prc is not null && result is (FireVehicleEffect.None or FireVehicleEffect.Immobilized) && column >= 0
                        && AirBurstUnarmored(attack.OrdnanceHit is { Area: true }, attack.TargetTerrain, definition) && vehicle.Abandoned != true)
                    {
                        unprotectedFinal = arithmetic.OriginalDr + (int)drm.Sum(item => item.Value);
                        var crewOutcome = reference.Result(unprotectedFinal.Value, column);
                        IReadOnlyList<int>? crewDice = null;

                        // Pass 70 (ruling R70.10): a crew the record says is already broken takes no PTC, and a failed MC eliminates it.
                        var crewBroken = vehicle.CrewBroken == true;
                        if (UnprotectedCrewChecks(crewOutcome, crewBroken))
                        {
                            if (attack.Rolls!.Checks?.TryGetValue(id, out crewDice) != true)
                            {
                                undecided.Add("asl.a1.fire.roll-missing:checks:" + id);
                                return null;
                            }

                            usedRolls.Add("checks:" + id);
                        }

                        (unprotectedCheck, unprotectedResult) = UnprotectedCrewOutcome(crewOutcome, crewDice,
                            vehicle.CrewMorale ?? reference.AfvCrewMorale(definition.Nationality)!.Value, vehicle.CrewMorale, crewBroken);
                    }

                    var crew = LineCrew(vehicle, definition, unprotectedResult is FireVehicleEffect.Broken or FireVehicleEffect.Eliminated ? FireVehicleEffect.None : result);
                    if (crew is null)
                    {
                        return null;
                    }

                    var lineEffect = WithPrc(new FireVehicleEffect(id, definition.Id, result, kill, line, lineFinal, unlikely, unprotectedCheck, unprotectedResult)
                    {
                        UnprotectedCrewFinalDr = unprotectedFinal,
                        UnlikelyKillDeclined = declined,
                        TaskCheck = crew.Value.TaskCheck,
                        Abandoned = crew.Value.TaskCheck is { Passed: false } ? true : null,
                        CrewSurvival = crew.Value.Survival,
                        RandomSelectionDr = selectionDrs.TryGetValue(id, out var selectionDr) ? selectionDr : null,
                        NotSelected = notSelected ? true : null,
                    }, vehicle, definition, column, arithmetic, drm);
                    if (lineEffect is null)
                    {
                        return null;
                    }

                    effects.Add(lineEffect);
                    continue;
                }

                // D10.1 (pass 65, ruling R65.23): an armored wreck is unharmed by the IFT (A7.307) and has no crew to attack.
                if (vehicle.Wreck == true)
                {
                    effects.Add(new FireVehicleEffect(id, definition.Id, FireVehicleEffect.None, null, drm, final, null, null, FireVehicleEffect.NotVulnerable));
                    continue;
                }

                // D6.84 (p. 207; pass 65, ruling R65.18): a Carrier's crew "remain CE if stunned or shocked, and are subject to the Collateral Attack",
                // where the attack carries the vehicle's PRC (ruling R65.5), as every attack planned since the pass does. An Abandoned AFV has no crew inside to
                // attack (D5.41; the referee's review), where the attack carries that fact.
                if (vehicle.CrewExposed != true || (vehicle.Prc is not null && vehicle.Abandoned == true)
                    || (vehicle.Stunned == true && !(vehicle.Prc is not null && ScenarioA1CarrierRules.IsCarrier(definition.VehicleClass))))
                {
                    var unexposed = WithPrc(new FireVehicleEffect(id, definition.Id, FireVehicleEffect.None, null, drm, final, null, null, FireVehicleEffect.NotVulnerable),
                        vehicle, definition, column, arithmetic, drm);
                    if (unexposed is null)
                    {
                        return null;
                    }

                    effects.Add(unexposed);
                    continue;
                }

                // B10.31, D5.31 (pp. 125, 203; referee's review of pass 55): "The CE DRM is not cumulative with any positive TEM". A vehicle's MG
                // firing from below leaves the crew its +2 without Height Advantage, and a Canister attack leaves it without a wall's or a
                // hedge's TEM too. Infantry fire keeps the line it had before the pass (ruling R10.4), so that its records resolve as they did.
                // D5.31 (pass 65, ruling R65.7): an attack that carries the vehicle's PRC leaves the crew neither Height Advantage nor a wall's or hedge's TEM.
                var fact = vehicle.Prc is not null;
                List<FireModifier> crewDrm =
                [
                    .. drm.Where(item => !((attack.VehicleFire is not null || attack.Canister is not null || fact) && item.Name == "height-advantage")
                        && !((attack.Canister is not null || fact) && item.Name is "wall" or "hedge")),
                ];

                // D5.31, D6.61 (pp. 203, 205; pass 65, the referee's review): an OT AFV's CE DRM is one less for each full level of the firer's elevation
                // advantage beyond the range, as its Passengers' is, where the attack carries the PRC fact. Treating the AFV as unarmored when it is
                // reduced (D5.311) is pass 105's.
                var beyondRange = fact && definition.OpenTopped == true ? Elevation().Beyond : 0;
                var crewCe = ScenarioA1PrcRules.CeDrm(beyondRange);
                if (crewCe > 0)
                {
                    crewDrm.Add(new FireModifier("crew-exposed", crewCe, beyondRange > 0 ? "D6.61" : "D5.31"));
                }
                var crewFinal = arithmetic.OriginalDr + (int)crewDrm.Sum(item => item.Value);
                var outcome = column < 0 ? "none" : reference.Result(crewFinal, column);
                // D3.42, D3.45 (p. 200; pass 55, rulings R55.17, R55.18): the crew checks at its Armor Leader's Morale Level, or an Inexperienced
                // Crew's 6; his leadership modifier is not added, since he takes the check himself.
                var morale = vehicle.CrewMorale ?? reference.AfvCrewMorale(definition.Nationality)!.Value;
                IReadOnlyList<int>? dice = null;
                if (CrewChecks(outcome))
                {
                    if (attack.Rolls!.CrewChecks?.TryGetValue(id, out dice) != true)
                    {
                        undecided.Add("asl.a1.fire.roll-missing:crewCheck:" + id);
                        return null;
                    }

                    usedRolls.Add("crewCheck:" + id);
                }

                // D5.342 (p. 204): a second Stun Recalls the crew; only a Carrier's crew is attacked while Stunned (D6.84). Under the attack's StunReading
                // (the D5.34 claim, ruling RD5.4) a merely Stunned crew's MC takes no +1; before it, the Stunned crew took the +1 too.
                var (check, crewResult) = attack.StunReading == true
                    ? CrewOutcome(outcome, dice, ScenarioA1StunCalculator.Drm(StunDr.MoraleCheck, vehicle.StunRecovery == true),
                        ScenarioA1StunCalculator.SecondStun(vehicle.Stunned == true, vehicle.StunRecovery == true), morale, vehicle.CrewMorale)
                    : CrewOutcome(outcome, dice, vehicle.StunRecovery == true || vehicle.Stunned == true, morale, vehicle.CrewMorale);
                var crewEffect = WithPrc(new FireVehicleEffect(id, definition.Id, FireVehicleEffect.None, null, crewDrm, crewFinal, null, check, crewResult),
                    vehicle, definition, column, arithmetic, drm);
                if (crewEffect is null)
                {
                    return null;
                }

                effects.Add(crewEffect);
            }

            return effects;
        }

        /// <summary>
        /// A7.308 (p. 56; pass 65, ruling R65.26): "nor can more vehicle/horse counters be affected by a DR than the highest KIA# of that column as randomly
        /// determined". When an IFT attack's unarmored vehicles, wrecks aside, outnumber its column's highest KIA#, each makes a Random Selection dr (keyed
        /// <c>randomSelection:</c>, as a target unit's is): the highest drs are affected, a tie in the attack's order, and the others are not. Only an
        /// attack that carries every vehicle's PRC holds more than one vehicle (ruling R25.3 for the others), so an older record makes no selection.
        /// Null when a dr is missing.
        /// </summary>
        private (IReadOnlyDictionary<string, int> Drs, HashSet<string> Spared)? VehicleSelection(FireArithmetic arithmetic)
        {
            var line = attack.Vehicles!.Where(vehicle => vehicle.Wreck != true && reference.Definitions[vehicle.DefinitionId!].Unarmored == true).ToArray();
            var column = arithmetic.ColumnFp is { } fp ? Array.IndexOf(ScenarioA1FireReference.ColumnFp, fp) : -1;
            if (attack.OrdnanceHit is not null || column < 0 || line.Length <= reference.HighestKia(column))
            {
                return (new Dictionary<string, int>(StringComparer.Ordinal), []);
            }

            var drs = new Dictionary<string, int>(StringComparer.Ordinal);
            foreach (var vehicle in line)
            {
                if (attack.Rolls!.RandomSelection?.TryGetValue(vehicle.VehicleId!, out var dr) != true)
                {
                    undecided.Add("asl.a1.fire.roll-missing:randomSelection:" + string.Join(",", line.Select(item => item.VehicleId)));
                    return null;
                }

                drs[vehicle.VehicleId!] = dr;
                usedRolls.Add("randomSelection:" + vehicle.VehicleId);
            }

            var affected = line.OrderByDescending(item => drs[item.VehicleId!]).Take(reference.HighestKia(column)).Select(item => item.VehicleId!).ToHashSet(StringComparer.Ordinal);
            return (drs, [.. line.Select(item => item.VehicleId!).Where(id => !affected.Contains(id))]);
        }

        /// <summary>
        /// What the Vehicle line's result does to the vehicle's Inherent crew (D5.5, D5.6, D9.2, A7.308, pp. 204, 209, 56; pass 65, ruling R65.21), for an
        /// attack that carries the vehicle's PRC, as every attack planned since the pass does; an older record takes neither. An immobilized vehicle's crew,
        /// neither Stunned nor Shocked and still inside, takes a TC (keyed <c>crewCheck:</c>, as a crew's check is); one eliminated without burning rolls
        /// its Crew Survival DR (keyed <c>vehicleSurvival:</c>). Only a crew: an unarmed vehicle's Inherent Driver "is never treated as a crew" (D5.1),
        /// and its cs# is printed in lower case. Null when a roll is missing.
        /// </summary>
        private (FireCheck? TaskCheck, OrdnanceCrewSurvival? Survival)? LineCrew(FireVehicle vehicle, FireDefinition definition, string result)
        {
            if (vehicle.Prc is null || reference.Armor.GetValueOrDefault(definition.Id) is not { } armor)
            {
                return (null, null);
            }

            var id = vehicle.VehicleId!;
            var shocked = vehicle.Shocked == true;
            var abandoned = vehicle.Abandoned == true;
            if (result == FireVehicleEffect.Immobilized && ScenarioA1CrewSurvivalRules.CrewSurvival(armor, false) is not null && vehicle.Immobilized != true
                && vehicle.Stunned != true && !shocked && !abandoned)
            {
                if (attack.Rolls!.CrewChecks?.TryGetValue(id, out var check) != true)
                {
                    undecided.Add("asl.a1.fire.roll-missing:crewCheck:" + id);
                    return null;
                }

                if ((vehicle.CrewMorale ?? reference.InherentCrewMorale(definition.Nationality!, definition.Unarmored == true)) is not { } morale)
                {
                    undecided.Add("asl.a1.fire.definition-incomplete:crew-morale:" + definition.Nationality);
                    return null;
                }

                usedRolls.Add("crewCheck:" + id);
                return (ScenarioA1CrewSurvivalRules.TaskCheck(check!, morale, vehicle.StunRecovery == true, vehicle.CrewMorale), null);
            }

            if (result == FireVehicleEffect.Eliminated && ScenarioA1CrewSurvivalRules.CrewSurvival(armor, abandoned) is { } cs)
            {
                if (attack.Rolls!.VehicleSurvival?.TryGetValue(id, out var survival) != true)
                {
                    undecided.Add("asl.a1.fire.roll-missing:vehicleSurvival:" + id);
                    return null;
                }

                usedRolls.Add("vehicleSurvival:" + id);
                return (null, ScenarioA1CrewSurvivalRules.Survival(cs, survival!,
                    ScenarioA1CrewSurvivalRules.SurvivalDrm(false, vehicle.Stunned == true, shocked, vehicle.StunRecovery == true, true)));
            }

            return (null, null);
        }

        /// <summary>
        /// D.8, D.8B, D6.61 to D6.66, D6.7, D6.9, A7.211, A7.308 (pp. 192, 205 to 207, 55; pass 65, rulings R65.6 to R65.9), for a vehicle whose facts carry
        /// its PRC: one eliminated without burning gives each PRC its Survival DR, a burning wreck takes them with it (A7.308); one left standing exposes its
        /// Vulnerable PRC to a General Collateral Attack on the attack's column and Original DR. A Rider and the PRC of an unarmored vehicle take the
        /// attack's DRM with the TEM (D6.7, D6.23); a CE Passenger takes the CE DRM instead of Height Advantage or a wall's TEM, never both (D5.31), one less
        /// for each full level of the firer's elevation advantage beyond the range (D6.61); a BU one is attacked only from such an elevation or by TPBF
        /// (D6.61, A7.211). A broken Passenger left BU is put under DM by an attack that could inflict a NMC on a CE one (D6.66), and a broken Vulnerable
        /// PRC by one that could inflict a NMC on it (A10.62). Null when a roll is missing.
        /// </summary>
        private FireVehicleEffect? WithPrc(FireVehicleEffect effect, FireVehicle vehicle, FireDefinition definition, int column, FireArithmetic arithmetic,
            IReadOnlyList<FireModifier> drm)
        {
            if (vehicle.Prc is not { Count: > 0 } aboard)
            {
                return effect;
            }

            if (effect.Result == FireVehicleEffect.Eliminated)
            {
                var (survival, missing) = PrcSurvivals(aboard, reference.Armor.GetValueOrDefault(definition.Id)?.CrewSurvival, attack.Rolls!.PrcSurvival);
                if (missing is not null)
                {
                    undecided.Add("asl.a1.fire.roll-missing:" + missing);
                    return null;
                }

                foreach (var made in survival ?? [])
                {
                    usedRolls.Add("prcSurvival:" + made.UnitId);
                }

                return effect with
                {
                    PrcSurvival = survival
                };
            }

            if (effect.Result == FireVehicleEffect.BurningWreck || column < 0)
            {
                return effect;
            }

            var (beyond, reached) = Elevation();
            // D5.311 (p. 203; pass 65, the referee's review): the PRC of an OT AFV under Air Bursts are Vulnerable even if BU.
            reached = reached || AirBurstUnarmored(attack.OrdnanceHit is { Area: true }, attack.TargetTerrain, definition);
            var ce = ScenarioA1PrcRules.CeDrm(beyond);

            // A4.62, D.6, D.8B (pp. 51, 192; pass 65, the referee's review): Hazardous Movement is the moving units', never the PRC's; and Mud's cushion
            // (E3.62) is a TEM, which a CE Passenger does not add to its CE DRM (D5.31).
            drm = [.. drm.Where(item => item.Name != "hazardous-movement")];
            List<FireModifier> exposedDrm = [.. drm.Where(item => item.Name is not ("height-advantage" or "wall" or "hedge" or "weather-cushion"))];
            List<FireModifier> buttonedDrm = [.. exposedDrm, new FireModifier("crew-exposed", 2m, "D6.66")];
            if (ce > 0)
            {
                exposedDrm.Add(new FireModifier("crew-exposed", ce, beyond > 0 ? "D6.61" : "D5.31"));
            }

            List<FireModifier> openDrm = [.. drm, .. arithmetic.Drm.Where(item => item.Name.StartsWith("tem:", StringComparison.Ordinal))];

            // B10.31 (p. 125; pass 65, the referee's review): an open PRC claims Height Advantage when it has no other positive TEM; the AFV's or wreck's
            // cover that kept it from the Infantry is not theirs (D6.7), so it is read for them apart.
            if (attack.HeightAdvantage == true && attack.OrdnanceHit is null && !Flamethrower && Cover(attack) is not null
                && openDrm.All(item => item.Value <= 0 || !(item.Name.StartsWith("tem:", StringComparison.Ordinal) || item.Name is "wall" or "hedge" or "height-advantage")))
            {
                openDrm.Add(new FireModifier("height-advantage", 1m, "B10.31"));
            }

            var open = new List<FirePrc>();
            var exposed = new List<FirePrc>();
            var desperate = new List<string>();
            foreach (var unit in aboard)
            {
                // D.8 (p. 192; pass 65, the referee's review): a Shocked AFV's Passengers are spared as a Stunned one's are.
                var exposure = ScenarioA1PrcRules.Exposure(unit, definition, vehicle.CrewExposed == true, vehicle.Stunned == true || vehicle.Shocked == true);
                if (exposure == ScenarioA1PrcRules.Open)
                {
                    open.Add(unit);
                }
                else if (exposure == ScenarioA1PrcRules.Exposed || (exposure == ScenarioA1PrcRules.Buttoned && reached))
                {
                    exposed.Add(unit);
                }
                else if (exposure == ScenarioA1PrcRules.Buttoned && unit.Broken == true && CouldCauseNmc(buttonedDrm, column))
                {
                    desperate.Add(unit.UnitId!);
                }
            }

            var attacks = new List<FirePrcAttack>();
            foreach (var (group, groupDrm) in new[] { (open, openDrm), (exposed, exposedDrm) })
            {
                if (group.Count == 0)
                {
                    continue;
                }

                if (CouldCauseNmc(groupDrm, column))
                {
                    desperate.AddRange(group.Where(unit => unit.Broken == true).Select(unit => unit.UnitId!));
                }

                if (!PrcAttack(FirePrcAttack.General, vehicle.VehicleId!, group, groupDrm, arithmetic.OriginalDr, column, attacks))
                {
                    return null;
                }
            }

            return effect with
            {
                PrcAttacks = attacks.Count > 0 ? attacks : null,
                PrcDesperation = desperate.Count > 0 ? desperate : null,
            };
        }

        /// <summary>
        /// D6.61, A7.211 (pp. 205, 55; pass 65): the least elevation advantage beyond the range among the firers (an attack with none, its own), and
        /// whether every firer reaches a BU Passenger: from an elevation advantage beyond the range, or by TPBF from within its Location at the same or a
        /// higher level (Small Arms only).
        /// </summary>
        private (int Beyond, bool Reached) Elevation()
        {
            (int Range, int Above)[] firers = attack.Firers is { Count: > 0 } list
                ? [.. list.Select(firer => (RangeOf(attack, firer) ?? 0, -LevelAboveOf(attack, firer)))]
                : [(attack.Range ?? 0, attack.SameLevel == true ? 0 : -(attack.TargetLevelAbove ?? 0))];
            var beyond = firers.Min(item => Math.Max(0, item.Above - item.Range));
            var smallArms = attack.OrdnanceHit is null && attack.Firers is { Count: > 0 };
            var reached = firers.All(item => item.Above > item.Range || (smallArms && item.Range == 0 && item.Above >= 0));
            return (beyond, reached);
        }

        /// <summary>Whether an attack on this column with these DRM could inflict at least a NMC (A10.62, D6.66): its lowest Original DR, 2, would.</summary>
        private bool CouldCauseNmc(IReadOnlyList<FireModifier> drm, int column) =>
            ScenarioA1Definitions.AtLeastNmc.Contains(reference.Result(2 + (int)drm.Sum(item => item.Value), column));

        /// <summary>
        /// A Collateral Attack on PRC (D.8, D.8A, D.8B; pass 65, ruling R65.7): the units take the IFT result of their own Final DR on the attack's column, as
        /// target units take theirs (A7.301 to A7.305), with their rolls keyed by their ids. False when a roll is missing.
        /// </summary>
        private bool PrcAttack(string kind, string vehicleId, IReadOnlyList<FirePrc> units, IReadOnlyList<FireModifier> drm, int originalDr, int column,
            List<FirePrcAttack> made)
        {
            var group = new List<TargetState>();
            foreach (var unit in units)
            {
                if (!prc.TryGetValue(unit.UnitId!, out var own))
                {
                    own = new TargetState(new FireTarget(unit.UnitId, unit.DefinitionId, "aboard:" + vehicleId, unit.Broken, unit.Pinned, false, false, false,
                        unit.Wounded, false)
                    {
                        Elr = unit.Elr,
                        Fanatic = unit.Fanatic,

                        // Pass 70 (ruling R70.8): with the pass's facts, the unit's vehicle, for D6.651's reach and A10.2's LLMC; an older record has none.
                        Aboard = unit.VehicleMoving is { } moving ? new FireAboard(vehicleId, unit.Role, moving, unit.CeInUnmovedHalftrack) : null,
                    }, reference.Definitions[unit.DefinitionId!]);
                    prc[unit.UnitId!] = own;
                }

                group.Add(own);
            }

            var final = originalDr + (int)drm.Sum(item => item.Value);
            var result = reference.Result(final, column);
            Apply(result, group);

            // A10.2, D6.651 (pass 70, ruling R70.8): a Passenger leader the attack eliminated or broke checks his fellow Passengers it attacked; a Rider's
            // loss checks no one. A record of before the pass carries no such fact and takes none.
            if (undecided.Count == 0 && group.Any(item => item.Target.Aboard is not null))
            {
                LeaderLoss(group);
            }

            if (undecided.Count != 0)
            {
                return false;
            }

            made.Add(new FirePrcAttack(kind, drm, final, result, [.. group.Select(item => item.Effect())]));
            return true;
        }

        /// <summary>
        /// The Specific Collateral Attack on the PRC of the AFV an attack names (D.8, D.8A, D6.71, p. 192; pass 65, ruling R65.7), on the attack's Original
        /// DR and collateral column: the CE DRM alone on an exposed Passenger, no DRM on a Rider or an unarmored vehicle's PRC, none on a BU one. A broken
        /// BU Passenger is put under DM by any ordnance hit, or by an attack that could inflict a NMC on a CE one (D6.66). Null when a roll is missing.
        /// </summary>
        public (IReadOnlyList<FirePrcAttack>? Attacks, IReadOnlyList<string>? Desperation)? SpecificOnPrc(OrdnanceVehicleTarget named, int column, int originalDr, bool ordnanceHit)
        {
            if (named.Prc is not { Count: > 0 } aboard || column < 0)
            {
                return (null, null);
            }

            var definition = reference.Definitions[named.DefinitionId!];
            IReadOnlyList<FireModifier> ceDrm = [new FireModifier("crew-exposed", 2m, "D5.31")];
            var open = new List<FirePrc>();
            var exposed = new List<FirePrc>();
            var desperate = new List<string>();
            foreach (var unit in aboard)
            {
                var exposure = ScenarioA1PrcRules.Exposure(unit, definition, named.CrewExposed == true, named.CrewImpaired == true);
                if (exposure == ScenarioA1PrcRules.Open)
                {
                    open.Add(unit);
                }
                else if (exposure == ScenarioA1PrcRules.Exposed)
                {
                    exposed.Add(unit);
                }
                else if (exposure == ScenarioA1PrcRules.Buttoned && unit.Broken == true && (ordnanceHit || CouldCauseNmc(ceDrm, column)))
                {
                    desperate.Add(unit.UnitId!);
                }
            }

            var attacks = new List<FirePrcAttack>();
            foreach (var (group, groupDrm) in new[] { (open, (IReadOnlyList<FireModifier>)[]), (exposed, ceDrm) })
            {
                if (group.Count == 0)
                {
                    continue;
                }

                if (CouldCauseNmc(groupDrm, column))
                {
                    desperate.AddRange(group.Where(unit => unit.Broken == true).Select(unit => unit.UnitId!));
                }

                if (!PrcAttack(FirePrcAttack.Specific, named.VehicleId!, group, groupDrm, originalDr, column, attacks))
                {
                    return null;
                }
            }

            return (attacks.Count > 0 ? attacks : null, desperate.Count > 0 ? desperate : null);
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
        /// Opportunity Fire uses Multiple ROF in the AFPh). Keeps its answer (the D3.7 claim, RD3.6): no X# and no circled B#, which
        /// <see cref="ScenarioA1VehicleBreakdownCalculator.Numbers"/> and <see cref="ScenarioA1VehicleBreakdownCalculator.Outcome"/> give (W-D3.7).
        /// </summary>
        private FireWeaponEffect VehicleWeaponEffect(FireVehicleFire vehicle, FireArithmetic arithmetic)
        {
            var definition = reference.Definitions[vehicle.DefinitionId!];
            // E3.741 (ruling R16.14): Extreme Winter lowers the B#.
            // A21.11, A21.12 (pass 65, ruling R65.22): a Temporary crew's captured-weapon penalty lowers the B# two and the ROF one, each time, by
            // ScenarioA1NonQualifiedUseCalculator's arithmetic (the A21.13 claim, ruling RA21.7), the same answer.
            var captured = vehicle.CapturedPenalty ?? 0;
            var breakdown = (definition.Breakdown ?? 12) - (attack.BreakdownReduction ?? 0) - ScenarioA1NonQualifiedUseCalculator.BreakdownLowering(captured);
            var malfunctioned = arithmetic.OriginalDr >= breakdown;
            var retained = !malfunctioned && vehicle.Pinned != true && attack.Phase != "AFPh" && definition.RateOfFire is { } rof
                && arithmetic.Dice[0] <= rof - ScenarioA1NonQualifiedUseCalculator.RofLowering(captured);
            return new FireWeaponEffect(vehicle.VehicleId!, breakdown, malfunctioned, retained, false, FireCounter(), null);
        }

        /// <summary>
        /// A vehicle's weapons after its attack (D3.7, A9.71, pp. 65 and 201; pass 50). Its MA MG as before. A BMG and a CMG have no B# of their
        /// own ("a printed B# &lt; 12 applies only to the MA") and no ROF: each malfunctions on an Original 12, and of two that fired together
        /// Random Selection finds the one, or both on a tie (A9.71: "this rule also applies to multiple MG ... attacks by the same AFV"). The
        /// vehicle's fire counter is carried by the first.
        /// </summary>
        private List<FireWeaponEffect> VehicleWeaponEffects(FireVehicleFire vehicle, FireArithmetic arithmetic)
        {
            if (vehicleBreakdown is not null)
            {
                var outcomes = VehicleBreakdowns(vehicle.VehicleId!, VehicleBreakdownUse.MachineGun, arithmetic.OriginalDr);
                if (outcomes is null)
                {
                    return [];
                }
                var names = vehicle.Weapons is { Count: > 0 } declared ? declared.Select(item => item.Weapon!).ToArray() : [FireOverrunEffect.MainArmament];
                return [.. names.Select((name, index) =>
                {
                    var outcome = outcomes[name];
                    var ma = name == FireOverrunEffect.MainArmament;
                    var rof = reference.Definitions[vehicle.DefinitionId!].RateOfFire;
                    var retained = ma && !outcome.Failed && vehicle.Pinned != true && attack.Phase != "AFPh"
                        && rof is { } rate && arithmetic.Dice[0] <= rate - ScenarioA1NonQualifiedUseCalculator.RofLowering(vehicle.CapturedPenalty ?? 0);
                    return new FireWeaponEffect(vehicle.VehicleId!, outcome.Numbers.BNumber, outcome.Outcome == VehicleBreakdownOutcome.Malfunction,
                        retained, false, index == 0 ? FireCounter() : null,
                        usedRolls.Contains("weaponSelection:" + vehicle.VehicleId + ":" + name) ? attack.Rolls!.WeaponSelection![vehicle.VehicleId + ":" + name] : null)
                    {
                        VehicleWeapon = ma ? null : name,
                        VehicleBreakdown = outcome,
                    };
                })];
            }

            if (vehicle.Weapons is not { Count: > 0 } named)
            {
                return [VehicleWeaponEffect(vehicle, arithmetic)];
            }

            var id = vehicle.VehicleId!;
            var breakdown = 12 - (attack.BreakdownReduction ?? 0) - ScenarioA1NonQualifiedUseCalculator.BreakdownLowering(vehicle.CapturedPenalty ?? 0);
            var selection = new Dictionary<string, int>(StringComparer.Ordinal);
            if (arithmetic.OriginalDr >= breakdown && named.Count > 1)
            {
                var keys = named.Select(item => id + ":" + item.Weapon).ToArray();
                foreach (var key in keys)
                {
                    if (attack.Rolls!.WeaponSelection?.TryGetValue(key, out var dr) != true)
                    {
                        undecided.Add("asl.a1.fire.roll-missing:weaponSelection:" + string.Join(",", keys));
                        return [];
                    }

                    selection[key] = dr;
                    usedRolls.Add("weaponSelection:" + key);
                }
            }

            var highest = selection.Count == 0 ? 0 : selection.Values.Max();
            return [.. named.Select((item, index) => new FireWeaponEffect(id, breakdown,
                arithmetic.OriginalDr >= breakdown && (selection.Count == 0 || selection[id + ":" + item.Weapon] == highest), false, false,
                index == 0 ? FireCounter() : null, selection.Count == 0 ? null : selection[id + ":" + item.Weapon])
            {
                VehicleWeapon = item.Weapon,
            })];
        }

        private Dictionary<string, VehicleBreakdownResolved>? VehicleBreakdowns(string vehicleId, VehicleBreakdownUse use, int originalDr)
        {
            var outcomes = vehicleBreakdown!.Weapons.ToDictionary(item => item.Key,
                item => ScenarioA1VehicleBreakdownResolutionCalculator.Resolve(item.Value, use, originalDr), StringComparer.Ordinal);
            // A9.71 (p.65): select among malfunction/disable candidates only. D3.71 Low Ammo is independent.
            var failed = outcomes.Where(item => item.Value.Failed).Select(item => item.Key).ToArray();
            if (failed.Length > 1)
            {
                var keys = failed.Select(name => vehicleId + ":" + name).ToArray();
                foreach (var key in keys)
                {
                    if (attack.Rolls!.WeaponSelection?.ContainsKey(key) != true)
                    {
                        undecided.Add("asl.a1.fire.roll-missing:weaponSelection:" + string.Join(",", keys));
                        return null;
                    }
                    usedRolls.Add("weaponSelection:" + key);
                }
                var highest = keys.Max(key => attack.Rolls!.WeaponSelection![key]);
                foreach (var name in failed.Where(name => attack.Rolls!.WeaponSelection![vehicleId + ":" + name] != highest))
                {
                    outcomes[name] = outcomes[name] with
                    {
                        Outcome = VehicleBreakdownOutcome.None
                    };
                }
            }
            return outcomes;
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
                    .Where(unit => !unit.IsDummy && !unit.Eliminated && unit.Target.LocationId == leader.Target.LocationId && !IsPrc(unit)
                        && ScenarioA1HeatOfBattle.Subject(unit.Definition, unit.IsHeroType, unit.Berserk, unit.Target.Unarmed == true))
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
        /// <summary>
        /// A firer's own counter (A8.3, A8.31; pass 45, ruling R45.1): in an attack of Subsequent First Fire or FPF a unit that uses neither is marked
        /// First Fire; every other firer takes the attack's counter.
        /// </summary>
        private string? OwnCounter(FireFirer firer) =>
            // A7.9 (pass 45): a unit that cowers "is automatically marked with a Final or Prep Fire counter as appropriate", whatever its fire.
            cowering.Contains(firer.UnitId!) && FireCounter() is not null ? attack.Phase is "PFPh" or "AFPh" ? "prep-fire" : "final-fire"
            : attack.FireKind is SubsequentFirstFire or FinalProtectiveFire && !UsesSubsequentFire(attack, firer) ? "first-fire" : FireCounter();

        /// <summary>The firers whose counter differs from the attack's, by id; null when none does.</summary>
        private Dictionary<string, string>? FirerCounters()
        {
            var own = (attack.Firers ?? []).Where(firer => OwnCounter(firer) is { } counter && counter != FireCounter())
                .ToDictionary(firer => firer.UnitId!, firer => OwnCounter(firer)!, StringComparer.Ordinal);
            return own.Count == 0 || attack.SprayShare == true ? null : own;
        }

        private string? FireCounter() => attack.FireKind switch
        {
            _ when attack.OrdnanceHit is not null => null,

            // C8.4 (pass 55): the Gun's and its crew's counters are the ordnance shot's own.
            _ when attack.Canister is not null => null,
            ResidualFire => null,
            FirstFire => "first-fire",
            BoundingFirstFire or OverrunFire => "bounding-fire",
            SubsequentFirstFire or FinalProtectiveFire => "final-fire",
            _ => attack.Phase is "PFPh" or "AFPh" ? "prep-fire" : "final-fire",
        };

        /// <summary>
        /// Whether a firer is subject to Cowering (A7.9, p. 58): "Cowering affects all fire except that from a SMC, berserk or Fanatic unit ...
        /// British Elite and First Line units, Finns". Pass 45 adds the berserk firer.
        /// </summary>
        private bool CanCower(FireFirer firer) => !reference.Definitions[firer.DefinitionId!].IsHero && !reference.Definitions[firer.DefinitionId!].IsLeader
            && firer.Fanatic != true && firer.Berserk != true && !NeverCowers(reference.Definitions[firer.DefinitionId!]);

        /// <summary>
        /// The units a cowering attack marks (A7.9; pass 45): its one firer that can cower, or of a fire group "Random Selection is used to determine
        /// the unit(s) (and its SW) that becomes marked with a Prep or Final Fire counter": the highest dr, ties with it. Empty when the attack did
        /// not cower; false when a selection dr is missing.
        /// </summary>
        private bool DecideCowering(FireArithmetic arithmetic)
        {
            cowering.Clear();
            if (!arithmetic.Cowered || attack.SprayShare == true || attack.CoweringMarksAndWeaponResidual != true)
            {
                return true;
            }

            var able = (attack.Firers ?? []).Where(CanCower).Select(item => item.UnitId!).ToArray();
            if (able.Length <= 1)
            {
                cowering.UnionWith(able);
                return true;
            }

            var drs = new Dictionary<string, int>(StringComparer.Ordinal);
            foreach (var id in able)
            {
                if (attack.Rolls!.CowerSelection?.TryGetValue(id, out var dr) != true)
                {
                    undecided.Add("asl.a1.fire.roll-missing:cowerSelection:" + string.Join(",", able));
                    return false;
                }

                drs[id] = dr;
                usedRolls.Add("cowerSelection:" + id);
            }

            var highest = drs.Values.Max();
            cowering.UnionWith(drs.Where(item => item.Value == highest).Select(item => item.Key));
            return true;
        }

        /// <summary>
        /// The Residual FP once the weapons' fates are known (A8.221, A8.23; pass 45): none for a weapon that malfunctioned on this DR, and none for
        /// one that kept its Multiple ROF, unless its owner gives the ROF up, which <see cref="FireArithmetic.ResidualFpIfForfeited"/> says.
        /// </summary>
        private FireArithmetic WithWeaponResidual(FireArithmetic arithmetic, IReadOnlyList<FireWeaponEffect>? weapons)
        {
            if (weapons is null || attack.CoweringMarksAndWeaponResidual != true
                || (arithmetic.ResidualFp is null && !weapons.Any(item => item.Malfunctioned || item.RateOfFireRetained)))
            {
                return arithmetic;
            }

            // An ATR leaves no Residual FP in any case (C13.24), and Residual sets it apart itself.
            bool Atr(string id) => (attack.Firers ?? []).SelectMany(firer => firer.Weapons ?? [])
                .Any(weapon => weapon.EquipmentId == id && reference.Definitions.GetValueOrDefault(weapon.DefinitionId ?? string.Empty)?.IsAtr == true);
            decimal Fp(Func<FireWeaponEffect, bool> which) => arithmetic.Firers.Where(item => item.VsConcealed != true && !Atr(item.UnitId)
                    && weapons.Any(weapon => weapon.EquipmentId == item.UnitId && which(weapon)))
                .Sum(item => item.Firepower);
            var broken = Fp(weapon => weapon.Malfunctioned);
            var kept = Fp(weapon => !weapon.Malfunctioned && weapon.RateOfFireRetained);
            if (broken == 0 && kept == 0)
            {
                return arithmetic;
            }

            var left = Residual(arithmetic, residualHindrance, residualLeadership, broken + kept);
            var forfeited = kept == 0 ? left : Residual(arithmetic, residualHindrance, residualLeadership, broken);
            return arithmetic with
            {
                ResidualFp = left,
                ResidualFpIfForfeited = forfeited is { } more && more > (left ?? 0) ? more : null,
            };
        }

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
            // against a non-AFV: every attack but one that names an AFV (A22.612; pass 60, ruling R60.15).
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
                if (user.Kind is "asl:half-squad" || ScenarioA1Definitions.IsCrew(user.Kind))
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

                if (attack.TargetVehicle is null)
                {
                    molDrm.Add(new FireModifier("non-afv", 1m, "A22.611"));
                }

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
                // C3.53, B16.31 (pp. 170, 130; pass 35, task 35.7): halved into a marsh, beside any halving of the Area Target Type.
                known = vsConcealed = hit.Firepower!.Value * (hit.CriticalHit == true ? 2 : 1) / (attack.TargetTerrain == "marsh" ? 2m : 1m);
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

                    // C7.346 (p. 177; pass 60, ruling R60.16): a DC at a named AFV attacks the Location's other units, on a Position DR of 12 or more, as Area
                    // Fire.
                    if (attack.TargetVehicle is not null && charge.Mode != FireDemolitionCharge.Thrower)
                    {
                        multipliers.Add(new FireModifier("area-fire-afv-target", 0.5m, "C7.346"));
                    }

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
            else if (attack.Canister is { } canister)
            {
                // C8.4, C8.41 (p. 178; pass 55, ruling R55.9): the FP of the Gun Size, halved by each of the shot's own facts and, as Area Fire,
                // against "HIP/\"?\" units": once, since a concealed target is halved here and nowhere else.
                FirerFirepower Canister(bool concealedTargets)
                {
                    List<FireModifier> multipliers = [.. ScenarioA1CanisterCalculator.Multipliers(canister)];
                    if (concealedTargets)
                    {
                        multipliers.Add(new FireModifier("area-fire-concealed-target", 0.5m, "C8.4"));
                    }

                    return new FirerFirepower(canister.GunId!, canister.Firepower!.Value, multipliers,
                        multipliers.Aggregate((decimal)canister.Firepower.Value, (value, item) => value * item.Value));
                }

                if (hasKnown)
                {
                    firers.Add(Canister(false));
                    known = firers[^1].Firepower;
                }

                if (hasConcealed)
                {
                    var concealedFp = Canister(true);
                    firers.Add(hasKnown ? concealedFp with
                    {
                        VsConcealed = true
                    } : concealedFp);
                    vsConcealed = concealedFp.Firepower;
                }
            }
            else if (attack.VehicleFire is { } vehicle)
            {
                // D3.5 (pass 50): the FP of the MGs that fire are added together for the one attack.
                string?[] mgs = vehicle.Weapons is { Count: > 0 } named ? [.. named.Select(item => item.Weapon)] : [null];
                if (hasKnown)
                {
                    firers.AddRange(mgs.Select(mg => VehicleFirepower(vehicle, false, mg)));
                    known = firers.Sum(item => item.Firepower);
                }

                if (hasConcealed)
                {
                    FirerFirepower[] concealedFp = [.. mgs.Select(mg => VehicleFirepower(vehicle, true, mg))];
                    firers.AddRange(concealedFp.Select(item => hasKnown ? item with
                    {
                        VsConcealed = true
                    } : item));
                    vsConcealed = concealedFp.Sum(item => item.Firepower);
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

                // A22.611 (ruling R15.4): a passed MOL adds four FP after every other modification, unless its user broke on the colored dr. A22.612 (pass
                // 60, ruling R60.15): at a named AFV the four FP go to its crew's Specific Collateral Attack alone, and the other units take the rest.
                if (molCheck is { Passed: true, UserBroken: false } bonus && attack.TargetVehicle is null)
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
            // A7.9 (p. 58; pass 55): "Cowering affects all fire except that from ... Canister".
            var cowered = !residual && hit is null && attack.VehicleFire is null && attack.Overrun is null && attack.DemolitionCharge is null && attack.Canister is null
                && dice[0] == dice[1] && !directed
                && attack.Firers!.Any(CanCower)
                && attack.FireLane != true;

            // A7.9 (p. 58; pass 45): "doubled ... for an attack by Inexperienced (19.33) Personnel (even in conjunction with other troops)". A
            // Conscript is always Inexperienced; a Green unit is so by its fact (A19.3), and by its class where a record gives none.
            var inexperienced = (attack.Firers ?? []).Any(item => reference.Definitions[item.DefinitionId!].Class == "conscript"
                || (attack.CoweringMarksAndWeaponResidual == true ? item.Inexperienced ?? reference.Definitions[item.DefinitionId!].Class == "green"
                    : reference.Definitions[item.DefinitionId!].Class == "green"));
            var shift = cowered ? (inexperienced ? 2 : 1) : 0;

            var drm = new List<FireModifier>();
            // B23.741 (the B23.74 claim, RB23F.5): a Factory's TEM, when the attack carries one, in place of the terrain key's; the wall and hedge rule
            // (B9.3) below reads the same number.
            var tem = attack.FactoryTem ?? ScenarioA1FireReference.Tem[attack.TargetTerrain!];
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

                // B13.3, C11.2 (p. 128; pass 60, ruling R60.12): the -1 "is always applicable", even with "emplacements": with the Emplacement TEM.
                if (hit.CriticalHit != true && GunCrewAlone(attack) && attack.GunTarget is { Emplaced: true } dugIn)
                {
                    drm.Add(new FireModifier("emplacement:" + dugIn.GunId, 2m, "C11.2"));
                }
            }
            else if (hit is { Area: true, CriticalHit: not true } && GunCrewAlone(attack) && attack.GunTarget is { Emplaced: true } emplaced && tem < 2)
            {
                // C11.2, C3.331 (pass 60, ruling R60.14): an Emplaced Gun's crew takes the Emplacement TEM instead of a lower one.
                drm.Add(new FireModifier("emplacement:" + emplaced.GunId, 2m, "C11.2"));
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

                // C3.71, B10.31 (p. 170; pass 60, ruling R60.14): the Height Advantage the target claimed on the To Hit DR is reversed as well.
                if (hit.CriticalHit == true && hit.HeightAdvantage == true)
                {
                    drm.Add(new FireModifier("critical-hit-tem:height-advantage", -1m, "C3.71"));
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
                drm.Add(attack.FactoryTem is null ? new FireModifier("tem:" + attack.TargetTerrain, tem, "A7.6") : new FireModifier("tem:factory", tem, "B23.741"));
            }

            // B23.32, C3.71 (pp. 136, 170; pass 60, ruling R60.11): +1 for each non-rooftop building level above the target Location, against an Area
            // Target Type hit, which a Critical Hit does not reverse.
            if (hit is { Area: true } && attack.LevelsAbove is > 0 and var above)
            {
                drm.Add(new FireModifier("tem:levels-above", above, "B23.32"));
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
                // C8.4 (pass 55): "LOS Hindrances and the individual TEM of each Location may vary the results in each hex".
                : attack.VehicleFire is not null || attack.Overrun is not null || attack.Canister is not null ? attack.Los!.HindranceDrm!.Value
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

            // A7.7 (ruling R12.11; the D5.34 claim, ruling RD5.16): +1 when an Encircled unit fires in the group, once however many do (A7.52).
            if ((attack.Firers ?? []).FirstOrDefault(item => item.Encircled == true) is { } encircled
                && ScenarioA1EncirclementCalculator.FirerDrm(true) is > 0 and var encircledDrm)
            {
                drm.Add(new FireModifier("encircled:" + encircled.UnitId, encircledDrm, "A7.7"));
            }

            // A7.7 (p. 57; the D5.34 claim, ruling RD5.16): a Canister shot has no firers (C8.4), so its crew's Encircled is a fact of the Canister,
            // read here alone: +1 on the IFT DR, and no other firer term. Null is the reading before the claim.
            if (attack.Canister is { Encircled: true } encircledCanister && ScenarioA1EncirclementCalculator.FirerDrm(true) is > 0 and var canisterDrm)
            {
                drm.Add(new FireModifier("encircled:" + encircledCanister.GunId, canisterDrm, "A7.7"));
            }

            // A4.51 (ruling R5.2): +1 when a CX unit makes or directs the attack, once however many do. A23.4, A23.6, A23.62, C7.346 (pass 60, ruling
            // R60.16): a DC at a named AFV takes its CX, Thrown, and AFPh DRM on its Position DR instead.
            var chargeAtAfv = attack.DemolitionCharge is not null && attack.TargetVehicle is not null;
            if (((attack.Firers ?? []).FirstOrDefault(item => item.Cx == true)?.UnitId ?? Directors(attack).FirstOrDefault(item => item.Cx == true)?.UnitId
                ?? (attack.DemolitionCharge is { Cx: true } cxCharge && !chargeAtAfv ? cxCharge.UserId : null)) is { } exhausted)
            {
                // The DC's +1 is A23.4's (the A23.3 claim, RA23.6); an ordinary firer's is A4.51's.
                drm.Add(new FireModifier("cx:" + exhausted, attack.DemolitionCharge is null ? 1m : ScenarioA1DemolitionChargeRules.DcCxDrm(true),
                    attack.DemolitionCharge is null ? "A4.51" : "A23.4"));
            }

            // A23.6, A23.62 (ruling R15.3): a Thrown DC is +2 at its target and +3 at its thrower's Location, each +1 more in the AFPh unless its
            // thrower is an Opportunity Firer.
            if (attack.DemolitionCharge is { Mode: not FireDemolitionCharge.Placed } thrown && !chargeAtAfv)
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

            // D5.34 (the D5.34 claim): a vehicle under Stun +1 adds one to its MG IFT DR.
            if (attack.VehicleFire is { } recovering && ScenarioA1StunCalculator.Drm(StunDr.MachineGunIft, recovering.StunRecovery == true) is > 0 and var stunDrm)
            {
                drm.Add(new FireModifier("stun-recovery:" + recovering.VehicleId, stunDrm, "D5.34"));
            }

            // D3.52, D3.51 (p. 201; pass 50): a CMG fired outside its TCA adds the To Hit DRM of Case A to the IFT DR, and one fired within a TCA
            // another turret weapon changed in this fire phase pays what that one paid.
            if (attack.VehicleFire is { Weapons.Count: > 0 } turning && VehicleCaseA(turning, reference.Definitions[turning.DefinitionId!]) is > 0 and var caseA)
            {
                drm.Add(new FireModifier("case-a:" + turning.VehicleId, caseA, "D3.52"));
            }

            // C8.4, C3.21, D3.52 (pp. 178, 169, 201; pass 55): a Canister shot adds the Case A of the hexspines its CA changed to the IFT DR.
            if (attack.Canister is { CaseA: > 0 and var canisterCaseA } turned)
            {
                drm.Add(new FireModifier("case-a:" + turned.GunId, canisterCaseA, "D3.52"));
            }

            // D5.34 (p. 203; pass 35, task 35.13 d; the D5.34 claim): and to its OVR DR.
            if (attack.Overrun is { } overrunning && ScenarioA1StunCalculator.Drm(StunDr.Overrun, overrunning.StunRecovery == true) is > 0 and var overrunStun)
            {
                drm.Add(new FireModifier("stun-recovery:" + overrunning.VehicleId, overrunStun, "D5.34"));
            }

            // D3.44, D7.16, D3.45 (pp. 200, 207; pass 55, rulings R55.16, R55.18): a vehicle's Armor Leader modifies the IFT DR of a MA that is a
            // MG, and its OVR DR whatever the weapons; "an Armor Leader cannot modify non-OVR MG/FT (unless MA) attacks", so a BMG or CMG takes none.
            // D7.16, D6.65 (pass 65, ruling R65.13): with Passengers adding FP, one leadership modifier, the Armor Leader's or a Passenger leader's.
            if ((attack.Overrun is { Passengers.Count: > 0 } carrying
                    ? ScenarioA1PassengerActionRules.OverrunLeadership(OverrunPassengers(carrying),
                        ScenarioA1PassengerActionRules.OverrunShare(reference.Definitions[carrying.DefinitionId!].Unarmored == true, carrying.CrewExposed == true,
                            reference.Definitions[carrying.DefinitionId!].VehicleClass) > 0, carrying.ArmorLeadership)
                    : ScenarioA1ArmorLeaders.Modifier(attack.Overrun is { } leading ? leading.ArmorLeadership
                        : attack.VehicleFire is { Weapons: null or { Count: 0 } } mainArmament ? mainArmament.ArmorLeadership : null)) is { } armorLeader)
            {
                drm.Add(armorLeader);
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
            // C6.44 (p. 174; pass 45): a MG's attack on its Bore Sighted Location deducts two, "unless using Spraying Fire or taking a Snap Shot
            // [EXC: vs Infantry, only such an attack conducted as Defensive First Fire (A8.1) qualifies]"; never Residual FP of any form.
            if (attack.MgBoreSighted == true && !residual && attack.FireLane != true && attack.SprayingFire != true && attack.SnapShot != true
                && (attack.FireKind == FirstFire || state.Count == 0))
            {
                drm.Add(new FireModifier("bore-sighting", -2m, "C6.44"));
            }

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
                // D6.5, A4.6, D5.43 (pass 65, ruling R65.14): units loading or unloading take FFNAM always, and FFMO only when unloading from an
                // unarmored conveyance; an attack recorded before the pass carries no (un)loaders and reads as before.
                var embarking = attack.Embarking;
                if (attack.TargetMovement!.AssaultMovement != true || embarking is not null)
                {
                    drm.Add(new FireModifier("ffnam", -1m, embarking is null ? "A4.6" : "D6.5"));
                }

                if (attack.TargetTerrain == "open-ground" && hindrance == 0 && Cover(attack) is null && !noFfmo
                    && (embarking is null || ScenarioA1PassengerCalculator.EmbarkingFfmo(embarking.Unloading == true, embarking.UnarmoredConveyance == true)))
                {
                    drm.Add(new FireModifier("ffmo", -1m, embarking is null ? "A4.6" : "D6.5"));
                }
            }

            // A5.12, A5.131 (p. 52; pass 40): overstacking on the IFT DR, from the firers' side and against the targets'. An ordnance hit took both
            // on its To Hit DR, and they are not taken again when the hit is resolved.
            if (attack.OrdnanceHit is null)
            {
                if (attack.FirerOverstack is { } over and > 0)
                {
                    drm.Add(new FireModifier("overstacked", over, "A5.12"));
                }

                if (attack.TargetOverstack is { } crowded and > 0)
                {
                    drm.Add(new FireModifier("vs-overstacked", -crowded, "A5.131"));
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

            // C11.4 (ruling R8.3): an HE hit whose DR gives no KIA or K on the Gun is a Near Miss, and the crew's gunshield adds +2 to it. C11.4, C11.5
            // (p. 182; pass 60, ruling R60.14): +1 against Indirect Fire, which the crew takes only where it has no positive TEM, the two never cumulative.
            var indirect = ScenarioA1MortarCalculator.IndirectFireOfHit(hit is { Area: true });
            if (hit is { CriticalHit: false } && GunCrewAlone(attack) && attack.GunTarget is { } shielded && GunshieldFaces(attack, shielded)
                && !Regex.IsMatch(result, "^([1-7])?KIA$") && !Regex.IsMatch(result, "^K/([1-4])$")
                && !(indirect && drm.Any(item => item.Value > 0 && (item.Name.StartsWith("tem:", StringComparison.Ordinal) || item.Name.StartsWith("emplacement:", StringComparison.Ordinal)))))
            {
                drm.Add(new FireModifier("gunshield:" + shielded.GunId, indirect ? 1m : 2m, "C11.4"));
                final += indirect ? 1 : 2;
                (column, shifted, result) = Column(main, shift, final);
            }
            FireColumn? second = null;
            if (hasKnown && hasConcealed && !residual && hit is null)
            {
                var (c2, s2, r2) = Column(vsConcealed, shift, final);
                second = new FireColumn(vsConcealed, c2, s2, r2);
            }

            // A23.4 (ruling R15.2): a DC's Original DR at its malfunction number removes it with no effect. C7.346 (pass 60, ruling R60.16): at a named AFV,
            // its Original Position DR does instead.
            if (attack.DemolitionCharge is { Mode: not FireDemolitionCharge.Thrower } exploded && !chargeAtAfv
                && ScenarioA1DemolitionChargeRules.DcMalfunction(original, AssaultWeaponRemoval(reference.Definitions[exploded.DefinitionId!],
                    reference.Definitions.GetValueOrDefault(exploded.UserDefinitionId ?? string.Empty), exploded.Captured == true, exploded.Inexperienced == true)))
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
        private int? Residual(FireArithmetic arithmetic, int hindrance, int leadership, decimal withheld = 0m)
        {
            residualHindrance = hindrance;
            residualLeadership = leadership;
            // A8.223 (referee, pass 10): a Snap Shot leaves no Residual FP.
            if (attack.FireKind is not (FirstFire or SubsequentFirstFire or FinalProtectiveFire) || attack.SnapShot == true)
            {
                return null;
            }

            // C13.24 (pass 9b): an ATR leaves no Residual FP, even in a fire group.
            // Referee, pass 9b: only the known targets' entry, not the halved one against concealed targets.
            var atr = arithmetic.Firers.Where(item => item.VsConcealed != true && (attack.Firers ?? []).SelectMany(firer => firer.Weapons ?? [])
                .Any(weapon => weapon.EquipmentId == item.UnitId && reference.Definitions.GetValueOrDefault(weapon.DefinitionId ?? string.Empty)?.IsAtr == true)).Sum(item => item.Firepower);
            // A8.221, A8.23 (pass 45): the FP of a weapon that malfunctioned, or kept its Multiple ROF, is withheld; the rest's column is taken,
            // lowered by the attack's Cowering shift as the attack's own column was (A8.224).
            var apart = atr + withheld;
            var columns = ScenarioA1FireReference.ColumnFp;
            var rest = Array.FindLastIndex(columns, fp => fp <= arithmetic.TotalFirepower - apart)
                - (withheld != 0 || (atr != 0 && attack.CoweringMarksAndWeaponResidual == true) ? arithmetic.ColumnShift : 0);
            var highest = apart == 0 ? Math.Max(arithmetic.ColumnFp ?? 0, arithmetic.Concealed?.ColumnFp ?? 0) : rest < 0 ? 0 : columns[rest];

            // A8.2 (p. 60; pass 45): "half (up to a maximum of 12 after adjustment as per 8.26; FRD) of the highest IFT FP column used". The half
            // is taken down to an IFT column, lowered a column for each +1 of A8.26, and only then held to 12: a 36 FP attack with one +1 leaves 12.
            if (attack.CoweringMarksAndWeaponResidual != true)
            {
                // An attack recorded before pass 45: the counter at most half the column, lowered by Hindrance, leadership, and hexside TEM.
                var before = Array.FindLastIndex(ResidualCounters, fp => fp <= highest / 2m) - (hindrance + Math.Max(leadership, 0) + (attack.HexsideTem?.Tem ?? 0)
                    + (attack.OrdnanceHit is not null && attack.CushionedOpenGround == true && attack.TargetTerrain == "open-ground" ? 1 : 0));
                return before < 0 ? null : ResidualCounters[before];
            }

            var index = Array.FindLastIndex(columns, fp => fp <= highest / 2m);

            // A8.26 (p. 60): "each positive DRM caused solely by conditions outside (including CX, BU, Stun, leadership modifier, or hexside/bridge
            // TEM ... but excluding Height Advantage and LV DRM) the target hex". B9.31 (ruling R10.5): a wall or hedge TEM claimed against the
            // attack lowers it as a Hindrance does. Pass 45 adds the firers' own DRM: CX, an Encircled firer, a hero's, a Stun, and overstacking.
            var firers = (int)arithmetic.Drm.Where(item => item.Value > 0 && (item.Name.StartsWith("cx:", StringComparison.Ordinal)
                || item.Name.StartsWith("encircled:", StringComparison.Ordinal) || item.Name.StartsWith("hero-mg:", StringComparison.Ordinal)
                || item.Name.StartsWith("stun-recovery:", StringComparison.Ordinal) || item.Name == "overstacked"
                // Pass 55 (D3.45): an Inexperienced Crew's +1 is a leadership modifier of the firer's.
                || item.Name == ScenarioA1ArmorLeaders.InexperiencedModifier)).Sum(item => item.Value);
            index -= hindrance + Math.Max(leadership, 0) + (attack.HexsideTem?.Tem ?? 0) + firers
                // E3.62, E3.731 (rulings R16.12, R16.13): an HE attack cushioned by Mud or Deep Snow in Open Ground leaves one counter less.
                + (attack.OrdnanceHit is not null && attack.CushionedOpenGround == true && attack.TargetTerrain == "open-ground" ? 1 : 0);
            return index < 0 ? null : Math.Min(columns[index], 12);
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

            // D7.11 (pass 65, ruling R65.13): the Passengers the declaration named add their share of their printed FP, tripled; an OVR recorded before
            // the pass names none.
            foreach (var passenger in ScenarioA1PassengerActionRules.OverrunFirepower(OverrunPassengers(overrun), definition.Unarmored == true, overrun.CrewExposed == true, common,
                definition.VehicleClass))
            {
                yield return passenger;
            }
        }

        /// <summary>The Passengers an OVR's record names (D7.11; ruling R65.13), with their definitions.</summary>
        private OverrunPassengerFacts[] OverrunPassengers(FireOverrun overrun) =>
            [.. (overrun.Passengers ?? []).Where(item => item.UnitId is not null && item.DefinitionId is not null && reference.Definitions.ContainsKey(item.DefinitionId))
                .Select(item => new OverrunPassengerFacts(item.UnitId!, reference.Definitions[item.DefinitionId!], item.Wounded == true))];

        /// <summary>
        /// A vehicle's MA AAMG FP (D1.83: Normal Range eight hexes): doubled at Point Blank Range (A7.21), halved beyond its Normal Range
        /// (A7.22), against concealed targets (A7.23), in the AFPh (D3.53), in Motion (D2.42), and when its crew is pinned (A7.82).
        /// </summary>
        private FirerFirepower VehicleFirepower(FireVehicleFire vehicle, bool vsConcealed, string? mg = null)
        {
            var definition = reference.Definitions[vehicle.DefinitionId!];
            var range = attack.Range!.Value;
            var multipliers = new List<FireModifier>();

            // A7.21 (ruling R10.4; pass 55): PBF at an adjacent target at most one level above the firer, as for Infantry. A record made before
            // the pass is at the firer's level.
            if (range == 1 && (attack.SameLevel == true ? 0 : attack.TargetLevelAbove ?? 0) <= 1)
            {
                multipliers.Add(new FireModifier("point-blank-fire", 2m, "A7.21"));
            }

            if (range > VehicleWeaponRange(mg))
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

            // D1.8 (pass 50): the BMG's or the CMG's own FP when the attack names it, under the vehicle's id and the weapon's name ("de-tank BMG").
            var printed = mg is null ? ScenarioA1MachineGunKill.MainArmamentFirepower(reference, definition)!.Value : VehicleWeaponFp(definition, mg)!.Value;
            return new FirerFirepower(mg is null ? vehicle.VehicleId! : vehicle.VehicleId + " " + mg.ToUpperInvariant(), printed, multipliers,
                multipliers.Aggregate((decimal)printed, (value, item) => value * item.Value));
        }

        /// <summary>
        /// Whether a firer adds its inherent FP (A7.351, A7.352): a squad keeps it with one SW, not two; a HS loses it with any; A15.23 (ruling R15.11): so does
        /// a hero.
        /// </summary>
        private static bool UsesInherent(FireFirer firer, FireDefinition definition)
        {
            var weapons = firer.Weapons ?? [];
            return firer.UsesInherentFp != false && !(definition.Kind == "asl:squad" && weapons.Count >= 2)
                && !((definition.Kind == "asl:half-squad" || definition.IsHero) && weapons.Count >= 1);
        }

        private IEnumerable<FirerFirepower> Firepower(bool vsConcealed)
        {
            foreach (var firer in attack.Firers!)
            {
                var definition = reference.Definitions[firer.DefinitionId!];
                var range = RangeOf(attack, firer)!.Value;
                var weapons = firer.Weapons ?? [];

                var inherent = UsesInherent(firer, definition);

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

                    multipliers.AddRange(Mounted(firer));
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

                    // Keeps its answer (the A8.41 claim, ruling RA8.6): a weapon's Area Fire is read here from its unit's First Fire counter, so an
                    // unmarked MG fired alone by a First-Fire-marked unit is halved; ScenarioA1FinalFireCalculator.Verdict reads the weapon's own.
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

                    // A9.4 (p. 64; pass 45): "MG attacks vs unconcealed Infantry at a range >= 17 hexes treat those Infantry as concealed".
                    if (attack.CoweringMarksAndWeaponResidual == true && mg.IsMg && range >= 17 && !vsConcealed && attack.BeyondNvr != true && state.Count > 0)
                    {
                        multipliers.Add(new FireModifier("mandatory-fire-direction", 0.5m, "A9.4"));
                    }

                    // A9.12 (ruling R12.4): a leader fires a MG alone as Area Fire; two SMC together fire it at full FP.
                    if (definition.IsLeader && firer.Partner is null)
                    {
                        multipliers.Add(new FireModifier("smc-area-fire", 0.5m, "A9.12"));
                    }

                    multipliers.AddRange(Mounted(firer));
                    var fp = multipliers.Aggregate((decimal)mg.Firepower!.Value, (value, item) => value * item.Value);
                    yield return new FirerFirepower(weapon.EquipmentId!, mg.Firepower.Value, multipliers, fp) { Operator = firer.UnitId };
                }
            }
        }

        /// <summary>
        /// A Passenger's Mounted Fire multipliers (D6.1, D6.63; pass 65, ruling R65.10), from the vehicle facts its record carries; none for a firer on foot.
        /// </summary>
        private IReadOnlyList<FireModifier> Mounted(FireFirer firer) =>
            firer.Mounted is { DefinitionId: { } vehicle } mounted
                ? ScenarioA1PassengerActionRules.Multipliers(reference.Definitions.GetValueOrDefault(vehicle)?.Unarmored == true, mounted.Moved == true, mounted.Motion == true, attack.Phase,
                    mounted.Rider == true)
                : [];

        /// <summary>A firer's Normal Range: a wounded hero's is his wounded side's (A15.2).</summary>
        private static int NormalRange(FireDefinition definition, FireFirer firer) =>
            firer.Wounded == true && definition.WoundedRange is { } wounded ? wounded : definition.Range!.Value;

        private List<FireModifier> Multipliers(int range, int normalRange, bool vsConcealed, bool finalFireAgain, bool sustained, int levelAbove, FireFirer firer)
        {
            var multipliers = new List<FireModifier>();

            // A7.21 (rulings R10.4, R10.14): PBF at an adjacent target at most one level above the firer; TPBF in the firer's own Location.
            // Pass 45: PBF, not TPBF, at the next level of the firer's own hex ("doubled while ... ADJACENT to its target"; TPBF is for "units in
            // the same Location as the attacker").
            if ((range == 1 && levelAbove <= 1) || (range == 0 && firer.LocationId != attack.TargetLocationId))
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
            var ownKind = OwnKind(attack, firer);
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

            // A4.63 (pp. 51 to 52; pass 40): Defensive First Fire at a Dashing unit in the road Location is Area Fire, but for a Fire Lane.
            if (attack.Dash == true && attack.FireLane != true && attack.OrdnanceHit is null && attack.FireKind is FirstFire or SubsequentFirstFire or FinalProtectiveFire)
            {
                multipliers.Add(new FireModifier("dash", 0.5m, "A4.63"));
            }

            // A7.24, A7.25 (ruling R12.1): AFPh fire is halved, unless it is Opportunity Fire. D6.22, D6.63 (pp. 205, 206; pass 65, the referee's review):
            // a Rider whose AFV moved or is in Motion is quartered, its movement halving and the AFPh's being one, so it takes no second.
            var riderQuartered = firer.Mounted is { Rider: true } riding && (riding.Moved == true || riding.Motion == true);
            if (attack.Phase == "AFPh" && firer.OpportunityFire != true && !riderQuartered)
            {
                multipliers.Add(new FireModifier("advancing-fire", 0.5m, "A7.24"));
            }

            return multipliers;
        }

        /// <summary>
        /// A19.32 (p. 86: "The Breakdown Number (even inherent B#/X#; 9.7) of any SW used by Inexperienced Personnel is reduced by one"): one lower in
        /// Inexperienced hands. With the pass 72 stamp (ruling R72.9) any SW here, a MG included, by the user's Inexperienced fact (A19.3: a Green MMC stacked
        /// with an unbroken leader is not), or by Conscript class where the fact is absent; a FT's is its removal number's (ruling R15.1). Without it, as
        /// recorded before the pass (referee, pass 9b): an ATR's only, by Green or Conscript class (ruling R9.10).
        /// </summary>
        private int Inexperience(FireWeapon weapon)
        {
            var definition = reference.Definitions.GetValueOrDefault(weapon.DefinitionId ?? string.Empty);
            if (definition is null || definition.IsFt || (attack.Firers ?? []).FirstOrDefault(firer => firer.Weapons?.Contains(weapon) == true) is not { } operatorUnit)
            {
                return 0;
            }

            var @class = reference.Definitions.GetValueOrDefault(operatorUnit.DefinitionId ?? string.Empty)?.Class;
            var inexperienced = attack.UnitSubstitution == true ? operatorUnit.Inexperienced ?? @class == "conscript" : definition.IsAtr && @class is "green" or "conscript";
            return inexperienced ? 1 : 0;
        }

        /// <summary>
        /// A19.131 (p. 86; pass 72, ruling R72.12): "all SW Original B#/X# [EXC: DC] ... are decreased by one during an Ammunition Shortage", a FT's removal number
        /// included; A21.11 (p. 88): "Ammunition Shortage (19.131) penalties do not apply to captured Gun/SW". A DC is no weapon of this list.
        /// </summary>
        private int Shortage(FireWeapon weapon) => attack.AmmunitionShortage == true && weapon.Captured != true ? 1 : 0;

        /// <summary>
        /// The words of a weapon's lowered B#, with its printed one and each cause, as <see cref="WeaponEffects"/> lowers it: "B10 (B11, less 1 for Ammunition
        /// Shortage, A19.131)"; for a weapon the attack removed, its X#, the printed B# less Extreme Winter's (A.11): "X11 (B11, ...)". Null for a FT, a weapon
        /// outside the catalog, and a B# as printed.
        /// </summary>
        internal string? BreakdownWords(FireWeapon weapon, bool removed)
        {
            if (reference.Definitions.GetValueOrDefault(weapon.DefinitionId ?? string.Empty) is not { IsFt: false } definition)
            {
                return null;
            }

            var printed = definition.Breakdown ?? 12;
            List<(int Less, string Cause)> causes = [];
            void Cause(int less, string cause)
            {
                if (less > 0)
                {
                    causes.Add((less, cause));
                }
            }

            Cause(IsSustained(attack, weapon, reference) ? ScenarioA1FinalFireCalculator.SustainedBreakdownReduction : 0, "in Sustained Fire, A9.3");
            Cause(Inexperience(weapon), "in Inexperienced hands, A19.32");
            Cause(ScenarioA1NonQualifiedUseCalculator.BreakdownLowering(weapon.Captured == true ? 1 : 0), "captured, A21.11");
            Cause(attack.BreakdownReduction ?? 0, "in Extreme Winter, E3.741");
            Cause(Shortage(weapon), "for Ammunition Shortage, A19.131");
            if (causes.Count == 0)
            {
                return null;
            }

            var lowered = printed - causes.Sum(item => item.Less);
            var said = string.Join("; ", causes.Select(item => $"less {item.Less.ToString(CultureInfo.InvariantCulture)} {item.Cause}"));
            return removed
                ? $"X{(printed - (attack.BreakdownReduction ?? 0)).ToString(CultureInfo.InvariantCulture)} (B{printed.ToString(CultureInfo.InvariantCulture)}, lowered to B{lowered.ToString(CultureInfo.InvariantCulture)}: {said})"
                : $"B{lowered.ToString(CultureInfo.InvariantCulture)} (B{printed.ToString(CultureInfo.InvariantCulture)}, {said})";
        }

        /// <summary>What the attack did to each MG: malfunction on the Original DR (A9.7, A9.71) and Multiple ROF on the colored die (A9.2).</summary>
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
                    ? -(attack.BreakdownReduction ?? 0) - Shortage(weapon) + AssaultWeaponRemoval(ft, (attack.Firers ?? []).FirstOrDefault(firer => firer.Weapons?.Contains(weapon) == true) is { } user
                        ? reference.Definitions[user.DefinitionId!] : null, weapon.Captured == true,
                        (attack.Firers ?? []).FirstOrDefault(firer => firer.Weapons?.Contains(weapon) == true)?.Inexperienced == true)
                    : (reference.Definitions[weapon.DefinitionId!].Breakdown ?? 12) - (IsSustained(attack, weapon, reference) ? ScenarioA1FinalFireCalculator.SustainedBreakdownReduction : 0) - Inexperience(weapon)
                    - ScenarioA1NonQualifiedUseCalculator.BreakdownLowering(weapon.Captured == true ? 1 : 0) - (attack.BreakdownReduction ?? 0) - Shortage(weapon), StringComparer.Ordinal);
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
                // A7.9 (pass 45): a cowering unit's SW are marked with it, so none keeps its Multiple ROF.
                var retained = !malfunctioned.Contains(id) && !sustained && !(attack.PinnedMgAreaFire == true && operatorUnit?.Pinned == true)
                    && !(operatorUnit is not null && cowering.Contains(operatorUnit.UnitId!))
                    && !(attack.FireKind == FinalProtectiveFire && operatorUnit?.FinalFireMarked == true)
                    && (attack.Phase != "AFPh" || operatorUnit?.OpportunityFire == true)
                    && reference.Definitions[weapon.DefinitionId!].RateOfFire is { } rof && colored <= rof - ScenarioA1NonQualifiedUseCalculator.RofLowering(weapon.Captured == true ? 1 : 0);
                // A.11 (p. 43; pass 45): "it also transforms the weapon's Original B# to an X# during that use": a weapon whose B# this fire lowered is
                // removed on a DR at least its printed B# (the page's LMG in Sustained Fire: B10, X12). A FT's own removal is ruling R15.1's.
                var definition = reference.Definitions[weapon.DefinitionId!];
                var printed = definition.Breakdown ?? 12;
                // A19.131 (pass 72, ruling R72.18, amending R72.12): Ammunition Shortage lowers the B# by one and, by A.11, its Original B# becomes its X#:
                // a B11 LMG is B10 and X11. Only a printed X# (a FT's removal number, above) is itself one lower.
                var lowered = sustained || weapon.Captured == true || Inexperience(weapon) > 0 || Shortage(weapon) > 0;
                var removed = attack.CoweringMarksAndWeaponResidual == true && !definition.IsFt && malfunctioned.Contains(id) && lowered
                    && original >= printed - (attack.BreakdownReduction ?? 0);
                return new FireWeaponEffect(id, breakdown[id], malfunctioned.Contains(id), retained, sustained,
                    retained ? null : sustained ? ScenarioA1FinalFireCalculator.SustainedCounter : operatorUnit is null ? counter : OwnCounter(operatorUnit), selection.TryGetValue(id, out var dr) ? dr : null)
                {
                    Removed = removed ? true : null,
                };
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
                    UnderscoredElr = firer.UnderscoredElr,
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

        /// <summary>
        /// A19.131 (p. 86; pass 72, ruling R72.11): "any MMC of that side which rolls an Original 12 DR on the IFT while using its Inherent FP suffers Unit
        /// Replacement after the attack which caused it is resolved. Any MMC subject to, but incapable of, further quality loss is broken after resolution of the
        /// attack, unless berserk. Not all members of a FG would be subject to Unit Replacement; determine the MMC affected by Random Selection after malfunction
        /// or elimination of applicable SW." Read after the SW's effects and an FPF firer's NMC: a firer that NMC eliminated, Reduced, or Replaced is no longer
        /// the unit that fired. A.9 (p. 43): the highest dr is affected, and "all of the applicable units which rolled that number are affected equally". The
        /// Replaced unit keeps its broken or unbroken status; an underscored squad becomes its two HS (A19.13); an underscored HS and a crew are incapable. "No
        /// effect on CC, Sniper, or Residual FP attacks": the first two are no fire attack, and Residual FP has no firers. Spraying Fire is one attack on one DR
        /// (A9.5): it is read once, with its second Location, after both are resolved. Null when none is affected.
        /// </summary>
        private List<FireUnitEffect>? ShortageReplacements(FireArithmetic arithmetic, IReadOnlyList<FireUnitEffect>? firerEffects)
        {
            if (attack.AmmunitionShortage != true || arithmetic.OriginalDr != 12 || undecided.Count != 0 || (attack.SprayingFire == true && attack.SprayShare != true)
                || attack.FireKind == ResidualFire || attack.VehicleFire is not null)
            {
                return null;
            }

            var after = (firerEffects ?? []).ToDictionary(effect => effect.UnitId, StringComparer.Ordinal);
            var candidates = (attack.Firers ?? []).Where(firer => reference.Definitions.GetValueOrDefault(firer.DefinitionId ?? string.Empty) is { } definition
                    && (definition.IsMmc || definition.Kind == "asl:crew") && UsesInherent(firer, definition)
                    && !(after.TryGetValue(firer.UnitId!, out var nmc) && (nmc.Eliminated || nmc.FinalDefinitionId != nmc.DefinitionId)))
                .ToArray();
            if (candidates.Length == 0)
            {
                return null;
            }

            var drs = new Dictionary<string, int>(StringComparer.Ordinal);
            if (candidates.Length > 1)
            {
                foreach (var firer in candidates)
                {
                    if (attack.Rolls!.ShortageSelection?.TryGetValue(firer.UnitId!, out var dr) != true)
                    {
                        undecided.Add("asl.a1.fire.roll-missing:shortageSelection:" + string.Join(",", candidates.Select(item => item.UnitId)));
                        return null;
                    }

                    drs[firer.UnitId!] = dr;
                    usedRolls.Add("shortageSelection:" + firer.UnitId);
                }
            }

            var highest = drs.Count == 0 ? 0 : drs.Values.Max();
            return [.. candidates.Where(firer => drs.Count == 0 || drs[firer.UnitId!] == highest).Select(firer =>
            {
                var id = firer.DefinitionId!;
                var definition = reference.Definitions[id];
                var nmc = after.GetValueOrDefault(firer.UnitId!);
                var broken = nmc?.Broken ?? firer.Broken == true;
                var pinned = nmc?.Pinned ?? firer.Pinned == true;
                var half = definition is { Kind: "asl:squad", UnderscoredMorale: true } ? ScenarioA1FireReference.HalfSquadOf(id) : null;
                var replacement = half ?? (definition.UnderscoredMorale == true ? null : ScenarioA1FireReference.ReplacementOf(id));
                int? dr = drs.TryGetValue(firer.UnitId!, out var selected) ? selected : null;
                if (replacement is not null)
                {
                    return new FireUnitEffect(firer.UnitId!, id, replacement, dr, false, broken, pinned, false, false, false,
                        [half is not null ? "ammunition-shortage-half-squads" : "ammunition-shortage-replaced"], [])
                    {
                        SplitIntoHalfSquads = half is not null ? true : null,
                    };
                }

                // Incapable of further quality loss: broken, unless berserk.
                var berserk = firer.Berserk == true;
                return new FireUnitEffect(firer.UnitId!, id, id, dr, false, broken || !berserk, pinned, false, false, false,
                    [berserk ? "ammunition-shortage-berserk" : "ammunition-shortage-broken"], []);
            })];
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
                    // The A20.54 claim (rulings RA20.2, RA20.9): ScenarioA1PrisonerCalculator.KiaSurvivor decides; a prisoner or an Unarmed unit is
                    // unarmed there only when the attack carries KiaUnarmedReduction, so an older record breaks it as it did (A7.301, A20.54).
                    else if (ScenarioA1PrisonerCalculator.KiaSurvivor(unit.Broken, unit.IsHeroType, unit.Berserk,
                        attack.KiaUnarmedReduction == true && (unit.Target.GuardId is not null || unit.Target.Unarmed == true)) == KiaSurvivorEffect.Broken)
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

            // A20.54 (ruling R12.9): a prisoner is never broken; one that fails a MC suffers Casualty Reduction, and it takes no Heat of Battle. Pass 70
            // (ruling R70.47; A20.54, p. 88: "A prisoner/unarmed unit which fails a MC is not broken; it suffers Casualty Reduction instead"; A10.3's EXC):
            // so does an Unarmed unit, where its facts carry R70.7's fact; an older record carries none, and its unit breaks as it did.
            if (unit.Target.GuardId is not null || unit.Target.Unarmed == true)
            {
                if (!passed && !Reduce(unit, unit.Target.GuardId is not null ? "casualty-reduced-prisoner" : "casualty-reduced-unarmed"))
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
            if (original == 2 && !unit.Eliminated && ScenarioA1HeatOfBattle.Subject(unit.Definition, unit.IsHeroType, unit.Berserk, unit.Target.Unarmed == true) && !IsPrc(unit))
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

        /// <summary>
        /// A1.23, A19.13 (referee, pass 15): a MMC with an underscored Morale Factor has an ELR of 5, or the ELR an SSR assigns it (A19.132; pass 72, ruling
        /// R72.13); any other unit its side's.
        /// </summary>
        private int? ElrOf(TargetState unit, bool firingSide)
        {
            // A19.11 (p. 86; pass 70, ruling R70.7): "unarmed ... units are never subject to Replacement due to ELR failure", so no failure exceeds theirs.
            if (unit.Target.Unarmed == true)
            {
                return int.MaxValue;
            }

            // Ruling R18.3: a unit's own ELR, from its OB group, before its side's; a unit ELR never Replaces needs none (A19.11).
            var elr = unit.Target.Elr ?? (ElrImmune(unit.Definition.Id, reference) && (firingSide ? attack.FiringSideElr : attack.TargetSideElr) is null ? 5 : Elr(firingSide));
            // A19.132 (p. 86; ruling R72.13): "Whenever a SSR ... specifically assigns an ELR ≤ 4 to units with an underscored Morale Factor, they suffer Unit
            // Replacement normally": the SSR's ELR in place of R15.9's 5.
            return unit.Definition is { IsMmc: true, UnderscoredMorale: true } && elr is not null ? unit.Target.UnderscoredElr ?? 5 : elr;
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
            if (ScenarioA1FireReference.IsCommissar(unit.Definition.Id) || ScenarioA1Definitions.IsCrew(unit.Definition.Kind))
            {
                unit.Note("not-subject-to-replacement");
                unit.Break(null);
                return;
            }

            // A19.13 (ruling R15.9): a squad with an underscored Morale Factor is Replaced by its two broken HS; such a HS is Disrupted instead,
            // unless Fanatic (A19.12). A19.132 (p. 86; pass 72, ruling R72.13): under an SSR ELR it "suffer[s] Unit Replacement normally regardless of the
            // severity of the quality drop", by the table below.
            var underscored = unit.Definition is { IsMmc: true, UnderscoredMorale: true };
            if (underscored && unit.Target.UnderscoredElr is null)
            {
                if (unit.Definition.Kind == "asl:squad" && ScenarioA1FireReference.HalfSquadOf(unit.Definition.Id) is { } own)
                {
                    unit.ReduceTo(reference.Definitions[own], "replaced-by-two-half-squads");
                    unit.Split = true;
                }
                else if (NeverDisrupted(unit) is { } exempt)
                {
                    unit.Note(exempt);
                }
                else
                {
                    unit.Disrupt();
                }

                unit.Break(null);
                return;
            }

            // A19.13: Replaced by a broken unit of lesser quality; A19.12: Disrupted when none exists.
            var replacement = underscored ? ScenarioA1FireReference.UnderscoredReplacementOf(unit.Definition.Id) : ScenarioA1FireReference.ReplacementOf(unit.Definition.Id);
            if (replacement is null && NeverDisrupted(unit) is { } notDisrupted)
            {
                unit.Note(notDisrupted);
            }
            else if (replacement is null)
            {
                unit.Disrupt();
            }
            else
            {
                unit.ReduceTo(reference.Definitions[replacement], "replaced-elr");
            }

            unit.Break(null);
        }

        /// <summary>
        /// Why a unit that cannot be Replaced is only broken, never Disrupted (A19.12, p. 86), as the note it takes; null when it is Disrupted. "Fanatic units, and
        /// PRC are never Disrupted; only broken": a Fanatic unit, and (pass 70, the verification of brief 3b) a PRC whose record carries the pass's facts;
        /// a PRC of an older record is Disrupted as it was.
        /// </summary>
        private static string? NeverDisrupted(TargetState unit) =>
            unit.Fanatic ? "fanatic-not-disrupted" : unit.Target.Aboard is not null ? "prc-not-disrupted" : null;

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
                if (lesser is null && NeverDisrupted(unit) is null)
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

            // A25.221 (ruling R15.6): a Commissar never takes another leader's DRM. Pass 65: nor does a PRC; pass 70 (ruling R70.8): a PRC whose record
            // carries the pass's facts takes the modifier of a leader who reaches it (D6.651).
            if (useLeadership && !unit.Berserk && !ScenarioA1FireReference.IsCommissar(unit.Definition.Id) && (!prc.ContainsKey(unit.Id) || unit.Target.Aboard is not null))
            {
                // A10.21, A10.22: one unbroken, unpinned leader of the Location other than the checker, and of higher
                // morale when the checker is a leader; A10.72: a non-zero modifier, a wounded leader's +1 included, cannot
                // be declined.
                // Rulings R12.8, R12.9: only a leader of the unit's own side, and not a prisoner.
                // A25.221 (ruling R15.6): an active Commissar's leadership alone applies in his Location.
                // Pass 31 (ruling R31.5): with several such leaders the owner chooses (A10.21), so the most favorable modifier is taken.
                // Pass 70 (ruling R70.8): a leader aboard, or a unit aboard, only as D6.651 reaches (A10.7 EXC).
                var pool = state.Values.Concat(prc.Values).Where(other => LeadershipReaches(other, unit)).ToArray();
                var leader = pool.FirstOrDefault(other => ActiveCommissar(other, unit))
                    ?? pool.Where(other => other.Definition.IsLeader && other != unit && !other.Eliminated
                    && !other.Broken && !other.Pinned && !other.IsDummy && other.Target.Berserk != true
                    && (other.Target.Friendly == true) == (unit.Target.Friendly == true) && other.Target.GuardId is null
                    && (!unit.Definition.IsLeader || other.MoraleLevel > unit.MoraleLevel))
                    .OrderBy(other => (other.Leadership ?? 0) + AlliedPenalty(other.Definition, unit.Definition)).FirstOrDefault();

                // A10.7 (ruling R15.8): one worse for Allied Troops of another nationality.
                if (leader?.Leadership is { } leadership && leadership + AlliedPenalty(leader.Definition, unit.Definition) is var allied and not 0)
                {
                    drm.Add(new FireModifier("leadership:" + leader.Id, allied, AlliedPenalty(leader.Definition, unit.Definition) > 0 ? "A10.7" : "A10.21"));
                    leading.Add(leader.Id);
                }
            }

            return (dice!, drm);
        }

        /// <summary>
        /// The LLMC and LLTC of the leaders lost among <paramref name="pool"/> (A10.2), the target Location's units when none is given; pass 70 (ruling R70.8):
        /// the units a Collateral Attack reached aboard one vehicle, when its record carries the pass's facts.
        /// </summary>
        private void LeaderLoss(IReadOnlyCollection<TargetState>? pool = null)
        {
            // Referee, pass 31 (A10.2, A11.141): units in Melee neither take nor cause a LLMC or LLTC.
            if (undecided.Count != 0 || attack.TargetsInMelee == true)
            {
                return;
            }

            var units = pool ?? state.Values;

            // Pass 31 (ruling R31.5; A10.2): every lost leader is taken in turn, one lost to another's LLMC included, the higher Morale Level first; a
            // leader that broke and is then eliminated is taken again for his LLMC.
            var taken = new HashSet<string>(StringComparer.Ordinal);
            var checksOf = new Dictionary<string, int>(StringComparer.Ordinal);
            while (units.Where(unit => unit.Definition.IsLeader && (unit.Eliminated || unit.BrokeInThisAttack) && !taken.Contains(unit.Id + (unit.Eliminated ? ":e" : ":b")))
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
                // Pass 70 (ruling R70.8; A10.2): Riders neither take nor cause one, and Passengers only among the Passengers of one vehicle.
                foreach (var unit in units.Where(unit => unit != leader && !unit.Eliminated && !unit.IsDummy && !unit.Berserk && LeaderLossReaches(leader, unit)
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

            // A7.302 (p. 55; pass 45): "Casualty Reduction eliminates any HS or crew". The Recall exception is a vehicle's Inherent crew's (D5.341).
            if (unit.Definition.Kind is "asl:half-squad" || ScenarioA1Definitions.IsCrew(unit.Definition.Kind))
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
                .Concat(rolls.CowerSelection?.Keys.Select(id => "cowerSelection:" + id) ?? [])
                .Concat(rolls.ShortageSelection?.Keys.Select(id => "shortageSelection:" + id) ?? [])
                .Concat(rolls.HeatOfBattle?.Keys.Select(id => "heatOfBattle:" + id) ?? [])
                .Concat(rolls.BerserkChecks?.Keys.Select(id => "berserkCheck:" + id) ?? [])
                .Concat(rolls.CrewChecks?.Keys.Select(id => "crewCheck:" + id) ?? [])
                .Concat(rolls.UnlikelyKill?.Keys.Select(id => "unlikelyKill:" + id) ?? [])
                .Concat(rolls.MolCheck is null ? [] : ["molCheck"])
                .Concat(rolls.WeaponDestruction?.Keys.Select(id => "weaponDestruction:" + id) ?? [])
                .Concat(rolls.PrcSurvival?.Keys.Select(id => "prcSurvival:" + id) ?? [])
                .Concat(rolls.VehicleSurvival?.Keys.Select(id => "vehicleSurvival:" + id) ?? [])
                .Concat(new[] { ("position", rolls.Position), ("crewSurvival", rolls.CrewSurvival), ("taskCheck", rolls.TaskCheck), ("collateralCheck", rolls.CollateralCheck) }
                    .Where(item => item.Item2 is not null).Select(item => item.Item1));
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

        /// <summary>Whether a Final KIA eliminated the unit (A7.301, A9.74), not a K's or a check's Casualty Reduction.</summary>
        public bool KilledByKia => events.Contains("eliminated-kia");

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
            target.Berserk == true) - ScenarioA1EncirclementCalculator.MoraleDrop(target.Encircled == true, target.Berserk == true, target.Heroic == true);

        /// <summary>
        /// The current Morale Level: one lower when wounded (A17.3); a hero's is printed on his wounded side and never lowered
        /// (A15.2); a heroic leader's is at least 9 (A15.21: a 10-2 is a 1-4-10 hero); a Fanatic unit's is one higher (A10.8);
        /// a hero's or heroic leader's never exceeds 10, or 9 if wounded (A15.2); a berserk unit's is 10, never lowered, and one
        /// higher when Fanatic (A15.42, ruling R30.3). A7.7 (ruling R12.11; the D5.34 claim, ruling RD5.16): an Encircled unit's is one lower, unless it
        /// is berserk or heroic, on its counter or by this attack (<see cref="ScenarioA1EncirclementCalculator.MoraleDrop"/>).
        /// </summary>
        public int? MoraleLevel => Morale(Definition, Broken, Wounded, Fanatic, Heroic, Berserk)
            - ScenarioA1EncirclementCalculator.MoraleDrop(Target.Encircled == true, Target.Berserk == true || Berserk, Target.Heroic == true || Heroic);

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

            // A17.2 (p. 85; pass 71, ruling R71.14): a SMC wounded after spending more than 3 MF is pinned, as the attack's record says of him; ruling R71.27
            // (A7.8, p. 58): never one broken, before the attack or by it.
            if (Target.WoundPins == true && !Pinned && !Broken)
            {
                Pin("pinned-wound");
            }
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
