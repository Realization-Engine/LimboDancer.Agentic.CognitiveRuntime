namespace LimboDancer.Domains.Asl.Rules;

/// <summary>
/// A24.5 SMOKE Strength (p. 92), the one calculator of the rule (the A24.5 claim's design, ruling RA24.1): each SMOKE source's Hindrance and the side
/// a placement takes. A Location's total (A24.2's +3 at most) and the outgoing +1 (A24.8) stay with <see cref="ScenarioA1VehicleSightRules.SmokeDrm(IReadOnlyList{int}, bool, bool, Func{bool})"/>,
/// which takes these values. "A Mild Breeze does not lower the Hindrance DRM of a SMOKE source": nothing lowers it, so nothing is computed (RA24.4).
/// A Blaze's SMOKE is B25.2's (RA24.6). Facts in, verdicts out.
/// </summary>
public static class ScenarioA1SmokeStrengthCalculator
{
    /// <summary>
    /// Ruling RA24.2: a SMOKE source's Hindrance DRM, or null when a fact is unknown or the source is another rule's. A full counter +3, or +2 for WP; a
    /// Dispersed one one less (A24.5); a grenade +2, or +1 for WP, never Dispersed (A24.5, A24.11, D13.35); a dispenser's white Dispersed SMOKE +2
    /// (D13.31 to D13.34); drifting SMOKE the Dispersed value (A24.61). Blaze, Cave, and Napalm SMOKE are other rules'.
    /// </summary>
    public static int? Hindrance(TurnSmokeSource source, TurnSmokeChemical chemical, TurnSmokeStrength strength)
    {
        Validate(source, chemical);
        if (!Enum.IsDefined(strength))
        {
            throw new ArgumentException("Unknown SMOKE strength.", nameof(strength));
        }

        if (chemical == TurnSmokeChemical.Unknown)
        {
            return null;
        }

        var full = chemical == TurnSmokeChemical.Conventional ? 3 : 2;
        return source switch
        {
            TurnSmokeSource.InfantryGrenade or TurnSmokeSource.VehicleGrenade => strength == TurnSmokeStrength.Dispersed ? null : full - 1,
            TurnSmokeSource.Ordnance or TurnSmokeSource.OffboardArtillery => strength switch
            {
                TurnSmokeStrength.Full => full,
                TurnSmokeStrength.Dispersed => full - 1,
                _ => null,
            },
            _ when Dispenser(source) => chemical == TurnSmokeChemical.Conventional && strength != TurnSmokeStrength.Full ? full - 1 : null,
            TurnSmokeSource.Drift => strength == TurnSmokeStrength.Full ? null : full - 1,
            _ => null,
        };
    }

    /// <summary>
    /// Ruling RA24.3 (A24.5; D13.31 to D13.34): the side a placement takes and its Hindrance. Full only for ordnance or OBA in the owner's PFPh as the
    /// first resolved attack, "before non-SMOKE attacks are made"; Dispersed otherwise; a dispenser always Dispersed; unknown when an ordnance or OBA
    /// placement's window is not known, or for a source this rule does not place. A24.5 refuses no placement: it only chooses the side.
    /// </summary>
    public static TurnWhiteSmokePlacement PlacementStrength(TurnSmokeSource source, TurnSmokeChemical chemical, bool ownerPrepFire, bool? beforeNonSmokeFire)
    {
        Validate(source, chemical);
        if (!Supported(source) || chemical == TurnSmokeChemical.Unknown
            || Dispenser(source) && chemical != TurnSmokeChemical.Conventional)
        {
            return new(TurnSmokeStrength.Unknown, null, "Source/type-specific placement remains unevaluated; this contract does not authorize placement.");
        }

        if (!Dispenser(source) && ownerPrepFire && beforeNonSmokeFire is null)
        {
            return new(TurnSmokeStrength.Unknown, null, "The early SMOKE window is unknown; full strength cannot be assumed.");
        }

        var full = !Dispenser(source) && ownerPrepFire && beforeNonSmokeFire == true;
        var hindrance = (chemical == TurnSmokeChemical.Conventional ? 3 : 2) - (full ? 0 : 1);
        return new(full ? TurnSmokeStrength.Full : TurnSmokeStrength.Dispersed, hindrance,
            "Strength only (A24.5; D13.31-.34). Placement eligibility, weather and consequences require their own rules.");
    }

    /// <summary>
    /// The values a count of SMOKE sources stands for in the old count members (ruling RA24.5): +2 each, a smoke grenade's (and a Wreck Blaze's, B25.2).
    /// A negative count, which no caller makes, keeps its old arithmetic as one value.
    /// </summary>
    public static IReadOnlyList<int> CountedSources(int sources) => sources >= 0 ? [.. Enumerable.Repeat(2, sources)] : [2 * sources];

    /// <summary>Ordnance, OBA, and the dispensers: the sources whose placement A24.5 and D13.31 to D13.34 give a side.</summary>
    internal static bool Supported(TurnSmokeSource source) => source is TurnSmokeSource.Ordnance or TurnSmokeSource.OffboardArtillery || Dispenser(source);

    /// <summary>D13.31 to D13.34: the smoke discharger, mortar, pots, and the Nahverteidigungswaffe, which place white Dispersed SMOKE.</summary>
    internal static bool Dispenser(TurnSmokeSource source) => source is TurnSmokeSource.SmokeDischarger or TurnSmokeSource.SmokeMortar
        or TurnSmokeSource.SmokePots or TurnSmokeSource.CloseDefenseWeapon;

    private static void Validate(TurnSmokeSource source, TurnSmokeChemical chemical)
    {
        if (!Enum.IsDefined(source) || !Enum.IsDefined(chemical))
        {
            throw new ArgumentException("Unknown SMOKE source or chemical.");
        }
    }
}
