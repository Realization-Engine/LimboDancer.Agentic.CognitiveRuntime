"""Pass 50, task 50.3: the MA while moving. The Rules part: Cases C, C1, C2, and C4, the Bounding Fire counter, the AFPh after it."""
import sys

sys.path.insert(0, "E:/Archive/GitHub/dlandi/pass32-tools/pass50")
from edits import Edits

e = Edits()
RULES = "LimboDancer.Domains.Asl.Rules/"
C = RULES + "ScenarioA1OrdnanceCalculator.cs"
A = RULES + "ScenarioA1ArmorCalculator.cs"

e.edit(RULES + "ScenarioA1ArmorModels.cs", """    /// <summary>
    /// The vehicle's other weapons against this shot (D3.5, D3.51, pp. 200 and 201; pass 50).""", """    /// <summary>
    /// The MP the vehicle has spent in a continuous LOS to its target in this MPh, rounded up (C5.31 to C5.33, p. 172; pass 50): at most two is
    /// Case C2, up to three Case C1, more Case C; 99 for a LOS held since before the MPh. A map read, recorded with a Bounding First Fire shot.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? MpInLos
    {
        get; init;
    }

    /// <summary>
    /// Whether the vehicle fired its MA in this Player Turn's MPh as Bounding First Fire (D3.32, p. 200; pass 50): its AFPh shot on the ROF it kept
    /// uses Case C though it entered no new hex. Null is false.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public bool? BoundingFirer
    {
        get; init;
    }

    /// <summary>
    /// The vehicle's other weapons against this shot (D3.5, D3.51, pp. 200 and 201; pass 50).""")

# The phase and the side.
e.edit(C, """        if (shot.FireKind is null
            ? (phase, shot.FiringSide) is not (("PFPh", "phasing") or ("AFPh", "phasing") or ("DFPh", "non-phasing"))
            : shot.FireKind != "first-fire" || (phase, shot.FiringSide) is not ("MPh", "non-phasing"))
        {
            outside.Add(Prefix + "phase-outside");
        }
""", """        // D3.3, C5.3 (pp. 172, 199; pass 50): a vehicle's MA fires in its own MPh as Bounding First Fire.
        var bounding = shot.FireKind == ScenarioA1FireCalculator.BoundingFirstFire;
        if (shot.FireKind is null
            ? (phase, shot.FiringSide) is not (("PFPh", "phasing") or ("AFPh", "phasing") or ("DFPh", "non-phasing"))
            : bounding ? shot.Vehicle is null || (phase, shot.FiringSide) is not ("MPh", "phasing")
            : shot.FireKind != "first-fire" || (phase, shot.FiringSide) is not ("MPh", "non-phasing"))
        {
            outside.Add(Prefix + "phase-outside");
        }
""")
e.edit(C, """        // D5.34, C7.42, D5.341 (ruling R7.10): a Stunned, Shocked, or Recalled AFV does not fire; D2.42, C5.35: a firer in Motion needs Case C4,
        // and C5.3: one that entered a new hex in its MPh needs Case C in the AFPh, neither built; D1.321, D1.322: an RST or 1MT MA fires only BU.
        if (shot.Vehicle is { } firing && (firing.Stunned == true || firing.Shocked == true || firing.Recalled == true || firing.InMotion == true
            || (phase == "AFPh" && firing.Moved == true)
            || (gun.MaType is "rst" or "1mt" && firing.ButtonedUp != true)))
        {
            outside.Add(Prefix + "vehicle-fire-outside");
        }
""", """        // D5.34, C7.42, D5.341 (ruling R7.10): a Stunned, Shocked, or Recalled AFV does not fire; D1.321, D1.322: an RST or 1MT MA fires only BU.
        // D2.42, C5.35 (pass 50): a firer in Motion fires with Case C4 in its own MPh and AFPh; D2.4: never in the PFPh; the DEFENDER's vehicle in
        // Motion is not built. C5.3 (pass 50): one that entered a new hex in its MPh fires in the AFPh with Case C.
        if (shot.Vehicle is { } firing && (firing.Stunned == true || firing.Shocked == true || firing.Recalled == true
            || (firing.InMotion == true && !bounding && !(phase == "AFPh" && shot.FiringSide == "phasing"))
            || (gun.MaType is "rst" or "1mt" && firing.ButtonedUp != true)))
        {
            outside.Add(Prefix + "vehicle-fire-outside");
        }
""")
e.edit(C, """        if (shot.HexspinesToTurn > 0 && (crew.Pinned == true || (shot.FirerInWoodsOrBuilding == true && gunShot.ShotsThisPhase > 0)))
""", """        // C5.13 (p. 172; pass 50): "a Bounding First Firer must always fire within its current CA".
        if (shot.HexspinesToTurn > 0 && (bounding || crew.Pinned == true || (shot.FirerInWoodsOrBuilding == true && gunShot.ShotsThisPhase > 0)))
""")

# Case C in place of Case B, in both runs.
e.edit(C, """        if (AfphFire(shot))
        {
            drm.Add(new FireModifier("case-b", woods ? 3 : 2, "C5.2"));
        }

        if (shot.Crew!.Pinned == true)
""", """        if (CaseC(shot, gun) is { } caseC)
        {
            drm.Add(caseC);
        }
        else if (AfphFire(shot))
        {
            drm.Add(new FireModifier("case-b", woods ? 3 : 2, "C5.2"));
        }

        if (shot.Crew!.Pinned == true)
""")
e.edit(A, """        else if (ScenarioA1OrdnanceCalculator.AfphFire(shot))
        {
            drm.Add(new FireModifier("case-b", woods ? 3 : 2, "C5.2"));
        }
""", """        else if (ScenarioA1OrdnanceCalculator.CaseC(shot, gun) is { } caseC)
        {
            drm.Add(caseC);
        }
        else if (ScenarioA1OrdnanceCalculator.AfphFire(shot))
        {
            drm.Add(new FireModifier("case-b", woods ? 3 : 2, "C5.2"));
        }
""")

# Case C4: the lower dr doubled, in both runs.
e.edit(C, """        var final = original + total;
        var lowest = 2 + total;

        // C3.6: when no Final DR can hit, an Original 2 still may, on a subsequent dr of 1 (a Critical Hit) to 3.
""", """        // C5.35 (p. 172; pass 50): a Motion firer with no Gyrostabilizer doubles the lower dr of its TH DR; the lowest Final DR is then one more.
        var motion = MotionFirer(shot) ? Math.Min(dice[0], dice[1]) : 0;
        if (motion > 0)
        {
            drm.Add(new FireModifier("case-c4:lower-dr", motion, "C5.35"));
        }

        var final = original + total + motion;
        var lowest = 2 + total + (motion > 0 ? 1 : 0);

        // C3.6: when no Final DR can hit, an Original 2 still may, on a subsequent dr of 1 (a Critical Hit) to 3.
""")
e.edit(A, """        var total = (int)drm.Sum(item => item.Value);
        var original = dice[0] + dice[1];
        var final = original + total;
        var lowest = 2 + total;
        var improbable = lowest > modified;
""", """        var total = (int)drm.Sum(item => item.Value);
        var original = dice[0] + dice[1];

        // C5.35 (p. 172; pass 50): a Motion firer with no Gyrostabilizer doubles the lower dr of its TH DR; the lowest Final DR is then one more.
        var motion = ScenarioA1OrdnanceCalculator.MotionFirer(shot) ? Math.Min(dice[0], dice[1]) : 0;
        if (motion > 0)
        {
            drm.Add(new FireModifier("case-c4:lower-dr", motion, "C5.35"));
        }

        var final = original + total + motion;
        var lowest = 2 + total + (motion > 0 ? 1 : 0);
        var improbable = lowest > modified;
""")

# The counter, and the helpers.
e.edit(C, """        var kept = !malfunctioned && rof > 0 && colored <= rof;
        return (kept, kept ? null : shot.Phase switch
        {
            "PFPh" or "AFPh" => "prep-fire",
            "MPh" => "first-fire",
            _ => "final-fire",
        });
    }
""", """        var kept = !malfunctioned && rof > 0 && colored <= rof;
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
    /// Bounding First Fire kept uses Case C; the subcases "do not apply in the AFPh". Case B is part of it and is not added again.
    /// </summary>
    internal static FireModifier? CaseC(OrdnanceShot shot, GunDefinition gun)
    {
        if (shot.Vehicle is not { } vehicle)
        {
            return null;
        }

        var bounding = shot.FireKind == ScenarioA1FireCalculator.BoundingFirstFire;
        if (!bounding && !(shot.Phase == "AFPh" && shot.FiringSide == "phasing" && (vehicle.Moved == true || vehicle.InMotion == true || vehicle.BoundingFirer == true)))
        {
            return null;
        }

        var mount = 2 + (gun.MaType is "t" or "st" or "rst" or "1mt" ? 2 : 3);
        return !bounding || vehicle.MpInLos is null or > 3 ? new FireModifier("case-c", mount, "C5.3")
            : vehicle.MpInLos > 2 ? new FireModifier("case-c1", mount + 1, "C5.31")
            : new FireModifier("case-c2", mount + 2, "C5.32");
    }

    /// <summary>
    /// Whether the shot is a Motion firer's (C5.35, D2.42, pp. 172 and 198; pass 50): a vehicle in Motion, or moving and not stopped, that fires in
    /// its own MPh or AFPh. It uses Case C, C1, or C2 as applicable and "must double the lower dr of its TH DR".
    /// </summary>
    internal static bool MotionFirer(OrdnanceShot shot) =>
        shot.Vehicle?.InMotion == true && (shot.FireKind == ScenarioA1FireCalculator.BoundingFirstFire || (shot.Phase == "AFPh" && shot.FiringSide == "phasing"));
""")

e.edit(RULES + "ScenarioA1OrdnanceEventRules.cs", """        "MPh" => fireCounter is "first-fire" or "intensive-fire" ? UnitCondition.FirstFire : null,
""", """        // D3.3 (pass 50): a Bounding Fire counter once a Bounding First Firer's ROF is spent.
        "MPh" => fireCounter is "first-fire" or "intensive-fire" ? UnitCondition.FirstFire : fireCounter == "bounding-fire" ? UnitCondition.BoundingFire : null,
""")

E = RULES + "ScenarioA1OrdnanceEligibility.cs"
e.edit(E, """    public static OrdnanceVehicleWeapons VehicleWeapons(IReadOnlyList<VehicleWeaponShotFacts> shots, string firePhase, string? maType)
    {
        ArgumentNullException.ThrowIfNull(shots);
        return new OrdnanceVehicleWeapons(shots.Any(item => item.FirePhase != firePhase), shots.Count > 0 && shots.All(item => item.Weapon != "ma"),
""", """    public static OrdnanceVehicleWeapons VehicleWeapons(IReadOnlyList<VehicleWeaponShotFacts> shots, string firePhase, string? maType, bool boundingFireMarked = false)
    {
        ArgumentNullException.ThrowIfNull(shots);

        // D3.32 (p. 200; pass 50): "if the vehicle did not exhaust its Multiple ROF during its MPh, and did not fire any other weapons ... during its
        // MPh, it may fire that Multiple ROF weapon (only) again once during its AFPh". A Bounding Fire counter says the ROF is spent (D3.3).
        var afterBounding = firePhase == "afph" && !boundingFireMarked && shots.Count > 0 && shots.All(item => item.Weapon == "ma" && item.FirePhase == "mph");
        return new OrdnanceVehicleWeapons(!afterBounding && shots.Any(item => item.FirePhase != firePhase), shots.Count > 0 && shots.All(item => item.Weapon != "ma"),
""")
e.edit(E, """    /// <summary>A firing vehicle's facts (D5.2, D5.34, C7.42, D5.341, D2.4; rulings R7.10, R7.11): a CT AFV is BU unless its crew is exposed.</summary>
    public static OrdnanceVehicleFirer VehicleFirer(bool crewExposed, bool motion, bool stunned, bool shocked, bool unconfirmedKill, bool recalled, bool stunRecovery,
        bool moved) =>
        new(!crewExposed, motion, stunned, shocked || unconfirmedKill, recalled)
        {
            StunRecovery = stunRecovery ? true : null,
            Moved = moved ? true : null,
        };
""", """    /// <summary>
    /// A firing vehicle's facts (D5.2, D5.34, C7.42, D5.341, D2.4; rulings R7.10, R7.11): a CT AFV is BU unless its crew is exposed. Pass 50 (D2.42,
    /// D3.32): the moving vehicle that has not stopped is a Motion firer too, and one that fired its MA in this MPh is a Bounding Firer in the AFPh.
    /// </summary>
    public static OrdnanceVehicleFirer VehicleFirer(bool crewExposed, bool motion, bool stunned, bool shocked, bool unconfirmedKill, bool recalled, bool stunRecovery,
        bool moved, bool movingUnstopped = false, bool boundingFirer = false) =>
        new(!crewExposed, motion || movingUnstopped, stunned, shocked || unconfirmedKill, recalled)
        {
            StunRecovery = stunRecovery ? true : null,
            Moved = moved ? true : null,
            BoundingFirer = boundingFirer ? true : null,
        };

    /// <summary>
    /// The kind of a shot in the MPh (D3.3, A8.1, C6.1; pass 50, ruling R8.1): the phasing side's vehicle fires its MA as Bounding First Fire when it
    /// may (at the outset of its MPh, or once the DEFENDER has passed on its last MP expenditure); the other side fires as Defensive First Fire at
    /// the moving stack while its window is open. The refusal otherwise.
    /// </summary>
    public static (string? Kind, string? Refusal) MphShotKind(bool phasing, bool vehicle, Func<bool> mayBoundingFire, bool windowOpen, Func<bool> windowAtTarget)
    {
        ArgumentNullException.ThrowIfNull(mayBoundingFire);
        ArgumentNullException.ThrowIfNull(windowAtTarget);
        if (phasing)
        {
            return vehicle && mayBoundingFire() ? (ScenarioA1FireCalculator.BoundingFirstFire, null)
                : (null, vehicle ? "play.ordnance-bounding: in its own MPh a vehicle's MA fires as Bounding First Fire, at the outset of its move or once the DEFENDER has passed on its last MP expenditure (D3.3)"
                    : MphWindowRefusal);
        }

        return OutsideMphWindow(false, windowOpen, windowAtTarget) ? (null, MphWindowRefusal) : ("first-fire", null);
    }

    /// <summary>
    /// C5.3 (p. 172; pass 50): "a vehicle with a Multiple ROF may not fire again until it has expended another MP (even if only a Delay MP)". The
    /// movement step of its last Bounding First Fire shot against the step now; null when it has made none.
    /// </summary>
    public static string? BoundingShotBar(string vehicleId, int? lastShotStep, int stepNow) =>
        lastShotStep is { } last && stepNow <= last
            ? $"play.ordnance-bounding-mp: {vehicleId} spends another MP, a Delay MP will do, before its MA fires again in Bounding First Fire (C5.3)"
            : null;
""")

# The shot record of the state gains the movement step of a Bounding First Fire shot.
e.edit(RULES + "ScenarioA1FireRuleModels.cs", """public sealed record VehicleWeaponShotFacts(string Weapon, string FirePhase, int Turn, bool Doubled);
""", """public sealed record VehicleWeaponShotFacts(string Weapon, string FirePhase, int Turn, bool Doubled, int? Step = null);
""")

e.apply()
