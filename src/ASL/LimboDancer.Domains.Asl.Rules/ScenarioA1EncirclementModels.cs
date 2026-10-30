namespace LimboDancer.Domains.Asl.Rules;

/// <summary>
/// A unit as A7.7 (p. 57) asks whether it may be Encircled (the D5.34 claim's design, rulings RD5.7, RD5.11): its kind ("asl:hero" and "asl:vehicle" are
/// never Encircled), whether it is in Melee, berserk, or heroic, and three facts the state does not yet gather: whether it is in a pillbox (B30.32,
/// p. 150: "Neither a pillbox nor its contents can be Encircled"), whether it is aboard a vehicle as a Passenger, Rider, or crew (PRC), and whether, so
/// aboard, it is the Vulnerable PRC of an Immobile vehicle, the only PRC A7.7 names. Null for any of the three reads as before the claim: not in a
/// pillbox, not aboard.
/// </summary>
public sealed record EncirclementSubjectFacts(string? Kind, bool Melee, bool Berserk, bool Heroic, bool? InPillbox, bool? Aboard, bool? VulnerablePrcOfImmobileVehicle)
{
    /// <summary>
    /// Whether the unit is Aerial (A7.7, p. 57: "any non-Aerial Infantry" may be Encircled), which no unit in play is today. Null reads as before the
    /// claim: not Aerial.
    /// </summary>
    public bool? Aerial
    {
        get; init;
    }
}

/// <summary>
/// The kind of fire an attack's unit makes, as A7.7 (p. 57; ruling RD5.8) sorts it: "using its inherent-FP/SW, ordnance, or vehicular armament" counts;
/// Residual FP and any other fire (a FFE, a DC, an Aerial attack) do not.
/// </summary>
public enum EncirclementFireKind
{
    InherentFp,
    SupportWeapon,
    Ordnance,
    VehicularArmament,
    Residual,
    Other,
}

/// <summary>
/// One unit's fire on the target Location, as A7.7 (p. 57) and A10.532 (p. 67) read it for an Encirclement (rulings RD5.8, RD5.9, RD5.15): the phase
/// ("PFPh", "DFPh", "AFPh", any case), the kind, the range in hexes, the Normal Range of the longest-ranged weapon the unit fires (its inherent FP's or a
/// SW's, ruling RD5.9; a Gun's or vehicular weapon's), whether an ordnance shot hit, whether the attack could inflict at least a NMC with Cowering
/// counted (the caller asks <see cref="ScenarioA1ResultTables.CouldCauseNmc"/>), and whether the unit is Aerial. <see cref="AboveOrBelow"/> is a LOF
/// from the Location directly above or below the target in a building, at range 0 (A7.7). Null is unknown, and an unknown fact is no valid fire.
/// </summary>
public sealed record EncirclementFireFacts(string? Phase, EncirclementFireKind Kind, int? Range, int? NormalRange, bool? Hit, bool? CouldInflictNmc, bool Aerial)
{
    /// <summary>Whether the fire is a LOF from the Location directly above or below the target in a building hex (A7.7, p. 57).</summary>
    public bool AboveOrBelow
    {
        get; init;
    }
}
