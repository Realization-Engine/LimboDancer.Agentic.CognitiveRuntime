using System.Text.Json.Serialization;

namespace LimboDancer.Domains.Asl.Rules;

/// <summary>
/// A declared fire attack for the Fire package (unit step 17). Every fact is supplied by the caller; the package never
/// reads a game. Nullable members are required: a missing one leaves the attack undecided.
/// </summary>
public sealed record FireAttack(
    string? Phase,
    string? FiringSide,
    bool? FireGroupComplete,
    string? FirerLocationId,
    string? TargetLocationId,
    IReadOnlyList<FireFirer>? Firers,
    FireDirector? Director,
    int? Range,
    bool? SameLevel,
    FireLos? Los,
    int? ScenarioMonth,
    string? TargetTerrain,
    IReadOnlyList<FireTarget>? Targets,
    int? TargetSideElr,
    FireRolls? Rolls)
{
    /// <summary>
    /// The kind of Defensive fire in the MPh (unit step 22): <c>first-fire</c> (A8.1), <c>subsequent-first-fire</c> (A8.3),
    /// <c>final-protective-fire</c> (A8.31), or <c>residual-fp</c> (A8.2); null for the fire phases.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? FireKind
    {
        get; init;
    }

    /// <summary>How the target stack moved, for FFNAM and FFMO (A4.6, A4.61); required in the MPh.</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public FireMovement? TargetMovement
    {
        get; init;
    }

    /// <summary>The Residual FP counter attacking a moving unit (A8.2); required for <c>residual-fp</c>.</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? ResidualFp
    {
        get; init;
    }

    /// <summary>The firing side's ELR, for the NMC FPF inflicts on its firers (A8.31, A19.13).</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? FiringSideElr
    {
        get; init;
    }

    /// <summary>Whether no target is farther than the closest armed Known enemy unit (A8.3); required for Subsequent First Fire.</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public bool? WithinSubsequentFirstFireRange
    {
        get; init;
    }

    /// <summary>The leaders directing the group from its other Locations (A7.531); null when the group is in one Location.</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public IReadOnlyList<FireDirector>? OtherDirectors
    {
        get; init;
    }

    /// <summary>Whether every Location of a group spanning Locations is ADJACENT to another of them (A7.5, A.8).</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public bool? FirerLocationsAdjacent
    {
        get; init;
    }

    /// <summary>
    /// The target side's units in the target Location that the attack does not attack, such as those not moving with the stack
    /// in the MPh: a leader who goes berserk takes them with him on a passed NTC (A15.41). Null when there are none.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public IReadOnlyList<FireTarget>? Companions
    {
        get; init;
    }

    /// <summary>
    /// An ordnance hit on the Infantry Target Type resolved on the IFT (C3.3, C3.32, unit step 24): no firers, the Gun's HE FP
    /// column (C.6), and no TEM or Hindrance on the Effects DR (C.3) unless a Critical Hit reverses the TEM (C3.71). Null for
    /// Infantry fire.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public FireOrdnanceHit? OrdnanceHit
    {
        get; init;
    }

    /// <summary>
    /// A Canister shot's attack on one of its Locations (C8.4, C8.41, p. 178; pass 55, ruling R55.9): no firers or leader, the FP of the Gun Size
    /// with its halvings, and, unlike an ordnance hit, the Location's own TEM and LOS Hindrance on the IFT DR. Null for other fire, as in every
    /// record made before the pass.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public FireCanister? Canister
    {
        get; init;
    }

    /// <summary>
    /// The vehicles in the target Location (unit step 25): an unarmored one is attacked on the Vehicle line with the attack's IFT DR
    /// (A7.308), and an armored one is unharmed but its Vulnerable CE crew takes a General Collateral Attack (A7.307, D.8B). Null when
    /// there are none.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public IReadOnlyList<FireVehicle>? Vehicles
    {
        get; init;
    }

    /// <summary>
    /// A vehicle's MA MG firing on the IFT (D1.83, D3.5, unit step 25): no Infantry firers or leader, the MG's FP with its own
    /// modifications. Null for other fire.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public FireVehicleFire? VehicleFire
    {
        get; init;
    }

    /// <summary>Whether the target side is faced with No Quarter (A20.3): its units treat a Heat of Battle Surrender as Berserk; null is false.</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public bool? TargetSideNoQuarter
    {
        get; init;
    }

    /// <summary>
    /// The AFV or wreck whose +1 TEM the target Location's Infantry may claim (D9.3, D10.3; ruling R6.1): the game decides that it is a
    /// non-burning wreck, a friendly AFV, or an abandoned enemy AFV, not in Motion and not under the Case J clause; the package adds it
    /// when the terrain gives no positive TEM and the attack does not come from within the Location. Null when there is none.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? AfvCover
    {
        get; init;
    }

    /// <summary>
    /// The non-rooftop building levels above the target Location, each +1 to an Area Target Type hit's IFT DR, never reversed by a Critical Hit
    /// (B23.32, C3.71, p. 136; pass 60, ruling R60.11). Null is none.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? LevelsAbove
    {
        get; init;
    }

    /// <summary>
    /// The manned Gun whose crew is the attack's target (C11; backlog pass 8, ruling R8.3): whether it is Emplaced, and whether its gunshield
    /// faces the attack. Null when the target Location holds no Gun.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public FireGunTarget? GunTarget
    {
        get; init;
    }

    /// <summary>
    /// Whether a Final KIA makes the weapons its victims held check for Random SW/Gun Destruction (A9.74, C8.42, C11.6, pp. 65, 178, 182; pass 60,
    /// rulings R60.3, R60.17), and a DC destroys the Gun its target Location's units man. Set on every attack planned from the pass on and taken from
    /// the record at replay; null on every older record, whose KIA destroys nothing.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public bool? RandomDestruction
    {
        get; init;
    }

    /// <summary>Whether the firing side is faced with No Quarter, for the Heat of Battle of an FPF NMC (A8.31, A20.3); null is false.</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public bool? FiringSideNoQuarter
    {
        get; init;
    }

    /// <summary>
    /// The owners' answers to the attack's options (ruling R5.8), <c>take</c> or <c>decline</c> by key: <c>battleHardening:&lt;unit&gt;</c>
    /// (A15.3; <c>&lt;unit&gt;:2</c> for a second Heat of Battle DR) and <c>unlikelyKill:&lt;vehicle&gt;</c> (A7.309). Null means every option is
    /// taken as before pass 5; otherwise an option the attack reaches with no answer leaves it undecided.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public IReadOnlyDictionary<string, string>? Choices
    {
        get; init;
    }

    /// <summary>
    /// How many levels the target is above the firer (its base level plus building level, less the firer's; negative below), when
    /// <see cref="SameLevel"/> is false (backlog pass 10, ruling R10.4): PBF doubles the FP at an adjacent target at most one level above
    /// (A7.21). Null at the same level.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? TargetLevelAbove
    {
        get; init;
    }

    /// <summary>
    /// The wall or hedge TEM the targets claim (B9.3, B9.31, B9.33; rulings R10.5 and R10.6): the game decides that the LOS crosses the
    /// hexside into the target hex, that the targets are at the wall's level, that no firer holds Wall Advantage over it, and any
    /// reduction for a higher firer. The package takes it instead of a lower in-hex TEM. Null when none applies.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public FireHexsideTem? HexsideTem
    {
        get; init;
    }

    /// <summary>
    /// Whether the targets are at a higher elevation than every firer and may claim Height Advantage (B10.31; ruling R10.4): +1 TEM
    /// when they have no other positive TEM, which also negates FFMO. The game excludes a mover that crossed a Crest Line through the
    /// hexside the LOS crosses. Null is false.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public bool? HeightAdvantage
    {
        get; init;
    }

    /// <summary>
    /// Whether the Defensive First Fire is a Snap Shot at the hexside the moving stack crossed (A8.15; ruling R10.13): Area Fire, with no
    /// TEM, FFMO, or FFNAM, but with Height Advantage. Null is false.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public bool? SnapShot
    {
        get; init;
    }

    /// <summary>
    /// Whether every MG of the attack fires alone at its Bore Sighted Location (C6.44; pass 45): minus 2 on the IFT DR, against Infantry only in
    /// Defensive First Fire, and never with Spraying Fire, a Snap Shot, or Residual FP. Null is false.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public bool? MgBoreSighted
    {
        get; init;
    }

    /// <summary>
    /// The Low Visibility Hindrance DRM of night and weather (E1.7, E3.1, E3.32; rulings R16.3, R16.11), which the game supplies for each attack it
    /// applies to: cumulative with every other Hindrance, never negating FFMO, never lowering Residual FP. Null is 0.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? LowVisibilityDrm
    {
        get; init;
    }

    /// <summary>
    /// Whether the target Location is beyond the firers' NVR, seen only by its Gunflash (E1.81; ruling R16.2): Area Fire as at a concealed target,
    /// halved once however many reasons apply. Null is false.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public bool? BeyondNvr
    {
        get; init;
    }

    /// <summary>
    /// Whether Mud or Deep Snow cushions an HE attack in Open Ground (E3.62, E3.731; rulings R16.12, R16.13): +1 TEM to an HE hit's Effects DR in
    /// Open Ground, and its Residual FP one counter lower. Null is false.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public bool? CushionedOpenGround
    {
        get; init;
    }

    /// <summary>
    /// Pass 45 (A7.9, A8.2, A8.221, A8.23, A8.26; ruling R45.4): the attack marks the units that cower, shifts two columns for Inexperienced Personnel
    /// by their fact, and leaves the Residual FP its weapons' fates give, held to 12 after its reduction. The planner records it with every attack
    /// from the pass on; an attack recorded before it has none, and replays as it was resolved.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public bool? CoweringMarksAndWeaponResidual
    {
        get; init;
    }

    /// <summary>
    /// Pass 31 (ruling R31.3; A7.81): pinned Infantry fires its MG as Area Fire and keeps no Multiple ROF. The planner records it with every attack
    /// from the pass on; an attack recorded before it has none, and replays as it was resolved.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public bool? PinnedMgAreaFire
    {
        get; init;
    }

    /// <summary>
    /// Referee, pass 31 (A10.2, A11.141: "Units in CC never take LLMC/LLTC"): whether the target Location is in Melee, so the loss of a leader there
    /// causes no Leader Loss check. Recorded with every attack from the pass on; an attack recorded before it has none and replays as it was resolved.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public bool? TargetsInMelee
    {
        get; init;
    }

    /// <summary>How much lower Extreme Winter makes the B# and X# of the firing side's weapons but a DC (E3.741; ruling R16.14): 1 or 2. Null is 0.</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? BreakdownReduction
    {
        get; init;
    }

    /// <summary>
    /// Whether the moving targets are engaged in Hazardous Movement (A4.62; ruling R10.8): -2 to the attack, with no FFMO or FFNAM, unless
    /// pinned. Null is false.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public bool? HazardousMovement
    {
        get; init;
    }

    /// <summary>
    /// Whether the attack is at a Dashing stack in its road Location, from a Location to which neither side of the road is Open Ground (A4.63,
    /// pp. 51 to 52; pass 40): Area Fire for the DEFENDER's non-ordnance Defensive First Fire. Null is false.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public bool? Dash
    {
        get; init;
    }

    /// <summary>
    /// The attack penalty of the firers' side for overstacking (A5.12, p. 52; pass 40): +1 to the IFT DR for each vehicle and squad-equivalent (FRU)
    /// above the limits in the Location fired from, the largest of a group's Locations (A7.52). Null when none.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? FirerOverstack
    {
        get; init;
    }

    /// <summary>
    /// The defense penalty of the targets' side for overstacking (A5.131, p. 52; pass 40): -1 to the IFT DR for each squad-equivalent (FRU) above the
    /// Personnel limit in the target Location. Null when none.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? TargetOverstack
    {
        get; init;
    }

    /// <summary>
    /// A vehicle's OVR of the Location it entered (D7.1 to D7.17; backlog pass 11, ruling R11.11): no Infantry firers or leader; the package
    /// computes its FP from the vehicle's definition. Null for other fire.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public FireOverrun? Overrun
    {
        get; init;
    }

    /// <summary>Spraying Fire at this Location and one other sharing a hexside, on one Original DR (A9.5; backlog pass 12, ruling R12.6): Area Fire; null is false.</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public bool? SprayingFire
    {
        get; init;
    }

    /// <summary>
    /// The second Location of Spraying Fire (A9.5; ruling R12.6): its record shares the first's Original DR, and the first alone decides the MGs'
    /// Multiple ROF and malfunction and marks the firers; null is false.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public bool? SprayShare
    {
        get; init;
    }

    /// <summary>A Fire Lane's Residual FP (A9.22, A9.222; ruling R12.7): never reduced by DRM, no CX, leader, or hero DRM, and no Cowering; null is false.</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public bool? FireLane
    {
        get; init;
    }

    /// <summary>
    /// The nationalities of the firing side, when it holds Allied Troops (A10.7; backlog pass 15, ruling R15.8): its units of any of them may fire
    /// together, and a leader directing or aiding one of another nationality does so one worse. Null is the first firer's nationality alone.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public IReadOnlyList<string>? FiringNationalities
    {
        get; init;
    }

    /// <summary>A DC's attack on one Location (A23; rulings R15.2, R15.3): no firers or leader, 30 FP; null for other fire.</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public FireDemolitionCharge? DemolitionCharge
    {
        get; init;
    }

    /// <summary>
    /// The AFV in the target Location a FT, MOL, or DC attack is predesignated against (A22.34, A22.612, A23.5, p. 89 to 91; pass 60, ruling R60.15): its
    /// state facts and the Target Facing of the hexside the attack crosses (none from within its Location, which is the rear). Null for every other
    /// attack, and in every record made before the pass.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public OrdnanceVehicleTarget? TargetVehicle
    {
        get; init;
    }
}

/// <summary>
/// A DC's attack (A23.1 to A23.63; rulings R15.2, R15.3): the DC and the unit that Placed or Threw it, the attack's kind (<c>placed</c> in the AFPh,
/// <c>thrown</c> at the target Location, or <c>thrower</c> at the thrower's own Location), whether its user was CX when it Placed or Threw it,
/// whether the DC is captured, whether every target was concealed when a Placed DC was operably Placed, and whether a Thrower in the AFPh is an
/// Opportunity Firer.
/// </summary>
public sealed record FireDemolitionCharge(
    string? EquipmentId,
    string? DefinitionId,
    string? Mode,
    string? UserId,
    string? UserDefinitionId,
    bool? Cx,
    bool? Captured,
    bool? ConcealedWhenPlaced,
    bool? OpportunityFire)
{
    public const string Placed = "placed";
    public const string Thrown = "thrown";
    public const string Thrower = "thrower";

    /// <summary>Whether its Green user is Inexperienced (A19.32; referee, pass 15): its malfunction number is one lower; null is false.</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public bool? Inexperienced
    {
        get; init;
    }
}

/// <summary>
/// An OVR (D7.1, D7.11, D7.17; ruling R11.11): the vehicle, its definition, its Location, whether its crew is CE (for an AAMG, D5.3), whether it
/// became Immobile before the OVR resolves (a Bog: the FP is halved, D7.11), and which of its weapons are malfunctioned or disabled.
/// </summary>
public sealed record FireOverrun(
    string? VehicleId,
    string? DefinitionId,
    string? LocationId,
    bool? CrewExposed,
    bool? Immobile,
    bool? MainArmamentMalfunctioned,
    bool? BmgMalfunctioned,
    bool? CmgMalfunctioned)
{
    /// <summary>Whether the vehicle is under a Stun "+1" counter (D5.34, p. 203; pass 35, task 35.13 d): one more on its OVR DR; null is false.</summary>
    [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
    public bool? StunRecovery
    {
        get; init;
    }

    /// <summary>
    /// The leadership modifier of the vehicle's Armor Leader on its OVR DR (D7.16, D3.44, pp. 207 and 200; pass 55, ruling R55.16), or the +1
    /// of an Inexperienced Crew (D3.45); null is none.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? ArmorLeadership
    {
        get; init;
    }
}

/// <summary>
/// What an OVR's Original IFT DR of 12 did (D7.17; ruling R11.11): the weapon it malfunctioned (<c>ma</c>, <c>bmg</c>, or <c>cmg</c>; the halftrack's MA
/// is its AAMG), chosen by Random Selection when several added FP, or the vehicle immobilized when none that added FP can malfunction.
/// </summary>
public sealed record FireOverrunEffect(string VehicleId, IReadOnlyList<string> MalfunctionedWeapons, bool Immobilized)
{
    public const string MainArmament = "ma";
    public const string BowMg = "bmg";
    public const string CoaxialMg = "cmg";
}

/// <summary>The wall (+2) or hedge (+1) TEM a target claims (B9.3), after any reduction for a higher firer (B9.33).</summary>
public sealed record FireHexsideTem(string? Terrain, int? Tem);

/// <summary>A vehicle in the target Location, with its reviewed catalog definition and its crew's state (D5.2, D5.3, D5.34).</summary>
public sealed record FireVehicle(
    string? VehicleId,
    string? DefinitionId,
    string? LocationId,
    bool? CrewExposed,
    bool? Stunned,
    bool? StunRecovery,
    bool? Immobilized)
{
    /// <summary>Whether the vehicle is concealed (A12.2; ruling R6.7): it is attacked on the column of the halved FP (A12.13); null is false.</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public bool? Concealed
    {
        get; init;
    }

    /// <summary>
    /// The Morale Level of the vehicle's crew when it is its Armor Leader's (D3.42, p. 200; pass 55, ruling R55.17) or an Inexperienced Crew's 6
    /// (D3.45). Null is its nationality's (D5.1); a record keeps it only when the attack made the crew check against it (D3.41).
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? CrewMorale
    {
        get; init;
    }

    /// <summary>
    /// The vehicle's Passengers and Riders (D6.1, D6.2; pass 65, ruling R65.5), empty when nobody is aboard. Written by every attack planned since the
    /// pass and taken from the record at replay; null on every older record, whose attack reaches no PRC (and an AFV in a positive TEM is refused,
    /// ruling R25.6) as before.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public IReadOnlyList<FirePrc>? Prc
    {
        get; init;
    }
}

/// <summary>
/// A Passenger or Rider of a vehicle (D6.1, D6.2; pass 65, ruling R65.5): the unit, its definition, its role, and the states its Vulnerability (D6.61,
/// D6.62) and its checks read.
/// </summary>
public sealed record FirePrc(string? UnitId, string? DefinitionId, string? Role, bool? Broken, bool? Pinned)
{
    public const string Passenger = "passenger";
    public const string Rider = "rider";

    /// <summary>Whether a SMC aboard is wounded (A17.2); null is false.</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public bool? Wounded
    {
        get; init;
    }

    /// <summary>Whether the unit is Fanatic (A10.8); null is false.</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public bool? Fanatic
    {
        get; init;
    }

    /// <summary>The unit's ELR (A19.1): its OB group's, or its side's.</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? Elr
    {
        get; init;
    }
}

/// <summary>
/// A Collateral Attack on a vehicle's Vulnerable PRC (D.8, D.8A, D.8B, p. 192; pass 65, ruling R65.7): <c>general</c> or <c>specific</c>, its DRM (TEM or
/// the CE DRM, never both), its Final DR and IFT result, and what it did to each unit, as an attack's target units are recorded.
/// </summary>
public sealed record FirePrcAttack(string Kind, IReadOnlyList<FireModifier> Drm, int FinalDr, string Result, IReadOnlyList<FireUnitEffect> Effects)
{
    public const string General = "general";
    public const string Specific = "specific";
}

/// <summary>A Passenger's or Rider's Survival DR (D6.9, D5.6, pp. 207, 204; pass 65, ruling R65.9): +1 if broken, against the CS# or cs#.</summary>
public sealed record PrcSurvival(string UnitId, IReadOnlyList<int> Dice, int Drm, int FinalDr, int CrewSurvival, bool Survived);

/// <summary>
/// A vehicle's MA MG attack (D1.83, D3.5): the vehicle, its definition, and the facts that modify its FP and DR: Motion (D2.42), a
/// pinned crew (A7.82), a Stun +1 (D5.34), whether its crew is CE (D5.3), and whether it fires again on its Multiple ROF (C2.24, A9.2).
/// </summary>
public sealed record FireVehicleFire(
    string? VehicleId,
    string? DefinitionId,
    string? LocationId,
    bool? CrewExposed,
    bool? InMotion,
    bool? Pinned,
    bool? Stunned,
    bool? StunRecovery,
    bool? Malfunctioned,
    bool? FiredThisPlayerTurn,
    bool? RateOfFireShot)
{
    /// <summary>
    /// The BMG and CMG this attack fires (D1.81, D1.82, D3.5, pp. 195 and 200; pass 50), each once a Player Turn, added together for one attack
    /// or fired apart. Null for an attack by the vehicle's MA MG, which is every record made before pass 50.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public IReadOnlyList<FireVehicleWeapon>? Weapons
    {
        get; init;
    }

    /// <summary>
    /// Whether a weapon of the vehicle fired in another fire phase of this Player Turn (D3.5, p. 201; pass 50): "the remaining weapons must fire
    /// in the same phase or forfeit their non-CC fire attack opportunity for that Player Turn". Defensive First Fire and Final Fire are one
    /// (A.15, p. 44). Null is false.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public bool? OtherPhase
    {
        get; init;
    }

    /// <summary>Whether the vehicle is in woods, a building, or rubble (C5.11, p. 172; pass 50): the Case A DRM is doubled. A map read, recorded with the attack; null is false.</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public bool? InWoodsOrBuilding
    {
        get; init;
    }

    /// <summary>
    /// The leadership modifier of the vehicle's Armor Leader on the IFT DR of a MA that is a MG (D3.44, p. 200; pass 55, ruling R55.16), or the
    /// +1 of an Inexperienced Crew (D3.45). Never with <see cref="Weapons"/>: "an Armor Leader cannot modify non-OVR MG/FT (unless MA) attacks".
    /// Null is none.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? ArmorLeadership
    {
        get; init;
    }
}

/// <summary>
/// A BMG or CMG of a firing vehicle (D1.81, D1.82, D3.5, D3.7; pass 50): which one, whether it has malfunctioned, and whether it has fired
/// this Player Turn.
/// </summary>
public sealed record FireVehicleWeapon(string? Weapon, bool? Malfunctioned, bool? Fired)
{
    public const string BowMg = "bmg";
    public const string CoaxialMg = "cmg";

    /// <summary>
    /// The hexspines its CA must change to cover the target (D3.52, C5.1; a map read, recorded with the attack): the turret turns for a CMG
    /// and the DRM of Case A is added to the IFT DR; a BMG fires only inside the VCA, so more than none puts its attack outside.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? Turn
    {
        get; init;
    }

    /// <summary>
    /// The Case A DRM the first turret-mounted weapon to fire in this fire phase paid (D3.51, p. 201): a CMG that fires within the current
    /// TCA pays the same. Null is none.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? MaintainedCaseA
    {
        get; init;
    }
}

/// <summary>An ordnance hit's IFT attack (C.6, C3.71): the Gun, its HE FP column, and whether the hit is Critical.</summary>
public sealed record FireOrdnanceHit(string? GunId, int? Firepower, bool? CriticalHit)
{
    /// <summary>
    /// Whether the hit is on the Area Target Type (C3.33, C3.331; ruling R9.3): its TEM applies to the Effects DR, woods giving the -1 of Air
    /// Bursts instead of +1 (B13.3) and a Critical Hit reversing only positive TEM; null is the Infantry Target Type.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public bool? Area
    {
        get; init;
    }

    /// <summary>
    /// Whether the target claimed Height Advantage on the To Hit DR of a Direct Fire Critical Hit that reverses it to -1 on the IFT DR (C3.71, B10.31;
    /// pass 60, ruling R60.14); null is not.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public bool? HeightAdvantage
    {
        get; init;
    }
}

/// <summary>
/// A Canister shot's attack on one Location (C8.4, C8.41, p. 178; pass 55, ruling R55.9): the Gun and the FP of its Gun Size (12, 20, or 24). Each
/// of the facts below halves the FP, and they multiply, as C8.4's "halved again" has it; a concealed target halves it once more on the IFT.
/// </summary>
public sealed record FireCanister(string? GunId, int? Firepower)
{
    /// <summary>
    /// C8.41: 1 when the Location is the hex adjacent to the firer along the LOS, attacked "as Area Fire with no doubling for PBF"; 2 when the LOS
    /// runs exactly along the hexside of two adjacent hexes, each of which is "quartered instead of halved". Null for a hex of the cluster.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? AdjacentHexes
    {
        get; init;
    }

    /// <summary>Whether the firer uses Bounding (First) Fire (C8.4, D3.31, p. 200): halved. Null is false.</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public bool? BoundingFirstFire
    {
        get; init;
    }

    /// <summary>Whether the firer is in Motion or not stopped (C8.4, D2.42, p. 198): halved, and quartered with Bounding First Fire. Null is false.</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public bool? Motion
    {
        get; init;
    }

    /// <summary>Whether the shot is Intensive Fire (C8.4, C5.6): halved. Null is false.</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public bool? IntensiveFire
    {
        get; init;
    }

    /// <summary>Whether the crew is pinned (A7.81, p. 58): "Pinned Infantry fires MG/IFE/Canister as Area Fire". Null is false.</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public bool? PinnedCrew
    {
        get; init;
    }

    /// <summary>
    /// The To Hit DRM of Case A for the hexspines the CA changed for the shot (C3.21, D3.52, C5.11), added to the IFT DR: "hexspine changes in
    /// the CA" (C8.4). Null is none.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? CaseA
    {
        get; init;
    }
}

/// <summary>How a moving target stack moved in the MPh: Assault Movement avoids FFNAM but not FFMO (A4.61).</summary>
public sealed record FireMovement(bool? AssaultMovement);

/// <summary>A MG a firer uses in the attack (A7.35, A9.1), with its reviewed catalog definition.</summary>
public sealed record FireWeapon(string? EquipmentId, string? DefinitionId, bool? Malfunctioned, bool? FiredThisPlayerTurn, bool? FirstFireMarked)
{
    /// <summary>Whether the MG is an enemy nationality's, Recovered by its firer (A21.1, A21.11, A21.12; ruling R13.7): B# two lower, Multiple ROF one lower; null is false.</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public bool? Captured
    {
        get; init;
    }
}

/// <summary>A unit of the fire group, with its reviewed catalog definition.</summary>
/// <summary>
/// A manned Gun in the target Location (C11.2, C11.5; ruling R8.3): its crew, whether it is Emplaced (set up manned and never moved), and
/// whether its gunshield protects the crew from this attack (an AT or INF Gun, the attack from within its CA and not from its own hex).
/// </summary>
public sealed record FireGunTarget(string? GunId, string? DefinitionId, string? CrewUnitId, bool? Emplaced, bool? Gunshield);

/// <summary>
/// A SW or Gun a target unit holds (A9.74, A9.8; pass 60, ruling R60.17): possessed, or a Gun it mans; whether it is malfunctioned. A dm SW is a SW.
/// </summary>
public sealed record FireHeldWeapon(string? EquipmentId, string? Kind, string? DefinitionId)
{
    /// <summary>Whether the weapon is malfunctioned; null is false.</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public bool? Malfunctioned
    {
        get; init;
    }
}

/// <summary>
/// What a KIA did to a weapon its victim held (A9.74; pass 60, ruling R60.17), or a DC to the Gun of its target Location (C11.6): the dr read on the
/// attack's column (null for a DC's Gun, read by the attack's own result), the IFT result, and the fate: <c>destroyed</c>, <c>malfunctioned</c>, or
/// <c>none</c>.
/// </summary>
public sealed record FireWeaponDestruction(string EquipmentId, string HolderId, string Result, string Fate, string Rule)
{
    /// <summary>The dr read on the attack's column; null for a DC's Gun.</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? Dr
    {
        get; init;
    }
}

public sealed record FireFirer(
    string? UnitId,
    string? DefinitionId,
    string? LocationId,
    bool? Broken,
    bool? Pinned,
    bool? Concealed,
    bool? FiredThisPlayerTurn,
    bool? UsesSupportWeapon)
{
    /// <summary>
    /// The unit's own ELR, from its OB group (A19.1; backlog pass 18, ruling R18.3); null to take the side's ELR the attack declares.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? Elr
    {
        get; init;
    }

    /// <summary>
    /// Whether a Good Order enemy ground unit within 16 hexes has a LOS to this concealed unit as it fires or directs (A12.14, read in the PDF, p. 77;
    /// pass 31d, ruling R31d.2), read by the planner from the game and the map. Null in every attack recorded before, and at night: the package then
    /// decides from the target Location alone, as it did.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public bool? SeenByGoodOrderEnemy
    {
        get; init;
    }

    /// <summary>Whether this crew fired its Gun this Player Turn, which costs it its inherent FP (A7.352; ruling R8.4); null is false.</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public bool? GunFired
    {
        get; init;
    }

    /// <summary>The range from this firer's Location, when the group spans Locations (A7.52); otherwise the attack's.</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? Range
    {
        get; init;
    }

    /// <summary>Whether this firer is at the target's level, when the group spans Locations; otherwise the attack's.</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public bool? SameLevel
    {
        get; init;
    }

    /// <summary>How many levels the target is above this firer, when the group spans Locations and this firer is at another level (ruling R10.4).</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? TargetLevelAbove
    {
        get; init;
    }

    /// <summary>The LOS from this firer's Location, when the group spans Locations; otherwise the attack's.</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public FireLos? Los
    {
        get; init;
    }

    /// <summary>Marked with a First Fire counter this MPh (A8.1); null is false.</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public bool? FirstFireMarked
    {
        get; init;
    }

    /// <summary>Marked with a Final Fire counter (A8.3, A8.31); null is false.</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public bool? FinalFireMarked
    {
        get; init;
    }

    /// <summary>The MGs the firer uses (A7.35); null is none.</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public IReadOnlyList<FireWeapon>? Weapons
    {
        get; init;
    }

    /// <summary>Whether the firer adds its inherent FP (A7.351); null is true. False for a MG firing again on its Multiple ROF.</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public bool? UsesInherentFp
    {
        get; init;
    }

    /// <summary>Whether the firer is Fanatic (A10.8): no Cowering, and a higher Morale Level in an FPF NMC; null is false.</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public bool? Fanatic
    {
        get; init;
    }

    /// <summary>Whether a hero firer is wounded (A15.2: 1-3-8); null is false.</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public bool? Wounded
    {
        get; init;
    }

    /// <summary>Whether the firer is berserk (A7.54, A7.9; pass 45): it joins no multi-Location group and never cowers; null is false.</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public bool? Berserk
    {
        get; init;
    }

    /// <summary>
    /// Whether a Green or Conscript firer is Inexperienced (A19.2), for the Heat of Battle DRM of an FPF NMC, and for its FT's removal number, one lower
    /// (A19.32; referee, pass 15); required for those classes in FPF.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public bool? Inexperienced
    {
        get; init;
    }

    /// <summary>Whether a Known enemy unit is in the firer's LOS (A15.44), the planner's read; needed when an FPF NMC can reach Heat of Battle.</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public bool? KnownEnemyInLos
    {
        get; init;
    }

    /// <summary>The ADJACENT Known Good Order armed enemy Infantry the firer may surrender to (A15.5), the planner's read; needed as above.</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public IReadOnlyList<string>? Captors
    {
        get; init;
    }

    /// <summary>Whether the firer is CX (A4.51): +1 to the attack's IFT DR; null is false.</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public bool? Cx
    {
        get; init;
    }

    /// <summary>An Opportunity Firer in its AFPh (A7.25; ruling R12.1): no AFPh halving, and its MG keeps Multiple ROF; null is false.</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public bool? OpportunityFire
    {
        get; init;
    }

    /// <summary>Whether the firer is Encircled (A7.7; ruling R12.11): +1 to the attack's IFT DR; null is false.</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public bool? Encircled
    {
        get; init;
    }

    /// <summary>The other SMC stacked with this leader who fire his one MG together at full FP (A9.12; ruling R12.4); null when he fires it alone, as Area Fire.</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Partner
    {
        get; init;
    }

    /// <summary>Whether this firer makes the attack's MOL Check (A22.611; ruling R15.4): at most one per attack; null is false.</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public bool? Mol
    {
        get; init;
    }
}

/// <summary>The leader directing the fire group (A7.53, A7.531).</summary>
public sealed record FireDirector(
    string? UnitId,
    string? DefinitionId,
    string? LocationId,
    bool? Broken,
    bool? Pinned,
    bool? Concealed,
    bool? DirectedThisPlayerTurn,
    bool? Wounded)
{
    /// <summary>
    /// Whether a Good Order enemy ground unit within 16 hexes has a LOS to this concealed unit as it fires or directs (A12.14, read in the PDF, p. 77;
    /// pass 31d, ruling R31d.2), read by the planner from the game and the map. Null in every attack recorded before, and at night: the package then
    /// decides from the target Location alone, as it did.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public bool? SeenByGoodOrderEnemy
    {
        get; init;
    }

    /// <summary>
    /// Whether the leader directed, earlier this phase, every MG this attack fires (A7.53, p. 57; pass 45): "a leader can direct the fire of a MG as
    /// many times as the MG can fire, even if he also directed other units as part of a FG in the MG's previous attack". The planner's read of the
    /// phase's records, recorded with the attack; null is false.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public bool? DirectedTheseMgs
    {
        get; init;
    }

    /// <summary>Whether the directing leader is CX (A4.51): +1 to the attack's IFT DR; null is false.</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public bool? Cx
    {
        get; init;
    }
}

/// <summary>The LOS of unit step 16 from the firers' Location to the target Location.</summary>
public sealed record FireLos(bool? Blocked, int? HindranceDrm, bool? HindranceAttributed, bool? GrainInLos);

/// <summary>A unit in the target Location.</summary>
public sealed record FireTarget(
    string? UnitId,
    string? DefinitionId,
    string? LocationId,
    bool? Broken,
    bool? Pinned,
    bool? Concealed,
    bool? Hidden,
    bool? Dummy,
    bool? Wounded,
    bool? Disrupted)
{
    /// <summary>
    /// The unit's own ELR, from its OB group (A19.1; backlog pass 18, ruling R18.3); null to take the side's ELR the attack declares.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? Elr
    {
        get; init;
    }

    /// <summary>Whether the unit is Fanatic (A10.8): both Morale Levels one higher, never Disrupted; null is false.</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public bool? Fanatic
    {
        get; init;
    }

    /// <summary>Whether a leader is heroic (A15.21): he is wounded, not broken, by a failed MC; null is false.</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public bool? Heroic
    {
        get; init;
    }

    /// <summary>Whether a Green or Conscript MMC is Inexperienced (A19.2), for the Heat of Battle DRM; required for those classes.</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public bool? Inexperienced
    {
        get; init;
    }

    /// <summary>
    /// Whether the unit is berserk (A15.42): Morale Level 10, Casualty Reduction instead of breaking, never pinned, no leader
    /// loss checks, and no leadership from a friendly leader; null is false.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public bool? Berserk
    {
        get; init;
    }

    /// <summary>Whether a Known enemy unit is in the unit's LOS (A15.44), the planner's read; needed before the attack when Heat of Battle can reach it.</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public bool? KnownEnemyInLos
    {
        get; init;
    }

    /// <summary>The ADJACENT Known Good Order armed enemy Infantry the unit may surrender to (A15.5), the planner's read; needed as above.</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public IReadOnlyList<string>? Captors
    {
        get; init;
    }

    /// <summary>Whether the unit is Encircled (A7.7; ruling R12.11): its Morale Level is one lower against this attack; null is false.</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public bool? Encircled
    {
        get; init;
    }

    /// <summary>Whether the unit is of the firing side, attacked in a Melee Location or as a prisoner (A11.15, A20.54; rulings R12.8, R12.9); null is false.</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public bool? Friendly
    {
        get; init;
    }

    /// <summary>The Guard of this prisoner (A20.54; ruling R12.9): never broken, Casualty Reduced on a failed MC, pinned only with its Guard, and no LLMC or LLTC; null when it is no prisoner.</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? GuardId
    {
        get; init;
    }

    /// <summary>The FT the unit possesses (A22.4; ruling R15.1): each is -1 to the attack's DR against it alone; null is none.</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? Flamethrowers
    {
        get; init;
    }

    /// <summary>
    /// The SW and Guns the unit holds (A9.74; pass 60, ruling R60.17), read from the state only when the attack carries
    /// <see cref="FireAttack.RandomDestruction"/>; null is none. Never written to a record (boundary review, pass 60): both sides read the record,
    /// and a concealed or hidden unit's weapons are not theirs to know; the planner and the verifier read them from the state again.
    /// </summary>
    [JsonIgnore]
    public IReadOnlyList<FireHeldWeapon>? HeldWeapons
    {
        get; init;
    }
}

/// <summary>
/// Recorded rolls. <see cref="Attack"/> is the IFT DR; <see cref="RandomSelection"/> holds one dr per target unit;
/// <see cref="Checks"/> holds the MC or NTC DR of a target unit; <see cref="LeaderLoss"/> holds its LLMC or LLTC DR;
/// <see cref="WoundSeverity"/> holds a wounded leader's Wound Severity dr (A17.11).
/// </summary>
public sealed record FireRolls(
    IReadOnlyList<int>? Attack,
    IReadOnlyDictionary<string, int>? RandomSelection,
    IReadOnlyDictionary<string, IReadOnlyList<int>>? Checks,
    IReadOnlyDictionary<string, IReadOnlyList<int>>? LeaderLoss,
    IReadOnlyDictionary<string, int>? WoundSeverity = null)
{
    /// <summary>One dr per MG whose B# the IFT DR reached, when two or more did (A9.71).</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public IReadOnlyDictionary<string, int>? WeaponSelection
    {
        get; init;
    }

    /// <summary>One dr per firer that can cower, when a fire group of two or more of them cowers (A7.9; pass 45): the highest is marked, ties with it.</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public IReadOnlyDictionary<string, int>? CowerSelection
    {
        get; init;
    }

    /// <summary>One dr per FPF firer, when a Casualty MC falls on two or more of them (A8.31).</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public IReadOnlyDictionary<string, int>? FirerSelection
    {
        get; init;
    }

    /// <summary>The Heat of Battle DR of each unit whose Original MC DR was 2 (A15.1).</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public IReadOnlyDictionary<string, IReadOnlyList<int>>? HeatOfBattle
    {
        get; init;
    }

    /// <summary>The NTC DR of each friendly unit a berserk leader tries to take berserk with him (A15.41).</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public IReadOnlyDictionary<string, IReadOnlyList<int>>? BerserkChecks
    {
        get; init;
    }

    /// <summary>The MC or NTC DR of each armored vehicle's Vulnerable crew in a General Collateral Attack (D.8B, D5.34).</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public IReadOnlyDictionary<string, IReadOnlyList<int>>? CrewChecks
    {
        get; init;
    }

    /// <summary>The Unlikely Kill dr of each unarmored vehicle whose Original IFT DR of 2 did not otherwise harm it (A7.309).</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public IReadOnlyDictionary<string, int>? UnlikelyKill
    {
        get; init;
    }

    /// <summary>The MOL Check dr (A22.611; ruling R15.4), made before the IFT DR.</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? MolCheck
    {
        get; init;
    }

    /// <summary>The Position DR of a DC at an AFV (C7.346; pass 60, ruling R60.16).</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public IReadOnlyList<int>? Position
    {
        get; init;
    }

    /// <summary>The Crew Survival DR of an AFV a FT, MOL, or DC eliminated (D5.6; pass 60).</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public IReadOnlyList<int>? CrewSurvival
    {
        get; init;
    }

    /// <summary>The Immobilization TC of the crew of an AFV a DC immobilized (D5.5; pass 60).</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public IReadOnlyList<int>? TaskCheck
    {
        get; init;
    }

    /// <summary>The MC or NTC DR of the crew of an AFV a FT, MOL, or DC attack names, in its Specific Collateral Attack (D.8A; pass 60).</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public IReadOnlyList<int>? CollateralCheck
    {
        get; init;
    }

    /// <summary>The Random SW/Gun Destruction dr of each weapon a unit eliminated by a Final KIA held (A9.74; pass 60, ruling R60.17).</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public IReadOnlyDictionary<string, int>? WeaponDestruction
    {
        get; init;
    }

    /// <summary>The Survival DR of each Passenger and Rider of a vehicle the attack eliminated (D6.9; pass 65, ruling R65.9), by unit id.</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public IReadOnlyDictionary<string, IReadOnlyList<int>>? PrcSurvival
    {
        get; init;
    }
}

/// <summary>One modifier with its value and rule.</summary>
public sealed record FireModifier(string Name, decimal Value, string Rule);

/// <summary>One firer's FP arithmetic.</summary>
public sealed record FirerFirepower(string UnitId, int Printed, IReadOnlyList<FireModifier> Multipliers, decimal Firepower)
{
    /// <summary>The unit operating this MG (A7.35); null for a unit's inherent FP.</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Operator
    {
        get; init;
    }

    /// <summary>Whether this FP attacks the concealed targets of a mixed Location (A12.13); null for all targets.</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public bool? VsConcealed
    {
        get; init;
    }
}

/// <summary>The attack's arithmetic, as A7.3 resolves it.</summary>
public sealed record FireArithmetic(
    IReadOnlyList<FirerFirepower> Firers,
    decimal TotalFirepower,
    int? UnshiftedColumnFp,
    int ColumnShift,
    bool Cowered,
    int? ColumnFp,
    IReadOnlyList<int> Dice,
    int OriginalDr,
    IReadOnlyList<FireModifier> Drm,
    int FinalDr,
    string Result)
{
    /// <summary>
    /// The column and result for the concealed targets of a Location that also holds known ones: the same DR on the
    /// column their halved FP gives (A12.13). Null when every target is of one kind.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public FireColumn? Concealed
    {
        get; init;
    }

    /// <summary>The Residual FP the attack leaves in the target Location (A8.2, A8.26, A7.372); null when it leaves none.</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? ResidualFp
    {
        get; init;
    }

    /// <summary>
    /// The Residual FP the attack would leave if the weapons that kept their Multiple ROF gave it up (A8.23, p. 60; pass 45): "The decision to
    /// forfeit possible remaining First Fire capability in order to leave Residual FP must be made immediately after resolving its attack." Given
    /// only when it is more than <see cref="ResidualFp"/>, which counts the other firers alone.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? ResidualFpIfForfeited
    {
        get; init;
    }

    /// <summary>The Final DR of the pinned movers of a stack that also holds unpinned ones: the same DR without FFNAM, FFMO, or Hazardous Movement (A7.83; ruling R12.3); null otherwise.</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? PinnedFinalDr
    {
        get; init;
    }

    /// <summary>The result for the pinned known movers on that Final DR; null otherwise.</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? PinnedResult
    {
        get; init;
    }

    /// <summary>The result for the pinned concealed movers on that Final DR; null otherwise.</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? PinnedConcealedResult
    {
        get; init;
    }
}

/// <summary>A second column of one attack: its total FP, the column before and after Cowering, and the IFT result.</summary>
public sealed record FireColumn(decimal TotalFirepower, int? UnshiftedColumnFp, int? ColumnFp, string Result);

/// <summary>What the attack did to a MG: malfunction (A9.7, A9.71), Multiple ROF kept (A9.2), Sustained Fire (A9.3), and its fire counter.</summary>
public sealed record FireWeaponEffect(string EquipmentId, int BreakdownNumber, bool Malfunctioned, bool RateOfFireRetained, bool SustainedFire,
    string? FireCounter, int? SelectionDr)
{
    /// <summary>
    /// Whether the weapon is removed for good (A.11, p. 43; A9.7, p. 65; pass 45): a fire that lowers the B# "also transforms the weapon's Original
    /// B# to an X# during that use", and a DR at least the X# removes it. Null is false.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public bool? Removed
    {
        get; init;
    }

    /// <summary>
    /// Which of a vehicle's MGs this is (D3.7, p. 201; pass 50): <c>bmg</c> or <c>cmg</c>, with the vehicle's id as the equipment. Null for an
    /// Infantry weapon and for a vehicle's MA MG.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? VehicleWeapon
    {
        get; init;
    }
}

/// <summary>A MC, NTC, LLMC, or LLTC taken by a target unit.</summary>
public sealed record FireCheck(
    string Kind,
    IReadOnlyList<int> Dice,
    int OriginalDr,
    IReadOnlyList<FireModifier> Drm,
    int FinalDr,
    int MoraleLevel,
    bool Passed,
    string Consequence)
{
    /// <summary>
    /// Whose Morale Level a vehicle crew checked at when it is not its nationality's (D3.42, D3.45, p. 200; pass 55, rulings R55.17, R55.18):
    /// <c>armor-leader</c> or <c>inexperienced-crew</c>. Null for every other check, and in every record made before pass 55.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? MoraleSource
    {
        get; init;
    }
}

/// <summary>What the attack did to one target unit.</summary>
public sealed record FireUnitEffect(
    string UnitId,
    string DefinitionId,
    string FinalDefinitionId,
    int? RandomSelectionDr,
    bool Eliminated,
    bool Broken,
    bool Pinned,
    bool Wounded,
    bool Disrupted,
    bool ConcealmentLost,
    IReadOnlyList<string> Events,
    IReadOnlyList<FireCheck> Checks)
{
    /// <summary>The unit's Heat of Battle DR and results (A15.1); null when it rolled none.</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public HeatOfBattleOutcome? HeatOfBattle
    {
        get; init;
    }

    /// <summary>The unit's second Heat of Battle DR in the attack, after a second Original MC DR of 2 (A15.1, ruling R5.10); null when none.</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public HeatOfBattleOutcome? SecondHeatOfBattle
    {
        get; init;
    }

    /// <summary>Whether the unit is Fanatic after the attack (A10.8); null when it is not.</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public bool? Fanatic
    {
        get; init;
    }

    /// <summary>Whether a leader is heroic after the attack (A15.21); null when he is not.</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public bool? Heroic
    {
        get; init;
    }

    /// <summary>Whether the unit is berserk after the attack (A15.4, A15.41); null when it is not.</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public bool? Berserk
    {
        get; init;
    }

    /// <summary>
    /// Whether a squad with an underscored Morale Factor was Replaced by two broken HS of <see cref="FinalDefinitionId"/> (A19.13; ruling R15.9); null
    /// when it was not.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public bool? SplitIntoHalfSquads
    {
        get; init;
    }
}

/// <summary>
/// One MG of an attack against its Bore Sighting (C6.42, C6.43; pass 45): whether it has one, whether its firer is the unit that set up with it,
/// whether it stands in its setup Location, whether the sighting is on the target Location, and whether it is dismantled.
/// </summary>
public sealed record MgSightingFacts(bool Sighted, bool ByItsPossessor, bool AtItsSetupLocation, bool OnTheTarget, bool Dismantled);

/// <summary>
/// A DC's Position DR at an AFV (C7.346, p. 177; pass 60, ruling R60.16): the dice, the DRM, the Final DR, the hit's Location (C3.9: a colored dr below
/// the white strikes a turret), and the result: <c>optimal</c> (5 or less, the Aerial AF), <c>positioned</c> (6 to 8, the AF), <c>poor</c> (9 to 11, the
/// crew alone), or <c>none</c> (12 or more, the other units alone).
/// </summary>
public sealed record FirePosition(IReadOnlyList<int> Dice, int OriginalDr, IReadOnlyList<FireModifier> Drm, int FinalDr, string HitLocation, string Result)
{
    public const string Optimal = "optimal";
    public const string Positioned = "positioned";
    public const string Poor = "poor";
    public const string None = "none";
}

/// <summary>The MOL Check of an attack (A22.611, A22.6111; ruling R15.4): its dr, drm, and pass, and whether the MOL user broke on a colored dr of 6.</summary>
public sealed record FireMolCheck(string UnitId, int Dr, IReadOnlyList<FireModifier> Drm, int Final, bool Passed, bool UserBroken);

/// <summary>The Fire package's answer: resolved with its arithmetic and effects, or Abstained or Indeterminate with reasons.</summary>
public sealed record FireResolution(
    string Disposition,
    IReadOnlyList<string> Reasons,
    FireArithmetic? Arithmetic,
    IReadOnlyList<FireUnitEffect> Effects,
    IReadOnlyList<string> FireCounterUnitIds,
    string? FireCounter,
    IReadOnlyList<string> FirerConcealmentLost)
{
    /// <summary>
    /// The firers whose own counter differs from <see cref="FireCounter"/> (A8.31; pass 45, ruling R45.1): a unit with no counter that fires First
    /// Fire in one group with Subsequent First Fire or FPF firers. Null when every firer takes the attack's counter.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public IReadOnlyDictionary<string, string>? FirerCounters
    {
        get; init;
    }

    /// <summary>
    /// The units that cowered (A7.9, p. 58; pass 45): "any unit that cowers (as well as all of its SW regardless of whether it was using its inherent
    /// FP) is automatically marked with a Final or Prep Fire counter", one or more of a fire group by Random Selection. Null when none did.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public IReadOnlyList<string>? CoweredUnitIds
    {
        get; init;
    }

    /// <summary>What the attack did to each MG used in it; null when none was.</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public IReadOnlyList<FireWeaponEffect>? WeaponEffects
    {
        get; init;
    }

    /// <summary>The NMC FPF inflicts on its firers and directing leader (A8.31); null for other fire.</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public IReadOnlyList<FireUnitEffect>? FirerEffects
    {
        get; init;
    }

    /// <summary>The companions a berserk leader took berserk with him (A15.41); null when he took none.</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public IReadOnlyList<FireUnitEffect>? CompanionEffects
    {
        get; init;
    }

    /// <summary>What the attack did to each vehicle in the target Location and its crew (A7.307 to A7.309, D.8B); null when there were none.</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public IReadOnlyList<FireVehicleEffect>? VehicleEffects
    {
        get; init;
    }

    /// <summary>What an OVR's Original DR of 12 did to the OVRing vehicle (D7.17; ruling R11.11); null when it rolled no 12 or was no OVR.</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public FireOverrunEffect? OverrunEffect
    {
        get; init;
    }

    /// <summary>Whether the firers' LOS was blocked (A6.11; ruling R12.2): they fired, and nothing in the target Location was affected; null is false.</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public bool? LosBlocked
    {
        get; init;
    }

    /// <summary>The attack's MOL Check (A22.611; ruling R15.4); null when none was made.</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public FireMolCheck? MolCheck
    {
        get; init;
    }

    /// <summary>Whether a DC's Original DR reached its malfunction number (A23.4; ruling R15.2): removed with no effect; null is false.</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public bool? DemolitionChargeMalfunctioned
    {
        get; init;
    }

    /// <summary>The FT whose Original DR ran it out of fuel (A22.5; ruling R15.1): removed after the attack; null when none.</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? FlamethrowerRemoved
    {
        get; init;
    }

    /// <summary>The Position DR of a DC at the AFV it names (C7.346; pass 60, ruling R60.16); null for any other attack.</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public FirePosition? Position
    {
        get; init;
    }

    /// <summary>
    /// The To Kill DR of a FT, MOL, or DC at the AFV it names, on the C7.34 HE and Flame To Kill Table (A22.34, A22.612, C7.344, C7.346; pass 60,
    /// rulings R60.15, R60.16); null when none was made.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public OrdnanceKill? VehicleKill
    {
        get; init;
    }

    /// <summary>The Specific Collateral Attack on the CE crew of the AFV a FT, MOL, or DC names (D.8A; pass 60); null when there was none.</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public OrdnanceCollateral? SpecificCollateral
    {
        get; init;
    }

    /// <summary>
    /// What the attack's KIA did to the weapons its victims held (A9.74), and a DC to the Gun of its target Location (C11.6; pass 60, ruling R60.17), in
    /// the order the units were attacked; null when there was none, and on every record made before the pass.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public IReadOnlyList<FireWeaponDestruction>? WeaponDestruction
    {
        get; init;
    }

    public const string Resolved = "resolved";
    public const string Abstained = "abstained";
    public const string Indeterminate = "indeterminate";
}

/// <summary>
/// What one attack did to a vehicle (A7.307 to A7.309) and to its Vulnerable crew (D.8B, D5.34, D5.341, A7.82). An unarmored vehicle's
/// Vehicle line result is <c>none</c>, <c>immobilized</c>, <c>eliminated</c>, or <c>burning-wreck</c>; an armored one is never harmed.
/// </summary>
public sealed record FireVehicleEffect(
    string VehicleId,
    string DefinitionId,
    string Result,
    int? KillNumber,
    IReadOnlyList<FireModifier> Drm,
    int FinalDr,
    int? UnlikelyKillDr,
    FireCheck? CrewCheck,
    string CrewResult)
{
    /// <summary>Whether the firer declined the Unlikely Kill dr it could make (A7.309, ruling R5.8); null when it made it or could not.</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public bool? UnlikelyKillDeclined
    {
        get; init;
    }

    /// <summary>The Collateral Attacks on the vehicle's Vulnerable PRC (D.8; pass 65, ruling R65.7); null when there were none.</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public IReadOnlyList<FirePrcAttack>? PrcAttacks
    {
        get; init;
    }

    /// <summary>
    /// The broken PRC the attack puts under DM (D6.66, A10.62; pass 65): a broken Passenger of an armored halftrack, BU, when the attack could have
    /// inflicted a NMC on a hypothetical CE target, and a broken Vulnerable PRC attacked by FP that could inflict one. Null when none.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public IReadOnlyList<string>? PrcDesperation
    {
        get; init;
    }

    /// <summary>The Survival DR of each PRC of the vehicle the attack eliminated without burning it (D6.9, A7.308; pass 65, ruling R65.9); null when none.</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public IReadOnlyList<PrcSurvival>? PrcSurvival
    {
        get; init;
    }

    public const string None = "none";
    public const string Immobilized = "immobilized";
    public const string Eliminated = "eliminated";
    public const string BurningWreck = "burning-wreck";
    public const string Stunned = "stunned";
    public const string Recalled = "recalled";
    public const string Pinned = "pinned";
    public const string NotVulnerable = "not-vulnerable";
}
