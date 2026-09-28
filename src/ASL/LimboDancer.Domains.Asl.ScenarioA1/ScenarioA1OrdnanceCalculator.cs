namespace LimboDancer.Domains.Asl.ScenarioA1;

/// <summary>
/// The pure resolution of the Ordnance package (unit step 24): a Gun's HE shot at Infantry on the Infantry Target Type. The To
/// Hit DR is made against the Modified TH# with the firer- and target-based DRM of the reviewed Cases (C3.3, C4, C5, C6); a hit
/// is resolved on the IFT by the Fire package with the Gun's HE FP (C.6), a Critical Hit on the unit Random Selection picks
/// (C3.71, C3.74). Every roll is asked for one at a time as <c>roll-missing:&lt;key&gt;</c>.
/// </summary>
public static class ScenarioA1OrdnanceCalculator
{
    private const string Prefix = "asl.a1.ordnance.";

    private static readonly string[] AdmittedGunTypes = ["at", "inf", "art"];

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
        return undecided.Count != 0 ? Refused(OrdnanceResolution.Indeterminate, undecided) : Run(shot, reference);
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
        if (first.Disposition != OrdnanceResolution.Indeterminate || first.Reasons is not [Prefix + "roll-missing:toHit"])
        {
            return first.Reasons;
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
        return missing;
    }

    private static List<string> Outside(OrdnanceShot shot, ScenarioA1OrdnanceReference reference)
    {
        var outside = new List<string>();
        var phase = shot.Phase!;
        if ((phase, shot.FiringSide) is not (("PFPh", "phasing") or ("AFPh", "phasing") or ("DFPh", "non-phasing")))
        {
            outside.Add(Prefix + "phase-outside");
        }

        // C2.21, C2.22, C2.3, C3.33: a Gun of the firing side that can fire HE; mortars (the Area Target Type), RCL, and 360-degree
        // mounts are not reviewed.
        var gunShot = shot.Gun!;
        if (!reference.Guns.TryGetValue(gunShot.DefinitionId!, out var gun) || gun.Nationality != shot.FiringNationality || gun.NoHe || gun.Mount360
            || !AdmittedGunTypes.Contains(gun.GunType))
        {
            outside.Add(Prefix + "gun-outside");
            return outside;
        }

        if (gunShot.Malfunctioned == true)
        {
            outside.Add(Prefix + "gun-malfunctioned");
        }

        // A21.13, C2.1: its own nationality's Good Order crew mans it; C5.8's non-qualified use is not reviewed, nor a concealed crew.
        var crew = shot.Crew!;
        if (reference.Fire.Definitions.GetValueOrDefault(crew.DefinitionId!) is not { Kind: "asl:crew" } crewDefinition || crewDefinition.Nationality != gun.Nationality
            || crew.Broken == true || crew.Berserk == true)
        {
            outside.Add(Prefix + "crew-outside");
        }

        // C2.24, C5.2, A7.1: in the PFPh and DFPh the Gun fires again only on a kept Multiple ROF; in the AFPh it fires once, and
        // not after firing earlier in the Player Turn; its crew fires the Gun or its inherent FP, not both.
        var mayFire = phase == "AFPh"
            ? gunShot.FiredThisPlayerTurn != true && gunShot.ShotsThisPhase == 0
            : gunShot.ShotsThisPhase == 0 ? gunShot.FiredThisPlayerTurn != true : gunShot.RateOfFireKept == true;
        if (!mayFire || crew.FiredInherentFp == true)
        {
            outside.Add(Prefix + "gun-already-fired");
        }

        // C5.5: a shot within the Gun's own hex is not reviewed; C2.25, C3.52: never beyond its range; C2.6: depression and elevation.
        if (shot.Range < 1 || (gun.RangeMaximum is { } maximum && shot.Range > maximum) || shot.ElevationAllowed != true)
        {
            outside.Add(Prefix + "out-of-range");
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

        // C3.32: the Infantry Target Type attacks the in-LOS enemy units of the target Location; one with no unit is not reviewed.
        if (hit.Targets!.Any(item => item.Dummy != true && reference.Fire.Definitions.GetValueOrDefault(item.DefinitionId ?? string.Empty)?.Nationality == gun.Nationality))
        {
            outside.Add(Prefix + "target-outside");
        }

        if (hit.Targets!.Count == 0 || hit.TargetLocationId != shot.TargetLocationId || hit.Phase != shot.Phase || hit.FiringSide != shot.FiringSide
            || hit.Firers is { Count: > 0 } || hit.Director is not null || hit.FireKind is not null || hit.OrdnanceHit is not null)
        {
            outside.Add(Prefix + "target-outside");
        }

        if (shot.Rolls is { } rolls && (rolls.ToHit is { } dice && (dice.Count != 2 || dice.Any(die => die is < 1 or > 6))
            || rolls.Subsequent is < 1 or > 6 || rolls.CriticalSelection?.Values.Any(dr => dr is < 1 or > 6) == true))
        {
            outside.Add(Prefix + "roll-malformed");
        }

        return outside.Distinct(StringComparer.Ordinal).ToList();
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
        if (los.HindranceAttributed != true || los.HindranceDrm < 0 || (los.GrainInLos == true && hit.ScenarioMonth is not (>= 6 and <= 9)))
        {
            undecided.Add(Prefix + "hindrance-unattributed");
        }

        // C6.2: Case K applies to each concealed target alone, so a Location mixing concealed and Known units would need two To
        // Hit results; the review leaves that out.
        var concealed = hit.Targets!.Select(Concealed).Distinct().ToArray();
        if (concealed.Length > 1)
        {
            undecided.Add(Prefix + "concealment-mixed");
        }

        // A12.14: a concealed crew that fires its Gun loses "?" in the LOS of a Good Order enemy ground unit within 16 hexes; the package
        // sees only the target Location, so it decides only when one of its units is Good Order within that range, as for Infantry fire.
        if (shot.Crew!.Concealed == true && !CrewRevealed(shot))
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
    private static FireAttack HitAttack(OrdnanceShot shot, GunDefinition gun, ScenarioA1OrdnanceReference reference, bool critical,
        IReadOnlyList<FireTarget> targets, FireRolls? rolls)
    {
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
            OrdnanceHit = new FireOrdnanceHit(shot.Gun!.GunId, reference.HeFirepower(gun.Caliber), critical),
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
            // C5.1, C5.11: a non-turreted Gun adds +3 for the first hexspine and +1 for each other, doubled in woods or a building.
            var caseA = (3 + turned - 1) * (woods ? 2 : 1);
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

        var concealedTarget = hit.Targets!.All(Concealed);
        if (concealedTarget)
        {
            drm.Add(new FireModifier("case-k", 2, "C6.2"));
        }

        if (range <= 2)
        {
            drm.Add(new FireModifier("case-l", range == 1 ? -2 : -1, "C6.3"));
        }

        // C6.51, C6.57: the Acquisition applies only to Known units, and a shot at a concealed target loses it.
        if (shot.Acquisition is int acquired && acquired < 0 && !concealedTarget)
        {
            drm.Add(new FireModifier("case-n", acquired, "C6.5"));
        }

        var tem = ScenarioA1FireReference.Tem.GetValueOrDefault(hit.TargetTerrain!);
        if (tem != 0)
        {
            drm.Add(new FireModifier("case-q:" + hit.TargetTerrain, tem, "C6.8"));
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

        // C2.28: an Original To Hit DR at or above the B# malfunctions the Gun.
        var malfunctioned = original >= gun.Breakdown;

        // C2.24: an Original colored dr at most the ROF keeps the Multiple ROF; C2.5: a non-vehicular NT Gun's ROF is one lower after a
        // CA change; C5.4: a pinned crew forfeits it; C5.2: none in the AFPh.
        var rof = gun.RateOfFire ?? 0;
        if (shot.HexspinesToTurn > 0)
        {
            rof--;
        }

        if (shot.Crew.Pinned == true || shot.Phase == "AFPh")
        {
            rof = 0;
        }

        var kept = !malfunctioned && rof > 0 && dice[0] <= rof;
        var counter = kept ? null : shot.Phase is "PFPh" or "AFPh" ? "prep-fire" : "final-fire";

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
        var gunEffect = new OrdnanceGunEffect(gun.Breakdown, malfunctioned, Math.Max(rof, 0), kept, counter, acquisition, acquires ? shot.TargetLocationId : null);
        return new OrdnanceResolution(OrdnanceResolution.Resolved, [], toHit, gunEffect)
        {
            CrewConcealmentLost = shot.Crew.Concealed == true ? true : null,
            Hit = normal,
            CriticalHit = criticalHit,
            CriticalTarget = criticalTarget,
        };
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
