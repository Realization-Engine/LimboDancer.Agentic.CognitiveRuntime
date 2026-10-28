namespace LimboDancer.Domains.Asl.Rules;

/// <summary>
/// The pure resolution of the Ordnance package (unit step 24): a Gun's HE shot at Infantry on the Infantry Target Type. The To
/// Hit DR is made against the Modified TH# with the firer- and target-based DRM of the reviewed Cases (C3.3, C4, C5, C6); a hit
/// is resolved on the IFT by the Fire package with the Gun's HE FP (C.6), a Critical Hit on the unit Random Selection picks
/// (C3.71, C3.74). Every roll is asked for one at a time as <c>roll-missing:&lt;key&gt;</c>.
/// </summary>
public static class ScenarioA1OrdnanceCalculator
{
    private const string Prefix = "asl.a1.ordnance.";

    private static readonly string[] AdmittedGunTypes = ["at", "inf", "art", "mortar", "latw"];

    private static readonly string[] Personnel = ["asl:squad", "asl:half-squad", "asl:crew", "asl:vehicle-crew", "asl:leader", "asl:hero"];

    public static OrdnanceResolution Resolve(OrdnanceShot shot, ScenarioA1OrdnanceReference reference)
    {
        ArgumentNullException.ThrowIfNull(shot);
        ArgumentNullException.ThrowIfNull(reference);
        var missing = Missing(shot);
        if (missing.Count != 0)
        {
            return Refused(OrdnanceResolution.Indeterminate, missing);
        }

        var outside = Outside(shot, reference);
        if (outside.Count != 0)
        {
            return Refused(OrdnanceResolution.Abstained, outside);
        }

        var undecided = Undecided(shot);
        if (undecided.Count != 0)
        {
            return Refused(OrdnanceResolution.Indeterminate, undecided);
        }

        // A9.61, D3.54 (pass 60): a MG fired as ordnance is read beside the Guns.
        var gun = reference.Weapon(shot.Gun!.DefinitionId)!;
        if (gun.LatwType == "pf")
        {
            // C13.31 (ruling R9.7): the PF Check dr comes first; only a final 1 to 3 gives a shot.
            if (PanzerfaustCheck(shot, reference) is not { } check)
            {
                return Refused(OrdnanceResolution.Indeterminate, [Prefix + "roll-missing:panzerfaustCheck"]);
            }

            if (check.Outcome != OrdnancePanzerfaustCheck.Shot)
            {
                return new OrdnanceResolution(OrdnanceResolution.Resolved, [], null, null)
                {
                    PanzerfaustCheck = check,
                    FirerEffect = check.Outcome == OrdnancePanzerfaustCheck.NoShot ? null : check.Outcome,
                };
            }

            // C13.31, C8.31 (pass 60, ruling R60.5): at Personnel a PF fires on the Infantry Target Type at its HE Equivalency.
            var fired = shot.VehicleTarget is not null ? ScenarioA1ArmorCalculator.Run(shot, gun, reference) : Run(shot, reference);
            return fired.Disposition == OrdnanceResolution.Resolved ? fired with
            {
                PanzerfaustCheck = check
            } : fired;
        }

        // C8.4 (pass 55, ruling R55.9): Canister makes no To Hit DR; its IFT DR decides the shot.
        if (IsCanister(shot))
        {
            return ScenarioA1CanisterCalculator.Run(shot, gun, reference);
        }

        var resolution = shot.TargetType == OrdnanceTargetTypes.Area ? ScenarioA1AreaCalculator.Run(shot, gun, reference)
            : shot.VehicleTarget is not null ? ScenarioA1ArmorCalculator.Run(shot, gun, reference) : Run(shot, reference);

        // C8.9 (the UI review of pass 55): a resolution that says how its ammunition was used says under which Depletion Number, so that its
        // record is worded from itself and not from the catalog of a later day.
        return resolution.AmmunitionUse is null ? resolution : resolution with
        {
            DepletionNumber = ScenarioA1ArmorCalculator.ShotDepletion(shot, gun)
        };
    }

    /// <summary>
    /// The PF Check (C13.31, the SW Chart's note F; ruling R9.7): -1 in 1945, +1 for a HS or crew, +2 for a SMC, +1 if CX; 1 to 3 gives a
    /// shot, 4 or more none, and an Original 6 pins the unit, or breaks it if already pinned (Casualty Reduction if berserk or heroic). Null
    /// while the dr is missing.
    /// </summary>
    private static OrdnancePanzerfaustCheck? PanzerfaustCheck(OrdnanceShot shot, ScenarioA1OrdnanceReference reference)
    {
        if (shot.Rolls!.PanzerfaustCheck is not { } dr)
        {
            return null;
        }

        var crew = shot.Crew!;
        var kind = reference.Fire.Definitions[crew.DefinitionId!].Kind;
        var drm = new List<FireModifier>();
        if (shot.ScenarioYear == 1945)
        {
            drm.Add(new FireModifier("1945", -1, "C13.31"));
        }

        if (kind is "asl:half-squad" || ScenarioA1Definitions.IsCrew(kind))
        {
            drm.Add(new FireModifier("hs-or-crew", 1, "C13.31"));
        }
        else if (kind is "asl:leader" or "asl:hero")
        {
            drm.Add(new FireModifier("smc", 2, "C13.31"));
        }

        if (crew.Cx == true)
        {
            drm.Add(new FireModifier("cx", 1, "C13.31"));
        }

        // C13.31 (p. 183; pass 60, ruling R60.5): +1 in August and September 1943, and +1 at other than an AFV. A PF shot recorded before the pass
        // was at an AFV, after September 1943.
        if (shot.ScenarioYear == 1943 && shot.Hit?.ScenarioMonth is 8 or 9)
        {
            drm.Add(new FireModifier("aug-sept-1943", 1, "C13.31"));
        }

        if (shot.VehicleTarget is null)
        {
            drm.Add(new FireModifier("not-afv", 1, "C13.31"));
        }

        var final = dr + (int)drm.Sum(item => item.Value);
        var outcome = dr == 6
            ? crew.Pinned != true ? OrdnancePanzerfaustCheck.Pinned
                : crew.Berserk == true || kind == "asl:hero" || shot.Panzerfaust!.Heroic == true ? OrdnancePanzerfaustCheck.CasualtyReduction : OrdnancePanzerfaustCheck.Broken
            : final <= 3 ? OrdnancePanzerfaustCheck.Shot : OrdnancePanzerfaustCheck.NoShot;
        return new OrdnancePanzerfaustCheck(dr, drm, final, outcome);
    }

    /// <summary>
    /// The leadership DRM of a leader directing a SW's To Hit DR (A7.531, C9.2, C13.35; ruling R9.2): his leadership modifier, one worse if
    /// wounded (A17.3).
    /// </summary>
    internal static IEnumerable<FireModifier> Leadership(OrdnanceShot shot, ScenarioA1OrdnanceReference reference)
    {
        if (shot.Director is { } director && reference.Fire.Definitions.GetValueOrDefault(director.DefinitionId ?? string.Empty)?.Leadership is { } leadership)
        {
            var value = leadership + (director.Wounded == true ? 1 : 0);
            if (value != 0)
            {
                yield return new FireModifier("leadership:" + director.UnitId, value, "A7.531");
            }
        }
    }

    /// <summary>
    /// A hero's DRM on the To Hit DR of a non-MG SW (A15.23, p. 83; pass 72, ruling R72.8): "a -1 DRM to its To Hit or IFT DR", taken on the To Hit DR of a light
    /// mortar or a LATW, never again on the IFT DR of its hit nor on a To Kill DR (C13.25); and the +1 of one man firing "other SW normally requiring two men",
    /// the PSK (C13.45, C13.48), which the -1 negates. Only a shot that carries the pass's fact (<see cref="OrdnanceShot.HeroWeapons"/>) takes them, and only for a
    /// light mortar or a LATW: the planner stamps every hero's shot, and this member decides the weapon.
    /// </summary>
    internal static IEnumerable<FireModifier> HeroDrm(OrdnanceShot shot, GunDefinition gun, ScenarioA1OrdnanceReference reference)
    {
        if (shot.HeroWeapons != true || shot.Vehicle is not null || gun.GunType is not ("mortar" or "latw")
            || reference.Fire.Definitions.GetValueOrDefault(shot.Crew!.DefinitionId ?? string.Empty)?.IsHero != true)
        {
            yield break;
        }

        yield return new FireModifier("hero:" + shot.Crew.UnitId, -1, "A15.23");
        if (gun.LatwType == "psk")
        {
            // A15.23 (p. 83; the reviews R3.4): the +1 for one man firing a SW normally requiring two is the hero rule's own.
            yield return new FireModifier("hero-two-man:" + shot.Crew.UnitId, 1, "A15.23");
        }
    }

    /// <summary>
    /// The reasons a shot cannot be committed before any roll: empty when every outcome the dice can reach is decided. The
    /// shot must stop only at its missing To Hit DR, and the Fire package must decide both a normal hit and a Critical Hit on
    /// every unit in the target Location.
    /// </summary>
    public static IReadOnlyList<string> Precheck(OrdnanceShot shot, ScenarioA1OrdnanceReference reference)
    {
        ArgumentNullException.ThrowIfNull(shot);
        ArgumentNullException.ThrowIfNull(reference);
        var first = Resolve(shot with
        {
            Rolls = new OrdnanceRolls(null, null, null, null, null)
        }, reference);
        if (first.Disposition != OrdnanceResolution.Indeterminate
            || first.Reasons is not ([Prefix + "roll-missing:toHit"] or [Prefix + "roll-missing:panzerfaustCheck"] or [Prefix + "roll-missing:canister"]))
        {
            return first.Reasons;
        }

        // A Vehicle Target Type shot has no IFT attack to precheck (C3.31). A Canister shot's attacks are fire attacks of their own, one a
        // Location, which the Fire package prechecks (C8.4; pass 55).
        if (shot.VehicleTarget is not null || IsCanister(shot))
        {
            return [];
        }

        var gun = reference.Guns[shot.Gun!.DefinitionId!];
        var reasons = new List<string>();
        foreach (var critical in new[] { false, true })
        {
            // B23.32, C3.332 (pass 60, rulings R60.10, R60.11): a mortar's hit is an attack on each Location of its hex, vehicles included.
            IEnumerable<FireAttack> attacks = shot.TargetType == OrdnanceTargetTypes.Area ? ScenarioA1AreaCalculator.PrecheckAttacks(shot, gun, reference, critical)
                : [HitAttack(shot, gun, reference, critical, HitTargets(shot, gun), null, HitVehicles(shot, reference))];
            reasons.AddRange(attacks.SelectMany(attack => ScenarioA1FireCalculator.Precheck(attack, reference.Fire))
                .Select(reason => Prefix + (critical ? "critical-hit:" : "hit:") + reason));
        }

        return reasons.Distinct(StringComparer.Ordinal).ToList();
    }

    private static OrdnanceResolution Refused(string disposition, IReadOnlyList<string> reasons) => new(disposition, reasons, null, null);

    /// <summary>
    /// Whether the shot is a Canister shot (C8.4, p. 178; pass 55, ruling R55.9): it names the ammunition and no vehicle. Named at a vehicle it is
    /// a Vehicle Target Type shot, which that Target Type refuses ("ineffective vs armored targets").
    /// </summary>
    public static bool IsCanister(OrdnanceShot shot)
    {
        ArgumentNullException.ThrowIfNull(shot);
        return shot.Ammunition == ScenarioA1CanisterCalculator.Ammunition && shot.VehicleTarget is null;
    }

    private static List<string> Missing(OrdnanceShot shot)
    {
        var missing = new List<string>();
        void Need(object? value, string name)
        {
            if (value is null || (value is string text && string.IsNullOrWhiteSpace(text)))
            {
                missing.Add(Prefix + "fact-missing:" + name);
            }
        }

        Need(shot.Phase, "phase");
        Need(shot.FiringSide, "firingSide");
        Need(shot.FiringNationality, "firingNationality");
        Need(shot.TargetLocationId, "targetLocationId");
        Need(shot.Range, "range");
        Need(shot.HexspinesToTurn, "hexspinesToTurn");
        Need(shot.FirerInWoodsOrBuilding, "firerInWoodsOrBuilding");
        Need(shot.ElevationAllowed, "elevationAllowed");
        Need(shot.Acquisition, "acquisition");
        Need(shot.Rolls, "rolls");
        Need(shot.Gun, "gun");
        Need(shot.Gun?.GunId, "gun.gunId");
        Need(shot.Gun?.DefinitionId, "gun.definitionId");
        Need(shot.Gun?.Malfunctioned, "gun.malfunctioned");
        Need(shot.Gun?.ShotsThisPhase, "gun.shotsThisPhase");
        Need(shot.Gun?.RateOfFireKept, "gun.rateOfFireKept");
        Need(shot.Gun?.FiredThisPlayerTurn, "gun.firedThisPlayerTurn");
        Need(shot.Crew, "crew");
        Need(shot.Crew?.UnitId, "crew.unitId");
        Need(shot.Crew?.DefinitionId, "crew.definitionId");
        Need(shot.Crew?.Broken, "crew.broken");
        Need(shot.Crew?.Pinned, "crew.pinned");
        Need(shot.Crew?.Berserk, "crew.berserk");
        Need(shot.Crew?.Concealed, "crew.concealed");
        Need(shot.Crew?.FiredInherentFp, "crew.firedInherentFp");
        Need(shot.Hit, "hit");
        Need(shot.Hit?.Targets, "hit.targets");
        if (IsCanister(shot))
        {
            // C8.4 (pass 55): a Canister shot has no one target Location; each Location it attacks carries its own terrain and LOS in its fire attack.
            ScenarioA1CanisterCalculator.Missing(shot, Need);
        }
        else
        {
            Need(shot.Hit?.TargetTerrain, "hit.targetTerrain");
            Need(shot.Hit?.Los, "hit.los");
            Need(shot.Hit?.Los?.Blocked, "hit.los.blocked");
            Need(shot.Hit?.Los?.HindranceDrm, "hit.los.hindranceDrm");
            Need(shot.Hit?.Los?.HindranceAttributed, "hit.los.hindranceAttributed");
            Need(shot.Hit?.Los?.GrainInLos, "hit.los.grainInLos");
            Need(shot.Hit?.SameLevel, "hit.sameLevel");
        }

        if (shot.VehicleTarget is not null)
        {
            ScenarioA1ArmorCalculator.Missing(shot, Need);
        }

        if (shot.FireKind == ScenarioA1FireCalculator.BoundingFirstFire)
        {
            // C5.31 to C5.33 (pass 50): a Bounding First Firer's MP in a continuous LOS to its target.
            Need(shot.Vehicle?.MpInLos, "vehicle.mpInLos");
        }
        else if (shot.FireKind is not null)
        {
            Need(shot.Movement, "movement");
            Need(shot.Movement?.SpentHere, "movement.spentHere");
            Need(shot.Movement?.ShotsHere, "movement.shotsHere");
            if (shot.VehicleTarget is not null)
            {
                Need(shot.Movement?.MpInLos, "movement.mpInLos");
            }
            else
            {
                Need(shot.Movement?.NonAssault, "movement.nonAssault");
                Need(shot.Movement?.OpenGround, "movement.openGround");
            }
        }

        // Rulings R9.2, R9.4, R9.7: a Spotter, a directing leader, and the Panzerfaust's own facts.
        if (shot.Spotter is { } spotter)
        {
            Need(spotter.UnitId, "spotter.unitId");
            Need(spotter.DefinitionId, "spotter.definitionId");
            Need(spotter.Broken, "spotter.broken");
            Need(spotter.Pinned, "spotter.pinned");
        }

        if (shot.Director is { } director)
        {
            Need(director.UnitId, "director.unitId");
            Need(director.DefinitionId, "director.definitionId");
            Need(director.Broken, "director.broken");
            Need(director.Pinned, "director.pinned");
            Need(director.DirectedThisPlayerTurn, "director.directedThisPlayerTurn");
            Need(director.Wounded, "director.wounded");
        }

        if (shot.Gun?.DefinitionId == OrdnanceTargetTypes.Panzerfaust)
        {
            Need(shot.Panzerfaust, "panzerfaust");
            Need(shot.Panzerfaust?.ShotsTaken, "panzerfaust.shotsTaken");
            Need(shot.Panzerfaust?.ShotsAllowed, "panzerfaust.shotsAllowed");
            Need(shot.Panzerfaust?.FromBuilding, "panzerfaust.fromBuilding");
            Need(shot.Panzerfaust?.Heroic, "panzerfaust.heroic");
            Need(shot.ScenarioYear, "scenarioYear");
            Need(shot.Hit?.ScenarioMonth, "hit.scenarioMonth");
        }

        if (shot.Vehicle is { } vehicle)
        {
            Need(vehicle.ButtonedUp, "vehicle.buttonedUp");
            Need(vehicle.InMotion, "vehicle.inMotion");
            Need(vehicle.Stunned, "vehicle.stunned");
            Need(vehicle.Shocked, "vehicle.shocked");
            Need(vehicle.Recalled, "vehicle.recalled");
        }

        return missing;
    }

    private static List<string> Outside(OrdnanceShot shot, ScenarioA1OrdnanceReference reference)
    {
        var outside = new List<string>();
        var phase = shot.Phase!;

        // A4.63 (p. 52; pass 40): "a weapon with a specified CA may not fire on a Dashing unit in the road Location if it must change its CA to do so".
        if (shot.Movement?.Dashing == true && shot.HexspinesToTurn is > 0)
        {
            outside.Add(Prefix + "dash-covered-arc");
        }

        // Ruling R8.1: Defensive First Fire in the MPh by the non-phasing side.
        // D3.3, C5.3 (pp. 172, 199; pass 50): a vehicle's MA fires in its own MPh as Bounding First Fire.
        var bounding = shot.FireKind == ScenarioA1FireCalculator.BoundingFirstFire;
        if (shot.FireKind is null
            ? (phase, shot.FiringSide) is not (("PFPh", "phasing") or ("AFPh", "phasing") or ("DFPh", "non-phasing"))
            : bounding ? shot.Vehicle is null || (phase, shot.FiringSide) is not ("MPh", "phasing")
            : shot.FireKind != "first-fire" || (phase, shot.FiringSide) is not ("MPh", "non-phasing"))
        {
            outside.Add(Prefix + "phase-outside");
        }

        // C2.21, C2.22, C2.3, C3.33: a Gun of the firing side that can fire HE; mortars (the Area Target Type), RCL, and 360-degree
        // mounts are not reviewed.
        var gunShot = shot.Gun!;
        var canister = IsCanister(shot);

        // C8.31 (p. 178; pass 60, ruling R60.4): AP, APCR, APDS, and HEAT named at a target with no vehicle attack by their HE Equivalency, so a
        // Gun with no HE may fire them there.
        var equivalency = Equivalency(shot);

        // A9.61, D3.54 (pass 60, ruling R60.19): a SW MG, or a vehicle's MA that is a MG, fires as ordnance only at an AFV it names.
        if (reference.Weapon(gunShot.DefinitionId) is not { } gun || gun.Nationality != shot.FiringNationality || gun.Mount360
            || !(gun.GunType == ScenarioA1MachineGunKill.GunType || (shot.Vehicle is null ? AdmittedGunTypes.Contains(gun.GunType) : gun.GunType == "vehicle"))
            || (shot.VehicleTarget is null && !canister && !equivalency && !ScenarioA1ArmorCalculator.Lists(gun, "he") && (gun.NoHe || shot.Ammunition is not (null or "he"))))
        {
            outside.Add(Prefix + "gun-outside");
            return outside;
        }

        var machineGun = gun.GunType == ScenarioA1MachineGunKill.GunType;
        if (machineGun)
        {
            ScenarioA1MachineGunKill.Outside(shot, reference, outside);
        }

        // C8.4 (pass 55): the Canister facts belong to a Canister shot alone.
        if (shot.Canister is not null && !canister)
        {
            outside.Add(Prefix + "canister-outside");
            return outside;
        }

        // C2.21, C8.8 (pp. 167, 179; pass 55, ruling R55.7): a Gun is "always assumed to be firing HE vs unarmored targets" only when that
        // ammunition "is not Special Ammunition (i.e., has no Depletion #; 8.8)": one whose counter lists an HE Depletion Number fires HE only
        // when the shot names it, and not at all before the number's date or once it has run out (C8.9). Another ammunition named there is
        // C8.31's HE Equivalency (pass 60), decided below.
        if (shot.VehicleTarget is null && !canister && !equivalency && ScenarioA1ArmorCalculator.Lists(gun, "he"))
        {
            if (shot.Ammunition is null)
            {
                outside.Add(Prefix + "he-not-named");
            }
            else if (ScenarioA1ArmorCalculator.ShotDepletion(shot, gun) is null || gunShot.Depleted?.Contains("he", StringComparer.Ordinal) == true)
            {
                outside.Add(Prefix + "he-unavailable" + (gunShot.Depleted?.Contains("he", StringComparer.Ordinal) == true ? string.Empty : ScenarioA1ArmorCalculator.StartMonthDetail(shot, gun)));
            }
        }

        // C9.1, C3.33 (rulings R9.2, R9.3): a mortar always fires on the Area Target Type; other Guns' Area Target Type fire is not built. C13.1:
        // a LATW never uses the Area Target Type. C8.31, C13.24, C11.52, C13.23 (pass 60, rulings R60.4 to R60.6): a PF or PSK fires at Personnel
        // on the Infantry Target Type, an ATR only at a Gun's crew unless it is 20L (at Personnel it is Small Arms). A SW has no CA, and neither
        // Intensive Fires (Case F is NA to SW).
        var mortar = gun.GunType == "mortar";
        var latw = gun.GunType == "latw";
        var pf = gun.LatwType == "pf";
        if (!mortar && equivalency)
        {
            EquivalencyOutside(shot, gun, reference, outside);
        }

        if ((shot.TargetType == OrdnanceTargetTypes.Area) != mortar
            || (latw && shot.VehicleTarget is null && gun.LatwType == "atr" && gun.Caliber != 20 && !AtrAtGun(shot, gun))
            || ((mortar || latw || machineGun) && (shot.HexspinesToTurn != 0 || shot.IntensiveFire == true || shot.BoreSighted == true || shot.NonQualified == true))
            // C9.3 (referee, pass 9): a Spotter is designated in the PFPh or DFPh; Opportunity Fire, which would let it spot in the AFPh, is not built.
            || (shot.Spotter is not null && (!mortar || shot.Phase is not ("PFPh" or "DFPh"))) || (shot.Director is not null && !mortar && !latw))
        {
            outside.Add(Prefix + "target-type-outside");
            return outside;
        }

        // D5.34, C7.42, D5.341 (ruling R7.10): a Stunned, Shocked, or Recalled AFV does not fire; D1.321, D1.322: an RST or 1MT MA fires only BU.
        // D2.42, C5.35 (pass 50): a firer in Motion fires with Case C4 in its own MPh and AFPh; D2.4: never in the PFPh. D2.401 (pass 55, ruling
        // R55.14): the DEFENDER's vehicle in Motion "may still use Motion Fire (Case C4)", as Defensive First Fire and in the DFPh.
        // C5.3 (pass 50): one that entered a new hex in its MPh fires in the AFPh with Case C.
        if (shot.Vehicle is { } firing && (firing.Stunned == true || firing.Shocked == true || firing.Recalled == true
            || (firing.InMotion == true && !bounding && !(phase == "AFPh" && shot.FiringSide == "phasing") && !DefenderMotionFirer(shot))
            || (gun.MaType is "rst" or "1mt" && firing.ButtonedUp != true)))
        {
            outside.Add(Prefix + "vehicle-fire-outside");
        }

        if (gunShot.Malfunctioned == true)
        {
            outside.Add(Prefix + "gun-malfunctioned");
        }

        // A21.13, C2.1: its own nationality's Good Order crew mans it; C5.8's non-qualified use is not reviewed, nor a concealed crew.
        var crew = shot.Crew!;
        // C5.8, A21.13 (ruling R8.8): a squad or HS of the Gun's nationality mans it as non-qualified Infantry, with Case H.
        // C9.2, C13.31 (rulings R9.2, R9.7): any Personnel unit of its nationality fires a light mortar or a PF; a berserk unit may fire a PF. The SW
        // Chart (pass 9b): a lone SMC does not fire a PSK at full effect. C13.45, C13.48, A15.23 (p. 184; pass 72, ruling R72.8): "A BAZ can be used by one SMC
        // only if that SMC is a Hero", with the +1 of HeroDrm; a lone leader stays refused (ruling R9.11), and two SMC together are pass 110's.
        if ((shot.Vehicle is null ? reference.Fire.Definitions.GetValueOrDefault(crew.DefinitionId!) is not { } crewDefinition
                || crewDefinition.Nationality != gun.Nationality
                || (mortar || latw || machineGun ? !Personnel.Contains(crewDefinition.Kind) : shot.NonQualified == true ? !crewDefinition.IsMmc : !ScenarioA1Definitions.IsCrew(crewDefinition.Kind))
                || (gun.LatwType == "psk" && (crewDefinition.Kind == "asl:leader" || (crewDefinition.Kind == "asl:hero" && shot.HeroWeapons != true)))
                : crew.DefinitionId != gun.Id || shot.NonQualified == true)
            || crew.Broken == true || (crew.Berserk == true && !pf))
        {
            outside.Add(Prefix + "crew-outside");
        }

        SupportWeaponOutside(shot, gun, reference, outside);

        // C2.24, C5.2, A7.1: in the PFPh and DFPh the Gun fires again only on a kept Multiple ROF; in the AFPh it fires once, and
        // not after firing earlier in the Player Turn; its crew fires the Gun or its inherent FP, not both.
        // C2.241, C5.6 (ruling R8.2): a Gun that has used its normal ROF (a Prep, First, or Final Fire counter) may Intensive Fire once, never
        // in the AFPh and never a vehicle's; a Gun marked First Fire fires once more only so.
        // C13.31, A7.351 (ruling R9.7): a unit not yet marked as having fired makes a PF Check, a squad twice in a phase, any other once.
        // D3.5 (p. 201; pass 50): a vehicle's fire counter placed by its BMG or CMG alone, in this fire phase, is not the MA's ("it makes no
        // difference which weapon fires first"); a weapon fired in another fire phase forfeits the MA's shot.
        var othersOnly = shot.Vehicle?.Weapons?.MarkedByOthers == true;
        var firedBefore = !othersOnly && gunShot.FiredThisPlayerTurn == true;
        var firstFired = !othersOnly && gunShot.FirstFire == true && !(shot.Vehicle is not null && gunShot.ShotsThisPhase > 0 && gunShot.RateOfFireKept == true);
        var mayFire = pf
            ? gunShot.FiredThisPlayerTurn != true && gunShot.ShotsThisPhase < (reference.Fire.Definitions.GetValueOrDefault(crew.DefinitionId!)?.Kind == "asl:squad" ? 2 : 1)
            : shot.IntensiveFire == true
            ? shot.Vehicle is null && phase != "AFPh" && gunShot.IntensiveFired != true && gunShot.FinalFire != true && crew.Pinned != true
                && (gunShot.FiredThisPlayerTurn == true || gunShot.FirstFire == true) && gunShot.RateOfFireKept != true
            : gunShot.IntensiveFired != true && !firstFired && (phase == "AFPh" && !OpportunityFire(shot)
                ? !firedBefore && gunShot.ShotsThisPhase == 0
                : gunShot.ShotsThisPhase == 0 ? !firedBefore : gunShot.RateOfFireKept == true);
        if (!mayFire || crew.FiredInherentFp == true || shot.Vehicle?.Weapons?.OtherPhase == true
            || (shot.Vehicle?.Weapons?.BoundingFireSpent == true && gunShot.RateOfFireKept != true))
        {
            outside.Add(Prefix + "gun-already-fired");
        }

        // C6.17 (ruling R8.1): no more Defensive First Fire shots at a target in a Location than the MF or MP it spent there, a minimum of one;
        // against a vehicle the MP earlier shots claimed are spent (the C6.17 EX).
        if (shot.Movement is { ShotsHere: { } shots, SpentHere: { } spent } movementFacts
            && (shots >= Math.Max(spent, 1) || (shots > 0 && shot.VehicleTarget is not null && (movementFacts.MpClaimed ?? 0) >= spent)))
        {
            outside.Add(Prefix + "first-fire-limit");
        }

        // C5.5: a shot within the Gun's own hex is not reviewed; C2.25, C3.52: never beyond its range; C2.6: depression and elevation.
        // C5.5 (ruling R8.8): a shot within the Gun's own Location, at Infantry, without changing its CA.
        var sameHex = shot.SameHex == true;
        // C5.51's Defensive First Fire in the Gun's own hex, turning its CA with Case A, is not built (ruling R8.8).
        // D3.2, C5.5 (pass 50): a vehicle in the firer's own Location is fired on with Case E, its Target Facing by the colored dr.
        if ((sameHex ? shot.Range != 0 || shot.HexspinesToTurn != 0 || shot.FireKind is not null || mortar || latw || (shot.Vehicle is not null && shot.VehicleTarget is null)
                : shot.Range < 1)
            || (gun.RangeMaximum is { } maximum && shot.Range > maximum)
            // C9.4: never nearer than a mortar's minimum range; C13.32: a PF's range by the scenario date.
            || (gun.RangeMinimum is { } minimum && shot.Range < minimum) || (pf && shot.Range > PanzerfaustRange(shot.ScenarioYear, shot.Hit!.ScenarioMonth))
            || (gun.ToHitTable.Count > 0 && shot.Range > gun.ToHitTable.Count))
        {
            outside.Add(Prefix + "out-of-range");
        }

        // C2.6 (p. 168): a target at another level at a range less than the elevation difference; a reason of its own (the table player's
        // review of pass 55), so that it is not read as a matter of the Gun's range.
        if (shot.ElevationAllowed != true)
        {
            outside.Add(Prefix + "elevation-outside");
        }

        if (shot.FirerOverstack is < 0 || shot.TargetOverstack is < 0)
        {
            outside.Add(Prefix + "fact-outside");
        }

        if (shot.HexspinesToTurn is < 0 or > 3 || shot.Acquisition is not (0 or -1 or -2))
        {
            outside.Add(Prefix + "fact-outside");
        }

        // A7.81, C5.11: a pinned crew cannot change its Gun's CA; a Gun that has fired from woods or a building fires again that phase
        // only inside its current CA (C5.11).
        // C5.13 (p. 172; pass 50): "a Bounding First Firer must always fire within its current CA".
        if (shot.HexspinesToTurn > 0 && (bounding || crew.Pinned == true || (shot.FirerInWoodsOrBuilding == true && gunShot.ShotsThisPhase > 0)))
        {
            outside.Add(Prefix + "covered-arc-fixed");
        }

        // C8.4 (pass 55, ruling R55.9): a Canister shot has no one target Location and no LOS of its own to it; its own facts decide the rest.
        if (canister)
        {
            ScenarioA1CanisterCalculator.Outside(shot, gun, outside);
            return outside.Distinct(StringComparer.Ordinal).ToList();
        }

        var hit = shot.Hit!;
        if (hit.Los!.Blocked == true)
        {
            outside.Add(Prefix + "los-blocked");
        }

        if (shot.VehicleTarget is not null)
        {
            ScenarioA1ArmorCalculator.Outside(shot, gun, reference, outside);
            // Pass 65 (ruling R65.5): each PRC the target carries names a unit of the catalog, as a Passenger or a Rider.
            if (hit.TargetLocationId != shot.TargetLocationId || hit.Phase != shot.Phase || hit.FiringSide != shot.FiringSide || hit.Firers is { Count: > 0 }
                || hit.Director is not null || hit.FireKind is not null || hit.OrdnanceHit is not null
                || ScenarioA1FireCalculator.PrcMalformed(shot.VehicleTarget.Prc, reference.Fire))
            {
                outside.Add(Prefix + "target-outside");
            }

            if (shot.Rolls is { } vehicleRolls && Malformed(vehicleRolls))
            {
                outside.Add(Prefix + "roll-malformed");
            }

            return outside.Distinct(StringComparer.Ordinal).ToList();
        }

        // C3.32: the Infantry Target Type attacks the in-LOS enemy units of the target Location; one with no unit is not reviewed.
        // C3.33 (ruling R9.3): the Area Target Type would hit friendly units and vehicles too, on their own side's ELR, which is not built.
        var inLocation = hit.Vehicles ?? [];
        if (hit.Targets!.Any(item => item.Dummy != true && reference.Fire.Definitions.GetValueOrDefault(item.DefinitionId ?? string.Empty)?.Nationality == gun.Nationality)
            || (mortar && inLocation.Any(item => reference.Fire.Definitions.GetValueOrDefault(item.DefinitionId ?? string.Empty)?.Nationality == gun.Nationality)))
        {
            outside.Add(Prefix + (mortar ? "area-friendly-units" : "target-outside"));
        }

        // C3.32, C3.41, A7.308 (pp. 169, 170, 56; pass 60, ruling R60.8): the vehicles of the Location, in a fire phase; an AFV is immune, so a
        // Location of AFVs alone is the Vehicle Target Type's. No more unarmored vehicles than the column's highest KIA# may be affected by one DR,
        // and their Random Selection is not built. C3.332, C1.55 (pp. 170, 165; pass 60, ruling R60.10): a mortar's hit attacks every vehicle of
        // its hex, AFVs too, and no more of them than the highest KIA# of its column.
        var unarmored = inLocation.Count(item => Unarmored(item, reference));
        if (inLocation.Count > 0 && (shot.FireKind is not null
            || inLocation.Any(item => (mortar ? !InTargetHex(shot, item.LocationId) : item.LocationId != shot.TargetLocationId)
                || reference.Fire.Definitions.GetValueOrDefault(item.DefinitionId ?? string.Empty) is not { IsVehicle: true })))
        {
            outside.Add(Prefix + "target-outside");
        }
        else if ((mortar ? inLocation.Count : unarmored) is var affected and > 0
            && Array.IndexOf(ScenarioA1FireReference.ColumnFp, mortar ? AreaFirepower(shot, gun, reference) : HeEquivalency(shot, reference) ?? reference.HeFirepower(gun.Caliber)) is var column and >= 0
            && affected > reference.Fire.HighestKia(column))
        {
            outside.Add(Prefix + "vehicles-beyond-kia");
        }

        // B23.32 (pass 60, ruling R60.11): a mortar's Locations of one building hex, the named one among them, each target in one of them.
        if ((shot.AreaLocations is { } levels && (!mortar || !levels.Any(item => item.LocationId == shot.TargetLocationId)))
            || (mortar && hit.Targets!.Any(item => !InTargetHex(shot, item.LocationId))))
        {
            outside.Add(Prefix + "target-outside");
        }

        if ((hit.Targets!.Count == 0 && unarmored == 0 && !(mortar && inLocation.Count > 0) && !Unattended(shot)) || hit.TargetLocationId != shot.TargetLocationId || hit.Phase != shot.Phase || hit.FiringSide != shot.FiringSide
            || hit.Firers is { Count: > 0 } || hit.Director is not null || hit.FireKind is not null || hit.OrdnanceHit is not null)
        {
            outside.Add(Prefix + "target-outside");
        }

        if (shot.Rolls is { } rolls && Malformed(rolls))
        {
            outside.Add(Prefix + "roll-malformed");
        }

        return outside.Distinct(StringComparer.Ordinal).ToList();
    }

    /// <summary>
    /// The reasons a SW's shot is outside (rulings R9.2, R9.4, R9.7): a Spotter must be Good Order Personnel of the firer's side; a directing
    /// leader must be a Good Order leader of that side who has not directed or fired this phase; a PF needs a scenario after September 1943 and
    /// a shot left on the usage track.
    /// </summary>
    private static void SupportWeaponOutside(OrdnanceShot shot, GunDefinition gun, ScenarioA1OrdnanceReference reference, List<string> outside)
    {
        if (shot.Spotter is { } spotter && (reference.Fire.Definitions.GetValueOrDefault(spotter.DefinitionId!) is not { } spotting
            || spotting.Nationality != gun.Nationality || !Personnel.Contains(spotting.Kind) || spotter.Broken == true || spotter.UnitId == shot.Crew!.UnitId))
        {
            outside.Add(Prefix + "spotter-outside");
        }

        if (shot.Director is { } director && (reference.Fire.Definitions.GetValueOrDefault(director.DefinitionId!) is not { IsLeader: true, Leadership: not null } leader
            || leader.Nationality != gun.Nationality || director.Broken == true || director.Pinned == true || director.DirectedThisPlayerTurn == true
            || director.UnitId == shot.Crew!.UnitId))
        {
            outside.Add(Prefix + "director-outside");
        }

        if (gun.LatwType == "pf" && (shot.ScenarioYear is not { } year || shot.Hit!.ScenarioMonth is not { } month || year < 1943 || (year == 1943 && month < 10)))
        {
            outside.Add(Prefix + "panzerfaust-date-outside");
        }

        // C13.48 (pass 9b): the PSK from September 1943.
        if (gun.LatwType == "psk" && (shot.ScenarioYear is not { } pskYear || shot.Hit!.ScenarioMonth is not { } pskMonth || pskYear < 1943 || (pskYear == 1943 && pskMonth < 9)))
        {
            outside.Add(Prefix + "latw-date-outside");
        }

        if (gun.LatwType == "pf" && shot.Panzerfaust is { ShotsTaken: { } taken, ShotsAllowed: { } allowed } && taken >= allowed)
        {
            outside.Add(Prefix + "panzerfaust-exhausted");
        }

        // C13.8 (referee, pass 9): from a ground-level building only an unpinned unit fires a PF or PSK without Desperation, which is not built.
        if (gun.LatwType is "pf" or "psk" && shot.Panzerfaust?.FromBuilding == true && shot.Crew!.Pinned == true)
        {
            outside.Add(Prefix + "panzerfaust-backblast");
        }
    }

    /// <summary>A7.25 (p. 55; pass 35, task 35.10): whether Infantry fires this ordnance by Opportunity Fire in its AFPh.</summary>
    public static bool OpportunityFire(OrdnanceShot shot)
    {
        ArgumentNullException.ThrowIfNull(shot);
        return shot.Phase == "AFPh" && shot.Vehicle is null && shot.Crew?.OpportunityFire == true;
    }

    /// <summary>
    /// C5.2, C5.34, C13.1 (pp. 172, 183; pass 35, task 35.10): whether the shot is AFPh fire with its penalties (Case B or Case C3, one shot, no
    /// Multiple ROF): any shot in the AFPh but an Opportunity Firer's.
    /// </summary>
    public static bool AfphFire(OrdnanceShot shot)
    {
        ArgumentNullException.ThrowIfNull(shot);
        return shot.Phase == "AFPh" && !OpportunityFire(shot);
    }

    /// <summary>
    /// The To Hit DRM of Defensive First Fire for how the target moves: C6.6 (p. 175; pass 35, task 35.10), Case O, -2 against Hazardous Movement,
    /// never with Case J's subcases; otherwise C6.13 and C6.14 (ruling R8.1), FFNAM and FFMO.
    /// </summary>
    public static IEnumerable<FireModifier> MoverDrm(OrdnanceMovement movement)
    {
        ArgumentNullException.ThrowIfNull(movement);
        if (movement.Hazardous == true)
        {
            yield return new FireModifier("case-o", -2, "C6.6");
            yield break;
        }

        // A4.63, C6.1 (pp. 52, 173; pass 40): ordnance "must use the Case J To Hit DRM (Cases J3 and J4 do not apply in the road Location)".
        if (movement.Dashing == true)
        {
            yield return new FireModifier("case-j", 2, "C6.1");
            yield break;
        }

        if (movement.NonAssault == true)
        {
            yield return new FireModifier("case-j3", -1, "C6.13");
        }

        if (movement.OpenGround == true)
        {
            yield return new FireModifier("case-j4", -1, "C6.14");
        }
    }

    /// <summary>
    /// B10.31 (p. 125; pass 55): whether the Height Advantage the shot carries is its target's TEM: no other Case Q of the To Hit DRM so far is a
    /// positive TEM. A shot recorded before the pass carries none.
    /// </summary>
    internal static bool HeightAdvantageApplies(OrdnanceShot shot, IEnumerable<FireModifier> drm) =>
        shot.HeightAdvantage == true && shot.SameHex != true && !drm.Any(item => item.Value > 0 && item.Name.StartsWith("case-q:", StringComparison.Ordinal));

    /// <summary>A PF's range (C13.32): one hex before June 1944, two from June to December 1944, three from 1945.</summary>
    public static int PanzerfaustRange(int? year, int? month) => year >= 1945 ? 3 : year == 1944 && month >= 6 ? 2 : 1;

    private static bool Malformed(OrdnanceRolls rolls)
    {
        static bool Dice(IReadOnlyList<int>? dice) => dice is not null && (dice.Count != 2 || dice.Any(die => die is < 1 or > 6));
        return Dice(rolls.ToHit) || rolls.Subsequent is < 1 or > 6 || rolls.CriticalSelection?.Values.Any(dr => dr is < 1 or > 6) == true
            || Dice(rolls.ToKill) || Dice(rolls.ShockCheck) || Dice(rolls.CrewCheck) || Dice(rolls.CrewSurvival) || Dice(rolls.CollateralCheck) || rolls.PanzerfaustCheck is < 1 or > 6
            || rolls.AreaSurvival?.Values.Any(Dice) == true || rolls.AreaCrewCheck?.Values.Any(Dice) == true;
    }

    private static List<string> Undecided(OrdnanceShot shot)
    {
        var undecided = new List<string>();
        var hit = shot.Hit!;

        // C8.4 (pass 55): each Location a Canister shot attacks is decided in its own fire attack; the shot itself only needs the read of who
        // sees a concealed crew fire (A12.14).
        if (IsCanister(shot))
        {
            if (shot.Crew!.Concealed == true && shot.Vehicle is null && shot.CrewSeen is null)
            {
                undecided.Add(Prefix + "concealment-unreviewed:crew");
            }

            return undecided;
        }

        // An unattributed Hindrance is not reviewed, as for Infantry fire. C2.6 (pass 55): a target at another level is fired at when the range
        // allows it, which the shot's elevation fact says.
        var los = hit.Los!;
        if (los.HindranceAttributed != true || los.HindranceDrm < 0 || (los.GrainInLos == true && hit.ScenarioMonth is null))
        {
            undecided.Add(Prefix + "hindrance-unattributed");
        }

        // C6.2, C3.4 (pass 60, ruling R60.9): Case K applies to each concealed target alone, so a Location mixing concealed and Known units is
        // judged unit by unit (Run).

        // A12.14: a concealed crew that fires its Gun loses "?" in the LOS of a Good Order enemy ground unit within 16 hexes; the package
        // sees only the target Location, so it decides only when one of its units is Good Order within that range, as for Infantry fire.
        if (shot.Crew!.Concealed == true && shot.Vehicle is null && shot.CrewSeen is null && !CrewRevealed(shot))
        {
            undecided.Add(Prefix + "concealment-unreviewed:crew");
        }

        return undecided;
    }

    private static bool Concealed(FireTarget target) => target.Concealed == true || target.Hidden == true || target.Dummy == true;

    private static bool CrewRevealed(OrdnanceShot shot) => shot.Range <= 16 && shot.Hit!.Targets!.Any(item => item.Broken == false && item.Dummy != true);

    /// <summary>
    /// The IFT attack of a hit: the Gun's HE FP column (C.6) or its round's HE Equivalency (C8.31), doubled by a Critical Hit (C3.71), on the given
    /// targets; the Location's other units, which the other attack of a split hit attacks, are its companions for a berserk leader's NTC (A15.41).
    /// The vehicles it hit are attacked on the same DR (C3.32, A7.308; pass 60, ruling R60.8).
    /// </summary>
    internal static FireAttack HitAttack(OrdnanceShot shot, GunDefinition gun, ScenarioA1OrdnanceReference reference, bool critical,
        IReadOnlyList<FireTarget> targets, FireRolls? rolls, IReadOnlyList<FireVehicle>? vehicles = null, bool heightAdvantage = false)
    {
        // C3.33, C9.5 (ruling R9.3): an Area Target Type hit attacks at half the HE FP (the IFT column at or below it), a Critical Hit at the
        // full HE FP doubled.
        // C8.31 (pass 60, ruling R60.4): AP, APCR, APDS, HEAT, and a LATW's round at their HE Equivalency, on the same column.
        var area = shot.TargetType == OrdnanceTargetTypes.Area;
        var firepower = area && !critical ? AreaFirepower(shot, gun, reference) : HeEquivalency(shot, reference) ?? reference.HeFirepower(gun.Caliber);
        var others = shot.Hit!.Targets!.Where(item => !targets.Contains(item)).ToArray();

        // Ruling R5.8: each attack of a split hit answers the options of its own targets, a vehicle's Unlikely Kill among them.
        var ids = targets.Select(item => item.UnitId).Concat((vehicles ?? []).Select(item => item.VehicleId)).ToHashSet(StringComparer.Ordinal);
        return shot.Hit with
        {
            Choices = shot.Hit.Choices is { } choices
                ? choices.Where(item => item.Key.Split(':') is [_, var unit, ..] && ids.Contains(unit)).ToDictionary(item => item.Key, item => item.Value, StringComparer.Ordinal)
                : null,
            Targets = targets,
            Vehicles = vehicles is { Count: > 0 } ? vehicles : null,
            Firers = [],
            Director = null,
            Companions = others.Length > 0 ? [.. others, .. shot.Hit.Companions ?? []] : shot.Hit.Companions,
            OrdnanceHit = new FireOrdnanceHit(shot.Gun!.GunId, firepower, critical)
            {
                Area = area ? true : null,

                // C3.71 (pass 60, rulings R60.3, R60.14): a Critical Hit's reversal of Height Advantage, when the record carries the rule.
                HeightAdvantage = critical && heightAdvantage && shot.ReversesHeightAdvantage == true ? true : null,
            },
            Rolls = rolls ?? new FireRolls(null, null, null, null),
        };
    }

    private static OrdnanceResolution Run(OrdnanceShot shot, ScenarioA1OrdnanceReference reference)
    {
        var gun = reference.Guns[shot.Gun!.DefinitionId!];
        var rolls = shot.Rolls!;
        var hit = shot.Hit!;
        var range = shot.Range!.Value;
        var color = CapturedColor(shot, ScenarioA1OrdnanceReference.Color(gun, shot.ScenarioYear));

        // C3.3, C4: the Basic TH# of the Infantry Target Type by range and color, and the Gun and Ammo modifications. A LATW (pass 60, rulings
        // R60.5, R60.6) takes the To Hit it takes at a vehicle: a PF 10 less two a hex (C13.33), a PSK its own table (C13.42, C13.48), an ATR the
        // black row (C13.2).
        var latw = gun.GunType == "latw";
        if (gun.LatwType == "atr")
        {
            color = "black";
        }

        var basic = gun.LatwType == "pf" ? 10 : gun.ToHitTable.Count > 0 ? gun.ToHitTable[range - 1] : reference.BasicToHit(color, range);
        IReadOnlyList<FireModifier> modifications = gun.LatwType == "pf" ? [new FireModifier("pf-range", -2 * range, "C13.33")]
            : gun.ToHitTable.Count > 0 ? [] : reference.Modifications(gun, range);
        var modified = basic + (int)modifications.Sum(item => item.Value);

        var drm = new List<FireModifier>();
        var woods = shot.FirerInWoodsOrBuilding == true;
        if (shot.HexspinesToTurn is int turned && turned > 0)
        {
            // C5.1, C5.11: a non-turreted Gun adds +3 for the first hexspine and +1 for each other, a turret less (ruling R7.10), doubled in
            // woods or a building.
            var caseA = ScenarioA1ArmorCalculator.CaseA(gun.MaType, turned) * (woods ? 2 : 1);
            drm.Add(new FireModifier("case-a:" + turned.ToString(System.Globalization.CultureInfo.InvariantCulture), caseA, "C5.1"));
        }
        else if (shot.Vehicle?.Weapons?.MaintainedCaseA is > 0 and var maintained && shot.SameHex != true)
        {
            // D3.51 (p. 201; pass 50): the MA fired within a TCA its CMG changed in this fire phase pays what the CMG paid.
            drm.Add(new FireModifier("case-a:maintained", maintained, "D3.51"));
        }

        if (latw)
        {
            // C13.1, C13.8 (ruling R9.8): a LATW's Case C3 instead of Case B, for the AFPh and for the Backblast of a PF or PSK from a ground-level
            // building or rubble; C5.34: neither for an Opportunity Firer. A7.531, C13.35: a directing leader.
            if (AfphFire(shot))
            {
                drm.Add(new FireModifier("case-c3:afph", 2, "C13.1"));
            }

            if (shot.Panzerfaust?.FromBuilding == true && gun.LatwType is "pf" or "psk" && !OpportunityFire(shot))
            {
                drm.Add(new FireModifier("case-c3:backblast", 2, "C13.8"));
            }

            drm.AddRange(Leadership(shot, reference));
            drm.AddRange(HeroDrm(shot, gun, reference));
        }
        else if (CaseC(shot, gun) is { } caseC)
        {
            drm.Add(caseC);
        }
        else if (AfphFire(shot))
        {
            drm.Add(new FireModifier("case-b", woods ? 3 : 2, "C5.2"));
        }

        if (shot.Crew!.Pinned == true)
        {
            drm.Add(new FireModifier("case-d", 2, "C5.4"));
        }

        // A4.51 (ruling R5.2): a CX crew adds one to its Gun's To Hit DR.
        if (shot.Crew.Cx == true)
        {
            drm.Add(new FireModifier("cx", 1, "A4.51"));
        }

        // C5.9 (ruling R7.10): a BU AFV firing its MA.
        if (shot.Vehicle?.ButtonedUp == true)
        {
            drm.Add(new FireModifier("case-i", 1, "C5.9"));
        }

        // D3.44, D3.45 (p. 200; pass 55, rulings R55.16, R55.18): the vehicle's Armor Leader modifies its MA's To Hit DR, BU or not; an
        // Inexperienced Crew adds one.
        if (ScenarioA1ArmorLeaders.Modifier(shot.Vehicle?.ArmorLeadership) is { } armorLeader)
        {
            drm.Add(armorLeader);
        }

        drm.AddRange(PassEightFirerDrm(shot));
        var sameHex = shot.SameHex == true;
        if (sameHex)
        {
            // C5.5: Case E, doubled in woods or a building; Cases J3, J4, L, and M do not apply.
            drm.Add(new FireModifier("case-e", woods ? 4 : 2, "C5.5"));
        }
        else if (shot.FireKind is not null && shot.Movement is { } movement)
        {
            drm.AddRange(MoverDrm(movement));
        }
        else if (shot.HazardousTarget == true)
        {
            // C6.6, A4.62 (pp. 175, 51; pass 40): Case O in any fire phase.
            drm.Add(new FireModifier("case-o", -2, "C6.6"));
        }

        // C13.23 (pass 60, ruling R60.6): an ATR that is not 20L attacks only the Gun and its manning crew.
        var targetsHit = HitTargets(shot, gun);

        // C3.32, C3.4, A12.13 (pp. 169, 170, 77; pass 60, rulings R60.8, R60.9): a Location holding a vehicle the hit can affect, or concealed and
        // Known units together, is hit target by target, each on its own Final DR: Cases K, N, P, and Q are each target's own.
        var vehicles = HitVehicles(shot, reference);
        var mixed = vehicles.Count > 0 || (targetsHit.Any(Concealed) && !targetsHit.All(Concealed));
        // C11.4 (pass 60, ruling R60.18): an unattended Gun is a Known target.
        var concealedTarget = targetsHit.All(Concealed) && vehicles.All(item => item.Concealed == true) && !Unattended(shot);
        if (concealedTarget && !mixed)
        {
            drm.Add(new FireModifier("case-k", 2, "C6.2"));
        }

        // C6.3 (p. 174; review of pass 50): "Case L is not applicable if ... the firer is in-Motion/Non-Stopped". Cases M and N are not LATW DRM;
        // Case L is the ATR's, which reads no To Hit Table of its own (pass 35, task 35.10).
        if (range is >= 1 and <= 2 && !sameHex && shot.Vehicle?.InMotion != true && (!latw || gun.LatwType == "atr"))
        {
            drm.Add(new FireModifier("case-l", range == 1 ? -2 : -1, "C6.3"));
        }

        // C6.4 (ruling R8.8): the Bore Sighted Location's -2, the firer choosing it or the Acquisition, never in its own hex.
        if (shot.BoreSighted == true && !sameHex)
        {
            drm.Add(new FireModifier("case-m", -2, "C6.4"));
        }
        else if (!latw && shot.Acquisition is int acquired && acquired < 0 && !concealedTarget && !AcquisitionDenied(shot))
        {
            // C6.51, C6.57: the Acquisition applies only to Known units, and a shot at a concealed target loses it.
            drm.Add(new FireModifier("case-n", acquired, "C6.5"));
        }

        var tem = ScenarioA1FireReference.Tem.GetValueOrDefault(hit.TargetTerrain!);
        var unattended = Unattended(shot);
        var gunTarget = unattended || ScenarioA1FireCalculator.GunCrewAlone(hit with
        {
            Targets = targetsHit
        }) ? hit.GunTarget : null;
        if (gunTarget is not null && reference.Guns.GetValueOrDefault(gunTarget.DefinitionId ?? string.Empty)?.TargetSize is { } size && size != "average")
        {
            // C11.2, C2.271 (ruling R8.3): the Gun's Target Size.
            drm.Add(new FireModifier("case-p:" + size, size == "small" ? 1 : -1, "C11.2"));
        }

        if (gunTarget?.Emplaced == true && tem < 2)
        {
            // C11.2: the Emplacement TEM instead of a lower positive TEM.
            drm.Add(new FireModifier("case-q:emplacement", 2, "C11.2"));
        }
        else if (tem != 0)
        {
            drm.Add(new FireModifier("case-q:" + hit.TargetTerrain, tem, "C6.8"));
        }
        else if (shot.CushionedOpenGround == true && hit.TargetTerrain == "open-ground")
        {
            // E3.62, E3.731 (rulings R16.12, R16.13): Mud or Deep Snow cushions HE at an Infantry Target Type in Open Ground: +1 TEM on the TH DR.
            drm.Add(new FireModifier("case-q:weather-cushion", 1, "E3.62"));
        }

        // D9.3, D10.3 (ruling R6.1): the +1 TEM of a wreck, a friendly AFV, or an abandoned enemy AFV, where the terrain gives none.
        if (ScenarioA1FireCalculator.Cover(hit) is { } cover)
        {
            drm.Add(new FireModifier("case-q:afv-cover:" + cover, 1, "D9.3"));
        }

        // B10.31, C6.8 (pp. 125, 175; pass 55): "Any unit in a hex receiving Direct Fire from a lower elevation is entitled to a +1 TEM, provided
        // that unit is not eligible to receive any other positive TEM", and is then "not subject to Interdiction/FFMO by an attack to which that
        // +1 TEM applies": no Case J4.
        if (HeightAdvantageApplies(shot, drm))
        {
            drm.Add(new FireModifier("case-q:height-advantage", 1, "B10.31"));
            drm.RemoveAll(item => item.Name == "case-j4");
        }

        // C3.71 (pass 60, ruling R60.14): a Critical Hit reverses the Height Advantage claimed here.
        var heightClaimed = drm.Any(item => item.Name == "case-q:height-advantage");

        // E1.7, E3.1 (referee, pass 16): the Low Visibility DRM is a Hindrance of its own on the TH DR.
        if (shot.LowVisibilityDrm is > 0 and var lowVisibility)
        {
            drm.Add(new FireModifier("case-r:lv", lowVisibility, "E1.7"));
        }

        if (hit.Los!.HindranceDrm is int hindrance && hindrance > 0)
        {
            drm.Add(new FireModifier("case-r", hindrance, "C6.9"));
        }

        // Each target's own DRM of a Location hit target by target: a unit's Case K, or its Case N, with the Location's Case P and Q; a vehicle's
        // its own (OwnVehicleDrm). The shot's DRM are the rest.
        var own = new List<(string Id, IReadOnlyList<FireModifier> Drm, bool Pool)>();
        if (mixed)
        {
            var caseN = drm.Where(item => item.Name == "case-n").ToArray();
            var location = drm.Where(item => item.Name.StartsWith("case-p:", StringComparison.Ordinal) || item.Name.StartsWith("case-q:", StringComparison.Ordinal)).ToArray();
            drm.RemoveAll(item => item.Name == "case-n" || item.Name.StartsWith("case-p:", StringComparison.Ordinal) || item.Name.StartsWith("case-q:", StringComparison.Ordinal));
            FireModifier[] caseK = [new FireModifier("case-k", 2, "C6.2")];
            own.AddRange(targetsHit.Select(unit => (unit.UnitId!, (IReadOnlyList<FireModifier>)[.. Concealed(unit) ? caseK : caseN, .. location], true)));
            own.AddRange(vehicles.Select(vehicle => (vehicle.VehicleId!, (IReadOnlyList<FireModifier>)OwnVehicleDrm(shot, vehicle, caseN, reference), Unarmored(vehicle, reference))));
        }

        if (rolls.ToHit is not { } dice)
        {
            return Refused(OrdnanceResolution.Indeterminate, [Prefix + "roll-missing:toHit"]);
        }

        var total = (int)drm.Sum(item => item.Value);
        var original = dice[0] + dice[1];
        // C5.35 (p. 172; pass 50): a Motion firer with no Gyrostabilizer doubles the lower dr of its TH DR; the lowest Final DR is then one more.
        var motion = MotionFirer(shot) ? Math.Min(dice[0], dice[1]) : 0;
        if (motion > 0)
        {
            drm.Add(new FireModifier("case-c4:lower-dr", motion, "C5.35"));
        }

        var final = original + total + motion;
        var lowest = 2 + total + (motion > 0 ? 1 : 0);

        // C3.7 (p. 170; pass 60, the first referee): on a LATW's own To Hit Table (a PF's, a PSK's) an Original 2 is a Critical Hit outright, unless only
        // the lowest Final DR possible hits, when it takes a subsequent dr of 1.
        var latwTable = gun.LatwType == "pf" || gun.ToHitTable.Count > 0;

        // C3.6: when no Final DR can hit, an Original 2 still may, on a subsequent dr of 1 (a Critical Hit) to 3.
        var improbable = lowest > modified;
        bool isHit, critical;
        int? subsequent = null;
        var judged = new List<(string Id, IReadOnlyList<FireModifier> Drm, int Final, bool Hit, bool Critical, bool Pool)>();
        if (mixed)
        {
            // C3.6, C3.7 as below, target by target; one subsequent dr serves them all.
            var sums = own.Select(item => (int)item.Drm.Sum(modifier => modifier.Value)).ToArray();
            if (original == 2 && sums.Any(sum => lowest + sum > modified
                || (final + sum <= modified && (latwTable ? lowest + sum == modified : (final + sum) * 2 >= modified))))
            {
                if (rolls.Subsequent is not { } dr)
                {
                    return Refused(OrdnanceResolution.Indeterminate, [Prefix + "roll-missing:subsequent"]);
                }

                subsequent = dr;
            }

            foreach (var ((id, ownDrm, pool), sum) in own.Zip(sums))
            {
                var targetFinal = final + sum;
                var hitOne = lowest + sum > modified ? subsequent is >= 1 and <= 3 : targetFinal <= modified;
                var criticalOne = lowest + sum > modified ? subsequent == 1
                    : hitOne && (targetFinal * 2 < modified || (original == 2 && (latwTable ? lowest + sum < modified || subsequent == 1
                        : subsequent == 1 || subsequent * 2 <= modified)));
                judged.Add((id, ownDrm, targetFinal, hitOne, criticalOne, pool));
            }

            improbable = sums.All(sum => lowest + sum > modified);
            isHit = judged.Any(item => item.Hit);
            critical = judged.Any(item => item.Critical && item.Pool);
        }
        else if (improbable)
        {
            if (original == 2)
            {
                if (rolls.Subsequent is not { } dr)
                {
                    return Refused(OrdnanceResolution.Indeterminate, [Prefix + "roll-missing:subsequent"]);
                }

                subsequent = dr;
            }

            isHit = subsequent is >= 1 and <= 3;
            critical = subsequent == 1;
        }
        else
        {
            isHit = final <= modified;

            // C3.7: on the Infantry Target Type a Final DR below half the Modified TH# is a Critical Hit, and so is an Original 2 that hits
            // followed by a subsequent dr of 1 or at most half the Modified TH# (the bracketed exception for the lowest Final DR belongs to
            // the Area and Vehicle Target Types; ruling R24.7).
            critical = isHit && final * 2 < modified;
            if (isHit && !critical && original == 2 && latwTable && lowest < modified)
            {
                critical = true;
            }
            else if (isHit && !critical && original == 2)
            {
                if (rolls.Subsequent is not { } dr)
                {
                    return Refused(OrdnanceResolution.Indeterminate, [Prefix + "roll-missing:subsequent"]);
                }

                subsequent = dr;
                critical = latwTable ? dr == 1 : dr == 1 || dr * 2 <= modified;
            }
        }

        var toHit = new OrdnanceToHit(color, basic, modifications, modified, [.. dice], original, drm, final, improbable, subsequent, isHit, critical);

        // C2.28: an Original To Hit DR at or above the B# malfunctions the Gun; C5.62: two lower while Intensive Firing.
        var malfunctioned = original >= Breakdown(shot, gun, reference);

        // C13.36 (ruling R9.8; pass 60): a PF never malfunctions, but an Original 12 misses and gives its firer Casualty Reduction; an 11 or 12 for
        // Inexperienced Infantry (A19.32).
        var casualty = gun.LatwType == "pf"
            && original >= (reference.Fire.Definitions.GetValueOrDefault(shot.Crew.DefinitionId ?? string.Empty)?.Class is "green" or "conscript" ? 11 : 12);
        if (casualty)
        {
            isHit = critical = false;
            toHit = toHit with
            {
                Hit = false,
                CriticalHit = false
            };
        }

        // C8.9, C8.8 (p. 179; pass 55, ruling R55.7): HE with a Depletion Number is used below it, used and run out at it, and above it "the firer
        // had no such ammunition and is considered not to have fired yet for any purpose unless Gun Malfunction ... occurs": no ROF check, no hit,
        // and no IFT DR for a round that was not there. A shot that names no ammunition, as every one before the pass, has no such number.
        string? use = null;
        if (ScenarioA1ArmorCalculator.ShotDepletion(shot, gun) is { } depletion)
        {
            use = ScenarioA1ArmorCalculator.Use(original, depletion);
            if (use == "none")
            {
                if (!malfunctioned)
                {
                    return ScenarioA1ArmorCalculator.Unused(shot, gun, toHit);
                }

                isHit = critical = false;
                toHit = toHit with
                {
                    Hit = false,
                    CriticalHit = false
                };
            }
        }

        // C2.24: an Original colored dr at most the ROF keeps the Multiple ROF; C2.5: a non-vehicular NT Gun's ROF is one lower after a
        // CA change; C5.4: a pinned crew forfeits it; C5.2: none in the AFPh. A21.12 (pass 65, ruling R65.22): a Temporary crew's captured-weapon penalty
        // lowers it one each.
        var rof = (gun.RateOfFire ?? 0) - (shot.CapturedPenalty ?? 0);
        if (shot.HexspinesToTurn > 0 && shot.Vehicle is null)
        {
            rof--;
        }

        // C13.1 (pass 60): a LATW has no Multiple ROF.
        if (shot.Crew.Pinned == true || AfphFire(shot) || latw)
        {
            rof = 0;
        }

        var (kept, counter) = Counter(shot, malfunctioned, rof, dice[0]);

        FireResolution? normal = null, criticalHit = null;
        string? criticalTarget = null;
        var criticalOnCrew = false;
        if (isHit)
        {
            // C3.32 (pass 60, ruling R60.8): a Location hit target by target attacks the units and vehicles the shot hit.
            var targets = mixed ? [.. targetsHit.Where(item => judged.Any(one => one.Id == item.UnitId && one.Hit))] : targetsHit;
            FireVehicle[] struck = [.. vehicles.Where(item => judged.Any(one => one.Id == item.VehicleId && one.Hit))];
            if (gun.LatwType == "pf" && targets.Count > 1)
            {
                // C13.31 (p. 183; pass 60, ruling R60.5): a PF "may not directly affect more than one unit ... unless the Random Selection DR
                // indicates several units are affected": the units the Random Selection dr picks (the highest, every tied unit alike; A.9), by the
                // same dr that picks a Critical Hit's unit (C3.74). The firer's choice of a Known unit manning a Gun or SW is left out (backlog).
                var key = "criticalSelection:" + string.Join(",", targets.Select(item => item.UnitId));
                if (rolls.CriticalSelection is not { } picks || targets.Any(item => !picks.ContainsKey(item.UnitId!)))
                {
                    return Refused(OrdnanceResolution.Indeterminate, [Prefix + "roll-missing:" + key]);
                }

                var top = targets.Max(item => picks[item.UnitId!]);
                targets = [.. targets.Where(item => picks[item.UnitId!] == top)];
            }

            // C3.74 (p. 171; pass 60, ruling R60.8): the Critical Hit falls among the units and unarmored vehicles it was obtained against; an AFV is
            // immune to it, and its crew is attacked normally.
            IReadOnlyList<string> pool = !critical ? [] : mixed ? [.. judged.Where(item => item.Critical && item.Pool).Select(item => item.Id)]
                : [.. targets.Select(item => item.UnitId!)];
            IReadOnlyList<string> picked = pool;
            if (pool.Count > 1)
            {
                // C3.74: a Critical Hit applies to the target Random Selection picks (the highest dr, every tied unit alike; A.9).
                var key = "criticalSelection:" + string.Join(",", pool);
                if (rolls.CriticalSelection is not { } drs || pool.Any(item => !drs.ContainsKey(item)))
                {
                    return Refused(OrdnanceResolution.Indeterminate, [Prefix + "roll-missing:" + key]);
                }

                var highest = pool.Max(item => drs[item]);
                picked = [.. pool.Where(item => drs[item] == highest)];
                criticalTarget = string.Join(",", picked);
            }

            FireTarget[] criticalTargets = [.. targets.Where(item => picked.Contains(item.UnitId!))];
            criticalOnCrew = gunTarget?.CrewUnitId is { } manning && picked.Contains(manning);
            FireVehicle[] criticalVehicles = [.. struck.Where(item => picked.Contains(item.VehicleId!))];
            if (picked.Count > 0)
            {
                criticalHit = ScenarioA1FireCalculator.Resolve(HitAttack(shot, gun, reference, true, criticalTargets, rolls.CriticalHit, criticalVehicles, heightClaimed), reference.Fire);
                if (Pending(criticalHit, "critical-hit:") is { } pending)
                {
                    return pending;
                }
            }

            // C3.32: every unit hit is attacked with a single Effects DR, so the normal hit reuses the Critical Hit's (C3.74).
            var others = targets.Where(item => !criticalTargets.Contains(item)).ToArray();
            var otherVehicles = struck.Where(item => !criticalVehicles.Contains(item)).ToArray();

            // C3.41, C11.4 (p. 182; pass 60, ruling R60.18): an unattended Gun alone takes the IFT DR for its own fate; a Critical Hit destroys it.
            if (others.Length > 0 || otherVehicles.Length > 0 || (unattended && !critical))
            {
                var hitRolls = criticalHit?.Arithmetic?.Dice is { } shared ? (rolls.Hit ?? new FireRolls(null, null, null, null)) with
                {
                    Attack = shared
                } : rolls.Hit;
                normal = ScenarioA1FireCalculator.Resolve(HitAttack(shot, gun, reference, false, others, hitRolls, otherVehicles), reference.Fire);
                if (Pending(normal, "hit:") is { } pending)
                {
                    return pending;
                }
            }
        }

        // C6.5, C6.57: the shot acquires the target Location, one step more (to -2) if already acquired, unless the Gun malfunctions; a
        // concealed target is acquired only when the shot costs it its concealment.
        var effects = (normal?.Effects ?? []).Concat(criticalHit?.Effects ?? []).ToArray();
        // C6.5 (the chart's G): a non-mortar SW acquires nothing.
        var acquires = !malfunctioned && !latw && (!concealedTarget || effects.Any(item => item.ConcealmentLost));
        var acquisition = !acquires ? 0 : concealedTarget ? -1 : Math.Max((AcquisitionDenied(shot) ? 0 : shot.Acquisition!.Value) - 1, -2);
        var gunEffect = new OrdnanceGunEffect(Breakdown(shot, gun, reference), malfunctioned, kept ? Math.Max(rof, 0) : shot.IntensiveFire == true ? 0 : Math.Max(rof, 0), kept, counter,
            acquisition, acquires ? shot.TargetLocationId : null);

        // C11.4, C11.6 (ruling R8.3): a Critical Hit destroys the Gun; a KIA before the gunshield destroys it, a K malfunctions it. C11.52, C13.23,
        // the table's note 5 (p. 182; pass 60, ruling R60.6): "The Gun and any manning Infantry are destroyed by a CH/any K/KIA result" of AP,
        // APCR, APDS, or an ATR; HEAT destroys it "using the same mechanics as an HE hit" (C11.51).
        // C3.74 (pass 60, the first referee): a Critical Hit destroys the Gun only when its Random Selection fell on the manning crew; one that fell on
        // a vehicle of the Location leaves the Gun to the crew's normal hit.
        string? fate = null;
        if (gunTarget is not null && isHit)
        {
            var result = (normal ?? criticalHit)?.Arithmetic?.Result ?? string.Empty;
            var armorPiercing = shot.VehicleTarget is null && (shot.Ammunition is "ap" or "apcr" or "apds" || gun.LatwType == "atr");
            var criticalOnGun = criticalHit is not null && (gunTarget.CrewUnitId is null || criticalOnCrew);
            fate = (normal?.Arithmetic?.Drm.Any(item => item.Name.StartsWith("gunshield:", StringComparison.Ordinal)) ?? false) && !criticalOnGun ? null
                : ScenarioA1GunDestruction.Fate(ScenarioA1GunDestruction.Ordnance, result, criticalOnGun || (unattended && critical), armorPiercing);
        }

        return new OrdnanceResolution(OrdnanceResolution.Resolved, [], toHit, gunEffect)
        {
            // A12.14 (ruling R8.5): the "?" is lost only in the view of a Good Order enemy ground unit within 16 hexes.
            CrewConcealmentLost = shot.Crew.Concealed == true && shot.CrewSeen != false ? true : null,
            Hit = normal,
            CriticalHit = criticalHit,
            CriticalTarget = criticalTarget,
            GunTargetFate = fate,
            AmmunitionUse = use,
            HeEquivalency = HeEquivalency(shot, reference),
            // C3.6 (pass 60, boundary review): the Original DR each target needs, for the proposal (a Motion firer's lower dr unknown before the roll).
            TargetHits = mixed ? [.. judged.Select(item => new OrdnanceAreaTarget(item.Id, item.Drm, item.Final, item.Hit && isHit)
            {
                NeededOriginal = modified - total - (item.Final - final),
            })] : null,
            FirerEffect = casualty ? OrdnancePanzerfaustCheck.CasualtyReduction : null,
        };
    }

    /// <summary>
    /// Whether the shot attacks by HE Equivalency (C8.31, p. 178; pass 60, ruling R60.4): AP, APCR, APDS, or HEAT named on the Infantry Target Type
    /// at a Location with no vehicle named. A LATW names its own round (a PSK's and a PF's HEAT, an ATR's AP).
    /// </summary>
    private static bool Equivalency(OrdnanceShot shot) =>
        shot.VehicleTarget is null && shot.TargetType is null && shot.Ammunition is "ap" or "apcr" or "apds" or "heat";

    /// <summary>
    /// The HE Equivalency FP a shot on the Infantry Target Type attacks with (C8.31, p. 178; pass 60, rulings R60.4 to R60.6), or null for HE: the
    /// table's value for its round and weapon, the HEAT of a Gun one IFT column left of its HE FP. Null also for a Gun's HEAT with no HE FP.
    /// </summary>
    public static int? HeEquivalency(OrdnanceShot shot, ScenarioA1OrdnanceReference reference)
    {
        ArgumentNullException.ThrowIfNull(shot);
        ArgumentNullException.ThrowIfNull(reference);
        return Equivalency(shot) && reference.Guns.TryGetValue(shot.Gun?.DefinitionId ?? string.Empty, out var gun)
            ? ScenarioA1HeEquivalency.Firepower(shot.Ammunition, ScenarioA1HeEquivalency.Weapon(gun), gun.Caliber, HeFirepowerOf(gun, reference))
            : null;
    }

    /// <summary>A Gun's normal HE FP (C.6), or null when it has no HE: its listing denies it and gives no HE Depletion Number (C2.21, C8.8).</summary>
    internal static int? HeFirepowerOf(GunDefinition gun, ScenarioA1OrdnanceReference reference) =>
        gun.NoHe && !ScenarioA1ArmorCalculator.Lists(gun, "he") ? null : reference.HeFirepower(gun.Caliber) is var fp and > 0 ? fp : null;

    /// <summary>The FP of an Area Target Type hit (C3.33, C9.5; ruling R9.3): half the HE FP, on the IFT column at or below it.</summary>
    internal static int AreaFirepower(OrdnanceShot shot, GunDefinition gun, ScenarioA1OrdnanceReference reference)
    {
        var firepower = HeEquivalency(shot, reference) ?? reference.HeFirepower(gun.Caliber);
        return ScenarioA1FireReference.ColumnFp.Where(column => column * 2 <= firepower).DefaultIfEmpty(0).Max();
    }

    /// <summary>Whether a Location is one a mortar's shot attacks (B23.32; pass 60, ruling R60.11): the named one, or another of its building hex.</summary>
    internal static bool InTargetHex(OrdnanceShot shot, string? locationId) =>
        locationId == shot.TargetLocationId || shot.AreaLocations?.Any(item => item.LocationId == locationId) == true;

    /// <summary>
    /// Whether an Infantry Target Type shot is at an unattended Gun (C3.41, C11.4, pp. 170, 182; pass 60, ruling R60.18): a Location whose only target
    /// is a Gun no unit mans and no vehicle tows, with no gunshield and no Emplacement. The Area Target Type's is not built.
    /// </summary>
    internal static bool Unattended(OrdnanceShot shot) =>
        shot.TargetType is null && shot.VehicleTarget is null && shot.Hit is { GunTarget: { CrewUnitId: null, GunId: not null }, Targets: { Count: 0 } } hit && hit.Vehicles is not { Count: > 0 };

    /// <summary>
    /// The units an Infantry Target Type hit attacks (C3.32): every in-LOS enemy unit of the Location, but only the Gun's manning crew for an ATR that
    /// is not 20L (C13.23, C13.24; pass 60, ruling R60.6).
    /// </summary>
    private static IReadOnlyList<FireTarget> HitTargets(OrdnanceShot shot, GunDefinition gun) =>
        gun.LatwType == "atr" && gun.Caliber != 20 && shot.Hit!.GunTarget is { CrewUnitId: { } crew }
            ? [.. shot.Hit.Targets!.Where(item => item.UnitId == crew)]
            : shot.Hit!.Targets!;

    /// <summary>
    /// The vehicles of the target Location an Infantry Target Type hit can affect (C3.32, A7.308, D.8B, p. 169; pass 60, ruling R60.8): each
    /// unarmored vehicle, and each AFV whose crew is Vulnerable (CE, not Stunned or Recalled; D5.3, D5.34), which a General Collateral Attack
    /// attacks; an AFV whose crew is not Vulnerable is immune and is not fired at. None for a Vehicle or Area Target Type shot.
    /// </summary>
    private static IReadOnlyList<FireVehicle> HitVehicles(OrdnanceShot shot, ScenarioA1OrdnanceReference reference) =>
        shot.VehicleTarget is not null || shot.TargetType is not null || shot.Hit?.Vehicles is not { Count: > 0 } inLocation ? []
            : [.. inLocation.Where(item => Unarmored(item, reference) || (item.CrewExposed == true && item.Stunned != true))];

    /// <summary>Whether a vehicle of the Location is unarmored (D1.21).</summary>
    private static bool Unarmored(FireVehicle vehicle, ScenarioA1OrdnanceReference reference) =>
        reference.Fire.Definitions.GetValueOrDefault(vehicle.DefinitionId ?? string.Empty)?.Unarmored == true;

    /// <summary>
    /// A vehicle's own To Hit DRM on the Infantry Target Type (C3.32 and its EX, p. 169; pass 60, ruling R60.8): Case K when it is concealed, else the
    /// shot's Case N; its size (Case P, C6.7); the TEM of its Location (Case Q, C6.8), as on the Vehicle Target Type; and Height Advantage when it has
    /// no other positive TEM (D4.22, B10.31).
    /// </summary>
    private static List<FireModifier> OwnVehicleDrm(OrdnanceShot shot, FireVehicle vehicle, IReadOnlyList<FireModifier> caseN, ScenarioA1OrdnanceReference reference)
    {
        var drm = new List<FireModifier>(vehicle.Concealed == true ? [new FireModifier("case-k", 2, "C6.2")] : caseN);
        var targetSize = reference.Armor.Vehicles.GetValueOrDefault(vehicle.DefinitionId ?? string.Empty)?.TargetSize;
        var size = targetSize switch
        {
            "very-small" => 2,
            "small" => 1,
            "large" => -1,
            "very-large" => -2,
            _ => 0,
        };
        if (size != 0)
        {
            drm.Add(new FireModifier("case-p:" + targetSize, size, "C6.7"));
        }

        if (ScenarioA1FireReference.Tem.GetValueOrDefault(shot.Hit!.TargetTerrain!) is var tem and not 0)
        {
            drm.Add(new FireModifier("case-q:" + shot.Hit.TargetTerrain, tem, "C6.8"));
        }

        if (HeightAdvantageApplies(shot, drm))
        {
            drm.Add(new FireModifier("case-q:height-advantage", 1, "B10.31"));
        }

        return drm;
    }

    /// <summary>Whether an ATR that is not 20L fires at a Gun's manning crew (C11.52, C13.23; pass 60, ruling R60.6), its only target on the Infantry Target Type.</summary>
    private static bool AtrAtGun(OrdnanceShot shot, GunDefinition gun) =>
        HitTargets(shot, gun) is [_] && ScenarioA1FireCalculator.GunCrewAlone(shot.Hit! with
        {
            Targets = HitTargets(shot, gun)
        });

    /// <summary>
    /// The reasons a shot by HE Equivalency is outside (C8.1, C8.9, C8.31; pass 60, ruling R60.4): a Gun fires AP unless its listing denies it, and
    /// APCR, APDS, and HEAT where it lists them, by the Depletion Number of pass 55; a Gun's HEAT needs a printed HE FP to step left from; and
    /// "HEAT may only be fired at a vehicle or Gun, or at Infantry/Cavalry receiving a wall/building/rubble/pillbox TEM".
    /// </summary>
    private static void EquivalencyOutside(OrdnanceShot shot, GunDefinition gun, ScenarioA1OrdnanceReference reference, List<string> outside)
    {
        var ammunition = shot.Ammunition!;
        if (gun.GunType != "latw")
        {
            var special = ammunition is "apcr" or "apds" or "heat";
            if ((ammunition == "ap" && gun.NoAp) || (special && !ScenarioA1ArmorCalculator.Lists(gun, ammunition)))
            {
                outside.Add(Prefix + "ammunition-outside");
                return;
            }

            if ((special || ScenarioA1ArmorCalculator.Lists(gun, ammunition)) && ScenarioA1ArmorCalculator.ShotDepletion(shot, gun) is null)
            {
                outside.Add(Prefix + "ammunition-unavailable" + ScenarioA1ArmorCalculator.StartMonthDetail(shot, gun));
            }

            if (shot.Gun!.Depleted?.Contains(ammunition, StringComparer.Ordinal) == true)
            {
                outside.Add(Prefix + "ammunition-depleted");
            }

            if (ammunition == "heat" && HeFirepowerOf(gun, reference) is null)
            {
                outside.Add(Prefix + "heat-no-he");
            }
        }

        // B23.3, B24.3, B9.3: a building or rubble Location, or a wall's TEM toward the firer; no pillbox is built. A Gun's crew alone is the Gun's.
        var hit = shot.Hit!;
        if (ammunition == "heat" && hit.GunTarget is null && !ScenarioA1Definitions.IsBuildingTerrain(hit.TargetTerrain)
            && !ScenarioA1Definitions.IsRubbleTerrain(hit.TargetTerrain) && shot.WallTem != true)
        {
            // The table player (pass 60): a PF or PSK, which fires no HE or AP, is told so in the refusal's detail.
            outside.Add(Prefix + "heat-target-outside" + (gun.LatwType is "pf" or "psk" ? ":" + LatwHeat : string.Empty));
        }
    }

    /// <summary>The detail of a HEAT refusal at Infantry in the open whose weapon is a PF or a PSK, which has no HE or AP to fire instead.</summary>
    public const string LatwHeat = "latw";

    /// <summary>A21.12 (pass 65, ruling R65.22): "All attacks by captured ordnance must use the red To Hit Numbers", under a Temporary crew's penalty.</summary>
    internal static string CapturedColor(OrdnanceShot shot, string color) => shot.CapturedPenalty is > 0 ? "red" : color;

    /// <summary>The Gun's B#, two lower while Intensive Firing (C5.62), lower in Extreme Winter (E3.741; ruling R16.14), and a SW's under Ammunition Shortage (A19.131).</summary>
    internal static int Breakdown(OrdnanceShot shot, GunDefinition gun, ScenarioA1OrdnanceReference reference) =>
        gun.Breakdown - (shot.IntensiveFire == true ? 2 : 0) - (shot.BreakdownReduction ?? 0)

        // A21.11 (pass 65, ruling R65.22): a Temporary crew's captured-weapon penalty, two lower each.
        - (2 * (shot.CapturedPenalty ?? 0))

        // A19.32 (referee, pass 9b): the B# or X# of a SW Inexperienced Personnel use is one lower.
        - (gun.GunType is "mortar" or "latw" or ScenarioA1MachineGunKill.GunType && gun.LatwType != "pf"
            && reference.Fire.Definitions.GetValueOrDefault(shot.Crew?.DefinitionId ?? string.Empty)?.Class is "green" or "conscript" ? 1 : 0)

        // A19.131 (p. 86; pass 72, ruling R72.12): "all SW Original B#/X# [EXC: DC] ... are decreased by one during an Ammunition Shortage"; A21.11 (p. 88): "do
        // not apply to captured Gun/SW". A vehicle's or Gun's circled B# (D3.71) is pass 105's.
        - (shot.AmmunitionShortage == true && shot.Vehicle is null && gun.GunType is "mortar" or "latw" or ScenarioA1MachineGunKill.GunType && gun.LatwType != "pf"
            && ScenarioA1FireEligibility.CapturedBy(gun.Nationality, shot.FiringNationality) is null && shot.CapturedPenalty is not > 0 ? 1 : 0);

    /// <summary>
    /// Whether the shot keeps the Multiple ROF, and the fire counter it leaves otherwise (C2.24, A8.1, C5.6): Prep Fire in the PFPh and
    /// AFPh, First Fire in the MPh, Final Fire in the DFPh, and Intensive Fire after an Intensive Fire shot, which never keeps it.
    /// </summary>
    internal static (bool Kept, string? Counter) Counter(OrdnanceShot shot, bool malfunctioned, int rof, int colored)
    {
        if (shot.IntensiveFire == true)
        {
            return (false, "intensive-fire");
        }

        var kept = !malfunctioned && rof > 0 && colored <= rof;
        return (kept, kept ? null : shot.Phase switch
        {
            "PFPh" or "AFPh" => "prep-fire",

            // D3.3 (p. 199; pass 50): "is marked with a Bounding Fire counter [EXC: if the only weapon fired did not exhaust its Multiple ROF]".
            "MPh" => shot.FireKind == ScenarioA1FireCalculator.BoundingFirstFire ? "bounding-fire" : "first-fire",
            _ => "final-fire",
        });
    }

    /// <summary>
    /// Case C and its subcases (C5.3 to C5.33, D3.32, pp. 172 and 200; pass 50), or null when none applies. A vehicle's MA fired in its own MPh
    /// "must add the +2 DRM of Case B to the applicable Gun Type DRM of Case C": 2 for a T or ST turret, 3 for a NT mount (the C5.3 EX: +4 and
    /// +5). Case C1 adds one for a continuous LOS of two and a half to three MP, Case C2 two for at most two MP, and more than three MP, or a LOS
    /// held since before the MPh, is Case C alone. In the AFPh a vehicle that entered a new hex in its MPh, is in Motion, or fires on the ROF its
    /// Bounding First Fire kept uses Case C; the subcases "do not apply in the AFPh". Case B is part of it and is not added again. A DEFENDER's
    /// vehicle in Motion uses Case C alone (C5.35; pass 55, ruling R55.14): C5.31 and C5.32 give C1 and C2 only to "a vehicle First Firing during
    /// its own MPh".
    /// </summary>
    internal static FireModifier? CaseC(OrdnanceShot shot, GunDefinition gun)
    {
        if (shot.Vehicle is not { } vehicle)
        {
            return null;
        }

        var bounding = shot.FireKind == ScenarioA1FireCalculator.BoundingFirstFire;
        if (!bounding && !DefenderMotionFirer(shot)
            && !(shot.Phase == "AFPh" && shot.FiringSide == "phasing" && (vehicle.Moved == true || vehicle.InMotion == true || vehicle.BoundingFirer == true)))
        {
            return null;
        }

        var mount = 2 + (gun.MaType is "t" or "st" or "rst" or "1mt" ? 2 : 3);
        return !bounding || vehicle.MpInLos is null or > 3 ? new FireModifier("case-c", mount, "C5.3")
            : vehicle.MpInLos > 2 ? new FireModifier("case-c1", mount + 1, "C5.31")
            : new FireModifier("case-c2", mount + 2, "C5.32");
    }

    /// <summary>
    /// C6.55 (p. 175; review of pass 50): "only a Stabilized Gun (D11.1) may claim/retain its Acquisition DRM for Motion/Bounding-First Fire TH
    /// attempts". No vehicle of the catalog is Stabilized: such a shot takes no Case N, and what it acquires starts from nothing (ruling R50.13).
    /// </summary>
    internal static bool AcquisitionDenied(OrdnanceShot shot) => shot.FireKind == ScenarioA1FireCalculator.BoundingFirstFire || MotionFirer(shot);

    /// <summary>
    /// Whether the shot is a Motion firer's (C5.35, D2.42, pp. 172 and 198; pass 50): a vehicle in Motion, or moving and not stopped, that fires in
    /// its own MPh or AFPh, or a DEFENDER's vehicle in Motion. It uses Case C, C1, or C2 as applicable and "must double the lower dr of its TH DR".
    /// </summary>
    internal static bool MotionFirer(OrdnanceShot shot) =>
        shot.Vehicle?.InMotion == true
            && (shot.FireKind == ScenarioA1FireCalculator.BoundingFirstFire || (shot.Phase == "AFPh" && shot.FiringSide == "phasing") || DefenderMotionFirer(shot));

    /// <summary>
    /// D2.401, D2.42, C5.35 (pp. 172, 198; pass 55, ruling R55.14): a vehicle in Motion of the side that is not phasing, which keeps its Motion
    /// counter until its own next MPh or gained it by a Motion attempt, and "may still use Motion Fire (Case C4)": Defensive First Fire in the
    /// enemy MPh and Final Fire in the DFPh, with Case C alone and the lower dr of its TH DR doubled.
    /// </summary>
    internal static bool DefenderMotionFirer(OrdnanceShot shot) =>
        shot.Vehicle?.InMotion == true && shot.FiringSide == "non-phasing" && shot.Phase is "MPh" or "DFPh";

    /// <summary>
    /// The firer-based DRM of the backlog pass 8 (rulings R8.2, R8.8, R8.10): Case F for Intensive Fire (C5.61), Case H for a non-qualified
    /// crew (C5.8), and the overstacking of the firer's and the target's Locations (A5.12, A5.131).
    /// </summary>
    internal static IEnumerable<FireModifier> PassEightFirerDrm(OrdnanceShot shot)
    {
        if (shot.IntensiveFire == true)
        {
            yield return new FireModifier("case-f", 2, "C5.61");
        }

        if (shot.NonQualified == true)
        {
            yield return new FireModifier("case-h", 2, "C5.8");
        }

        // A21.12, A21.13 (pass 65, ruling R65.22): a Temporary crew's captured ordnance adds Case H's +2, doubled for a HS in a captured vehicle.
        if (shot.CapturedPenalty is > 0 and var captured)
        {
            yield return new FireModifier("case-h:captured", 2 * captured, "A21.12");
        }

        if (shot.FirerOverstack is { } over && over > 0)
        {
            yield return new FireModifier("overstack-firer", over, "A5.12");
        }

        if (shot.TargetOverstack is { } crowded && crowded > 0 && shot.VehicleTarget is null)
        {
            yield return new FireModifier("overstack-target", -crowded, "A5.131");
        }
    }

    /// <summary>A nested IFT resolution that stopped: its missing roll asked for under the prefix, or its reasons.</summary>
    private static OrdnanceResolution? Pending(FireResolution resolution, string prefix)
    {
        if (resolution.Disposition == FireResolution.Resolved)
        {
            return null;
        }

        // Ruling R5.8: an option the hit reaches is asked for under its own key, which names its unit.
        if (resolution.Reasons is [{ } choice] && choice.StartsWith("asl.a1.fire.choice-missing:", StringComparison.Ordinal))
        {
            return Refused(OrdnanceResolution.Indeterminate, [Prefix + "choice-missing:" + choice["asl.a1.fire.choice-missing:".Length..]]);
        }

        return resolution.Reasons is [{ } reason] && reason.StartsWith("asl.a1.fire.roll-missing:", StringComparison.Ordinal)
            ? Refused(OrdnanceResolution.Indeterminate, [Prefix + "roll-missing:" + prefix + reason["asl.a1.fire.roll-missing:".Length..]])
            : Refused(resolution.Disposition == FireResolution.Abstained ? OrdnanceResolution.Abstained : OrdnanceResolution.Indeterminate,
                [.. resolution.Reasons.Select(item => Prefix + prefix + item)]);
    }
}
