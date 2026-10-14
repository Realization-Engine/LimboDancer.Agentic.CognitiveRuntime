"""Pass 50, the reviews' fixes, part A: Rules, Play, and Units."""
import sys

sys.path.insert(0, "E:/Archive/GitHub/dlandi/pass32-tools/pass50")
from edits import Edits

e = Edits()
RULES = "LimboDancer.Domains.Asl.Rules/"
PLAY = "LimboDancer.Domains.Asl.Play/"
UNITS = "LimboDancer.Domains.Asl.Units/State/"
C = RULES + "ScenarioA1OrdnanceCalculator.cs"
A = RULES + "ScenarioA1ArmorCalculator.cs"
E = RULES + "ScenarioA1OrdnanceEligibility.cs"

# Table player 1, referee 2.2 (BLOCKER): C6.55, no Acquisition DRM in Bounding First Fire or Motion Fire. Referee 2.3: C6.3, no Case L for a Motion firer.
e.edit(C, """        if (range is >= 1 and <= 2 && !sameHex)
        {
            drm.Add(new FireModifier("case-l", range == 1 ? -2 : -1, "C6.3"));
        }
""", """        // C6.3 (p. 174; review of pass 50): "Case L is not applicable if ... the firer is in-Motion/Non-Stopped".
        if (range is >= 1 and <= 2 && !sameHex && shot.Vehicle?.InMotion != true)
        {
            drm.Add(new FireModifier("case-l", range == 1 ? -2 : -1, "C6.3"));
        }
""")
e.edit(C, """        else if (shot.Acquisition is int acquired && acquired < 0 && !concealedTarget)
        {
            // C6.51, C6.57: the Acquisition applies only to Known units, and a shot at a concealed target loses it.
            drm.Add(new FireModifier("case-n", acquired, "C6.5"));
        }
""", """        else if (shot.Acquisition is int acquired && acquired < 0 && !concealedTarget && !AcquisitionDenied(shot))
        {
            // C6.51, C6.57: the Acquisition applies only to Known units, and a shot at a concealed target loses it.
            drm.Add(new FireModifier("case-n", acquired, "C6.5"));
        }
""")
e.edit(C, """        var acquisition = !acquires ? 0 : concealedTarget ? -1 : Math.Max(shot.Acquisition!.Value - 1, -2);
""", """        var acquisition = !acquires ? 0 : concealedTarget ? -1 : Math.Max((AcquisitionDenied(shot) ? 0 : shot.Acquisition!.Value) - 1, -2);
""")
e.edit(A, """        else if (!latw && shot.Acquisition is int acquired && acquired < 0 && target.Concealed != true)
        {
            drm.Add(new FireModifier("case-n", acquired, "C6.5"));
        }
""", """        else if (!latw && shot.Acquisition is int acquired && acquired < 0 && target.Concealed != true && !ScenarioA1OrdnanceCalculator.AcquisitionDenied(shot))
        {
            drm.Add(new FireModifier("case-n", acquired, "C6.5"));
        }
""")
e.edit(A, """        var acquisition = !acquires ? 0 : Math.Max(shot.Acquisition!.Value - 1, -2);
        var gunEffect = new OrdnanceGunEffect(ScenarioA1OrdnanceCalculator.Breakdown(shot, gun, reference), malfunctioned, Math.Max(rof, 0), kept, counter, acquisition,""",
       """        var acquisition = !acquires ? 0 : Math.Max((ScenarioA1OrdnanceCalculator.AcquisitionDenied(shot) ? 0 : shot.Acquisition!.Value) - 1, -2);
        var gunEffect = new OrdnanceGunEffect(ScenarioA1OrdnanceCalculator.Breakdown(shot, gun, reference), malfunctioned, Math.Max(rof, 0), kept, counter, acquisition,""")
e.edit(C, """    /// <summary>
    /// Whether the shot is a Motion firer's (C5.35, D2.42, pp. 172 and 198; pass 50)""", """    /// <summary>
    /// C6.55 (p. 175; review of pass 50): "only a Stabilized Gun (D11.1) may claim/retain its Acquisition DRM for Motion/Bounding-First Fire TH
    /// attempts". No vehicle of the catalog is Stabilized: such a shot takes no Case N, and what it acquires starts from nothing (ruling R50.13).
    /// </summary>
    internal static bool AcquisitionDenied(OrdnanceShot shot) => shot.FireKind == ScenarioA1FireCalculator.BoundingFirstFire || MotionFirer(shot);

    /// <summary>
    /// Whether the shot is a Motion firer's (C5.35, D2.42, pp. 172 and 198; pass 50)""")

# Referee 2.9: C5.51, Case A never with Case E outside Defensive First Fire.
e.edit(C, """        else if (shot.Vehicle?.Weapons?.MaintainedCaseA is > 0 and var maintained)
        {""", """        else if (shot.Vehicle?.Weapons?.MaintainedCaseA is > 0 and var maintained && shot.SameHex != true)
        {""")
e.edit(A, """        else if (shot.Vehicle?.Weapons?.MaintainedCaseA is > 0 and var maintained)
        {""", """        else if (shot.Vehicle?.Weapons?.MaintainedCaseA is > 0 and var maintained && shot.SameHex != true)
        {""")

# Rules boundary 1: inside its own Location a tank's MA fires at a vehicle only; the calculator says so too.
e.edit(C, """        if ((sameHex ? shot.Range != 0 || shot.HexspinesToTurn != 0 || shot.FireKind is not null || mortar || latw : shot.Range < 1)""",
       """        if ((sameHex ? shot.Range != 0 || shot.HexspinesToTurn != 0 || shot.FireKind is not null || mortar || latw || (shot.Vehicle is not null && shot.VehicleTarget is null)
                : shot.Range < 1)""")

# Referee 1.1, 1.2: the vehicle's counter and the MA. A Bounding Fire counter beside a MA entry with no kept ROF is the MA's (an OVR's, too);
# a First Fire counter is not the MA's while the MA's last shot of the phase kept its ROF.
e.edit(C, """        var firstFired = !othersOnly && gunShot.FirstFire == true;
""", """        var firstFired = !othersOnly && gunShot.FirstFire == true && !(shot.Vehicle is not null && gunShot.ShotsThisPhase > 0 && gunShot.RateOfFireKept == true);
""")
e.edit(C, """        if (!mayFire || crew.FiredInherentFp == true || shot.Vehicle?.Weapons?.OtherPhase == true)
""", """        if (!mayFire || crew.FiredInherentFp == true || shot.Vehicle?.Weapons?.OtherPhase == true
            || (shot.Vehicle?.Weapons?.BoundingFireSpent == true && gunShot.RateOfFireKept != true))
""")
e.edit(RULES + "ScenarioA1ArmorModels.cs", """public sealed record OrdnanceVehicleWeapons(bool OtherPhase, bool MarkedByOthers, int? MaintainedCaseA);
""", """public sealed record OrdnanceVehicleWeapons(bool OtherPhase, bool MarkedByOthers, int? MaintainedCaseA)
{
    /// <summary>
    /// Whether the vehicle's Bounding Fire counter stands beside a shot of its MA in this MPh, an OVR among them (D3.3, D7.1; review of pass 50):
    /// the MA then fires again only on a ROF it kept. Null is false.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public bool? BoundingFireSpent
    {
        get; init;
    }
}
""")
e.edit(E, """        return new OrdnanceVehicleWeapons(!afterBounding && shots.Any(item => item.FirePhase != firePhase), shots.Count > 0 && shots.All(item => item.Weapon != "ma"),
            shots.Any(item => item.Weapon == "ma" && item.FirePhase == firePhase) ? null
                : ScenarioA1FireEligibility.MaintainedCaseA(shots.Where(item => item.Weapon != "ma"), firePhase, maType));
    }
""", """        // D3.51 (review of pass 50): the MA pays the CMG's Case A on its first shot after the CMG turned the turret, whether or not it fired before.
        var lastTurn = shots.Select((item, index) => (item, index)).LastOrDefault(pair => pair.item.FirePhase == firePhase && pair.item.Weapon != "ma" && pair.item.Turn > 0);
        var paid = lastTurn.item is null || shots.Skip(lastTurn.index + 1).Any(item => item.Weapon == "ma" && item.FirePhase == firePhase);
        return new OrdnanceVehicleWeapons(!afterBounding && shots.Any(item => item.FirePhase != firePhase), shots.Count > 0 && shots.All(item => item.Weapon != "ma"),
            paid ? null : ScenarioA1FireEligibility.MaintainedCaseA([lastTurn.item!], firePhase, maType))
        {
            BoundingFireSpent = boundingFireMarked && shots.Any(item => item.Weapon == "ma" && item.FirePhase == "mph") ? true : null,
        };
    }
""")

# Referee 1.3, 2.1: C5.3's MP between shots is skipped only when the MA's own ROF is spent, not for a counter a MG placed.
e.edit(PLAY + "LiveOrdnance.cs", """                    Is(gun, Conditions.BoundingFire)) is { } mpBar)""",
       """                    state.OrdnanceShots.FirstOrDefault(item => item.Gun == gun.Id) is { Shots: > 0, RateOfFireKept: false }) is { } mpBar)""")

# Referee 1.8: D3.31, D3.3: the MGs fire in the AFPh after a MA-only Bounding First Fire that left no counter.
e.edit(RULES + "ScenarioA1FireRuleModels.cs", """public sealed record VehicleWeaponsFacts(IReadOnlyList<string> Named, bool BmgMalfunctioned, bool CmgMalfunctioned, string FirePhase, IReadOnlyList<VehicleWeaponShotFacts> Shots, string? MaType);
""", """public sealed record VehicleWeaponsFacts(IReadOnlyList<string> Named, bool BmgMalfunctioned, bool CmgMalfunctioned, string FirePhase, IReadOnlyList<VehicleWeaponShotFacts> Shots, string? MaType,
    bool BoundingFireMarked = false);
""")
F = RULES + "ScenarioA1FireEligibility.cs"
e.edit(F, """            OtherPhase = facts.Shots.Any(item => item.FirePhase != facts.FirePhase) ? true : null,
        };
    }
""", """            OtherPhase = OtherFirePhase(facts.Shots, facts.FirePhase, facts.BoundingFireMarked) ? true : null,
        };
    }

    /// <summary>
    /// Whether a weapon of the vehicle fired in another fire phase of this Player Turn, so that the rest have forfeited theirs (D3.5, p. 201).
    /// D3.31, D3.3 (review of pass 50): not in the AFPh when only the MA fired in the MPh and left no Bounding Fire counter: "if a vehicle fires any
    /// weapon other than its MA during the MPh it may not fire its other weapons/PRC during the AFPh".
    /// </summary>
    public static bool OtherFirePhase(IReadOnlyList<VehicleWeaponShotFacts> shots, string firePhase, bool boundingFireMarked)
    {
        ArgumentNullException.ThrowIfNull(shots);
        var afterBounding = firePhase == "afph" && !boundingFireMarked && shots.Count > 0 && shots.All(item => item.Weapon == "ma" && item.FirePhase == "mph");
        return !afterBounding && shots.Any(item => item.FirePhase != firePhase);
    }

    /// <summary>D5.41 (review of pass 50): an Abandoned vehicle has no crew to fire its MGs.</summary>
    public static string? AbandonedVehicleBar(string vehicleId, bool abandoned) =>
        abandoned ? $"play.fire-vehicle-abandoned: {vehicleId} is Abandoned and has no crew to fire its weapons (D5.41)" : null;
""")
# Table player 11: the words of two bars.
e.edit(F, """        disabled ? "is disabled" : malfunctioned ? "has malfunctioned" : fired ? "has fired"
        : otherPhase ? "the vehicle fired in another phase (D3.5)" : turretBarred ? "a CE crew does not fire this turret's weapons (D1.321)" : null;
""", """        disabled ? "is disabled" : malfunctioned ? "has malfunctioned" : fired ? "has fired this Player Turn (D3.5)"
        : otherPhase ? "the vehicle fired in another phase (D3.5)" : turretBarred ? "a RST or 1MT turret's CMG does not fire while the crew is CE (D1.321, D1.322)" : null;
""")
LF = PLAY + "LiveFire.cs"
e.edit(LF, """            ScenarioA1FireEligibility.FirePhase(state.Phase, vehicle.Side == state.PhasingSide), VehicleShots(state, vehicle.Id),
            vehicle.Definition is { } definition ? CatalogReference.Value.Definitions.GetValueOrDefault(definition.Definition)?.MaType : null));
""", """            ScenarioA1FireEligibility.FirePhase(state.Phase, vehicle.Side == state.PhasingSide), VehicleShots(state, vehicle.Id),
            vehicle.Definition is { } definition ? CatalogReference.Value.Definitions.GetValueOrDefault(definition.Definition)?.MaType : null,
            Is(vehicle, Conditions.BoundingFire)));
""")
e.edit(LF, """        var otherPhase = shots.Any(item => item.FirePhase != ScenarioA1FireEligibility.FirePhase(state.Phase, vehicle.Side == state.PhasingSide));
""", """        var otherPhase = ScenarioA1FireEligibility.OtherFirePhase(shots, ScenarioA1FireEligibility.FirePhase(state.Phase, vehicle.Side == state.PhasingSide),
            Is(vehicle, Conditions.BoundingFire));
""")
# Referee 1.5: an Abandoned vehicle's MGs.
e.edit(LF, """        if (vehicleFirers.Length > 0 && ScenarioA1FireEligibility.VehicleWeaponsNamedBar(""", """        if (vehicleFirers.Length > 0 && ScenarioA1FireEligibility.AbandonedVehicleBar(vehicleFirers[0].Id, Is(vehicleFirers[0], Conditions.Abandoned)) is { } abandoned)
        {
            return (null, abandoned);
        }

        if (vehicleFirers.Length > 0 && ScenarioA1FireEligibility.VehicleWeaponsNamedBar(""")
# Table player 11: a failure worded as a failure.
e.edit(LF, """            return "The fire record turns the turret exactly the hexspines its CMG's facts say the shot changes its TCA by.";
""", """            return "The fire record does not turn the turret by the hexspines its CMG's facts give.";
""")

# Referee 1.5, table player 9: the repair's bars, each with its own words; an Abandoned vehicle repairs nothing.
RR = RULES + "ScenarioA1RallyRules.cs"
e.edit(RR, """    public static bool VehicleCrewImpaired(bool stunned, bool shocked, bool unconfirmedKill, bool recalled, bool stunRecovery) =>
        stunned || shocked || unconfirmedKill || (recalled && !stunRecovery);
""", """    public static bool VehicleCrewImpaired(bool stunned, bool shocked, bool unconfirmedKill, bool recalled, bool stunRecovery, bool abandoned = false) =>
        stunned || shocked || unconfirmedKill || (recalled && !stunRecovery) || abandoned;
""")
e.edit(RR, """        !malfunctioned || disabled ? $"play.repair-weapon: {vehicleId}'s {VehicleWeaponName(weapon)} is not malfunctioned, or is disabled (D3.7)"
        : crewImpaired || repairedThisPhase
            ? $"play.repair-unit: {vehicleId}'s {VehicleWeaponName(weapon)} is repaired once per RPh by a crew that is not shocked or stunned (D3.7)"
        : null;
""", """        !IsVehicleWeapon(weapon) ? $"play.repair-weapon: {vehicleId} has no weapon named '{weapon}' to repair (D3.7)"
        : disabled ? $"play.repair-weapon: {vehicleId}'s {VehicleWeaponName(weapon)} is disabled for good (D3.7)"
        : !malfunctioned ? $"play.repair-weapon: {vehicleId}'s {VehicleWeaponName(weapon)} is not malfunctioned (D3.7)"
        : repairedThisPhase ? $"play.repair-unit: {vehicleId}'s {VehicleWeaponName(weapon)} has had its repair attempt this RPh (D3.7)"
        : crewImpaired
            ? $"play.repair-unit: {vehicleId}'s crew repairs nothing while it is shocked, stunned, or Recalled in this Player Turn, which is treated as a Stun, or while the vehicle is Abandoned (D3.7, D5.341)"
        : null;
""")
e.edit(PLAY + "GamePlanner.Rally.cs", """                Is(vehicle, Conditions.Recalled), Is(vehicle, Conditions.StunRecovery)),
            state.RepairsThisPhase.Contains(vehicle.Id + ":" + weapon)) is { } bar)""", """                Is(vehicle, Conditions.Recalled), Is(vehicle, Conditions.StunRecovery), Is(vehicle, Conditions.Abandoned)),
            state.RepairsThisPhase.Contains(vehicle.Id + ":" + weapon)) is { } bar)""")
P = UNITS + "GameProjector.cs"
e.edit(P, """                            GameState.Condition(vehicle, Conditions.Recalled) == ConditionState.True, GameState.Condition(vehicle, Conditions.StunRecovery) == ConditionState.True),
                        state.RepairsThisPhase.Contains(repairKey)) is not null""", """                            GameState.Condition(vehicle, Conditions.Recalled) == ConditionState.True, GameState.Condition(vehicle, Conditions.StunRecovery) == ConditionState.True,
                            GameState.Condition(vehicle, Conditions.Abandoned) == ConditionState.True),
                        state.RepairsThisPhase.Contains(repairKey)) is not null""")

# Referee 1.1: an OVR is a shot of the vehicle's weapons in the MPh.
# Referee 2.7: a turret turned with an MP expenditure is a CA change for the Acquisition.
e.edit(P, """                    Rules.ScenarioA1FireFollowUps.AcquiringCaChanged(payload is GunTurned turned && turned.Gun == acquisition.Gun,""",
       """                    Rules.ScenarioA1FireFollowUps.AcquiringCaChanged((payload is GunTurned turned && turned.Gun == acquisition.Gun)
                            || (payload is VehicleStepped { Turret: not null } turretStep && turretStep.Vehicle == acquisition.Gun),""")

# Rules boundary 9: a record that cannot predate the pass keeps its facts of the pass.
e.edit(PLAY + "LiveOrdnance.cs", """        if (merged.Vehicle is { Weapons: not null } withWeapons && recorded.Vehicle is { Weapons: null })
        {""", """        if (merged.Vehicle is { Weapons: not null } withWeapons && recorded.Vehicle is { Weapons: null }
            && recorded.FireKind != ScenarioA1FireCalculator.BoundingFirstFire && !(recorded.SameHex == true && recorded.VehicleTarget is not null))
        {""")

# Referee 2.4, 2.10: Bypass. The same-Location shot and the turret turned with an MP are refused for a vehicle in Bypass.
e.edit(RULES + "ScenarioA1VehicleMovementCalculator.cs", """    public static string? TurretStepBar(string id, string kind, bool turreted, bool facingRead, bool inObstacle) =>
        !turreted ? $"play.move-vehicle-turret: {id} has no turret to turn (D3.12)"
        : !facingRead ? "play.move-vehicle-turret: the TCA is named by a hexspine (D3.12)"
        : !TurretTurnsWith(kind) ? $"play.move-vehicle-turret: {id}'s turret turns with a Delay MP, a stop, a VCA change, or an entry (D3.12)"
""", """    public static string? TurretStepBar(string id, string kind, bool turreted, bool facingRead, bool inObstacle, bool bypass = false) =>
        !turreted ? $"play.move-vehicle-turret: {id} has no turret to turn (D3.12)"
        : !facingRead ? "play.move-vehicle-turret: name the hexspine the turret turns to, such as north-east or west (D3.12)"
        : !TurretTurnsWith(kind) ? $"play.move-vehicle-turret: {id}'s turret turns with a Delay MP, a stop, a VCA change, or an entry (D3.12)"
        : bypass ? $"play.move-vehicle-turret: {id} is in Bypass, where the TCA has its own four positions; that is not built (D2.32, D2.321)"
""")
V = PLAY + "GamePlanner.Vehicles.cs"
e.edit(V, """            terrain is not null && (WoodsOrBuilding.Contains(terrain) || IsRubbleTerrain(terrain)) && !bypass && !road);
""", """            terrain is not null && (WoodsOrBuilding.Contains(terrain) || IsRubbleTerrain(terrain)) && !bypass && !road, bypass);
""")
e.edit(RULES + "ScenarioA1OrdnanceMapRules.cs", """    /// <summary>C5.13 (p. 172; pass 50): "a Bounding First Firer""", """    /// <summary>D3.2, D2.32, C5.5 (review of pass 50): a shot within the firer's own Location at or by a vehicle in Bypass has its own Target Facing and LOS; not built.</summary>
    public static string? SameHexBypassRefusal(bool firerInBypass, bool targetInBypass) =>
        firerInBypass || targetInBypass
            ? "play.ordnance-own-hex: a shot within its own hex by or at a vehicle in Bypass is not built: its Target Facing and LOS are read from the CAFP (D2.32, C5.5)"
            : null;

    /// <summary>C5.13 (p. 172; pass 50): "a Bounding First Firer""")
e.edit(PLAY + "GamePlanner.Ordnance.cs", """            // C5.5: a wreck, SMOKE, or an AFV in the hex adds its Hindrance (Case R), "other than the target or firer".
""", """            if (ScenarioA1OrdnanceMapRules.SameHexBypassRefusal(firer is UnitInstance { Straddling: not null },
                shot.VehicleTarget is { VehicleId: { } aimedAt } && state.Unit(aimedAt) is { Straddling: not null }) is { } bypassBar)
            {
                return (null, bypassBar);
            }

            // C5.5: a wreck, SMOKE, or an AFV in the hex adds its Hindrance (Case R), "other than the target or firer".
""")
e.apply()
