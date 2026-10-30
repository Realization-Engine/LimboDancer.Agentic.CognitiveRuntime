namespace LimboDancer.Domains.Asl.Rules;

/// <summary>
/// A MG's To Kill attack on an AFV (A9.6, A9.61, A9.611, pp. 64 and 65; D3.54, p. 201; pass 60, ruling R60.19): a SW MG fired alone, or a vehicle's
/// MA that is a MG, fires as ordnance on the Vehicle Target Type with the black To Hit numbers, within its Normal Range and with no halved FP, at a
/// named AFV. Its To Kill DR uses the AP To Kill Table's @MG entry, Basic TK# 4 (p. 190), Case A through the rear (C7.21), and Case D's row of 25mm
/// and below (C7.24), less the AF: at most half the Final TK# burns, below it eliminates, equal Stuns even a BU AFV (D5.34), above it nothing. No
/// Critical Hit, Multiple Hit (C3.8), Shock, or Immobilization; an Original 12 is a Dud (C7.35). The Residual FP of a MG fired so and its fire in
/// the AFPh are backlog.
/// </summary>
public static class ScenarioA1MachineGunKill
{
    private const string Prefix = "asl.a1.ordnance.";

    /// <summary>The Gun type of a MG fired as ordnance.</summary>
    public const string GunType = "mg";

    /// <summary>The AP To Kill Table's @MG Basic TK# (C7.31, p. 190). No MG of the catalog is a 12.7mm or 15mm one, whose rows are higher.</summary>
    public const int BasicTk = 4;

    /// <summary>The ammunition a MG's To Kill attack is recorded with: AP, on whose table it is resolved (A9.6).</summary>
    public const string Ammunition = "ap";

    /// <summary>
    /// A MG as the ordnance shot reads it: a SW MG of the catalog, by its counter (FP, Normal Range, ROF, B#), or a vehicle whose MA is a MG (D1.3,
    /// D3.54), by the vehicle's printed ROF and B# and its MA's FP and Normal Range (D1.83). Null for any other definition. Its caliber is read as 0, so
    /// that Case D takes the row of 25mm and below.
    /// </summary>
    public static GunDefinition? Gun(ScenarioA1FireReference fire, string definitionId)
    {
        ArgumentNullException.ThrowIfNull(fire);
        if (fire.Definitions.GetValueOrDefault(definitionId) is not { } definition)
        {
            return null;
        }

        if (definition.IsMg && definition.Firepower is { } fp && definition.Range is { } range)
        {
            return new GunDefinition(definition.Id, definition.Nationality, GunType, 0, null, definition.RateOfFire, definition.Breakdown ?? 12, range, true, false)
            {
                Firepower = fp
            };
        }

        return definition.IsVehicle && MainArmamentFirepower(fire, definition) is { } maFp
            ? new GunDefinition(definition.Id, definition.Nationality, GunType, 0, null, definition.RateOfFire, definition.Breakdown ?? 12,
                ScenarioA1FireCalculator.VehicleWeaponRange(null), true, false)
            {
                Firepower = maFp
            }
            : null;
    }

    /// <summary>
    /// The IFT FP of a vehicle's MA that is a MG (D1.3, D1.8): an AAMG's printed FP; for a MA printed as a plain MG with no FP of its own (the
    /// manufactured Bren Carrier), the FP of its nationality's LMG counter, decided for pass 60. Null when the MA is no MG or no FP is found.
    /// </summary>
    public static int? MainArmamentFirepower(ScenarioA1FireReference fire, FireDefinition vehicle)
    {
        ArgumentNullException.ThrowIfNull(fire);
        ArgumentNullException.ThrowIfNull(vehicle);
        return vehicle.MainArmament switch
        {
            "aamg" => vehicle.AntiAircraftMg,
            "mg" => fire.Definitions.Values.Where(item => item.IsMg && item.Size == "light" && item.Nationality == vehicle.Nationality)
                .OrderBy(item => item.Id, StringComparer.Ordinal).Select(item => item.Firepower).FirstOrDefault(),
            _ => null,
        };
    }

    /// <summary>
    /// D3.54 (p. 201): "A vehicular-mounted MG may not attempt a To Kill attack unless it is the vehicle's MA. Another MG may never substitute for
    /// the listed MA of a vehicle." The refusal of a vehicle's named weapon, or null for its MA.
    /// </summary>
    public static string? VehicleWeaponRefusal(string vehicleId, string? weapon) =>
        weapon is null or FireOverrunEffect.MainArmament ? null
            : $"play.mg-to-kill: {vehicleId}'s {weapon.ToUpperInvariant()} makes no To Kill attack; only a vehicle's MA that is a MG does (D3.54)";

    /// <summary>
    /// The reasons a MG's To Kill attack is outside (A9.61, D3.53, D3.54): it names an AFV and fires AP; in the PFPh or DFPh, or as Defensive First
    /// Fire, never in the AFPh (backlog) nor as Bounding First Fire; with no halved FP: not by a pinned possessor, a lone SMC (A9.12), or a vehicle in
    /// Motion (D2.42); not by a BU vehicle (an AAMG fires only CE, D1.83); and no Intensive Fire.
    /// </summary>
    internal static void Outside(OrdnanceShot shot, ScenarioA1OrdnanceReference reference, List<string> outside)
    {
        var crewKind = reference.Fire.Definitions.GetValueOrDefault(shot.Crew?.DefinitionId ?? string.Empty)?.Kind;
        if (shot.VehicleTarget is null || shot.Ammunition != Ammunition || shot.IntensiveFire == true
            || Bar(shot.Phase, shot.FireKind == ScenarioA1FireCalculator.BoundingFirstFire, shot.Crew?.Pinned == true, crewKind is "asl:leader" or "asl:hero",
                shot.Vehicle?.InMotion == true, shot.Vehicle?.ButtonedUp == true) is not null)
        {
            outside.Add(Prefix + "mg-to-kill-outside");
        }

        // A9.61, A7.23 (pass 60, ruling R60.27): at a concealed AFV the IFT would halve the MG's FP, which A9.61 does not allow.
        if (shot.VehicleTarget?.Concealed == true)
        {
            outside.Add(Prefix + "mg-to-kill-concealed");
        }
    }

    /// <summary>
    /// Why a MG may not make a To Kill attack now (A9.61, D2.42, D3.53, D1.83; pass 60), in a few words for the Gun panel, or null when it may: in the
    /// AFPh (backlog), as Bounding First Fire, by a pinned possessor, by a lone SMC (A9.12), or as the MA of a vehicle in Motion or BU. Phases as the
    /// shot names them ("AFPh") or as the state does ("afph").
    /// </summary>
    public static string? Bar(string? phase, bool boundingFirstFire, bool pinned, bool smcAlone, bool vehicleInMotion, bool vehicleButtonedUp) =>
        string.Equals(phase, "AFPh", StringComparison.OrdinalIgnoreCase) ? "no To Kill attack in the AFPh (A9.61)"
        : boundingFirstFire ? "no To Kill attack as Bounding First Fire (A9.61)"
        : pinned ? "its possessor is pinned (A9.61)"
        : smcAlone ? "a SMC alone halves its FP (A9.12, A9.61)"
        : vehicleInMotion ? "its vehicle is in Motion (D2.42)"
        : vehicleButtonedUp ? "its vehicle is BU: an AAMG fires only CE (D1.83)"
        : null;

    /// <summary>
    /// The C7.7 result of a MG's To Kill DR (A9.61, D5.34): a Dud on an Original 12, a burn at most half the Final TK#, an elimination below it, a Stun on it
    /// (<see cref="ScenarioA1StunCalculator.MachineGunStuns"/>, the D5.34 claim).
    /// </summary>
    public static string Result(int dr, int finalTk) =>
        dr == 12 ? OrdnanceKill.Dud
            : dr * 2 <= finalTk ? OrdnanceKill.Burn
            : dr < finalTk ? OrdnanceKill.Eliminated
            : ScenarioA1StunCalculator.MachineGunStuns(dr, finalTk) ? OrdnanceKill.Stun
            : OrdnanceKill.None;
}
