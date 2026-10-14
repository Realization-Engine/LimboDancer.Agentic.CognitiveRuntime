"""Pass 50, tasks 50.1 and 50.2: a tank's MA beside its other weapons (D3.5, D3.51)."""
import sys

sys.path.insert(0, "E:/Archive/GitHub/dlandi/pass32-tools/pass50")
from edits import Edits

e = Edits()
RULES = "LimboDancer.Domains.Asl.Rules/"
PLAY = "LimboDancer.Domains.Asl.Play/"

e.edit(RULES + "ScenarioA1ArmorModels.cs", """    public bool? Moved
    {
        get; init;
    }
}
""", """    public bool? Moved
    {
        get; init;
    }

    /// <summary>
    /// The vehicle's other weapons against this shot (D3.5, D3.51, pp. 200 and 201; pass 50). Null in a record made before pass 50, which then
    /// resolves as it was recorded.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public OrdnanceVehicleWeapons? Weapons
    {
        get; init;
    }
}

/// <summary>
/// A tank's MA beside its other weapons (D3.5, D3.51, pp. 200 and 201; pass 50): whether a weapon of the vehicle fired in another fire phase of
/// this Player Turn, so that the MA has forfeited its shot; whether the vehicle's fire counter was placed by its other weapons alone, in this
/// fire phase, so that the MA has not fired; and the Case A DRM the first turret-mounted weapon to fire in this fire phase paid, which the MA's
/// first shot within the current TCA pays too.
/// </summary>
public sealed record OrdnanceVehicleWeapons(bool OtherPhase, bool MarkedByOthers, int? MaintainedCaseA);
""")

e.edit(RULES + "ScenarioA1OrdnanceEligibility.cs", """    /// <summary>A firing vehicle's facts (D5.2, D5.34, C7.42, D5.341, D2.4; rulings R7.10, R7.11): a CT AFV is BU unless its crew is exposed.</summary>
""", """    /// <summary>
    /// A tank's MA beside its other weapons, from the vehicle's shots of this Player Turn (D3.5, D3.51; pass 50): a shot in another fire phase
    /// forfeits the MA's; a fire counter is the other weapons' alone when the MA has no shot yet; and the MA's first shot of the fire phase pays
    /// the Case A the CMG paid before it.
    /// </summary>
    public static OrdnanceVehicleWeapons VehicleWeapons(IReadOnlyList<VehicleWeaponShotFacts> shots, string firePhase, string? maType)
    {
        ArgumentNullException.ThrowIfNull(shots);
        return new OrdnanceVehicleWeapons(shots.Any(item => item.FirePhase != firePhase), shots.Count > 0 && shots.All(item => item.Weapon != "ma"),
            shots.Any(item => item.Weapon == "ma" && item.FirePhase == firePhase) ? null
                : ScenarioA1FireEligibility.MaintainedCaseA(shots.Where(item => item.Weapon != "ma"), firePhase, maType));
    }

    /// <summary>A firing vehicle's facts (D5.2, D5.34, C7.42, D5.341, D2.4; rulings R7.10, R7.11): a CT AFV is BU unless its crew is exposed.</summary>
""")

C = RULES + "ScenarioA1OrdnanceCalculator.cs"
e.edit(C, """        var mayFire = pf
            ? gunShot.FiredThisPlayerTurn != true && gunShot.ShotsThisPhase < (reference.Fire.Definitions.GetValueOrDefault(crew.DefinitionId!)?.Kind == "asl:squad" ? 2 : 1)
            : shot.IntensiveFire == true
            ? shot.Vehicle is null && phase != "AFPh" && gunShot.IntensiveFired != true && gunShot.FinalFire != true && crew.Pinned != true
                && (gunShot.FiredThisPlayerTurn == true || gunShot.FirstFire == true) && gunShot.RateOfFireKept != true
            : gunShot.IntensiveFired != true && gunShot.FirstFire != true && (phase == "AFPh" && !OpportunityFire(shot)
                ? gunShot.FiredThisPlayerTurn != true && gunShot.ShotsThisPhase == 0
                : gunShot.ShotsThisPhase == 0 ? gunShot.FiredThisPlayerTurn != true : gunShot.RateOfFireKept == true);
        if (!mayFire || crew.FiredInherentFp == true)
""", """        // D3.5 (p. 201; pass 50): a vehicle's fire counter placed by its BMG or CMG alone, in this fire phase, is not the MA's ("it makes no
        // difference which weapon fires first"); a weapon fired in another fire phase forfeits the MA's shot.
        var othersOnly = shot.Vehicle?.Weapons?.MarkedByOthers == true;
        var firedBefore = !othersOnly && gunShot.FiredThisPlayerTurn == true;
        var firstFired = !othersOnly && gunShot.FirstFire == true;
        var mayFire = pf
            ? gunShot.FiredThisPlayerTurn != true && gunShot.ShotsThisPhase < (reference.Fire.Definitions.GetValueOrDefault(crew.DefinitionId!)?.Kind == "asl:squad" ? 2 : 1)
            : shot.IntensiveFire == true
            ? shot.Vehicle is null && phase != "AFPh" && gunShot.IntensiveFired != true && gunShot.FinalFire != true && crew.Pinned != true
                && (gunShot.FiredThisPlayerTurn == true || gunShot.FirstFire == true) && gunShot.RateOfFireKept != true
            : gunShot.IntensiveFired != true && !firstFired && (phase == "AFPh" && !OpportunityFire(shot)
                ? !firedBefore && gunShot.ShotsThisPhase == 0
                : gunShot.ShotsThisPhase == 0 ? !firedBefore : gunShot.RateOfFireKept == true);
        if (!mayFire || crew.FiredInherentFp == true || shot.Vehicle?.Weapons?.OtherPhase == true)
""")
e.edit(C, """            drm.Add(new FireModifier("case-a:" + turned.ToString(System.Globalization.CultureInfo.InvariantCulture), caseA, "C5.1"));
        }
""", """            drm.Add(new FireModifier("case-a:" + turned.ToString(System.Globalization.CultureInfo.InvariantCulture), caseA, "C5.1"));
        }
        else if (shot.Vehicle?.Weapons?.MaintainedCaseA is > 0 and var maintained)
        {
            // D3.51 (p. 201; pass 50): the MA fired within a TCA its CMG changed in this fire phase pays what the CMG paid.
            drm.Add(new FireModifier("case-a:maintained", maintained, "D3.51"));
        }
""")
e.edit(RULES + "ScenarioA1ArmorCalculator.cs", """            drm.Add(new FireModifier("case-a:" + turned.ToString(CultureInfo.InvariantCulture), CaseA(gun.MaType, turned) * (woods ? 2 : 1), "C5.1"));
        }
""", """            drm.Add(new FireModifier("case-a:" + turned.ToString(CultureInfo.InvariantCulture), CaseA(gun.MaType, turned) * (woods ? 2 : 1), "C5.1"));
        }
        else if (shot.Vehicle?.Weapons?.MaintainedCaseA is > 0 and var maintained)
        {
            // D3.51 (p. 201; pass 50): the MA fired within a TCA its CMG changed in this fire phase pays what the CMG paid.
            drm.Add(new FireModifier("case-a:maintained", maintained, "D3.51"));
        }
""")

L = PLAY + "LiveOrdnance.cs"
e.edit(L, """                    state.MovedVehicles.Contains(firer.Id, StringComparer.Ordinal)),
            };
        }
""", """                    state.MovedVehicles.Contains(firer.Id, StringComparer.Ordinal)) with
                {
                    // D3.5, D3.51 (pass 50): the MA beside the vehicle's other weapons.
                    Weapons = ScenarioA1OrdnanceEligibility.VehicleWeapons(LiveFire.VehicleShots(state, firer.Id),
                        ScenarioA1FireEligibility.FirePhase(state.Phase, firer.Side == state.PhasingSide),
                        LiveFire.MaTypeOf(definition)),
                },
            };
        }
""")
e.edit(PLAY + "LiveFire.cs", """    /// <summary>Whether a unit is a vehicle (D1).</summary>
""", """    /// <summary>A vehicle definition's turret type (D1.3), as the catalog prints it; null when it prints none.</summary>
    internal static string? MaTypeOf(string? definition) => definition is null ? null : CatalogReference.Value.Definitions.GetValueOrDefault(definition)?.MaType;

    /// <summary>Whether a unit is a vehicle (D1).</summary>
""")
e.edit(L, """        var merged = ScenarioA1OrdnanceEligibility.WithRecordedMapFacts(expected, recorded);
        var gunAt""", """        var merged = ScenarioA1OrdnanceEligibility.WithRecordedMapFacts(expected, recorded);

        // Pass 50 (ruling R45.4's rule for a new fact): a record made before the pass says nothing of the vehicle's other weapons, and is compared without.
        if (merged.Vehicle is { Weapons: not null } withWeapons && recorded.Vehicle is { Weapons: null })
        {
            merged = merged with
            {
                Vehicle = withWeapons with { Weapons = null }
            };
        }

        var gunAt""")

e.apply()
