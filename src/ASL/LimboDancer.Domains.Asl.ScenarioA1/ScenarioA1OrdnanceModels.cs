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

    /// <summary>The unit a Critical Hit among several targets fell on by Random Selection (C3.74); null otherwise.</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? CriticalTarget
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
}
