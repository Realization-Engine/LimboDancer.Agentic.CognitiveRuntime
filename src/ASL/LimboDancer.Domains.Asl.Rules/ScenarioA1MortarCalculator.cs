namespace LimboDancer.Domains.Asl.Rules;

/// <summary>
/// C9 Mortars (p. 179) less C9.3 and C9.31, the one calculator of C9.1, C9.2, C9.4, and C9.5 (the C9 claim's design, ruling RC9.1): what a mortar
/// is, its Target Type and Indirect Fire, the light mortar as a SW, its minimum range, and its Critical Hit. The resolution itself (the IFT attack,
/// Air Bursts, the TEM, C3.71's doubling) stays in <see cref="ScenarioA1AreaCalculator"/> and <see cref="ScenarioA1FireCalculator"/>, which ask this
/// calculator at the points C9 governs. The Spotter is <see cref="ScenarioA1SpotterCalculator"/>'s. Facts in, verdicts out.
/// </summary>
public static class ScenarioA1MortarCalculator
{
    /// <summary>C9.2: a mortar of 60mm or less is a light mortar.</summary>
    public const int LightCaliber = 60;

    /// <summary>A21.13: a lone SMC may fire a mortar of 82mm or less as if it were a light mortar.</summary>
    public const int LoneSmcCaliber = 82;

    /// <summary>
    /// Ruling RC9.2: a mortar is a light mortar SW (kind <c>asl:light-mortar</c>) or a Gun whose gun type is a mortar, in either spelling the code
    /// has ("mortar", "mtr").
    /// </summary>
    public static bool IsMortar(string? kind, string? gunType) => kind == "asl:light-mortar" || gunType is "mortar" or "mtr";

    /// <summary>Ruling RC9.2 (C9.2): a light mortar is one of 60mm or less; an unknown caliber is not light.</summary>
    public static bool IsLight(int? caliber) => caliber is > 0 and <= LightCaliber;

    /// <summary>Ruling RC9.3 (C9.1, C.1): a mortar's fire is resolved as Indirect Fire, whatever its LOS.</summary>
    public static bool IndirectFire(bool isMortar) => isMortar;

    /// <summary>
    /// Ruling RC9.3: whether a recorded hit is Indirect Fire, where the hit carries its Target Type and not its weapon (the recorded fact records keep
    /// their shape). Only a mortar fires on the Area Target Type (<see cref="ScenarioA1OrdnanceCalculator"/>'s Outside), so an Area hit is a
    /// mortar's. When a Gun's Area HE fire is built, this is the one place that must then read the weapon.
    /// </summary>
    public static bool IndirectFireOfHit(bool areaTargetType) => IndirectFire(isMortar: areaTargetType);

    /// <summary>
    /// Ruling RC9.3 (C9.1, C3.33): a mortar fires only on the Area Target Type. Null for a non-mortar, whose Target Type is not C9's (C3.33 allows a
    /// Gun's Area fire; that it is not built is the ordnance calculator's limit).
    /// </summary>
    public static string? TargetTypeRefusal(bool isMortar, bool areaTargetType) =>
        isMortar && !areaTargetType ? "A mortar always fires on the Area Target Type (C9.1)." : null;

    /// <summary>
    /// Ruling RC9.3 (C9.1, C1.55): a mortar never uses the To Kill Tables, so it names no vehicle as a To Kill target; its hits on vehicles are the
    /// Area Target Type's, resolved on the IFT.
    /// </summary>
    public static string? VehicleTargetRefusal(bool isMortar, bool namesVehicle) =>
        isMortar && namesVehicle ? "A mortar never uses the To Kill Tables; its hits on vehicles are resolved on the IFT (C9.1, C1.55)." : null;

    /// <summary>Ruling RC9.4 (C9.2): a light mortar has no CA, so it does not turn and takes no To Hit DRM for fire outside one.</summary>
    public static bool HasCoveredArc(bool isLight) => !isLight;

    /// <summary>Ruling RC9.4 (C9.2): any squad or HS fires a light mortar with no detriment; no crew is needed.</summary>
    public static bool CrewNeeded(bool isLight) => !isLight;

    /// <summary>Ruling RC9.4 (C9.2, A21.13): a light mortar, or a mortar of 82mm or less fired by a lone SMC, fires as a light mortar.</summary>
    public static bool FiresAsLight(int? caliber, bool loneSmc) => IsLight(caliber) || (loneSmc && caliber is > 0 and <= LoneSmcCaliber);

    /// <summary>
    /// Ruling RC9.4 (C9.2, A21.13): a lone SMC firing a mortar loses its Multiple ROF; it fires one as a light mortar or not at all. Two SMC firing
    /// together are one combined unit (A7.5, A9.12, the SW Chart's note C) and are not lone.
    /// </summary>
    public static bool MultipleRofKept(bool loneSmc) => !loneSmc;

    /// <summary>A21.13: a lone SMC fires a mortar only as a light mortar, so only one of 82mm or less.</summary>
    public static string? LoneSmcRefusal(int? caliber, bool loneSmc) =>
        loneSmc && !FiresAsLight(caliber, loneSmc) ? "A lone SMC fires only a mortar of 82mm or less, as a light mortar (A21.13)." : null;

    /// <summary>Ruling RC9.4 (C9.2, C6.521): a mortar may use Area Target Acquisition.</summary>
    public static bool UsesAreaAcquisition(bool isMortar) => isMortar;

    /// <summary>C9.4: the range is below a recorded minimum. The ordnance calculator's check for every Gun (ruling RC9.5).</summary>
    public static bool BelowMinimum(int? range, int? minimum) => minimum is { } least && range < least;

    /// <summary>
    /// Ruling RC9.5 (C9.4): a mortar never fires at a target nearer than its minimum range. An unknown range is refused; an unknown minimum is
    /// refused for a mortar only, since a Gun with none recorded has none.
    /// </summary>
    public static string? MinimumRangeRefusal(bool isMortar, int? range, int? minimum) =>
        range is null ? "The range to the target is not known (C9.4)."
        : isMortar && minimum is null ? "The mortar's minimum range is not known (C9.4)."
        : BelowMinimum(range, minimum) ? $"A mortar may not fire at a target nearer than its minimum range of {minimum} (C9.4)."
        : null;

    /// <summary>
    /// The verdict of a mortar's shot (rulings RC9.3 to RC9.5), its first refusal in this order: the Target Type, a vehicle named, a lone SMC's
    /// caliber, the minimum range.
    /// </summary>
    public static MortarShotVerdict Shot(MortarShotFacts facts)
    {
        ArgumentNullException.ThrowIfNull(facts);
        var refusal = TargetTypeRefusal(facts.Mortar, facts.AreaTargetType)
            ?? VehicleTargetRefusal(facts.Mortar, facts.NamesVehicle)
            ?? (facts.Mortar ? LoneSmcRefusal(facts.Caliber, facts.LoneSmc) : null)
            ?? MinimumRangeRefusal(facts.Mortar, facts.Range, facts.MinimumRange);
        return new MortarShotVerdict(refusal, facts.Mortar && FiresAsLight(facts.Caliber, facts.LoneSmc), MultipleRofKept(facts.Mortar && facts.LoneSmc));
    }

    /// <summary>
    /// Ruling RC9.6 (C9.5, C3.7): a mortar's Critical Hit comes only on an Original To Hit DR of 2, never on the Infantry Target Type's Final DR below
    /// half the TH#. C3.7's subsequent dr of 1 and C3.74's selection stay in <see cref="ScenarioA1AreaCalculator"/>.
    /// </summary>
    public static bool CriticalHitAllowed(int originalToHitDr) => originalToHitDr == 2;

    /// <summary>
    /// Ruling RC9.6 (C9.5): a Critical Hit on the Area Target Type takes the full HE FP instead of half; C3.71 then doubles it, in the fire
    /// calculator's arithmetic.
    /// </summary>
    public static int CriticalFirepower(int heFirepower) => heFirepower;
}
