using System.Text.Json.Serialization;

namespace LimboDancer.Domains.Asl.Rules;

/// <summary>
/// A Gun's HE shot at Infantry on the Infantry Target Type for the Ordnance package (unit step 24; C3.3, C3.32). Every fact is
/// supplied by the caller; the package never reads a game. Nullable members are required: a missing one leaves the shot
/// undecided. The map reads (range, the Covered Arc, the LOS and its Hindrance, the target's terrain) are the planner's.
/// </summary>
public sealed record OrdnanceShot(
    string? Phase,
    string? FiringSide,
    string? FiringNationality,
    OrdnanceGun? Gun,
    OrdnanceCrew? Crew,
    string? TargetLocationId,
    int? Range,
    int? HexspinesToTurn,
    bool? FirerInWoodsOrBuilding,
    bool? ElevationAllowed,
    int? Acquisition,
    FireAttack? Hit,
    OrdnanceRolls? Rolls)
{
    /// <summary>The tank whose MA fires (ruling R7.10); null for a Gun manned by its crew.</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public OrdnanceVehicleFirer? Vehicle
    {
        get; init;
    }

    /// <summary>The vehicle a Vehicle Target Type shot fires at (C3.31; ruling R7.2); null for the Infantry Target Type.</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public OrdnanceVehicleTarget? VehicleTarget
    {
        get; init;
    }

    /// <summary>
    /// The ammunition declared (C8.1; ruling R7.6): <c>ap</c>, <c>apcr</c>, <c>apds</c>, <c>heat</c>, or <c>he</c>; null is HE that is not Special
    /// Ammunition (C2.21): a Gun whose HE has a Depletion Number fires it only when it is named (C8.8; pass 55, ruling R55.7).
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Ammunition
    {
        get; init;
    }

    /// <summary>
    /// Whether the firer's force is Elite for its Depletion Numbers (C8.2, p. 178; pass 55, ruling R55.6), which are then one higher: the game's
    /// special rule <c>elite-ammunition:&lt;side&gt;</c>. Null is false: a shot recorded before the pass was not Elite.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public bool? Elite
    {
        get; init;
    }

    /// <summary>The scenario year, which decides the special ammunition's Depletion Number (C8.9, C8.91); null when the game names none.</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? ScenarioYear
    {
        get; init;
    }

    /// <summary><c>first-fire</c> for Defensive First Fire in the MPh (C6.1, A8.1; ruling R8.1); null in a fire phase.</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? FireKind
    {
        get; init;
    }

    /// <summary>
    /// Whether the shot is the first shot of a Gun Duel (C2.2401, p. 167; pass 55, ruling R55.19): a vehicle's MA fired as Bounding First Fire
    /// while the DEFENDER's window on its MP expenditure is open, before the declared shot it answers. Null is false: no shot recorded before the
    /// pass was one.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public bool? GunDuel
    {
        get; init;
    }

    /// <summary>Whether the shot is Intensive Fire (C5.6; ruling R8.2).</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public bool? IntensiveFire
    {
        get; init;
    }

    /// <summary>
    /// Whether Mud or Deep Snow cushions the HE shot (E3.62, E3.731; rulings R16.12, R16.13): +1 TEM to the TH DR of an HE shot at an Infantry
    /// Target Type in Open Ground (never at a vehicle or its PRC). Null is false.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public bool? CushionedOpenGround
    {
        get; init;
    }

    /// <summary>
    /// Whether the target is at a higher elevation than the firer and may claim Height Advantage (B10.31, C6.8, D4.22; pass 55): Case Q, +1 on the
    /// TH DR of the Infantry and Vehicle Target Types when the target has no other positive TEM, and then no Case J4. Null is false: a shot
    /// recorded before the pass was at the firer's level.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public bool? HeightAdvantage
    {
        get; init;
    }

    /// <summary>
    /// Whether the target receives a wall's TEM from the firer (B9.3), which lets HEAT be fired at Infantry (C8.31, p. 178; pass 60, ruling R60.4): the
    /// planner's map read, made for a HEAT shot at a Location with no vehicle named. Null is false: no shot recorded before the pass was HEAT at
    /// Infantry.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public bool? WallTem
    {
        get; init;
    }

    /// <summary>
    /// Whether a Vehicle Target Type hit that does not destroy an AFV attacks its Vulnerable crew Collaterally (D.8A, p. 192; pass 60, rulings R60.3,
    /// R60.7): set by the planner on every shot at a vehicle made since the pass, and taken from the record at replay. Null is false: a hit recorded
    /// before the pass resolves as it did, with no such attack.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public bool? SpecificCollateral
    {
        get; init;
    }

    /// <summary>
    /// Whether a Critical Hit reverses the Height Advantage the target claimed on the To Hit DR into a -1 on the IFT DR (C3.71, p. 170; pass 60,
    /// rulings R60.3, R60.14): set by the planner on every shot since the pass that carries <see cref="HeightAdvantage"/>, and taken from the record at
    /// replay. Null is false: a Critical Hit recorded before the pass resolves as it did.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public bool? ReversesHeightAdvantage
    {
        get; init;
    }

    /// <summary>
    /// The Locations of a mortar's target building hex that hold its targets (B23.32, p. 136; pass 60, ruling R60.11), the named one among them: the
    /// planner's map reads, taken from the record at replay. Null for any other shot, as in every record made before the pass: the target hex is the
    /// one Location, with no level above it.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public IReadOnlyList<OrdnanceAreaLocation>? AreaLocations
    {
        get; init;
    }

    /// <summary>
    /// A mortar's Area Acquisition of another Location of the target hex (C6.521, p. 174; pass 60, ruling R60.13), when the shot names a Location its
    /// counter is not on: a state read taken from the record at replay. Null when there is none, as in every record made before the pass, which then
    /// resolves with <see cref="Acquisition"/> as it did.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? AreaAcquisition
    {
        get; init;
    }

    /// <summary>
    /// The Low Visibility DRM of night and weather on the TH DR (E1.7, E3.1; referee, pass 16): a Case R Hindrance of its own, which cancels neither FFMO
    /// nor the Open Ground cases. Null is 0.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? LowVisibilityDrm
    {
        get; init;
    }

    /// <summary>How much lower Extreme Winter makes the Gun's B# (E3.741; ruling R16.14): 1 or 2. Null is 0.</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? BreakdownReduction
    {
        get; init;
    }

    /// <summary>
    /// The captured-weapon penalty of a vehicle's Temporary crew, as a multiple (A21.22, A21.12, A21.13, p. 88; pass 65, ruling R65.22): the red To Hit
    /// numbers, and for each a +2 To Hit DRM (Case H), a B# two lower, and a Multiple ROF one lower. Null is none, which is every record made before the pass.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? CapturedPenalty
    {
        get; init;
    }

    /// <summary>The moving target's facts for Defensive First Fire (C6.11 to C6.17); required when <see cref="FireKind"/> is set.</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public OrdnanceMovement? Movement
    {
        get; init;
    }

    /// <summary>Whether the target is in the Gun's own Location (C5.5 Case E; ruling R8.8); the range is then 0.</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public bool? SameHex
    {
        get; init;
    }

    /// <summary>Whether a squad or HS of the Gun's nationality mans it (C5.8 Case H, A21.13; ruling R8.8).</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public bool? NonQualified
    {
        get; init;
    }

    /// <summary>Whether the target Location is the Gun's Bore Sighted Location, fired by its original crew from its setup Location (C6.4).</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public bool? BoreSighted
    {
        get; init;
    }

    /// <summary>
    /// Whether every target of a shot outside the MPh is under Hazardous Movement (C6.6, A4.62; pass 40, ruling R40.10): Case O. In the MPh the
    /// shot's movement facts carry it. Null is false.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public bool? HazardousTarget
    {
        get; init;
    }

    /// <summary>
    /// Whether a Good Order enemy ground unit within 16 hexes sees the firing Gun's Location, so a concealed crew and Gun lose their "?" by
    /// firing (A12.14; ruling R8.5); null leaves it to the target Location, as before.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public bool? CrewSeen
    {
        get; init;
    }

    /// <summary>The squad equivalents and vehicles by which the firer's side overstacks its Location (A5.12; ruling R8.10).</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? FirerOverstack
    {
        get; init;
    }

    /// <summary>The squad equivalents by which the target side's Personnel overstack the target Location (A5.131; ruling R8.10).</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? TargetOverstack
    {
        get; init;
    }

    /// <summary><c>area</c> for the Area Target Type, which a mortar always uses (C3.33, C9.1; ruling R9.3); null for the Infantry or Vehicle Target Type.</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? TargetType
    {
        get; init;
    }

    /// <summary>The Spotter along whose LOS a mortar fires (C9.3; ruling R9.4); null when the firer sees the target itself.</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public OrdnanceSpotter? Spotter
    {
        get; init;
    }

    /// <summary>C9.31 Player Turn use validation version; null preserves historical pre-integration replay.</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? SpotterUseVersion
    {
        get; init;
    }

    /// <summary>The leader who directs a SW's To Hit DR with his leadership modifier (A7.531, C9.2, C13.35; ruling R9.2); null for none.</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public FireDirector? Director
    {
        get; init;
    }

    /// <summary>The Panzerfaust's facts (C13.3; rulings R9.7, R9.8); required when the Gun is the Panzerfaust.</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public OrdnancePanzerfaust? Panzerfaust
    {
        get; init;
    }

    /// <summary>
    /// The aim of a Canister shot and the Locations it attacks (C8.4, C8.41, p. 178; pass 55, rulings R55.8, R55.9); required when the ammunition
    /// is <c>canister</c>, and null for every other shot, as in every record made before the pass.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public OrdnanceCanister? Canister
    {
        get; init;
    }
}

/// <summary>
/// A Canister shot's aim (C8.4, C8.41, p. 178; pass 55, ruling R55.8): which of the five vertices of the Covered Arc it is fired at, the facing
/// whose Covered Arc that is (the Gun's, after any turn the shot makes), and the Locations the planner read as attacked: those of the cluster and
/// of the adjacent hex or hexes on the LOS that are at the firer's elevation and in its LOS.
/// </summary>
public sealed record OrdnanceCanister(int? Aim, string? Facing, IReadOnlyList<OrdnanceCanisterLocation>? Locations);

/// <summary>
/// A Location a Canister shot attacks (C8.4, C8.41): a hex of the cluster, or, with <see cref="AdjacentHexes"/> 1, the hex adjacent to the firer
/// along the LOS (Area Fire, halved), or, with 2, one of the two adjacent hexes whose common hexside the LOS runs along (quartered).
/// </summary>
public sealed record OrdnanceCanisterLocation(string? LocationId)
{
    /// <summary>1 for the adjacent hex on the LOS, 2 for one of two adjacent hexes the LOS runs between (C8.41); null for a hex of the cluster.</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? AdjacentHexes
    {
        get; init;
    }
}

/// <summary>
/// What a Canister shot's IFT DR decided for the shot itself (C8.4, p. 178; pass 55, ruling R55.9): the aim and the facing, the FP of the Gun
/// Size, the DR that is also the Depletion, malfunction, and ROF check, the Case A DRM of a CA change (D3.52), the Depletion Number, and the FP
/// each Location is attacked with; no Location when the Gun had no Canister (C8.9). Each attack is a fire record of its own on the same DR.
/// </summary>
public sealed record OrdnanceCanisterResolution(int Aim, string Facing, int Firepower, IReadOnlyList<int> Dice, int OriginalDr, int CaseA, int DepletionNumber,
    IReadOnlyList<OrdnanceCanisterAttack> Attacks);

/// <summary>A Location a Canister shot attacks, with what halves its FP there and the FP it is left (C8.4, C8.41; a concealed target halves it again on the IFT).</summary>
public sealed record OrdnanceCanisterAttack(string LocationId, IReadOnlyList<FireModifier> Multipliers, decimal Firepower);

/// <summary>
/// A moving target of Defensive First Fire (C6.1 to C6.17; ruling R8.1): a vehicle's MP spent in the firer's continuous LOS; Infantry's
/// non-Assault Movement (Case J3) and a move into Open Ground (Case J4); the MF or MP the target spent in its Location; and the Gun's shots
/// at it there already.
/// </summary>
public sealed record OrdnanceMovement(int? MpInLos, bool? NonAssault, bool? OpenGround, int? SpentHere, int? ShotsHere)
{
    /// <summary>
    /// The MP of the moving vehicle the Gun's earlier shots at it in this Location already claimed (C6.17 and its EX): they count neither
    /// toward this shot's Case J1 or J2 nor toward its limit. Null is none.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? MpClaimed
    {
        get; init;
    }

    /// <summary>
    /// Whether the vehicle fired at began its MPh bogged and has not left its Bog hex (D8.4, p. 209; pass 35, task 35.13 j): its Bog Removal MP allow
    /// Defensive First Fire with no Case J; null is false.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public bool? InBogHex
    {
        get; init;
    }

    /// <summary>Whether the moving target is engaged in Hazardous Movement (C6.6, A4.62; pass 35, task 35.10): Case O in place of Case J's subcases; null is false.</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public bool? Hazardous
    {
        get; init;
    }

    /// <summary>
    /// Whether the moving target is a Dashing stack in its road Location, to a firer for whom the Dash qualifies (A4.63, C6.1; pass 40): Case J in
    /// place of Cases J3 and J4, and no shot that must change its CA. Null is false.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public bool? Dashing
    {
        get; init;
    }
}

/// <summary>
/// The firing Gun (C2.1): its reviewed catalog definition, whether it is malfunctioned, its shots so far this fire phase, whether
/// it still may fire (its Multiple ROF kept, C2.24), and whether it carries a Prep or Final Fire counter from this Player Turn (A3.5, C5.2).
/// </summary>
public sealed record OrdnanceGun(string? GunId, string? DefinitionId, bool? Malfunctioned, int? ShotsThisPhase, bool? RateOfFireKept, bool? FiredThisPlayerTurn)
{
    /// <summary>The special ammunition the Gun has run out of this scenario (C8.9), such as <c>apcr</c>, <c>apds</c>, <c>heat</c>, or <c>he</c>; null is none.</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public IReadOnlyList<string>? Depleted
    {
        get; init;
    }

    /// <summary>Whether the Gun carries a First Fire counter this Player Turn (C2.241); null is false.</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public bool? FirstFire
    {
        get; init;
    }

    /// <summary>Whether the Gun carries a Final Fire counter this Player Turn, which bars Intensive Fire (C5.6); null is false.</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public bool? FinalFire
    {
        get; init;
    }

    /// <summary>Whether the Gun carries an Intensive Fire counter this Player Turn (C5.6); null is false.</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public bool? IntensiveFired
    {
        get; init;
    }
}

/// <summary>
/// The Infantry manning the Gun (A21.13): its crew, which must be Good Order to fire it; a concealed crew loses its "?" by firing
/// (A12.14). Crews' inherent fire is not built, so <see cref="OrdnanceCrew.FiredInherentFp"/> is false in live play (A7.352; ruling R24.4).
/// </summary>
public sealed record OrdnanceCrew(string? UnitId, string? DefinitionId, bool? Broken, bool? Pinned, bool? Berserk, bool? Concealed, bool? FiredInherentFp)
{
    /// <summary>Whether the crew is CX (A4.51): +1 to the Gun's To Hit DR; null is false.</summary>
    [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
    public bool? Cx
    {
        get; init;
    }

    /// <summary>
    /// Whether the firer is Infantry using Opportunity Fire in its AFPh (A7.25, p. 55; pass 35, task 35.10): no Case B and no Case C3, and its Gun
    /// keeps a Multiple ROF (C5.2, C5.34, C13.1); null is false.
    /// </summary>
    [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
    public bool? OpportunityFire
    {
        get; init;
    }
}

/// <summary>
/// Recorded rolls: the To Hit DR (colored die first, C2.24), the subsequent dr of an Original 2 (C3.6, C3.7), a Random Selection
/// dr per target unit when a Critical Hit falls among several (C3.74), and the IFT rolls of the normal hit and of the Critical Hit.
/// </summary>
public sealed record OrdnanceRolls(IReadOnlyList<int>? ToHit, int? Subsequent, IReadOnlyDictionary<string, int>? CriticalSelection, FireRolls? Hit,
    FireRolls? CriticalHit)
{
    /// <summary>The To Kill DR (C7.1).</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public IReadOnlyList<int>? ToKill
    {
        get; init;
    }

    /// <summary>The crew's NTC of a possible Shock (C7.41).</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public IReadOnlyList<int>? ShockCheck
    {
        get; init;
    }

    /// <summary>The crew's Immobilization TC (D5.5).</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public IReadOnlyList<int>? CrewCheck
    {
        get; init;
    }

    /// <summary>The PF Check dr (C13.31; ruling R9.7).</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? PanzerfaustCheck
    {
        get; init;
    }

    /// <summary>The Crew Survival DR (D5.6).</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public IReadOnlyList<int>? CrewSurvival
    {
        get; init;
    }

    /// <summary>
    /// The IFT DR of a Canister shot (C8.4, p. 178; pass 55), colored die first: it "also serves as the Ammo Depletion, armament breakdown, and
    /// ROF checks usually resolved with the To Hit DR", which such a shot does not make.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public IReadOnlyList<int>? Canister
    {
        get; init;
    }

    /// <summary>The MC or NTC DR of the crew a Specific Collateral Attack calls on to check (D.8A, D5.34; pass 60).</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public IReadOnlyList<int>? CollateralCheck
    {
        get; init;
    }

    /// <summary>The Crew Survival DR of each AFV a mortar's hit destroyed without burning it (C1.55, D5.6; pass 60), by vehicle id.</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public IReadOnlyDictionary<string, IReadOnlyList<int>>? AreaSurvival
    {
        get; init;
    }

    /// <summary>The Immobilization TC of each AFV crew a mortar's hull hit immobilized (C1.55, D5.5; pass 65, ruling R65.21), by vehicle id.</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public IReadOnlyDictionary<string, IReadOnlyList<int>>? AreaCrewCheck
    {
        get; init;
    }

    /// <summary>
    /// The rolls of the target's Passengers and Riders (pass 65, rulings R65.7, R65.9): the checks and Random Selection of a Specific Collateral Attack
    /// on them, keyed as the Fire package keys a target unit's, and each one's Survival DR.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public FireRolls? Prc
    {
        get; init;
    }
}

/// <summary>The To Hit arithmetic (C3.3, C4, C5, C6): Basic and Modified TH#, the DRM, the Final DR, and what it achieved.</summary>
public sealed record OrdnanceToHit(
    string Color,
    int BasicToHit,
    IReadOnlyList<FireModifier> Modifications,
    int ModifiedToHit,
    IReadOnlyList<int> Dice,
    int OriginalDr,
    IReadOnlyList<FireModifier> Drm,
    int FinalDr,
    bool Improbable,
    int? SubsequentDr,
    bool Hit,
    bool CriticalHit);

/// <summary>What the shot did to the Gun: malfunction (C2.28), Multiple ROF kept (C2.24, C2.5), and the Acquisition it holds after it (C6.5).</summary>
public sealed record OrdnanceGunEffect(int BreakdownNumber, bool Malfunctioned, int RateOfFire, bool RateOfFireKept, string? FireCounter, int Acquisition,
    string? AcquiredLocationId);

/// <summary>The Ordnance package's answer: resolved with its To Hit arithmetic, the Gun's state, and the IFT resolutions of the hit.</summary>
public sealed record OrdnanceResolution(
    string Disposition,
    IReadOnlyList<string> Reasons,
    OrdnanceToHit? ToHit,
    OrdnanceGunEffect? Gun)
{
    /// <summary>The IFT resolution of a normal hit on every hit unit (or on those the Critical Hit did not select); null when none.</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public FireResolution? Hit
    {
        get; init;
    }

    /// <summary>The IFT resolution of a Critical Hit on the unit it selected (C3.71, C3.74); null when none.</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public FireResolution? CriticalHit
    {
        get; init;
    }

    /// <summary>Whether the concealed crew lost its "?" by firing (A12.14); null when it was not concealed.</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public bool? CrewConcealmentLost
    {
        get; init;
    }

    /// <summary>The To Kill resolution of a Vehicle Target Type hit (C7; ruling R7.5); null otherwise.</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public OrdnanceKill? Kill
    {
        get; init;
    }

    /// <summary>
    /// The special ammunition's use (C8.9): <c>used</c>, <c>depleted</c> (used and run out), or <c>none</c> (there was none, so the Gun did
    /// not fire); null for AP and HE that have no Depletion Number.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? AmmunitionUse
    {
        get; init;
    }

    /// <summary>
    /// The Depletion Number <see cref="AmmunitionUse"/> was decided under (C8.9, C8.2), an Elite force's one more included; null when the shot
    /// had none, in a Canister resolution, whose own block names it, and in a record made before the reviews of pass 55, which is compared
    /// without it.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? DepletionNumber
    {
        get; init;
    }

    /// <summary>
    /// What a hit did to the Gun in the target Location (C11.4, C11.6; ruling R8.3): <c>destroyed</c> by a KIA before its gunshield or a
    /// Critical Hit, <c>malfunctioned</c> by a K; null when it was untouched or there is none.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? GunTargetFate
    {
        get; init;
    }

    /// <summary>
    /// The HE Equivalency FP the hit attacked with (C8.31, p. 178; pass 60, ruling R60.4): AP, APCR, APDS, HEAT, an ATR, a PF, or a PSK on the
    /// Infantry Target Type. Null for HE, as in every record made before the pass.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? HeEquivalency
    {
        get; init;
    }

    /// <summary>The unit a Critical Hit among several targets fell on by Random Selection (C3.74); null otherwise.</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? CriticalTarget
    {
        get; init;
    }

    /// <summary>The To Hit DR as judged for each unit of an Area Target Type shot's hex (C3.331; ruling R9.3); null otherwise.</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public IReadOnlyList<OrdnanceAreaTarget>? AreaTargets
    {
        get; init;
    }

    /// <summary>
    /// The IFT resolutions of a mortar's normal hit on the other Locations of a building hex it hit (B23.32, p. 136; pass 60, ruling R60.11), on the
    /// same DR as <see cref="Hit"/>, which holds the first; null when one Location was hit, as in every record made before the pass.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public IReadOnlyList<FireResolution>? LevelHits
    {
        get; init;
    }

    /// <summary>
    /// What a mortar's hit did to each AFV it hit, on the IFT (C1.55, C9.1, p. 165; pass 60, ruling R60.10); null when it hit none, as in every record
    /// made before the pass. Its unarmored vehicles and AFV crews are in the IFT resolutions' vehicle effects.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public IReadOnlyList<OrdnanceAreaVehicle>? AreaVehicles
    {
        get; init;
    }

    /// <summary>
    /// The To Hit DR as judged for each unit and vehicle of an Infantry Target Type shot at a Location holding a vehicle, or concealed and Known units
    /// together (C3.32, C3.4, p. 169; pass 60, rulings R60.8, R60.9): its own DRM, its Final DR, and whether the shot hit it. Null for every other
    /// shot, as in every record made before the pass.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public IReadOnlyList<OrdnanceAreaTarget>? TargetHits
    {
        get; init;
    }

    /// <summary>
    /// The Specific Collateral Attack on the Vulnerable crew of an AFV a Vehicle Target Type hit did not destroy (D.8A, p. 192; pass 60, ruling
    /// R60.7); null when there was none, as in every record made before the pass.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public OrdnanceCollateral? SpecificCollateral
    {
        get; init;
    }

    /// <summary>The PF Check of a Panzerfaust shot (C13.31; ruling R9.7); null otherwise.</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public OrdnancePanzerfaustCheck? PanzerfaustCheck
    {
        get; init;
    }

    /// <summary>
    /// What the shot did to its own firer (C13.31, C13.36; ruling R9.7): <c>pinned</c>, <c>broken</c>, or <c>casualty-reduction</c>; null for
    /// nothing.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? FirerEffect
    {
        get; init;
    }

    /// <summary>A Canister shot's own part of its resolution (C8.4; pass 55, ruling R55.9); null for every other shot.</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public OrdnanceCanisterResolution? Canister
    {
        get; init;
    }

    public const string Resolved = "resolved";
    public const string Abstained = "abstained";
    public const string Indeterminate = "indeterminate";
}

/// <summary>
/// A Specific Collateral Attack (D.8, D.8A, p. 192; pass 60, ruling R60.7): the round's IFT FP (its HE FP, or its HE Equivalency, C8.31), the IFT
/// result of the Original TK DR with the CE DRM alone, and what it did to the crew, as an IFT attack's General Collateral Attack is recorded.
/// </summary>
public sealed record OrdnanceCollateral(int Firepower, string Result, FireVehicleEffect Effect);

/// <summary>
/// One ammunition a Gun may be asked to fire at a kind of target (C8.1; pass 55), for the list a player chooses from: its words with the
/// Depletion Number it has at the game's date (C8.9), one more for an Elite force (C8.2), and, when the Gun cannot fire it now, why.
/// </summary>
public sealed record AmmunitionChoice(string Ammunition, string Label, int? DepletionNumber, string? Unavailable)
{
    /// <summary>
    /// At Infantry, what the round attacks with by its HE Equivalency (C8.31; pass 60), such as "2 FP by HE Equivalency, C8.31"; null for HE and
    /// Canister, and at a vehicle.
    /// </summary>
    public string? Equivalency
    {
        get; init;
    }
}

/// <summary>A Gun as the Ordnance package reads its reviewed catalog definition (C2.21 to C2.3).</summary>
public sealed record GunDefinition(
    string Id,
    string Nationality,
    string GunType,
    int Caliber,
    string? Suffix,
    int? RateOfFire,
    int Breakdown,
    int? RangeMaximum,
    bool NoHe,
    bool Mount360)
{
    /// <summary>Whether it may not fire AP (C2.21); false for a Gun whose listing does not say so.</summary>
    public bool NoAp
    {
        get; init;
    }

    /// <summary>Its special ammunition as the counter lists it (C8.9): items such as <c>H7</c>, <c>A4^1</c> (from 1941), or <c>HE7^3m2</c> (from February 1943).</summary>
    public IReadOnlyList<string> SpecialAmmo
    {
        get; init;
    } = [];

    /// <summary>The MA type of a tank's gun (D1.3); null for a Gun.</summary>
    public string? MaType
    {
        get; init;
    }

    /// <summary>A Gun's Target Size (C2.271): <c>small</c>, <c>average</c>, or <c>large</c>; null for a tank.</summary>
    public string? TargetSize
    {
        get; init;
    }

    /// <summary>A Gun's Manhandling Number (C2.27, C10.3); null for a tank.</summary>
    public int? Manhandling
    {
        get; init;
    }

    /// <summary>A mortar's minimum range (C9.4); null for none.</summary>
    public int? RangeMinimum
    {
        get; init;
    }

    /// <summary>A LATW's type (C13.1): <c>pf</c>, <c>psk</c>, or <c>atr</c>; null for other ordnance.</summary>
    public string? LatwType
    {
        get; init;
    }

    /// <summary>A LATW's own To Hit Table, the Basic TH# at ranges 1, 2, ... (C13.42); empty when it has none.</summary>
    public IReadOnlyList<int> ToHitTable
    {
        get; init;
    } = [];

    /// <summary>The weapon's row of the C7.33 HEAT To Kill Table when it is not its caliber, such as <c>PF (Oct43)</c> (C13.34).</summary>
    public string? HeatRow
    {
        get; init;
    }

    /// <summary>A MG's IFT FP, which its Specific Collateral Attack uses (A9.611; pass 60); null for any other weapon.</summary>
    public int? Firepower
    {
        get; init;
    }
}
