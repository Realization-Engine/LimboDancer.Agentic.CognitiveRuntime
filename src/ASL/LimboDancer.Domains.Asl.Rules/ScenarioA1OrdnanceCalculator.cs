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

    private static readonly string[] Personnel = ["asl:squad", "asl:half-squad", "asl:crew", "asl:leader", "asl:hero"];

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

        var gun = reference.Guns[shot.Gun!.DefinitionId!];
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

            var fired = ScenarioA1ArmorCalculator.Run(shot, gun, reference);
            return fired.Disposition == OrdnanceResolution.Resolved ? fired with
            {
                PanzerfaustCheck = check
            } : fired;
        }

        return shot.TargetType == OrdnanceTargetTypes.Area ? ScenarioA1AreaCalculator.Run(shot, gun, reference)
            : shot.VehicleTarget is not null ? ScenarioA1ArmorCalculator.Run(shot, gun, reference) : Run(shot, reference);
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

        if (kind is "asl:half-squad" or "asl:crew")
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
        if (first.Disposition != OrdnanceResolution.Indeterminate || first.Reasons is not ([Prefix + "roll-missing:toHit"] or [Prefix + "roll-missing:panzerfaustCheck"]))
        {
            return first.Reasons;
        }

        // A Vehicle Target Type shot has no IFT attack to precheck (C3.31).
        if (shot.VehicleTarget is not null)
        {
            return [];
        }

        var gun = reference.Guns[shot.Gun!.DefinitionId!];
        var reasons = new List<string>();
        foreach (var critical in new[] { false, true })
        {
            reasons.AddRange(ScenarioA1FireCalculator.Precheck(HitAttack(shot, gun, reference, critical, shot.Hit!.Targets!, null), reference.Fire)
                .Select(reason => Prefix + (critical ? "critical-hit:" : "hit:") + reason));
        }

        return reasons.Distinct(StringComparer.Ordinal).ToList();
    }

    private static OrdnanceResolution Refused(string disposition, IReadOnlyList<string> reasons) => new(disposition, reasons, null, null);

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
        Need(shot.Hit?.TargetTerrain, "hit.targetTerrain");
        Need(shot.Hit?.Los, "hit.los");
        Need(shot.Hit?.Los?.Blocked, "hit.los.blocked");
        Need(shot.Hit?.Los?.HindranceDrm, "hit.los.hindranceDrm");
        Need(shot.Hit?.Los?.HindranceAttributed, "hit.los.hindranceAttributed");
        Need(shot.Hit?.Los?.GrainInLos, "hit.los.grainInLos");
        Need(shot.Hit?.SameLevel, "hit.sameLevel");
        if (shot.VehicleTarget is not null)
        {
            ScenarioA1ArmorCalculator.Missing(shot, Need);
        }

        if (shot.FireKind is not null)
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
        // Ruling R8.1: Defensive First Fire in the MPh by the non-phasing side.
        if (shot.FireKind is null
            ? (phase, shot.FiringSide) is not (("PFPh", "phasing") or ("AFPh", "phasing") or ("DFPh", "non-phasing"))
            : shot.FireKind != "first-fire" || (phase, shot.FiringSide) is not ("MPh", "non-phasing"))
        {
            outside.Add(Prefix + "phase-outside");
        }

        // C2.21, C2.22, C2.3, C3.33: a Gun of the firing side that can fire HE; mortars (the Area Target Type), RCL, and 360-degree
        // mounts are not reviewed.
        var gunShot = shot.Gun!;
        if (!reference.Guns.TryGetValue(gunShot.DefinitionId!, out var gun) || gun.Nationality != shot.FiringNationality || gun.Mount360
            || !(shot.Vehicle is null ? AdmittedGunTypes.Contains(gun.GunType) : gun.GunType == "vehicle")
            || (shot.VehicleTarget is null && (gun.NoHe || shot.Ammunition is not (null or "he"))))
        {
            outside.Add(Prefix + "gun-outside");
            return outside;
        }

        // C9.1, C3.33 (rulings R9.2, R9.3): a mortar always fires on the Area Target Type; other Guns' Area Target Type fire is not built. C13.1,
        // C13.3 (ruling R9.8): a PF fires only on the Vehicle Target Type. A SW has no CA, and neither Intensive Fires (Case F is NA to SW).
        var mortar = gun.GunType == "mortar";
        var latw = gun.GunType == "latw";
        var pf = gun.LatwType == "pf";
        if ((shot.TargetType == OrdnanceTargetTypes.Area) != mortar || (latw && shot.VehicleTarget is null)
            || ((mortar || latw) && (shot.HexspinesToTurn != 0 || shot.IntensiveFire == true || shot.BoreSighted == true || shot.NonQualified == true))
            // C9.3 (referee, pass 9): a Spotter is designated in the PFPh or DFPh; Opportunity Fire, which would let it spot in the AFPh, is not built.
            || (shot.Spotter is not null && (!mortar || shot.Phase is not ("PFPh" or "DFPh"))) || (shot.Director is not null && !mortar && !latw))
        {
            outside.Add(Prefix + "target-type-outside");
            return outside;
        }

        // D5.34, C7.42, D5.341 (ruling R7.10): a Stunned, Shocked, or Recalled AFV does not fire; D2.42, C5.35: a firer in Motion needs Case C4,
        // and C5.3: one that entered a new hex in its MPh needs Case C in the AFPh, neither built; D1.321, D1.322: an RST or 1MT MA fires only BU.
        if (shot.Vehicle is { } firing && (firing.Stunned == true || firing.Shocked == true || firing.Recalled == true || firing.InMotion == true
            || (phase == "AFPh" && firing.Moved == true)
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
        // Chart (pass 9b): a lone SMC does not fire a PSK at full effect.
        if ((shot.Vehicle is null ? reference.Fire.Definitions.GetValueOrDefault(crew.DefinitionId!) is not { } crewDefinition
                || crewDefinition.Nationality != gun.Nationality
                || (mortar || latw ? !Personnel.Contains(crewDefinition.Kind) : shot.NonQualified == true ? !crewDefinition.IsMmc : crewDefinition.Kind != "asl:crew")
                || (gun.LatwType == "psk" && crewDefinition.Kind is "asl:leader" or "asl:hero")
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
        var mayFire = pf
            ? gunShot.FiredThisPlayerTurn != true && gunShot.ShotsThisPhase < (reference.Fire.Definitions.GetValueOrDefault(crew.DefinitionId!)?.Kind == "asl:squad" ? 2 : 1)
            : shot.IntensiveFire == true
            ? shot.Vehicle is null && phase != "AFPh" && gunShot.IntensiveFired != true && gunShot.FinalFire != true && crew.Pinned != true
                && (gunShot.FiredThisPlayerTurn == true || gunShot.FirstFire == true) && gunShot.RateOfFireKept != true
            : gunShot.IntensiveFired != true && gunShot.FirstFire != true && (phase == "AFPh"
                ? gunShot.FiredThisPlayerTurn != true && gunShot.ShotsThisPhase == 0
                : gunShot.ShotsThisPhase == 0 ? gunShot.FiredThisPlayerTurn != true : gunShot.RateOfFireKept == true);
        if (!mayFire || crew.FiredInherentFp == true)
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
        if ((sameHex ? shot.Range != 0 || shot.VehicleTarget is not null || shot.HexspinesToTurn != 0 || shot.FireKind is not null || mortar || latw : shot.Range < 1)
            || (gun.RangeMaximum is { } maximum && shot.Range > maximum) || shot.ElevationAllowed != true
            // C9.4: never nearer than a mortar's minimum range; C13.32: a PF's range by the scenario date.
            || (gun.RangeMinimum is { } minimum && shot.Range < minimum) || (pf && shot.Range > PanzerfaustRange(shot.ScenarioYear, shot.Hit!.ScenarioMonth))
            || (gun.ToHitTable.Count > 0 && shot.Range > gun.ToHitTable.Count))
        {
            outside.Add(Prefix + "out-of-range");
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
        if (shot.HexspinesToTurn > 0 && (crew.Pinned == true || (shot.FirerInWoodsOrBuilding == true && gunShot.ShotsThisPhase > 0)))
        {
            outside.Add(Prefix + "covered-arc-fixed");
        }

        var hit = shot.Hit!;
        if (hit.Los!.Blocked == true)
        {
            outside.Add(Prefix + "los-blocked");
        }

        if (shot.VehicleTarget is not null)
        {
            ScenarioA1ArmorCalculator.Outside(shot, gun, reference, outside);
            if (hit.TargetLocationId != shot.TargetLocationId || hit.Phase != shot.Phase || hit.FiringSide != shot.FiringSide || hit.Firers is { Count: > 0 }
                || hit.Director is not null || hit.FireKind is not null || hit.OrdnanceHit is not null)
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
        if (hit.Targets!.Any(item => item.Dummy != true && reference.Fire.Definitions.GetValueOrDefault(item.DefinitionId ?? string.Empty)?.Nationality == gun.Nationality))
        {
            // C3.33 (ruling R9.3): the Area Target Type would hit friendly units too, on their own side's ELR, which is not built.
            outside.Add(Prefix + (mortar ? "area-friendly-units" : "target-outside"));
        }

        // Ruling R9.3: an Area Target Type shot at a Gun and its crew is not built.
        if (mortar && hit.GunTarget is not null)
        {
            outside.Add(Prefix + "target-outside");
        }

        if (hit.Targets!.Count == 0 || hit.TargetLocationId != shot.TargetLocationId || hit.Phase != shot.Phase || hit.FiringSide != shot.FiringSide
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

    /// <summary>A PF's range (C13.32): one hex before June 1944, two from June to December 1944, three from 1945.</summary>
    public static int PanzerfaustRange(int? year, int? month) => year >= 1945 ? 3 : year == 1944 && month >= 6 ? 2 : 1;

    private static bool Malformed(OrdnanceRolls rolls)
    {
        static bool Dice(IReadOnlyList<int>? dice) => dice is not null && (dice.Count != 2 || dice.Any(die => die is < 1 or > 6));
        return Dice(rolls.ToHit) || rolls.Subsequent is < 1 or > 6 || rolls.CriticalSelection?.Values.Any(dr => dr is < 1 or > 6) == true
            || Dice(rolls.ToKill) || Dice(rolls.ShockCheck) || Dice(rolls.CrewCheck) || Dice(rolls.CrewSurvival) || rolls.PanzerfaustCheck is < 1 or > 6;
    }

    private static List<string> Undecided(OrdnanceShot shot)
    {
        var undecided = new List<string>();
        var hit = shot.Hit!;

        // Levels and an unattributed Hindrance are not reviewed, as for Infantry fire.
        if (hit.SameLevel != true)
        {
            undecided.Add(Prefix + "levels-differ");
        }

        var los = hit.Los!;
        if (los.HindranceAttributed != true || los.HindranceDrm < 0 || (los.GrainInLos == true && hit.ScenarioMonth is null))
        {
            undecided.Add(Prefix + "hindrance-unattributed");
        }

        // C6.2: Case K applies to each concealed target alone, so a Location mixing concealed and Known units would need two To
        // Hit results; the review leaves that out.
        var concealed = hit.Targets!.Select(Concealed).Distinct().ToArray();
        if (shot.VehicleTarget is null && shot.TargetType is null && concealed.Length > 1)
        {
            undecided.Add(Prefix + "concealment-mixed");
        }

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
    /// The IFT attack of a hit: the Gun's HE FP column (C.6), doubled by a Critical Hit (C3.71), on the given targets; the Location's
    /// other units, which the other attack of a split hit attacks, are its companions for a berserk leader's NTC (A15.41).
    /// </summary>
    internal static FireAttack HitAttack(OrdnanceShot shot, GunDefinition gun, ScenarioA1OrdnanceReference reference, bool critical,
        IReadOnlyList<FireTarget> targets, FireRolls? rolls)
    {
        // C3.33, C9.5 (ruling R9.3): an Area Target Type hit attacks at half the HE FP (the IFT column at or below it), a Critical Hit at the
        // full HE FP doubled.
        var area = shot.TargetType == OrdnanceTargetTypes.Area;
        var firepower = reference.HeFirepower(gun.Caliber);
        if (area && !critical)
        {
            firepower = ScenarioA1FireReference.ColumnFp.Where(column => column * 2 <= firepower).DefaultIfEmpty(0).Max();
        }

        var others = shot.Hit!.Targets!.Where(item => !targets.Contains(item)).ToArray();

        // Ruling R5.8: each attack of a split hit answers the options of its own targets.
        var ids = targets.Select(item => item.UnitId).ToHashSet(StringComparer.Ordinal);
        return shot.Hit with
        {
            Choices = shot.Hit.Choices is { } choices
                ? choices.Where(item => item.Key.Split(':') is [_, var unit, ..] && ids.Contains(unit)).ToDictionary(item => item.Key, item => item.Value, StringComparer.Ordinal)
                : null,
            Targets = targets,
            Firers = [],
            Director = null,
            Companions = others.Length > 0 ? [.. others, .. shot.Hit.Companions ?? []] : shot.Hit.Companions,
            OrdnanceHit = new FireOrdnanceHit(shot.Gun!.GunId, firepower, critical)
            {
                Area = area ? true : null
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
        var color = ScenarioA1OrdnanceReference.Color(shot.FiringNationality!);

        // C3.3, C4: the Basic TH# of the Infantry Target Type by range and color, and the Gun and Ammo modifications.
        var basic = reference.BasicToHit(color, range);
        var modifications = reference.Modifications(gun, range);
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

        if (shot.Phase == "AFPh")
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

        drm.AddRange(PassEightFirerDrm(shot));
        var sameHex = shot.SameHex == true;
        if (sameHex)
        {
            // C5.5: Case E, doubled in woods or a building; Cases J3, J4, L, and M do not apply.
            drm.Add(new FireModifier("case-e", woods ? 4 : 2, "C5.5"));
        }
        else if (shot.FireKind is not null && shot.Movement is { } movement)
        {
            // C6.13, C6.14 (ruling R8.1): FFNAM and FFMO as To Hit DRM of Defensive First Fire.
            if (movement.NonAssault == true)
            {
                drm.Add(new FireModifier("case-j3", -1, "C6.13"));
            }

            if (movement.OpenGround == true)
            {
                drm.Add(new FireModifier("case-j4", -1, "C6.14"));
            }
        }

        var concealedTarget = hit.Targets!.All(Concealed);
        if (concealedTarget)
        {
            drm.Add(new FireModifier("case-k", 2, "C6.2"));
        }

        if (range is >= 1 and <= 2 && !sameHex)
        {
            drm.Add(new FireModifier("case-l", range == 1 ? -2 : -1, "C6.3"));
        }

        // C6.4 (ruling R8.8): the Bore Sighted Location's -2, the firer choosing it or the Acquisition, never in its own hex.
        if (shot.BoreSighted == true && !sameHex)
        {
            drm.Add(new FireModifier("case-m", -2, "C6.4"));
        }
        else if (shot.Acquisition is int acquired && acquired < 0 && !concealedTarget)
        {
            // C6.51, C6.57: the Acquisition applies only to Known units, and a shot at a concealed target loses it.
            drm.Add(new FireModifier("case-n", acquired, "C6.5"));
        }

        var tem = ScenarioA1FireReference.Tem.GetValueOrDefault(hit.TargetTerrain!);
        var gunTarget = ScenarioA1FireCalculator.GunCrewAlone(hit) ? hit.GunTarget : null;
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

        // E1.7, E3.1 (referee, pass 16): the Low Visibility DRM is a Hindrance of its own on the TH DR.
        if (shot.LowVisibilityDrm is > 0 and var lowVisibility)
        {
            drm.Add(new FireModifier("case-r:lv", lowVisibility, "E1.7"));
        }

        if (hit.Los!.HindranceDrm is int hindrance && hindrance > 0)
        {
            drm.Add(new FireModifier("case-r", hindrance, "C6.9"));
        }

        if (rolls.ToHit is not { } dice)
        {
            return Refused(OrdnanceResolution.Indeterminate, [Prefix + "roll-missing:toHit"]);
        }

        var total = (int)drm.Sum(item => item.Value);
        var original = dice[0] + dice[1];
        var final = original + total;
        var lowest = 2 + total;

        // C3.6: when no Final DR can hit, an Original 2 still may, on a subsequent dr of 1 (a Critical Hit) to 3.
        var improbable = lowest > modified;
        bool isHit, critical;
        int? subsequent = null;
        if (improbable)
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
            if (isHit && !critical && original == 2)
            {
                if (rolls.Subsequent is not { } dr)
                {
                    return Refused(OrdnanceResolution.Indeterminate, [Prefix + "roll-missing:subsequent"]);
                }

                subsequent = dr;
                critical = dr == 1 || dr * 2 <= modified;
            }
        }

        var toHit = new OrdnanceToHit(color, basic, modifications, modified, [.. dice], original, drm, final, improbable, subsequent, isHit, critical);

        // C2.28: an Original To Hit DR at or above the B# malfunctions the Gun; C5.62: two lower while Intensive Firing.
        var malfunctioned = original >= Breakdown(shot, gun, reference);

        // C2.24: an Original colored dr at most the ROF keeps the Multiple ROF; C2.5: a non-vehicular NT Gun's ROF is one lower after a
        // CA change; C5.4: a pinned crew forfeits it; C5.2: none in the AFPh.
        var rof = gun.RateOfFire ?? 0;
        if (shot.HexspinesToTurn > 0 && shot.Vehicle is null)
        {
            rof--;
        }

        if (shot.Crew.Pinned == true || shot.Phase == "AFPh")
        {
            rof = 0;
        }

        var (kept, counter) = Counter(shot, malfunctioned, rof, dice[0]);

        FireResolution? normal = null, criticalHit = null;
        string? criticalTarget = null;
        if (isHit)
        {
            var targets = hit.Targets!;
            IReadOnlyList<FireTarget> criticalTargets = [];
            if (critical)
            {
                if (targets.Count == 1)
                {
                    criticalTargets = targets;
                }
                else
                {
                    // C3.74: a Critical Hit applies to the target Random Selection picks (the highest dr, every tied unit alike; A.9).
                    var key = "criticalSelection:" + string.Join(",", targets.Select(item => item.UnitId));
                    if (rolls.CriticalSelection is not { } drs || targets.Any(item => !drs.ContainsKey(item.UnitId!)))
                    {
                        return Refused(OrdnanceResolution.Indeterminate, [Prefix + "roll-missing:" + key]);
                    }

                    var highest = targets.Max(item => drs[item.UnitId!]);
                    criticalTargets = [.. targets.Where(item => drs[item.UnitId!] == highest)];
                    criticalTarget = string.Join(",", criticalTargets.Select(item => item.UnitId));
                }

                criticalHit = ScenarioA1FireCalculator.Resolve(HitAttack(shot, gun, reference, true, criticalTargets, rolls.CriticalHit), reference.Fire);
                if (Pending(criticalHit, "critical-hit:") is { } pending)
                {
                    return pending;
                }
            }

            // C3.32: every unit hit is attacked with a single Effects DR, so the normal hit reuses the Critical Hit's (C3.74).
            var others = targets.Where(item => !criticalTargets.Contains(item)).ToArray();
            if (others.Length > 0)
            {
                var hitRolls = criticalHit?.Arithmetic?.Dice is { } shared ? (rolls.Hit ?? new FireRolls(null, null, null, null)) with
                {
                    Attack = shared
                } : rolls.Hit;
                normal = ScenarioA1FireCalculator.Resolve(HitAttack(shot, gun, reference, false, others, hitRolls), reference.Fire);
                if (Pending(normal, "hit:") is { } pending)
                {
                    return pending;
                }
            }
        }

        // C6.5, C6.57: the shot acquires the target Location, one step more (to -2) if already acquired, unless the Gun malfunctions; a
        // concealed target is acquired only when the shot costs it its concealment.
        var effects = (normal?.Effects ?? []).Concat(criticalHit?.Effects ?? []).ToArray();
        var acquires = !malfunctioned && (!concealedTarget || effects.Any(item => item.ConcealmentLost));
        var acquisition = !acquires ? 0 : concealedTarget ? -1 : Math.Max(shot.Acquisition!.Value - 1, -2);
        var gunEffect = new OrdnanceGunEffect(Breakdown(shot, gun, reference), malfunctioned, kept ? Math.Max(rof, 0) : shot.IntensiveFire == true ? 0 : Math.Max(rof, 0), kept, counter,
            acquisition, acquires ? shot.TargetLocationId : null);

        // C11.4, C11.6 (ruling R8.3): a Critical Hit destroys the Gun; a KIA before the gunshield destroys it, a K malfunctions it.
        string? fate = null;
        if (gunTarget is not null && isHit)
        {
            var result = (normal ?? criticalHit)?.Arithmetic?.Result ?? string.Empty;
            fate = criticalHit is not null ? "destroyed"
                : (normal?.Arithmetic?.Drm.Any(item => item.Name.StartsWith("gunshield:", StringComparison.Ordinal)) ?? false) ? null
                : System.Text.RegularExpressions.Regex.IsMatch(result, "^([1-7])?KIA$") ? "destroyed"
                : System.Text.RegularExpressions.Regex.IsMatch(result, "^K/([1-4])$") ? "malfunctioned"
                : null;
        }

        return new OrdnanceResolution(OrdnanceResolution.Resolved, [], toHit, gunEffect)
        {
            // A12.14 (ruling R8.5): the "?" is lost only in the view of a Good Order enemy ground unit within 16 hexes.
            CrewConcealmentLost = shot.Crew.Concealed == true && shot.CrewSeen != false ? true : null,
            Hit = normal,
            CriticalHit = criticalHit,
            CriticalTarget = criticalTarget,
            GunTargetFate = fate,
        };
    }

    /// <summary>The Gun's B#, two lower while Intensive Firing (C5.62), and lower in Extreme Winter (E3.741; ruling R16.14).</summary>
    internal static int Breakdown(OrdnanceShot shot, GunDefinition gun, ScenarioA1OrdnanceReference reference) =>
        gun.Breakdown - (shot.IntensiveFire == true ? 2 : 0) - (shot.BreakdownReduction ?? 0)

        // A19.32 (referee, pass 9b): the B# or X# of a SW Inexperienced Personnel use is one lower.
        - (gun.GunType is "mortar" or "latw" && gun.LatwType != "pf"
            && reference.Fire.Definitions.GetValueOrDefault(shot.Crew?.DefinitionId ?? string.Empty)?.Class is "green" or "conscript" ? 1 : 0);

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
            "MPh" => "first-fire",
            _ => "final-fire",
        });
    }

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
