"""Pass 50, tasks 50.1 and 50.2: the state, the events, and the Rules members beside the calculator."""
import sys

sys.path.insert(0, "E:/Archive/GitHub/dlandi/pass32-tools/pass50")
from edits import Edits

e = Edits()
UNITS = "LimboDancer.Domains.Asl.Units/State/"
RULES = "LimboDancer.Domains.Asl.Rules/"

# --- Units: conditions, records, state -------------------------------------------------------------------------------
e.edit(UNITS + "GameTypes.cs", """    public const string CmgMalfunctioned = "asl:cmg-malfunctioned";
""", """    public const string CmgMalfunctioned = "asl:cmg-malfunctioned";

    /// <summary>A BMG disabled for good by a repair dr of 6 (D3.7, p. 201; pass 50); it stays malfunctioned.</summary>
    public const string BmgDisabled = "asl:bmg-disabled";

    /// <summary>A CMG disabled for good by a repair dr of 6 (D3.7, p. 201; pass 50); it stays malfunctioned.</summary>
    public const string CmgDisabled = "asl:cmg-disabled";
""")
e.edit(UNITS + "GameTypes.cs", "[Captured, Melee, Abandoned, Bogged, Mired, BmgMalfunctioned, CmgMalfunctioned, CcReaction, Dismantled, Unarmed];",
       "[Captured, Melee, Abandoned, Bogged, Mired, BmgMalfunctioned, CmgMalfunctioned, CcReaction, Dismantled, Unarmed, BmgDisabled, CmgDisabled];")

e.edit(UNITS + "OrdnanceRecords.cs", """public sealed record TurretFacing(string Vehicle, UnitFacing Facing);
""", """public sealed record TurretFacing(string Vehicle, UnitFacing Facing);

/// <summary>
/// A weapon of a vehicle that fired this Player Turn (D3.5, D3.51, pp. 200 and 201; pass 50): the vehicle, the weapon (<c>ma</c>, <c>bmg</c>, or
/// <c>cmg</c>), the fire phase (Defensive First Fire and Final Fire are one, A.15), the hexspines its CA changed for the shot, and whether that
/// change was doubled by woods, a building, or rubble (C5.11). Kept until the Player Turn ends.
/// </summary>
public sealed record VehicleWeaponShot(string Vehicle, string Weapon, string FirePhase, int Turn, bool Doubled);
""")

e.edit(UNITS + "GameState.cs", """    /// <summary>The Turret Covered Arcs that differ from their VCA (D3.12; ruling R7.10).</summary>
""", """    /// <summary>The weapons each vehicle has fired this Player Turn, with the CA change each made (D3.5, D3.51; pass 50); cleared as the Player Turn ends.</summary>
    public IReadOnlyList<VehicleWeaponShot> VehicleWeaponShots { get; init; } = [];

    /// <summary>The Turret Covered Arcs that differ from their VCA (D3.12; ruling R7.10).</summary>
""")

e.edit(UNITS + "FireRecords.cs", """using System.Text.Json;

namespace""", """using System.Text.Json;
using LimboDancer.Domains.Asl.Units.Documents;

namespace""")
e.edit(UNITS + "FireRecords.cs", """    public int? MovementStep
    {
        get; init;
    }
}
""", """    public int? MovementStep
    {
        get; init;
    }

    /// <summary>The TCA a vehicle's CMG turned its turret to for this attack (D3.52, D3.12, pp. 199 and 201; pass 50); null when it did not turn.</summary>
    public UnitFacing? TurretFacing
    {
        get; init;
    }
}
""")

e.edit(UNITS + "LiveRecords.cs", """    public const string Repaired = "repaired";
    public const string NoChange = "no-change";
    public const string Eliminated = "eliminated";
}
""", """    public const string Repaired = "repaired";
    public const string NoChange = "no-change";
    public const string Eliminated = "eliminated";

    /// <summary>
    /// Which weapon of a vehicle the attempt is for (D3.7, p. 201; pass 50): <c>ma</c> for a Gun MA, <c>bmg</c>, or <c>cmg</c>, with the vehicle as
    /// unit and equipment. Null for a SW and for a vehicle's MA AAMG, which is every record made before pass 50.
    /// </summary>
    public string? Weapon
    {
        get; init;
    }
}
""")

# --- Units: reader and writer ---------------------------------------------------------------------------------------
e.edit(UNITS + "GameEventReader.cs", """                        MovementStep = fields.OptionalInteger(payload, "movementStep", path),
                    };
""", """                        MovementStep = fields.OptionalInteger(payload, "movementStep", path),
                        TurretFacing = fields.OptionalString(payload, "turretFacing", path) is { } turretText && UnitFacings.TryParse(turretText, out var turretFacing)
                            ? turretFacing : null,
                    };
""")
e.edit(UNITS + "GameEventReader.cs", """                    : new RepairAttempted(repairUnit, repaired, repairRoll, repairNumber.Value, repairResult);
""", """                    : new RepairAttempted(repairUnit, repaired, repairRoll, repairNumber.Value, repairResult)
                    {
                        Weapon = fields.OptionalString(payload, "weapon", path),
                    };
""")
e.edit(UNITS + "GameEventWriter.cs", """                if (fire.MovementStep is { } movementStep)
                {
                    writer.WriteNumber("movementStep", movementStep);
                }
""", """                if (fire.MovementStep is { } movementStep)
                {
                    writer.WriteNumber("movementStep", movementStep);
                }

                if (fire.TurretFacing is { } turretFacing)
                {
                    writer.WriteString("turretFacing", Documents.UnitFacings.Name(turretFacing));
                }
""")
e.edit(UNITS + "GameEventWriter.cs", """                writer.WriteNumber("repairNumber", repair.RepairNumber);
                writer.WriteString("result", repair.Result);
""", """                writer.WriteNumber("repairNumber", repair.RepairNumber);
                writer.WriteString("result", repair.Result);
                if (repair.Weapon is { } repairedWeapon)
                {
                    writer.WriteString("weapon", repairedWeapon);
                }
""")

# --- Units: the projector -------------------------------------------------------------------------------------------
P = UNITS + "GameProjector.cs"
e.edit(P, """                    MovedVehicles = newPlayerTurn ? [] : state.MovedVehicles,
""", """                    MovedVehicles = newPlayerTurn ? [] : state.MovedVehicles,
                    VehicleWeaponShots = newPlayerTurn ? [] : state.VehicleWeaponShots,
""")
e.edit(P, """            // C2.24, D3.5 (unit step 25): a vehicle's MG shot keeps its Multiple ROF for this phase when its record says so, as a Gun's does.
            var shots = state.OrdnanceShots;
            if (fire.Facts.TryGetProperty("vehicleFire", out var vehicleFire) && vehicleFire.ValueKind == JsonValueKind.Object
                && vehicleFire.TryGetProperty("vehicleId", out var vehicleId) && vehicleId.GetString() is { } vehicle)
            {
""", """            // C2.24, D3.5 (unit step 25): a vehicle's MG shot keeps its Multiple ROF for this phase when its record says so, as a Gun's does.
            var shots = state.OrdnanceShots;
            var weaponShots = state.VehicleWeaponShots;
            var turrets = state.TurretFacings;
            if (fire.Facts.TryGetProperty("vehicleFire", out var vehicleFire) && vehicleFire.ValueKind == JsonValueKind.Object
                && vehicleFire.TryGetProperty("vehicleId", out var vehicleId) && vehicleId.GetString() is { } armed
                && vehicleFire.TryGetProperty("weapons", out var namedWeapons) && namedWeapons.ValueKind == JsonValueKind.Array)
            {
                // D3.5, D3.51, D3.52 (pass 50): a BMG and a CMG fire once a Player Turn; the state keeps which fired, in which fire phase, and the
                // CA change each made. A CMG fired outside the TCA turns the turret. The MA's own ROF record is left as it is.
                var firePhase = Rules.ScenarioA1FireEligibility.FirePhase(state.Phase, state.Unit(armed)?.Side == state.PhasingSide);
                var doubled = vehicleFire.TryGetProperty("inWoodsOrBuilding", out var inWoods) && inWoods.ValueKind == JsonValueKind.True;
                weaponShots = [.. weaponShots, .. namedWeapons.EnumerateArray().Select(item => new VehicleWeaponShot(armed,
                    item.TryGetProperty("weapon", out var weaponName) ? weaponName.GetString() ?? string.Empty : string.Empty, firePhase,
                    item.TryGetProperty("turn", out var turned) && turned.ValueKind == JsonValueKind.Number ? turned.GetInt32() : 0, doubled))];
                if (fire.TurretFacing is { } turret)
                {
                    turrets = [.. turrets.Where(item => item.Vehicle != armed), new TurretFacing(armed, turret)];
                }
            }
            else if (fire.Facts.TryGetProperty("vehicleFire", out vehicleFire) && vehicleFire.ValueKind == JsonValueKind.Object
                && vehicleFire.TryGetProperty("vehicleId", out vehicleId) && vehicleId.GetString() is { } vehicle)
            {
                weaponShots = [.. weaponShots, new VehicleWeaponShot(vehicle, "ma",
                    Rules.ScenarioA1FireEligibility.FirePhase(state.Phase, state.Unit(vehicle)?.Side == state.PhasingSide), 0, false)];
""")
e.edit(P, """                OrdnanceShots = shots,
                FireLanes = state.FireLanes.Count == 0 ? state.FireLanes
""", """                OrdnanceShots = shots,
                VehicleWeaponShots = weaponShots,
                TurretFacings = turrets,
                FireLanes = state.FireLanes.Count == 0 ? state.FireLanes
""")
e.edit(P, """            state = SupportWeapons(state, fired, shooter, mortar, panzerfaust);
            return state with
            {
""", """            state = SupportWeapons(state, fired, shooter, mortar, panzerfaust);
            return state with
            {
                // D3.5, D3.51 (pass 50): a tank's MA shot is one of its weapons' shots of the fire phase, with the turn its turret made.
                VehicleWeaponShots = tank
                    ? [.. state.VehicleWeaponShots, new VehicleWeaponShot(fired.Gun, "ma", Rules.ScenarioA1FireEligibility.FirePhase(state.Phase, shooter.Side == state.PhasingSide),
                        fired.Facts.TryGetProperty("hexspinesToTurn", out var maTurn) && maTurn.ValueKind == JsonValueKind.Number ? maTurn.GetInt32() : 0,
                        fired.Facts.TryGetProperty("firerInWoodsOrBuilding", out var maWoods) && maWoods.ValueKind == JsonValueKind.True)]
                    : state.VehicleWeaponShots,
""")
e.edit(P, """            // D3.7 (ruling R6.10): a vehicle's malfunctioned MG, repaired by its CE crew that is not Stunned or Recalled, on a dr of 1; a 6
            // disables it.
            if (repair.Unit == repair.Equipment && Active(state, repair.Unit) is UnitInstance vehicle && vocabulary.IsA(vehicle.Kind, "asl:vehicle"))
            {
                if (!Rules.ScenarioA1RoutRallyProjection.VehicleRepairAllowedAsRecorded(state.Phase, GameState.Condition(vehicle, Conditions.Malfunctioned) == ConditionState.True,
""", """            // D3.7 (ruling R6.10): a vehicle's malfunctioned MG, repaired by its CE crew that is not Stunned or Recalled, on a dr of 1; a 6
            // disables it.
            if (repair.Unit == repair.Equipment && Active(state, repair.Unit) is UnitInstance vehicle && vocabulary.IsA(vehicle.Kind, "asl:vehicle"))
            {
                // D3.7 (p. 201; pass 50): a Gun MA, a BMG, or a CMG, each by its own dr once a RPh, by a crew that is not shocked or stunned.
                var repairKey = repair.Weapon is { } weaponKey ? vehicle.Id + ":" + weaponKey : vehicle.Id;
                if (repair.Weapon is { } repairedWeapon
                    ? state.Phase != "rph" || Rules.ScenarioA1RallyRules.VehicleWeaponRepairBar(vehicle.Id, repairedWeapon,
                        GameState.Condition(vehicle, Rules.ScenarioA1RallyRules.VehicleWeaponMalfunction(repairedWeapon) switch
                        {
                            Rules.UnitCondition.BmgMalfunctioned => Conditions.BmgMalfunctioned,
                            Rules.UnitCondition.CmgMalfunctioned => Conditions.CmgMalfunctioned,
                            _ => Conditions.Malfunctioned,
                        }) == ConditionState.True,
                        GameState.Condition(vehicle, Rules.ScenarioA1RallyRules.VehicleWeaponDisabled(repairedWeapon) switch
                        {
                            Rules.UnitCondition.BmgDisabled => Conditions.BmgDisabled,
                            Rules.UnitCondition.CmgDisabled => Conditions.CmgDisabled,
                            _ => Conditions.Disabled,
                        }) == ConditionState.True,
                        Rules.ScenarioA1RallyRules.VehicleCrewImpaired(GameState.Condition(vehicle, Conditions.Stunned) == ConditionState.True,
                            GameState.Condition(vehicle, Conditions.Shocked) == ConditionState.True, GameState.Condition(vehicle, Conditions.UnconfirmedKill) == ConditionState.True,
                            GameState.Condition(vehicle, Conditions.Recalled) == ConditionState.True, GameState.Condition(vehicle, Conditions.StunRecovery) == ConditionState.True),
                        state.RepairsThisPhase.Contains(repairKey)) is not null
                    : !Rules.ScenarioA1RoutRallyProjection.VehicleRepairAllowedAsRecorded(state.Phase, GameState.Condition(vehicle, Conditions.Malfunctioned) == ConditionState.True,
""")
e.edit(P, """                return state with
                {
                    RepairsThisPhase = [.. state.RepairsThisPhase, vehicle.Id]
                };
""", """                return state with
                {
                    RepairsThisPhase = [.. state.RepairsThisPhase, repairKey]
                };
""")

# --- Rules: the condition names, the weapons fired, the repair -------------------------------------------------------
e.edit(RULES + "ScenarioA1FireRuleModels.cs", """    Recalled,
    Stunned,
    StunRecovery,
}
""", """    Recalled,
    Stunned,
    StunRecovery,
    BmgDisabled,
    CmgDisabled,
}

/// <summary>A weapon of a vehicle that fired this Player Turn, as the state has it (D3.5, D3.51; pass 50): the weapon, its fire phase, the hexspines its CA changed, and whether woods, a building, or rubble doubled that.</summary>
public sealed record VehicleWeaponShotFacts(string Weapon, string FirePhase, int Turn, bool Doubled);

/// <summary>The BMG and CMG a vehicle's attack names, against the state (D3.5, D3.51, D3.7; pass 50): the names, whether each MG has malfunctioned, the fire phase now, the vehicle's shots of this Player Turn, and its turret's type.</summary>
public sealed record VehicleWeaponsFacts(IReadOnlyList<string> Named, bool BmgMalfunctioned, bool CmgMalfunctioned, string FirePhase, IReadOnlyList<VehicleWeaponShotFacts> Shots, string? MaType);
""")

e.edit(RULES + "ScenarioA1FireEligibility.cs", """    /// <summary>
    /// A unit in the target Location, with the Fanatic (A10.8) and heroic (A15.21) states the Fire package reads, its Inexperience (A19.2; ruling
""", """    /// <summary>
    /// The fire phase a vehicle's weapon fires in (D3.5, p. 201; A.15, p. 44; pass 50): the phase, but Defensive First Fire in the MPh and Final
    /// Fire in the DFPh are one for the side that is not phasing ("an AFV firing its CMG as Defensive First Fire may still fire its MA in its DFPh").
    /// </summary>
    public static string FirePhase(string? phase, bool phasing) => !phasing && phase is "mph" or "dfph" ? "defensive" : phase ?? string.Empty;

    /// <summary>
    /// The Case A DRM a turret-mounted weapon pays when it fires within the current TCA (D3.51, p. 201; pass 50): "any of its other turret-mounted
    /// weapons which fire within the current respective CA must pay the same CA Change penalty as the first weapon which fired". The first shot of
    /// this fire phase by the MA or the CMG that turned the turret gives it; null when none did.
    /// </summary>
    public static int? MaintainedCaseA(IEnumerable<VehicleWeaponShotFacts> shots, string firePhase, string? maType)
    {
        ArgumentNullException.ThrowIfNull(shots);
        return shots.Where(item => item.FirePhase == firePhase && item.Weapon is "ma" or FireVehicleWeapon.CoaxialMg && item.Turn > 0)
            .Select(item => (int?)(ScenarioA1ArmorCalculator.CaseA(maType, item.Turn) * (item.Doubled ? 2 : 1))).FirstOrDefault();
    }

    /// <summary>
    /// A vehicle's attack with the BMG and CMG it names (D3.5, D3.51, D3.7; pass 50): each MG's malfunction, whether it has fired this Player Turn
    /// (one attack each), the Case A a CMG maintains, and whether a weapon of the vehicle fired in another fire phase.
    /// </summary>
    public static FireVehicleFire WithVehicleWeapons(FireVehicleFire fire, VehicleWeaponsFacts facts)
    {
        ArgumentNullException.ThrowIfNull(fire);
        ArgumentNullException.ThrowIfNull(facts);
        var maintained = MaintainedCaseA(facts.Shots, facts.FirePhase, facts.MaType);
        return fire with
        {
            Weapons = [.. facts.Named.Select(name => new FireVehicleWeapon(name,
                name == FireVehicleWeapon.BowMg ? facts.BmgMalfunctioned : name == FireVehicleWeapon.CoaxialMg && facts.CmgMalfunctioned,
                facts.Shots.Any(item => item.Weapon == name))
            {
                MaintainedCaseA = name == FireVehicleWeapon.CoaxialMg ? maintained : null,
            })],
            OtherPhase = facts.Shots.Any(item => item.FirePhase != facts.FirePhase) ? true : null,
        };
    }

    /// <summary>
    /// Why a vehicle's MG may not fire at a target its CA does not cover (D1.81, D3.52, C5.13; pass 50), or null: a BMG fires only inside the VCA
    /// (the pivot of the hull for a shot is not built), and no CA changes for Bounding First Fire.
    /// </summary>
    public static string? VehicleWeaponArcBar(string vehicleId, string weapon, int turn, bool boundingFirstFire) =>
        turn <= 0 ? null
        : weapon == FireVehicleWeapon.BowMg
            ? $"play.fire-vehicle-vca: {vehicleId}'s BMG fires only at a target inside its VCA (D1.81); turning the hull for a shot is not built"
        : boundingFirstFire
            ? $"play.fire-vehicle-tca: {vehicleId}'s CMG fires within its current TCA in Bounding First Fire; the turret turns with an MP expenditure (C5.13, D3.12)"
        : null;

    /// <summary>
    /// A unit in the target Location, with the Fanatic (A10.8) and heroic (A15.21) states the Fire package reads, its Inexperience (A19.2; ruling
""")

e.edit(RULES + "ScenarioA1FireFollowUps.cs", """        var conditions = new List<(UnitCondition, bool)>();
        if (weapon.Malfunctioned)
        {
            conditions.Add((UnitCondition.Malfunctioned, true));
        }
""", """        var conditions = new List<(UnitCondition, bool)>();
        if (weapon.Malfunctioned)
        {
            // D3.7 (pass 50): a vehicle's BMG or CMG has its own Malfunction counter.
            conditions.Add((weapon.VehicleWeapon switch
            {
                FireVehicleWeapon.BowMg => UnitCondition.BmgMalfunctioned,
                FireVehicleWeapon.CoaxialMg => UnitCondition.CmgMalfunctioned,
                _ => UnitCondition.Malfunctioned,
            }, true));
        }
""")

e.edit(RULES + "ScenarioA1RallyRules.cs", """    /// <summary>The plan's words for a vehicle MG repair.</summary>
""", """    /// <summary>The name a vehicle's weapon is repaired under (D3.7; pass 50): <c>ma</c>, <c>bmg</c>, or <c>cmg</c>.</summary>
    public static bool IsVehicleWeapon(string? weapon) => weapon is "ma" or FireVehicleWeapon.BowMg or FireVehicleWeapon.CoaxialMg;

    /// <summary>The weapon's name in a sentence.</summary>
    public static string VehicleWeaponName(string weapon) => weapon switch
    {
        FireVehicleWeapon.BowMg => "BMG",
        FireVehicleWeapon.CoaxialMg => "CMG",
        _ => "MA",
    };

    /// <summary>The condition that says a vehicle's weapon has malfunctioned (D3.7).</summary>
    public static UnitCondition VehicleWeaponMalfunction(string weapon) => weapon switch
    {
        FireVehicleWeapon.BowMg => UnitCondition.BmgMalfunctioned,
        FireVehicleWeapon.CoaxialMg => UnitCondition.CmgMalfunctioned,
        _ => UnitCondition.Malfunctioned,
    };

    /// <summary>The condition that says a vehicle's weapon is disabled for good (D3.7).</summary>
    public static UnitCondition VehicleWeaponDisabled(string weapon) => weapon switch
    {
        FireVehicleWeapon.BowMg => UnitCondition.BmgDisabled,
        FireVehicleWeapon.CoaxialMg => UnitCondition.CmgDisabled,
        _ => UnitCondition.Disabled,
    };

    /// <summary>
    /// Whether a vehicle's crew is shocked or stunned (D3.7, D5.34, D5.341, C7.42; pass 50): under a Stun, a Shock, or an Unconfirmed Kill, or
    /// Recalled before its counter is flipped ("Recall is treated the same as a Stun" until that Player Turn ends).
    /// </summary>
    public static bool VehicleCrewImpaired(bool stunned, bool shocked, bool unconfirmedKill, bool recalled, bool stunRecovery) =>
        stunned || shocked || unconfirmedKill || (recalled && !stunRecovery);

    /// <summary>
    /// D3.7 (p. 201; pass 50), for a Gun MA, a BMG, or a CMG; one member for the planner and the projector: the weapon is malfunctioned and not
    /// disabled; "repair of each malfunctioned weapon can be attempted once per RPh with a separate dr for each malfunctioned weapon by any
    /// inherent crew that is not shocked or stunned". Only an AAMG asks for a CE crew.
    /// </summary>
    public static string? VehicleWeaponRepairBar(string vehicleId, string weapon, bool malfunctioned, bool disabled, bool crewImpaired, bool repairedThisPhase) =>
        !malfunctioned || disabled ? $"play.repair-weapon: {vehicleId}'s {VehicleWeaponName(weapon)} is not malfunctioned, or is disabled (D3.7)"
        : crewImpaired || repairedThisPhase
            ? $"play.repair-unit: {vehicleId}'s {VehicleWeaponName(weapon)} is repaired once per RPh by a crew that is not shocked or stunned (D3.7)"
        : null;

    /// <summary>
    /// What a repair dr does to a vehicle's Gun MA, BMG, or CMG (D3.7, D5.341, pp. 201 and 203; pass 50): a 1 removes the Malfunction counter; a 6
    /// disables the weapon, which stays malfunctioned; and "any vehicle whose MA and all Secondary Armament ... are all disabled is immediately
    /// Recalled (5.341) unless that vehicle has Passenger/Towing capability": the Recall of D5.341, a Stun until the Player Turn ends.
    /// </summary>
    public static IReadOnlyList<(UnitCondition Condition, bool Value)> VehicleWeaponRepairChanges(string weapon, RepairOutcome outcome, bool passengerOrTowing) => outcome switch
    {
        RepairOutcome.Repaired => [(VehicleWeaponMalfunction(weapon), false)],
        RepairOutcome.Eliminated when weapon == "ma" && !passengerOrTowing =>
        [
            (UnitCondition.Disabled, true), (UnitCondition.Recalled, true), (UnitCondition.Stunned, false), (UnitCondition.StunRecovery, false),
            (UnitCondition.ButtonedUp, true), (UnitCondition.Motion, false),
        ],
        RepairOutcome.Eliminated => [(VehicleWeaponDisabled(weapon), true)],
        _ => [],
    };

    /// <summary>The plan's words for the repair of a vehicle's Gun MA, BMG, or CMG.</summary>
    public static string VehicleWeaponRepairSummary(string vehicleId, string weapon, bool recallsIfDisabled) =>
        $"play.repair: {vehicleId}'s crew attempts to repair its {VehicleWeaponName(weapon)} (a dr of 1 repairs it, a 6 disables it"
        + (recallsIfDisabled ? " and Recalls the vehicle" : string.Empty) + "; D3.7)";

    /// <summary>The plan's words for a vehicle MG repair.</summary>
""")

e.apply()
