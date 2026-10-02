using System.Globalization;

namespace LimboDancer.Domains.Asl.Rules;

/// <summary>
/// The Vehicle Target Type and To Kill resolution of the Ordnance package (backlog pass 7, rulings R7.2 to R7.10): a Gun's or a tank's shot
/// at one vehicle on the C3 Vehicle row with the reviewed To Hit Cases, where the hit strikes (C3.9), the Target Facing and AF it meets
/// (D3.2, D1.6), the To Kill number of the declared ammunition (C7), and the To Kill DR's result with the crew's checks (C7.4 to C7.7, D5.5,
/// D5.6). The special ammunition's Depletion Number decides whether it was fired at all (C8.9).
/// </summary>
internal static class ScenarioA1ArmorCalculator
{
    private const string Prefix = "asl.a1.ordnance.";

    /// <summary>The C5.1 Case A To Hit DRM of a turn of the given hexspines by a Gun of this type (T: +1 each; ST, RST, 1MT: +2 then +1; NT: +3 then +1).</summary>
    public static int CaseA(string? maType, int hexspines) => hexspines <= 0 ? 0 : maType switch
    {
        "t" => hexspines,
        "st" or "rst" or "1mt" => 2 + hexspines - 1,
        _ => 3 + hexspines - 1,
    };

    /// <summary>The Depletion Number of an ammunition in a year (C8.9, C8.91, C8.3), or null when the Gun has none then.</summary>
    public static int? Depletion(GunDefinition gun, string ammunition, int? year, int? month)
    {
        ArgumentNullException.ThrowIfNull(gun);
        var letter = ammunition switch
        {
            "apcr" => "A",
            "heat" => "H",
            _ => null,
        };
        if (letter is null || year is not { } scenarioYear)
        {
            return null;
        }

        var entries = gun.SpecialAmmo.Where(item => item.StartsWith(letter, StringComparison.Ordinal) && item.Length > 1 && char.IsDigit(item[1]))
            .Select(item =>
            {
                var parts = item[1..].Split('^');
                var number = int.Parse(parts[0], CultureInfo.InvariantCulture);
                int? first = parts.Length > 1 ? 1940 + int.Parse(parts[1], CultureInfo.InvariantCulture) : null;
                return (Number: number, Year: first);
            }).ToArray();
        if (entries.Length == 0)
        {
            return null;
        }

        // C8.3: HEAT is available to the Germans from May 1942, to the U.S., Britain, and Russia from 1943.
        if (letter == "H" && entries.All(entry => entry.Year is null))
        {
            var from = gun.Nationality == "german" ? (Year: 1942, Month: 5) : (Year: 1943, Month: 1);
            return scenarioYear > from.Year || (scenarioYear == from.Year && (month ?? 0) >= from.Month) ? entries[0].Number : null;
        }

        var dated = entries.Where(entry => entry.Year is not null).OrderBy(entry => entry.Year).ToArray();
        return dated.LastOrDefault(entry => entry.Year <= scenarioYear) is { Year: not null } found ? found.Number : null;
    }

    /// <summary>The facts a Vehicle Target Type shot needs beyond an Infantry Target Type shot's.</summary>
    public static void Missing(OrdnanceShot shot, Action<object?, string> need)
    {
        var target = shot.VehicleTarget!;
        need(target.VehicleId, "vehicleTarget.vehicleId");
        need(target.DefinitionId, "vehicleTarget.definitionId");
        need(target.HullFacing, "vehicleTarget.hullFacing");
        need(target.Moving, "vehicleTarget.moving");
        need(target.NonStopped, "vehicleTarget.nonStopped");
        need(target.Concealed, "vehicleTarget.concealed");
        need(target.CrewImpaired, "vehicleTarget.crewImpaired");
        need(target.CrewMayTakeTc, "vehicleTarget.crewMayTakeTc");
        need(shot.Ammunition, "ammunition");
    }

    /// <summary>The reasons a Vehicle Target Type shot is outside the review.</summary>
    public static void Outside(OrdnanceShot shot, GunDefinition gun, ScenarioA1OrdnanceReference reference, List<string> outside)
    {
        var target = shot.VehicleTarget!;
        if (!reference.Armor.Vehicles.TryGetValue(target.DefinitionId!, out var armor) || armor.Nationality == gun.Nationality
            || target.HullFacing is not ("front" or "side" or "rear") || (armor.Turreted && target.TurretFacing is not ("front" or "side" or "rear"))
            || shot.Hit!.Targets is { Count: > 0 })
        {
            outside.Add(Prefix + "vehicle-target-outside");
            return;
        }

        var ammunition = shot.Ammunition!;
        var armored = !armor.Unarmored && armor.FrontAf is not null;
        var latw = gun.GunType == "latw";

        // C13.3, C13.34, C8.31 (ruling R9.8): a PF fires HEAT only at an AFV; its other targets need HE Equivalency, not built. Pass 9b: a PSK fires
        // HEAT and an ATR AP at any vehicle (C13.43, C13.2).
        if (latw && (gun.LatwType switch
        {
            "pf" => ammunition != "heat" || !armored,
            "psk" => ammunition != "heat",
            _ => ammunition != "ap"
        }))
        {
            outside.Add(Prefix + "ammunition-outside");
            return;
        }

        var tk = armored ? reference.Armor.BasicTk(ammunition, gun) : reference.Armor.UnarmoredTk(ammunition, gun);
        if (ammunition is not ("ap" or "apcr" or "heat" or "he") || (ammunition == "ap" && gun.NoAp) || (ammunition == "he" && gun.NoHe) || tk is null)
        {
            outside.Add(Prefix + "ammunition-outside");
        }

        if (!latw && ammunition is "apcr" or "heat" && Depletion(gun, ammunition, shot.ScenarioYear, shot.Hit!.ScenarioMonth) is null)
        {
            outside.Add(Prefix + "ammunition-unavailable");
        }

        if (shot.Gun!.Depleted?.Contains(ammunition, StringComparer.Ordinal) == true)
        {
            outside.Add(Prefix + "ammunition-depleted");
        }

        // C7.24: AP and APCR need a Case D value at the range (NA is out of reach).
        if (armored && tk is not null && reference.Armor.CaseD(ammunition, gun, shot.Range!.Value) is null)
        {
            outside.Add(Prefix + "out-of-range");
        }
    }

    public static OrdnanceResolution Run(OrdnanceShot shot, GunDefinition gun, ScenarioA1OrdnanceReference reference)
    {
        var rolls = shot.Rolls!;
        var hit = shot.Hit!;
        var target = shot.VehicleTarget!;
        var armor = reference.Armor.Vehicles[target.DefinitionId!];
        var ammunition = shot.Ammunition!;
        var range = shot.Range!.Value;
        var column = reference.Column(range);
        var color = ScenarioA1OrdnanceReference.Color(shot.FiringNationality!);

        // C3.31, C4: the Vehicle row's Basic TH#, the Gun's modifications, and APCR's (C4.3). C13.33 (ruling R9.8): a PF's Basic TH# is 10, less
        // two for each hex of range, with no C4 modification.
        var latw = gun.GunType == "latw";
        var pf = gun.LatwType == "pf";

        // Pass 9b: a PSK reads its own To Hit Table (C13.48); an ATR the black Vehicle row whatever its nationality (C13.2).
        if (gun.LatwType == "atr")
        {
            color = "black";
        }

        var basic = pf ? 10 : gun.ToHitTable.Count > 0 ? gun.ToHitTable[range - 1] : reference.Armor.BasicToHit(color, column);
        var modifications = pf ? [new FireModifier("pf-range", -2 * range, "C13.33")] : gun.ToHitTable.Count > 0 ? [] : reference.Modifications(gun, range).ToList();
        if (ammunition == "apcr" && reference.Armor.ApcrToHit(column) is var apcr && apcr != 0)
        {
            modifications.Add(new FireModifier("apcr", apcr, "C4.3"));
        }

        var modified = basic + (int)modifications.Sum(item => item.Value);
        var drm = new List<FireModifier>();
        var woods = shot.FirerInWoodsOrBuilding == true;
        if (shot.HexspinesToTurn is int turned && turned > 0)
        {
            drm.Add(new FireModifier("case-a:" + turned.ToString(CultureInfo.InvariantCulture), CaseA(gun.MaType, turned) * (woods ? 2 : 1), "C5.1"));
        }

        if (latw)
        {
            // C13.1, C13.8 (ruling R9.8): Case C3 instead of Case B, for the AFPh and for the Backblast of a shot from a ground-level building.
            if (shot.Phase == "AFPh")
            {
                drm.Add(new FireModifier("case-c3:afph", 2, "C13.1"));
            }

            if (shot.Panzerfaust?.FromBuilding == true && gun.LatwType is "pf" or "psk")
            {
                drm.Add(new FireModifier("case-c3:backblast", 2, "C13.8"));
            }
        }
        else if (shot.Phase == "AFPh")
        {
            drm.Add(new FireModifier("case-b", woods ? 3 : 2, "C5.2"));
        }

        if (shot.Crew!.Pinned == true)
        {
            drm.Add(new FireModifier("case-d", 2, "C5.4"));
        }

        if (shot.Crew.Cx == true)
        {
            drm.Add(new FireModifier("cx", 1, "A4.51"));
        }

        if (shot.Vehicle?.ButtonedUp == true)
        {
            drm.Add(new FireModifier("case-i", 1, "C5.9"));
        }

        // D5.34: an AFV under a "+1" counter after a Stun adds one to its To Hit DR.
        if (shot.Vehicle?.StunRecovery == true)
        {
            drm.Add(new FireModifier("stun", 1, "D5.34"));
        }

        drm.AddRange(ScenarioA1OrdnanceCalculator.PassEightFirerDrm(shot));
        drm.AddRange(ScenarioA1OrdnanceCalculator.Leadership(shot, reference));

        // C6.11, C6.12 (ruling R8.1): Defensive First Fire at a vehicle that has spent at most one MP in the firer's continuous LOS takes Case J2,
        // at most three Case J1, else Case J; a moving target in a fire phase takes Case J.
        if (shot.FireKind is not null && shot.Movement?.MpInLos is { } seen)
        {
            var mpInLos = seen - (shot.Movement.MpClaimed ?? 0);
            drm.Add(mpInLos <= 1 ? new FireModifier("case-j2", 4, "C6.12") : mpInLos <= 3 ? new FireModifier("case-j1", 3, "C6.11") : new FireModifier("case-j", 2, "C6.1"));
        }
        else if (target.Moving == true)
        {
            drm.Add(new FireModifier("case-j", 2, "C6.1"));
        }

        if (target.Concealed == true)
        {
            drm.Add(new FireModifier("case-k", 2, "C6.2"));
        }

        // C6.3: no Point Blank Range against a Non-Stopped or Motion target, nor by a Motion firer. Cases L, M, and N are not LATW DRM.
        if (!latw && range <= 2 && target.NonStopped != true && shot.Vehicle?.InMotion != true)
        {
            drm.Add(new FireModifier("case-l", range == 1 ? -2 : -1, "C6.3"));
        }

        if (!latw && shot.BoreSighted == true)
        {
            drm.Add(new FireModifier("case-m", -2, "C6.4"));
        }
        else if (!latw && shot.Acquisition is int acquired && acquired < 0 && target.Concealed != true)
        {
            drm.Add(new FireModifier("case-n", acquired, "C6.5"));
        }

        // C6.7, D1.7: the target's size.
        var size = armor.TargetSize switch
        {
            "very-small" => 2,
            "small" => 1,
            "large" => -1,
            "very-large" => -2,
            _ => 0,
        };
        if (size != 0)
        {
            drm.Add(new FireModifier("case-p:" + armor.TargetSize, size, "C6.7"));
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
        var improbable = lowest > modified;
        bool isHit, critical;
        int? subsequent = null;
        if (improbable)
        {
            // C3.6: an Original 2 still hits on a subsequent dr of 1 (a Critical Hit) to 3.
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
            // C3.7: on the Vehicle Target Type an Original 2 is a Critical Hit, unless only the lowest Final DR hits, when it takes a
            // subsequent dr of 1 (a 2 to 6 is a normal hull hit).
            isHit = final <= modified;
            critical = false;
            if (isHit && original == 2)
            {
                if (lowest == modified)
                {
                    if (rolls.Subsequent is not { } dr)
                    {
                        return Refused(OrdnanceResolution.Indeterminate, [Prefix + "roll-missing:subsequent"]);
                    }

                    subsequent = dr;
                    critical = dr == 1;
                }
                else
                {
                    critical = true;
                }
            }
        }

        var toHit = new OrdnanceToHit(color, basic, modifications, modified, [.. dice], original, drm, final, improbable, subsequent, isHit, critical);
        var malfunctioned = original >= ScenarioA1OrdnanceCalculator.Breakdown(shot, gun, reference);

        // C13.36 (ruling R9.8): a PF never malfunctions, but an Original 12 misses and gives its firer Casualty Reduction; an 11 or 12 for
        // Inexperienced Infantry (A19.32; referee, pass 9).
        var inexperienced = reference.Fire.Definitions.GetValueOrDefault(shot.Crew!.DefinitionId ?? string.Empty)?.Class is "green" or "conscript";
        var casualty = pf && original >= (inexperienced ? 11 : 12);
        if (casualty)
        {
            isHit = critical = false;
            toHit = toHit with
            {
                Hit = false,
                CriticalHit = false
            };
        }

        // C8.9: special ammunition is used below its Depletion Number, used and run out at it, and was never there above it, when the Gun
        // did not fire at all unless it malfunctioned.
        string? use = null;
        if (!latw && ammunition is "apcr" or "heat")
        {
            var depletion = Depletion(gun, ammunition, shot.ScenarioYear, hit.ScenarioMonth)!.Value;
            use = original < depletion ? "used" : original == depletion ? "depleted" : "none";
            if (use == "none" && !malfunctioned)
            {
                var unused = new OrdnanceGunEffect(gun.Breakdown, false, gun.RateOfFire ?? 0, true, null, shot.Acquisition!.Value, null);
                return new OrdnanceResolution(OrdnanceResolution.Resolved, [], toHit with { Hit = false, CriticalHit = false }, unused) { AmmunitionUse = use };
            }
        }

        var rof = gun.RateOfFire ?? 0;
        if (shot.HexspinesToTurn > 0 && shot.Vehicle is null)
        {
            rof--;
        }

        if (shot.Crew.Pinned == true || shot.Phase == "AFPh" || latw)
        {
            rof = 0;
        }

        var (kept, counter) = ScenarioA1OrdnanceCalculator.Counter(shot, malfunctioned, rof, dice[0]);
        OrdnanceKill? kill = null;
        if (isHit && use != "none")
        {
            var (resolved, pending) = Kill(shot, gun, armor, reference, dice, critical, ammunition, range, improbable ? subsequent : null);
            if (pending is not null)
            {
                return pending;
            }

            kill = resolved;
        }

        // C6.5: the shot acquires the target's Location, one step more if already acquired, unless the Gun malfunctions; a concealed vehicle
        // is acquired only when the shot costs it its "?" (the game decides that, R6.7).
        // C6.5 (the chart's G): a non-mortar SW acquires nothing.
        var acquires = !malfunctioned && target.Concealed != true && !latw;
        var acquisition = !acquires ? 0 : Math.Max(shot.Acquisition!.Value - 1, -2);
        var gunEffect = new OrdnanceGunEffect(ScenarioA1OrdnanceCalculator.Breakdown(shot, gun, reference), malfunctioned, Math.Max(rof, 0), kept, counter, acquisition,
            acquires ? shot.TargetLocationId : null);
        return new OrdnanceResolution(OrdnanceResolution.Resolved, [], toHit, gunEffect)
        {
            CrewConcealmentLost = shot.Crew.Concealed == true && shot.CrewSeen != false ? true : null,
            Kill = kill,
            AmmunitionUse = use,
            FirerEffect = casualty ? OrdnancePanzerfaustCheck.CasualtyReduction : null,
        };
    }

    private static (OrdnanceKill? Kill, OrdnanceResolution? Pending) Kill(OrdnanceShot shot, GunDefinition gun, ArmorDefinition armor, ScenarioA1OrdnanceReference reference,
        IReadOnlyList<int> dice, bool critical, string ammunition, int range, int? improbable)
    {
        var target = shot.VehicleTarget!;
        var rolls = shot.Rolls!;

        // C3.9: the colored dr below the white dr strikes a turret; otherwise the hull. C3.6: an improbable hit's subsequent dr decides
        // instead: 2 strikes the turret, 3 the hull (1, the Critical Hit, strikes as its Original 2, the hull). The turret's Target Facing
        // follows its TCA (D3.12).
        var location = armor.Turreted && (improbable is { } subsequent ? subsequent == 2 : dice[0] < dice[1]) ? "turret" : "hull";
        var facing = location == "turret" ? target.TurretFacing ?? target.HullFacing! : target.HullFacing!;
        var armored = !armor.Unarmored && armor.FrontAf is not null;
        int finalTk;
        int? basicTk = null, modifiedTk = null, af = null;
        var modifiers = new List<FireModifier>();
        if (armored)
        {
            // C7.11, C7.2: the Basic TK#, doubled by a Critical Hit (Case C), +1 against the rear (Case A), the range change (Case D), less the AF.
            basicTk = reference.Armor.BasicTk(ammunition, gun)!.Value;
            var value = basicTk.Value;
            if (critical)
            {
                modifiers.Add(new FireModifier("case-c", value, "C7.23"));
                value *= 2;
            }

            if (facing == "rear")
            {
                modifiers.Add(new FireModifier("case-a", 1, "C7.21"));
                value++;
            }

            if (reference.Armor.CaseD(ammunition, gun, range) is int caseD && caseD != 0)
            {
                modifiers.Add(new FireModifier("case-d", caseD, "C7.24"));
                value += caseD;
            }

            modifiedTk = value;
            af = ScenarioA1ArmorReference.ArmorFactor(armor, location, facing)!.Value;
            finalTk = value - af.Value;
        }
        else
        {
            // C7.311, C7.331, C7.342: the unarmored Final TK#, doubled by a Critical Hit.
            finalTk = reference.Armor.UnarmoredTk(ammunition, gun)!.Value * (critical ? 2 : 1);
        }

        if (rolls.ToKill is not { } kill)
        {
            return (null, Refused(OrdnanceResolution.Indeterminate, [Prefix + "roll-missing:toKill"]));
        }

        var dr = kill[0] + kill[1];
        OrdnanceKill Made(string result) => new(ammunition, location, facing, basicTk, modifiers, modifiedTk, af, finalTk, [.. kill], dr, result);
        var plusOne = target.StunRecovery == true ? 1 : 0;
        IReadOnlyList<FireModifier> checkDrm = plusOne == 0 ? [] : [new FireModifier("stun", 1, "D5.34")];

        // C7.35: an Original 12 is a dud.
        if (dr == 12)
        {
            return (Made(OrdnanceKill.Dud), null);
        }

        // C7.7, C7.6: at most half the Final TK# burns, less eliminates, equal immobilizes (hull) or Shocks (turret); one more Shocks or
        // immobilizes an HE hit and gives any other ammunition a possible Shock. The unarmored table has no Shock.
        var result = dr * 2 <= finalTk ? OrdnanceKill.Burn
            : dr < finalTk ? OrdnanceKill.Eliminated
            : dr == finalTk ? (armored && location == "turret" ? OrdnanceKill.Shock : OrdnanceKill.Immobilized)
            : armored && dr == finalTk + 1 ? (ammunition == "he" ? (location == "turret" ? OrdnanceKill.Shock : OrdnanceKill.Immobilized) : OrdnanceKill.PossibleShock)
            : OrdnanceKill.None;
        var made = Made(result);

        // D5.1: an AFV's crew has Elite morale, an unarmored vehicle's the 1st Line morale of its nationality.
        var morale = reference.Fire.Definitions.Values.Where(item => item.IsMmc && item.Nationality == armor.Nationality && item.Class == (armored ? "elite" : "1st-line"))
            .Max(item => item.Morale);
        if (morale is null && (result == OrdnanceKill.PossibleShock || (result == OrdnanceKill.Immobilized && target.CrewMayTakeTc == true)))
        {
            return (null, Refused(OrdnanceResolution.Indeterminate, [Prefix + "definition-incomplete:crew-morale:" + armor.Nationality]));
        }

        if (result == OrdnanceKill.Shock)
        {
            made = made with
            {
                Shocked = true
            };
        }
        else if (result == OrdnanceKill.PossibleShock && morale is { } shockMorale)
        {
            // C7.41: the crew takes a NTC; failure Shocks the AFV.
            if (rolls.ShockCheck is not { } check)
            {
                return (null, Refused(OrdnanceResolution.Indeterminate, [Prefix + "roll-missing:shockCheck"]));
            }

            var total = check[0] + check[1];
            var passed = total + plusOne <= shockMorale;
            made = made with
            {
                ShockCheck = new FireCheck("NTC", [.. check], total, checkDrm, total + plusOne, shockMorale, passed, passed ? "none" : OrdnanceKill.Shock),
                Shocked = passed ? null : true,
            };
        }
        else if (result == OrdnanceKill.Immobilized && target.CrewMayTakeTc == true && morale is { } tcMorale)
        {
            // D5.5: an immobilized vehicle's crew takes a TC at once; failure Abandons it.
            if (rolls.CrewCheck is not { } check)
            {
                return (null, Refused(OrdnanceResolution.Indeterminate, [Prefix + "roll-missing:crewCheck"]));
            }

            var total = check[0] + check[1];
            var passed = total + plusOne <= tcMorale;
            made = made with
            {
                CrewCheck = new FireCheck("TC", [.. check], total, checkDrm, total + plusOne, tcMorale, passed, passed ? "none" : "abandoned"),
                Abandoned = passed ? null : true,
            };
        }
        else if (result == OrdnanceKill.Eliminated && armor.CrewSurvival is { } cs && !armor.CrewSurvivalPassengersOnly && target.Abandoned != true)
        {
            // D5.6: a Final DR at most the CS#, +1 if the crew was Stunned, Shocked, Recalled, or under a "+1" counter, places a crew beneath the
            // wreck.
            if (rolls.CrewSurvival is not { } survival)
            {
                return (null, Refused(OrdnanceResolution.Indeterminate, [Prefix + "roll-missing:crewSurvival"]));
            }

            var drmValue = target.CrewImpaired == true || target.StunRecovery == true ? 1 : 0;
            var finalDr = survival[0] + survival[1] + drmValue;
            made = made with
            {
                CrewSurvival = new OrdnanceCrewSurvival([.. survival], drmValue, finalDr, cs, finalDr <= cs)
            };
        }

        return (made, null);
    }

    private static OrdnanceResolution Refused(string disposition, IReadOnlyList<string> reasons) => new(disposition, reasons, null, null);
}
