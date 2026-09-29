using System.Text.Json.Serialization;

namespace LimboDancer.Domains.Asl.ScenarioA1;

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

    /// <summary>The ammunition declared (C8.1; ruling R7.6): <c>ap</c>, <c>apcr</c>, <c>heat</c>, or <c>he</c>; null is HE.</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Ammunition
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
}

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
}

/// <summary>
/// The firing Gun (C2.1): its reviewed catalog definition, whether it is malfunctioned, its shots so far this fire phase, whether
/// it still may fire (its Multiple ROF kept, C2.24), and whether it carries a Prep or Final Fire counter from this Player Turn (A3.5, C5.2).
/// </summary>
public sealed record OrdnanceGun(string? GunId, string? DefinitionId, bool? Malfunctioned, int? ShotsThisPhase, bool? RateOfFireKept, bool? FiredThisPlayerTurn)
{
    /// <summary>The special ammunition the Gun has run out of this scenario (C8.9): <c>apcr</c> or <c>heat</c>; null is none.</summary>
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
    /// not fire); null for AP and HE.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? AmmunitionUse
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

    public const string Resolved = "resolved";
    public const string Abstained = "abstained";
    public const string Indeterminate = "indeterminate";
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

    /// <summary>Its special ammunition as the counter lists it (C8.9): items such as <c>H7</c> or <c>A4/1941</c>.</summary>
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
}
